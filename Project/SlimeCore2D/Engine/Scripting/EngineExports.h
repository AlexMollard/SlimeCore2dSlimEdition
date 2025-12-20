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
