#include "ExportRenderer.h"
#include "Rendering/Renderer2D.h"
#include "Rendering/Texture.h"
#include "Scene/Scene.h"
#include "Core/Input.h"
#include <gtc/matrix_transform.hpp>

SLIME_EXPORT void __cdecl Renderer_DrawBatch(BatchQuad* quads, int count)
{
    for (int i = 0; i < count; i++)
    {
        BatchQuad& q = quads[i];
        glm::vec2 pos = { q.x, q.y };
        glm::vec2 size = { q.w, q.h };
        glm::vec4 color = { q.r, q.g, q.b, q.a };
        
        if (q.texture)
        {
            Renderer2D::DrawQuad(pos, size, (Texture*)q.texture, 1.0f, color);
        }
        else
        {
            Renderer2D::DrawQuad(pos, size, color);
        }
    }
}

SLIME_EXPORT void __cdecl Renderer_BeginScenePrimary()
{
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
            
            // Projection
            float left = -orthoSize * aspect * zoom * 0.5f;
            float right = orthoSize * aspect * zoom * 0.5f;
            float bottom = -orthoSize * zoom * 0.5f;
            float top = orthoSize * zoom * 0.5f;
            
            glm::mat4 proj = glm::ortho(left, right, bottom, top, -1.0f, 1.0f);
            
            // View
            glm::mat4 transform = glm::translate(glm::mat4(1.0f), tc.Position) * 
                                  glm::rotate(glm::mat4(1.0f), tc.Rotation, glm::vec3(0, 0, 1));
            glm::mat4 view = glm::inverse(transform);
            
            glm::mat4 viewProj = proj * view;
            
            Renderer2D::BeginScene(viewProj);
        }
    }
}

SLIME_EXPORT void __cdecl Renderer_EndScene()
{
    Renderer2D::EndScene();
}
