#pragma once

#include <rex/kernel/xam/module.h>
#include <rex/logging.h>
#include <Windows.h>
#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <vector>

// A fresh process is necessary: the two compiled titles have overlapping guest
// addresses and different function tables. The launcher brokers this small,
// bounded message; only these two original title paths are accepted.
namespace title_handoff {
inline std::filesystem::path EnvironmentPath(const wchar_t* name) {
  wchar_t value[32768];
  auto count = GetEnvironmentVariableW(name, value, 32768);
  return count && count < 32768 ? std::filesystem::path(value) : std::filesystem::path{};
}
inline void Restore(rex::Runtime* runtime, uint32_t current_title) {
  const auto path = EnvironmentPath(L"PGR4_HANDOFF_INPUT");
  if (path.empty()) return;
  std::ifstream file(path, std::ios::binary);
  uint32_t header[5]{};
  file.read(reinterpret_cast<char*>(header), sizeof(header));
  if (!file || header[0] != 0x48475250 || header[1] != current_title ||
      header[3] > 1 || header[4] > 3072) {
    REXLOG_ERROR("Invalid title handoff data");
    return;
  }
  std::vector<uint8_t> data(header[4]);
  file.read(reinterpret_cast<char*>(data.data()), data.size());
  if (!file || file.peek() != std::char_traits<char>::eof()) return;
  auto xam = runtime->kernel_state()->GetKernelModule<rex::kernel::xam::XamModule>("xam.xex");
  if (!xam) return;
  auto& loader = xam->loader_data();
  loader.launch_flags = header[2];
  loader.launch_data_present = header[3] != 0;
  loader.launch_data = std::move(data);
  REXLOG_INFO("Restored title launch data: {} bytes, flags={:#x}", loader.launch_data.size(), loader.launch_flags);
}
inline void Complete(rex::Runtime* runtime, uint32_t current_title) {
  auto xam = runtime->kernel_state()->GetKernelModule<rex::kernel::xam::XamModule>("xam.xex");
  if (!xam) return;
  const auto& loader = xam->loader_data();
  auto target = loader.launch_path;
  std::transform(target.begin(), target.end(), target.begin(), [](unsigned char c) { return char(std::tolower(c)); });
  uint32_t title = target == "game:\\default.xex" ? 1 : target == "game:\\gw.xex" ? 2 : 0;
  if (!title || title == current_title) return;
  REXLOG_INFO("Title handoff: '{}', flags={:#x}, launch data={} bytes", loader.launch_path, loader.launch_flags, loader.launch_data.size());
  const auto path = EnvironmentPath(L"PGR4_HANDOFF_OUTPUT");
  if (!path.empty() && loader.launch_data.size() <= 3072) {
    std::ofstream file(path, std::ios::binary | std::ios::trunc);
    uint32_t header[5] = {0x48475250, title, loader.launch_flags, uint32_t(loader.launch_data_present), uint32_t(loader.launch_data.size())};
    file.write(reinterpret_cast<const char*>(header), sizeof(header));
    file.write(reinterpret_cast<const char*>(loader.launch_data.data()), loader.launch_data.size());
    file.flush();
    if (!file) REXLOG_ERROR("Failed to save title handoff");
  }
  // Match the SDK's window-close path. TerminateTitle can leave guest worker
  // threads running; destroying the runtime while they still use it is unsafe.
  // Saves have already been handled by the guest before its launch request.
  rex::FlushLogging();
  std::_Exit(0);
}
}
