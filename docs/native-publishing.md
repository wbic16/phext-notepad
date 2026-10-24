# Publishing native Phext Notepad builds

The cross-platform app is in [`native/`](../native/). A version tag on `exo` builds unsigned Windows, macOS, and Linux artifacts and attaches them to the GitHub Release. The install page at <https://wbic16.github.io/phext-notepad/> links to those files. The WinForms ClickOnce publish still runs from the same tag.

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
2. Sets the app version from the tag (`v0.5.0` becomes `0.5.0`).
3. Builds on three runners:
   - Windows: NSIS `*-setup.exe` and WiX `.msi`
   - macOS: a universal `.dmg` (Apple silicon and Intel) and the `.app`
   - Linux: `.deb` and `.AppImage`
4. Attaches those files to the GitHub Release for the tag.

The install page reads the latest release and points the macOS and Linux buttons at those files. It also keeps the Windows ClickOnce `setup.exe` button. The page itself is copied from `install/index.html` by the ClickOnce workflow, which runs on the same tag. Open the page after both workflows finish.

A manual **Run workflow** is available once this file is on the repository default branch. That button runs the tests. It does not publish a release. Tag pushes do not need the button, and they are what publish.

## What you get

People do not need a GitHub account. Release files download from the public release.

These builds are **unsigned**. Expect the usual warnings:

- Windows SmartScreen: **More info**, then **Run anyway**.
- macOS Gatekeeper: the app is from an unidentified developer. Right-click the app, choose **Open**, then **Open** again. A `.dmg` opens the same way.
- Linux: the AppImage is not signed. Mark it executable and run it. The `.deb` installs with your package tool.

Code signing is deliberately not part of this workflow. There is no Azure step.

## Where code signing plugs in

The plug-in point is the `env` block of the `tauri-apps/tauri-action` step in `.github/workflows/publish-native.yml`. Leave the secrets unset and the build stays unsigned. When a certificate exists, add the secrets below. No other workflow change is required for macOS.

| Secret | Use |
| --- | --- |
| `APPLE_CERTIFICATE` | Apple Distribution or Developer ID certificate, `.p12`, base64, one line |
| `APPLE_CERTIFICATE_PASSWORD` | Password for that `.p12` |
| `APPLE_SIGNING_IDENTITY` | The identity name inside the certificate |
| `APPLE_ID` | Apple ID email, for notarization |
| `APPLE_PASSWORD` | App-specific password for that Apple ID |
| `APPLE_TEAM_ID` | Ten-character team id |

`tauri-action` signs and notarizes the macOS app when those secrets are present.

Windows Authenticode is a later step, not Azure Trusted Signing. The place for it is `bundle.windows.signCommand` in `native/src-tauri/tauri.conf.json`, or a `signtool` step after the Windows build. Do not commit the certificate. A self-signed certificate only puts a name on the file; SmartScreen still warns.

Linux has no platform signature. A detached GPG signature on the AppImage can be added beside the upload step later.

## Running on Windows

Prerequisites:

- [Rust](https://rustup.rs/) 1.90 or newer (`rustup update stable`)
- [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/). Windows 11 already has it. On Windows 10, install the Evergreen runtime if the window says the runtime is missing.
- [Node.js](https://nodejs.org/) current or LTS, only for `npm run tauri`. `cargo build` does not need Node.
- The Tauri CLI comes from `npm install` in `native/` (`@tauri-apps/cli`). You do not install it globally.

From the repository, in PowerShell:

```powershell
cd native
cargo test -p phext-notepad-core
cargo build -p phext-notepad-native
.\target\debug\phext-notepad-native.exe
```

That executable has the window UI inside it. Rebuild after pulling this fix. An older debug exe opens `http://127.0.0.1:1420` and Edge shows "can't reach this page" / `ERR_CONNECTION_REFUSED` because nothing is serving that address.

While editing the UI or the Rust shell:

```powershell
cd native
npm install
npm run tauri dev
```

`tauri dev` serves `ui/` itself and recompiles when files change. It does not use a separate Python or `sh` server.

A release exe and the NSIS/MSI installers:

```powershell
cd native
npm install
npm run tauri build
.\target\release\phext-notepad-native.exe
```

The installers are under `target\release\bundle\`.

## Build on your own machine

Rust 1.90 or newer, plus the Tauri system libraries for your OS. Windows prerequisites are in the section above.

```sh
cd native
cargo test -p phext-notepad-core
cargo build -p phext-notepad-native
npm install
npm run tauri dev
npm run tauri build -- --bundles deb
```

`cargo build` is enough to run the app. `npm run tauri dev` is the reload loop. `npm run tauri build` makes the installer. On Linux the bundle flag is `deb` or `appimage`; on Windows the default `tauri build` produces `nsis` and `msi`; on macOS it produces `dmg` and `app`. The release workflow asks each runner only for the bundles that OS can produce.

## What the native app is

`native/core` is the phext model. It uses [libphext](https://github.com/wbic16/libphext-rs) 0.3.1 for `explode` / `implode` and coordinate breaks, and [phext-lattice](https://github.com/wbic16/phext-lattice) `CoordinateNav` for dimension steps. Zoom follows lattice's three tiers: 11D `library.shelf.series`, 7D `collection.volume.book`, 4D `chapter.section.scroll`. The window is a Tauri shell in `native/src-tauri`. The C# project at the repository root is unchanged.
