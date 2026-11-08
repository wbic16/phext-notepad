//! An in-memory phext. Scrolls live in a map keyed by [`Coordinate`].
//! Open and save go through [`crate::parse`]: single-pass equivalents of
//! libphext's `explode` / `implode` that stay proportional to the occupied
//! scrolls (libphext 0.3.1's `explode` was quadratic in file size, and its
//! `implode` forced a full clone of every scroll on each call).
//!
//! Coordinates follow libphext 0.5.0 semantics: components are unbounded
//! (the 0.3.1 `COORDINATE_MAXIMUM` of 1000, which silently collapsed
//! large files, is gone). Byte-level scroll access, ingestion of foreign
//! byte streams, and the xz seal live here and in [`crate::binary`] /
//! [`crate::seal`].

use std::collections::BTreeMap;
use std::path::{Path, PathBuf};

use libphext::phext::{default_coordinate, Coordinate, COORDINATE_MINIMUM};

use crate::parse;
use crate::prefs::Prefs;
use crate::zoom::{self, Crumb, DimView, OutlineNode, ZoomLevel};

const BASELINE: &str = include_str!("../../../baseline.phext");

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct ArmView {
    pub level: String,
    pub subspace: String,
    pub title: String,
    pub text: String,
    pub active: bool,
    pub dimensions: Vec<DimView>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct DocumentView {
    pub origin: String,
    pub coordinate: String,
    pub zoom: String,
    pub subspace: String,
    pub can_zoom_in: bool,
    pub can_zoom_out: bool,
    pub arms: Vec<ArmView>,
    pub crumbs: Vec<Crumb>,
    pub outline_title: String,
    pub outline: Vec<OutlineNode>,
    pub scroll: String,
    pub path: Option<String>,
    pub filename: String,
    pub dirty: bool,
    pub bytes: usize,
    pub words: usize,
    pub scroll_words: usize,
    pub scroll_count: usize,
    pub summary: String,
    pub message: String,
    pub tree_visible: bool,
    pub splitter: u32,
    pub theme: String,
    pub word_wrap: bool,
    pub recent: Vec<String>,
}

#[derive(Debug, Clone)]
pub struct Document {
    scrolls: BTreeMap<Coordinate, String>,
    coordinate: Coordinate,
    zoom: ZoomLevel,
    path: Option<PathBuf>,
    dirty: bool,
    virtual_name: Option<String>,
    message: String,
    /// Length of the serialized phext, maintained arithmetically by
    /// [`parse::serialized_len`] — never by serializing.
    byte_count: usize,
    /// Sum of the word counts of every scroll, maintained incrementally
    /// so `view()` does not rescan the whole document.
    total_words: usize,
    /// Bumped on every scroll mutation. Together with the zoom and the
    /// coordinate it keys the outline cache: the outline is a pure
    /// function of (scrolls, zoom, coordinate), and rebuilding it walks
    /// every occupied scroll, so repeated views at the same position
    /// reuse the last build instead.
    revision: u64,
    outline_cache: std::cell::RefCell<Option<(u64, ZoomLevel, Coordinate, Vec<OutlineNode>)>>,
}

impl Document {
    pub fn baseline() -> Self {
        let mut doc = Self::from_phext(BASELINE, None);
        doc.virtual_name = Some("baseline.phext".to_string());
        doc.message = "Loaded baseline.phext".to_string();
        doc
    }

    pub fn empty() -> Self {
        Self {
            scrolls: BTreeMap::new(),
            coordinate: default_coordinate(),
            zoom: ZoomLevel::Inner,
            path: None,
            dirty: false,
            virtual_name: None,
            message: String::new(),
            byte_count: 0,
            total_words: 0,
            revision: 0,
            outline_cache: std::cell::RefCell::new(None),
        }
    }

    pub fn from_phext(text: &str, path: Option<PathBuf>) -> Self {
        let text = text.strip_prefix('\u{feff}').unwrap_or(text);
        let scrolls = parse::explode(text);
        let total_words = scrolls.values().map(|text| word_count(text)).sum();
        let byte_count = parse::serialized_len(&scrolls);
        Self {
            scrolls,
            coordinate: default_coordinate(),
            zoom: ZoomLevel::Inner,
            path,
            dirty: false,
            virtual_name: None,
            message: String::new(),
            byte_count,
            total_words,
            revision: 0,
            outline_cache: std::cell::RefCell::new(None),
        }
    }

    pub fn load_path(&mut self, path: &Path) -> Result<(), String> {
        let bytes = std::fs::read(path).map_err(|err| err.to_string())?;
        let text = String::from_utf8(bytes).map_err(|_| {
            "This file is not UTF-8. Phext Notepad opens UTF-8 phext files.".to_string()
        })?;
        let zoom = self.zoom;
        *self = Self::from_phext(&text, Some(path.to_path_buf()));
        self.zoom = zoom;
        self.message = format!("Opened {}", path.display());
        Ok(())
    }

    pub fn save_to(&mut self, path: &Path) -> Result<(), String> {
        let data = self.serialize();
        if let Some(parent) = path.parent() {
            if !parent.as_os_str().is_empty() {
                std::fs::create_dir_all(parent).map_err(|err| err.to_string())?;
            }
        }
        std::fs::write(path, &data).map_err(|err| err.to_string())?;
        self.byte_count = data.len();
        self.path = Some(path.to_path_buf());
        self.virtual_name = None;
        self.dirty = false;
        self.message = format!("Saved {}", path.display());
        Ok(())
    }

    pub fn serialize(&self) -> String {
        parse::serialize(&self.scrolls)
    }

    pub fn set_scroll(&mut self, text: &str) {
        let current = self.scroll_text();
        if current == text {
            return;
        }
        // Keep the running totals exact without rescanning the document:
        // words adjust by the edited scroll's delta, and the byte count
        // is an arithmetic walk over the map (no string is built).
        self.total_words -= word_count(current);
        self.total_words += word_count(text);
        if text.is_empty() {
            self.scrolls.remove(&self.coordinate);
        } else {
            self.scrolls.insert(self.coordinate, text.to_string());
        }
        self.dirty = true;
        self.message.clear();
        self.byte_count = parse::serialized_len(&self.scrolls);
        self.revision += 1;
    }

    /// The bytes held at the current coordinate, with libphext 0.5.0
    /// `fetch_binary` semantics: a well-formed binary scroll decodes to
    /// its payload; any other scroll (plain text, a malformed binary
    /// scroll) yields its own UTF-8 bytes; an unoccupied coordinate
    /// yields no bytes.
    pub fn scroll_bytes(&self) -> Vec<u8> {
        let scroll = self.scroll_text();
        match crate::binary::decode_scroll(scroll) {
            Some(bytes) => bytes,
            None => scroll.as_bytes().to_vec(),
        }
    }

    /// Store `payload` at the current coordinate under the 0.5.0
    /// ingestion rule: valid UTF-8 stays ordinary text; anything else is
    /// stored as a binary scroll holding the base64 of the exact bytes,
    /// verbatim, so [`Document::scroll_bytes`] returns them exactly.
    pub fn set_scroll_bytes(&mut self, payload: &[u8]) {
        match std::str::from_utf8(payload) {
            Ok(text) => self.set_scroll(text),
            Err(_) => {
                let stored = crate::binary::encode_scroll(payload);
                self.set_scroll(&stored);
            }
        }
    }

    /// Store `payload` at the current coordinate as a binary scroll,
    /// unconditionally — the caller has declared this scroll binary,
    /// even if the bytes happen to be valid UTF-8.
    pub fn set_binary_scroll(&mut self, payload: &[u8]) {
        let stored = crate::binary::encode_scroll(payload);
        self.set_scroll(&stored);
    }

    /// Build a document from a raw phext byte stream (the 0.5.0 `ingest`
    /// path): valid UTF-8 opens as ordinary text; a stream carrying
    /// non-UTF-8 scrolls has those scrolls stored as binary scrolls
    /// holding the exact observed bytes. Never rejects.
    pub fn from_bytes(bytes: &[u8], path: Option<PathBuf>) -> Self {
        match std::str::from_utf8(bytes) {
            Ok(text) => Self::from_phext(text, path),
            Err(_) => Self::from_phext(&crate::binary::ingest(bytes), path),
        }
    }

    /// Build a document from xz-sealed phext bytes (see [`crate::seal`]).
    /// The unsealed bytes go through the same ingestion rule as
    /// [`Document::from_bytes`], so a sealed foreign stream opens too.
    pub fn from_sealed(bytes: &[u8], path: Option<PathBuf>) -> Result<Self, String> {
        let raw = crate::seal::unseal(bytes).map_err(|err| err.to_string())?;
        Ok(Self::from_bytes(&raw, path))
    }

    /// Seal the document's serialized bytes (xz, preset 6). The seal is
    /// a transport/storage form only; the working document stays text.
    pub fn seal(&self) -> std::io::Result<Vec<u8>> {
        crate::seal::seal(&self.serialize())
    }

    /// The outline for the current zoom and coordinate, rebuilt only
    /// when the scrolls, zoom, or coordinate changed since the last one.
    fn outline(&self) -> Vec<OutlineNode> {
        if let Some((revision, zoom, coordinate, outline)) = &*self.outline_cache.borrow() {
            if *revision == self.revision && *zoom == self.zoom && *coordinate == self.coordinate
            {
                return outline.clone();
            }
        }
        let outline = zoom::outline(&self.scrolls, self.zoom, self.coordinate);
        *self.outline_cache.borrow_mut() = Some((
            self.revision,
            self.zoom,
            self.coordinate,
            outline.clone(),
        ));
        outline
    }

    pub fn scroll_text(&self) -> &str {
        self.scrolls
            .get(&self.coordinate)
            .map(String::as_str)
            .unwrap_or("")
    }

    pub fn jump(&mut self, text: &str) -> Result<(), String> {
        let coord = parse_coordinate(text).ok_or_else(|| {
            "Use a coordinate like 1.1.1/1.1.1/1.1.1. Each number must be 1 or larger.".to_string()
        })?;
        self.coordinate = coord;
        self.message.clear();
        Ok(())
    }

    pub fn nudge(&mut self, dimension: &str, delta: i32) -> Result<(), String> {
        zoom::nudge_dimension(&mut self.coordinate, dimension, delta)?;
        self.message.clear();
        Ok(())
    }

    pub fn set_zoom(&mut self, zoom: ZoomLevel) {
        self.zoom = zoom;
        self.message.clear();
    }

    pub fn zoom_in(&mut self) -> bool {
        if let Some(next) = self.zoom.finer() {
            self.zoom = next;
            self.message.clear();
            true
        } else {
            false
        }
    }

    pub fn zoom_out(&mut self) -> bool {
        if let Some(next) = self.zoom.coarser() {
            self.zoom = next;
            self.message.clear();
            true
        } else {
            false
        }
    }

    pub fn zoom(&self) -> ZoomLevel {
        self.zoom
    }

    pub fn coordinate(&self) -> Coordinate {
        self.coordinate
    }

    pub fn set_coordinate(&mut self, coordinate: Coordinate) {
        self.coordinate = coordinate;
    }

    pub fn path(&self) -> Option<&Path> {
        self.path.as_deref()
    }

    pub fn dirty(&self) -> bool {
        self.dirty
    }

    pub fn mark_clean(&mut self) {
        self.dirty = false;
    }

    pub fn scrolls(&self) -> &BTreeMap<Coordinate, String> {
        &self.scrolls
    }

    pub fn clear_message(&mut self) {
        self.message.clear();
    }

    pub fn set_message(&mut self, message: impl Into<String>) {
        self.message = message.into();
    }

    fn filename(&self) -> String {
        if let Some(path) = &self.path {
            if let Some(name) = path.file_name() {
                return name.to_string_lossy().into_owned();
            }
        }
        self.virtual_name.clone().unwrap_or_else(|| "Untitled".to_string())
    }

    pub fn view(&self, origin: &str, prefs: &Prefs) -> DocumentView {
        let coordinate = format!("{}", self.coordinate);
        let scroll = self.scroll_text().to_string();
        let words = self.total_words;
        let summary = format!(
            "LB: {}  SF: {}  SR: {}  /  CN: {}  VM: {}  BK: {}  /  CH: {}  SN: {}  SC: {}    Bytes: {}, Words (Total): {}, Words (Page): {}",
            self.coordinate.z.library,
            self.coordinate.z.shelf,
            self.coordinate.z.series,
            self.coordinate.y.collection,
            self.coordinate.y.volume,
            self.coordinate.y.book,
            self.coordinate.x.chapter,
            self.coordinate.x.section,
            self.coordinate.x.scroll,
            self.byte_count,
            words,
            word_count(&scroll),
        );
        let arms = [ZoomLevel::Outer, ZoomLevel::Middle, ZoomLevel::Inner]
            .into_iter()
            .map(|level| {
                let text = match level {
                    ZoomLevel::Outer => format!("{}", self.coordinate.z),
                    ZoomLevel::Middle => format!("{}", self.coordinate.y),
                    ZoomLevel::Inner => format!("{}", self.coordinate.x),
                };
                ArmView {
                    level: level.as_str().to_string(),
                    subspace: level.subspace().to_string(),
                    title: level.arm_title().to_string(),
                    text,
                    active: level == self.zoom,
                    dimensions: zoom::dimensions(level, self.coordinate),
                }
            })
            .collect();

        DocumentView {
            origin: origin.to_string(),
            coordinate,
            zoom: self.zoom.as_str().to_string(),
            subspace: self.zoom.subspace().to_string(),
            can_zoom_in: self.zoom.finer().is_some(),
            can_zoom_out: self.zoom.coarser().is_some(),
            arms,
            crumbs: zoom::crumbs(self.zoom, self.coordinate),
            outline_title: format!("{} · {}", self.zoom.subspace(), self.zoom.arm_title()),
            outline: self.outline(),
            scroll,
            path: self.path.as_ref().map(|path| path.display().to_string()),
            filename: self.filename(),
            dirty: self.dirty,
            bytes: self.byte_count,
            words,
            scroll_words: word_count(self.scroll_text()),
            scroll_count: self.scrolls.len(),
            summary,
            message: self.message.clone(),
            tree_visible: prefs.tree_visible,
            splitter: prefs.splitter,
            theme: prefs.theme.as_str().to_string(),
            word_wrap: prefs.word_wrap,
            recent: prefs.recent.clone(),
        }
    }
}

pub fn parse_coordinate(address: &str) -> Option<Coordinate> {
    let address = address.trim();
    if address.is_empty() {
        return None;
    }
    let arms: Vec<&str> = if address.contains('/') {
        address.split('/').collect()
    } else if address.contains(';') {
        address.split(';').collect()
    } else {
        return None;
    };
    if arms.len() != 3 {
        return None;
    }
    let mut numbers = [0usize; 9];
    for (arm_index, arm) in arms.iter().enumerate() {
        let parts: Vec<&str> = arm.trim().split('.').collect();
        if parts.len() != 3 {
            return None;
        }
        for (part_index, part) in parts.iter().enumerate() {
            let part = part.trim();
            if part.is_empty() || !part.bytes().all(|byte| byte.is_ascii_digit()) {
                return None;
            }
            let value: usize = part.parse().ok()?;
            if value < COORDINATE_MINIMUM {
                return None;
            }
            numbers[arm_index * 3 + part_index] = value;
        }
    }
    let mut coord = default_coordinate();
    coord.z.library = numbers[0];
    coord.z.shelf = numbers[1];
    coord.z.series = numbers[2];
    coord.y.collection = numbers[3];
    coord.y.volume = numbers[4];
    coord.y.book = numbers[5];
    coord.x.chapter = numbers[6];
    coord.x.section = numbers[7];
    coord.x.scroll = numbers[8];
    Some(coord)
}

fn word_count(text: &str) -> usize {
    text.split_whitespace().count()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn sample() -> Document {
        let mut raw = String::new();
        for (coordinate, text) in [
            ("1.1.1/1.1.1/1.1.1", "alpha line"),
            ("1.1.1/1.1.1/1.1.2", "beta line"),
            ("1.1.1/1.1.2/1.1.1", "gamma book"),
            ("1.1.2/1.1.1/1.1.1", "delta series"),
        ] {
            raw = libphext::phext::insert(raw, parse_coordinate(coordinate).unwrap(), text);
        }
        Document::from_phext(&raw, None)
    }

    fn node<'a>(nodes: &'a [OutlineNode], id: &str) -> &'a OutlineNode {
        fn walk<'a>(nodes: &'a [OutlineNode], id: &str) -> Option<&'a OutlineNode> {
            for node in nodes {
                if node.id == id {
                    return Some(node);
                }
                if let Some(found) = walk(&node.children, id) {
                    return Some(found);
                }
            }
            None
        }
        walk(nodes, id).unwrap_or_else(|| panic!("missing node {id}"))
    }

    #[test]
    fn coordinate_parser_accepts_canonical_and_url_forms() {
        let coord = parse_coordinate(" 2.3.4/5.6.7/8.9.10 ").unwrap();
        assert_eq!(format!("{coord}"), "2.3.4/5.6.7/8.9.10");
        let url = parse_coordinate("1.1.1;1.1.1;1.1.2").unwrap();
        assert_eq!(format!("{url}"), "1.1.1/1.1.1/1.1.2");
        assert!(parse_coordinate("1.1.1/1.1.1").is_none());
        assert!(parse_coordinate("0.1.1/1.1.1/1.1.1").is_none());
        // 0.5.0 parity: components are unbounded — past the old 1000 cap
        // and past the WinForms app's old 32,767 wrap alike.
        let past_cap = parse_coordinate("1.1.1/1.1.1/1.1.1001").unwrap();
        assert_eq!(format!("{past_cap}"), "1.1.1/1.1.1/1.1.1001");
        let ultra_max = parse_coordinate("9.9.9/9.9.9/9.9.223854").unwrap();
        assert_eq!(format!("{ultra_max}"), "9.9.9/9.9.9/9.9.223854");
        assert!(parse_coordinate("1.1.1/1.1.1/1.1.a").is_none());
    }

    #[test]
    fn sample_round_trips_through_libphext() {
        let doc = sample();
        assert_eq!(doc.scrolls.len(), 4);
        let again = Document::from_phext(&doc.serialize(), None);
        assert_eq!(again.scrolls, doc.scrolls);
        let mut opened = sample();
        opened.jump("1.1.1/1.1.2/1.1.1").unwrap();
        assert_eq!(opened.scroll_text(), "gamma book");
    }

    #[test]
    fn outer_tree_groups_library_shelf_series() {
        let doc = sample();
        let view = doc.view("open", &Prefs::default());
        // Default zoom is the inner arm, where editing happens.
        assert_eq!(view.zoom, "inner");
        assert_eq!(view.outline_title, "4D · chapter.section.scroll");
        let mut outer = sample();
        outer.set_zoom(ZoomLevel::Outer);
        let view = outer.view("navigate", &Prefs::default());
        assert_eq!(view.outline_title, "11D · library.shelf.series");
        assert_eq!(view.arms.iter().filter(|arm| arm.active).count(), 1);
        assert_eq!(view.arms[0].text, "1.1.1");
        assert!(view.arms[0].active);
        let series = node(&view.outline, "library:1/shelf:1/series:1");
        assert_eq!(series.scroll_count, 3);
        assert_eq!(series.preview, "alpha line");
        assert!(series.current);
        let other = node(&view.outline, "library:1/shelf:1/series:2");
        assert_eq!(other.scroll_count, 1);
        assert_eq!(other.preview, "delta series");
        assert!(!other.current);
        assert!(view.crumbs.is_empty());
    }

    #[test]
    fn middle_zoom_hides_other_series() {
        let mut doc = sample();
        doc.set_zoom(ZoomLevel::Middle);
        let view = doc.view("navigate", &Prefs::default());
        assert_eq!(view.outline_title, "7D · collection.volume.book");
        assert_eq!(view.crumbs.len(), 1);
        assert_eq!(view.crumbs[0].label, "1.1.1");
        let book = node(&view.outline, "collection:1/volume:1/book:1");
        assert_eq!(book.scroll_count, 2);
        let second = node(&view.outline, "collection:1/volume:1/book:2");
        assert_eq!(second.preview, "gamma book");
        assert!(view.outline.iter().all(|node| node.kind == "collection"));
        doc.jump("1.1.2/1.1.1/1.1.1").unwrap();
        let scoped = doc.view("navigate", &Prefs::default());
        let only = node(&scoped.outline, "collection:1/volume:1/book:1");
        assert_eq!(only.scroll_count, 1);
        assert_eq!(only.preview, "delta series");
    }

    #[test]
    fn inner_zoom_lists_scrolls_in_the_current_book() {
        let doc = sample();
        let view = doc.view("open", &Prefs::default());
        assert_eq!(view.crumbs.len(), 2);
        let first = node(&view.outline, "chapter:1/section:1/scroll:1");
        let second = node(&view.outline, "chapter:1/section:1/scroll:2");
        assert_eq!(first.preview, "alpha line");
        assert!(first.current);
        assert_eq!(second.preview, "beta line");
        assert!(!second.current);
        assert_eq!(view.scroll, "alpha line");
    }

    #[test]
    fn zoom_in_and_out_move_one_arm_and_keep_the_coordinate() {
        let mut doc = sample();
        doc.set_zoom(ZoomLevel::Outer);
        assert!(doc.zoom_in());
        assert_eq!(doc.zoom(), ZoomLevel::Middle);
        assert!(doc.zoom_in());
        assert_eq!(doc.zoom(), ZoomLevel::Inner);
        assert!(!doc.zoom_in());
        doc.jump("1.1.2/4.1.1/1.1.1").unwrap();
        assert!(doc.zoom_out());
        assert_eq!(format!("{}", doc.coordinate()), "1.1.2/4.1.1/1.1.1");
        assert_eq!(doc.zoom(), ZoomLevel::Middle);
    }

    #[test]
    fn editing_inserts_and_clearing_removes_a_scroll() {
        let mut doc = Document::empty();
        doc.jump("1.1.1/1.1.1/2.1.1").unwrap();
        doc.set_scroll("fresh scroll");
        assert!(doc.dirty());
        let view = doc.view("edit", &Prefs::default());
        assert_eq!(view.scroll_count, 1);
        assert_eq!(node(&view.outline, "chapter:2/section:1/scroll:1").preview, "fresh scroll");
        let restored = Document::from_phext(&doc.serialize(), None);
        let mut restored = restored;
        restored.jump("1.1.1/1.1.1/2.1.1").unwrap();
        assert_eq!(restored.scroll_text(), "fresh scroll");
        doc.set_scroll("");
        assert!(doc.scrolls().is_empty());
        assert!(doc.dirty());
    }

    #[test]
    fn nudge_from_the_document_uses_lattice_break_semantics() {
        let mut doc = sample();
        doc.jump("1.2.3/4.5.6/7.8.9").unwrap();
        doc.nudge("book", 1).unwrap();
        assert_eq!(format!("{}", doc.coordinate()), "1.2.3/4.5.7/1.1.1");
    }

    #[test]
    fn totals_track_edits_without_a_rescan() {
        let mut doc = sample();
        let view = doc.view("open", &Prefs::default());
        assert_eq!(view.bytes, doc.serialize().len());
        assert_eq!(
            view.words,
            doc.scrolls.values().map(|text| word_count(text)).sum::<usize>()
        );
        doc.jump("1.1.1/1.1.1/1.1.2").unwrap();
        doc.set_scroll("one two three four");
        let view = doc.view("edit", &Prefs::default());
        assert_eq!(view.bytes, doc.serialize().len());
        assert_eq!(
            view.words,
            doc.scrolls.values().map(|text| word_count(text)).sum::<usize>()
        );
        doc.set_scroll("");
        let view = doc.view("edit", &Prefs::default());
        assert_eq!(view.bytes, doc.serialize().len());
        assert_eq!(
            view.words,
            doc.scrolls.values().map(|text| word_count(text)).sum::<usize>()
        );
        // A scroll created far from every other scroll: the byte count
        // must pick up the full break run to reach it.
        doc.jump("50.60.70/80.90.100/110.120.130").unwrap();
        doc.set_scroll("far away");
        let view = doc.view("edit", &Prefs::default());
        assert_eq!(view.bytes, doc.serialize().len());
    }

    #[test]
    fn cached_outline_matches_a_fresh_build() {
        let mut doc = sample();
        doc.set_zoom(ZoomLevel::Outer);
        let first = doc.view("open", &Prefs::default());
        // Second view at the same position takes the cache path.
        let second = doc.view("again", &Prefs::default());
        assert_eq!(first.outline, second.outline);
        assert_eq!(
            second.outline,
            zoom::outline(doc.scrolls(), ZoomLevel::Outer, doc.coordinate())
        );
        // An edit invalidates the cache.
        doc.set_scroll("changed alpha line");
        let third = doc.view("edit", &Prefs::default());
        assert_eq!(
            third.outline,
            zoom::outline(doc.scrolls(), ZoomLevel::Outer, doc.coordinate())
        );
        assert_ne!(third.outline, first.outline);
        // So does a jump.
        doc.jump("1.1.2/1.1.1/1.1.1").unwrap();
        let fourth = doc.view("navigate", &Prefs::default());
        assert_eq!(
            fourth.outline,
            zoom::outline(doc.scrolls(), ZoomLevel::Outer, doc.coordinate())
        );
    }

    #[test]
    fn coordinates_past_the_old_caps_navigate_and_round_trip() {
        let mut doc = Document::empty();
        // Jump to ultra.phext's maximum scroll coordinate.
        doc.jump("9.9.9/9.9.9/9.9.223854").unwrap();
        doc.set_scroll("floatable was here");
        // Nudge across the old 1000 cap in a fresh document.
        let mut near = Document::empty();
        near.jump("1.1.1/1.1.1/1.1.999").unwrap();
        near.nudge("scroll", 2).unwrap();
        assert_eq!(format!("{}", near.coordinate()), "1.1.1/1.1.1/1.1.1001");
        // The far scroll survives a serialize/reload round trip at its
        // true coordinate, as the document's second scroll.
        let again = Document::from_phext(&doc.serialize(), None);
        assert_eq!(again.scrolls().len(), 1);
        let mut again = again;
        again.jump("9.9.9/9.9.9/9.9.223854").unwrap();
        assert_eq!(again.scroll_text(), "floatable was here");
    }

    #[test]
    fn binary_scrolls_round_trip_through_the_document() {
        let mut doc = Document::empty();
        // Non-UTF-8 payload, including dimension-break bytes: armoured
        // as base64, it returns verbatim.
        let payload: Vec<u8> = vec![0xFF, 0x17, 0x1C, 0x80, 0x00, 0x61];
        doc.set_scroll_bytes(&payload);
        assert!(crate::binary::is_binary_scroll(doc.scroll_text()));
        assert_eq!(doc.scroll_bytes(), payload);
        // UTF-8 bytes stay ordinary text.
        doc.jump("1.1.1/1.1.1/1.1.2").unwrap();
        doc.set_scroll_bytes("ordinary text ✓".as_bytes());
        assert_eq!(doc.scroll_text(), "ordinary text ✓");
        assert_eq!(doc.scroll_bytes(), "ordinary text ✓".as_bytes());
        // Unconditional binary storage, even for valid UTF-8 bytes.
        doc.jump("1.1.1/1.1.1/1.1.3").unwrap();
        doc.set_binary_scroll(b"declared binary");
        assert!(crate::binary::is_binary_scroll(doc.scroll_text()));
        assert_eq!(doc.scroll_bytes(), b"declared binary");
        // A malformed binary scroll fetches its own bytes verbatim.
        doc.jump("1.1.1/1.1.1/1.1.4").unwrap();
        doc.set_scroll("phext-binary:1:base64\n!!!");
        assert_eq!(doc.scroll_bytes(), b"phext-binary:1:base64\n!!!");
        // And everything survives serialization.
        let again = Document::from_phext(&doc.serialize(), None);
        assert_eq!(again.scrolls(), doc.scrolls());
    }

    #[test]
    fn from_bytes_ingests_a_foreign_stream() {
        let mut stream: Vec<u8> = Vec::new();
        stream.extend_from_slice(b"hello");
        stream.push(0x17); // SCROLL_BREAK
        stream.extend_from_slice(&[0xFF, 0xFE, 0x41, 0x80]);
        let mut doc = Document::from_bytes(&stream, None);
        assert_eq!(doc.scroll_text(), "hello");
        doc.jump("1.1.1/1.1.1/1.1.2").unwrap();
        assert!(crate::binary::is_binary_scroll(doc.scroll_text()));
        assert_eq!(doc.scroll_bytes(), vec![0xFF, 0xFE, 0x41, 0x80]);
    }

    #[test]
    fn seal_round_trips_the_document() {
        let doc = sample();
        let sealed = doc.seal().unwrap();
        let opened = Document::from_sealed(&sealed, None).unwrap();
        assert_eq!(opened.scrolls(), doc.scrolls());
        // Garbage is an error, not a panic or an empty document.
        assert!(Document::from_sealed(b"not xz at all", None).is_err());
    }

    #[test]
    fn baseline_opens_and_exposes_its_scrolls() {
        let doc = Document::baseline();
        assert!(doc.scrolls().len() >= 2);
        let view = doc.view("open", &Prefs::default());
        assert_eq!(view.filename, "baseline.phext");
        assert!(view.scroll.contains("Phext Notepad") || view.scroll_count >= 2);
        assert!(view.tree_visible);
        assert_eq!(view.splitter, 320);
        assert_eq!(view.theme, "system");
    }
}
