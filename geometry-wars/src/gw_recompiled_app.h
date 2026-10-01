// gw_recompiled - ReXGlue Recompiled Project
//
// Customize your app by overriding virtual hooks from rex::ReXApp.

#pragma once

#include <rex/rex_app.h>
#include <rex/kernel/xam/module.h>
#include <rex/logging.h>
#include "../../src/title_handoff.h"

class GwRecompiledApp : public rex::ReXApp {
 public:
  using rex::ReXApp::ReXApp;

  static std::unique_ptr<rex::ui::WindowedApp> Create(
      rex::ui::WindowedAppContext& ctx) {
    return std::unique_ptr<GwRecompiledApp>(new GwRecompiledApp(ctx, "gw_recompiled",
        PPCImageConfig));
  }

  // Override virtual hooks for customization:
  // void OnPreSetup(rex::RuntimeConfig& config) override {}
  void OnLoadXexImage(std::string& xex_image) override {
    xex_image = "game:\\gw.xex";
  }
  void OnGuestThreadExit(rex::system::XThread*) override {
    auto xam = runtime()->kernel_state()->GetKernelModule<rex::kernel::xam::XamModule>("xam.xex");
    if (xam) {
      const auto& loader = xam->loader_data();
      REXLOG_INFO("Geometry Wars exit request: path='{}', flags={:#x}, launch_data_present={}, launch_data_bytes={}",
                  loader.launch_path, loader.launch_flags,
                  loader.launch_data_present, loader.launch_data.size());
    }
    title_handoff::Complete(runtime(), 2);
  }
  void OnPostLoadXexImage() override { title_handoff::Restore(runtime(), 2); }
  // void OnPostSetup() override {}
  // void OnCreateDialogs(rex::ui::ImGuiDrawer* drawer) override {}
  // std::unique_ptr<rex::ui::ImGuiDialog> CreateAchievementsOverlay() override;
  // std::unique_ptr<rex::ui::AchievementNotificationDialog>
  // CreateAchievementNotificationDialog() override;
  std::unique_ptr<rex::ui::AchievementNotificationDialog>
  CreateAchievementNotificationDialog() override { return nullptr; }
  // void OnShutdown() override {}
  // void OnConfigurePaths(rex::PathConfig& paths) override {}
};
