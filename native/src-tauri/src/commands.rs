//! Tauri commands. Phext work stays in `phext-notepad-core`; this file only
//! moves files, dialogs, and preferences across the process boundary.

use std::path::{Path, PathBuf};
use std::sync::Mutex;

use phext_notepad_core::{Document, DocumentView};
use tauri::{Manager, State};
use tauri_plugin_dialog::DialogExt;

use crate::session::Session;

pub type AppState = Mutex<Session>;

fn lock<'a>(state: &'a State<'_, AppState>) -> std::sync::MutexGuard<'a, Session> {
    state.lock().unwrap_or_else(|err| err.into_inner())
}

fn to_path(file: tauri_plugin_dialog::FilePath) -> Result<PathBuf, String> {
    file.into_path().map_err(|err| err.to_string())
}

fn with_phext_extension(path: PathBuf) -> PathBuf {
    if path.extension().is_some() {
        path
    } else {
        let mut path = path;
        path.set_extension("phext");
        path
    }
}

fn pick_open(app: &tauri::AppHandle) -> Result<Option<PathBuf>, String> {
    let picked = app
        .dialog()
        .file()
        .add_filter("Phext", &["phext"])
        .add_filter("All files", &["*"])
        .set_title("Open phext")
        .blocking_pick_file();
    match picked {
        Some(file) => Ok(Some(to_path(file)?)),
        None => Ok(None),
    }
}

fn pick_save(app: &tauri::AppHandle, suggested: &str) -> Result<Option<PathBuf>, String> {
    let picked = app
        .dialog()
        .file()
        .add_filter("Phext", &["phext"])
        .add_filter("All files", &["*"])
        .set_title("Save phext")
        .set_file_name(suggested)
        .blocking_save_file();
    match picked {
        Some(file) => Ok(Some(with_phext_extension(to_path(file)?))),
        None => Ok(None),
    }
}

#[tauri::command]
pub fn snapshot(state: State<'_, AppState>) -> DocumentView {
    lock(&state).view("open")
}

#[tauri::command]
pub fn set_zoom(state: State<'_, AppState>, level: String) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.set_zoom(&level)?;
    session.persist()?;
    Ok(session.view("navigate"))
}

#[tauri::command]
pub fn zoom_in(state: State<'_, AppState>) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.doc.zoom_in();
    session.doc.clear_message();
    session.remember_place();
    session.persist()?;
    Ok(session.view("navigate"))
}

#[tauri::command]
pub fn zoom_out(state: State<'_, AppState>) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.doc.zoom_out();
    session.doc.clear_message();
    session.remember_place();
    session.persist()?;
    Ok(session.view("navigate"))
}

#[tauri::command]
pub fn jump(state: State<'_, AppState>, coordinate: String) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.doc.jump(&coordinate)?;
    session.remember_place();
    session.persist()?;
    Ok(session.view("navigate"))
}

#[tauri::command]
pub fn nudge(state: State<'_, AppState>, dimension: String, delta: i32) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.doc.nudge(&dimension, delta)?;
    session.remember_place();
    session.persist()?;
    Ok(session.view("navigate"))
}

#[tauri::command]
pub fn set_scroll(state: State<'_, AppState>, text: String) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.doc.set_scroll(&text);
    Ok(session.view("edit"))
}

#[tauri::command]
pub fn set_prefs(
    state: State<'_, AppState>,
    tree_visible: bool,
    splitter: u32,
    theme: String,
    word_wrap: bool,
) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.apply_prefs(tree_visible, splitter, &theme, word_wrap)?;
    session.persist()?;
    Ok(session.view("prefs"))
}

#[tauri::command]
pub fn new_document(state: State<'_, AppState>) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    let zoom = session.doc.zoom();
    session.doc = Document::empty();
    session.doc.set_zoom(zoom);
    session.doc.set_message("New phext");
    session.remember_place();
    session.persist()?;
    Ok(session.view("open"))
}

#[tauri::command]
pub fn open_baseline(state: State<'_, AppState>) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    let zoom = session.doc.zoom();
    session.doc = Document::baseline();
    session.doc.set_zoom(zoom);
    session.doc.set_message("Loaded baseline.phext");
    session.remember_place();
    session.persist()?;
    Ok(session.view("open"))
}

#[tauri::command]
pub fn open_path(state: State<'_, AppState>, path: String) -> Result<DocumentView, String> {
    let mut session = lock(&state);
    session.open_path(Path::new(&path))?;
    session.persist()?;
    Ok(session.view("open"))
}

#[tauri::command]
pub async fn open_dialog(
    app: tauri::AppHandle,
    state: State<'_, AppState>,
) -> Result<DocumentView, String> {
    let Some(path) = pick_open(&app)? else {
        return Ok(lock(&state).view("cancel"));
    };
    let mut session = lock(&state);
    session.open_path(&path)?;
    session.persist()?;
    Ok(session.view("open"))
}

#[tauri::command]
pub async fn save(app: tauri::AppHandle, state: State<'_, AppState>) -> Result<DocumentView, String> {
    let existing = lock(&state).doc.path().map(Path::to_path_buf);
    let path = if let Some(path) = existing {
        path
    } else {
        let suggested = suggested_name(&lock(&state));
        let Some(path) = pick_save(&app, &suggested)? else {
            return Ok(lock(&state).view("cancel"));
        };
        path
    };
    let mut session = lock(&state);
    session.save_path(&path)?;
    session.persist()?;
    Ok(session.view("save"))
}

#[tauri::command]
pub async fn save_as(app: tauri::AppHandle, state: State<'_, AppState>) -> Result<DocumentView, String> {
    let suggested = suggested_name(&lock(&state));
    let Some(path) = pick_save(&app, &suggested)? else {
        return Ok(lock(&state).view("cancel"));
    };
    let mut session = lock(&state);
    session.save_path(&path)?;
    session.persist()?;
    Ok(session.view("save"))
}

#[tauri::command]
pub fn discard(state: State<'_, AppState>) -> DocumentView {
    let mut session = lock(&state);
    session.doc.mark_clean();
    session.view("prefs")
}

#[tauri::command]
pub fn close_window(app: tauri::AppHandle) -> Result<(), String> {
    if let Some(window) = app.get_webview_window("main") {
        window.close().map_err(|err| err.to_string())?;
    }
    Ok(())
}

fn suggested_name(session: &Session) -> String {
    session
        .doc
        .path()
        .and_then(|path| path.file_name())
        .map(|name| name.to_string_lossy().into_owned())
        .filter(|name| name != "baseline.phext")
        .unwrap_or_else(|| "notes.phext".to_string())
}
