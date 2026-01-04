#include "ExportRenderer.h"

#include "Core/Input.h"
#include "Rendering/Renderer.h"
#include "Rendering/Texture.h"
#include "Scene/Scene.h"

#define GLM_FORCE_DEPTH_ZERO_TO_ONE
#define GLM_FORCE_LEFT_HANDED
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
			Renderer::DrawQuad(pos, size, (Texture*) q.texture, 1.0f, color);
		}
		else
		{
			Renderer::DrawQuad(pos, size, color);
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
			float width = (float) viewport.z;
			float height = (float) viewport.w;
			float aspect = (height > 0) ? width / height : 16.0f / 9.0f;

			float orthoSize = cc.OrthographicSize;
			float zoom = cc.ZoomLevel;

			// Projection
			float left = -orthoSize * aspect * zoom * 0.5f;
			float right = orthoSize * aspect * zoom * 0.5f;
			float bottom = -orthoSize * zoom * 0.5f;
			float top = orthoSize * zoom * 0.5f;

			// Match Camera.cpp projection (LH, Zero-to-One depth, Near=-10, Far=10)
			glm::mat4 proj = glm::orthoLH_ZO(left, right, bottom, top, -10.0f, 10.0f);

			// View
			glm::mat4 transform = glm::translate(glm::mat4(1.0f), tc.Position) * glm::rotate(glm::mat4(1.0f), glm::radians(tc.Rotation), glm::vec3(0, 0, 1));
			glm::mat4 view = glm::inverse(transform);

			glm::mat4 viewProj = proj * view;

			Renderer::BeginScene(viewProj);
		}
	}
}

SLIME_EXPORT void __cdecl Renderer_EndScene()
{
	Renderer::EndScene();
}
