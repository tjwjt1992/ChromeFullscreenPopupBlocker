[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $repo 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'VERSION must contain a semantic version.' }
$out = Join-Path $repo 'build\x64'
$binaries = @('FullscreenPopupBlocker-x64.exe', 'PopupHook-x64.dll')
foreach ($name in $binaries) {
    if (-not (Test-Path -LiteralPath (Join-Path $out $name) -PathType Leaf)) { throw 'Run scripts/build.ps1 before packaging.' }
}
$stage = Join-Path $repo 'build\package'
$dist = Join-Path $repo 'dist'
New-Item -ItemType Directory -Force -Path $stage, $dist | Out-Null
foreach ($name in $binaries) {
    Copy-Item -LiteralPath (Join-Path $out $name) -Destination $stage -Force
}
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination $stage -Force
(Get-Content -LiteralPath (Join-Path $repo 'README.md') -Raw).Replace('(third_party/minhook/LICENSE.txt)', '(MINHOOK-LICENSE.txt)') |
    Set-Content -LiteralPath (Join-Path $stage 'README.md') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $repo 'third_party\minhook\LICENSE.txt') -Destination (Join-Path $stage 'MINHOOK-LICENSE.txt') -Force
$zip = Join-Path $dist "FullscreenPopupBlocker-$version-x64.zip"
$files = ($binaries + @('README.md', 'LICENSE', 'MINHOOK-LICENSE.txt')) | ForEach-Object { Join-Path $stage $_ }
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
((Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant() + '  ' + (Split-Path $zip -Leaf)) |
    Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Encoding ascii
Write-Host "Portable ZIP and SHA-256 checksum: $dist"
