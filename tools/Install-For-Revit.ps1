param(
    [ValidateSet("2022", "2023", "2026")]
    [string]$RevitVersion = "2023",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "SmartViewFilter.Revit\SmartViewFilter.Revit.csproj"
$projectOutput = Join-Path $root "build\Revit$RevitVersion"
$addinsRoot = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$installFolder = Join-Path $addinsRoot "SmartViewFilter"
$manifestPath = Join-Path $addinsRoot "SmartViewFilter.addin"
$dllPath = Join-Path $installFolder "SmartViewFilter.Revit.dll"

dotnet build $project -c $Configuration -p:Platform=x64 -p:RevitVersion=$RevitVersion

New-Item -ItemType Directory -Force -Path $installFolder | Out-Null
Copy-Item (Join-Path $projectOutput "SmartViewFilter.Revit.dll") $dllPath -Force

$manifest = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Smart View Filter</Name>
    <Assembly>$dllPath</Assembly>
    <AddInId>8D83C886-B739-4ACD-A9DB-3D1E3B6D1F11</AddInId>
    <FullClassName>SmartViewFilter.Revit.App</FullClassName>
    <VendorId>ASHP</VendorId>
    <VendorDescription>CAD Automation by Ashish</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8

Write-Host "Installed Smart View Filter for Revit $RevitVersion."
Write-Host "Manifest: $manifestPath"
Write-Host "Assembly: $dllPath"
