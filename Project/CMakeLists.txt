cmake_minimum_required(VERSION 3.20)

project(SlimeCore2D
    VERSION 0.1.0
    LANGUAGES C CXX
)

# ----------------------------------
# Global config
# ----------------------------------
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# ----------------------------------
# Platform defines
# ----------------------------------
if (WIN32)
    add_compile_definitions(SLIME_WINDOWS)
elseif (UNIX)
    add_compile_definitions(SLIME_LINUX)
endif()

# ----------------------------------
# FetchContent
# ----------------------------------
include(FetchContent)
set(FETCHCONTENT_QUIET FALSE)

# ----------------------------------
# OpenGL (system-level only)
# ----------------------------------
find_package(OpenGL REQUIRED)

# ----------------------------------
# GLFW
# ----------------------------------
FetchContent_Declare(
    glfw
    GIT_REPOSITORY https://github.com/glfw/glfw.git
    GIT_TAG 3.4
)
set(GLFW_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_DOCS OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(glfw)

# ----------------------------------
# GLEW
# ----------------------------------
FetchContent_Declare(
    glew
    GIT_REPOSITORY https://github.com/nigels-com/glew.git
    GIT_TAG glew-2.2.0
)
set(GLEW_BUILD_SHARED OFF CACHE BOOL "" FORCE)
set(GLEW_BUILD_STATIC ON CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(glew)

# ----------------------------------
# FreeType
# ----------------------------------
FetchContent_Declare(
    freetype
    GIT_REPOSITORY https://gitlab.freedesktop.org/freetype/freetype.git
    GIT_TAG VER-2-13-2
)
FetchContent_MakeAvailable(freetype)

# ----------------------------------
# GLM (header-only)
# ----------------------------------
FetchContent_Declare(
    glm
    GIT_REPOSITORY https://github.com/g-truc/glm.git
    GIT_TAG 1.0.1
)
FetchContent_MakeAvailable(glm)

# ----------------------------------
# stb (header-only)
# ----------------------------------
FetchContent_Declare(
    stb
    GIT_REPOSITORY https://github.com/nothings/stb.git
    GIT_TAG master
)
FetchContent_MakeAvailable(stb)

add_library(stb INTERFACE)
target_include_directories(stb INTERFACE
    ${stb_SOURCE_DIR}
)

# ----------------------------------
# Subprojects
# ----------------------------------
add_subdirectory(SlimeCore2D)
add_subdirectory(SlimeScriptingNative)
