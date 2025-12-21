file(GLOB_RECURSE SLIME_ENGINE_SRC
    Engine/*.cpp
    Game/*.cpp
)

add_library(SlimeCore2D SHARED ${SLIME_ENGINE_SRC})

target_include_directories(SlimeCore2D PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)

target_link_libraries(SlimeCore2D PRIVATE
    OpenGL::GL
    glfw
    libglew_static
    freetype
    glm::glm
    stb
)

if (UNIX AND NOT APPLE)
    target_link_libraries(SlimeCore2D PRIVATE
        dl
        pthread
        X11
    )
endif()
