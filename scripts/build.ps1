[CmdletBinding()]
param(
    [string]$VcVarsPath
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (-not $VcVarsPath) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) { throw 'Visual Studio locator not found. Install C++ Build Tools or supply -VcVarsPath.' }
    $installation = & $vswhere -latest -prerelease -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if (-not $installation) { throw 'Install Visual Studio / Build Tools with the C++ desktop workload and Windows SDK.' }
    $VcVarsPath = Join-Path $installation 'VC\Auxiliary\Build\vcvarsall.bat'
}
$VcVarsPath = (Resolve-Path -LiteralPath $VcVarsPath).Path
$batch = Join-Path $PSScriptRoot 'build-native.cmd'
$out = Join-Path $repo 'build\x64'
New-Item -ItemType Directory -Force -Path $out | Out-Null
& $batch $VcVarsPath 2>&1 | Tee-Object -FilePath (Join-Path $out 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Build failed. See build/x64/build.log.' }
Write-Host "Build complete: $out"

