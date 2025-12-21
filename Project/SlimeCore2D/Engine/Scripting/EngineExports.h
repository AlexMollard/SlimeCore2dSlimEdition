#pragma once
#include <cstdint>

#if defined(_WIN32)
    #define SLIME_API extern "C" __declspec(dllexport)
    #define SLIME_CALL SLIME_CALL
#else
    #define SLIME_API extern "C" __attribute__((visibility("default")))
    #define SLIME_CALL
#endif

using EntityId = std::uint64_t;

// -----------------------------
// Core / Logging
// -----------------------------
SLIME_API void SLIME_CALL Engine_Log(const char* msg);

// -----------------------------
// Entity lifecycle (Create/Destroy/IsAlive)
// -----------------------------
SLIME_API EntityId SLIME_CALL Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_API void SLIME_CALL Entity_Destroy(EntityId id);
SLIME_API bool SLIME_CALL Entity_IsAlive(EntityId id);

// -----------------------------
// Entity transform & visual API
// (position, size, color, layer, anchor)
// -----------------------------
SLIME_API void SLIME_CALL Entity_SetPosition(EntityId id, float x, float y);
SLIME_API void SLIME_CALL Entity_GetPosition(EntityId id, float* outX, float* outY);
SLIME_API void SLIME_CALL Entity_SetSize(EntityId id, float sx, float sy);
SLIME_API void SLIME_CALL Entity_GetSize(EntityId id, float* outSx, float* outSy);
SLIME_API void SLIME_CALL Entity_SetColor(EntityId id, float r, float g, float b);
SLIME_API void SLIME_CALL Entity_SetLayer(EntityId id, int layer);
SLIME_API int  SLIME_CALL Entity_GetLayer(EntityId id);
SLIME_API void SLIME_CALL Entity_SetAnchor(EntityId id, float ax, float ay);
SLIME_API void SLIME_CALL Entity_GetAnchor(EntityId id, float* outAx, float* outAy);

// -----------------------------
// Entity visual helpers (texture / visibility / animation)
// -----------------------------
SLIME_API void SLIME_CALL Entity_SetTexture(EntityId id, unsigned int texId, int width, int height);
SLIME_API void SLIME_CALL Entity_SetRender(EntityId id, bool value);
SLIME_API bool SLIME_CALL Entity_GetRender(EntityId id);

SLIME_API void SLIME_CALL Entity_SetFrame(EntityId id, int frame);
SLIME_API int  SLIME_CALL Entity_GetFrame(EntityId id);
SLIME_API void SLIME_CALL Entity_AdvanceFrame(EntityId id);
SLIME_API void SLIME_CALL Entity_SetSpriteWidth(EntityId id, int width);
SLIME_API int  SLIME_CALL Entity_GetSpriteWidth(EntityId id);
SLIME_API void SLIME_CALL Entity_SetHasAnimation(EntityId id, bool value);
SLIME_API void SLIME_CALL Entity_SetFrameRate(EntityId id, float frameRate);
SLIME_API float SLIME_CALL Entity_GetFrameRate(EntityId id);

// -----------------------------
// Input: keyboard / mouse / window
// -----------------------------
SLIME_API bool SLIME_CALL Input_GetKeyDown(int key);
SLIME_API bool SLIME_CALL Input_GetKeyReleased(int key);

// Mouse & window input
SLIME_API void SLIME_CALL Input_GetMousePos(float* outX, float* outY);
SLIME_API void SLIME_CALL Input_GetMouseDelta(float* outX, float* outY);
SLIME_API bool SLIME_CALL Input_GetMouseDown(int button);
SLIME_API void SLIME_CALL Input_GetMouseToWorldPos(float* outX, float* outY);

// Window and viewport
SLIME_API void SLIME_CALL Input_GetWindowSize(float* outW, float* outH);
SLIME_API void SLIME_CALL Input_GetAspectRatio(float* outX, float* outY);
SLIME_API void SLIME_CALL Input_SetViewportRect(int x, int y, int width, int height);
SLIME_API void SLIME_CALL Input_GetViewportRect(int* outX, int* outY, int* outW, int* outH);

// Scroll
SLIME_API void SLIME_CALL Input_SetScroll(float newScroll);
SLIME_API float SLIME_CALL Input_GetScroll();

// Focus
SLIME_API bool SLIME_CALL Input_GetFocus();
SLIME_API void SLIME_CALL Input_SetFocus(bool focus);

// -----------------------------
// Text / Font helpers
// -----------------------------
SLIME_API unsigned int SLIME_CALL Text_CreateTextureFromFontFile(const char* fontPath, const char* text, int pixelHeight, int* outWidth, int* outHeight);
SLIME_API void* SLIME_CALL Font_LoadFromFile(const char* path);
SLIME_API void SLIME_CALL Font_Free(void* font);
SLIME_API unsigned int SLIME_CALL Text_RenderToEntity(void* font, EntityId id, const char* text, int pixelHeight);

// -----------------------------
// ObjectManager helpers
// -----------------------------
SLIME_API EntityId SLIME_CALL ObjectManager_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_API EntityId SLIME_CALL ObjectManager_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_API EntityId SLIME_CALL ObjectManager_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId);
SLIME_API void SLIME_CALL ObjectManager_Destroy(EntityId id);
SLIME_API bool SLIME_CALL ObjectManager_IsAlive(EntityId id);
SLIME_API int SLIME_CALL ObjectManager_GetSize();
SLIME_API EntityId SLIME_CALL ObjectManager_GetIdAtIndex(int index);

// -----------------------------
// UI helpers
// -----------------------------
SLIME_API EntityId SLIME_CALL UI_CreateText(const char* text, int fontSize, float x, float y);
SLIME_API void SLIME_CALL UI_Destroy(EntityId id);
SLIME_API void SLIME_CALL UI_SetText(EntityId id, const char* text);
SLIME_API void SLIME_CALL UI_SetPosition(EntityId id, float x, float y);
SLIME_API void SLIME_CALL UI_SetAnchor(EntityId id, float ax, float ay);
SLIME_API void SLIME_CALL UI_SetColor(EntityId id, float r, float g, float b);
SLIME_API void SLIME_CALL UI_SetVisible(EntityId id, bool visible);
SLIME_API void SLIME_CALL UI_SetLayer(EntityId id, int layer);
