//! UI preferences that survive restarts: zoom, splitter, tree, theme.
//!
//! The file is one `key=value` per line. A missing key keeps the default,
//! which is what a partial settings file needs. No JSON crate.

use std::path::Path;

use crate::document::parse_coordinate;
use crate::zoom::ZoomLevel;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Theme {
    System,
    Dark,
    Light,
}

impl Theme {
    pub fn parse(value: &str) -> Option<Self> {
        match value.trim().to_ascii_lowercase().as_str() {
            "system" => Some(Self::System),
            "dark" => Some(Self::Dark),
            "light" => Some(Self::Light),
            _ => None,
        }
    }

    pub fn as_str(self) -> &'static str {
        match self {
            Self::System => "system",
            Self::Dark => "dark",
            Self::Light => "light",
        }
    }
}

impl Default for Theme {
    fn default() -> Self {
        Self::System
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Prefs {
    pub tree_visible: bool,
    /// Tree pane width in pixels. Hidden tree keeps this value.
    pub splitter: u32,
    pub zoom: ZoomLevel,
    pub theme: Theme,
    pub word_wrap: bool,
    pub last_path: Option<String>,
    pub coordinate: String,
    pub recent: Vec<String>,
}

impl Default for Prefs {
    fn default() -> Self {
        Self {
            tree_visible: true,
            splitter: 320,
            zoom: ZoomLevel::Inner,
            theme: Theme::System,
            word_wrap: true,
            last_path: None,
            coordinate: "1.1.1/1.1.1/1.1.1".to_string(),
            recent: Vec::new(),
        }
    }
}

impl Prefs {
    pub fn load(path: &Path) -> Self {
        let Ok(text) = std::fs::read_to_string(path) else {
            return Self::default();
        };
        let mut prefs = Self::default();
        prefs.recent.clear();
        for line in text.lines() {
            let line = line.trim();
            if line.is_empty() || line.starts_with('#') {
                continue;
            }
            let Some((key, value)) = line.split_once('=') else {
                continue;
            };
            let value = value.trim();
            match key.trim() {
                "tree_visible" => prefs.tree_visible = value != "false",
                "splitter" => {
                    if let Ok(width) = value.parse() {
                        prefs.set_splitter(width);
                    }
                }
                "zoom" => {
                    if let Some(zoom) = ZoomLevel::parse(value) {
                        prefs.zoom = zoom;
                    }
                }
                "theme" => {
                    if let Some(theme) = Theme::parse(value) {
                        prefs.theme = theme;
                    }
                }
                "word_wrap" => prefs.word_wrap = value != "false",
                "coordinate" => {
                    if parse_coordinate(value).is_some() {
                        prefs.coordinate = value.to_string();
                    }
                }
                "last_path" if !value.is_empty() => prefs.last_path = Some(value.to_string()),
                "recent" if !value.is_empty() => prefs.recent.push(value.to_string()),
                _ => {}
            }
        }
        prefs.recent.truncate(8);
        prefs
    }

    pub fn store(&self, path: &Path) -> Result<(), String> {
        if let Some(parent) = path.parent() {
            if !parent.as_os_str().is_empty() {
                std::fs::create_dir_all(parent).map_err(|err| err.to_string())?;
            }
        }
        let mut text = format!(
            "tree_visible={}\nsplitter={}\nzoom={}\ntheme={}\nword_wrap={}\ncoordinate={}\n",
            self.tree_visible,
            self.splitter,
            self.zoom.as_str(),
            self.theme.as_str(),
            self.word_wrap,
            self.coordinate,
        );
        if let Some(path) = &self.last_path {
            text.push_str("last_path=");
            text.push_str(path);
            text.push('\n');
        }
        for recent in &self.recent {
            text.push_str("recent=");
            text.push_str(recent);
            text.push('\n');
        }
        std::fs::write(path, text).map_err(|err| err.to_string())
    }

    pub fn set_splitter(&mut self, width: u32) {
        self.splitter = width.clamp(120, 4000);
    }

    pub fn remember(&mut self, path: &Path) {
        let text = path.to_string_lossy().to_string();
        self.recent.retain(|item| item != &text);
        self.recent.insert(0, text);
        self.recent.truncate(8);
        self.last_path = self.recent.first().cloned();
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn defaults_match_the_notepad_layout() {
        let prefs = Prefs::default();
        assert!(prefs.tree_visible);
        assert_eq!(prefs.splitter, 320);
        assert_eq!(prefs.zoom, ZoomLevel::Inner);
        assert_eq!(prefs.theme, Theme::System);
        assert!(prefs.word_wrap);
    }

    #[test]
    fn missing_fields_stay_at_defaults() {
        let dir = std::env::temp_dir();
        let path = dir.join("phext-notepad-prefs-partial.txt");
        std::fs::write(&path, "splitter=480\n").unwrap();
        let prefs = Prefs::load(&path);
        let _ = std::fs::remove_file(&path);
        assert_eq!(prefs.splitter, 480);
        assert!(prefs.tree_visible);
        assert_eq!(prefs.zoom, ZoomLevel::Inner);
    }

    #[test]
    fn remember_dedupes_and_caps_the_list() {
        let mut prefs = Prefs::default();
        for i in 0..10 {
            prefs.remember(Path::new(&format!("/tmp/note-{i}.phext")));
        }
        prefs.remember(Path::new("/tmp/note-9.phext"));
        assert_eq!(prefs.recent.len(), 8);
        assert_eq!(prefs.recent[0], "/tmp/note-9.phext");
        assert_eq!(prefs.last_path.as_deref(), Some("/tmp/note-9.phext"));
    }
}
