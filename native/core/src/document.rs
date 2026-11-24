//! An in-memory phext. Scrolls live in a map keyed by [`Coordinate`].
//! Open and save go through [`crate::parse`]: single-pass equivalents of
//! libphext's `explode` / `implode` that stay proportional to the occupied
//! scrolls (libphext 0.3.1's `explode` is quadratic in file size, and its
//! `implode` forced a full clone of every scroll on each call).
//!
//! A document is one *universe* — a single phext — or, when the stream
//! arrives as a chain (libphext's stream level: member phexts joined by
//! 0x02, optionally closed by 0x03), a *multiverse*: one universe per
//! chain member, traversed with [`Document::next_universe`] and friends,
//! and addressed across members by `$N:` multiverse addresses
//! ([`Document::fetch`]). Editing always happens in the active universe
//! and stays byte-local to it: untouched members serialize from the
//! exact bytes they arrived in.

use std::collections::BTreeMap;
use std::path::{Path, PathBuf};

use libphext::phext::{default_coordinate, Coordinate, COORDINATE_MINIMUM};

use crate::parse;
use crate::prefs::Prefs;
use crate::COORDINATE_MAXIMUM;
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
    /// True when the document is a chain (a multiverse). The fields
    /// below are only meaningful then; a plain phext reports universe
    /// 1 of 1, closed.
    pub is_chain: bool,
    /// The active universe, 1-based — the `$N` of the multiverse
    /// address for the universe being edited.
    pub universe: usize,
    /// How many universes the chain holds.
    pub universe_count: usize,
    /// Whether the chain was terminated by a chain close. An unclosed
    /// chain ran out at EOF — possibly truncated — and is never
    /// silently promoted to closed.
    pub chain_closed: bool,
    pub can_prev_universe: bool,
    pub can_next_universe: bool,
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

/// One universe of a document: a single phext's scroll map, plus the
/// member's original stream text while it is untouched. Serializing an
/// untouched member reproduces its arrival bytes exactly, whatever
/// shape they were in; the first edit drops `raw`, and that member
/// serializes from its scroll map from then on. The single universe of
/// a chain-free document never carries `raw` — plain phexts have
/// always normalized through the scroll map.
#[derive(Debug, Clone)]
struct Universe {
    scrolls: BTreeMap<Coordinate, String>,
    raw: Option<String>,
}

impl Universe {
    fn from_member(text: &str) -> Self {
        Self {
            scrolls: parse::explode(text),
            raw: Some(text.to_string()),
        }
    }

    /// The bytes this member contributes to the serialized document.
    fn byte_len(&self) -> usize {
        match &self.raw {
            Some(raw) => raw.len(),
            None => parse::serialized_len(&self.scrolls),
        }
    }

    fn word_count(&self) -> usize {
        self.scrolls.values().map(|text| word_count(text)).sum()
    }
}

#[derive(Debug, Clone)]
pub struct Document {
    /// Every universe of the document, in chain order. Always at
    /// least one; exactly one for a chain-free phext.
    universes: Vec<Universe>,
    /// Index into `universes` of the universe being edited
    /// (0 = `$1`). The coordinate, zoom, and outline all describe
    /// this universe.
    active: usize,
    /// True when the document arrived as a chain.
    chain: bool,
    /// Whether the chain was terminated by a chain close. Meaningless
    /// when `chain` is false (stored true: nothing is missing).
    chain_closed: bool,
    coordinate: Coordinate,
    zoom: ZoomLevel,
    path: Option<PathBuf>,
    dirty: bool,
    virtual_name: Option<String>,
    message: String,
    /// Length of the serialized document, maintained arithmetically —
    /// never by serializing. For a chain this counts every member plus
    /// the chain breaks between them and the close, when present.
    byte_count: usize,
    /// Sum of the word counts of every scroll in every universe,
    /// maintained incrementally so `view()` does not rescan the whole
    /// document.
    total_words: usize,
    /// Bumped on every scroll mutation. Together with the active
    /// universe, the zoom, and the coordinate it keys the outline
    /// cache: the outline is a pure function of (active scrolls, zoom,
    /// coordinate), and rebuilding it walks every occupied scroll, so
    /// repeated views at the same position reuse the last build instead.
    revision: u64,
    outline_cache:
        std::cell::RefCell<Option<(u64, usize, ZoomLevel, Coordinate, Vec<OutlineNode>)>>,
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
            universes: vec![Universe {
                scrolls: BTreeMap::new(),
                raw: None,
            }],
            active: 0,
            chain: false,
            chain_closed: true,
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
        // A single chain opens as a multiverse, one universe per
        // member; anything else is one universe on the flat path
        // (see `parse::split_chain` for the boundary).
        let (universes, chain, chain_closed) = match parse::split_chain(text) {
            Some((members, closed)) => (
                members.iter().map(|member| Universe::from_member(member)).collect(),
                true,
                closed,
            ),
            None => (
                vec![Universe {
                    scrolls: parse::explode(text),
                    raw: None,
                }],
                false,
                true,
            ),
        };
        let total_words = universes.iter().map(|universe| universe.word_count()).sum();
        let mut doc = Self {
            universes,
            active: 0,
            chain,
            chain_closed,
            coordinate: default_coordinate(),
            zoom: ZoomLevel::Inner,
            path,
            dirty: false,
            virtual_name: None,
            message: String::new(),
            byte_count: 0,
            total_words,
            revision: 0,
            outline_cache: std::cell::RefCell::new(None),
        };
        let byte_count = doc.compute_byte_count();
        doc.byte_count = byte_count;
        doc
    }

    /// The serialized length of the whole document, computed by
    /// walking the universes arithmetically: each member contributes
    /// its raw length while untouched, else its scroll map's
    /// serialized length; a chain adds one break byte per boundary
    /// plus one for the close when closed.
    fn compute_byte_count(&self) -> usize {
        let members: usize = self.universes.iter().map(|universe| universe.byte_len()).sum();
        if self.chain {
            members + (self.universes.len() - 1) + usize::from(self.chain_closed)
        } else {
            members
        }
    }

    /// The active universe's scroll map — the one the coordinate,
    /// zoom, and outline describe, and the one editing writes to.
    fn active_scrolls(&self) -> &BTreeMap<Coordinate, String> {
        &self.universes[self.active].scrolls
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
        if !self.chain {
            return parse::serialize(&self.universes[0].scrolls);
        }
        // Untouched members contribute their arrival bytes; edited
        // members serialize from their scroll maps. Either way the
        // join is libphext's chain serialization over the members.
        let members: Vec<String> = self
            .universes
            .iter()
            .map(|universe| match &universe.raw {
                Some(raw) => raw.clone(),
                None => parse::serialize(&universe.scrolls),
            })
            .collect();
        parse::join_chain(&members, self.chain_closed)
    }

    pub fn set_scroll(&mut self, text: &str) {
        if self.scroll_text() == text {
            return;
        }
        let coordinate = self.coordinate;
        self.apply_scroll(coordinate, text);
        self.message.clear();
    }

    /// The text at an arbitrary coordinate in the active universe,
    /// if a scroll lives there.
    pub fn scroll_at(&self, coordinate: Coordinate) -> Option<&str> {
        self.active_scrolls().get(&coordinate).map(String::as_str)
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
        // is an arithmetic walk over the maps (no string is built).
        let current = self.scroll_at(coordinate).unwrap_or("").to_string();
        self.total_words -= word_count(&current);
        self.total_words += word_count(text);
        let universe = &mut self.universes[self.active];
        if text.is_empty() {
            universe.scrolls.remove(&coordinate);
        } else {
            universe.scrolls.insert(coordinate, text.to_string());
        }
        // The member has been edited: it serializes from its scroll
        // map from now on, never from its arrival bytes again.
        universe.raw = None;
        self.dirty = true;
        self.byte_count = self.compute_byte_count();
        self.revision += 1;
    }

    /// The outline for the current zoom and coordinate, rebuilt only
    /// when the active universe's scrolls, the universe itself, the
    /// zoom, or the coordinate changed since the last one.
    fn outline(&self) -> Vec<OutlineNode> {
        if let Some((revision, active, zoom, coordinate, outline)) = &*self.outline_cache.borrow()
        {
            if *revision == self.revision
                && *active == self.active
                && *zoom == self.zoom
                && *coordinate == self.coordinate
            {
                return outline.clone();
            }
        }
        let outline = zoom::outline(self.active_scrolls(), self.zoom, self.coordinate);
        *self.outline_cache.borrow_mut() = Some((
            self.revision,
            self.active,
            self.zoom,
            self.coordinate,
            outline.clone(),
        ));
        outline
    }

    pub fn scroll_text(&self) -> &str {
        self.active_scrolls()
            .get(&self.coordinate)
            .map(String::as_str)
            .unwrap_or("")
    }

    /// True when the document is a chain — a multiverse of two or
    /// more member phexts, or a single phext closed by a chain close.
    pub fn is_chain(&self) -> bool {
        self.chain
    }

    /// Whether the chain was terminated by a chain close. An unclosed
    /// chain ran out at EOF — possibly truncated — and the flag is
    /// state to surface, never an error to swallow. True for
    /// chain-free documents, where nothing is missing.
    pub fn chain_closed(&self) -> bool {
        self.chain_closed
    }

    /// How many universes the document holds: the chain's member
    /// count, or 1 for a plain phext.
    pub fn universe_count(&self) -> usize {
        self.universes.len()
    }

    /// The active universe, 1-based — its `$N` in a multiverse
    /// address.
    pub fn current_universe(&self) -> usize {
        self.active + 1
    }

    /// The table of contents of one universe: the addresses of its
    /// occupied scrolls, in coordinate order. `universe` is 1-based;
    /// an out-of-range universe has an empty table of contents.
    pub fn universe_toc(&self, universe: usize) -> Vec<Coordinate> {
        if universe < 1 {
            return Vec::new();
        }
        self.universes
            .get(universe - 1)
            .map(|universe| universe.scrolls.keys().copied().collect())
            .unwrap_or_default()
    }

    /// Fetch a scroll by multiverse address: `"$N:1.1.1/1.1.1/1.1.1"`,
    /// or a bare coordinate, which names universe 1 — libphext's
    /// `Chain::fetch` semantics exactly. An absent scroll, an
    /// out-of-range universe, and a malformed address all yield an
    /// empty string, as `fetch` does: absence is a named empty,
    /// never a crash.
    pub fn fetch(&self, address: &str) -> String {
        let Some((universe, coord)) = parse_multiverse_address(address) else {
            return String::new();
        };
        self.universes
            .get(universe - 1)
            .and_then(|universe| universe.scrolls.get(&coord))
            .cloned()
            .unwrap_or_default()
    }

    /// Step to the next universe, keeping the coordinate: the same
    /// address in the next member is the same element one step later
    /// in the chain. Returns false at the last universe, like a
    /// dimension step at its bound.
    pub fn next_universe(&mut self) -> bool {
        if self.active + 1 >= self.universes.len() {
            return false;
        }
        self.active += 1;
        self.message.clear();
        true
    }

    /// Step to the previous universe, keeping the coordinate.
    /// Returns false at universe 1.
    pub fn prev_universe(&mut self) -> bool {
        if self.active == 0 {
            return false;
        }
        self.active -= 1;
        self.message.clear();
        true
    }

    /// Jump straight to universe `universe` (1-based), keeping the
    /// coordinate. Returns false when the universe does not exist.
    pub fn goto_universe(&mut self, universe: usize) -> bool {
        if universe < 1 || universe > self.universes.len() {
            return false;
        }
        self.active = universe - 1;
        self.message.clear();
        true
    }

    /// Jump to a coordinate. The address may carry a `$N:` universe
    /// prefix — `"$3:1.1.1/1.1.1/1.1.1"` moves to that universe and
    /// coordinate together; a bare coordinate stays in the current
    /// universe. A failed jump changes nothing.
    pub fn jump(&mut self, text: &str) -> Result<(), String> {
        let bad_address = || {
            "Use a coordinate like 1.1.1/1.1.1/1.1.1. Each number is from 1 to 1000.".to_string()
        };
        let text = text.trim();
        let (universe, address) = match text.strip_prefix('$') {
            Some(rest) => {
                let (number, coord) = rest.split_once(':').ok_or_else(bad_address)?;
                let number: usize = number.trim().parse().map_err(|_| bad_address())?;
                if number < 1 {
                    return Err(bad_address());
                }
                (Some(number), coord)
            }
            None => (None, text),
        };
        let coord = parse_coordinate(address).ok_or_else(bad_address)?;
        if let Some(universe) = universe {
            if !self.chain {
                return Err(
                    "This document is a single phext, not a chain — a $N: address needs a multiverse."
                        .to_string(),
                );
            }
            if universe > self.universes.len() {
                return Err(format!(
                    "Universe ${universe} is not in this chain — it has {} universes.",
                    self.universes.len()
                ));
            }
            self.active = universe - 1;
        }
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

    /// The active universe's scroll map. For a chain this is one
    /// member's scrolls — [`Document::universe_toc`] and
    /// [`Document::fetch`] reach the others.
    pub fn scrolls(&self) -> &BTreeMap<Coordinate, String> {
        self.active_scrolls()
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
            is_chain: self.chain,
            universe: self.current_universe(),
            universe_count: self.universe_count(),
            chain_closed: self.chain_closed,
            can_prev_universe: self.active > 0,
            can_next_universe: self.active + 1 < self.universes.len(),
            path: self.path.as_ref().map(|path| path.display().to_string()),
            filename: self.filename(),
            dirty: self.dirty,
            bytes: self.byte_count,
            words,
            scroll_words: word_count(self.scroll_text()),
            scroll_count: self.active_scrolls().len(),
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

/// Parse a multiverse address: an optional `$N:` universe prefix and
/// a coordinate. A bare coordinate names universe 1 — libphext's
/// `parse_universe_address` semantics, with this editor's stricter
/// coordinate parser (components bounded, as everywhere here).
pub fn parse_multiverse_address(address: &str) -> Option<(usize, Coordinate)> {
    let address = address.trim();
    let (universe, coord_part) = match address.strip_prefix('$') {
        Some(rest) => {
            let (number, coord) = rest.split_once(':')?;
            let number: usize = number.trim().parse().ok()?;
            if number < 1 {
                return None;
            }
            (number, coord)
        }
        None => (1, address),
    };
    Some((universe, parse_coordinate(coord_part)?))
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
        assert_eq!(doc.scrolls().len(), 4);
        let again = Document::from_phext(&doc.serialize(), None);
        assert_eq!(again.scrolls(), doc.scrolls());
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
            doc.scrolls().values().map(|text| word_count(text)).sum::<usize>()
        );
        doc.jump("1.1.1/1.1.1/1.1.2").unwrap();
        doc.set_scroll("one two three four");
        let view = doc.view("edit", &Prefs::default());
        assert_eq!(view.bytes, doc.serialize().len());
        assert_eq!(
            view.words,
            doc.scrolls().values().map(|text| word_count(text)).sum::<usize>()
        );
        doc.set_scroll("");
        let view = doc.view("edit", &Prefs::default());
        assert_eq!(view.bytes, doc.serialize().len());
        assert_eq!(
            view.words,
            doc.scrolls().values().map(|text| word_count(text)).sum::<usize>()
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

    // ---- multiverse (chain) documents ---------------------------------

    const CHAIN_EXAMPLE: &str = include_str!("../../../multiverse-example.phext");

    fn chain_example() -> Document {
        Document::from_phext(CHAIN_EXAMPLE, None)
    }

    #[test]
    fn chain_fixture_is_the_canonical_example() {
        // The fixture at the repo root is the canonical multiversal
        // phext: 742 bytes, the hash the demo correction pass pinned.
        assert_eq!(CHAIN_EXAMPLE.len(), 742);
        assert_eq!(
            crate::sha256::sha256_hex(CHAIN_EXAMPLE.as_bytes()),
            "62e8881e3a3444cd2673b668640bc09cd8a543d6beedaea5089c957565dc7652"
        );
    }

    #[test]
    fn chain_detection_member_count_and_tocs() {
        let doc = chain_example();
        assert!(doc.is_chain());
        assert!(doc.chain_closed());
        assert_eq!(doc.universe_count(), 8);
        assert_eq!(doc.current_universe(), 1);
        // Universe $1 holds the scene scroll plus the first frame's
        // two element scrolls; every later universe holds two.
        assert_eq!(doc.universe_toc(1).len(), 3);
        for universe in 2..=8 {
            assert_eq!(doc.universe_toc(universe).len(), 2, "universe {universe}");
        }
        let addresses: Vec<String> = doc
            .universe_toc(1)
            .iter()
            .map(|coord| format!("{coord}"))
            .collect();
        assert_eq!(
            addresses,
            ["1.1.1/1.1.1/1.1.1", "1.1.1/1.1.1/1.1.2", "1.1.1/1.1.1/1.1.3"]
        );
        // Out-of-range universes have empty tables of contents.
        assert!(doc.universe_toc(0).is_empty());
        assert!(doc.universe_toc(9).is_empty());
        // The view reports the multiverse.
        let view = doc.view("open", &Prefs::default());
        assert!(view.is_chain);
        assert_eq!(view.universe, 1);
        assert_eq!(view.universe_count, 8);
        assert!(view.chain_closed);
        assert!(!view.can_prev_universe);
        assert!(view.can_next_universe);
    }

    #[test]
    fn chain_fetch_matches_libphext() {
        let doc = chain_example();
        let chain = libphext::phext::parse_chains(CHAIN_EXAMPLE).remove(0);
        for address in [
            "1.1.1/1.1.1/1.1.1",
            "1.1.1/1.1.1/1.1.2",
            "$1:1.1.1/1.1.1/1.1.2",
            "$2:1.1.1/1.1.1/1.1.2",
            "$5:1.1.1/1.1.1/1.1.3",
            "$8:1.1.1/1.1.1/1.1.2",
            "$9:1.1.1/1.1.1/1.1.1",
            "$2:1.1.1/1.1.1/1.1.1",
            "$3:9.9.9/9.9.9/9.9.9",
        ] {
            assert_eq!(doc.fetch(address), chain.fetch(address), "{address}");
        }
        // The circle in universe $2 is the one at cx=44.
        assert!(doc.fetch("$2:1.1.1/1.1.1/1.1.2").contains("cx=44"));
        // Absent and malformed addresses are named empties, not crashes.
        assert_eq!(doc.fetch("$9:1.1.1/1.1.1/1.1.1"), "");
        assert_eq!(doc.fetch("$0:1.1.1/1.1.1/1.1.1"), "");
        assert_eq!(doc.fetch("not an address"), "");
        assert_eq!(doc.fetch("$x:1.1.1/1.1.1/1.1.1"), "");
        assert_eq!(doc.fetch(""), "");
    }

    #[test]
    fn member_maps_match_libphext_universe_for_universe() {
        // Every universe's scroll map equals libphext's own explode of
        // that member — the chain layer reads members with the same
        // semantics the library does.
        let doc = chain_example();
        let chain = libphext::phext::parse_chains(CHAIN_EXAMPLE).remove(0);
        for (index, member) in chain.members.iter().enumerate() {
            let expected: BTreeMap<Coordinate, String> = libphext::phext::explode(member)
                .into_iter()
                .filter(|(_, scroll)| !scroll.is_empty())
                .collect();
            let toc = doc.universe_toc(index + 1);
            assert_eq!(toc.len(), expected.len(), "universe {}", index + 1);
            for coord in &toc {
                assert_eq!(
                    doc.fetch(&format!("${}:{coord}", index + 1)),
                    expected[coord],
                    "universe {} at {coord}",
                    index + 1
                );
            }
            // Members also round-trip canonically through the sparse map.
            assert_eq!(
                crate::parse::serialize(&expected),
                *member,
                "universe {}",
                index + 1
            );
        }
    }

    #[test]
    fn traversal_walks_the_frames_in_order() {
        let mut doc = chain_example();
        doc.jump("1.1.1/1.1.1/1.1.2").unwrap();
        // The same address in each universe is the same element one
        // frame later: the circle's cx advances by 24 per universe.
        let mut centres = vec![doc.scroll_text().to_string()];
        while doc.next_universe() {
            centres.push(doc.scroll_text().to_string());
        }
        assert_eq!(doc.current_universe(), 8);
        assert!(!doc.next_universe());
        let cx: Vec<&str> = centres
            .iter()
            .map(|text| {
                text.lines()
                    .find_map(|line| line.strip_prefix("cx="))
                    .unwrap()
            })
            .collect();
        assert_eq!(cx, ["20", "44", "68", "92", "116", "140", "164", "188"]);
        // Traversal kept the coordinate the whole way.
        assert_eq!(format!("{}", doc.coordinate()), "1.1.1/1.1.1/1.1.2");
        // And back again.
        while doc.prev_universe() {}
        assert_eq!(doc.current_universe(), 1);
        assert!(!doc.prev_universe());
        // Direct jumps land; out-of-range jumps refuse and stay put.
        assert!(doc.goto_universe(5));
        assert_eq!(doc.current_universe(), 5);
        assert!(!doc.goto_universe(9));
        assert!(!doc.goto_universe(0));
        assert_eq!(doc.current_universe(), 5);
    }

    #[test]
    fn jump_accepts_universe_prefixes() {
        let mut doc = chain_example();
        doc.jump("$3:1.1.1/1.1.1/1.1.2").unwrap();
        assert_eq!(doc.current_universe(), 3);
        assert!(doc.scroll_text().contains("cx=68"));
        // A bare coordinate stays in the current universe.
        doc.jump("1.1.1/1.1.1/1.1.3").unwrap();
        assert_eq!(doc.current_universe(), 3);
        // Out-of-range universes error and change nothing.
        let before = (doc.current_universe(), format!("{}", doc.coordinate()));
        assert!(doc.jump("$9:1.1.1/1.1.1/1.1.1").is_err());
        assert_eq!(
            (doc.current_universe(), format!("{}", doc.coordinate())),
            before
        );
        // A plain phext refuses $N: outright.
        let mut plain = sample();
        assert!(plain.jump("$2:1.1.1/1.1.1/1.1.1").is_err());
        assert_eq!(plain.current_universe(), 1);
    }

    #[test]
    fn untouched_chain_round_trips_byte_identical() {
        let doc = chain_example();
        assert_eq!(doc.serialize(), CHAIN_EXAMPLE);
        let chains = libphext::phext::parse_chains(CHAIN_EXAMPLE);
        assert_eq!(
            doc.serialize(),
            libphext::phext::serialize_chains(&chains)
        );
        // Totals cover the whole multiverse.
        let view = doc.view("open", &Prefs::default());
        assert_eq!(view.bytes, CHAIN_EXAMPLE.len());
        let expected_words: usize = chains[0]
            .members
            .iter()
            .map(|member| {
                crate::parse::explode(member)
                    .values()
                    .map(|text| word_count(text))
                    .sum::<usize>()
            })
            .sum();
        assert_eq!(view.words, expected_words);
    }

    #[test]
    fn editing_one_universe_leaves_the_others_byte_identical() {
        let mut doc = chain_example();
        let before = libphext::phext::parse_chains(&doc.serialize());
        assert!(doc.goto_universe(3));
        doc.jump("1.1.1/1.1.1/1.1.2").unwrap();
        doc.set_scroll("circle\ncx=999\ncy=60\nr=12\nfill=#000000");
        assert!(doc.dirty());
        let after = libphext::phext::parse_chains(&doc.serialize());
        assert_eq!(after.len(), 1);
        assert_eq!(after[0].members.len(), 8);
        assert!(after[0].closed);
        for (index, (was, is)) in before[0]
            .members
            .iter()
            .zip(&after[0].members)
            .enumerate()
        {
            if index == 2 {
                assert_ne!(was, is, "universe 3 must change");
                assert!(is.contains("cx=999"));
            } else {
                assert_eq!(was, is, "universe {} must not change", index + 1);
            }
        }
        // Totals still describe the whole serialized document.
        let view = doc.view("edit", &Prefs::default());
        assert_eq!(view.bytes, doc.serialize().len());
    }

    #[test]
    fn unclosed_chain_surfaces_its_state() {
        // The canonical example with its close removed is truncated,
        // not closed — and it round-trips without gaining one.
        let truncated = CHAIN_EXAMPLE.strip_suffix('\u{03}').unwrap();
        let doc = Document::from_phext(truncated, None);
        assert!(doc.is_chain());
        assert_eq!(doc.universe_count(), 8);
        assert!(!doc.chain_closed());
        assert_eq!(doc.serialize(), truncated);
        let view = doc.view("open", &Prefs::default());
        assert!(view.is_chain && !view.chain_closed);
        // A minimal unclosed chain behaves the same way.
        let doc = Document::from_phext("one\u{02}two", None);
        assert!(doc.is_chain());
        assert!(!doc.chain_closed());
        assert_eq!(doc.serialize(), "one\u{02}two");
    }

    #[test]
    fn closed_single_member_is_a_one_universe_chain() {
        let doc = Document::from_phext("hello\u{03}", None);
        assert!(doc.is_chain());
        assert_eq!(doc.universe_count(), 1);
        assert!(doc.chain_closed());
        assert_eq!(doc.scroll_text(), "hello");
        assert_eq!(doc.serialize(), "hello\u{03}");
    }

    #[test]
    fn plain_and_multi_chain_streams_keep_flat_behaviour() {
        let doc = sample();
        assert!(!doc.is_chain());
        assert_eq!(doc.universe_count(), 1);
        assert_eq!(doc.current_universe(), 1);
        let mut doc = doc;
        assert!(!doc.next_universe());
        assert!(!doc.prev_universe());
        // A chain-free phext is a single universe at $1.
        assert_eq!(doc.fetch("$1:1.1.1/1.1.1/1.1.1"), "alpha line");
        assert_eq!(doc.fetch("$2:1.1.1/1.1.1/1.1.1"), "");
        // A stream of several chains is not one multiverse: it opens
        // flat, chain bytes riding as content, bytes preserved.
        let multi = "a\u{02}b\u{03}c\u{02}d\u{03}";
        let doc = Document::from_phext(multi, None);
        assert!(!doc.is_chain());
        assert_eq!(doc.serialize(), multi);
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
