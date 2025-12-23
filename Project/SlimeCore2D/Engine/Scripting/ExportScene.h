#pragma once
#include "Scripting/EngineExports.h"

// -----------------------------
// Scene Wrappers
// -----------------------------
SLIME_EXPORT EntityId __cdecl Scene_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT EntityId __cdecl Scene_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT EntityId __cdecl Scene_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId);
SLIME_EXPORT void __cdecl Scene_Destroy(EntityId id);
SLIME_EXPORT bool __cdecl Scene_IsAlive(EntityId id);
SLIME_EXPORT int __cdecl Scene_GetEntityCount();
SLIME_EXPORT EntityId __cdecl Scene_GetEntityIdAtIndex(int index);
