#include "Scripting/ExportResource.h"

#include <string>

#include "Resources/ResourceManager.h"

SLIME_EXPORT void* __cdecl Resources_LoadTexture(const char* name, const char* path)
{
	if (!name)
		return nullptr;
	std::string strName = name;
	std::string strPath = path ? path : "";
	return (void*) ResourceManager::GetInstance().LoadTexture(strName, strPath);
}

SLIME_EXPORT void* __cdecl Resources_GetTexture(const char* name)
{
	if (!name)
		return nullptr;
	return (void*) ResourceManager::GetInstance().GetTexture(name);
}

SLIME_EXPORT void* __cdecl Resources_LoadFont(const char* name, const char* path, int fontSize)
{
	if (!name)
		return nullptr;
	std::string strName = name;
	std::string strPath = path ? path : "";
	return (void*) ResourceManager::GetInstance().LoadFont(strName, strPath, fontSize);
}

SLIME_EXPORT const char* __cdecl Resources_LoadText(const char* name, const char* path)
{
	if (!name)
		return nullptr;
	std::string strName = name;
	std::string strPath = path ? path : "";
	return ResourceManager::GetInstance().LoadText(strName, strPath);
}

SLIME_EXPORT void* __cdecl Texture_Load(const char* path)
{
	if (!path)
		return nullptr;
	return (void*) ResourceManager::GetInstance().LoadTexture(path, path);
}
