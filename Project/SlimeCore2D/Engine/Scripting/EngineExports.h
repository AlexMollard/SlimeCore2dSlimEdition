#pragma once
#include <cstdint>

#if defined(_WIN32)
#	define SLIME_EXPORT extern "C" __declspec(dllexport)
#else
#	define SLIME_EXPORT extern "C"
#endif

using EntityId = std::uint64_t;

// -----------------------------
// Core / Logging
// -----------------------------
SLIME_EXPORT void __cdecl Engine_Log(const char* msg);

// -----------------------------
// Entity lifecycle (Create/Destroy/IsAlive)
// -----------------------------
SLIME_EXPORT EntityId __cdecl Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT void __cdecl Entity_Destroy(EntityId id);
SLIME_EXPORT bool __cdecl Entity_IsAlive(EntityId id);

// -----------------------------
// Entity transform & visual API
// (position, size, color, layer, anchor)
// -----------------------------
SLIME_EXPORT void __cdecl Entity_SetPosition(EntityId id, float x, float y);
SLIME_EXPORT void __cdecl Entity_GetPosition(EntityId id, float* outX, float* outY);
SLIME_EXPORT void __cdecl Entity_SetSize(EntityId id, float sx, float sy);
SLIME_EXPORT void __cdecl Entity_GetSize(EntityId id, float* outSx, float* outSy);
SLIME_EXPORT void __cdecl Entity_SetColor(EntityId id, float r, float g, float b);
SLIME_EXPORT void __cdecl Entity_SetLayer(EntityId id, int layer);
SLIME_EXPORT int  __cdecl Entity_GetLayer(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetAnchor(EntityId id, float ax, float ay);
SLIME_EXPORT void __cdecl Entity_GetAnchor(EntityId id, float* outAx, float* outAy);

// -----------------------------
// Entity visual helpers (texture / visibility / animation)
// -----------------------------
SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height);
SLIME_EXPORT void __cdecl Entity_SetRender(EntityId id, bool value);
SLIME_EXPORT bool __cdecl Entity_GetRender(EntityId id);

SLIME_EXPORT void __cdecl Entity_SetFrame(EntityId id, int frame);
SLIME_EXPORT int  __cdecl Entity_GetFrame(EntityId id);
SLIME_EXPORT void __cdecl Entity_AdvanceFrame(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetSpriteWidth(EntityId id, int width);
SLIME_EXPORT int  __cdecl Entity_GetSpriteWidth(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetHasAnimation(EntityId id, bool value);
SLIME_EXPORT void __cdecl Entity_SetFrameRate(EntityId id, float frameRate);
SLIME_EXPORT float __cdecl Entity_GetFrameRate(EntityId id);

// -----------------------------
// Input: keyboard / mouse / window
// -----------------------------
SLIME_EXPORT bool __cdecl Input_GetKeyDown(int key);
SLIME_EXPORT bool __cdecl Input_GetKeyReleased(int key);

// Mouse & window input
SLIME_EXPORT void __cdecl Input_GetMousePos(float* outX, float* outY);
SLIME_EXPORT void __cdecl Input_GetMouseDelta(float* outX, float* outY);
SLIME_EXPORT bool __cdecl Input_GetMouseDown(int button);
SLIME_EXPORT void __cdecl Input_GetMouseToWorldPos(float* outX, float* outY);

// Window and viewport
SLIME_EXPORT void __cdecl Input_GetWindowSize(float* outW, float* outH);
SLIME_EXPORT void __cdecl Input_GetAspectRatio(float* outX, float* outY);
SLIME_EXPORT void __cdecl Input_SetViewportRect(int x, int y, int width, int height);
SLIME_EXPORT void __cdecl Input_GetViewportRect(int* outX, int* outY, int* outW, int* outH);

// Scroll
SLIME_EXPORT void __cdecl Input_SetScroll(float newScroll);
SLIME_EXPORT float __cdecl Input_GetScroll();

// Focus
SLIME_EXPORT bool __cdecl Input_GetFocus();
SLIME_EXPORT void __cdecl Input_SetFocus(bool focus);

// -----------------------------
// Text / Font helpers
// -----------------------------
SLIME_EXPORT unsigned int __cdecl Text_CreateTextureFromFontFile(const char* fontPath, const char* text, int pixelHeight, int* outWidth, int* outHeight);
SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path);
SLIME_EXPORT void __cdecl Font_Free(void* font);
SLIME_EXPORT unsigned int __cdecl Text_RenderToEntity(void* font, EntityId id, const char* text, int pixelHeight);

// -----------------------------
// ObjectManager helpers
// -----------------------------
SLIME_EXPORT EntityId __cdecl ObjectManager_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT EntityId __cdecl ObjectManager_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT EntityId __cdecl ObjectManager_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId);
SLIME_EXPORT void __cdecl ObjectManager_Destroy(EntityId id);
SLIME_EXPORT bool __cdecl ObjectManager_IsAlive(EntityId id);
SLIME_EXPORT int __cdecl ObjectManager_GetSize();
SLIME_EXPORT EntityId __cdecl ObjectManager_GetIdAtIndex(int index);

// -----------------------------
// UI helpers
// -----------------------------
SLIME_EXPORT EntityId __cdecl UI_CreateText(const char* text, int fontSize, float x, float y);
SLIME_EXPORT void __cdecl UI_Destroy(EntityId id);
SLIME_EXPORT void __cdecl UI_SetText(EntityId id, const char* text);
SLIME_EXPORT void __cdecl UI_SetPosition(EntityId id, float x, float y);
SLIME_EXPORT void __cdecl UI_SetAnchor(EntityId id, float ax, float ay);
SLIME_EXPORT void __cdecl UI_SetColor(EntityId id, float r, float g, float b);
SLIME_EXPORT void __cdecl UI_SetVisible(EntityId id, bool visible);
SLIME_EXPORT void __cdecl UI_SetLayer(EntityId id, int layer);
