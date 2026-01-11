#include "Scene.h"

#include <algorithm>
#include <string>

#include "Core/Logger.h"
#include "Core/Input.h"
#include "Physics/RigidBody.h"
#include "Rendering/ParticleSystem.h"
#include "Rendering/Renderer.h"

#define ENABLE_SCENE_LOGGING 0

Scene* Scene::s_ActiveScene = nullptr;

// Helper function to convert screen-space coordinates (pixels, top-left origin) to UI space (world units, center origin)
static glm::vec2 ScreenSpaceToUISpace(float screenX, float screenY, float uiHeight)
{
	auto viewport = Input::GetInstance()->GetViewportRect();
	float vpW = viewport.z > 0 ? (float) viewport.z : 1920.0f;
	float vpH = viewport.w > 0 ? (float) viewport.w : 1080.0f;
	float aspect = (vpH > 0.0f) ? (vpW / vpH) : (16.0f / 9.0f);

	const float uiWidth = uiHeight * aspect;

	float uiX = (screenX / vpW) * uiWidth - (uiWidth * 0.5f);
	float uiY = (uiHeight * 0.5f) - (screenY / vpH) * uiHeight;

	return glm::vec2(uiX, uiY);
}

Scene::Scene()
{
	s_ActiveScene = this;
	m_PhysicsScene = new PhysicsScene();
}

Scene::~Scene()
{
	if (s_ActiveScene == this)
		s_ActiveScene = nullptr;

	// Cleanup all active entities properly to ensure physics bodies are released
	for (auto entity: m_ActiveEntities)
	{
		if (m_Registry.HasComponent<RigidBodyComponent>(entity))
		{
			auto& rb = m_Registry.GetComponent<RigidBodyComponent>(entity);
			if (rb.RuntimeBody && m_PhysicsScene)
			{
				m_PhysicsScene->removeActor((RigidBody*) rb.RuntimeBody);
				delete (RigidBody*) rb.RuntimeBody;
				rb.RuntimeBody = nullptr;
			}
		}
	}

	if (m_PhysicsScene)
	{
		delete m_PhysicsScene;
		m_PhysicsScene = nullptr;
	}

	// Registry cleans up itself, but we should clear active entities
	m_ActiveEntities.clear();
	m_UIElements.clear();
}

Entity Scene::GetPrimaryCameraEntity()
{
	auto view = m_Registry.View<CameraComponent>();
	for (auto entity: view)
	{
		const auto& camera = m_Registry.GetComponent<CameraComponent>(entity);
		if (camera.IsPrimary)
			return entity;
	}
	return NullEntity;
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
		// Cleanup Physics Body if exists
		if (m_Registry.HasComponent<RigidBodyComponent>(id))
		{
			auto& rb = m_Registry.GetComponent<RigidBodyComponent>(id);
			if (rb.RuntimeBody && m_PhysicsScene)
			{
				m_PhysicsScene->removeActor((RigidBody*) rb.RuntimeBody);
				delete (RigidBody*) rb.RuntimeBody;
				rb.RuntimeBody = nullptr;
			}
		}

		m_ActiveEntities.erase(it);
		m_Registry.DestroyEntity(id);
	}
}

bool Scene::IsAlive(ObjectId id) const
{
	if (id >= 100000)
		return m_UIElements.find(id) != m_UIElements.end();

	// Check if in active entities list (O(N) but safe)
	for (auto e: m_ActiveEntities)
		if (e == id)
			return true;
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

void Scene::RenderUI(float uiHeight)
{
	// Sort UI elements by Layer (Ascending) so higher layers draw last (Painter's Algorithm)
	// Since we disabled Depth Testing in Renderer2D, draw order is critical.
	std::vector<PersistentUIElement*> sortedUI;
	for (auto& [id, element] : m_UIElements)
	{
		if (element.IsVisible)
			sortedUI.push_back(&element);
	}
	std::sort(sortedUI.begin(), sortedUI.end(), [](PersistentUIElement* a, PersistentUIElement* b) {
		return a->Layer < b->Layer;
	});

	for (auto* elementPtr : sortedUI)
	{
		auto& element = *elementPtr;

		bool clipped = false;
		if (element.ClipRect.z > 0.0f && element.ClipRect.w > 0.0f)
		{
			Renderer::EnableScissor(element.ClipRect.x, element.ClipRect.y, element.ClipRect.z, element.ClipRect.w);
			clipped = true;
		}

		glm::vec2 position = element.Position;
		glm::vec2 size = element.Scale;

		if (element.UseScreenSpace)
		{
			position = ScreenSpaceToUISpace(element.Position.x, element.Position.y, uiHeight);

			if (!element.IsText)
			{
				auto viewport = Input::GetInstance()->GetViewportRect();
				float vpH = viewport.w > 0 ? (float) viewport.w : 1080.0f;
				float scaleFactor = uiHeight / vpH;
				size = size * scaleFactor;
			}
		}

		// Z is irrelevant now that Depth Test is disabled, but keep it 0.0f
		glm::vec3 drawPos = glm::vec3(position.x, position.y, 0.0f);

		if (element.IsText && element.Font)
		{
			glm::vec3 textInfo = element.Font->CalculateSizeWithBaseline(element.TextContent, element.Scale.x, element.WrapWidth);
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

			Renderer::DrawString(element.TextContent, element.Font, finalPos, element.Scale.x, element.Color, element.WrapWidth);
		}
		else if (!element.IsText)
		{
			float offX = (0.5f - element.Anchor.x) * size.x;
			float offY = (0.5f - element.Anchor.y) * size.y;

			glm::vec3 finalPos = drawPos;
			finalPos.x += offX;
			finalPos.y += offY;

			if (element.Image)
			{
				Renderer::DrawQuad(finalPos, { size.x, -size.y }, element.Image, 1.0f, element.Color);
			}
			else
			{
				Renderer::DrawQuad(finalPos, size, element.Color);
			}
		}

		if (clipped)
		{
			Renderer::DisableScissor();
		}
	}
}

ObjectId Scene::GetIdAtIndex(int index) const
{
	if (index < 0 || index >= (int) m_ActiveEntities.size())
		return InvalidObjectId;
	return m_ActiveEntities[index];
}

void Scene::Update(float deltaTime)
{
	// Physics System Integration
	if (m_PhysicsScene)
	{
		const auto& rbEntities = m_Registry.View<RigidBodyComponent>();

		for (Entity entity: rbEntities)
		{
			if (!m_Registry.HasComponent<TransformComponent>(entity))
				continue;

			auto& transform = m_Registry.GetComponent<TransformComponent>(entity);
			auto& rb = m_Registry.GetComponent<RigidBodyComponent>(entity);

			if (!rb.RuntimeBody)
			{
				// Create Physics Body
				RigidBody* body = new RigidBody();
				body->SetPos(transform.Position);
				body->SetMass(rb.Mass);
				body->SetKinematic(rb.IsKinematic);
				body->SetFixedRotation(rb.FixedRotation);
				body->SetVelocity(rb.Velocity);

				// Handle Colliders
				if (m_Registry.HasComponent<BoxColliderComponent>(entity))
				{
					auto& bc = m_Registry.GetComponent<BoxColliderComponent>(entity);
					body->SetBoundingBox(bc.Offset, bc.Size);
				}

				m_PhysicsScene->addActor(body, "Entity", rb.IsKinematic);
				rb.RuntimeBody = body;
			}
			else
			{
				RigidBody* body = (RigidBody*) rb.RuntimeBody;

				// Sync ECS -> Physics (if Kinematic or properties changed)
				if (rb.IsKinematic)
				{
					body->SetPos(transform.Position);
				}
				else
				{
					// Allow ECS to drive velocity for dynamic bodies (Arcade Physics style)
					body->SetVelocity(rb.Velocity);
				}

				// Sync properties that might change at runtime
				body->SetMass(rb.Mass);
				body->SetKinematic(rb.IsKinematic);
				body->SetFixedRotation(rb.FixedRotation);
			}
		}

		m_PhysicsScene->update(deltaTime);

		// Sync Physics -> ECS
		for (Entity entity: rbEntities)
		{
			if (!m_Registry.HasComponent<TransformComponent>(entity))
				continue;

			auto& transform = m_Registry.GetComponent<TransformComponent>(entity);
			auto& rb = m_Registry.GetComponent<RigidBodyComponent>(entity);
			RigidBody* body = (RigidBody*) rb.RuntimeBody;

			if (body && !rb.IsKinematic)
			{
				transform.Position = body->GetPos();
				rb.Velocity = body->GetVelocity();
			}
		}
	}

	// Animation System
	for (Entity entity: m_ActiveEntities)
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
								if (maxFrames < 1)
									maxFrames = 1;

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

void Scene::SetGravity(glm::vec2 gravity)
{
	if (m_PhysicsScene)
		m_PhysicsScene->setGravity(glm::vec3(gravity, 0.0f));
}

void Scene::Render(Camera& camera)
{
	// Sort entities by Z-order (Back-to-Front) to handle transparency correctly
	// In our LH_ZO projection (Near=10, Far=-10), smaller Z is "farther" (Depth 1)
	// So we sort Ascending: -10 (Far) -> 10 (Near)
	std::vector<Entity> sortedEntities = m_ActiveEntities;
	std::sort(sortedEntities.begin(),
	        sortedEntities.end(),
	        [&](Entity a, Entity b)
	        {
		        float zA = 0.0f;
		        float zB = 0.0f;
		        if (m_Registry.HasComponent<TransformComponent>(a))
			        zA = m_Registry.GetComponent<TransformComponent>(a).Position.z;
		        if (m_Registry.HasComponent<TransformComponent>(b))
			        zB = m_Registry.GetComponent<TransformComponent>(b).Position.z;
		        return zA < zB;
	        });

	Renderer::BeginScene(camera);

	static int frameCount = 0;
	frameCount++;
#if ENABLE_SCENE_LOGGING
	bool doLog = (frameCount % 60 == 0);
#else
	bool doLog = false;
#endif

	int countTotal = 0;
	int countComponents = 0;
	int countVisible = 0;
	int countDraw = 0;

	if (doLog)
	{
		Logger::Info("Scene::Render - Entity Count: " + std::to_string(sortedEntities.size()));
		if (!sortedEntities.empty())
		{
			Entity first = sortedEntities[0];
			if (m_Registry.HasComponent<TransformComponent>(first))
			{
				float z = m_Registry.GetComponent<TransformComponent>(first).Position.z;
				Logger::Info("First Entity Z: " + std::to_string(z));
			}
			if (m_Registry.HasComponent<SpriteComponent>(first))
			{
				auto& sprite = m_Registry.GetComponent<SpriteComponent>(first);
				Logger::Info("First Entity Texture Ptr: " + std::to_string((uint64_t)sprite.Texture));
                Logger::Info("First Entity Color: " + std::to_string(sprite.Color.r) + ", " + std::to_string(sprite.Color.g) + ", " + std::to_string(sprite.Color.b) + ", " + std::to_string(sprite.Color.a));
			}
		}
	}

	int failures = 0;
	for (Entity entity: sortedEntities)
	{
		countTotal++;
		bool hasTransform = m_Registry.HasComponent<TransformComponent>(entity);
		bool hasSprite = m_Registry.HasComponent<SpriteComponent>(entity);

		if (!hasTransform || !hasSprite)
		{
			if (doLog && failures < 5)
			{
				Logger::Warn("Entity " + std::to_string(entity) + " missing components: " + 
					(hasTransform ? "" : "Transform ") + (hasSprite ? "" : "Sprite"));
			}
			failures++;
			continue;
		}

		countComponents++;
		auto& transform = m_Registry.GetComponent<TransformComponent>(entity);
		auto& sprite = m_Registry.GetComponent<SpriteComponent>(entity);

		if (!sprite.IsVisible)
		{
			if (doLog && failures < 5)
			{
				Logger::Warn("Entity " + std::to_string(entity) + " is not visible");
			}
			failures++;
			continue;
		}

		countVisible++;
		countDraw++;

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
						if (framesPerRow == 0)
							framesPerRow = 1;
						int column = anim.Frame % framesPerRow;

						float u0 = (float) (column * anim.SpriteWidth) / texWidth;
						float v0 = 0.0f;
						float u1 = (float) ((column + 1) * anim.SpriteWidth) / texWidth;
						float v1 = 1.0f;

						glm::vec2 uvs[4] = {
							{ u0, v0 }, // BL
							{ u1, v0 }, // BR
							{ u1, v1 }, // TR
							{ u0, v1 }  // TL
						};

						Renderer::DrawQuadUV(transformMat, sprite.Texture, uvs, sprite.Color);
					}
				}

				if (!hasAnim)
				{
					// Logger::Info("Drawing Entity " + std::to_string(entity));
					Renderer::DrawQuad(transformMat, sprite.Texture, sprite.TilingFactor, sprite.Color);
				}
			}
			else
			{
				Renderer::DrawQuad(transformMat, sprite.Color);
			}
	}

	for (auto ps: m_ParticleSystems)
		ps->OnRender();

	Renderer::EndScene();

	if (doLog)
	{
		Logger::Info("Scene::Render Stats:");
		Logger::Info("  Total Entities: " + std::to_string(countTotal));
		Logger::Info("  With Components: " + std::to_string(countComponents));
		Logger::Info("  Visible: " + std::to_string(countVisible));
		Logger::Info("  Draw Calls (Quads): " + std::to_string(countDraw));

		auto stats = Renderer::GetStats();
		Logger::Info("  Renderer Stats - Quads: " + std::to_string(stats.QuadCount) + " DrawCalls: " + std::to_string(stats.DrawCalls));
	}
}
