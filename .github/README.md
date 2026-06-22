# Alibre STL to STEP Importer

An Alibre Design add-on that converts an STL mesh file to a STEP (.stp) solid and imports the result directly into the active Alibre Design session.

## Features

- Adds an "STL Importer" ribbon menu with an "Import STL File" command.
- File picker for selecting the source STL file.
- Converts the selected STL mesh to a STEP (.stp) solid using the bundled `stltostp.exe` converter.
- Automatically imports the generated STEP file into Alibre Design via the automation API.
- Reports conversion success (including output file size) and surfaces converter errors in dialog messages.

## Requirements

- Alibre Design 29.0.0.29060 (the project references `AlibreAddOn.dll` and `AlibreX.dll` from this installation).
- .NET Framework 4.8.1, built for the x64 platform.
- Windows.

## Installation

1. Build the `source/alibre-stltostp.sln` solution using the Release | Any CPU configuration (the project targets the x64 platform).
2. Copy the build output, including `alibre-stltostp-addon.dll`, `stltostp.exe`, and `logo.ico`, to your Alibre add-ons directory.
3. Ensure the `alibre-stltostp-addon.adc` add-on manifest is present alongside the DLL. Alibre Design loads the add-on at startup based on this manifest.
4. Restart Alibre Design.

## Usage

1. In Alibre Design, open the "STL Importer" menu.
2. Choose "Import STL File".
3. Select an STL file in the file dialog.
4. The add-on converts the file to a STEP (.stp) file next to the source and imports it into the current session.

## License
See [LICENSE](../LICENSE).
