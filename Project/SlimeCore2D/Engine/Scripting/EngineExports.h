#pragma once
#include <cstdint>

#if defined(_WIN32)
#	define SLIME_EXPORT extern "C" __declspec(dllexport)
#else
#	define SLIME_EXPORT extern "C"
#endif

using EntityId = std::uint64_t;

SLIME_EXPORT void __cdecl Engine_Log(const char* msg);

// Entity/object lifecycle
SLIME_EXPORT EntityId __cdecl Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT void __cdecl Entity_Destroy(EntityId id);
SLIME_EXPORT bool __cdecl Entity_IsAlive(EntityId id);

// Transform-ish API (position)
SLIME_EXPORT void __cdecl Transform_SetPosition(EntityId id, float x, float y);
SLIME_EXPORT void __cdecl Transform_GetPosition(EntityId id, float* outX, float* outY);

// Transform-ish API (size)
SLIME_EXPORT void __cdecl Transform_SetSize(EntityId id, float sx, float sy);
SLIME_EXPORT void __cdecl Transform_GetSize(EntityId id, float* outSx, float* outSy);

SLIME_EXPORT void __cdecl Visual_SetColor(EntityId id, float r, float g, float b);

SLIME_EXPORT void __cdecl Visual_SetLayer(EntityId id, int layer);
SLIME_EXPORT int  __cdecl Visual_GetLayer(EntityId id);

SLIME_EXPORT void __cdecl Visual_SetAnchor(EntityId id, float ax, float ay);
SLIME_EXPORT void __cdecl Visual_GetAnchor(EntityId id, float* outAx, float* outAy);

// Input
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

// Text/Font helpers
SLIME_EXPORT unsigned int __cdecl Text_CreateTextureFromFontFile(const char* fontPath, const char* text, int pixelHeight, int* outWidth, int* outHeight);

// Load font file into memory and return an opaque handle (free with Font_Free)
SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path);
SLIME_EXPORT void __cdecl Font_Free(void* font);

// Render text using a loaded font and attach resulting texture to an entity.
// Returns GL texture id (0 on failure).
SLIME_EXPORT unsigned int __cdecl Text_RenderToEntity(void* font, EntityId id, const char* text, int pixelHeight);

SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height);
