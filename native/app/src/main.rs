//! Phext Notepad window.
//!
//! FLTK draws the tree, the editor, and the native open/save dialogs. The
//! phext itself stays in `phext-notepad-core` (libphext for the bytes). No
//! web view and no extra crates.

#![cfg_attr(target_os = "windows", windows_subsystem = "windows")]

use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;

use fltk::enums::{Align, Color, Event, Font, FrameType, Key, Shortcut};
use fltk::prelude::*;
use fltk::{app, button, dialog, frame, input, menu, text, tree, window};
use phext_notepad_core::zoom::OutlineNode;
use phext_notepad_core::{parse_coordinate, Document, Prefs, Theme, ZoomLevel};

const MENU_H: i32 = 28;
const COORD_H: i32 = 100;
const STEP_H: i32 = 32;
const STATUS_H: i32 = 24;
const GRIP: i32 = 6;

const LEVELS: [ZoomLevel; 3] = [ZoomLevel::Outer, ZoomLevel::Middle, ZoomLevel::Inner];
const ARM_RGB: [(u8, u8, u8); 3] = [(0, 168, 176), (196, 64, 168), (36, 150, 72)];

#[derive(Clone)]
enum Ui {
    New,
    Open,
    Save,
    SaveAs,
    Baseline,
    Close,
    Recent(usize),
    ToggleTree,
    ToggleWrap,
    Zoom(ZoomLevel),
    ZoomIn,
    ZoomOut,
    Theme(Theme),
    About,
    TreePick { clicks: i32 },
    Buffer,
    CoordCommit,
    Nudge(usize, i32),
    Splitter { x: i32, release: bool },
    Relayout,
}

struct Palette {
    window: Color,
    paper: Color,
    ink: Color,
    muted: Color,
    grip: Color,
    select: Color,
}

fn palette(dark: bool) -> Palette {
    if dark {
        Palette {
            window: Color::from_rgb(23, 23, 23),
            paper: Color::from_rgb(28, 28, 28),
            ink: Color::from_rgb(230, 230, 230),
            muted: Color::from_rgb(160, 160, 160),
            grip: Color::from_rgb(55, 55, 55),
            select: Color::from_rgb(36, 92, 140),
        }
    } else {
        Palette {
            window: Color::from_rgb(247, 244, 238),
            paper: Color::from_rgb(255, 253, 248),
            ink: Color::from_rgb(28, 25, 21),
            muted: Color::from_rgb(90, 84, 74),
            grip: Color::from_rgb(210, 204, 192),
            select: Color::from_rgb(186, 210, 236),
        }
    }
}

fn system_dark() -> bool {
    #[cfg(target_os = "macos")]
    {
        let output = std::process::Command::new("defaults")
            .args(["read", "-g", "AppleInterfaceStyle"])
            .output();
        return output
            .ok()
            .map(|out| {
                String::from_utf8_lossy(&out.stdout)
                    .to_ascii_lowercase()
                    .contains("dark")
            })
            .unwrap_or(false);
    }
    #[cfg(target_os = "windows")]
    {
        let output = std::process::Command::new("reg")
            .args([
                "query",
                r"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "/v",
                "AppsUseLightTheme",
            ])
            .output();
        if let Ok(output) = output {
            let text = String::from_utf8_lossy(&output.stdout);
            if text.contains("0x0") {
                return true;
            }
            if text.contains("0x1") {
                return false;
            }
        }
        return false;
    }
    #[cfg(not(any(target_os = "macos", target_os = "windows")))]
    {
        if std::env::var("GTK_THEME")
            .map(|theme| theme.to_ascii_lowercase().contains("dark"))
            .unwrap_or(false)
        {
            return true;
        }
        let output = std::process::Command::new("gsettings")
            .args(["get", "org.gnome.desktop.interface", "color-scheme"])
            .output();
        if let Ok(output) = output {
            let text = String::from_utf8_lossy(&output.stdout).to_ascii_lowercase();
            if text.contains("dark") {
                return true;
            }
        }
        false
    }
}

fn prefs_path() -> PathBuf {
    if cfg!(target_os = "windows") {
        if let Ok(appdata) = std::env::var("APPDATA") {
            return PathBuf::from(appdata)
                .join("phext-notepad")
                .join("settings.txt");
        }
    }
    if cfg!(target_os = "macos") {
        if let Ok(home) = std::env::var("HOME") {
            return PathBuf::from(home)
                .join("Library")
                .join("Application Support")
                .join("phext-notepad")
                .join("settings.txt");
        }
    }
    let base = std::env::var("XDG_CONFIG_HOME")
        .ok()
        .filter(|value| !value.is_empty())
        .map(PathBuf::from)
        .or_else(|| {
            std::env::var("HOME")
                .ok()
                .map(|home| PathBuf::from(home).join(".config"))
        })
        .unwrap_or_else(|| PathBuf::from("."));
    base.join("phext-notepad").join("settings.txt")
}

fn menu_label(name: &str) -> String {
    name.chars()
        .map(|ch| match ch {
            '/' | '\\' => ' ',
            '&' | '_' => ' ',
            other => other,
        })
        .collect()
}

fn with_phext(mut path: PathBuf) -> PathBuf {
    let missing = path
        .extension()
        .and_then(|ext| ext.to_str())
        .map(|ext| ext.is_empty())
        .unwrap_or(true);
    if missing {
        path.set_extension("phext");
    }
    path
}

fn suggested_name(doc: &Document) -> String {
    if let Some(path) = doc.path() {
        if let Some(name) = path.file_name() {
            let name = name.to_string_lossy();
            if name != "baseline.phext" {
                return name.into_owned();
            }
        }
    }
    "notes.phext".to_string()
}

struct Notepad {
    app: app::App,
    wind: window::Window,
    menu: menu::MenuBar,
    crumbs: frame::Frame,
    zoom_out: button::Button,
    zoom_in: button::Button,
    level_buttons: [button::Button; 3],
    coord_label: frame::Frame,
    coordinate: input::Input,
    coord_error: frame::Frame,
    arm_labels: [frame::Frame; 3],
    minus: [button::Button; 3],
    plus: [button::Button; 3],
    dim_labels: [frame::Frame; 3],
    outline_title: frame::Frame,
    tree: tree::Tree,
    grip: frame::Frame,
    editor: text::TextEditor,
    buffer: text::TextBuffer,
    status: frame::Frame,
    doc: Document,
    prefs: Prefs,
    prefs_path: PathBuf,
    tree_coords: HashMap<String, String>,
    recent_labels: Vec<String>,
    applying: Arc<AtomicBool>,
    filling: Arc<AtomicBool>,
    tree_key: String,
    tx: app::Sender<Ui>,
    last_caret: i32,
    coord_was_focused: bool,
    coord_select_pending: bool,
    dark: bool,
    quitting: bool,
}

impl Notepad {
    fn new(running: app::App, tx: app::Sender<Ui>) -> Self {
        let prefs_path = prefs_path();
        let mut prefs = Prefs::load(&prefs_path);
        let (doc, open_note) = open_initial(&prefs);
        prefs.zoom = doc.zoom();
        prefs.coordinate = format!("{}", doc.coordinate());

        let mut wind = window::Window::default().with_size(1100, 720);
        wind.set_label("Phext Notepad");
        wind.size_range(720, 480, 0, 0);
        wind.make_resizable(true);

        let mut menu_bar = menu::MenuBar::new(0, 0, 1100, MENU_H, "");
        menu_bar.end();
        add_menus(&mut menu_bar, &tx, &prefs);

        let mut crumbs = frame::Frame::new(8, 32, 700, 22, "");
        crumbs.set_align(Align::Left | Align::Inside);
        crumbs.set_frame(FrameType::NoBox);

        let mut zoom_out = button::Button::new(0, 30, 72, 26, "Zoom out");
        let mut level_buttons = [
            button::Button::new(0, 30, 52, 26, "11D"),
            button::Button::new(0, 30, 52, 26, "7D"),
            button::Button::new(0, 30, 52, 26, "4D"),
        ];
        let mut zoom_in = button::Button::new(0, 30, 72, 26, "Zoom in");
        wire(&mut zoom_out, &tx, Ui::ZoomOut);
        wire(&mut zoom_in, &tx, Ui::ZoomIn);
        for (index, button) in level_buttons.iter_mut().enumerate() {
            wire(button, &tx, Ui::Zoom(LEVELS[index]));
            button.set_tooltip(LEVELS[index].arm_title());
        }

        let mut coord_label = frame::Frame::new(0, 0, 100, 26, "Coordinate");
        coord_label.set_align(Align::Left | Align::Inside);
        coord_label.set_frame(FrameType::NoBox);
        coord_label.set_label_size(14);

        let mut coordinate = input::Input::new(0, 0, 10, 28, "");
        coordinate.set_text_font(Font::Courier);
        coordinate.set_text_size(16);
        coordinate.set_maximum_size(128);
        coordinate.set_tooltip(
            "Type or paste a coordinate such as 1.2.3/4.5.6/7.8.9, then press Enter",
        );
        // set_callback keeps FLTK's own key handling. A custom handle()
        // would replace it, and the coordinate box would ignore typing and paste.
        coordinate.set_trigger(enums_trigger());
        let tx_field = tx.clone();
        coordinate.set_callback(move |_| {
            let _ = tx_field.send(Ui::CoordCommit);
        });

        let mut coord_error = frame::Frame::new(0, 0, 10, 18, "");
        coord_error.set_align(Align::Left | Align::Inside);
        coord_error.set_frame(FrameType::NoBox);
        coord_error.set_label_size(12);

        let mut arm_labels = [
            frame::Frame::new(0, 0, 10, 16, "library.shelf.series"),
            frame::Frame::new(0, 0, 10, 16, "collection.volume.book"),
            frame::Frame::new(0, 0, 10, 16, "chapter.section.scroll"),
        ];
        for (index, label) in arm_labels.iter_mut().enumerate() {
            label.set_align(Align::Left | Align::Inside);
            label.set_frame(FrameType::NoBox);
            label.set_label_size(12);
            label.set_label_color(Color::from_rgb(
                ARM_RGB[index].0,
                ARM_RGB[index].1,
                ARM_RGB[index].2,
            ));
        }

        let mut minus = [
            button::Button::new(0, 0, 28, 24, "-"),
            button::Button::new(0, 0, 28, 24, "-"),
            button::Button::new(0, 0, 28, 24, "-"),
        ];
        let mut plus = [
            button::Button::new(0, 0, 28, 24, "+"),
            button::Button::new(0, 0, 28, 24, "+"),
            button::Button::new(0, 0, 28, 24, "+"),
        ];
        let mut dim_labels = [
            frame::Frame::new(0, 0, 88, 24, ""),
            frame::Frame::new(0, 0, 88, 24, ""),
            frame::Frame::new(0, 0, 88, 24, ""),
        ];
        for (index, button) in minus.iter_mut().enumerate() {
            wire(button, &tx, Ui::Nudge(index, -1));
        }
        for (index, button) in plus.iter_mut().enumerate() {
            wire(button, &tx, Ui::Nudge(index, 1));
        }
        for label in &mut dim_labels {
            label.set_align(Align::Center | Align::Inside);
            label.set_frame(FrameType::NoBox);
            label.set_label_size(13);
        }

        let mut outline_title = frame::Frame::new(0, 0, 10, 22, "");
        outline_title.set_align(Align::Left | Align::Inside);
        outline_title.set_frame(FrameType::NoBox);
        outline_title.set_label_size(13);

        let mut tree = tree::Tree::new(0, 0, 10, 10, "");
        tree.set_select_mode(tree::TreeSelect::Single);
        tree.set_show_root(false);
        let filling = Arc::new(AtomicBool::new(false));
        {
            let tx = tx.clone();
            let filling = Arc::clone(&filling);
            tree.set_callback(move |widget| {
                if filling.load(Ordering::Relaxed) {
                    return;
                }
                let reason = widget.callback_reason();
                if reason == tree::TreeReason::Selected || reason == tree::TreeReason::Reselected {
                    let _ = tx.send(Ui::TreePick {
                        clicks: app::event_clicks_num(),
                    });
                }
            });
        }

        let mut grip = frame::Frame::new(0, 0, GRIP, 10, "");
        grip.set_frame(FrameType::FlatBox);
        {
            let tx = tx.clone();
            grip.handle(move |_, event| {
                if event == Event::Push || event == Event::Drag || event == Event::Released {
                    let _ = tx.send(Ui::Splitter {
                        x: app::event_x(),
                        release: event == Event::Released,
                    });
                    return true;
                }
                false
            });
        }

        let mut buffer = text::TextBuffer::default();
        let applying = Arc::new(AtomicBool::new(false));
        {
            let tx = tx.clone();
            let applying = Arc::clone(&applying);
            buffer.add_modify_callback(move |_, _, _, _, _| {
                if !applying.load(Ordering::Relaxed) {
                    let _ = tx.send(Ui::Buffer);
                }
            });
        }
        let mut editor = text::TextEditor::new(0, 0, 10, 10, "");
        editor.set_buffer(buffer.clone());
        editor.set_text_font(Font::Courier);
        editor.set_text_size(15);
        editor.set_linenumber_width(46);
        editor.wrap_mode(
            if prefs.word_wrap {
                text::WrapMode::AtBounds
            } else {
                text::WrapMode::None
            },
            0,
        );
        let mut status = frame::Frame::new(0, 0, 1100, STATUS_H, "");
        status.set_align(Align::Left | Align::Inside);
        status.set_frame(FrameType::FlatBox);
        status.set_label_size(12);

        {
            let tx = tx.clone();
            // FLTK calls this when the user closes the window. The event is
            // already finished by then, so there is nothing else to match.
            wind.set_callback(move |_| {
                let _ = tx.send(Ui::Close);
            });
        }
        {
            let tx = tx.clone();
            wind.resize_callback(move |_, _, _, _, _| {
                let _ = tx.send(Ui::Relayout);
            });
        }

        wind.end();

        let mut notepad = Self {
            app: running,
            wind,
            menu: menu_bar,
            crumbs,
            zoom_out,
            zoom_in,
            level_buttons,
            coord_label,
            coordinate,
            coord_error,
            arm_labels,
            minus,
            plus,
            dim_labels,
            outline_title,
            tree,
            grip,
            editor,
            buffer,
            status,
            doc,
            prefs,
            prefs_path,
            tree_coords: HashMap::new(),
            recent_labels: Vec::new(),
            applying,
            filling,
            tree_key: String::new(),
            tx,
            last_caret: -1,
            coord_was_focused: false,
            coord_select_pending: false,
            dark: false,
            quitting: false,
        };
        if let Some(note) = open_note {
            notepad.doc.set_message(note);
        }
        notepad.apply_theme();
        notepad.sync_recent();
        notepad
    }

    fn apply_theme(&mut self) {
        self.dark = match self.prefs.theme {
            Theme::Dark => true,
            Theme::Light => false,
            Theme::System => system_dark(),
        };
        let colors = palette(self.dark);
        if self.dark {
            app::foreground(230, 230, 230);
            app::background(23, 23, 23);
            app::background2(28, 28, 28);
        } else {
            app::foreground(28, 25, 21);
            app::background(247, 244, 238);
            app::background2(255, 253, 248);
        }
        self.wind.set_color(colors.window);
        self.crumbs.set_label_color(colors.ink);
        self.outline_title.set_label_color(colors.ink);
        for label in &mut self.dim_labels {
            label.set_label_color(colors.ink);
        }
        self.tree.set_color(colors.paper);
        self.tree.set_item_label_fgcolor(colors.ink);
        self.tree.set_item_label_bgcolor(colors.paper);
        self.grip.set_color(colors.grip);
        self.editor.set_color(colors.paper);
        self.editor.set_text_color(colors.ink);
        self.editor.set_cursor_color(colors.ink);
        self.editor.set_selection_color(colors.select);
        self.status.set_color(colors.window);
        self.status.set_label_color(colors.muted);
        self.coord_label.set_label_color(colors.ink);
        self.coordinate.set_color(colors.paper);
        self.coordinate.set_text_color(colors.ink);
        self.coordinate.set_cursor_color(colors.ink);
        self.coordinate.set_selection_color(colors.select);
        self.coord_error.set_label_color(if self.dark {
            Color::from_rgb(255, 140, 130)
        } else {
            Color::from_rgb(176, 32, 32)
        });
        self.wind.redraw();
    }

    fn layout(&mut self) {
        let width = self.wind.w();
        let height = self.wind.h();
        self.menu.resize(0, 0, width, MENU_H);

        let button_w = 52;
        let zoom_in_x = width - 8 - 72;
        let levels_x = zoom_in_x - 8 - button_w * 3;
        let zoom_out_x = levels_x - 8 - 72;
        self.zoom_out.resize(zoom_out_x, MENU_H + 2, 72, 26);
        for (index, button) in self.level_buttons.iter_mut().enumerate() {
            button.resize(levels_x + index as i32 * button_w, MENU_H + 2, button_w, 26);
        }
        self.zoom_in.resize(zoom_in_x, MENU_H + 2, 72, 26);
        self.crumbs.resize(8, MENU_H + 4, (zoom_out_x - 16).max(40), 22);

        let coord_y = MENU_H + 32;
        self.coord_label.resize(8, coord_y, 100, 28);
        self.coordinate
            .resize(112, coord_y, (width - 124).max(160), 28);

        let legend_y = coord_y + 30;
        let legend_w = ((width - 24) / 3).max(40);
        for index in 0..3 {
            self.arm_labels[index].resize(8 + index as i32 * legend_w, legend_y, legend_w - 8, 16);
        }
        self.coord_error
            .resize(8, legend_y + 16, (width - 16).max(40), 20);

        let step_y = MENU_H + COORD_H;
        let mut x = 8;
        for index in 0..3 {
            self.minus[index].resize(x, step_y + 4, 28, 24);
            x += 28;
            self.dim_labels[index].resize(x, step_y + 4, 96, 24);
            x += 100;
            self.plus[index].resize(x, step_y + 4, 28, 24);
            x += 40;
        }

        let body_y = step_y + STEP_H;
        let body_h = (height - body_y - STATUS_H).max(40);
        let tree_limit = (width - 200).max(120);
        let tree_w = if self.prefs.tree_visible {
            (self.prefs.splitter as i32).clamp(120, tree_limit)
        } else {
            0
        };
        if self.prefs.tree_visible {
            self.outline_title.show();
            self.tree.show();
            self.grip.show();
            self.outline_title.resize(8, body_y, (tree_w - 8).max(8), 22);
            self.tree.resize(0, body_y + 22, tree_w, (body_h - 22).max(20));
            self.grip.resize(tree_w, body_y, GRIP, body_h);
            let editor_x = tree_w + GRIP;
            self.editor.resize(editor_x, body_y, (width - editor_x).max(80), body_h);
        } else {
            self.outline_title.hide();
            self.tree.hide();
            self.grip.hide();
            self.editor.resize(0, body_y, width, body_h);
        }
        self.status.resize(0, height - STATUS_H, width, STATUS_H);
        self.wind.redraw();
    }

    fn refresh(&mut self) {
        let view = self.doc.view("show", &self.prefs);
        let star = if view.dirty { "*" } else { "" };
        self.wind.set_label(&format!("{star}{} - Phext Notepad", view.filename));

        let mut crumb = view.outline_title.clone();
        if !view.crumbs.is_empty() {
            crumb.push_str("    ");
            for (index, item) in view.crumbs.iter().enumerate() {
                if index > 0 {
                    crumb.push_str("   /   ");
                }
                crumb.push_str(&item.label);
            }
        }
        self.crumbs.set_label(&crumb);

        self.sync_coordinate_field(&view.coordinate);
        for (index, arm) in view.arms.iter().enumerate() {
            let (red, green, blue) = ARM_RGB[index];
            let color = if arm.active {
                Color::from_rgb(red, green, blue)
            } else {
                palette(self.dark).muted
            };
            self.arm_labels[index].set_label_color(color);
            let button_color = if arm.active {
                Color::from_rgb(red, green, blue)
            } else {
                palette(self.dark).ink
            };
            self.level_buttons[index].set_label_color(button_color);
        }

        if let Some(arm) = view.arms.iter().find(|arm| arm.active) {
            for (index, dim) in arm.dimensions.iter().enumerate() {
                self.dim_labels[index].set_label(&format!("{} {}", dim.label, dim.value));
                self.minus[index].set_tooltip(&format!("Previous {}", dim.label.to_lowercase()));
                self.plus[index].set_tooltip(&format!("Next {}", dim.label.to_lowercase()));
            }
        }
        if view.can_zoom_in {
            self.zoom_in.activate();
        } else {
            self.zoom_in.deactivate();
        }
        if view.can_zoom_out {
            self.zoom_out.activate();
        } else {
            self.zoom_out.deactivate();
        }

        let scroll = view.scroll.clone();
        if self.buffer.text() != scroll {
            self.applying.store(true, Ordering::Relaxed);
            self.buffer.set_text(&scroll);
            self.applying.store(false, Ordering::Relaxed);
            self.editor.redraw();
        }
        self.editor.wrap_mode(
            if self.prefs.word_wrap {
                text::WrapMode::AtBounds
            } else {
                text::WrapMode::None
            },
            0,
        );
        let key = tree_key(&view.outline, &view.outline_title);
        if key != self.tree_key {
            self.tree_key = key;
            self.fill_tree(&view.outline, &view.outline_title);
        }
        self.sync_checks();
        self.update_status();
        self.layout();
    }

    fn fill_tree(&mut self, nodes: &[OutlineNode], title: &str) {
        self.filling.store(true, Ordering::Relaxed);
        self.tree.clear();
        self.tree_coords.clear();
        self.tree.set_root_label(title);
        let current = add_nodes(&mut self.tree, &mut self.tree_coords, nodes, "");
        self.tree.set_show_root(nodes.is_empty());
        if let Some(path) = current {
            let _ = self.tree.select(&path, false);
        }
        self.outline_title.set_label(title);
        self.filling.store(false, Ordering::Relaxed);
    }

    fn sync_coordinate_field(&mut self, canonical: &str) {
        let typed = self.coordinate.value();
        if self.coordinate.has_focus() && typed != canonical {
            match parse_coordinate(&typed) {
                Some(parsed) if format!("{parsed}") == canonical => {}
                _ => return,
            }
        }
        if typed != canonical {
            self.coordinate.set_value(canonical);
            if self.coordinate.has_focus() {
                self.select_coordinate_text();
            }
        }
        self.coord_error.set_label("");
    }

    fn select_coordinate_text(&mut self) {
        let len = self.coordinate.value().len() as i32;
        // The one-argument position call also moves the mark. Place the
        // cursor at the end, then the mark at the start, so the whole
        // address is selected and a paste replaces it.
        let _ = self.coordinate.set_position(len);
        let _ = self.coordinate.set_mark(0);
    }

    fn note_coordinate_focus(&mut self) {
        let focused = self.coordinate.has_focus();
        let event = app::event();
        if focused && !self.coord_was_focused {
            // A click focuses on mouse-down and then places the caret. Wait
            // for mouse-up before selecting, so that click does not undo it.
            // Tab and other focus changes select immediately.
            self.coord_select_pending = event == Event::Push || event == Event::Released;
            if !self.coord_select_pending {
                self.select_coordinate_text();
            }
        }
        if focused && self.coord_select_pending && event == Event::Released {
            self.select_coordinate_text();
            self.coord_select_pending = false;
        }
        if !focused {
            self.coord_select_pending = false;
        }
        self.coord_was_focused = focused;
        let draft_ok = focused && parse_coordinate(&self.coordinate.value()).is_some();
        if draft_ok && !self.coord_error.label().is_empty() {
            self.coord_error.set_label("");
            self.coord_error.redraw();
        }
    }

    fn note_caret(&mut self) {
        let pos = self.editor.insert_position();
        if pos == self.last_caret {
            return;
        }
        self.last_caret = pos;
        self.update_status();
    }

    fn update_status(&mut self) {
        let view = self.doc.view("status", &self.prefs);
        let pos = self.editor.insert_position();
        let line = self.buffer.count_lines(0, pos) + 1;
        let column = pos - self.buffer.line_start(pos) + 1;
        let message = if view.message.is_empty() {
            String::new()
        } else {
            format!("    {}", view.message)
        };
        self.status.set_label(&format!(
            "  {}    Line {}  Col {}{}",
            view.summary, line, column, message
        ));
        let star = if view.dirty { "*" } else { "" };
        self.wind.set_label(&format!("{star}{} - Phext Notepad", view.filename));
        self.status.redraw();
    }

    fn sync_checks(&mut self) {
        set_check(&mut self.menu, "View/Show Tree", self.prefs.tree_visible);
        set_check(&mut self.menu, "View/Word Wrap", self.prefs.word_wrap);
        set_radio(
            &mut self.menu,
            &[
                ("View/Zoom/11D Library", ZoomLevel::Outer),
                ("View/Zoom/7D Collection", ZoomLevel::Middle),
                ("View/Zoom/4D Chapter", ZoomLevel::Inner),
            ],
            self.doc.zoom(),
        );
        set_radio(
            &mut self.menu,
            &[
                ("View/Theme/System", Theme::System),
                ("View/Theme/Dark", Theme::Dark),
                ("View/Theme/Light", Theme::Light),
            ],
            self.prefs.theme,
        );
    }

    fn sync_recent(&mut self) {
        let mut indexes = Vec::new();
        for label in &self.recent_labels {
            let index = self.menu.find_index(label);
            if index >= 0 {
                indexes.push(index);
            }
        }
        indexes.sort_unstable();
        for index in indexes.into_iter().rev() {
            self.menu.remove(index);
        }
        self.recent_labels.clear();

        if self.prefs.recent.is_empty() {
            let path = "File/Open Recent/(none)".to_string();
            self.menu.add(&path, Shortcut::None, menu::MenuFlag::Inactive, |_| {});
            self.recent_labels.push(path);
            return;
        }
        for (index, full) in self.prefs.recent.iter().enumerate() {
            let name = Path::new(full)
                .file_name()
                .map(|name| name.to_string_lossy().into_owned())
                .unwrap_or_else(|| full.clone());
            let mut safe = menu_label(&name);
            if self.recent_labels.iter().any(|label| label.ends_with(&safe)) {
                safe = format!("{safe}  {}", index + 1);
            }
            let path = format!("File/Open Recent/{safe}");
            let tx = self.tx.clone();
            self.menu.add(
                &path,
                Shortcut::None,
                menu::MenuFlag::Normal,
                move |_| {
                    let _ = tx.send(Ui::Recent(index));
                },
            );
            self.recent_labels.push(path);
        }
    }

    fn commit_buffer(&mut self) {
        let text = self.buffer.text();
        self.doc.set_scroll(&text);
    }

    fn persist(&mut self) {
        self.prefs.zoom = self.doc.zoom();
        self.prefs.coordinate = format!("{}", self.doc.coordinate());
        if let Err(err) = self.prefs.store(&self.prefs_path) {
            self.doc.set_message(err);
        }
    }

    fn confirm_discard(&mut self) -> bool {
        self.commit_buffer();
        if !self.doc.dirty() {
            return true;
        }
        match dialog::choice2_default(
            "Save changes to this phext?",
            "Save",
            "Don't Save",
            "Cancel",
        ) {
            Some(0) => self.save(false),
            Some(1) => true,
            _ => false,
        }
    }

    fn save(&mut self, save_as: bool) -> bool {
        self.commit_buffer();
        let existing = if save_as {
            None
        } else {
            self.doc.path().map(Path::to_path_buf)
        };
        let Some(path) = existing.or_else(|| choose_save(&self.doc, &self.prefs)) else {
            return false;
        };
        match self.doc.save_to(&path) {
            Ok(()) => {
                self.prefs.remember(&path);
                self.persist();
                self.sync_recent();
                self.refresh();
                true
            }
            Err(err) => {
                self.doc.set_message(err);
                self.update_status();
                false
            }
        }
    }

    fn open_path(&mut self, path: &Path) {
        match self.doc.load_path(path) {
            Ok(()) => {
                self.prefs.remember(path);
                self.doc.set_zoom(self.prefs.zoom);
                self.persist();
                self.sync_recent();
            }
            Err(err) => self.doc.set_message(err),
        }
        self.refresh();
    }

    fn close_requested(&mut self) {
        if self.quitting {
            return;
        }
        if !self.confirm_discard() {
            return;
        }
        self.persist();
        self.quitting = true;
        self.wind.hide();
        self.app.quit();
    }

    fn handle(&mut self, message: Ui) {
        if self.quitting {
            return;
        }
        match message {
            Ui::Relayout => self.layout(),
            Ui::Buffer => {
                let previous = leaf_preview(self.doc.scroll_text());
                self.commit_buffer();
                if leaf_preview(self.doc.scroll_text()) == previous {
                    self.update_status();
                } else {
                    self.refresh();
                }
            }
            Ui::New => {
                if !self.confirm_discard() {
                    return;
                }
                let zoom = self.doc.zoom();
                self.doc = Document::empty();
                self.doc.set_zoom(zoom);
                self.doc.set_message("New phext");
                self.refresh();
            }
            Ui::Open => {
                if !self.confirm_discard() {
                    return;
                }
                if let Some(path) = choose_open(&self.doc, &self.prefs) {
                    self.open_path(&path);
                }
            }
            Ui::Baseline => {
                if !self.confirm_discard() {
                    return;
                }
                let zoom = self.doc.zoom();
                self.doc = Document::baseline();
                self.doc.set_zoom(zoom);
                self.doc.set_message("Loaded baseline.phext");
                self.refresh();
            }
            Ui::Save => {
                self.save(false);
            }
            Ui::SaveAs => {
                self.save(true);
            }
            Ui::Close => self.close_requested(),
            Ui::Recent(index) => {
                let Some(path) = self.prefs.recent.get(index).cloned() else {
                    return;
                };
                if !self.confirm_discard() {
                    return;
                }
                self.open_path(Path::new(&path));
            }
            Ui::ToggleTree => {
                self.prefs.tree_visible = !self.prefs.tree_visible;
                self.persist();
                self.refresh();
            }
            Ui::ToggleWrap => {
                self.prefs.word_wrap = !self.prefs.word_wrap;
                self.persist();
                self.refresh();
            }
            Ui::Zoom(level) => {
                self.doc.set_zoom(level);
                self.prefs.zoom = level;
                self.persist();
                self.refresh();
            }
            Ui::ZoomIn => {
                if self.doc.zoom_in() {
                    self.prefs.zoom = self.doc.zoom();
                    self.persist();
                    self.refresh();
                }
            }
            Ui::ZoomOut => {
                if self.doc.zoom_out() {
                    self.prefs.zoom = self.doc.zoom();
                    self.persist();
                    self.refresh();
                }
            }
            Ui::Theme(theme) => {
                self.prefs.theme = theme;
                self.persist();
                self.apply_theme();
                self.refresh();
            }
            Ui::About => {
                dialog::message_default(&format!(
                    "Phext Notepad {}\n\nA plain-text editor for 11-dimensional phext.\n\n11D  library.shelf.series\n7D   collection.volume.book\n4D   chapter.section.scroll\n\nFiles open and save through libphext.",
                    env!("CARGO_PKG_VERSION")
                ));
            }
            Ui::CoordCommit => self.commit_coordinate(),
            Ui::Nudge(index, delta) => {
                let name = self
                    .doc
                    .view("nudge", &self.prefs)
                    .arms
                    .into_iter()
                    .find(|arm| arm.active)
                    .and_then(|arm| arm.dimensions.get(index).map(|dim| dim.name.clone()));
                if let Some(name) = name {
                    let _ = self.doc.nudge(&name, delta);
                    self.persist();
                    self.refresh();
                }
            }
            Ui::TreePick { clicks } => {
                let Some(item) = self.tree.callback_item() else {
                    return;
                };
                let Ok(path) = self.tree.item_pathname(&item) else {
                    return;
                };
                let Some(coordinate) = self.tree_coords.get(&path).cloned() else {
                    return;
                };
                if self.doc.jump(&coordinate).is_ok() {
                    if clicks >= 2 {
                        self.doc.zoom_in();
                    }
                    self.prefs.zoom = self.doc.zoom();
                    self.persist();
                    self.refresh();
                }
            }
            Ui::Splitter { x, release } => {
                if !self.prefs.tree_visible {
                    return;
                }
                let limit = (self.wind.w() - 200).max(120);
                let width = x.clamp(120, limit) as u32;
                self.prefs.set_splitter(width);
                if release {
                    self.persist();
                }
                self.layout();
            }
        }
    }

    fn commit_coordinate(&mut self) {
        let address = self.coordinate.value();
        match self.doc.jump(&address) {
            Ok(()) => {
                self.coord_error.set_label("");
                self.persist();
                self.refresh();
            }
            Err(err) => {
                self.coord_error.set_label(&err);
                self.coord_error.redraw();
            }
        }
    }
}

fn add_menus(menu_bar: &mut menu::MenuBar, tx: &app::Sender<Ui>, prefs: &Prefs) {
    let entries: [(&str, Shortcut, menu::MenuFlag, Ui); 16] = [
        ("File/New", Shortcut::Command | 'n', menu::MenuFlag::Normal, Ui::New),
        ("File/Open...", Shortcut::Command | 'o', menu::MenuFlag::Normal, Ui::Open),
        ("File/Open Baseline", Shortcut::None, menu::MenuFlag::Normal, Ui::Baseline),
        ("File/Save", Shortcut::Command | 's', menu::MenuFlag::Normal, Ui::Save),
        (
            "File/Save As...",
            Shortcut::Command | Shortcut::Shift | 's',
            menu::MenuFlag::Normal,
            Ui::SaveAs,
        ),
        ("File/Exit", Shortcut::Command | 'q', menu::MenuFlag::Normal, Ui::Close),
        (
            "View/Show Tree",
            Shortcut::None,
            if prefs.tree_visible {
                menu::MenuFlag::Toggle | menu::MenuFlag::Value
            } else {
                menu::MenuFlag::Toggle
            },
            Ui::ToggleTree,
        ),
        (
            "View/Word Wrap",
            Shortcut::None,
            if prefs.word_wrap {
                menu::MenuFlag::Toggle | menu::MenuFlag::Value
            } else {
                menu::MenuFlag::Toggle
            },
            Ui::ToggleWrap,
        ),
        (
            "View/Zoom/11D Library",
            Shortcut::Alt | '1',
            menu::MenuFlag::Radio,
            Ui::Zoom(ZoomLevel::Outer),
        ),
        (
            "View/Zoom/7D Collection",
            Shortcut::Alt | '2',
            menu::MenuFlag::Radio,
            Ui::Zoom(ZoomLevel::Middle),
        ),
        (
            "View/Zoom/4D Chapter",
            Shortcut::Alt | '3',
            menu::MenuFlag::Radio,
            Ui::Zoom(ZoomLevel::Inner),
        ),
        ("View/Zoom In", Shortcut::Alt | Key::Down, menu::MenuFlag::Normal, Ui::ZoomIn),
        ("View/Zoom Out", Shortcut::Alt | Key::Up, menu::MenuFlag::Normal, Ui::ZoomOut),
        ("View/Theme/System", Shortcut::None, menu::MenuFlag::Radio, Ui::Theme(Theme::System)),
        ("View/Theme/Dark", Shortcut::None, menu::MenuFlag::Radio, Ui::Theme(Theme::Dark)),
        ("View/Theme/Light", Shortcut::None, menu::MenuFlag::Radio, Ui::Theme(Theme::Light)),
    ];
    for (path, shortcut, flag, message) in entries {
        let tx = tx.clone();
        menu_bar.add(path, shortcut, flag, move |_| {
            let _ = tx.send(message.clone());
        });
    }
    let tx = tx.clone();
    menu_bar.add("Help/About", Shortcut::None, menu::MenuFlag::Normal, move |_| {
        let _ = tx.send(Ui::About);
    });
}

fn wire(button: &mut button::Button, tx: &app::Sender<Ui>, message: Ui) {
    let tx = tx.clone();
    button.set_callback(move |_| {
        let _ = tx.send(message.clone());
    });
}

fn set_check(menu_bar: &mut menu::MenuBar, path: &str, on: bool) {
    if let Some(mut item) = menu_bar.find_item(path) {
        if on {
            item.set();
        } else {
            item.clear();
        }
    }
}

fn set_radio<T: PartialEq>(menu_bar: &mut menu::MenuBar, items: &[(&str, T)], current: T) {
    for (path, value) in items {
        if let Some(mut item) = menu_bar.find_item(path) {
            if *value == current {
                item.set();
            } else {
                item.clear();
            }
        }
    }
}

fn add_nodes(
    tree: &mut tree::Tree,
    coords: &mut HashMap<String, String>,
    nodes: &[OutlineNode],
    parent: &str,
) -> Option<String> {
    let mut current = None;
    for node in nodes {
        let mut label = node.label.clone();
        if !node.preview.is_empty() {
            label.push_str("  ");
            label.push_str(&node.preview.replace(['/', '\\'], " "));
        } else if node.scroll_count > 1 && node.children.is_empty() {
            label.push_str(&format!("  ({})", node.scroll_count));
        } else if !node.children.is_empty() {
            label.push_str(&format!("  ({})", node.scroll_count));
        }
        let label = menu_label(&label);
        let path = if parent.is_empty() {
            label
        } else {
            format!("{parent}/{label}")
        };
        let Some(item) = tree.add(&path) else {
            continue;
        };
        coords.insert(path.clone(), node.coordinate.clone());
        if let Ok(canonical) = tree.item_pathname(&item) {
            coords.insert(canonical, node.coordinate.clone());
        }
        if node.on_path && !node.children.is_empty() {
            let _ = tree.open(&path, false);
        }
        if node.current {
            current = Some(path.clone());
        }
        if let Some(child) = add_nodes(tree, coords, &node.children, &path) {
            current = Some(child);
        }
    }
    current
}

fn choose_open(doc: &Document, prefs: &Prefs) -> Option<PathBuf> {
    let mut dialog = dialog::NativeFileChooser::new(dialog::FileDialogType::BrowseFile);
    dialog.set_title("Open phext");
    dialog.set_filter("Phext\t*.phext\nAll files\t*");
    if let Some(dir) = start_dir(doc, prefs) {
        let _ = dialog.set_directory(&dir);
    }
    match dialog.try_show() {
        Ok(dialog::FileDialogAction::Success) => {
            let path = dialog.filename();
            if path.as_os_str().is_empty() {
                None
            } else {
                Some(path)
            }
        }
        _ => None,
    }
}

fn choose_save(doc: &Document, prefs: &Prefs) -> Option<PathBuf> {
    let mut dialog = dialog::NativeFileChooser::new(dialog::FileDialogType::BrowseSaveFile);
    dialog.set_title("Save phext");
    dialog.set_filter("Phext\t*.phext\nAll files\t*");
    dialog.set_option(dialog::FileDialogOptions::SaveAsConfirm);
    dialog.set_preset_file(&suggested_name(doc));
    if let Some(dir) = start_dir(doc, prefs) {
        let _ = dialog.set_directory(&dir);
    }
    match dialog.try_show() {
        Ok(dialog::FileDialogAction::Success) => {
            let path = dialog.filename();
            if path.as_os_str().is_empty() {
                None
            } else {
                Some(with_phext(path))
            }
        }
        _ => None,
    }
}

fn start_dir(doc: &Document, prefs: &Prefs) -> Option<PathBuf> {
    if let Some(path) = doc.path() {
        if let Some(parent) = path.parent() {
            if !parent.as_os_str().is_empty() {
                return Some(parent.to_path_buf());
            }
        }
    }
    prefs
        .recent
        .first()
        .and_then(|path| Path::new(path).parent().map(Path::to_path_buf))
}

fn tree_key(nodes: &[OutlineNode], title: &str) -> String {
    fn walk(out: &mut String, nodes: &[OutlineNode]) {
        for node in nodes {
            out.push_str(&node.id);
            out.push('|');
            out.push_str(&node.preview);
            out.push('|');
            out.push(if node.current { '1' } else { '0' });
            out.push(if node.on_path { '1' } else { '0' });
            out.push(';');
            walk(out, &node.children);
        }
    }
    let mut out = title.to_string();
    walk(&mut out, nodes);
    out
}

fn leaf_preview(text: &str) -> String {
    let line = text.trim_start().lines().next().unwrap_or("");
    let mut short: String = line.chars().take(40).collect();
    if line.chars().count() > 40 {
        short.push('…');
    }
    short
}

fn open_initial(prefs: &Prefs) -> (Document, Option<String>) {
    let arg = std::env::args().skip(1).find(|arg| !arg.starts_with('-'));
    if let Some(arg) = arg {
        let path = PathBuf::from(&arg);
        if path.is_file() {
            let mut doc = Document::empty();
            doc.set_zoom(prefs.zoom);
            return match doc.load_path(&path) {
                Ok(()) => {
                    restore_coordinate(&mut doc, prefs);
                    (doc, None)
                }
                Err(err) => {
                    let mut doc = Document::baseline();
                    doc.set_zoom(prefs.zoom);
                    (doc, Some(err))
                }
            };
        }
        let mut doc = Document::baseline();
        doc.set_zoom(prefs.zoom);
        return (doc, Some(format!("Could not open {arg}")));
    }
    if let Some(last) = &prefs.last_path {
        let path = PathBuf::from(last);
        if path.is_file() {
            let mut doc = Document::empty();
            doc.set_zoom(prefs.zoom);
            if doc.load_path(&path).is_ok() {
                restore_coordinate(&mut doc, prefs);
                return (doc, None);
            }
        }
    }
    let mut doc = Document::baseline();
    doc.set_zoom(prefs.zoom);
    restore_coordinate(&mut doc, prefs);
    doc.set_message("Loaded baseline.phext");
    (doc, None)
}

fn restore_coordinate(doc: &mut Document, prefs: &Prefs) {
    if let Some(coordinate) = parse_coordinate(&prefs.coordinate) {
        doc.set_coordinate(coordinate);
    }
    doc.mark_clean();
}

fn main() {
    let running = app::App::default().with_scheme(app::Scheme::Oxy);
    let (tx, rx) = app::channel::<Ui>();
    let mut notepad = Notepad::new(running, tx);
    notepad.wind.show();
    notepad.layout();
    notepad.refresh();
    while running.wait() {
        notepad.note_coordinate_focus();
        notepad.note_caret();
        while let Some(message) = rx.recv() {
            notepad.handle(message);
        }
    }
}

fn enums_trigger() -> fltk::enums::CallbackTrigger {
    fltk::enums::CallbackTrigger::EnterKeyAlways
}
