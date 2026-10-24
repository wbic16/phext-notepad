# Publishes Phext Notepad as a ClickOnce installer (setup.exe plus the
# deployment folder). Windows-only: the .NET SDK's GenerateLauncher task
# and the ClickOnce bootstrapper do not run on Linux.
#
# Called by .github/workflows/publish-clickonce.yml.
# Local check of the version and URL only:
#   pwsh ./publish-clickonce.ps1 -ResolveOnly -Version 0.4.5 -Revision 3

#Requires -Version 7
[CmdletBinding()]
param(
    [string]$Version = "",
    [string]$InstallUrl = "",
    [int]$Revision = 0,
    [switch]$ResolveOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
Set-Location $RepoRoot

function Get-VersionPrefix {
    $csproj = Get-Content -Raw (Join-Path $RepoRoot "PhextNotepad.csproj")
    if ($csproj -notmatch "<VersionPrefix>([^<]+)</VersionPrefix>") {
        throw "PhextNotepad.csproj has no VersionPrefix."
    }
    return $Matches[1].Trim()
}

function Get-BaseVersion {
    param([string]$Explicit)

    $raw = $Explicit
    if ([string]::IsNullOrWhiteSpace($raw) -and $env:GITHUB_REF -like "refs/tags/*") {
        $raw = $env:GITHUB_REF.Substring("refs/tags/".Length)
    }
    if ([string]::IsNullOrWhiteSpace($raw)) {
        $raw = Get-VersionPrefix
    }

    $raw = $raw.Trim()
    if ($raw.StartsWith("v") -or $raw.StartsWith("V")) {
        $raw = $raw.Substring(1)
    }
    if ($raw -notmatch "^(\d+)\.(\d+)\.(\d+)$") {
        throw "Version '$raw' is not MAJOR.MINOR.PATCH. Push a tag like v0.4.5, or pass -Version 0.4.5."
    }
    foreach ($part in @($Matches[1], $Matches[2], $Matches[3])) {
        if ([int]$part -gt 65535) {
            throw "Version '$raw' has a component above 65535, which ClickOnce cannot store."
        }
    }
    return $raw
}

function Get-InstallUrl {
    param([string]$Explicit)

    $url = $Explicit
    if ([string]::IsNullOrWhiteSpace($url)) {
        $url = $env:CLICKONCE_INSTALL_URL
    }
    if ([string]::IsNullOrWhiteSpace($url)) {
        if ($env:GITHUB_REPOSITORY -match "^([^/]+)/([^/]+)$") {
            $url = "https://$($Matches[1]).github.io/$($Matches[2])/"
        }
        else {
            $url = "https://wbic16.github.io/phext-notepad/"
        }
    }

    $url = $url.Trim()
    if ($url -notmatch "^https?://") {
        throw "Install URL must start with https:// (or http://). Got '$url'."
    }
    if (-not $url.EndsWith("/")) {
        $url += "/"
    }
    return $url
}

function Write-GitHubOutput {
    param([string]$Name, [string]$Value)
    if ($env:GITHUB_OUTPUT) {
        Add-Content -Path $env:GITHUB_OUTPUT -Value "$Name=$Value"
    }
}

function Find-BootstrapperSdk {
    $known = @()
    if (${env:ProgramFiles(x86)}) {
        $known += Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs\ClickOnce Bootstrapper"
    }
    if ($env:ProgramFiles) {
        $known += Join-Path $env:ProgramFiles "Microsoft SDKs\ClickOnce Bootstrapper"
    }
    foreach ($dir in $known) {
        if (Test-Path (Join-Path $dir "Engine\setup.bin")) {
            return (Resolve-Path $dir).Path
        }
    }

    $roots = @()
    if (${env:ProgramFiles(x86)}) {
        $roots += Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs"
    }
    if ($env:ProgramFiles) {
        $roots += Join-Path $env:ProgramFiles "Microsoft SDKs"
    }
    foreach ($root in $roots) {
        if (-not $root -or -not (Test-Path $root)) {
            continue
        }
        $hit = Get-ChildItem -Path $root -Filter setup.bin -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.Directory.Name -eq "Engine" } |
            Select-Object -First 1
        if ($hit) {
            return $hit.Directory.Parent.FullName
        }
    }
    return $null
}

function Import-ClickOnceCertificate {
    $b64 = $env:CLICKONCE_PFX_BASE64
    $password = $env:CLICKONCE_PFX_PASSWORD
    $hasB64 = -not [string]::IsNullOrWhiteSpace($b64)
    $hasPassword = -not [string]::IsNullOrWhiteSpace($password)
    if (-not $hasB64 -and -not $hasPassword) {
        return $null
    }
    if (-not $hasB64 -or -not $hasPassword) {
        throw "Set both CLICKONCE_PFX_BASE64 and CLICKONCE_PFX_PASSWORD, or leave both empty to publish unsigned."
    }

    $tempRoot = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
    $pfxPath = Join-Path $tempRoot "phext-notepad-clickonce.pfx"
    try {
        $clean = ($b64.Trim().TrimStart([char]0xFEFF) -replace '\s', '')
        [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($clean))
        $secure = ConvertTo-SecureString -String $password -AsPlainText -Force
        $imported = @(Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\CurrentUser\My -Password $secure)
        $cert = $imported | Where-Object { $_.HasPrivateKey } | Select-Object -First 1
        if (-not $cert) {
            throw "The PFX did not include a private key. Export the code-signing certificate again and include the key."
        }
        Write-Host "Signing ClickOnce manifests with certificate thumbprint $($cert.Thumbprint)."
        return $cert.Thumbprint
    }
    finally {
        if (Test-Path $pfxPath) {
            Remove-Item $pfxPath -Force
        }
    }
}

$baseVersion = Get-BaseVersion -Explicit $Version
if ($Revision -lt 0 -or $Revision -gt 65535) {
    throw "Revision $Revision is outside the ClickOnce range 0..65535."
}
$fullVersion = "$baseVersion.$Revision"
$resolvedInstallUrl = Get-InstallUrl -Explicit $InstallUrl

Write-Host "ClickOnce version: $fullVersion"
Write-Host "Install URL: $resolvedInstallUrl"

Write-GitHubOutput -Name "version" -Value $fullVersion
Write-GitHubOutput -Name "version_base" -Value $baseVersion
Write-GitHubOutput -Name "install_url" -Value $resolvedInstallUrl

if ($ResolveOnly) {
    Write-Host "ResolveOnly set; not publishing."
    exit 0
}

if (-not $IsWindows) {
    throw "ClickOnce publish uses Windows-only tools (GenerateLauncher and setup.exe). Run this on windows-latest."
}

$bootstrapperSdk = Find-BootstrapperSdk
if (-not $bootstrapperSdk) {
    throw "Could not find the ClickOnce bootstrapper (Engine\setup.bin). The Windows runner needs the Visual Studio ClickOnce bootstrapper SDK."
}
Write-Host "Bootstrapper SDK: $bootstrapperSdk"

$thumbprint = Import-ClickOnceCertificate
$signed = -not [string]::IsNullOrWhiteSpace($thumbprint)
Write-GitHubOutput -Name "signed" -Value ($(if ($signed) { "true" } else { "false" }))
if (-not $signed) {
    Write-Host "No CLICKONCE_PFX_BASE64 secret. Publishing unsigned. Installers still run; Windows shows an unknown-publisher warning."
}

$publishDir = Join-Path $RepoRoot "artifacts\clickonce"
$artifactDir = Join-Path $RepoRoot "artifacts"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

$commonProps = @(
    "-p:Configuration=Release",
    "-p:PublishProfile=ClickOnceProfile",
    "-p:RuntimeIdentifier=win-x64",
    "-p:SelfContained=true",
    "-p:PublishSingleFile=false",
    "-p:Version=$fullVersion",
    "-p:IncludeSourceRevisionInInformationalVersion=false",
    "-p:DebugType=none",
    "-p:DebugSymbols=false"
)

Write-Host "Restoring..."
& dotnet restore (Join-Path $RepoRoot "PhextNotepad.csproj") -r win-x64 @commonProps
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

$publishProps = $commonProps + @(
    "-p:PublishDir=$publishDir",
    "-p:PublishUrl=$publishDir",
    "-p:InstallUrl=$resolvedInstallUrl",
    "-p:UpdateUrl=$resolvedInstallUrl",
    "-p:ApplicationVersion=$fullVersion",
    "-p:ApplicationRevision=$Revision",
    "-p:IsRevisionIncremented=false",
    "-p:GenerateBootstrapperSdkPath=$bootstrapperSdk",
    "-v:m",
    "-nologo"
)
if ($signed) {
    $publishProps += "-p:SignManifests=true"
    $publishProps += "-p:ManifestCertificateThumbprint=$thumbprint"
    $publishProps += "-p:ManifestTimestampUrl=http://timestamp.digicert.com"
}
else {
    $publishProps += "-p:SignManifests=false"
}

Write-Host "Publishing ClickOnce..."
& dotnet msbuild (Join-Path $RepoRoot "PhextNotepad.csproj") -t:Publish @publishProps
if ($LASTEXITCODE -ne 0) {
    throw "ClickOnce publish failed with exit code $LASTEXITCODE."
}

if ($signed) {
    Remove-Item -Path "Cert:\CurrentUser\My\$thumbprint" -Force -ErrorAction SilentlyContinue
}

$siteFiles = @(
    @{ Source = "install\index.html"; Dest = "index.html" },
    @{ Source = "install\INSTALL.txt"; Dest = "INSTALL.txt" },
    @{ Source = "install\web.config"; Dest = "web.config" },
    @{ Source = "install\.htaccess"; Dest = ".htaccess" },
    @{ Source = "Phext.png"; Dest = "Phext.png" }
)
foreach ($file in $siteFiles) {
    $sourcePath = Join-Path $RepoRoot $file.Source
    if (-not (Test-Path $sourcePath)) {
        throw "Missing $($file.Source)."
    }
    Copy-Item $sourcePath (Join-Path $publishDir $file.Dest) -Force
}
New-Item -ItemType File -Force -Path (Join-Path $publishDir ".nojekyll") | Out-Null
Set-Content -Path (Join-Path $publishDir "VERSION.txt") -Value @"
version=$fullVersion
install_url=$resolvedInstallUrl
signed=$signed
"@

$setupPath = Join-Path $publishDir "setup.exe"
$manifest = Get-ChildItem -Path $publishDir -Filter "*.application" | Select-Object -First 1
if (-not (Test-Path $setupPath)) {
    throw "Publish did not produce setup.exe."
}
if (-not $manifest) {
    throw "Publish did not produce a .application deployment manifest."
}

$manifestText = Get-Content -Raw $manifest.FullName
$urlMarker = $resolvedInstallUrl.TrimEnd("/")
if ($manifestText -notlike "*$urlMarker*") {
    throw "Deployment manifest $($manifest.Name) does not mention $resolvedInstallUrl, so updates would look in the wrong place."
}

$setupBytes = [IO.File]::ReadAllBytes($setupPath)
$asAscii = [Text.Encoding]::ASCII.GetString($setupBytes)
$asUnicode = [Text.Encoding]::Unicode.GetString($setupBytes)
$oddCount = $setupBytes.Length - 1
if ($oddCount % 2 -eq 1) { $oddCount-- }
$asUnicodeOdd = [Text.Encoding]::Unicode.GetString($setupBytes, 1, $oddCount)
if ($asAscii -notlike "*$urlMarker*" -and $asUnicode -notlike "*$urlMarker*" -and $asUnicodeOdd -notlike "*$urlMarker*") {
    throw "setup.exe does not contain $resolvedInstallUrl. The bootstrapper would not know where to download the app."
}

Write-Host "Published files:"
Get-ChildItem $publishDir -Force | ForEach-Object {
    if ($_.PSIsContainer) {
        $count = (Get-ChildItem $_.FullName -Recurse -File -Force).Count
        Write-Host ("  {0}/ ({1} files)" -f $_.Name, $count)
    }
    else {
        Write-Host ("  {0}  {1:N0} bytes" -f $_.Name, $_.Length)
    }
}

$setupZip = Join-Path $artifactDir "PhextNotepad-Setup.zip"
$siteZip = Join-Path $artifactDir "PhextNotepad-ClickOnce.zip"
$setupStage = Join-Path $artifactDir "setup-zip"
if (Test-Path $setupStage) { Remove-Item $setupStage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $setupStage | Out-Null
Copy-Item $setupPath (Join-Path $setupStage "setup.exe")
Copy-Item (Join-Path $publishDir "INSTALL.txt") (Join-Path $setupStage "INSTALL.txt")
if (Test-Path $setupZip) { Remove-Item $setupZip -Force }
if (Test-Path $siteZip) { Remove-Item $siteZip -Force }
Push-Location $setupStage
try {
    Compress-Archive -Path "setup.exe", "INSTALL.txt" -DestinationPath $setupZip
}
finally {
    Pop-Location
}
Push-Location $publishDir
try {
    $siteEntries = Get-ChildItem -Force -Name
    Compress-Archive -Path $siteEntries -DestinationPath $siteZip
}
finally {
    Pop-Location
}
Remove-Item $setupStage -Recurse -Force

Write-Host "Wrote $setupZip"
Write-Host "Wrote $siteZip"

if ($env:GITHUB_STEP_SUMMARY) {
    $signedLabel = if ($signed) { "yes (repository certificate)" } else { "no (unsigned; SmartScreen warning expected)" }
    @"
### ClickOnce publish

- Version: ``$fullVersion``
- Install page: $resolvedInstallUrl
- Direct download: ${resolvedInstallUrl}setup.exe
- Signed: $signedLabel
"@ | Add-Content -Path $env:GITHUB_STEP_SUMMARY
}
