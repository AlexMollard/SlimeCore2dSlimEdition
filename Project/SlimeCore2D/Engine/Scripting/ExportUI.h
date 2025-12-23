#pragma once
#include "Scripting/EngineExports.h"

// -----------------------------
// UI System
// -----------------------------
SLIME_EXPORT EntityId __cdecl UI_CreateText(const char* text, int fontSize, float x, float y);
SLIME_EXPORT void __cdecl UI_Destroy(EntityId id);
SLIME_EXPORT void __cdecl UI_SetText(EntityId id, const char* text);
SLIME_EXPORT void __cdecl UI_SetPosition(EntityId id, float x, float y);
SLIME_EXPORT void __cdecl UI_GetPosition(EntityId id, float* outX, float* outY);
SLIME_EXPORT void __cdecl UI_SetAnchor(EntityId id, float ax, float ay);
SLIME_EXPORT void __cdecl UI_SetColor(EntityId id, float r, float g, float b);
SLIME_EXPORT void __cdecl UI_SetVisible(EntityId id, bool visible);
SLIME_EXPORT void __cdecl UI_SetLayer(EntityId id, int layer);
SLIME_EXPORT void __cdecl UI_SetUseScreenSpace(EntityId id, bool useScreenSpace);
SLIME_EXPORT void __cdecl UI_GetTextSize(EntityId id, float* outWidth, float* outHeight);
SLIME_EXPORT float __cdecl UI_GetTextWidth(EntityId id);
SLIME_EXPORT float __cdecl UI_GetTextHeight(EntityId id);
