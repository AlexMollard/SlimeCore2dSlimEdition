#include "Scene.h"
#include "Rendering/Renderer2D.h"
#include "Rendering/ParticleSystem.h"
#include "Core/Input.h"
#include <algorithm>

Scene* Scene::s_ActiveScene = nullptr;

// Helper function to convert screen-space coordinates (pixels, top-left origin) to UI space (world units, center origin)
static glm::vec2 ScreenSpaceToUISpace(float screenX, float screenY)
{
	auto viewport = Input::GetInstance()->GetViewportRect();
	float vpW = viewport.z > 0 ? (float) viewport.z : 1920.0f;
	float vpH = viewport.w > 0 ? (float) viewport.w : 1080.0f;
	float aspect = (vpH > 0.0f) ? (vpW / vpH) : (16.0f / 9.0f);

	const float uiHeight = 18.0f; // match Camera ortho size used in Game2D
	const float uiWidth = uiHeight * aspect;

	float uiX = (screenX / vpW) * uiWidth - (uiWidth * 0.5f);
	float uiY = (uiHeight * 0.5f) - (screenY / vpH) * uiHeight;

	return glm::vec2(uiX, uiY);
}

Scene::Scene()
{
	s_ActiveScene = this;
}

Scene::~Scene()
{
	if (s_ActiveScene == this)
		s_ActiveScene = nullptr;
	
	// Registry cleans up itself, but we should clear active entities
	m_ActiveEntities.clear();
	m_UIElements.clear();
}

Scene* Scene::GetActiveScene()
{
	return s_ActiveScene;
}

ObjectId Scene::CreateEntity()
{
	Entity entity = m_Registry.CreateEntity();
	m_ActiveEntities.push_back(entity);
	return entity;
}

ObjectId Scene::CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	Entity entity = m_Registry.CreateEntity();

	TransformComponent transform;
	transform.Position = pos;
	transform.Scale = size;
	m_Registry.AddComponent(entity, transform);

	SpriteComponent sprite;
	sprite.Color = glm::vec4(color, 1.0f);
	m_Registry.AddComponent(entity, sprite);

	AnimationComponent anim;
	m_Registry.AddComponent(entity, anim);

	m_ActiveEntities.push_back(entity);
	return entity;
}

ObjectId Scene::CreateQuad(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	return CreateGameObject(pos, size, color);
}

ObjectId Scene::CreateQuad(glm::vec3 pos, glm::vec2 size, Texture* tex)
{
	Entity entity = m_Registry.CreateEntity();

	TransformComponent transform;
	transform.Position = pos;
	transform.Scale = size;
	m_Registry.AddComponent(entity, transform);

	SpriteComponent sprite;
	sprite.Color = glm::vec4(1.0f);
	sprite.Texture = tex;
	m_Registry.AddComponent(entity, sprite);

	AnimationComponent anim;
	m_Registry.AddComponent(entity, anim);

	m_ActiveEntities.push_back(entity);
	return entity;
}

void Scene::DestroyObject(ObjectId id)
{
	if (id >= 100000) // UI ID range check
	{
		m_UIElements.erase(id);
		return;
	}

	auto it = std::find(m_ActiveEntities.begin(), m_ActiveEntities.end(), id);
	if (it != m_ActiveEntities.end())
	{
		m_ActiveEntities.erase(it);
		m_Registry.DestroyEntity(id);
	}
}

bool Scene::IsAlive(ObjectId id) const
{
	if (id >= 100000)
		return m_UIElements.find(id) != m_UIElements.end();
	
	// Check if in active entities list (O(N) but safe)
	for (auto e : m_ActiveEntities)
		if (e == id) return true;
	return false;
}

ObjectId Scene::CreateUIElement(bool isText)
{
	ObjectId id = m_NextUIId++;
	PersistentUIElement el;
	el.IsText = isText;
	m_UIElements[id] = el;
	return id;
}

PersistentUIElement* Scene::GetUIElement(ObjectId id)
{
	auto it = m_UIElements.find(id);
	if (it != m_UIElements.end())
		return &it->second;
	return nullptr;
}

void Scene::RenderUI()
{
	for (auto& [id, element] : m_UIElements)
	{
		if (!element.IsVisible)
			continue;

		glm::vec2 position = element.Position;
		if (element.UseScreenSpace)
		{
			position = ScreenSpaceToUISpace(element.Position.x, element.Position.y);
		}

		glm::vec3 drawPos = glm::vec3(position.x, position.y, 0.9f + (element.Layer * 0.001f));

		if (element.IsText && element.Font)
		{
			glm::vec3 textInfo = element.Font->CalculateSizeWithBaseline(element.TextContent, element.Scale.x);
			float textWidth = textInfo.x;
			float textHeight = textInfo.y;
			float maxY = textInfo.z;
			float minY = textHeight - maxY;

			glm::vec3 finalPos = drawPos;
			finalPos.x += (0.0f - element.Anchor.x) * textWidth;

			float baselineForBottom = drawPos.y + minY;
			float baselineForCenter = drawPos.y - (maxY - minY) * 0.5f;
			float baselineForTop = drawPos.y - maxY;

			if (element.Anchor.y <= 0.5f)
			{
				float t = element.Anchor.y * 2.0f;
				finalPos.y = baselineForBottom * (1.0f - t) + baselineForCenter * t;
			}
			else
			{
				float t = (element.Anchor.y - 0.5f) * 2.0f;
				finalPos.y = baselineForCenter * (1.0f - t) + baselineForTop * t;
			}

			Renderer2D::DrawString(element.TextContent, element.Font, finalPos, element.Scale.x, element.Color);
		}
		else if (!element.IsText)
		{
			float offX = (0.5f - element.Anchor.x) * element.Scale.x;
			float offY = (0.5f - element.Anchor.y) * element.Scale.y;

			glm::vec3 finalPos = drawPos;
			finalPos.x += offX;
			finalPos.y += offY;

			if (element.Image)
			{
				Renderer2D::DrawQuad(finalPos, element.Scale, element.Image, 1.0f, element.Color);
			}
			else
			{
				Renderer2D::DrawQuad(finalPos, element.Scale, element.Color);
			}
		}
	}
}

ObjectId Scene::GetIdAtIndex(int index) const
{
	if (index < 0 || index >= (int)m_ActiveEntities.size())
		return InvalidObjectId;
	return m_ActiveEntities[index];
}

void Scene::Update(float deltaTime)
{
	// Animation System
	for (Entity entity : m_ActiveEntities)
	{
		if (m_Registry.HasComponent<AnimationComponent>(entity))
		{
			auto& anim = m_Registry.GetComponent<AnimationComponent>(entity);
			if (anim.HasAnimation && anim.SpriteWidth > 0)
			{
				anim.Timer += deltaTime;
				float timePerFrame = (anim.FrameRate > 0.0f) ? (1.0f / anim.FrameRate) : 0.0f;
				
				if (timePerFrame > 0.0f && anim.Timer >= timePerFrame)
				{
					while (anim.Timer >= timePerFrame)
					{
						anim.Timer -= timePerFrame;
						
						// Advance Frame
						if (m_Registry.HasComponent<SpriteComponent>(entity))
						{
							auto& sprite = m_Registry.GetComponent<SpriteComponent>(entity);
							if (sprite.Texture)
							{
								int texWidth = sprite.Texture->GetWidth();
								int maxFrames = texWidth / anim.SpriteWidth;
								if (maxFrames < 1) maxFrames = 1;
								
								anim.Frame++;
								if (anim.Frame >= maxFrames)
									anim.Frame = 0;
							}
						}
					}
				}
			}
		}
	}
}

void Scene::RegisterParticleSystem(ParticleSystem* system)
{
	m_ParticleSystems.push_back(system);
}

void Scene::UnregisterParticleSystem(ParticleSystem* system)
{
	auto it = std::find(m_ParticleSystems.begin(), m_ParticleSystems.end(), system);
	if (it != m_ParticleSystems.end())
		m_ParticleSystems.erase(it);
}

void Scene::Render(Camera& camera)
{
	Renderer2D::BeginScene(camera);

	for (Entity entity : m_ActiveEntities)
	{
		if (m_Registry.HasComponent<TransformComponent>(entity) && 
			m_Registry.HasComponent<SpriteComponent>(entity))
		{
			auto& transform = m_Registry.GetComponent<TransformComponent>(entity);
			auto& sprite = m_Registry.GetComponent<SpriteComponent>(entity);

			if (!sprite.IsVisible)
				continue;

			glm::mat4 transformMat = transform.GetTransform();

			if (sprite.Texture)
			{
				bool hasAnim = false;
				if (m_Registry.HasComponent<AnimationComponent>(entity))
				{
					auto& anim = m_Registry.GetComponent<AnimationComponent>(entity);
					if (anim.SpriteWidth > 0)
					{
						hasAnim = true;
						int texWidth = sprite.Texture->GetWidth();
						int framesPerRow = texWidth / anim.SpriteWidth;
						if (framesPerRow == 0) framesPerRow = 1;
						int column = anim.Frame % framesPerRow;

						float u0 = (float)(column * anim.SpriteWidth) / texWidth;
						float v0 = 0.0f;
						float u1 = (float)((column + 1) * anim.SpriteWidth) / texWidth;
						float v1 = 1.0f;

						glm::vec2 uvs[4] = {
							{ u0, v0 }, // BL
							{ u1, v0 }, // BR
							{ u1, v1 }, // TR
							{ u0, v1 }  // TL
						};

						Renderer2D::DrawQuadUV(transformMat, sprite.Texture, uvs, sprite.Color);
					}
				}
				
				if (!hasAnim)
				{
					Renderer2D::DrawQuad(transformMat, sprite.Texture, sprite.TilingFactor, sprite.Color);
				}
			}
			else
			{
				Renderer2D::DrawQuad(transformMat, sprite.Color);
			}
		}
	}

	for (auto ps : m_ParticleSystems)
		ps->OnRender();

	Renderer2D::EndScene();
}