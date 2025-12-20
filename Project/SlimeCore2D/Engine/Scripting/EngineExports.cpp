#include "Scripting/EngineExports.h"

#include <cstdio>

#include "Utils/ObjectManager.h"

void __cdecl Engine_Log(const char* msg)
{
	std::printf("[C#] %s\n", msg ? msg : "<null>");
	std::fflush(stdout);
}

// These are pseudo-calls. You'll map them to your actual object/entity system.
EntityId __cdecl Entity_Create()
{
	return 0;
}

void __cdecl Entity_Destroy(EntityId id)
{
	// wip
}

bool __cdecl Entity_IsAlive(EntityId id)
{
	return true;
}

void __cdecl Transform_SetPosition(EntityId id, float x, float y)
{
	// wip
}

void __cdecl Transform_GetPosition(EntityId id, float* outX, float* outY)
{
	// wip
}
