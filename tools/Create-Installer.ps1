param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$installerProject = Join-Path $root "SmartViewFilter.Installer\SmartViewFilter.Installer.csproj"
$installerOutput = Join-Path $root "build\Installer"
$dist = Join-Path $root "dist"

& (Join-Path $PSScriptRoot "Create-AutodeskPackage.ps1") -Configuration $Configuration

dotnet build $installerProject -c $Configuration -p:Platform=x64

New-Item -ItemType Directory -Force -Path $dist | Out-Null
Copy-Item (Join-Path $installerOutput "SmartViewFilter.Installer.exe") (Join-Path $dist "SmartViewFilter.Installer.exe") -Force

Write-Host "Created installer:"
Write-Host (Join-Path $dist "SmartViewFilter.Installer.exe")
