mod commands;
mod session;

use std::path::PathBuf;
use std::sync::Mutex;

use tauri::{Emitter, Manager, WindowEvent};

use commands::AppState;
use session::Session;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .setup(|app| {
            let session = Session::launch(prefs_file(app.handle()));
            app.manage(Mutex::new(session));
            if let Some(path) = startup_file() {
                if let Some(state) = app.try_state::<AppState>() {
                    let mut session = state.lock().unwrap_or_else(|err| err.into_inner());
                    if session.open_path(&path).is_ok() {
                        let _ = session.persist();
                    }
                }
            }
            if let Some(window) = app.get_webview_window("main") {
                let handle = app.handle().clone();
                window.on_window_event(move |event| {
                    if let WindowEvent::CloseRequested { api, .. } = event {
                        on_close(&handle, api);
                    }
                });
            }
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::snapshot,
            commands::set_zoom,
            commands::zoom_in,
            commands::zoom_out,
            commands::jump,
            commands::nudge,
            commands::set_scroll,
            commands::set_prefs,
            commands::new_document,
            commands::open_baseline,
            commands::open_path,
            commands::open_dialog,
            commands::save,
            commands::save_as,
            commands::discard,
            commands::close_window,
        ])
        .run(tauri::generate_context!())
        .expect("Phext Notepad failed to start");
}

fn prefs_file(app: &tauri::AppHandle) -> PathBuf {
    let dir = app.path().app_config_dir().unwrap_or_else(|_| {
        std::env::current_dir().unwrap_or_else(|_| PathBuf::from("."))
    });
    let _ = std::fs::create_dir_all(&dir);
    dir.join("settings.json")
}

fn startup_file() -> Option<PathBuf> {
    std::env::args().skip(1).find_map(|arg| {
        if arg.starts_with('-') {
            return None;
        }
        let path = PathBuf::from(arg);
        path.is_file().then_some(path)
    })
}

fn on_close(app: &tauri::AppHandle, api: &tauri::CloseRequestApi) {
    let Some(state) = app.try_state::<AppState>() else {
        return;
    };
    let mut session = state.lock().unwrap_or_else(|err| err.into_inner());
    if session.doc.dirty() {
        if let Some(path) = session.doc.path().map(std::path::Path::to_path_buf) {
            if session.save_path(&path).is_ok() {
                let _ = session.persist();
            }
        }
    }
    let needs_prompt = session.doc.dirty() && session.doc.path().is_none();
    if needs_prompt {
        api.prevent_close();
        let _ = app.emit("prompt-save-on-close", ());
        return;
    }
    session.remember_place();
    let _ = session.persist();
}
