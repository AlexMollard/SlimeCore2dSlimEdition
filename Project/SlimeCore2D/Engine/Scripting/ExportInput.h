#pragma once
#include "Scripting/EngineExports.h"

// -----------------------------
// Input
// -----------------------------
SLIME_EXPORT bool __cdecl Input_GetKeyDown(int key);
SLIME_EXPORT bool __cdecl Input_GetKeyReleased(int key);
SLIME_EXPORT void __cdecl Input_GetMousePos(float* outX, float* outY);
SLIME_EXPORT bool __cdecl Input_GetMouseDown(int button);
SLIME_EXPORT void __cdecl Input_GetMouseToWorldPos(float* outX, float* outY);
SLIME_EXPORT void __cdecl Input_SetViewportRect(int x, int y, int width, int height);
SLIME_EXPORT void __cdecl Input_GetViewportRect(int* outX, int* outY, int* outW, int* outH);
SLIME_EXPORT void __cdecl Input_SetScroll(float v, float h);
SLIME_EXPORT float __cdecl Input_GetScroll();
