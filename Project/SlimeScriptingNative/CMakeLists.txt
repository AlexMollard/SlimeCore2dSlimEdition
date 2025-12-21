file(GLOB SLIME_SCRIPTING_SRC *.cpp)

add_library(SlimeScriptingNative SHARED ${SLIME_SCRIPTING_SRC})

target_include_directories(SlimeScriptingNative PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)

if (WIN32)
    target_compile_definitions(SlimeScriptingNative PRIVATE SLIME_EXPORT)
else()
    target_compile_options(SlimeScriptingNative PRIVATE -fvisibility=hidden)
    target_link_libraries(SlimeScriptingNative PRIVATE dl pthread)
endif()
