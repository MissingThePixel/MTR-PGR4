# Building MTR-PGR4 on Windows

## Requirements

- Windows x64 and Visual Studio's **Desktop development with C++** workload,
  including the Windows SDK and MSVC libraries.
- Clang/clang-cl 20 or newer, CMake 3.25+, Ninja and Git on PATH.
- .NET Framework 4.8, including the Windows Framework C# compiler.
- Your own complete extracted PGR4 game, including `default.xex` and `gw.xex`.

Run the commands in a **Developer PowerShell for Visual Studio**, with the x64
compiler environment selected. The official [ReXGlue getting-started guide](https://github.com/rexglue/rexglue-sdk/wiki/Getting-Started)
describes the compiler workload and SDK prerequisites.

## 1. Obtain and build the patched SDK

From this repository's root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\setup-sdk.ps1
```

This clones ReXGlue **v0.10.0** at the recorded commit, initializes its pinned
dependencies, applies the runtime/recompiler patch and Windows libmspack patch,
copies the additional source files, and builds a Release SDK into `sdk-install`.
The patch is essential: an unmodified SDK omits the audio, animation and renderer
fixes used by this project. Use a dedicated SDK clone to avoid changing another
project's installation. `-SdkDirectory` can select an alternate clone location.

## 2. Generate and compile both titles

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1 -GameDataRoot "D:\Games\PGR4 extracted"
```

The script generates game C++ locally, applies the maintained game edits and
builds PGR4, Geometry Wars and the launcher. Local manifests containing your path
are ignored by Git. Generated game C++ is not included in this repository.

If using an already built **patched** SDK, add `-SdkPrefix "D:\SDKs\rexglue-install"`.
`-Jobs` controls the number of parallel compiler jobs (default 4).

Outputs:

- `out/pgr4/pgr4_recompiled.exe`
- `out/geometry-wars/gw_recompiled.exe`
- `out/launcher/MTR-PGR4.exe`

The original internal target names are kept for compatibility with the manifests
and generated code. The user-facing project and launcher are **MTR-PGR4**.

## 3. Make a clean player release

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1
```

The packager uses an explicit file list, includes required license notices and
creates a ZIP under `dist`. It never copies extracted game files, generated game
source, your launcher settings, saves, caches, archives, captures or logs.
Players open `MTR-PGR4.exe` and select their own extracted game folder.

## Maintaining fixes

- `tools/apply_game_patches.ps1`: persistent edits to generated game functions.
- `src/pgr4_patches.cpp`: guarded 60 FPS, garage walking and other game helpers.
- `src/title_handoff.h`: launch-data transfer between the two compiled titles.
- `patches` and `sdk-overrides`: the SDK modifications required by both titles.
- `launcher`: the graphical launcher and process broker.

Preserve the pinned SDK version when regenerating code: changing its partitioning
or the game executable version can invalidate the function-level patch matches.
Do not check local manifests or generated game instructions into the repository.
