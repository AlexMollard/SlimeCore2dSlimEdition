#include "ExportConveyorMap.h"

#include "Core/Input.h"
#include "Rendering/ConveyorMap.h"
#include "Scene/Scene.h"

#define GLM_FORCE_DEPTH_ZERO_TO_ONE
#define GLM_FORCE_LEFT_HANDED
#include <gtc/matrix_transform.hpp>

SLIME_EXPORT void* __cdecl ConveyorMap_Create(int width, int height, float tileSize)
{
	return new ConveyorMap(width, height, tileSize);
}

SLIME_EXPORT void __cdecl ConveyorMap_Destroy(void* map)
{
	if (map)
		delete (ConveyorMap*) map;
}

SLIME_EXPORT void __cdecl ConveyorMap_SetConveyor(void* map, int x, int y, int tier, int direction)
{
	if (map)
		((ConveyorMap*) map)->SetConveyor(x, y, tier, direction);
}

SLIME_EXPORT void __cdecl ConveyorMap_RemoveConveyor(void* map, int x, int y)
{
	if (map)
		((ConveyorMap*) map)->RemoveConveyor(x, y);
}

SLIME_EXPORT void __cdecl ConveyorMap_UpdateMesh(void* map)
{
	if (map)
		((ConveyorMap*) map)->UpdateMesh();
}

SLIME_EXPORT void __cdecl ConveyorMap_Render(void* map, float time)
{
	if (!map)
		return;

	// Calculate ViewProj from Primary Camera
	Scene* scene = Scene::GetActiveScene();
	if (scene)
	{
		Entity camEntity = scene->GetPrimaryCameraEntity();
		if (camEntity != NullEntity)
		{
			auto& tc = scene->GetRegistry().GetComponent<TransformComponent>(camEntity);
			auto& cc = scene->GetRegistry().GetComponent<CameraComponent>(camEntity);

			auto viewport = Input::GetInstance()->GetViewportRect();
			float width = (float) viewport.z;
			float height = (float) viewport.w;
			float aspect = (height > 0) ? width / height : 16.0f / 9.0f;

			float orthoSize = cc.OrthographicSize;
			float zoom = cc.ZoomLevel;

			float left = -orthoSize * aspect * zoom * 0.5f;
			float right = orthoSize * aspect * zoom * 0.5f;
			float bottom = -orthoSize * zoom * 0.5f;
			float top = orthoSize * zoom * 0.5f;

			// Match Camera.cpp projection (LH, Zero-to-One depth, Near=-100, Far=100)
			glm::mat4 proj = glm::orthoLH_ZO(left, right, bottom, top, -100.0f, 100.0f);

			glm::mat4 transform = glm::translate(glm::mat4(1.0f), tc.Position) * glm::rotate(glm::mat4(1.0f), glm::radians(tc.Rotation), glm::vec3(0, 0, 1));
			glm::mat4 view = glm::inverse(transform);

			// Push ConveyorMap slightly behind entities (Z=0) but in front of TileMap (Z=-0.02)
			// Entities are at Z=0. We want Conveyor at Z=-0.01 (Behind entities)

			glm::mat4 model = glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, 0.0f));

			glm::mat4 viewProj = proj * view * model;

			((ConveyorMap*) map)->Render(viewProj, time);
		}
	}
}
