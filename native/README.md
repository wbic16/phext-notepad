# Phext Notepad (native)

Cross-platform Phext Notepad for Windows, macOS, and Linux. The C# WinForms app stays at the repository root and still publishes with ClickOnce.

## Stack

Rust and [FLTK](https://www.fltk.org/) through [fltk-rs](https://github.com/fltk-rs/fltk-rs). The window, tree, text editor, and open/save dialogs are FLTK widgets. FLTK is compiled into the binary (`fltk-bundled`), so a machine that runs the app does not need FLTK installed. There is no web view and no Node.js.

The phext model is a separate crate, `core/`, so it can be tested without a window.

- [libphext-rs](https://github.com/wbic16/libphext-rs) 0.3.1 opens and saves phext (`explode` / `implode`, coordinate limits, break rules). This is the same crate [phext-lattice](https://github.com/wbic16/phext-lattice) depends on. libphext-py is the Python port of that format. It is not linked here.
- Dimension steps match phext-lattice `CoordinateNav` at exo commit `7c91992`. Stepping a dimension forward or back resets the dimensions below it, matching a phext break. That code lives in `core/src/nav.rs`. phext-lattice itself is not a dependency: its memmap2, rayon, and tempfile crates are required even when unused, and this editor does not use them.
- Zoom is the lattice's three tiers, built into the outline rather than layered on afterwards:
  - **Outer / 11D / z** — `library.shelf.series`
  - **Middle / 7D / y** — `collection.volume.book` inside the current outer arm
  - **Inner / 4D / x** — `chapter.section.scroll` inside the current book, which is the scroll you edit

The lattice memory-map and sentron panel are not in this editor. Notepad holds the scrolls in memory the way the WinForms app does.

## What it does

- New, open, and save `.phext` files, plus the embedded baseline
- Coordinate bar: `library.shelf.series / collection.volume.book / chapter.section.scroll`
- Outline of the arm you are zoomed to, side by side with the scroll editor
- Draggable splitter. Hiding the tree gives the editor the whole row. The width is kept while the tree is hidden, and both are saved.
- Three zoom levels. The tree and the coordinate bar follow the current level. Double-click a row to zoom in.
- Dark, light, or follow the system
- Recent files, word wrap, line and column

Not carried over from WinForms: SQ sync, vim keys, scroll lock, font settings, and the hierarchical checksum panel.

## Crates

Two crates are linked into the app, and each one earns its place:

| Crate | Why it is here |
| --- | --- |
| `libphext` | Phext `explode` / `implode` and the coordinate type. Rewriting the format would drift from the other phext tools. |
| `fltk` | The window, tree, editor, and native file dialogs. The `fltk-bundled` feature compiles FLTK in, so Windows, macOS, and Linux builds do not need a system copy. Image decoders and OpenGL are off. |

`fltk` also brings its own small crates (`fltk-sys`, and a font parser). CMake is a build tool, used while FLTK is compiled, and it is not linked into the exe. Preferences are a hand-written `key=value` file. The outline and the dimension steps are hand-written in `core/`.

## Develop

Rust 1.90 or newer. A C++ compiler and CMake are needed the first time FLTK is compiled. Linux also needs the X11, Cairo, and Pango development packages listed in [docs/native-publishing.md](../docs/native-publishing.md).

```sh
cd native
cargo test -p phext-notepad-core
cargo build --release -p phext-notepad
./target/release/phext-notepad
```

On Windows the executable is `target\release\phext-notepad.exe`. A debug build is `cargo build -p phext-notepad` and `target\debug\phext-notepad.exe`.

`cargo tree --prefix none | sort -u | wc -l` counts the crates in the resolved graph.

Release builds are described in [docs/native-publishing.md](../docs/native-publishing.md).
