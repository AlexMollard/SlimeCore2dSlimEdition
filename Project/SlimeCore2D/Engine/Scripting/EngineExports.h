#pragma once
#include <cstdint>

// DLL Export Macro
#if defined(_WIN32)
#	define SLIME_EXPORT extern "C" __declspec(dllexport)
#else
#	define SLIME_EXPORT extern "C"
#endif

using EntityId = std::uint64_t;

// =================================================================================
// C++ ENGINE INTERFACE
// =================================================================================
void EngineExports_RenderUI();

// =================================================================================
// C# SCRIPTING EXPORTS (DLL API)
// =================================================================================

// -----------------------------
// Core / Logging
// -----------------------------
SLIME_EXPORT void __cdecl Engine_Log(const char* msg);

// -----------------------------
// Resources (ResourceManager)
// -----------------------------
// Loads a texture via ResourceManager. Returns the pointer.
SLIME_EXPORT void* __cdecl Resources_LoadTexture(const char* name, const char* path);
SLIME_EXPORT void* __cdecl Resources_GetTexture(const char* name);

// Loads a font via ResourceManager. Returns the pointer.
SLIME_EXPORT void* __cdecl Resources_LoadFont(const char* name, const char* path, int fontSize);

// Legacy wrappers (mapped to ResourceManager now)
SLIME_EXPORT void* __cdecl Texture_Load(const char* path);
SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path);
SLIME_EXPORT void __cdecl Font_Free(void* font); // No-op if managed by RM, or used for manual cleanup

// -----------------------------
// Entity lifecycle
// -----------------------------
SLIME_EXPORT EntityId __cdecl Entity_Create();
SLIME_EXPORT EntityId __cdecl Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT void __cdecl Entity_Destroy(EntityId id);
SLIME_EXPORT bool __cdecl Entity_IsAlive(EntityId id);

// -----------------------------
// Entity Transform
// -----------------------------
SLIME_EXPORT void __cdecl Entity_SetPosition(EntityId id, float x, float y);
SLIME_EXPORT void __cdecl Entity_GetPosition(EntityId id, float* outX, float* outY);
SLIME_EXPORT void __cdecl Entity_SetSize(EntityId id, float sx, float sy);
SLIME_EXPORT void __cdecl Entity_GetSize(EntityId id, float* outSx, float* outSy);
SLIME_EXPORT void __cdecl Entity_SetColor(EntityId id, float r, float g, float b);
SLIME_EXPORT void __cdecl Entity_SetRotation(EntityId id, float degrees);
SLIME_EXPORT float __cdecl Entity_GetRotation(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetLayer(EntityId id, int layer);
SLIME_EXPORT int __cdecl Entity_GetLayer(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetAnchor(EntityId id, float ax, float ay);
SLIME_EXPORT void __cdecl Entity_GetAnchor(EntityId id, float* outAx, float* outAy);

// -----------------------------
// Entity Visuals & Animation
// -----------------------------
SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height);
SLIME_EXPORT void __cdecl Entity_SetTexturePtr(EntityId id, void* texPtr);
SLIME_EXPORT void* __cdecl Entity_GetTexturePtr(EntityId id);

SLIME_EXPORT void __cdecl Entity_SetRender(EntityId id, bool value);
SLIME_EXPORT bool __cdecl Entity_GetRender(EntityId id);

SLIME_EXPORT void __cdecl Entity_SetFrame(EntityId id, int frame);
SLIME_EXPORT int __cdecl Entity_GetFrame(EntityId id);
SLIME_EXPORT void __cdecl Entity_AdvanceFrame(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetSpriteWidth(EntityId id, int width);
SLIME_EXPORT int __cdecl Entity_GetSpriteWidth(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetHasAnimation(EntityId id, bool value);
SLIME_EXPORT void __cdecl Entity_SetFrameRate(EntityId id, float frameRate);
SLIME_EXPORT float __cdecl Entity_GetFrameRate(EntityId id);

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

// -----------------------------
// Component Management
// -----------------------------
SLIME_EXPORT void __cdecl Entity_AddComponent_Transform(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_Transform(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_Transform(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_Sprite(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_Sprite(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_Sprite(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_Animation(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_Animation(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_Animation(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_Tag(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_Tag(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_Tag(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_Relationship(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_Relationship(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_Relationship(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_RigidBody(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_RigidBody(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_RigidBody(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_BoxCollider(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_BoxCollider(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_BoxCollider(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_CircleCollider(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_CircleCollider(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_CircleCollider(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_Camera(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_Camera(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_Camera(EntityId id);

SLIME_EXPORT void __cdecl Entity_AddComponent_AudioSource(EntityId id);
SLIME_EXPORT bool __cdecl Entity_HasComponent_AudioSource(EntityId id);
SLIME_EXPORT void __cdecl Entity_RemoveComponent_AudioSource(EntityId id);

// -----------------------------
// Physics Accessors
// -----------------------------
SLIME_EXPORT void __cdecl Entity_SetVelocity(EntityId id, float x, float y);
SLIME_EXPORT void __cdecl Entity_GetVelocity(EntityId id, float* outX, float* outY);
SLIME_EXPORT void __cdecl Entity_SetMass(EntityId id, float mass);
SLIME_EXPORT float __cdecl Entity_GetMass(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetKinematic(EntityId id, bool value);
SLIME_EXPORT bool __cdecl Entity_GetKinematic(EntityId id);

SLIME_EXPORT void __cdecl Entity_SetColliderSize(EntityId id, float w, float h);
SLIME_EXPORT void __cdecl Entity_GetColliderSize(EntityId id, float* outW, float* outH);
SLIME_EXPORT void __cdecl Entity_SetColliderOffset(EntityId id, float x, float y);
SLIME_EXPORT void __cdecl Entity_GetColliderOffset(EntityId id, float* outX, float* outY);
SLIME_EXPORT void __cdecl Entity_SetTrigger(EntityId id, bool value);
SLIME_EXPORT bool __cdecl Entity_GetTrigger(EntityId id);

// -----------------------------
// Camera Accessors
// -----------------------------
SLIME_EXPORT void __cdecl Entity_SetCameraSize(EntityId id, float size);
SLIME_EXPORT float __cdecl Entity_GetCameraSize(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetCameraZoom(EntityId id, float zoom);
SLIME_EXPORT float __cdecl Entity_GetCameraZoom(EntityId id);
SLIME_EXPORT void __cdecl Entity_SetPrimaryCamera(EntityId id, bool value);
SLIME_EXPORT bool __cdecl Entity_GetPrimaryCamera(EntityId id);

// -----------------------------
// Scene Wrappers (Replaces ObjectManager)
// -----------------------------
SLIME_EXPORT EntityId __cdecl Scene_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT EntityId __cdecl Scene_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b);
SLIME_EXPORT EntityId __cdecl Scene_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId);
SLIME_EXPORT void __cdecl Scene_Destroy(EntityId id);
SLIME_EXPORT bool __cdecl Scene_IsAlive(EntityId id);
SLIME_EXPORT int __cdecl Scene_GetEntityCount();
SLIME_EXPORT EntityId __cdecl Scene_GetEntityIdAtIndex(int index);
