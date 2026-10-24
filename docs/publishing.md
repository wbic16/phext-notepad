# Publishing the Windows installer

Phext Notepad installs from one page:

**https://wbic16.github.io/phext-notepad/**

That page has a **Download Phext Notepad** button. The button saves `setup.exe`. People do not need a GitHub account. After install, opening the app checks that same site and asks to update when a newer publish is there.

The program shortcut is named Phext Notepad. The process file is still `pnd.exe`.

This builds on the window layout in pull request #1, which is already merged to `exo`.

## One-time setup

Do this once, before the first publish. The workflow cannot turn these on by itself.

1. Merge the ClickOnce pull request into `exo`.
2. Open the repository **Settings → Pages**. Under **Build and deployment**, set **Source** to **GitHub Actions**. The repository needs to be public (or on a plan that serves Pages to anonymous visitors) so the download works for people who are not signed in.
3. Leave the two certificate secrets empty for the first publish. The installer is unsigned until you add them. Windows shows a warning; the install still completes. Details are below.
4. Publish by pushing a version tag to the `exo` commit you want to ship:

   ```sh
   git checkout exo
   git pull
   git tag v0.4.5
   git push origin v0.4.5
   ```

   The tag must be `MAJOR.MINOR.PATCH`, with or without a leading `v`. `v0.4.5` and `0.4.5` both work. The tagged commit has to contain `.github/workflows/publish-clickonce.yml`.

5. Wait for the **Publish ClickOnce** workflow to finish. The install page above should offer the download. A tag also creates or updates the matching GitHub Release and attaches `PhextNotepad-Setup.zip`.

The Actions **Run workflow** button stays hidden until this workflow file is on the repository's default branch (`master` today). Tag pushes do not need that. To use the button earlier, merge the workflow file to the default branch, or change the default branch to `exo`. When the button is available, run it from the **exo** branch.

## What each publish does

The workflow runs on `windows-latest` and calls `publish-clickonce.ps1`.

- It builds a self-contained 64-bit app, so the person installing it does not install .NET first.
- It writes a ClickOnce folder: `setup.exe`, `pnd.application`, and `Application Files\`.
- It puts the plain-language page from `install/index.html` at the site root, plus `.nojekyll` so GitHub Pages does not rewrite those files.
- It deploys that folder to GitHub Pages.
- On a tag, it attaches two zip files to the GitHub Release:
  - `PhextNotepad-Setup.zip` — `setup.exe` and `INSTALL.txt`. This is the simple download.
  - `PhextNotepad-ClickOnce.zip` — the whole site, if you want to host the same files somewhere else.

The ClickOnce version is `<tag>.<workflow run number>`. Tag `v0.4.5` on run 12 becomes version `0.4.5.12`. Each publish is a higher number, so copies that are already installed are offered the update. The update is checked when the app opens. If the site is down, the installed app still launches.

A manual run uses `VersionPrefix` in `PhextNotepad.csproj` unless you type a version. It updates the install page and uploads the zips as workflow artifacts. It does not create a GitHub Release.

## Code signing and the SmartScreen warning

With no certificate secrets, the publish is **unsigned**. That is enough for the app to install.

People will see some or all of these:

- **Windows protected your PC** (SmartScreen) when they open `setup.exe`. They click **More info**, then **Run anyway**.
- A ClickOnce window that says the **publisher is unknown**. They click **Install**.

`setup.exe` is the ClickOnce bootstrapper. It only knows how to download the app from the install URL baked in at publish time. Unsigned manifests still install on a normal Windows PC. Some locked-down work PCs block unsigned ClickOnce; a real certificate is the fix for that.

### Add a certificate later

Create two repository secrets (Settings → Secrets and variables → Actions):

| Secret | Value |
| --- | --- |
| `CLICKONCE_PFX_BASE64` | The `.pfx` file, base64-encoded, as one line |
| `CLICKONCE_PFX_PASSWORD` | The password for that `.pfx` |

The next publish signs the ClickOnce manifests (and `setup.exe`, which the SDK signs with the same certificate). Use the **same** certificate for later releases. A new certificate looks like a different publisher, and people who already installed may have to install again instead of updating.

A **self-signed** certificate is enough to put a publisher name on the ClickOnce window. Windows still does not trust it, so SmartScreen and the unknown-publisher warning remain. Do not ask people to install your self-signed certificate into their trusted store.

A **purchased code-signing certificate** (the kind sold for software publishers) is what removes those warnings. Export it as a `.pfx` and store it in the same two secrets. No workflow change is required.

Create a self-signed certificate on a Windows PC and turn it into the secret value:

```powershell
$cert = New-SelfSignedCertificate `
  -Type CodeSigningCert `
  -Subject "CN=Phext, Inc." `
  -CertStoreLocation Cert:\CurrentUser\My `
  -FriendlyName "Phext Notepad ClickOnce" `
  -NotAfter (Get-Date).AddYears(5)

$secure = Read-Host "PFX password" -AsSecureString
$pfx = Join-Path $env:USERPROFILE "phext-notepad-clickonce.pfx"
Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $secure

$b64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($pfx))
$b64Path = Join-Path $env:USERPROFILE "phext-notepad-clickonce.pfx.b64.txt"
[IO.File]::WriteAllText($b64Path, $b64, [Text.UTF8Encoding]::new($false))
Write-Host "Paste the contents of $b64Path into the CLICKONCE_PFX_BASE64 secret."
Write-Host "Do not commit the .pfx or the .b64.txt file."
```

If signing fails because the timestamp server is unreachable, rerun the workflow. A signed publish is not uploaded when signing fails.

## Use a different install address

The default address is `https://<owner>.github.io/<repo>/` (this repo: `https://wbic16.github.io/phext-notepad/`).

To publish somewhere else, including the older `https://mirrorborn.us/phext-notepad/` folder, set the repository **variable** `CLICKONCE_INSTALL_URL` to that folder URL. It must start with `https://` or `http://`. The workflow adds a trailing slash if you leave it off. You can also type an address in the **Run workflow** form.

The address is stored inside `setup.exe` and `pnd.application` at publish time. Change the variable, then publish again. Copies installed from the old address do not move over by themselves.

An earlier publish profile pointed at `https://mirrorborn.us/phext-notepad/`. This profile uses GitHub Pages so there is one stable link that GitHub Actions can update.

GitHub Pages does not let you set custom MIME types. `setup.exe` downloads the app itself, so the install does not depend on the browser opening `.application` files. If a future Windows update gets picky about that, host `PhextNotepad-ClickOnce.zip` on a server that serves:

- `.application` as `application/x-ms-application`
- `.manifest` as `application/x-ms-manifest`
- `.deploy` as `application/octet-stream`

`install/web.config` and `install/.htaccess` are copied into the site for IIS and Apache. Point `CLICKONCE_INSTALL_URL` at that copy and publish again.

## If a publish fails

Re-running a failed Actions job checks out the same tag, so it builds the same scripts and fails the same way. Merge the fix into `exo` first, then move the tag onto that commit (or push a new tag) so the workflow file that runs is the fixed one.

`setup.exe` is created by MSBuild's `GenerateBootstrapper` task. `dotnet msbuild` stops with **MSB4803** ("The task GenerateBootstrapper is not supported on the .NET Core version of MSBuild") and does not produce an installer. The script calls Visual Studio's `MSBuild.exe` instead. A failure is also written as a GitHub Actions error annotation that quotes the first `error` line from MSBuild.

The v0.4.4 tag's first run failed this way: https://github.com/wbic16/phext-notepad/actions/runs/40057451929

## Local notes

`pwsh ./publish-clickonce.ps1 -ResolveOnly -Version 0.4.5 -Revision 3` prints the version and URL and does not build.

A full publish has to be on Windows, using Visual Studio's MSBuild. `dotnet build` works on Linux. `dotnet msbuild -t:Publish` does not produce `setup.exe`.
