#pragma once
#include "Scripting/EngineExports.h"

// -----------------------------
// Resources (ResourceManager)
// -----------------------------
SLIME_EXPORT void* __cdecl Resources_LoadTexture(const char* name, const char* path);
SLIME_EXPORT void* __cdecl Resources_GetTexture(const char* name);
SLIME_EXPORT void* __cdecl Resources_LoadFont(const char* name, const char* path, int fontSize);
SLIME_EXPORT const char* __cdecl Resources_LoadText(const char* name, const char* path);

// Legacy wrappers
SLIME_EXPORT void* __cdecl Texture_Load(const char* path);
