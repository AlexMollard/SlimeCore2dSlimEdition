#include "ExportTileMap.h"
#include "Rendering/TileMap.h"
#include "Scene/Scene.h"
#include "Core/Input.h"

#define GLM_FORCE_DEPTH_ZERO_TO_ONE
#define GLM_FORCE_LEFT_HANDED
#include <gtc/matrix_transform.hpp>

SLIME_EXPORT void* __cdecl TileMap_Create(int width, int height, float tileSize)
{
    return new TileMap(width, height, tileSize);
}

SLIME_EXPORT void __cdecl TileMap_Destroy(void* tileMap)
{
    if (tileMap) delete (TileMap*)tileMap;
}

SLIME_EXPORT void __cdecl TileMap_SetTile(void* tileMap, int x, int y, int layer, void* texturePtr, float u0, float v0, float u1, float v1, float r, float g, float b, float a, float rotation)
{
    if (tileMap) ((TileMap*)tileMap)->SetTile(x, y, layer, texturePtr, u0, v0, u1, v1, r, g, b, a, rotation);
}

SLIME_EXPORT void __cdecl TileMap_UpdateMesh(void* tileMap)
{
    if (tileMap) ((TileMap*)tileMap)->UpdateMesh();
}

SLIME_EXPORT void __cdecl TileMap_Render(void* tileMap)
{
    if (!tileMap) return;

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
            float width = (float)viewport.z;
            float height = (float)viewport.w;
            float aspect = (height > 0) ? width / height : 16.0f/9.0f;
            
            float orthoSize = cc.OrthographicSize;
            float zoom = cc.ZoomLevel;
            
            float left = -orthoSize * aspect * zoom * 0.5f;
            float right = orthoSize * aspect * zoom * 0.5f;
            float bottom = -orthoSize * zoom * 0.5f;
            float top = orthoSize * zoom * 0.5f;
            
            // Match Camera.cpp projection (LH, Zero-to-One depth, Near=10, Far=-10)
            glm::mat4 proj = glm::orthoLH_ZO(left, right, bottom, top, 10.0f, -10.0f);
            
            glm::mat4 transform = glm::translate(glm::mat4(1.0f), tc.Position) * 
                                  glm::rotate(glm::mat4(1.0f), tc.Rotation, glm::vec3(0, 0, 1));
            glm::mat4 view = glm::inverse(transform);
            
            // Push TileMap back slightly to ensure it's behind Z=0 entities and ConveyorMap
            // ConveyorMap is at Z=-0.01. We want TileMap at Z=-0.02
            glm::mat4 model = glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, -0.02f));

            glm::mat4 viewProj = proj * view * model;
            
            ((TileMap*)tileMap)->Render(viewProj);
        }
    }
}
