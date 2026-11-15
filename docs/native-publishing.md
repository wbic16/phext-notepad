# Publishing native Phext Notepad builds

The cross-platform app is in [`native/`](../native/). A version tag on `exo` builds unsigned Windows, macOS, and Linux binaries and attaches them to the GitHub Release. The install page at <https://wbic16.github.io/phext-notepad/> links to those files. The WinForms ClickOnce publish still runs from the same tag.

## Cut a release

The tag has to be `MAJOR.MINOR.PATCH`, with or without a leading `v`. The tagged commit needs `.github/workflows/publish-native.yml`.

```sh
git checkout exo
git pull
git tag v0.5.0
git push origin v0.5.0
```

Wait for **Publish native builds** to finish. It:

1. Runs the Rust tests for the document and zoom model.
2. Sets the app version from the tag (`v0.5.0` becomes `0.5.0` in the two `Cargo.toml` files for that build).
3. Builds one release binary on each runner and packs it:
   - Windows: `phext-notepad-windows-x86_64.zip`
   - macOS: `phext-notepad-macos-aarch64.tar.gz` (the macOS runner is Apple silicon)
   - Linux: `phext-notepad-linux-x86_64.tar.gz`
4. Attaches those archives to the GitHub Release for the tag. If the release already exists, the notes already on it stay. The upload does not replace them.

The install page reads the latest release and points the macOS, Linux, and native Windows buttons at those archives. It also keeps the Windows ClickOnce `setup.exe` button. The page itself is copied from `install/index.html` by the ClickOnce workflow, which runs on the same tag. Open the page after both workflows finish.

A manual **Run workflow** is available once this file is on the repository default branch. That button builds the three binaries and uploads them as Actions artifacts. It does not publish a GitHub Release. Tag pushes are what publish.

## What you get

People do not need a GitHub account. Release files download from the public release.

Each archive contains one executable, `phext-notepad` (`phext-notepad.exe` on Windows). Unzip or untar it and run that file. There is no installer, and the app does not need WebView2 or Node.js.

These builds are **unsigned**. Expect the usual warnings:

- Windows SmartScreen: **More info**, then **Run anyway**.
- macOS Gatekeeper: the binary is from an unidentified developer. Right-click it, choose **Open**, then choose **Open** again. On recent macOS you may also need **Privacy & Security** and **Open Anyway**.
- Linux: mark the file executable (`chmod +x phext-notepad`) and run it. A desktop may ask you to trust an untrusted program.

Code signing is deliberately not part of this workflow. There is no Azure step.

## Where code signing plugs in

Sign the finished binary after `cargo build --release`, then upload that signed file. The workflow does not call a signing action.

| Platform | Tool | When |
| --- | --- | --- |
| macOS | `codesign` and `notarytool` | After the release binary exists. A Developer ID Application certificate stays in repository secrets, not in the repo. |
| Windows | `signtool` with an Authenticode certificate | After `phext-notepad.exe` is linked. Do not commit the certificate. A self-signed certificate only puts a name on the file; SmartScreen still warns. |
| Linux | A detached signature next to the archive | Optional. There is no platform signature for a bare binary. |

Do not add Azure Trusted Signing.

## Running on Windows

Prerequisites:

- [Rust](https://rustup.rs/) 1.90 or newer (`rustup update stable`)
- CMake and the Visual Studio C++ build tools. Rustup's MSVC toolchain prompt installs the build tools. CMake is used once, while FLTK is compiled into the binary. After that, the exe does not need them.

WebView2 is not used. Node.js is not used.

From the repository, in PowerShell:

```powershell
cd native
cargo test -p phext-notepad-core
cargo build --release -p phext-notepad
.\target\release\phext-notepad.exe
```

`cargo build -p phext-notepad` writes a debug exe at `.\target\debug\phext-notepad.exe`. Run that file directly. The window is drawn by FLTK, which is compiled into the exe.

A file path on the command line opens that phext:

```powershell
.\target\release\phext-notepad.exe C:\notes\journal.phext
```

## Build on your own machine

Rust 1.90 or newer, plus a C++ compiler and CMake. The first release build compiles FLTK from source. Later builds reuse that.

Linux also needs the X11, Cairo, and Pango headers the FLTK build links:

```sh
sudo apt-get install -y build-essential cmake pkg-config \
  libx11-dev libxext-dev libxft-dev libxinerama-dev \
  libxcursor-dev libxrender-dev libxfixes-dev \
  libpango1.0-dev libcairo2-dev libfontconfig1-dev \
  libgl1-mesa-dev libglu1-mesa-dev
```

macOS needs the Xcode command line tools and CMake (`brew install cmake`).

```sh
cd native
cargo test -p phext-notepad-core
cargo build --release -p phext-notepad
./target/release/phext-notepad
```

## What the native app is

`native/core` is the phext model. It uses [libphext](https://github.com/wbic16/libphext-rs) 0.3.1 for `explode` / `implode` and coordinate limits. Dimension steps are the `CoordinateNav` behavior from [phext-lattice](https://github.com/wbic16/phext-lattice) at exo `7c91992`, copied into `native/core/src/nav.rs`. The lattice crate also links memmap2, rayon, and tempfile, and those are not optional features, so this app does not depend on the crate. Zoom follows lattice's three tiers: 11D `library.shelf.series`, 7D `collection.volume.book`, 4D `chapter.section.scroll`. The window is FLTK in `native/app`. The C# project at the repository root is unchanged.
