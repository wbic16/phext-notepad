//! Document model for the cross-platform Phext Notepad.
//!
//! Phext bytes go through [`libphext`] (the same crate phext-lattice uses):
//! `explode` on open, `implode` on save. Zoom is not a view trick on top of a
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
pub mod prefs;
pub mod zoom;

pub use document::{parse_coordinate, Document, DocumentView};
pub use prefs::{Prefs, Theme};
pub use zoom::ZoomLevel;
