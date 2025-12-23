#pragma once
#include "Scripting/EngineExports.h"

// -----------------------------
// Text / Fonts
// -----------------------------
SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path);
SLIME_EXPORT void __cdecl Font_Free(void* font);
