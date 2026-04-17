# alibre-stltostp-addon

> Note: This repository is undergoing significant changes and is currently a work in progress.

| Item | Value |
| --- | --- |
| Type | Alibre add-on / plugin |
| Primary stack | C#, Alibre automation |

## Overview
This repository contains the source and supporting assets for alibre-stltostp-addon, organized under the standardized repository layout.

## Repository Layout
- source/: project source, solution or project files, and runtime assets.
- submodules/: external git submodules used by the repository when required.
- documentation/: supplementary notes, changelogs, and non-GitHub documentation.
- .github/: repository README, templates, and GitHub-specific community files.
- `source/alibre-stltostp-addon.adc`: key source or build entry point.
- `source/alibre-stltostp-addon.csproj`: key source or build entry point.
- `source/alibre-stltostp.sln`: key source or build entry point.
- `LICENSE`: repository license file kept at the root.

## Requirements
- Windows development environment.
- A .NET build environment compatible with the projects under source/.
- Alibre Design installed if you need to run, debug, or validate the Alibre integration.

## Build and Use
1. Open `source/alibre-stltostp.sln` in your preferred IDE.
2. Restore dependencies and build from the source/ layout.
3. Use the notes in documentation/ and .github/README.md as the primary repository guide.

## Current Limitations
- The repository has been normalized for layout consistency; any path-sensitive tooling should be revalidated against the new folder structure.
- Existing runtime behavior and project-specific limitations remain unchanged.

## License
See [LICENSE](../LICENSE).

