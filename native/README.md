# Phext Notepad (native)

Cross-platform Phext Notepad for Windows, macOS, and Linux. The C# WinForms app stays at the repository root and still publishes with ClickOnce.

## Stack

Rust and [Tauri](https://tauri.app/) 2. The window is HTML and CSS. The phext model is a separate crate, `core/`, so it can be tested without a window.

- [libphext-rs](https://github.com/wbic16/libphext-rs) 0.3.1 opens and saves phext (`explode` / `implode`, coordinate parsing limits, break rules). This is the same crate [phext-lattice](https://github.com/wbic16/phext-lattice) depends on. libphext-py is the Python port of that format; it is not used here because the desktop shells are Rust.
- phext-lattice, pinned to `exo` commit `7c91992`, supplies `CoordinateNav`. Stepping a dimension forward or back resets the dimensions below it, matching a phext break.
- Zoom is the lattice's three tiers, built into the outline rather than layered on afterwards:
  - **Outer / 11D / z** — `library.shelf.series`
  - **Middle / 7D / y** — `collection.volume.book` inside the current outer arm
  - **Inner / 4D / x** — `chapter.section.scroll` inside the current book, which is the scroll you edit
- Colors follow phext-edit: cyan Z, magenta Y, green X, gold coordinates, on the lattice dark paper. Light mode uses the install-page paper. The default theme follows the operating system.

The lattice memory-map and sentron panel are not in this editor. Notepad holds the scrolls in memory the way the WinForms app does. That mmap index is the path for very large files later.

## What it does

- New, open, and save `.phext` files, plus the embedded baseline
- Coordinate bar: `library.shelf.series / collection.volume.book / chapter.section.scroll`
- Outline of the arm you are zoomed to, side by side with the scroll editor
- Draggable splitter. Hiding the tree gives the editor the whole row. The width and the hidden state are saved.
- Dark, light, or follow the system
- Recent files, word wrap, line and column

Not carried over from WinForms: SQ sync, vim keys, scroll lock, font settings, and the hierarchical checksum panel.

## Develop

Rust 1.90 or newer. On Linux, WebKitGTK 4.1 development headers. On Windows, the WebView2 runtime (already on Windows 11).

`cargo build` embeds `ui/` into the executable. Run that file directly. A plain debug build used to open `http://127.0.0.1:1420` and show a blank page when nothing was listening there.

```sh
cd native
cargo test -p phext-notepad-core
cargo build -p phext-notepad-native
```

The executable is `target/debug/phext-notepad-native` (`target\debug\phext-notepad-native.exe` on Windows).

`npm run tauri dev` rebuilds while you edit and serves `ui/` with Tauri's own file server. `npm run tauri build` produces the installer and a release executable at `target/release/phext-notepad-native`. Node.js is only required for those two npm commands. Windows setup is in [docs/native-publishing.md](../docs/native-publishing.md#running-on-windows).

`ui/fixture.json` is the baseline rendered by `cargo run -p phext-notepad-core --bin dump-fixture`. Opening `ui/index.html` from a local web server uses that fixture when Tauri is not there, so the layout can be checked in a browser. Open and save dialogs need the native shell.

Release builds are described in [docs/native-publishing.md](../docs/native-publishing.md).

## Dependencies

`ui/` is plain HTML, CSS, and JavaScript. Nothing bundles it. `package-lock.json` lists 12 packages, and every one is `@tauri-apps/cli` or a per-OS build of that CLI. `Cargo.lock` lists the crates the native app compiles. The window is Tauri with its default compression turned off, so those files are stored in the executable as themselves. Open and save call `rfd` directly. libphext and phext-lattice stay. Lattice also depends on its memory-map and rayon crates; this editor does not call them.
