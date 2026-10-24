//! Running editor: the phext document plus the preferences file.

use std::path::{Path, PathBuf};

use phext_notepad_core::{parse_coordinate, Document, DocumentView, Prefs, Theme, ZoomLevel};

pub struct Session {
    pub doc: Document,
    pub prefs: Prefs,
    prefs_path: PathBuf,
}

impl Session {
    pub fn launch(prefs_path: PathBuf) -> Self {
        let prefs = Prefs::load(&prefs_path);
        let mut doc = Document::empty();
        doc.set_zoom(prefs.zoom);
        let mut opened = false;
        if let Some(path) = prefs.last_path.as_ref().map(PathBuf::from) {
            if path.is_file() && doc.load_path(&path).is_ok() {
                opened = true;
            }
        }
        if !opened {
            doc = Document::baseline();
        }
        doc.set_zoom(prefs.zoom);
        if let Some(coord) = parse_coordinate(&prefs.coordinate) {
            doc.set_coordinate(coord);
        }
        if opened {
            if let Some(path) = doc.path() {
                doc.set_message(format!("Opened {}", path.display()));
            }
        } else {
            doc.set_message("Loaded baseline.phext");
        }
        doc.mark_clean();
        Self {
            doc,
            prefs,
            prefs_path,
        }
    }

    pub fn view(&self, origin: &str) -> DocumentView {
        let mut view = self.doc.view(origin, &self.prefs);
        view.recent
            .retain(|path| Path::new(path).is_file());
        view
    }

    pub fn persist(&self) -> Result<(), String> {
        self.prefs.store(&self.prefs_path)
    }

    pub fn remember_place(&mut self) {
        self.prefs.zoom = self.doc.zoom();
        self.prefs.coordinate = format!("{}", self.doc.coordinate());
        if let Some(path) = self.doc.path() {
            self.prefs.last_path = Some(path.display().to_string());
        }
    }

    pub fn open_path(&mut self, path: &Path) -> Result<(), String> {
        self.doc.load_path(path)?;
        self.prefs.remember(path);
        self.remember_place();
        Ok(())
    }

    pub fn save_path(&mut self, path: &Path) -> Result<(), String> {
        self.doc.save_to(path)?;
        self.prefs.remember(path);
        self.remember_place();
        Ok(())
    }

    pub fn apply_prefs(
        &mut self,
        tree_visible: bool,
        splitter: u32,
        theme: &str,
        word_wrap: bool,
    ) -> Result<(), String> {
        self.prefs.tree_visible = tree_visible;
        self.prefs.set_splitter(splitter);
        self.prefs.theme = Theme::parse(theme).ok_or_else(|| format!("Unknown theme '{theme}'"))?;
        self.prefs.word_wrap = word_wrap;
        Ok(())
    }

    pub fn set_zoom(&mut self, level: &str) -> Result<(), String> {
        let zoom = ZoomLevel::parse(level).ok_or_else(|| format!("Unknown zoom '{level}'"))?;
        self.doc.set_zoom(zoom);
        self.doc.clear_message();
        self.remember_place();
        Ok(())
    }
}
