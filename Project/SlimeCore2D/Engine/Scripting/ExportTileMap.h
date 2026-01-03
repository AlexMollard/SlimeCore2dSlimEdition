#pragma once
#include "EngineExports.h"

extern "C"
{
	SLIME_EXPORT void* __cdecl TileMap_Create(int width, int height, float tileSize);
	SLIME_EXPORT void __cdecl TileMap_Destroy(void* tileMap);
	SLIME_EXPORT void __cdecl TileMap_SetTile(void* tileMap, int x, int y, int layer, void* texturePtr, float u0, float v0, float u1, float v1, float r, float g, float b, float a, float rotation);
	SLIME_EXPORT void __cdecl TileMap_UpdateMesh(void* tileMap);
	SLIME_EXPORT void __cdecl TileMap_Render(void* tileMap);
}
