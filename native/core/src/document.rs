//! An in-memory phext. Scrolls live in a map keyed by [`Coordinate`].
//! Open and save go through [`crate::parse`]: single-pass equivalents of
//! libphext's `explode` / `implode` that stay proportional to the occupied
//! scrolls (libphext 0.3.1's `explode` is quadratic in file size, and its
//! `implode` forced a full clone of every scroll on each call).

use std::collections::BTreeMap;
use std::path::{Path, PathBuf};

use libphext::phext::{default_coordinate, Coordinate, COORDINATE_MAXIMUM, COORDINATE_MINIMUM};

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
        if self.scroll_text() == text {
            return;
        }
        let coordinate = self.coordinate;
        self.apply_scroll(coordinate, text);
        self.message.clear();
    }

    /// The text at an arbitrary coordinate, if a scroll lives there.
    pub fn scroll_at(&self, coordinate: Coordinate) -> Option<&str> {
        self.scrolls.get(&coordinate).map(String::as_str)
    }

    /// Write a scroll at an arbitrary coordinate (empty text removes
    /// it). The sync layer uses this to land pulled scrolls without
    /// moving the user's cursor; editing uses [`Document::set_scroll`].
    pub fn set_scroll_at(&mut self, coordinate: Coordinate, text: &str) {
        if self.scroll_at(coordinate).unwrap_or("") == text {
            return;
        }
        self.apply_scroll(coordinate, text);
    }

    fn apply_scroll(&mut self, coordinate: Coordinate, text: &str) {
        // Keep the running totals exact without rescanning the document:
        // words adjust by the edited scroll's delta, and the byte count
        // is an arithmetic walk over the map (no string is built).
        let current = self.scroll_at(coordinate).unwrap_or("").to_string();
        self.total_words -= word_count(&current);
        self.total_words += word_count(text);
        if text.is_empty() {
            self.scrolls.remove(&coordinate);
        } else {
            self.scrolls.insert(coordinate, text.to_string());
        }
        self.dirty = true;
        self.byte_count = parse::serialized_len(&self.scrolls);
        self.revision += 1;
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
            "Use a coordinate like 1.1.1/1.1.1/1.1.1. Each number is from 1 to 1000.".to_string()
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
            if !(COORDINATE_MINIMUM..=COORDINATE_MAXIMUM).contains(&value) {
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
        assert!(parse_coordinate("1.1.1/1.1.1/1.1.1001").is_none());
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
