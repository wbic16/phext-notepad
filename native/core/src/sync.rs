//! Scroll-level sync with a phext-edit server (edit.phext.io).
//!
//! The server holds one open phext per process and speaks a small JSON
//! API (mapped from phext-lattice's `phext_edit.rs` / `oauth.rs`):
//!
//! * `GET  /api/session` — who am I: `{signed_in, editor, who, csrf, …}`
//! * `GET  /api/phexts` — the library: `[{name, path, bytes}]`
//! * `POST /api/open` — body is a library path; switches the open phext
//! * `GET  /api/status` — `{scrolls, bytes, file, dirty}` of the open one
//! * `GET  /api/tree` — `{coordinates: [...]}` of the open phext
//! * `GET  /api/scroll/{coord}` — `{coordinate, content, bytes}`
//! * `POST /api/update` — `{coordinate, content}` writes in memory
//! * `POST /api/save` — persists the open phext
//!
//! There is no per-scroll version on the server, so sync versions
//! scrolls itself: every scroll's base is the SHA-256 of the server
//! text at the last sync, kept in a [`SyncBook`]. A push re-reads the
//! scroll first and refuses to overwrite when the server text no
//! longer matches the base — that scroll is reported as a
//! [`Conflict`], never silently clobbered. The check is read-then-write
//! (the API offers nothing atomic), so a writer racing the exact
//! instant of a push can still interleave; the window is one round
//! trip and the next pull surfaces it as a conflict rather than
//! losing text.
//!
//! Auth is the server's OAuth session only: the `phext_session`
//! cookie (HMAC-sealed by the server, 14-day life) plus the session's
//! CSRF token in `x-csrf-token` on POSTs, exactly as the web editor
//! presents them. See [`StoredSession`] for how the cookie is kept.
//!
//! Sign-in prefers the server's desktop SSO flow when it is armed
//! (`PHEXT_SSO_DESKTOP=1` there): the app opens a loopback listener
//! ([`LoopbackCatch`]), sends the browser to
//! `GET /api/auth/sso/desktop` with that listener as the return, and
//! POSTs the assertion it catches to
//! `POST /api/auth/desktop/exchange`. The server verifies the
//! assertion itself — a shipped app cannot hold the SSO secret, so
//! the secret never leaves the server — burns its single-use id, and
//! answers with a freshly sealed session value, which the app then
//! stores and presents exactly like a cookie the user pasted. When
//! the desktop endpoint answers 404 (unarmed, or a server older than
//! the flow) the app falls back to that paste; see
//! [`EditClient::desktop_sso_probe`].

use std::collections::{BTreeMap, HashSet};
use std::io::{Read, Write};
use std::net::TcpListener;
use std::path::Path;
use std::sync::atomic::{AtomicU64, Ordering};
use std::time::{Duration, Instant, SystemTime, UNIX_EPOCH};

use libphext::phext::Coordinate;
use serde_json::{json, Value};

use crate::document::{parse_coordinate, Document};
use crate::sha256::sha256_hex;

/// A sync failure. The text is shown to the user as-is; where the
/// server answered with its own message (a refusal, a bad coordinate)
/// that message is carried verbatim.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct SyncError(pub String);

impl std::fmt::Display for SyncError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str(&self.0)
    }
}

impl std::error::Error for SyncError {}

impl From<String> for SyncError {
    fn from(message: String) -> Self {
        Self(message)
    }
}

type Result<T> = std::result::Result<T, SyncError>;

/// SHA-256 of a scroll's text — its sync version stamp. An absent
/// scroll and an empty scroll are the same thing in phext, and both
/// hash as the empty string.
pub fn scroll_hash(text: &str) -> String {
    sha256_hex(text.as_bytes())
}

/// One entry of the server's library (`GET /api/phexts`).
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct PhextFile {
    pub name: String,
    /// Server-side path. Opaque to the client except as the argument
    /// to `POST /api/open`.
    pub path: String,
    pub bytes: u64,
}

/// `GET /api/session`: the sign-in state behind the cookie we hold.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct SessionInfo {
    /// Some credential is required for the API.
    pub locked: bool,
    /// Sign-in providers are configured on the server.
    pub ready: bool,
    /// Provider names the server offers (`github`, `google`, `x`).
    pub providers: Vec<String>,
    pub signed_in: bool,
    /// The signed-in account is on the server's editor list.
    pub editor: bool,
    pub who: String,
    pub handle: String,
    pub name: String,
    /// CSRF token for POSTs; empty when signed out.
    pub csrf: String,
}

/// `GET /api/status`: the server's currently open phext.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct ServerStatus {
    pub scrolls: usize,
    pub bytes: usize,
    pub file: String,
    pub dirty: bool,
}

/// A scroll both sides changed since the last sync. The local text
/// stays in the document; the remote text rides along so the UI can
/// show exactly what pushing would have overwritten.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Conflict {
    pub coordinate: String,
    pub local: String,
    pub remote: String,
}

#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct SyncReport {
    /// Scrolls whose local text was replaced by the server's.
    pub pulled: usize,
    /// Scrolls written to the server.
    pub pushed: usize,
    /// Scrolls both sides changed to the same text (base advanced).
    pub converged: usize,
    /// Local edits kept because the server side did not move.
    pub kept_local: usize,
    pub conflicts: Vec<Conflict>,
    /// A push ended with a successful `POST /api/save`.
    pub saved: bool,
}

impl SyncReport {
    pub fn summary(&self) -> String {
        let mut parts = Vec::new();
        if self.pulled > 0 {
            parts.push(format!("{} pulled", self.pulled));
        }
        if self.pushed > 0 {
            parts.push(format!("{} pushed", self.pushed));
        }
        if self.converged > 0 {
            parts.push(format!("{} already matched", self.converged));
        }
        if self.kept_local > 0 {
            parts.push(format!("{} local edits kept", self.kept_local));
        }
        if parts.is_empty() {
            parts.push("already in sync".to_string());
        }
        if !self.conflicts.is_empty() {
            parts.push(format!("{} conflict(s)", self.conflicts.len()));
        }
        parts.join(", ")
    }
}

/// HTTP client for one phext-edit server. Holds the OAuth session
/// cookie once the user signs in; every request presents it, and
/// POSTs add the session's CSRF token, mirroring the web editor.
pub struct EditClient {
    base: String,
    agent: ureq::Agent,
    cookie: Option<String>,
    csrf: Option<String>,
}

impl EditClient {
    pub fn new(base: &str) -> Self {
        let agent = ureq::AgentBuilder::new()
            .timeout(Duration::from_secs(30))
            .redirects(0)
            .build();
        Self {
            base: base.trim().trim_end_matches('/').to_string(),
            agent,
            cookie: None,
            csrf: None,
        }
    }

    pub fn base(&self) -> &str {
        &self.base
    }

    /// The OAuth start URL for a provider: visiting it in a browser
    /// runs the provider sign-in and lands back on the server.
    pub fn auth_start_url(&self, provider: &str) -> String {
        format!("{}/api/auth/{}", self.base, provider)
    }

    /// The desktop SSO start URL for one sign-in attempt: visiting
    /// it in a browser runs the sign-in there and redirects to the
    /// attempt's loopback `return_url` carrying the assertion and
    /// the attempt's `state`.
    pub fn desktop_sso_url(&self, return_url: &str, state: &str) -> String {
        format!(
            "{}/api/auth/sso/desktop?return={}&state={}",
            self.base,
            form_encode(return_url),
            form_encode(state)
        )
    }

    /// Ask whether this server has the desktop SSO audience armed.
    /// The probe is a plain GET of the mint URL with a throwaway
    /// loopback return: an armed server answers 302 (toward sign-in,
    /// or toward the return when a session is presented), an unarmed
    /// or older server answers 404 — the distinction the app's
    /// paste fallback hangs on.
    pub fn desktop_sso_probe(&self) -> Result<DesktopSso> {
        let url = self.desktop_sso_url("http://127.0.0.1:8912/", "probe-state-0123456789abcdef");
        let status = match self.agent.get(&url).call() {
            Ok(resp) => resp.status(),
            Err(ureq::Error::Status(status, _)) => status,
            Err(err) => return Err(self.wrap_error(err)),
        };
        match status {
            302 => Ok(DesktopSso::Armed),
            404 => Ok(DesktopSso::Unavailable),
            other => Err(SyncError(format!(
                "{} answered HTTP {other} on the desktop sign-in probe",
                self.base
            ))),
        }
    }

    /// Trade a desktop SSO assertion for the session it grants:
    /// POSTs the assertion and the attempt's state to the exchange
    /// endpoint, and on success this client holds the returned
    /// session cookie and CSRF token, exactly as if the user had
    /// pasted the cookie. Refusals surface in the server's own words
    /// (a 401 for a bad, spent, or foreign assertion; the editor-list
    /// refusal for a signed-in non-editor).
    pub fn desktop_exchange(&mut self, assertion: &str, state: &str) -> Result<()> {
        let resp = self
            .agent
            .post(&format!("{}/api/auth/desktop/exchange", self.base))
            .set("x-phext-assertion", assertion)
            .set("x-phext-state", state)
            .call()
            .map_err(|e| self.wrap_error(e))?;
        let value = resp
            .into_json::<Value>()
            .map_err(|e| SyncError(format!("bad reply from {}: {e}", self.base)))?;
        check_ok(&value)?;
        let session = value
            .get("session")
            .and_then(Value::as_str)
            .filter(|s| !s.is_empty())
            .ok_or_else(|| SyncError("the exchange returned no session".to_string()))?;
        let csrf = value.get("csrf").and_then(Value::as_str).unwrap_or("");
        self.set_cookie(session);
        self.set_csrf(csrf);
        Ok(())
    }

    /// Present a `phext_session` cookie value (the full sealed value,
    /// as the browser holds it). The CSRF token is learned separately
    /// from [`EditClient::session`].
    pub fn set_cookie(&mut self, cookie: impl Into<String>) {
        let cookie = cookie.into();
        let cookie = cookie.trim().to_string();
        self.cookie = if cookie.is_empty() { None } else { Some(cookie) };
    }

    pub fn set_csrf(&mut self, csrf: impl Into<String>) {
        let csrf = csrf.into();
        self.csrf = if csrf.is_empty() { None } else { Some(csrf) };
    }

    pub fn has_cookie(&self) -> bool {
        self.cookie.is_some()
    }

    /// The session cookie value currently held, for the app's
    /// protected session store.
    pub fn cookie_value(&self) -> Option<String> {
        self.cookie.clone()
    }

    fn request(&self, method: &str, path: &str) -> ureq::Request {
        let mut req = match method {
            "POST" => self.agent.post(&format!("{}{}", self.base, path)),
            _ => self.agent.get(&format!("{}{}", self.base, path)),
        };
        if let Some(cookie) = &self.cookie {
            req = req.set("Cookie", &format!("phext_session={cookie}"));
        }
        if method == "POST" {
            if let Some(csrf) = &self.csrf {
                req = req.set("x-csrf-token", csrf);
            }
        }
        req
    }

    fn get_json(&self, path: &str) -> Result<Value> {
        let resp = self
            .request("GET", path)
            .call()
            .map_err(|e| self.wrap_error(e))?;
        resp.into_json::<Value>()
            .map_err(|e| SyncError(format!("bad reply from {}: {e}", self.base)))
    }

    fn post_json(&self, path: &str, body: Value) -> Result<Value> {
        let resp = self
            .request("POST", path)
            .send_json(body)
            .map_err(|e| self.wrap_error(e))?;
        let value = resp
            .into_json::<Value>()
            .map_err(|e| SyncError(format!("bad reply from {}: {e}", self.base)))?;
        check_ok(&value)?;
        Ok(value)
    }

    fn post_text(&self, path: &str, body: &str) -> Result<Value> {
        let resp = self
            .request("POST", path)
            .send_string(body)
            .map_err(|e| self.wrap_error(e))?;
        let value = resp
            .into_json::<Value>()
            .map_err(|e| SyncError(format!("bad reply from {}: {e}", self.base)))?;
        check_ok(&value)?;
        Ok(value)
    }

    /// Turn a ureq error into the server's own words where it sent
    /// any: phext-edit answers refusals as `{"ok":false,"error":…}`
    /// or an `ApiOk` with `ok:false`, and both are more useful than
    /// a bare status code.
    fn wrap_error(&self, err: ureq::Error) -> SyncError {
        match err {
            ureq::Error::Status(status, resp) => {
                if let Ok(value) = resp.into_json::<Value>() {
                    if let Some(message) = value
                        .get("error")
                        .and_then(Value::as_str)
                        .or_else(|| value.get("message").and_then(Value::as_str))
                    {
                        return SyncError(message.to_string());
                    }
                }
                SyncError(format!("{} answered HTTP {status}", self.base))
            }
            ureq::Error::Transport(t) => {
                SyncError(format!("cannot reach {}: {t}", self.base))
            }
        }
    }

    pub fn session(&self) -> Result<SessionInfo> {
        let value = self.get_json("/api/session")?;
        let strings = |key: &str| {
            value
                .get(key)
                .and_then(Value::as_array)
                .map(|items| {
                    items
                        .iter()
                        .filter_map(Value::as_str)
                        .map(str::to_string)
                        .collect()
                })
                .unwrap_or_default()
        };
        let flag = |key: &str| value.get(key).and_then(Value::as_bool).unwrap_or(false);
        let text = |key: &str| {
            value
                .get(key)
                .and_then(Value::as_str)
                .unwrap_or("")
                .to_string()
        };
        Ok(SessionInfo {
            locked: flag("locked"),
            ready: flag("ready"),
            providers: strings("providers"),
            signed_in: flag("signed_in"),
            editor: flag("editor"),
            who: text("who"),
            handle: text("handle"),
            name: text("name"),
            csrf: text("csrf"),
        })
    }

    pub fn list_phexts(&self) -> Result<Vec<PhextFile>> {
        let value = self.get_json("/api/phexts")?;
        let mut files = Vec::new();
        if let Some(items) = value.as_array() {
            for item in items {
                files.push(PhextFile {
                    name: item
                        .get("name")
                        .and_then(Value::as_str)
                        .unwrap_or("")
                        .to_string(),
                    path: item
                        .get("path")
                        .and_then(Value::as_str)
                        .unwrap_or("")
                        .to_string(),
                    bytes: item.get("bytes").and_then(Value::as_u64).unwrap_or(0),
                });
            }
        }
        Ok(files)
    }

    /// Switch the server's open phext. The server validates the path
    /// against its library directory.
    pub fn open(&self, path: &str) -> Result<()> {
        self.post_text("/api/open", path)?;
        Ok(())
    }

    pub fn status(&self) -> Result<ServerStatus> {
        let value = self.get_json("/api/status")?;
        Ok(ServerStatus {
            scrolls: value.get("scrolls").and_then(Value::as_u64).unwrap_or(0) as usize,
            bytes: value.get("bytes").and_then(Value::as_u64).unwrap_or(0) as usize,
            file: value
                .get("file")
                .and_then(Value::as_str)
                .unwrap_or("")
                .to_string(),
            dirty: value.get("dirty").and_then(Value::as_bool).unwrap_or(false),
        })
    }

    /// Every populated coordinate in the open phext, as strings.
    pub fn tree(&self) -> Result<Vec<String>> {
        let value = self.get_json("/api/tree")?;
        let coords = value
            .get("coordinates")
            .and_then(Value::as_array)
            .map(|items| {
                items
                    .iter()
                    .filter_map(Value::as_str)
                    .map(str::to_string)
                    .collect()
            })
            .unwrap_or_default();
        Ok(coords)
    }

    pub fn fetch_scroll(&self, coordinate: &str) -> Result<String> {
        let value = self.get_json(&format!("/api/scroll/{coordinate}"))?;
        Ok(value
            .get("content")
            .and_then(Value::as_str)
            .unwrap_or("")
            .to_string())
    }

    /// Write one scroll in the server's open phext (memory only
    /// until [`EditClient::save`]). Empty content clears the scroll,
    /// the same edit the web editor makes.
    pub fn update_scroll(&self, coordinate: &str, content: &str) -> Result<()> {
        self.post_json(
            "/api/update",
            json!({ "coordinate": coordinate, "content": content }),
        )?;
        Ok(())
    }

    pub fn save(&self) -> Result<()> {
        self.post_text("/api/save", "")?;
        Ok(())
    }
}

/// `POST` replies carry `{ok, message}`; a false `ok` is a failure
/// even at HTTP 200 (the server's `ApiOk` convention).
/// Whether a server's desktop SSO audience answered the probe.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum DesktopSso {
    /// The mint endpoint answered 302: the flow is armed.
    Armed,
    /// The mint endpoint answered 404: unarmed there, or a server
    /// older than the flow. The paste sign-in is the fallback.
    Unavailable,
}

/// Percent-encode for a query value: unreserved characters pass
/// through, everything else becomes `%XX`.
fn form_encode(text: &str) -> String {
    let mut out = String::with_capacity(text.len());
    for byte in text.bytes() {
        match byte {
            b'A'..=b'Z' | b'a'..=b'z' | b'0'..=b'9' | b'-' | b'_' | b'.' | b'~' => {
                out.push(byte as char)
            }
            _ => out.push_str(&format!("%{byte:02X}")),
        }
    }
    out
}

/// Percent-decode a query value (`+` reads as a space, per form
/// encoding; neither the assertion alphabet nor the state alphabet
/// contains either character, so nothing real is altered).
fn percent_decode(text: &str) -> String {
    let bytes = text.as_bytes();
    let mut out = Vec::with_capacity(bytes.len());
    let mut i = 0;
    while i < bytes.len() {
        match bytes[i] {
            b'%' if i + 2 < bytes.len() => {
                let hex = std::str::from_utf8(&bytes[i + 1..i + 3]).unwrap_or("");
                match u8::from_str_radix(hex, 16) {
                    Ok(byte) => {
                        out.push(byte);
                        i += 3;
                    }
                    Err(_) => {
                        out.push(bytes[i]);
                        i += 1;
                    }
                }
            }
            b'+' => {
                out.push(b' ');
                i += 1;
            }
            byte => {
                out.push(byte);
                i += 1;
            }
        }
    }
    String::from_utf8_lossy(&out).into_owned()
}

static NONCE_COUNTER: AtomicU64 = AtomicU64::new(0);

/// A fresh per-attempt state nonce for the desktop flow: 32 hex
/// characters of the crate's SHA-256 over the process id, the
/// wall clock, and a process-wide counter. The nonce's job is
/// binding — the app accepts only the landing that carries its own
/// attempt's state, and the server demands the same state back at
/// the exchange — and its unguessability rests on the hash, not on
/// the inputs being secret. (The assertion itself is additionally
/// single-use, server-burned, and worthless to anyone but an editor
/// account; the state is the attempt binding, not the credential.)
pub fn new_state_nonce() -> String {
    let nanos = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_nanos())
        .unwrap_or(0);
    let tick = NONCE_COUNTER.fetch_add(1, Ordering::Relaxed);
    let seed = format!("phext-notepad-desktop-sso:{}:{nanos}:{tick}", std::process::id());
    sha256_hex(seed.as_bytes())[..32].to_string()
}

/// One desktop sign-in attempt's loopback catch: a listener on an
/// ephemeral loopback port, waiting for the browser the server
/// redirects back with the attempt's assertion.
///
/// The listener binds the numeric loopback only and serves nothing
/// but this one landing; unrelated requests (a favicon fetch, a
/// stray local process probing the port) are answered and the wait
/// continues until the assertion lands or the deadline passes.
pub struct LoopbackCatch {
    listener: TcpListener,
    return_url: String,
    state: String,
}

impl LoopbackCatch {
    /// Bind the listener and mint the attempt's state. Open the
    /// browser at the client's `desktop_sso_url` for this catch's
    /// return and state, then call [`LoopbackCatch::wait`].
    pub fn start() -> Result<Self> {
        let listener = TcpListener::bind(("127.0.0.1", 0))
            .map_err(|e| SyncError(format!("cannot open the sign-in listener: {e}")))?;
        let port = listener
            .local_addr()
            .map_err(|e| SyncError(format!("cannot open the sign-in listener: {e}")))?
            .port();
        Ok(Self {
            listener,
            return_url: format!("http://127.0.0.1:{port}/"),
            state: new_state_nonce(),
        })
    }

    /// The return URL the server redirects the browser to.
    pub fn return_url(&self) -> &str {
        &self.return_url
    }

    /// The attempt's state nonce; the landing must carry it back.
    pub fn state(&self) -> &str {
        &self.state
    }

    /// Block until the assertion lands (answering the browser with
    /// a small "you can close this tab" page) or the timeout passes.
    /// The landing's state must equal this attempt's — a redirect
    /// minted for a different attempt is refused and named as such —
    /// and the verified assertion is returned for the exchange.
    pub fn wait(self, timeout: Duration) -> Result<String> {
        self.listener
            .set_nonblocking(true)
            .map_err(|e| SyncError(format!("sign-in listener failed: {e}")))?;
        let deadline = Instant::now() + timeout;
        loop {
            match self.listener.accept() {
                Ok((stream, _)) => {
                    if let Some(assertion) = self.serve(stream)? {
                        return Ok(assertion);
                    }
                }
                Err(e) if e.kind() == std::io::ErrorKind::WouldBlock => {
                    if Instant::now() >= deadline {
                        return Err(SyncError(
                            "sign-in timed out — the browser never came back".to_string(),
                        ));
                    }
                    std::thread::sleep(Duration::from_millis(25));
                }
                Err(e) => return Err(SyncError(format!("sign-in listener failed: {e}"))),
            }
        }
    }

    /// Serve one connection. `Ok(Some)` when it carried this
    /// attempt's assertion, `Ok(None)` for anything else the wait
    /// should outlive.
    fn serve(&self, mut stream: std::net::TcpStream) -> Result<Option<String>> {
        let _ = stream.set_nonblocking(false);
        let _ = stream.set_read_timeout(Some(Duration::from_secs(10)));
        let mut head = Vec::with_capacity(2048);
        let mut byte = [0u8; 1];
        while !head.ends_with(b"\r\n\r\n") && head.len() < 16384 {
            match stream.read(&mut byte) {
                Ok(0) => break,
                Ok(_) => head.push(byte[0]),
                Err(_) => break,
            }
        }
        let head = String::from_utf8_lossy(&head);
        let target = head
            .lines()
            .next()
            .and_then(|line| line.split_whitespace().nth(1))
            .unwrap_or("")
            .to_string();
        let query = target.split_once('?').map(|(_, q)| q).unwrap_or("");
        let param = |key: &str| -> Option<String> {
            query.split('&').find_map(|pair| {
                let (name, value) = pair.split_once('=')?;
                (name == key).then(|| percent_decode(value))
            })
        };
        let Some(assertion) = param("assertion").filter(|a| !a.is_empty()) else {
            // Not the landing (a favicon, a probe): answer and wait on.
            let _ = stream.write_all(
                b"HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n",
            );
            return Ok(None);
        };
        if param("state").as_deref() != Some(self.state.as_str()) {
            let _ = stream.write_all(
                b"HTTP/1.1 400 Bad Request\r\nContent-Type: text/plain\r\nConnection: close\r\n\r\nThis sign-in belongs to a different attempt.",
            );
            return Err(SyncError(
                "the browser returned a sign-in for a different attempt — try Connect again"
                    .to_string(),
            ));
        }
        let page = "<!doctype html><title>Phext Notepad</title>\
                    <p>Signed in. You can close this tab and return to Phext Notepad.</p>";
        let _ = stream.write_all(
            format!(
                "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {}\r\nConnection: close\r\n\r\n{}",
                page.len(),
                page
            )
            .as_bytes(),
        );
        Ok(Some(assertion))
    }
}

fn check_ok(value: &Value) -> Result<()> {
    if value.get("ok").and_then(Value::as_bool) == Some(false) {
        let message = value
            .get("message")
            .and_then(Value::as_str)
            .unwrap_or("the server refused the request");
        return Err(SyncError(message.to_string()));
    }
    Ok(())
}

/// Base hashes for one (server, remote phext) pair: the version
/// ledger sync reconciles against. Not a credential — it lives in a
/// plain `key=value` file next to the app's settings, in the same
/// format as [`crate::Prefs`].
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct SyncBook {
    pub server: String,
    pub remote_path: String,
    base: BTreeMap<String, String>,
}

impl SyncBook {
    pub fn new(server: &str, remote_path: &str) -> Self {
        Self {
            server: server.to_string(),
            remote_path: remote_path.to_string(),
            base: BTreeMap::new(),
        }
    }

    pub fn base_hash(&self, coordinate: &str) -> Option<&str> {
        self.base.get(coordinate).map(String::as_str)
    }

    fn set_base(&mut self, coordinate: &str, hash: String) {
        self.base.insert(coordinate.to_string(), hash);
    }

    pub fn coordinates(&self) -> Vec<String> {
        self.base.keys().cloned().collect()
    }

    pub fn load(path: &Path) -> Option<Self> {
        let text = std::fs::read_to_string(path).ok()?;
        let mut book = Self::default();
        for line in text.lines() {
            let Some((key, value)) = line.split_once('=') else {
                continue;
            };
            match key.trim() {
                "server" => book.server = value.trim().to_string(),
                "remote" => book.remote_path = value.trim().to_string(),
                coord => {
                    book.base.insert(coord.to_string(), value.trim().to_string());
                }
            }
        }
        if book.server.is_empty() || book.remote_path.is_empty() {
            return None;
        }
        Some(book)
    }

    pub fn store(&self, path: &Path) -> std::result::Result<(), String> {
        if let Some(parent) = path.parent() {
            if !parent.as_os_str().is_empty() {
                std::fs::create_dir_all(parent).map_err(|err| err.to_string())?;
            }
        }
        let mut text = format!("server={}\nremote={}\n", self.server, self.remote_path);
        for (coord, hash) in &self.base {
            text.push_str(&format!("{coord}={hash}\n"));
        }
        std::fs::write(path, text).map_err(|err| err.to_string())
    }
}

/// The stored OAuth session: the sealed `phext_session` cookie value
/// and the CSRF token learned from `/api/session`. This IS a
/// credential (14-day life, editor rights), so it is kept out of the
/// settings file and the phext itself: a dedicated file, owner-only
/// (`0600`) on Unix. On Windows it sits in the per-user config
/// directory under the account's own ACLs.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct StoredSession {
    pub base_url: String,
    pub cookie: String,
    pub csrf: String,
    pub who: String,
}

impl StoredSession {
    pub fn load(path: &Path) -> Option<Self> {
        let text = std::fs::read_to_string(path).ok()?;
        let mut session = StoredSession {
            base_url: String::new(),
            cookie: String::new(),
            csrf: String::new(),
            who: String::new(),
        };
        for line in text.lines() {
            let Some((key, value)) = line.split_once('=') else {
                continue;
            };
            match key.trim() {
                "server" => session.base_url = value.trim().to_string(),
                "cookie" => session.cookie = value.trim().to_string(),
                "csrf" => session.csrf = value.trim().to_string(),
                "who" => session.who = value.trim().to_string(),
                _ => {}
            }
        }
        if session.cookie.is_empty() {
            return None;
        }
        Some(session)
    }

    pub fn store(&self, path: &Path) -> std::result::Result<(), String> {
        if let Some(parent) = path.parent() {
            if !parent.as_os_str().is_empty() {
                std::fs::create_dir_all(parent).map_err(|err| err.to_string())?;
            }
        }
        let text = format!(
            "server={}\ncookie={}\ncsrf={}\nwho={}\n",
            self.base_url, self.cookie, self.csrf, self.who
        );
        std::fs::write(path, &text).map_err(|err| err.to_string())?;
        #[cfg(unix)]
        {
            use std::os::unix::fs::PermissionsExt;
            let _ = std::fs::set_permissions(path, std::fs::Permissions::from_mode(0o600));
        }
        Ok(())
    }

    pub fn clear(path: &Path) {
        let _ = std::fs::remove_file(path);
    }
}

/// Make sure the server's open phext is the one we mean to sync.
/// Switching is refused while the server holds unsaved edits to a
/// different phext: `POST /api/open` would discard them, and they
/// may belong to someone at the web editor.
pub fn ensure_open(client: &EditClient, remote_path: &str) -> Result<ServerStatus> {
    let status = client.status()?;
    if status.file == remote_path {
        return Ok(status);
    }
    if status.dirty {
        return Err(SyncError(format!(
            "the server has unsaved changes in {} — not switching it to {}",
            status.file, remote_path
        )));
    }
    client.open(remote_path)?;
    client.status()
}

enum Decision {
    /// Local and remote agree; just advance the base.
    InSync,
    /// Only the remote moved: take its text.
    TakeRemote,
    /// Only the local side moved: a pull keeps it, a push sends it.
    KeepLocal,
    /// Both moved (or both hold unsynced text): conflict.
    Conflict,
}

fn classify(local_hash: &str, base: Option<&str>, remote_hash: &str) -> Decision {
    if local_hash == remote_hash {
        return Decision::InSync;
    }
    match base {
        Some(base) if base == local_hash => Decision::TakeRemote,
        Some(base) if base == remote_hash => Decision::KeepLocal,
        None if local_hash == scroll_hash("") => Decision::TakeRemote,
        _ => Decision::Conflict,
    }
}

/// One pull step, shared by single-scroll and whole-phext pulls:
/// reconcile the document's scroll at `coordinate` with the
/// server's current `remote` text.
fn apply_pull(
    doc: &mut Document,
    book: &mut SyncBook,
    coordinate: &str,
    remote: &str,
    report: &mut SyncReport,
) {
    let Some(coord) = parse_coordinate(coordinate) else {
        return;
    };
    let local = doc.scroll_at(coord).unwrap_or("").to_string();
    match classify(&scroll_hash(&local), book.base_hash(coordinate), &scroll_hash(remote)) {
        Decision::InSync => {
            book.set_base(coordinate, scroll_hash(&local));
        }
        Decision::TakeRemote => {
            doc.set_scroll_at(coord, remote);
            book.set_base(coordinate, scroll_hash(remote));
            report.pulled += 1;
        }
        Decision::KeepLocal => {
            report.kept_local += 1;
        }
        Decision::Conflict => {
            report.conflicts.push(Conflict {
                coordinate: coordinate.to_string(),
                local,
                remote: remote.to_string(),
            });
        }
    }
}

/// Pull one scroll from the server's open phext into the document.
pub fn pull_scroll(
    client: &EditClient,
    doc: &mut Document,
    book: &mut SyncBook,
    coordinate: &str,
) -> Result<SyncReport> {
    ensure_open(client, &book.remote_path)?;
    let remote = client.fetch_scroll(coordinate)?;
    let mut report = SyncReport::default();
    apply_pull(doc, book, coordinate, &remote, &mut report);
    Ok(report)
}

/// Pull every scroll of the remote phext, including remote
/// deletions (a coordinate the server no longer holds reconciles
/// against empty text).
pub fn pull_all(
    client: &EditClient,
    doc: &mut Document,
    book: &mut SyncBook,
) -> Result<SyncReport> {
    ensure_open(client, &book.remote_path)?;
    let mut report = SyncReport::default();
    let mut seen: HashSet<String> = HashSet::new();
    for coordinate in client.tree()? {
        seen.insert(coordinate.clone());
        let remote = client.fetch_scroll(&coordinate)?;
        apply_pull(doc, book, &coordinate, &remote, &mut report);
    }
    for coordinate in book.coordinates() {
        if !seen.contains(&coordinate) {
            apply_pull(doc, book, &coordinate, "", &mut report);
        }
    }
    Ok(report)
}

/// Push every local change since the last sync. Each candidate is
/// re-read from the server first; if the server text moved off the
/// base, the scroll is a conflict and is NOT written. Ends with
/// `POST /api/save` when anything was written.
pub fn push_all(
    client: &EditClient,
    doc: &mut Document,
    book: &mut SyncBook,
) -> Result<SyncReport> {
    ensure_open(client, &book.remote_path)?;
    let mut report = SyncReport::default();
    let mut coords: Vec<String> = doc
        .scrolls()
        .keys()
        .map(|coord: &Coordinate| format!("{coord}"))
        .collect();
    for coordinate in book.coordinates() {
        if !coords.contains(&coordinate) {
            coords.push(coordinate);
        }
    }
    for coordinate in coords {
        let Some(coord) = parse_coordinate(&coordinate) else {
            continue;
        };
        let local = doc.scroll_at(coord).unwrap_or("").to_string();
        let local_hash = scroll_hash(&local);
        if book.base_hash(&coordinate) == Some(local_hash.as_str()) {
            continue; // No local move since the last sync.
        }
        let remote = client.fetch_scroll(&coordinate)?;
        let remote_hash = scroll_hash(&remote);
        if remote_hash == local_hash {
            book.set_base(&coordinate, local_hash);
            report.converged += 1;
            continue;
        }
        if book.base_hash(&coordinate) != Some(remote_hash.as_str()) {
            report.conflicts.push(Conflict {
                coordinate: coordinate.clone(),
                local,
                remote,
            });
            continue;
        }
        client.update_scroll(&coordinate, &local)?;
        book.set_base(&coordinate, local_hash);
        report.pushed += 1;
    }
    if report.pushed > 0 {
        client.save().map_err(|e| {
            SyncError(format!(
                "{} scroll(s) reached the server's memory but saving failed: {}",
                report.pushed, e.0
            ))
        })?;
        report.saved = true;
    }
    Ok(report)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn doc_with(entries: &[(&str, &str)]) -> Document {
        let mut doc = Document::empty();
        for (coord, text) in entries {
            doc.set_scroll_at(parse_coordinate(coord).unwrap(), text);
        }
        doc.mark_clean();
        doc
    }

    #[test]
    fn classify_truth_table() {
        let a = scroll_hash("a");
        let b = scroll_hash("b");
        let c = scroll_hash("c");
        assert!(matches!(classify(&a, Some(&a), &a), Decision::InSync));
        assert!(matches!(classify(&a, Some(&a), &b), Decision::TakeRemote));
        assert!(matches!(classify(&b, Some(&a), &a), Decision::KeepLocal));
        assert!(matches!(classify(&b, Some(&a), &c), Decision::Conflict));
        // Never synced: an empty local side takes the remote text,
        // two different texts conflict.
        let empty = scroll_hash("");
        assert!(matches!(classify(&empty, None, &b), Decision::TakeRemote));
        assert!(matches!(classify(&b, None, &c), Decision::Conflict));
        assert!(matches!(classify(&b, None, &b), Decision::InSync));
    }

    #[test]
    fn pull_takes_remote_when_local_is_untouched() {
        let mut doc = doc_with(&[("1.1.1/1.1.1/1.1.1", "old")]);
        let mut book = SyncBook::new("http://x", "/lib/a.phext");
        book.set_base("1.1.1/1.1.1/1.1.1", scroll_hash("old"));
        let mut report = SyncReport::default();
        apply_pull(&mut doc, &mut book, "1.1.1/1.1.1/1.1.1", "new", &mut report);
        assert_eq!(report.pulled, 1);
        assert_eq!(
            doc.scroll_at(parse_coordinate("1.1.1/1.1.1/1.1.1").unwrap()),
            Some("new")
        );
        assert_eq!(
            book.base_hash("1.1.1/1.1.1/1.1.1"),
            Some(scroll_hash("new").as_str())
        );
    }

    #[test]
    fn pull_conflict_keeps_local_text() {
        let mut doc = doc_with(&[("1.1.1/1.1.1/1.1.1", "mine")]);
        let mut book = SyncBook::new("http://x", "/lib/a.phext");
        book.set_base("1.1.1/1.1.1/1.1.1", scroll_hash("base"));
        let mut report = SyncReport::default();
        apply_pull(&mut doc, &mut book, "1.1.1/1.1.1/1.1.1", "theirs", &mut report);
        assert_eq!(report.conflicts.len(), 1);
        assert_eq!(report.conflicts[0].remote, "theirs");
        assert_eq!(
            doc.scroll_at(parse_coordinate("1.1.1/1.1.1/1.1.1").unwrap()),
            Some("mine")
        );
        // Base must not advance past a conflict.
        assert_eq!(
            book.base_hash("1.1.1/1.1.1/1.1.1"),
            Some(scroll_hash("base").as_str())
        );
    }

    #[test]
    fn pull_remote_deletion_clears_an_untouched_scroll() {
        let mut doc = doc_with(&[("1.1.1/1.1.1/1.1.1", "gone soon")]);
        let mut book = SyncBook::new("http://x", "/lib/a.phext");
        book.set_base("1.1.1/1.1.1/1.1.1", scroll_hash("gone soon"));
        let mut report = SyncReport::default();
        apply_pull(&mut doc, &mut book, "1.1.1/1.1.1/1.1.1", "", &mut report);
        assert_eq!(report.pulled, 1);
        assert_eq!(
            doc.scroll_at(parse_coordinate("1.1.1/1.1.1/1.1.1").unwrap()),
            None
        );
    }

    #[test]
    fn book_and_session_round_trip() {
        let dir = std::env::temp_dir();
        let book_path = dir.join("phext-notepad-syncbook-test.txt");
        let mut book = SyncBook::new("https://edit.phext.io", "/srv/lib/agi.phext");
        book.set_base("1.1.1/1.1.1/1.1.1", scroll_hash("alpha"));
        book.store(&book_path).unwrap();
        let loaded = SyncBook::load(&book_path).unwrap();
        assert_eq!(loaded, book);
        let _ = std::fs::remove_file(&book_path);

        let session_path = dir.join("phext-notepad-session-test.txt");
        let session = StoredSession {
            base_url: "https://edit.phext.io".into(),
            cookie: "payload.sig".into(),
            csrf: "tok".into(),
            who: "github:1".into(),
        };
        session.store(&session_path).unwrap();
        assert_eq!(StoredSession::load(&session_path), Some(session));
        #[cfg(unix)]
        {
            use std::os::unix::fs::PermissionsExt;
            let mode = std::fs::metadata(&session_path).unwrap().permissions().mode();
            assert_eq!(mode & 0o777, 0o600);
        }
        let _ = std::fs::remove_file(&session_path);
    }

    #[test]
    fn desktop_sso_url_encodes_return_and_state() {
        let client = EditClient::new("http://127.0.0.1:8791/");
        assert_eq!(
            client.desktop_sso_url("http://127.0.0.1:8912/", "abc-def_123"),
            "http://127.0.0.1:8791/api/auth/sso/desktop?return=http%3A%2F%2F127.0.0.1%3A8912%2F&state=abc-def_123"
        );
    }

    #[test]
    fn state_nonces_are_shaped_and_distinct() {
        let a = new_state_nonce();
        let b = new_state_nonce();
        assert_eq!(a.len(), 32);
        assert!(a.chars().all(|c| c.is_ascii_hexdigit()));
        assert_ne!(a, b);
    }

    /// Fire one raw HTTP request at a catch, as the redirected
    /// browser would, and read its answer's status line.
    fn knock(return_url: &str, target: &str) -> String {
        use std::io::Read as _;
        let address = return_url
            .strip_prefix("http://")
            .and_then(|rest| rest.split('/').next())
            .unwrap()
            .to_string();
        let mut stream = std::net::TcpStream::connect(address).unwrap();
        stream
            .write_all(format!("GET {target} HTTP/1.1\r\nHost: x\r\n\r\n").as_bytes())
            .unwrap();
        let mut answer = String::new();
        let _ = stream.read_to_string(&mut answer);
        answer
    }

    #[test]
    fn loopback_catch_takes_only_its_own_attempts_landing() {
        let catch = LoopbackCatch::start().unwrap();
        let return_url = catch.return_url().to_string();
        let state = catch.state().to_string();
        assert!(return_url.starts_with("http://127.0.0.1:"));

        // Unrelated knocks (a favicon fetch) do not end the wait.
        let waiting = std::thread::spawn(move || catch.wait(Duration::from_secs(10)));
        std::thread::sleep(Duration::from_millis(50));
        let favicon = knock(&return_url, "/favicon.ico");
        assert!(favicon.starts_with("HTTP/1.1 404"), "{favicon}");
        let landing = knock(
            &return_url,
            &format!("/?assertion=sealed.value-here_123&state={state}"),
        );
        assert!(landing.starts_with("HTTP/1.1 200"), "{landing}");
        assert_eq!(waiting.join().unwrap().unwrap(), "sealed.value-here_123");
    }

    #[test]
    fn loopback_catch_refuses_a_foreign_attempts_landing() {
        let catch = LoopbackCatch::start().unwrap();
        let return_url = catch.return_url().to_string();
        let waiting = std::thread::spawn(move || catch.wait(Duration::from_secs(10)));
        std::thread::sleep(Duration::from_millis(50));
        let landing = knock(
            &return_url,
            "/?assertion=sealed.value-here_123&state=someone-elses-attempt-state",
        );
        assert!(landing.starts_with("HTTP/1.1 400"), "{landing}");
        let err = waiting.join().unwrap().unwrap_err();
        assert!(err.0.contains("different attempt"), "{}", err.0);
    }
}
