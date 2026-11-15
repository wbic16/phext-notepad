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
    pull_all, pull_scroll, push_all, EditClient, SyncBook, SyncReport,
};
use phext_notepad_core::Document;

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
