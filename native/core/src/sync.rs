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

use std::collections::{BTreeMap, HashSet};
use std::path::Path;
use std::time::Duration;

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
}
