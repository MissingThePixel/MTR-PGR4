# pgr4_recompiled - ReXGlue Recompiled Project
#
# SDK boilerplate lives in generated/rexglue.cmake.

cmake_minimum_required(VERSION 3.25)
project(pgr4_recompiled LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Keep a user-supplied game path outside the committed manifest. Codegen may
# regenerate its integration file, so apply this override at configuration time.
set(MTR_GAME_MANIFEST "${CMAKE_CURRENT_SOURCE_DIR}/pgr4_recompiled_manifest.toml" CACHE FILEPATH "Local game manifest")
file(READ "${CMAKE_CURRENT_SOURCE_DIR}/generated/rexglue.cmake" MTR_SDK_INTEGRATION)
string(REPLACE "codegen \${CMAKE_CURRENT_SOURCE_DIR}/pgr4_recompiled_manifest.toml"
    "codegen \"${MTR_GAME_MANIFEST}\"" MTR_SDK_INTEGRATION "${MTR_SDK_INTEGRATION}")
file(WRITE "${CMAKE_CURRENT_BINARY_DIR}/mtr-rexglue.cmake" "${MTR_SDK_INTEGRATION}")
include("${CMAKE_CURRENT_BINARY_DIR}/mtr-rexglue.cmake")

# Sources
set(PGR4RECOMPILED_SOURCES
    src/main.cpp
    src/compat_math.cpp
    src/pgr4_patches.cpp
)

if(WIN32)
    add_executable(pgr4_recompiled WIN32 ${PGR4RECOMPILED_SOURCES})
else()
    add_executable(pgr4_recompiled ${PGR4RECOMPILED_SOURCES})
endif()

rexglue_setup_target(pgr4_recompiled)
if(MSVC)
    target_link_options(pgr4_recompiled PRIVATE "/MAP:${CMAKE_CURRENT_BINARY_DIR}/pgr4_recompiled.map")
endif()

# The stamp is an object dependency so Ninja recompiles patched generated code
# in the same build that runs codegen or changes this patch script.
set(PGR4_PATCH_STAMP "${CMAKE_CURRENT_BINARY_DIR}/pgr4_game_patches.stamp")
add_custom_command(OUTPUT "${PGR4_PATCH_STAMP}"
    COMMAND powershell -NoProfile -ExecutionPolicy Bypass -File
            "${CMAKE_CURRENT_SOURCE_DIR}/tools/apply_game_patches.ps1"
    COMMAND "${CMAKE_COMMAND}" -E touch "${PGR4_PATCH_STAMP}"
    WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}"
    DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/generated/default/codegen.build.stamp"
            "${CMAKE_CURRENT_SOURCE_DIR}/tools/apply_game_patches.ps1"
    VERBATIM
)
add_custom_target(pgr4_apply_game_patches DEPENDS "${PGR4_PATCH_STAMP}")
add_dependencies(pgr4_recompiled_recomp pgr4_apply_game_patches)
# Codegen may repartition functions. All generated objects depend on the patch
# stamp, ensuring patched units are rebuilt in the same invocation.
set_source_files_properties(${REXGLUE_ENTRYPOINT_GENERATED_SOURCES}
    PROPERTIES OBJECT_DEPENDS "${PGR4_PATCH_STAMP}")