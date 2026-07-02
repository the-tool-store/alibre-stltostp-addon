# Alibre STL to STEP Importer

An Alibre Design add-on that converts an STL mesh into a STEP (.stp) solid and imports the result into the active Alibre Design session.

The add-on adds an "STL Importer" ribbon menu with an "Import STL File" command. Picking an STL file runs the bundled `stltostp.exe` converter, then calls the Alibre automation API to import the generated STEP file. The project is a C# add-on targeting .NET Framework 4.8.1 (net481), built for x64, and references `AlibreAddOn.dll` and `AlibreX.dll` from an Alibre Design 29.0.0.29060 installation.

## Table Of Contents

- [What Is Here](#what-is-here)
- [Official Alibre Resources](#official-alibre-resources)
- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Installation](#installation)
- [Usage](#usage)
- [Key Files](#key-files)
- [Key Folders](#key-folders)
- [Notes](#notes)
- [License](#license)

## What Is Here

- A C# add-on (`source/AlibreAddOn.cs`) that registers an "STL Importer" ribbon menu and an "Import STL File" command.
- A file picker for selecting the source STL file.
- STL-to-STEP conversion through the bundled `stltostp.exe`.
- Automatic import of the generated STEP file into Alibre Design through the automation API (`ImportSTEPFile`).
- Dialog-based reporting of conversion success (including output file size) and converter errors.
- An add-on manifest (`.adc`) that Alibre Design loads at startup.

## Official Alibre Resources

Alibre's official resources for API development and AI/LLM/agent workflows: <https://www.alibre.com/api/>

## Requirements

- Alibre Design 29.0.0.29060. The project references `AlibreAddOn.dll` and `AlibreX.dll` from `C:\Program Files\Alibre Design 29.0.0.29060\Program\`.
- .NET Framework 4.8.1 (net481), built for the x64 platform.
- Windows.
- Visual Studio or the .NET SDK with MSBuild to build the solution.

## Quick Start

1. Build `source/alibre-stltostp.sln` in the Release configuration (the project targets x64).
2. Copy the build output to your Alibre add-ons directory, keeping `alibre-stltostp-addon.dll`, `stltostp.exe`, `logo.ico`, and `alibre-stltostp-addon.adc` together.
3. Restart Alibre Design.
4. Run STL Importer > Import STL File and select an STL file.

## Installation

1. Build `source/alibre-stltostp.sln` using the Release configuration. The project targets the x64 platform.
2. Copy the build output, including `alibre-stltostp-addon.dll`, `stltostp.exe`, and `logo.ico`, to your Alibre add-ons directory.
3. Keep `alibre-stltostp-addon.adc` alongside the DLL. Alibre Design reads this manifest at startup to load the add-on.
4. Restart Alibre Design.

## Usage

1. In Alibre Design, open the "STL Importer" menu.
2. Choose "Import STL File".
3. Select an STL file in the file dialog.
4. The add-on writes a STEP (.stp) file next to the source STL and imports it into the current session. A dialog reports the output path and file size, or the converter error if the run fails.

## Key Files

| File | Purpose |
| --- | --- |
| `source/AlibreAddOn.cs` | Add-on entry point: ribbon menu, STL file picker, converter invocation, and STEP import. |
| `source/alibre-stltostp-addon.adc` | Add-on manifest that Alibre loads at startup (identifier GUID, DLL, icon, menu). |
| `source/alibre-stltostp-addon.csproj` | Project file: net481, x64, Alibre and IronPython references. |
| `source/alibre-stltostp.sln` | Visual Studio solution. |
| `source/stltostp.exe` | Bundled STL-to-STEP converter (slugdev, BSD-4-Clause). |
| `source/logo.ico` | Add-on icon. |
| `source/Properties/launchSettings.json` | Debug launch settings. |
| `source/github-slugdev-stltostp-license.txt` | Third-party license notice for the bundled converter. |
| `source/alibre.disclaimer.txt` | Project disclaimer. |
| `LICENSE` | MIT license. |

## Key Folders

| Folder | Purpose |
| --- | --- |
| `source/` | C# add-on project, manifest, and the bundled converter. |
| `source/Properties/` | Project properties and launch settings. |
| `source/bin/` | Build output. |
| `source/obj/` | Build intermediates. |
| `.github/` | This README and community-health files. |
| `reviews/` | Dated code-review notes. |
| `documentation/` | Placeholder for documentation (holds only a `.gitkeep`). |
| `submodules/` | Placeholder for submodules (holds only a `.gitkeep`). |

## Notes

- The add-on launches `stltostp.exe` as an external process and reads its standard output and error. The DLL, the executable, and the `.adc` manifest must sit in the same folder for the converter to be found and the add-on to load.
- The bundled converter comes from slugdev (<https://github.com/slugdev/stltostp>) under the BSD-4-Clause license. See `source/github-slugdev-stltostp-license.txt`.
- The manifest loads the add-on at startup with identifier `{B4106059-92F8-41E1-8CF9-F56130C30068}`.
- The build pulls in the IronPython 3.4.2 and IronPython.StdLib 3.4.2 packages referenced in the project.

## License

See [LICENSE](../LICENSE).

Alibre, Alibre Design, and Alibre Script names and related materials belong to their respective owners.
