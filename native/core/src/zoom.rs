//! Three zoom levels, one per arm of a phext coordinate.
//!
//! phext-lattice draws the same drill in its web editor: 11D is every
//! library.shelf.series, 7D is the collection.volume.book groups inside the
//! current Z arm, and 4D is the chapter.section.scroll list inside the current
//! book. `ZoomLevel` is that model. The outline for a level only contains the
//! three dimensions of that arm, so the tree and the coordinate bar change
//! together when the user zooms.

use std::collections::BTreeMap;

use libphext::phext::Coordinate;

use crate::nav::{CoordinateNav, Dimension};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ZoomLevel {
    /// library.shelf.series — phext-lattice tier `z` (11D).
    Outer,
    /// collection.volume.book — phext-lattice tier `y` (7D).
    Middle,
    /// chapter.section.scroll — phext-lattice tier `x` (4D).
    Inner,
}

impl Default for ZoomLevel {
    fn default() -> Self {
        Self::Inner
    }
}

impl ZoomLevel {
    pub fn parse(value: &str) -> Option<Self> {
        match value.trim().to_ascii_lowercase().as_str() {
            "outer" | "z" | "11d" | "11" => Some(Self::Outer),
            "middle" | "y" | "7d" | "7" => Some(Self::Middle),
            "inner" | "x" | "4d" | "4" => Some(Self::Inner),
            _ => None,
        }
    }

    pub fn as_str(self) -> &'static str {
        match self {
            Self::Outer => "outer",
            Self::Middle => "middle",
            Self::Inner => "inner",
        }
    }

    /// Subspace label used by phext-lattice's nav tree.
    pub fn subspace(self) -> &'static str {
        match self {
            Self::Outer => "11D",
            Self::Middle => "7D",
            Self::Inner => "4D",
        }
    }

    pub fn arm_title(self) -> &'static str {
        match self {
            Self::Outer => "library.shelf.series",
            Self::Middle => "collection.volume.book",
            Self::Inner => "chapter.section.scroll",
        }
    }

    pub fn finer(self) -> Option<Self> {
        match self {
            Self::Outer => Some(Self::Middle),
            Self::Middle => Some(Self::Inner),
            Self::Inner => None,
        }
    }

    pub fn coarser(self) -> Option<Self> {
        match self {
            Self::Outer => None,
            Self::Middle => Some(Self::Outer),
            Self::Inner => Some(Self::Middle),
        }
    }

    fn levels(self) -> [Level; 3] {
        match self {
            Self::Outer => [
                Level::new("library", "Library", |c| c.z.library),
                Level::new("shelf", "Shelf", |c| c.z.shelf),
                Level::new("series", "Series", |c| c.z.series),
            ],
            Self::Middle => [
                Level::new("collection", "Collection", |c| c.y.collection),
                Level::new("volume", "Volume", |c| c.y.volume),
                Level::new("book", "Book", |c| c.y.book),
            ],
            Self::Inner => [
                Level::new("chapter", "Chapter", |c| c.x.chapter),
                Level::new("section", "Section", |c| c.x.section),
                Level::new("scroll", "Scroll", |c| c.x.scroll),
            ],
        }
    }

    fn in_scope(self, current: Coordinate, coord: Coordinate) -> bool {
        match self {
            Self::Outer => true,
            Self::Middle => coord.z == current.z,
            Self::Inner => coord.z == current.z && coord.y == current.y,
        }
    }
}

struct Level {
    kind: &'static str,
    label: &'static str,
    value: fn(&Coordinate) -> usize,
}

impl Level {
    fn new(kind: &'static str, label: &'static str, value: fn(&Coordinate) -> usize) -> Self {
        Self { kind, label, value }
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct OutlineNode {
    pub id: String,
    pub kind: String,
    pub label: String,
    /// Coordinate selected when this node is activated.
    pub coordinate: String,
    pub preview: String,
    pub scroll_count: usize,
    /// This node is the deepest group that contains the cursor.
    pub current: bool,
    /// The cursor sits somewhere inside this node, so the tree starts open here.
    pub on_path: bool,
    pub children: Vec<OutlineNode>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Crumb {
    pub level: String,
    pub label: String,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct DimView {
    pub name: String,
    pub label: String,
    pub value: usize,
}

/// Build the outline for `zoom`, scoped to `current`.
///
/// Only populated scrolls appear. The coordinate stored on a node is the
/// cursor when the cursor is already inside that group, otherwise the first
/// populated scroll in the group.
pub fn outline(
    scrolls: &BTreeMap<Coordinate, String>,
    zoom: ZoomLevel,
    current: Coordinate,
) -> Vec<OutlineNode> {
    let items: Vec<(Coordinate, &str)> = scrolls
        .iter()
        .filter(|(coord, _)| zoom.in_scope(current, **coord))
        .map(|(coord, text)| (*coord, text.as_str()))
        .collect();
    build_level(&items, &zoom.levels(), current, 0, String::new())
}

pub fn crumbs(zoom: ZoomLevel, current: Coordinate) -> Vec<Crumb> {
    match zoom {
        ZoomLevel::Outer => Vec::new(),
        ZoomLevel::Middle => vec![Crumb {
            level: ZoomLevel::Outer.as_str().to_string(),
            label: format!("{}", current.z),
        }],
        ZoomLevel::Inner => vec![
            Crumb {
                level: ZoomLevel::Outer.as_str().to_string(),
                label: format!("{}", current.z),
            },
            Crumb {
                level: ZoomLevel::Middle.as_str().to_string(),
                label: format!("{}", current.y),
            },
        ],
    }
}

pub fn dimensions(zoom: ZoomLevel, current: Coordinate) -> Vec<DimView> {
    zoom.levels()
        .into_iter()
        .map(|level| DimView {
            name: level.kind.to_string(),
            label: level.label.to_string(),
            value: (level.value)(&current),
        })
        .collect()
}

pub fn nudge_dimension(coord: &mut Coordinate, name: &str, delta: i32) -> Result<bool, String> {
    let dim = parse_dimension(name).ok_or_else(|| format!("Unknown dimension '{name}'"))?;
    if delta == 0 {
        return Ok(false);
    }
    let step = delta.signum();
    let mut moved = false;
    for _ in 0..delta.abs() {
        let ok = if step > 0 {
            coord.navigate_forward(dim)
        } else {
            coord.navigate_backward(dim)
        };
        if !ok {
            break;
        }
        moved = true;
    }
    Ok(moved)
}

fn parse_dimension(name: &str) -> Option<Dimension> {
    match name.trim().to_ascii_lowercase().as_str() {
        "library" => Some(Dimension::Library),
        "shelf" => Some(Dimension::Shelf),
        "series" => Some(Dimension::Series),
        "collection" => Some(Dimension::Collection),
        "volume" => Some(Dimension::Volume),
        "book" => Some(Dimension::Book),
        "chapter" => Some(Dimension::Chapter),
        "section" => Some(Dimension::Section),
        "scroll" => Some(Dimension::Scroll),
        _ => None,
    }
}

fn build_level(
    items: &[(Coordinate, &str)],
    levels: &[Level; 3],
    current: Coordinate,
    depth: usize,
    id_prefix: String,
) -> Vec<OutlineNode> {
    let level = &levels[depth];
    let mut groups: BTreeMap<usize, Vec<(Coordinate, &str)>> = BTreeMap::new();
    for (coord, text) in items {
        groups.entry((level.value)(coord)).or_default().push((*coord, *text));
    }

    groups
        .into_iter()
        .map(|(value, bucket)| {
            let id = if id_prefix.is_empty() {
                format!("{}:{value}", level.kind)
            } else {
                format!("{id_prefix}/{}:{value}", level.kind)
            };
            // Every scroll in this bucket shares the keys above `depth`.
            let on_path = (0..=depth).all(|index| {
                let node_value = if index == depth {
                    value
                } else {
                    (levels[index].value)(&bucket[0].0)
                };
                (levels[index].value)(&current) == node_value
            });
            let leaf = depth == 2;
            let coordinate = if on_path {
                current
            } else {
                bucket[0].0
            };
            let children = if leaf {
                Vec::new()
            } else {
                build_level(&bucket, levels, current, depth + 1, id.clone())
            };
            OutlineNode {
                id,
                kind: level.kind.to_string(),
                label: format!("{} {value}", level.label),
                coordinate: format!("{coordinate}"),
                preview: if leaf { preview(bucket[0].1) } else { String::new() },
                scroll_count: bucket.len(),
                current: leaf && on_path,
                on_path,
                children,
            }
        })
        .collect()
}

fn preview(text: &str) -> String {
    let line = text.trim_start().lines().next().unwrap_or("");
    let mut short: String = line.chars().take(40).collect();
    if line.chars().count() > 40 {
        short.push('…');
    }
    short
}

#[cfg(test)]
mod tests {
    use super::*;
    use libphext::phext::default_coordinate;

    #[test]
    fn zoom_names_follow_the_lattice_tiers() {
        assert_eq!(ZoomLevel::parse("z"), Some(ZoomLevel::Outer));
        assert_eq!(ZoomLevel::parse("7D"), Some(ZoomLevel::Middle));
        assert_eq!(ZoomLevel::parse("x"), Some(ZoomLevel::Inner));
        assert_eq!(ZoomLevel::Outer.subspace(), "11D");
        assert_eq!(ZoomLevel::Middle.subspace(), "7D");
        assert_eq!(ZoomLevel::Inner.subspace(), "4D");
        assert_eq!(ZoomLevel::Outer.finer(), Some(ZoomLevel::Middle));
        assert_eq!(ZoomLevel::Inner.coarser(), Some(ZoomLevel::Middle));
        assert_eq!(ZoomLevel::Outer.coarser(), None);
    }

    #[test]
    fn nudging_library_resets_every_lower_dimension() {
        let mut coord = crate::document::parse_coordinate("1.2.3/4.5.6/7.8.9").unwrap();
        assert!(nudge_dimension(&mut coord, "library", 1).unwrap());
        assert_eq!(format!("{coord}"), "2.1.1/1.1.1/1.1.1");
    }

    #[test]
    fn nudging_scroll_stays_inside_the_section() {
        let mut coord = default_coordinate();
        assert!(nudge_dimension(&mut coord, "scroll", 1).unwrap());
        assert_eq!(format!("{coord}"), "1.1.1/1.1.1/1.1.2");
        let mut origin = default_coordinate();
        assert!(!nudge_dimension(&mut origin, "scroll", -1).unwrap());
        assert_eq!(format!("{origin}"), "1.1.1/1.1.1/1.1.1");
    }

    #[test]
    fn unknown_dimension_is_rejected() {
        let mut coord = default_coordinate();
        assert!(nudge_dimension(&mut coord, "page", 1).is_err());
    }
}
