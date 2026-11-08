//! Single-pass phext parsing and serialization over the sparse scroll map.
//!
//! These replace `libphext::phext::explode` / `implode` on the hot path.
//! libphext 0.3.1's `explode` is built on `phokenize`, which re-copies the
//! entire remaining buffer for every scroll it finds — quadratic in the
//! file size (a 1.47 MB file with ~7.3k scrolls cost ~2.9 s to open).
//! `implode` is linear, but `Document::serialize` had to clone every scroll
//! string into a fresh `HashMap` to call it, and the document paid that
//! full serialization again on every edit just to refresh a byte count.
//!
//! The functions here walk the bytes once, keep scrolls in the document's
//! `BTreeMap`, and never materialize anything proportional to the address
//! space — only to the occupied scrolls. Output bytes are identical to
//! libphext's: the tests in this module pin `serialize` against
//! `libphext::phext::implode` and `explode` against
//! `libphext::phext::explode` (empty scrolls filtered, as `Document` has
//! always done) on generated documents, and the `agi_bench` example checks
//! the round trip on real files.

use std::collections::BTreeMap;

use libphext::phext::{
    default_coordinate, Coordinate, BOOK_BREAK, CHAPTER_BREAK, COLLECTION_BREAK, LIBRARY_BREAK,
    SCROLL_BREAK, SECTION_BREAK, SERIES_BREAK, SHELF_BREAK, VOLUME_BREAK,
};

/// The nine phext break bytes, highest dimension first.
fn break_dimension(byte: u8) -> Option<usize> {
    match byte {
        0x01 => Some(0), // LIBRARY_BREAK
        0x1f => Some(1), // SHELF_BREAK
        0x1e => Some(2), // SERIES_BREAK
        0x1d => Some(3), // COLLECTION_BREAK
        0x1c => Some(4), // VOLUME_BREAK
        0x1a => Some(5), // BOOK_BREAK
        0x19 => Some(6), // CHAPTER_BREAK
        0x18 => Some(7), // SECTION_BREAK
        0x17 => Some(8), // SCROLL_BREAK
        _ => None,
    }
}

fn apply_break(coord: &mut Coordinate, dimension: usize) {
    match dimension {
        0 => coord.library_break(),
        1 => coord.shelf_break(),
        2 => coord.series_break(),
        3 => coord.collection_break(),
        4 => coord.volume_break(),
        5 => coord.book_break(),
        6 => coord.chapter_break(),
        7 => coord.section_break(),
        _ => coord.scroll_break(),
    }
}

/// Parse `text` into the sparse scroll map: one entry per non-empty scroll,
/// keyed by its coordinate. Equivalent to `libphext::phext::explode` with
/// empty scrolls dropped, in a single O(bytes) pass.
pub fn explode(text: &str) -> BTreeMap<Coordinate, String> {
    let bytes = text.as_bytes();
    let mut scrolls = BTreeMap::new();
    let mut coord = default_coordinate();
    let mut run_start: Option<usize> = None;
    let mut run_coord = coord;
    for (index, &byte) in bytes.iter().enumerate() {
        if let Some(dimension) = break_dimension(byte) {
            if let Some(start) = run_start.take() {
                scrolls.insert(run_coord, text[start..index].to_string());
            }
            apply_break(&mut coord, dimension);
        } else if run_start.is_none() {
            run_start = Some(index);
            run_coord = coord;
        }
    }
    if let Some(start) = run_start {
        scrolls.insert(run_coord, text[start..].to_string());
    }
    scrolls
}

/// The nine components of a coordinate, highest dimension first.
fn components(coord: &Coordinate) -> [usize; 9] {
    [
        coord.z.library,
        coord.z.shelf,
        coord.z.series,
        coord.y.collection,
        coord.y.volume,
        coord.y.book,
        coord.x.chapter,
        coord.x.section,
        coord.x.scroll,
    ]
}

/// Number of break bytes `append_scroll` in libphext emits to advance
/// `from` to `target`. Only called for `from <= target` in sorted order,
/// which is the only case `implode` (and this module) can produce: the
/// highest lagging dimension is advanced in bulk, and each of its breaks
/// resets every lower dimension to 1.
fn break_count(from: &Coordinate, target: &Coordinate) -> usize {
    let mut current = components(from);
    let target = components(target);
    let mut count = 0;
    for dimension in 0..9 {
        if current[dimension] < target[dimension] {
            count += target[dimension] - current[dimension];
            current[dimension] = target[dimension];
            for lower in current.iter_mut().skip(dimension + 1) {
                *lower = 1;
            }
        }
    }
    count
}

const BREAK_CHARS: [char; 9] = [
    LIBRARY_BREAK,
    SHELF_BREAK,
    SERIES_BREAK,
    COLLECTION_BREAK,
    VOLUME_BREAK,
    BOOK_BREAK,
    CHAPTER_BREAK,
    SECTION_BREAK,
    SCROLL_BREAK,
];

/// Emit the breaks that advance `coord` to `target`, exactly as libphext's
/// `append_scroll` does: repeatedly advance the highest lagging dimension.
fn push_breaks(out: &mut String, coord: &mut Coordinate, target: &Coordinate) {
    // Dimension-by-dimension in bulk: advancing one dimension by k emits
    // the same bytes as k single steps, because after the first break the
    // lower dimensions are already reset to 1.
    let target_components = components(target);
    for dimension in 0..9 {
        let current = components(coord)[dimension];
        if current < target_components[dimension] {
            for _ in current..target_components[dimension] {
                out.push(BREAK_CHARS[dimension]);
                apply_break(coord, dimension);
            }
        }
    }
}

/// Serialize the scroll map to phext bytes, byte-identical to
/// `libphext::phext::implode` on the same (non-empty) scrolls.
pub fn serialize(scrolls: &BTreeMap<Coordinate, String>) -> String {
    let mut out = String::with_capacity(serialized_len(scrolls));
    let mut coord = default_coordinate();
    for (target, text) in scrolls {
        if !text.is_empty() {
            push_breaks(&mut out, &mut coord, target);
            out.push_str(text);
        }
        coord = *target;
    }
    out
}

/// The exact length of `serialize(scrolls)`, computed by walking the map
/// once with integer arithmetic — no string is built and nothing is
/// cloned. This is what lets the document keep its byte count current on
/// every edit without paying for a full serialization.
pub fn serialized_len(scrolls: &BTreeMap<Coordinate, String>) -> usize {
    let mut total = 0;
    let mut coord = default_coordinate();
    for (target, text) in scrolls {
        if !text.is_empty() {
            total += break_count(&coord, target) + text.len();
        }
        coord = *target;
    }
    total
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::collections::HashMap;

    fn libphext_map(text: &str) -> BTreeMap<Coordinate, String> {
        libphext::phext::explode(text)
            .into_iter()
            .filter(|(_, scroll)| !scroll.is_empty())
            .collect()
    }

    fn libphext_bytes(scrolls: &BTreeMap<Coordinate, String>) -> String {
        let map: HashMap<Coordinate, String> = scrolls
            .iter()
            .map(|(coord, text)| (*coord, text.clone()))
            .collect();
        libphext::phext::implode(map)
    }

    /// Deterministic pseudo-random scroll sets spread over the address
    /// space, including the far corner, gaps, and shared prefixes.
    fn generated(seed: u64) -> BTreeMap<Coordinate, String> {
        let mut state = seed ^ 0x9e37_79b9_7f4a_7c15;
        let mut next = move || {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            state
        };
        let mut scrolls = BTreeMap::new();
        for index in 0..300 {
            let mut pick = |max: u64| ((next() % max) + 1) as usize;
            let mut coord = default_coordinate();
            coord.z.library = pick(4);
            coord.z.shelf = pick(30);
            coord.z.series = pick(30);
            coord.y.collection = pick(12);
            coord.y.volume = pick(12);
            coord.y.book = pick(12);
            coord.x.chapter = pick(40);
            coord.x.section = pick(40);
            coord.x.scroll = pick(40);
            if index == 0 {
                coord = crate::document::parse_coordinate("999.999.999/999.999.999/999.999.999")
                    .unwrap();
            }
            if index == 1 {
                coord = default_coordinate();
            }
            scrolls.insert(coord, format!("scroll {index} with some words\nand two lines"));
        }
        scrolls
    }

    #[test]
    fn explode_matches_libphext_on_generated_documents() {
        for seed in [1, 7, 42, 1_000] {
            let scrolls = generated(seed);
            let text = libphext_bytes(&scrolls);
            assert_eq!(explode(&text), scrolls, "seed {seed}");
            assert_eq!(libphext_map(&text), scrolls, "seed {seed}");
        }
    }

    #[test]
    fn serialize_matches_libphext_implode() {
        for seed in [1, 7, 42, 1_000] {
            let scrolls = generated(seed);
            assert_eq!(serialize(&scrolls), libphext_bytes(&scrolls), "seed {seed}");
            assert_eq!(serialized_len(&scrolls), serialize(&scrolls).len());
        }
    }

    #[test]
    fn round_trip_is_byte_identical() {
        for seed in [3, 99] {
            let scrolls = generated(seed);
            let text = serialize(&scrolls);
            assert_eq!(explode(&text), scrolls);
            assert_eq!(serialize(&explode(&text)), text);
        }
    }

    #[test]
    fn empty_and_break_only_inputs() {
        assert!(explode("").is_empty());
        assert!(explode("\u{17}\u{18}\u{19}").is_empty());
        assert_eq!(serialize(&BTreeMap::new()), "");
        assert_eq!(serialized_len(&BTreeMap::new()), 0);
        let mut one = BTreeMap::new();
        one.insert(default_coordinate(), "home".to_string());
        assert_eq!(serialize(&one), "home");
        assert_eq!(explode("home"), one);
    }

    #[test]
    fn baseline_matches_libphext() {
        let baseline = include_str!("../../../baseline.phext");
        let scrolls = explode(baseline);
        assert_eq!(scrolls, libphext_map(baseline));
        assert_eq!(serialize(&scrolls), libphext_bytes(&scrolls));
    }
}
