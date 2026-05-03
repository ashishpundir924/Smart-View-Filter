# Smart View Filter

Smart View Filter is a standalone Revit add-in for reading a source selection, building a category/family/type scope tree, composing parameter rules, and applying, selecting, or temporarily isolating matching elements.

It uses a modeless Revit window with ExternalEvent execution, so users can leave the tool open, change the Revit selection, click `Read Selection`, and keep filtering.

Version: `2.0.0`

Supported Revit versions: `2022`, `2023`, `2026`

## Project Layout

- `SmartViewFilter.sln`
- `SmartViewFilter.Revit/`
- `Bundle/SmartViewFilter.bundle/`
- `SmartViewFilter.Installer/`
- `tools/Create-AutodeskPackage.ps1`

## Build

1. Install Autodesk Revit 2022, 2023, or 2026.
2. Open `SmartViewFilter.sln` in Visual Studio.
3. Build `SmartViewFilter.Revit` as `Release|x64`.

The project references:

- `C:\Program Files\Autodesk\Revit 2022\RevitAPI.dll`
- `C:\Program Files\Autodesk\Revit 2023\RevitAPI.dll`
- `C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll`

Build output is written to version-specific folders:

`build\Revit2022\`

`build\Revit2023\`

`build\Revit2026\`

## Run In Revit

For development testing, copy these files to a Revit add-ins folder or use the package script:

- `SmartViewFilter.Revit.dll`
- `SmartViewFilter.addin`

The add-in appears on:

`Smart Revit > Filters > Live Filter`

## Autodesk App Store Package

The App Store bundle scaffold is:

```text
SmartViewFilter.bundle/
  PackageContents.xml
  Contents/
    2022/
      SmartViewFilter.addin
      SmartViewFilter.Revit.dll
      README.txt
      icon.png
    2023/
      SmartViewFilter.addin
      SmartViewFilter.Revit.dll
      README.txt
      icon.png
    2026/
      SmartViewFilter.addin
      SmartViewFilter.Revit.dll
      README.txt
      icon.png
```

Run this from the repository root after building:

```powershell
.\tools\Create-AutodeskPackage.ps1
```

The script creates:

`dist\SmartViewFilter.bundle.zip`

To create a test installer EXE, run:

```powershell
.\tools\Create-Installer.ps1
```

The script creates:

`dist\SmartViewFilter.Installer.exe`

Before publishing, edit `Bundle\SmartViewFilter.bundle\PackageContents.xml` and replace `your-email@example.com` with your real support email. Also confirm the privacy policy URL is hosted and accessible.

## Local Install For Testing

Run this from the repository root:

```powershell
.\tools\Install-For-Revit.ps1 -RevitVersion 2023
```

Then restart Revit. The tool should appear under:

`Smart Revit > Filters > Live Filter`

## Version

- Version: `2.0.0`
- Target Revit versions: `2022`, `2023`, `2026`
- Platform: `Win64`

## Privacy

Smart View Filter works locally inside Revit. It does not collect, store, transmit, or track personal data. Saved filter configurations are stored locally in `%APPDATA%\SmartViewFilter\saved-configurations.json`.
