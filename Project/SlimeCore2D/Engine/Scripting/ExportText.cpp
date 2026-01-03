#include "Scripting/ExportText.h"

#include "Resources/ResourceManager.h"

SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path)
{
	if (!path)
		return nullptr;
	return (void*) ResourceManager::GetInstance().LoadFont(path, path, 48);
}

SLIME_EXPORT void __cdecl Font_Free(void* font)
{
	// Managed by ResourceManager
}
