#include "Scripting/ExportScene.h"
#include "Scene/Scene.h"
#include "Scripting/ExportEntity.h"

SLIME_EXPORT EntityId __cdecl Scene_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b)
{
	if (!Scene::GetActiveScene())
		return 0;
	return (EntityId) Scene::GetActiveScene()->CreateGameObject(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(r, g, b));
}

SLIME_EXPORT EntityId __cdecl Scene_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
{
	return Entity_CreateQuad(px, py, sx, sy, r, g, b);
}

SLIME_EXPORT EntityId __cdecl Scene_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId)
{
	if (!Scene::GetActiveScene())
		return 0;
	ObjectId id = Scene::GetActiveScene()->CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(1.0f));
	Entity_SetTexture((EntityId) id, texId, 0, 0);
	return (EntityId) id;
}

SLIME_EXPORT void __cdecl Scene_Destroy(EntityId id)
{
	Entity_Destroy(id);
}

SLIME_EXPORT bool __cdecl Scene_IsAlive(EntityId id)
{
	return Entity_IsAlive(id);
}

SLIME_EXPORT int __cdecl Scene_GetEntityCount()
{
	return Scene::GetActiveScene() ? Scene::GetActiveScene()->GetObjectCount() : 0;
}

SLIME_EXPORT EntityId __cdecl Scene_GetEntityIdAtIndex(int index)
{
	return Scene::GetActiveScene() ? (EntityId) Scene::GetActiveScene()->GetIdAtIndex(index) : 0;
}
