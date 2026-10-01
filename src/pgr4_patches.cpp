#include <rex/cvar.h>
#include <rex/logging.h>

#include <atomic>
#include <chrono>
#include <cstring>
#include <thread>

// These switches are consumed by the two guarded edits to generated game code.
REXCVAR_DEFINE_BOOL(pgr4_60fps, false, "PGR4", "Use the game's 60 FPS frame pacing path")
    .lifecycle(rex::cvar::Lifecycle::kRequiresRestart);

REXCVAR_DEFINE_BOOL(pgr4_disable_hdr, false, "PGR4",
                    "Disable the game's HDR path to reduce upscale flicker")
    .lifecycle(rex::cvar::Lifecycle::kRequiresRestart);

REXCVAR_DEFINE_BOOL(pgr4_disable_motion_blur, false, "PGR4",
                    "Disable the game's motion blur effect")
    .lifecycle(rex::cvar::Lifecycle::kRequiresRestart);

REXCVAR_DEFINE_BOOL(pgr4_frame_wait_yield, false, "PGR4",
                    "Experiment: yield during the game's busy frame wait")
    .lifecycle(rex::cvar::Lifecycle::kRequiresRestart);

bool Pgr4Enable60Fps() {
  const bool enabled = REXCVAR_GET(pgr4_60fps);
  static std::atomic_bool logged = false;
  if (enabled && !logged.exchange(true)) REXLOG_INFO("PGR4 60 FPS patch active");
  return enabled;
}

bool Pgr4DisableHdr() {
  const bool enabled = REXCVAR_GET(pgr4_disable_hdr);
  static std::atomic_bool logged = false;
  if (enabled && !logged.exchange(true)) REXLOG_INFO("PGR4 HDR disabled for upscaling");
  return enabled;
}

bool Pgr4DisableMotionBlur() {
  const bool enabled = REXCVAR_GET(pgr4_disable_motion_blur);
  static std::atomic_bool logged = false;
  if (enabled && !logged.exchange(true)) REXLOG_INFO("PGR4 motion blur disabled");
  return enabled;
}

void Pgr4FrameWaitYield() {
  if (!REXCVAR_GET(pgr4_frame_wait_yield)) return;
  thread_local uint32_t wait_iterations = 0;
  // The guest's db16cyc delay instructions are discarded by the recompiler.
  // Let other ready threads run occasionally while this frame wait polls.
  if (!(++wait_iterations & 63)) std::this_thread::yield();
}

REXCVAR_DEFINE_BOOL(pgr4_trace_garage_walk, false, "PGR4",
                    "Diagnostic: sample walking camera movement before and after collision");

REXCVAR_DEFINE_BOOL(pgr4_garage_walk_fix, false, "PGR4",
                    "Keep a collision step for short 60 FPS garage walking moves");

extern "C" int32_t Pgr4GarageCollisionSteps(int32_t steps, uint32_t caller) {
  // The walking caller already rejects effectively zero movement. Its
  // collision callback truncates distance * 20, discarding sub-5cm moves.
  // Only fix that exact call path; preserve other collision users and 30 FPS.
  if (steps != 0 || caller != 0x827A7510 ||
      !REXCVAR_GET(pgr4_garage_walk_fix) || !REXCVAR_GET(pgr4_60fps)) return steps;
  static std::atomic_bool logged = false;
  if (!logged.exchange(true)) REXLOG_INFO("PGR4 garage walking: minimum collision step active");
  return 1;
}

extern "C" void Pgr4TraceGarageWalk(uint8_t* base, uint32_t camera, uint32_t phase,
                         uint32_t original, uint32_t delta, uint32_t result) {
  if (!REXCVAR_GET(pgr4_trace_garage_walk)) return;
  struct Sample {
    std::chrono::steady_clock::time_point next{};
    bool active = false;
    float before[3]{}, requested[3]{}, dt = 0, ground = 0;
    uint32_t callback = 0, camera = 0;
  };
  thread_local Sample sample;
  auto read_float = [base](uint32_t address) {
    uint32_t bits;
    std::memcpy(&bits, base + address, sizeof(bits));
    bits = __builtin_bswap32(bits);
    float value;
    std::memcpy(&value, &bits, sizeof(value));
    return value;
  };
  if (phase == 0) {
    auto now = std::chrono::steady_clock::now();
    sample.active = now >= sample.next;
    if (!sample.active) return;
    sample.next = now + std::chrono::milliseconds(250);
    sample.camera = camera;
    sample.dt = read_float(camera + 280);
    sample.ground = read_float(camera + 652);
    std::memcpy(&sample.callback, base + camera + 640, sizeof(sample.callback));
    sample.callback = __builtin_bswap32(sample.callback);
    for (uint32_t i = 0; i < 3; ++i) {
      sample.before[i] = read_float(original + i * 4);
      sample.requested[i] = read_float(delta + i * 4);
    }
  } else if (sample.active && sample.camera == camera) {
    sample.active = false;
    REXLOG_INFO("PGR4 walk: camera={:08X} callback={:08X} dt={:.6f} ground_offset={:.6f} before=({:.6f},{:.6f},{:.6f}) requested=({:.6f},{:.6f},{:.6f}) after=({:.6f},{:.6f},{:.6f})",
                camera, sample.callback, sample.dt, sample.ground,
                sample.before[0], sample.before[1], sample.before[2],
                sample.requested[0], sample.requested[1], sample.requested[2],
                read_float(result), read_float(result + 4), read_float(result + 8));
  }
}

void Pgr4LogCarPreviewChainLimit(uint32_t node, uint32_t cell) {
  static std::atomic_bool logged = false;
  if (!logged.exchange(true)) {
    REXLOG_WARN("PGR4 car preview visibility list exceeded 4096 nodes (node={:#x}, cell={:#x}); skipping the remaining entries", node, cell);
  }
}

void Pgr4LogCarPreviewCellLimit(uint32_t row, uint32_t column) {
  static std::atomic_bool logged = false;
  if (!logged.exchange(true)) {
    REXLOG_WARN("PGR4 car preview visibility search exceeded 1024 cells (row={}, column={}); ending this search", row, column);
  }
}

extern "C" __declspec(noinline) void Pgr4TraceBoneStore(uint32_t address, uint32_t value) {
  static std::atomic_uint32_t count{0};
  if (count.fetch_add(1, std::memory_order_relaxed) < 80) {
    REXLOG_INFO("PGR4 bone store: address {:08X} value {:08X} caller {:016X}",
                address, value, uint64_t(reinterpret_cast<uintptr_t>(__builtin_return_address(0))));
  }
}
