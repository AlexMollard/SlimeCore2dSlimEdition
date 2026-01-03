#pragma once
#include "EngineExports.h"

extern "C"
{
	struct BatchQuad
	{
		float x, y;
		float w, h;
		float r, g, b, a;
		void* texture;
	};

	SLIME_EXPORT void __cdecl Renderer_DrawBatch(BatchQuad* quads, int count);
	SLIME_EXPORT void __cdecl Renderer_BeginScenePrimary();
	SLIME_EXPORT void __cdecl Renderer_EndScene();
}
