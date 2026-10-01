# Working on MTR-PGR4

- Read README.md, BUILDING.md and THIRD_PARTY.md before changing build or release code.
- Keep original game files outside version control. Never distribute XEX/ISO
  files, extracted assets, saves, captures, personal paths or diagnostic logs.
- Build the pinned, patched ReXGlue SDK before generating game C++.
- Generated game code is disposable. Persist game changes in
  tools/apply_game_patches.ps1 and the maintained source helpers instead.
- Keep the 30 FPS path intact. Garage walking needs a collision step for short
  60 FPS moves; do not bypass collision checks or change global physics timing.
- PGR4 and Geometry Wars have overlapping guest addresses and separate function
  tables. Use the existing process handoff; preserve launch data and wait for
  one process to exit before starting the next.
- Preserve menu XMA split-frame decoding, music notifications and automatic next
  song behavior when modifying audio. Optional tracing is off by default.
- Launcher choices for resolution, FPS, fullscreen and blur are independent.
  Preserve arbitrary game-folder paths, including spaces, and Quiver's ability
  to start the launcher from another working directory.
- Verify the specific risk changed. For gameplay-dependent changes, build and
  check startup first, then request one focused controller playtest.
- Release from an explicit file list. Inspect the ZIP for excluded data and
  retain required third-party license notices. Do not package generated game
  source as authored project source.
