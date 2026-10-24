//! Dimension steps copied from phext-lattice `coordinate_ext::CoordinateNav`
//! at exo commit `7c91992`.
//!
//! That crate also links memmap2, rayon, and tempfile, and those dependencies
//! are not behind features. This editor only needs the break-style move:
//! stepping a dimension resets every dimension below it. `peek` and the
//! delimiter helpers from the same file are not used here.

use libphext::phext::{Coordinate, COORDINATE_MAXIMUM, COORDINATE_MINIMUM};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub(crate) enum Dimension {
    Scroll,
    Section,
    Chapter,
    Book,
    Volume,
    Collection,
    Series,
    Shelf,
    Library,
}

pub(crate) trait CoordinateNav {
    fn navigate_forward(&mut self, dim: Dimension) -> bool;
    fn navigate_backward(&mut self, dim: Dimension) -> bool;
    fn dimension_value(&self, dim: Dimension) -> usize;
    fn set_dimension(&mut self, dim: Dimension, value: usize);
}

impl CoordinateNav for Coordinate {
    fn navigate_forward(&mut self, dim: Dimension) -> bool {
        if self.dimension_value(dim) >= COORDINATE_MAXIMUM {
            return false;
        }
        match dim {
            Dimension::Scroll => self.scroll_break(),
            Dimension::Section => self.section_break(),
            Dimension::Chapter => self.chapter_break(),
            Dimension::Book => self.book_break(),
            Dimension::Volume => self.volume_break(),
            Dimension::Collection => self.collection_break(),
            Dimension::Series => self.series_break(),
            Dimension::Shelf => self.shelf_break(),
            Dimension::Library => self.library_break(),
        }
        true
    }

    fn navigate_backward(&mut self, dim: Dimension) -> bool {
        let current = self.dimension_value(dim);
        if current <= COORDINATE_MINIMUM {
            return false;
        }
        self.set_dimension(dim, current - 1);
        match dim {
            Dimension::Library => {
                self.z.shelf = 1;
                self.z.series = 1;
                self.y.collection = 1;
                self.y.volume = 1;
                self.y.book = 1;
                self.x.chapter = 1;
                self.x.section = 1;
                self.x.scroll = 1;
            }
            Dimension::Shelf => {
                self.z.series = 1;
                self.y.collection = 1;
                self.y.volume = 1;
                self.y.book = 1;
                self.x.chapter = 1;
                self.x.section = 1;
                self.x.scroll = 1;
            }
            Dimension::Series => {
                self.y.collection = 1;
                self.y.volume = 1;
                self.y.book = 1;
                self.x.chapter = 1;
                self.x.section = 1;
                self.x.scroll = 1;
            }
            Dimension::Collection => {
                self.y.volume = 1;
                self.y.book = 1;
                self.x.chapter = 1;
                self.x.section = 1;
                self.x.scroll = 1;
            }
            Dimension::Volume => {
                self.y.book = 1;
                self.x.chapter = 1;
                self.x.section = 1;
                self.x.scroll = 1;
            }
            Dimension::Book => {
                self.x.chapter = 1;
                self.x.section = 1;
                self.x.scroll = 1;
            }
            Dimension::Chapter => {
                self.x.section = 1;
                self.x.scroll = 1;
            }
            Dimension::Section => {
                self.x.scroll = 1;
            }
            Dimension::Scroll => {}
        }
        true
    }

    fn dimension_value(&self, dim: Dimension) -> usize {
        match dim {
            Dimension::Scroll => self.x.scroll,
            Dimension::Section => self.x.section,
            Dimension::Chapter => self.x.chapter,
            Dimension::Book => self.y.book,
            Dimension::Volume => self.y.volume,
            Dimension::Collection => self.y.collection,
            Dimension::Series => self.z.series,
            Dimension::Shelf => self.z.shelf,
            Dimension::Library => self.z.library,
        }
    }

    fn set_dimension(&mut self, dim: Dimension, value: usize) {
        let clamped = value.clamp(COORDINATE_MINIMUM, COORDINATE_MAXIMUM);
        match dim {
            Dimension::Scroll => self.x.scroll = clamped,
            Dimension::Section => self.x.section = clamped,
            Dimension::Chapter => self.x.chapter = clamped,
            Dimension::Book => self.y.book = clamped,
            Dimension::Volume => self.y.volume = clamped,
            Dimension::Collection => self.y.collection = clamped,
            Dimension::Series => self.z.series = clamped,
            Dimension::Shelf => self.z.shelf = clamped,
            Dimension::Library => self.z.library = clamped,
        }
    }
}
