#!/usr/bin/env pwsh
param(
   [string] $version,
   [string] $app = "PhextNotepad",
   [switch] $force
)
if (-not $version) {
   Write-Host "Usage: build.ps1 <version>"
   exit 1
}
$known = git tag
if (-not $force -and ($known -match $version)) {
   Write-Host "Error: $version is already tagged"
   exit 1
}
$csproj = Get-Content -raw "$app.csproj"
if (-not ($csproj -match "$version")) {
   $csproj = $csproj -replace "<VersionPrefix>[^<]*</VersionPrefix>","<VersionPrefix>$version</VersionPrefix>"
   Write-Host "Patching $app.csproj..."
   $csproj | Out-File -Encoding utf8 "$app.csproj"
}
$phext = Get-Content -raw "Changelog.phext"
if (-not ($phext -match "v$version")) {
   Write-Host "Error: You need to document the changes first."
   git diff
   exit 1
}
$test = git status
if ($test -match "Changes not staged") {
   Write-Host "You have un-committed changes."
   git status
   exit 1
}
git tag $version -f
git push -f
git push --tags -f
Write-Host "Released $version into the wild..."