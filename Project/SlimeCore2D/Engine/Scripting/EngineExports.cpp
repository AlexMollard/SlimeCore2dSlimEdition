#include "Scripting/EngineExports.h"

#include <cstdio>

#include "Utils/ObjectManager.h"

SLIME_EXPORT void __cdecl Engine_Log(const char* msg)
{
	std::printf("[C#] %s\n", msg ? msg : "<null>");
	std::fflush(stdout);
}

SLIME_EXPORT EntityId __cdecl Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
{
	if (!ObjectManager::IsCreated())
		return 0;

	ObjectId id = ObjectManager::Get().CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(r, g, b));

	return (EntityId) id;
}

SLIME_EXPORT void __cdecl Entity_Destroy(EntityId id)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;

	ObjectManager::Get().DestroyObject((ObjectId) id);
}

SLIME_EXPORT bool __cdecl Entity_IsAlive(EntityId id)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return false;

	return ObjectManager::Get().IsAlive((ObjectId) id);
}

SLIME_EXPORT void __cdecl Transform_SetPosition(EntityId id, float x, float y)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;

	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	obj->SetPos(glm::vec3(x, y, 0.0f));
}

SLIME_EXPORT void __cdecl Transform_GetPosition(EntityId id, float* outX, float* outY)
{
	if (!ObjectManager::IsCreated() || id == 0 || !outX || !outY)
		return;

	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	glm::vec3 p = obj->GetPos();

	*outX = p.x;
	*outY = p.y;
}
