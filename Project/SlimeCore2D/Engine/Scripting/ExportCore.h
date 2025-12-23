#pragma once
#include "Scripting/EngineExports.h"

// -----------------------------
// Core / Logging
// -----------------------------
SLIME_EXPORT void __cdecl Engine_Log(const char* msg);
SLIME_EXPORT void __cdecl Engine_LogTrace(const char* msg);
SLIME_EXPORT void __cdecl Engine_LogInfo(const char* msg);
SLIME_EXPORT void __cdecl Engine_LogWarn(const char* msg);
SLIME_EXPORT void __cdecl Engine_LogError(const char* msg);
