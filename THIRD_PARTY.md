# Upstream code and credits

## PGR4-Recomp lineage

This project began from [beatrixzy/PGR4-Recomp](https://github.com/beatrixzy/PGR4-Recomp),
at commit `464cc5b62527755d6a365b36a0e40ff843d722e9`.

- The initial PGR4 manifest, discovered function addresses and project setup came
  from that project. These addresses and build metadata are retained and credited.
- Its minimal `src/main.cpp`, app class and CMake scaffolding are generated
  ReXGlue templates. Those templates can be independently obtained from the
  licensed ReXGlue SDK's `resources/templates/init` directory. The app lifecycle
  hooks and build changes here extend that SDK scaffolding.
- Upstream's README credits **Tera / Creesic** for the earlier recompilation work.
  That credit is retained here. Thanks to beatrixzy for publishing the starting
  project and making these improvements possible.
- The inspected PGR4-Recomp snapshot has no explicit license file. Its README's
  credits are not represented as a license grant, and MTR-PGR4's MIT license does
  not relicense any independently authored upstream material.

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
  [Xenia Canary](https://github.com/xenia-canary/xenia-canary): comparison reference
  for music service behavior, renderer behavior and PowerPC instruction handling.
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
