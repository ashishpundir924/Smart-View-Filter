param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "SmartViewFilter.Revit\SmartViewFilter.Revit.csproj"
$sourceBundle = Join-Path $root "Bundle\SmartViewFilter.bundle"
$dist = Join-Path $root "dist"
$workingBundle = Join-Path $dist "SmartViewFilter.bundle"
$zipPath = Join-Path $dist "SmartViewFilter.bundle.zip"
$versions = @("2022", "2023", "2026")

if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $workingBundle | Out-Null
Copy-Item (Join-Path $sourceBundle "PackageContents.xml") (Join-Path $workingBundle "PackageContents.xml")

foreach ($version in $versions) {
    dotnet build $project -c $Configuration -p:Platform=x64 -p:RevitVersion=$version

    $projectOutput = Join-Path $root "build\Revit$version"
    $contents = Join-Path $workingBundle "Contents\$version"
    New-Item -ItemType Directory -Force -Path $contents | Out-Null

    Copy-Item (Join-Path $sourceBundle "Contents\2023\SmartViewFilter.addin") (Join-Path $contents "SmartViewFilter.addin")
    Copy-Item (Join-Path $sourceBundle "Contents\2023\README.txt") (Join-Path $contents "README.txt")
    Copy-Item (Join-Path $sourceBundle "Contents\2023\PRIVACY_POLICY.txt") (Join-Path $contents "PRIVACY_POLICY.txt")
    Copy-Item (Join-Path $sourceBundle "Contents\2023\icon.png") (Join-Path $contents "icon.png")
    Copy-Item (Join-Path $projectOutput "SmartViewFilter.Revit.dll") (Join-Path $contents "SmartViewFilter.Revit.dll")
}

Compress-Archive -Path $workingBundle -DestinationPath $zipPath -Force

Write-Host "Created Autodesk package:"
Write-Host $zipPath
