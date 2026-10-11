# Upstream code and credits

## PGR4-Recomp lineage

This project began from [beatrixzy/PGR4-Recomp](https://github.com/beatrixzy/PGR4-Recomp),
at commit `464cc5b62527755d6a365b36a0e40ff843d722e9`. MTR-PGR4 is maintained
independently and is not affiliated with or endorsed by PGR4-Recomp.

MTR-PGR4 began from beatrixzy's PGR4-Recomp project. It retains the initial game
configuration, recorded function locations and build setup. This groundwork
helps ReXGlue turn the original Xbox 360 executable into a PC build. Credit for
this foundational work belongs to beatrixzy and the earlier contributors.
MTR-PGR4 builds on it with additional game fixes, runtime improvements and its
own launcher.

- Its initial `src/main.cpp`, app class and CMake scaffolding were based on
  ReXGlue's templates, available in the SDK's `resources/templates/init` directory.
  These files were inherited through PGR4-Recomp, and that contribution is credited
  here. MTR-PGR4 extends the application hooks and build setup.
- Upstream's README credits **Tera / Creesic** for the earlier recompilation work.
  That credit is retained here. Thanks to beatrixzy for publishing the starting
  project and making these improvements possible.
- PGR4-Recomp is now published under the [MIT license](https://github.com/beatrixzy/PGR4-Recomp/blob/main/LICENSE),
  copyright (c) 2026 Bea. Its copyright and permission notice are retained in
  `LICENSES/PGR4-Recomp-MIT.txt`. Upstream material remains credited to its authors.

Upstream documentation, repository history, generated game C++, executables,
game files and local testing material are not copied into this source repository.

## ReXGlue and Xenia

- [ReXGlue SDK](https://github.com/rexglue/rexglue-sdk), v0.10.0, commit
  `f5337cdc947ff6d4c4196737e2c807a48f2a1fc2`: runtime, recompiler, application
  templates and generated CMake integration. BSD 3-Clause; copyright Tom Clay,
  with portions derived from Ben Vanik and Xenia contributors.
- SDK changes are distributed as `patches/rexglue-v0.10.0.patch` plus the new
  source files in `sdk-overrides`. Original copyright headers are preserved.
- [Xenia](https://github.com/xenia-project/xenia) and
  [Xenia Canary](https://github.com/xenia-canary/xenia-canary): ReXGlue's graphics
  renderer is derived from Xenia. This project uses that renderer with modifications,
  so inherited Xenia graphics problems are likely to appear here too. Xenia also
  served as a comparison reference for music service behavior, renderer behavior
  and PowerPC instruction handling.
- The 60 FPS and motion-blur game edits follow the addresses/instruction changes
  in the [Xenia Canary PGR4 patches](https://github.com/xenia-canary/game-patches/blob/main/patches/4D5307F9%20-%20Project%20Gotham%20Racing%204.patch.toml).

Players receive the compiled runtime and do not need a separate SDK installation.
Developers clone the pinned SDK and apply the included changes; see BUILDING.md.

## Bundled runtime dependencies

- [D3D12 Memory Allocator](https://github.com/GPUOpen-LibrariesAndSDKs/D3D12MemoryAllocator),
  commit `1d86c1130f61453634b1df85782e1fecfd59a525`: shared texture allocation;
  MIT license. Vendored source is in `sdk-overrides/thirdparty/d3d12ma`.
- [FFmpeg](https://github.com/wmarti/FFmpeg), the SDK's pinned fork: game audio
  decoding; LGPL. This build disables GPL and nonfree FFmpeg features.
- libmspack: XEX decompression; LGPL. The Windows symlink materialization patch
  is included in `patches/libmspack-windows.patch`.
- SDL, fmt, spdlog, Dear ImGui, SIMDe, xxHash and the SDK's other graphics,
  compression and utility components retain their original notices in LICENSES.

The SDK pins dependency commits through its Git submodules. The setup script
fetches those matching sources. The modified SDK can be rebuilt and its runtime
DLL replaced; MTR-PGR4 imposes no restriction on modifying those dependencies or
debugging such modifications. License texts are supplied with source and binaries.

## Extraction utility and artwork

**XBOX360 ISO Extract** is by **somski** and is not bundled. The README links to
its release thread. xextool is credited by the original PGR4-Recomp project and
is not bundled here.

The launcher icon uses supplied Project Gotham Racing 4 artwork. Original game
artwork and trademarks remain the property of their respective owners; the MIT
license for new code does not grant rights to them or to the original game.
