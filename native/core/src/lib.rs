//! Document model for the cross-platform Phext Notepad.
//!
//! Phext bytes are parsed and serialized by [`parse`], a single-pass
//! sparse implementation that is byte-compatible with [`libphext`] (the
//! crate phext-lattice uses) — libphext 0.3.1's `explode` is quadratic in
//! file size, which made multi-megabyte phexts take seconds to open.
//! Zoom is not a view trick on top of a
//! flat tree. A coordinate is three arms, and the outline is always built for
//! the arm the user is in:
//!
//! * outer / z / 11D — library.shelf.series
//! * middle / y / 7D — collection.volume.book, inside the current outer arm
//! * inner / x / 4D — chapter.section.scroll, inside the current book
//!
//! Those names and the drill direction match `zoom_z` / `zoom_y` / `zoom_x`
//! in phext-lattice's editor. Dimension steps follow that editor's
//! `CoordinateNav` (see `nav`), so moving library or book resets the
//! dimensions below it the same way a phext break does.

pub mod document;
pub mod nav;
pub mod parse;
pub mod prefs;
mod sha256;
pub mod sync;
pub mod zoom;

pub use document::{parse_coordinate, Document, DocumentView};
pub use prefs::{Prefs, Theme};
pub use zoom::ZoomLevel;
