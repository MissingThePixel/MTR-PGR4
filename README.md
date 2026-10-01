# MTR-PGR4

**MissingTheRecompilation: Project Gotham Racing 4**

A Windows PC recompilation of Project Gotham Racing 4, with a graphical launcher,
720p/1440p rendering, 30/60 FPS options, optional motion blur, and the garage's
Geometry Wars game. Built with [ReXGlue](https://github.com/rexglue/rexglue-sdk),
following the work of [beatrixzy's PGR4-Recomp](https://github.com/beatrixzy/PGR4-Recomp).

## Getting started

1. Download the Windows x64 ZIP from [Releases](https://github.com/MissingThePixel/MTR-PGR4/releases)
   and extract the whole ZIP to a writable folder.
2. Extract your own PGR4 disc image as described below. No game files are included.
3. Open **MTR-PGR4.exe**, choose the extracted game folder with **Browse**, and pick
   your resolution, frame rate, fullscreen and motion blur settings.
4. Click **Play PGR4**. Settings are remembered. Geometry Wars can be launched from
   the launcher or the in-game garage; its Exit to PGR4 option is supported.

The release includes the runtime and renderer: you do **not** need to install
ReXGlue to play. Windows x64, a Direct3D 12-capable graphics card and .NET Framework
4.8 are required. A controller is recommended, particularly for garage walking.

## Extracting your ISO

The tool used for this project is **XBOX360 ISO Extract 0.6 by somski**. Its
[release thread and download](https://www.realmodscene.com/index.php?/topic/31-xbox360-iso-extract-version-06-with-ftp-support/)
and the [ConsoleMods extraction guide](https://consolemods.org/wiki/Xbox:ISO_Extraction_%26_Repacking#XBOX360_ISO_Extract_by_somski)
describe the same utility (`XBOX360 ISO Extract.exe`).

1. Open the extractor and set the source directory containing your PGR4 ISO.
2. Set a separate destination directory for the extracted files. Keep deletion of
   the original ISO disabled if you want to retain your backup.
3. Rescan, select PGR4 in the extraction list, and click **Go**. Wait for completion.
4. In MTR-PGR4, browse to the resulting game folder—not the ISO itself or its
   parent folder. That folder should contain `default.xex`, `Game`, `UI`, and the
   other extracted directories. Geometry Wars also needs `gw.xex` and `gw`.

Use your own legally obtained game dump. Preserve the complete extraction; moving
only the XEX files is insufficient. The game folder can be on another drive.

## Known bugs

- Vibration and engine noise can disappear at high RPM ranges.
- Small frame-rate hitches remain in certain maps.
- Some shadows have minor visual problems, especially in car interiors.
- Liveries sometimes differ from their menu previews. The left side can show a
  small corrupted area near the front wheel arch.

## Saves and settings

Your saves are created locally; the release contains no pre-existing saves.
PGR4 uses `runtime/userdata`, Geometry Wars uses `runtime/geometry-wars/userdata`,
and launcher settings are stored in `runtime/launcher-settings.json`.
Keep these when updating an existing installation.

## Source and credits

See [BUILDING.md](BUILDING.md) to build both games and the launcher. Developers
need their own extracted PGR4 files and ReXGlue v0.10.0 with the included changes;
the build guide explains how to obtain and apply them.

[THIRD_PARTY.md](THIRD_PARTY.md) records upstream code provenance and credits,
including beatrixzy, Tera/Creesic, ReXGlue, Xenia and the graphics/audio libraries.
New MTR-PGR4 code is available under the MIT license. Third-party components keep
their own licenses; these permissions do not cover the original game's assets.

Project Gotham Racing 4 and its artwork belong to their respective owners.
This is an independent community project.
