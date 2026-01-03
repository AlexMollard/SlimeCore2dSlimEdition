#pragma once
#include "EngineExports.h"

extern "C"
{
	SLIME_EXPORT void* __cdecl ConveyorMap_Create(int width, int height, float tileSize);
	SLIME_EXPORT void __cdecl ConveyorMap_Destroy(void* map);
	SLIME_EXPORT void __cdecl ConveyorMap_SetConveyor(void* map, int x, int y, int tier, int direction);
	SLIME_EXPORT void __cdecl ConveyorMap_RemoveConveyor(void* map, int x, int y);
	SLIME_EXPORT void __cdecl ConveyorMap_UpdateMesh(void* map);
	SLIME_EXPORT void __cdecl ConveyorMap_Render(void* map, float time);
}
