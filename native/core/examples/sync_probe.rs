//! Command-line driver for the sync layer, used by the end-to-end
//! checks against a real phext-edit server (and handy for debugging
//! one). Not part of the app.
//!
//! Environment:
//!   SYNC_BASE    server base URL (required), e.g. http://127.0.0.1:8791
//!   SYNC_COOKIE  phext_session cookie value, when signed in
//!   SYNC_CSRF    the session's CSRF token, for POSTs when signed in
//!
//! Commands:
//!   session
//!   desktop-login   run the desktop SSO flow end to end (below)
//!   list
//!   open <server-path>
//!   status
//!   fetch <coordinate>
//!   update <coordinate> <text>
//!   save
//!   pull        <local.phext> <book.txt> <server-path>
//!   pull-scroll <local.phext> <book.txt> <server-path> <coordinate>
//!   push        <local.phext> <book.txt> <server-path>
//!
//! pull/push load the local phext into a Document, run the sync,
//! write the document back, persist the book, and print a
//! machine-greppable REPORT line plus one CONFLICT line per conflict.

use std::path::{Path, PathBuf};
use std::process::ExitCode;

use phext_notepad_core::sync::{
    pull_all, pull_scroll, push_all, DesktopSso, EditClient, LoopbackCatch, SyncBook, SyncReport,
};
use phext_notepad_core::Document;

/// A minimal percent-decoder for the one hop the mock browser has
/// to read back (the `next` of the sign-in bounce).
fn percent_decode(text: &str) -> String {
    let bytes = text.as_bytes();
    let mut out = Vec::with_capacity(bytes.len());
    let mut i = 0;
    while i < bytes.len() {
        if bytes[i] == b'%' && i + 2 < bytes.len() {
            let hex = std::str::from_utf8(&bytes[i + 1..i + 3]).unwrap_or("");
            if let Ok(byte) = u8::from_str_radix(hex, 16) {
                out.push(byte);
                i += 3;
                continue;
            }
        }
        out.push(bytes[i]);
        i += 1;
    }
    String::from_utf8_lossy(&out).into_owned()
}

/// The desktop SSO flow, driven the way the app drives it, with a
/// mock browser for the legs a browser would perform: the harness
/// session cookie (SYNC_COOKIE) stands in for a browser that has
/// completed the provider sign-in — that leg needs a human at a real
/// provider and stays outside the harness, as in Track B. Everything
/// from the first bounce onward runs against the real server: bounce
/// to sign-in, mint redirect to the loopback catch, exchange, and a
/// closing /api/session read. Prints COOKIE/CSRF lines so a script
/// can carry the exchanged session into the other commands.
fn desktop_login() -> Result<(), String> {
    let base = std::env::var("SYNC_BASE").expect("SYNC_BASE is required");
    let harness_cookie = std::env::var("SYNC_COOKIE").unwrap_or_default();
    let mut client = EditClient::new(&base);
    match client.desktop_sso_probe().map_err(|e| e.0)? {
        DesktopSso::Armed => println!("PROBE armed"),
        DesktopSso::Unavailable => {
            println!("PROBE unavailable");
            return Err("desktop SSO is not armed on this server".to_string());
        }
    }
    let catch = LoopbackCatch::start().map_err(|e| e.0)?;
    let state = catch.state().to_string();
    let return_url = catch.return_url().to_string();
    let start = client.desktop_sso_url(&return_url, &state);
    let waiting = std::thread::spawn(move || catch.wait(std::time::Duration::from_secs(60)));

    // The mock browser: no redirect following. The first leg runs
    // signed out (no cookie — that is what produces the bounce);
    // after it, the harness cookie is presented as a browser's
    // would be once the provider sign-in has completed.
    let browser = ureq::AgentBuilder::new()
        .redirects(0)
        .timeout(std::time::Duration::from_secs(30))
        .build();
    let get = |url: &str, cookie: Option<&str>| -> Result<(u16, Option<String>), String> {
        let req = match cookie {
            Some(value) => browser
                .get(url)
                .set("Cookie", &format!("phext_session={value}")),
            None => browser.get(url),
        };
        match req.call() {
            Ok(resp) => Ok((resp.status(), resp.header("location").map(str::to_string))),
            Err(ureq::Error::Status(status, resp)) => {
                Ok((status, resp.header("location").map(str::to_string)))
            }
            Err(e) => Err(e.to_string()),
        }
    };

    // Leg 1: signed out at the mint endpoint — bounce to sign-in.
    let (status, location) = get(&start, None)?;
    let location = location.ok_or("mint answered without a redirect")?;
    println!("BOUNCE status={status} location={location}");
    if status != 302 || !location.contains("/?next=") {
        return Err("expected the sign-in bounce".to_string());
    }
    let next = location
        .split("next=")
        .nth(1)
        .ok_or("bounce carries no next")?;
    let next = percent_decode(next);

    // The provider sign-in happens here in a real browser (human
    // leg, outside the harness); from here on the harness cookie
    // stands in for the browser that completed it.
    // Leg 2: the (mock) signed-in browser follows `next` — the mint
    // redirects it to the loopback catch with the assertion.
    let (status, location) = get(&format!("{base}{next}"), Some(&harness_cookie))?;
    let location = location.ok_or("mint answered without a redirect")?;
    println!("MINT status={status} location={location}");
    if status != 302 || !location.starts_with(&return_url) {
        return Err("expected the loopback mint redirect".to_string());
    }

    // Leg 3: the browser lands on the catch; the catch answers it.
    let (status, _) = get(&location, None)?;
    println!("LANDING status={status}");
    let assertion = waiting
        .join()
        .map_err(|_| "catch thread failed".to_string())?
        .map_err(|e| e.0)?;
    println!("ASSERTION caught len={}", assertion.len());

    // Leg 4: the exchange, then the server's own view of the session.
    client.desktop_exchange(&assertion, &state).map_err(|e| e.0)?;
    let info = client.session().map_err(|e| e.0)?;
    println!(
        "SESSION signed_in={} editor={} who={} handle={}",
        info.signed_in, info.editor, info.who, info.handle
    );
    println!("COOKIE {}", client.cookie_value().unwrap_or_default());
    println!("CSRF {}", info.csrf);
    Ok(())
}

fn client() -> EditClient {
    let base = std::env::var("SYNC_BASE").expect("SYNC_BASE is required");
    let mut client = EditClient::new(&base);
    if let Ok(cookie) = std::env::var("SYNC_COOKIE") {
        client.set_cookie(cookie);
    }
    if let Ok(csrf) = std::env::var("SYNC_CSRF") {
        client.set_csrf(csrf);
    }
    client
}

fn load_doc(path: &Path) -> Document {
    match std::fs::read_to_string(path) {
        Ok(text) => Document::from_phext(&text, Some(path.to_path_buf())),
        Err(_) => Document::empty(),
    }
}

fn load_book(path: &Path, base: &str, remote: &str) -> SyncBook {
    match SyncBook::load(path) {
        Some(book) if book.server == base && book.remote_path == remote => book,
        _ => SyncBook::new(base, remote),
    }
}

fn print_report(report: &SyncReport) {
    println!(
        "REPORT pulled={} pushed={} converged={} kept_local={} conflicts={} saved={}",
        report.pulled,
        report.pushed,
        report.converged,
        report.kept_local,
        report.conflicts.len(),
        report.saved
    );
    for conflict in &report.conflicts {
        println!(
            "CONFLICT {} local={:?} remote={:?}",
            conflict.coordinate, conflict.local, conflict.remote
        );
    }
}

fn run() -> Result<(), String> {
    let args: Vec<String> = std::env::args().collect();
    let cmd = args.get(1).map(String::as_str).unwrap_or("");
    if cmd == "desktop-login" {
        return desktop_login();
    }
    let client = client();
    match cmd {
        "session" => {
            let info = client.session().map_err(|e| e.0)?;
            println!(
                "SESSION locked={} ready={} providers={:?} signed_in={} editor={} who={} handle={} csrf={}",
                info.locked, info.ready, info.providers, info.signed_in, info.editor,
                info.who, info.handle, info.csrf
            );
        }
        "list" => {
            for file in client.list_phexts().map_err(|e| e.0)? {
                println!("FILE {} {} {}", file.name, file.bytes, file.path);
            }
        }
        "open" => {
            client.open(args.get(2).map(String::as_str).unwrap_or("")).map_err(|e| e.0)?;
            println!("OPENED");
        }
        "status" => {
            let status = client.status().map_err(|e| e.0)?;
            println!(
                "STATUS file={} scrolls={} bytes={} dirty={}",
                status.file, status.scrolls, status.bytes, status.dirty
            );
        }
        "fetch" => {
            let content = client
                .fetch_scroll(args.get(2).map(String::as_str).unwrap_or(""))
                .map_err(|e| e.0)?;
            println!("CONTENT-BEGIN");
            print!("{content}");
            println!("\nCONTENT-END");
        }
        "update" => {
            client
                .update_scroll(
                    args.get(2).map(String::as_str).unwrap_or(""),
                    args.get(3).map(String::as_str).unwrap_or(""),
                )
                .map_err(|e| e.0)?;
            println!("UPDATED");
        }
        "save" => {
            client.save().map_err(|e| e.0)?;
            println!("SAVED");
        }
        "pull" | "push" | "pull-scroll" => {
            let local = PathBuf::from(args.get(2).map(String::as_str).unwrap_or(""));
            let book_path = PathBuf::from(args.get(3).map(String::as_str).unwrap_or(""));
            let remote = args.get(4).map(String::as_str).unwrap_or("");
            let mut doc = load_doc(&local);
            let mut book = load_book(&book_path, client.base(), remote);
            let report = match cmd {
                "pull" => pull_all(&client, &mut doc, &mut book),
                "push" => push_all(&client, &mut doc, &mut book),
                _ => pull_scroll(
                    &client,
                    &mut doc,
                    &mut book,
                    args.get(5).map(String::as_str).unwrap_or(""),
                ),
            }
            .map_err(|e| e.0)?;
            std::fs::write(&local, doc.serialize()).map_err(|e| e.to_string())?;
            book.store(&book_path)?;
            print_report(&report);
        }
        _ => {
            return Err(format!("unknown command {cmd:?}; see the file header"));
        }
    }
    Ok(())
}

fn main() -> ExitCode {
    match run() {
        Ok(()) => ExitCode::SUCCESS,
        Err(message) => {
            eprintln!("ERROR {message}");
            ExitCode::FAILURE
        }
    }
}
