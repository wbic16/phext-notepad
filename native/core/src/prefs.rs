//! UI preferences that survive restarts: zoom, splitter, tree, theme.

use serde::{Deserialize, Serialize};

use crate::zoom::ZoomLevel;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
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

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(default)]
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
    pub fn load(path: &std::path::Path) -> Self {
        let Ok(text) = std::fs::read_to_string(path) else {
            return Self::default();
        };
        serde_json::from_str(&text).unwrap_or_default()
    }

    pub fn store(&self, path: &std::path::Path) -> Result<(), String> {
        if let Some(parent) = path.parent() {
            if !parent.as_os_str().is_empty() {
                std::fs::create_dir_all(parent).map_err(|err| err.to_string())?;
            }
        }
        let json = serde_json::to_string_pretty(self).map_err(|err| err.to_string())?;
        std::fs::write(path, json).map_err(|err| err.to_string())
    }

    pub fn set_splitter(&mut self, width: u32) {
        self.splitter = width.clamp(120, 4000);
    }

    pub fn remember(&mut self, path: &std::path::Path) {
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
    fn missing_fields_deserialize_as_defaults() {
        let prefs: Prefs = serde_json::from_str(r#"{"splitter": 480}"#).unwrap();
        assert_eq!(prefs.splitter, 480);
        assert!(prefs.tree_visible);
        assert_eq!(prefs.zoom, ZoomLevel::Inner);
    }

    #[test]
    fn remember_dedupes_and_caps_the_list() {
        let mut prefs = Prefs::default();
        for i in 0..10 {
            prefs.remember(std::path::Path::new(&format!("/tmp/note-{i}.phext")));
        }
        prefs.remember(std::path::Path::new("/tmp/note-9.phext"));
        assert_eq!(prefs.recent.len(), 8);
        assert_eq!(prefs.recent[0], "/tmp/note-9.phext");
        assert_eq!(prefs.last_path.as_deref(), Some("/tmp/note-9.phext"));
    }
}
