#include "Scene.h"
#include "Rendering/Renderer2D.h"
#include "Entities/Quad.h"
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

	// Convert from screen space (0,0 = top-left) to UI space (0,0 = center, Y-up)
	// Screen: (0,0) = top-left, (vpW, vpH) = bottom-right
	// UI: (-uiWidth/2, uiHeight/2) = top-left, (uiWidth/2, -uiHeight/2) = bottom-right
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

	for (auto* obj : m_AllObjects)
	{
		delete obj;
	}
	m_AllObjects.clear();
	m_ObjectsById.clear();
	m_RootObjects.clear();
	m_UIElements.clear();
}

Scene* Scene::GetActiveScene()
{
	return s_ActiveScene;
}

ObjectId Scene::CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	ObjectId id = m_NextId++;
	GameObject* go = new GameObject();
	go->Create(pos, color, size, (int)id);

	m_AllObjects.push_back(go);
	m_ObjectsById[id] = go;
	AddGameObject(go);

	return id;
}

ObjectId Scene::CreateQuad(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	ObjectId id = m_NextId++;
	Quad* go = new Quad();
	go->Create(pos, color, size, (int)id);

	m_AllObjects.push_back(go);
	m_ObjectsById[id] = go;
	AddGameObject(go);

	return id;
}

ObjectId Scene::CreateQuad(glm::vec3 pos, glm::vec2 size, Texture* tex)
{
	ObjectId id = m_NextId++;
	Quad* go = new Quad();
	go->Create(pos, glm::vec3(1.0f), size, (int)id);
	go->SetTexture(tex);

	m_AllObjects.push_back(go);
	m_ObjectsById[id] = go;
	AddGameObject(go);

	return id;
}

void Scene::DestroyObject(ObjectId id)
{
	auto it = m_ObjectsById.find(id);
	if (it != m_ObjectsById.end())
	{
		GameObject* obj = it->second;
		RemoveGameObject(obj); // Remove from root list if it's there

		// Remove from all objects list
		auto allIt = std::find(m_AllObjects.begin(), m_AllObjects.end(), obj);
		if (allIt != m_AllObjects.end())
			m_AllObjects.erase(allIt);

		m_ObjectsById.erase(it);
		delete obj;
		return;
	}

	auto itUI = m_UIElements.find(id);
	if (itUI != m_UIElements.end())
	{
		m_UIElements.erase(itUI);
	}
}

GameObject* Scene::GetGameObject(ObjectId id)
{
	auto it = m_ObjectsById.find(id);
	return (it != m_ObjectsById.end()) ? it->second : nullptr;
}

bool Scene::IsAlive(ObjectId id) const
{
	if (m_ObjectsById.find(id) != m_ObjectsById.end())
		return true;
	if (m_UIElements.find(id) != m_UIElements.end())
		return true;
	return false;
}

ObjectId Scene::CreateUIElement(bool isText)
{
	ObjectId id = m_NextId++;
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
	// Iterate over all persistent UI elements and submit them to the immediate renderer
	for (auto& [id, element] : m_UIElements)
	{
		if (!element.IsVisible)
			continue;

		// Convert position based on coordinate space
		glm::vec2 position = element.Position;
		if (element.UseScreenSpace)
		{
			// Convert from screen pixels to UI world space
			position = ScreenSpaceToUISpace(element.Position.x, element.Position.y);
		}

		// Calculate Draw Position based on Anchor
		// Renderer2D draws at center/position. We need to offset based on anchor.
		glm::vec3 drawPos = glm::vec3(position.x, position.y, 0.9f + (element.Layer * 0.001f));

		if (element.IsText && element.Font)
		{
			// Anchor-aware positioning for text: measure and offset
			// DrawString draws from baseline/left, so we need to offset based on anchor
			glm::vec3 textInfo = element.Font->CalculateSizeWithBaseline(element.TextContent, element.Scale.x);
			float textWidth = textInfo.x;
			float textHeight = textInfo.y;
			float maxY = textInfo.z;        // How far above baseline text extends
			float minY = textHeight - maxY; // How far below baseline text extends

			glm::vec3 finalPos = drawPos;

			// Horizontal anchor: 0.0 = left, 0.5 = center, 1.0 = right
			finalPos.x += (0.0f - element.Anchor.x) * textWidth;

			// Vertical anchor: 0.0 = bottom, 0.5 = center, 1.0 = top
			float baselineForBottom = drawPos.y + minY;
			float baselineForCenter = drawPos.y - (maxY - minY) * 0.5f;
			float baselineForTop = drawPos.y - maxY;

			// Linear interpolation between the three points
			if (element.Anchor.y <= 0.5f)
			{
				// Interpolate between bottom (0.0) and center (0.5)
				float t = element.Anchor.y * 2.0f; // Maps 0.0->0.0, 0.5->1.0
				finalPos.y = baselineForBottom * (1.0f - t) + baselineForCenter * t;
			}
			else
			{
				// Interpolate between center (0.5) and top (1.0)
				float t = (element.Anchor.y - 0.5f) * 2.0f; // Maps 0.5->0.0, 1.0->1.0
				finalPos.y = baselineForCenter * (1.0f - t) + baselineForTop * t;
			}

			// SDF Text Render
			Renderer2D::DrawString(element.TextContent,
				element.Font,
				finalPos,
				element.Scale.x, // Scale 1.0 = 48px
				element.Color);
		}
		else if (!element.IsText)
		{
			// UI Quad Render
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
	if (index < 0 || index >= (int)m_AllObjects.size())
		return InvalidObjectId;
	GameObject* obj = m_AllObjects[index];
	if (!obj)
		return InvalidObjectId;
	return static_cast<ObjectId>(obj->GetID());
}

void Scene::AddGameObject(GameObject* gameObject)
{
	m_RootObjects.push_back(gameObject);
}

void Scene::RemoveGameObject(GameObject* gameObject)
{
	auto it = std::find(m_RootObjects.begin(), m_RootObjects.end(), gameObject);
	if (it != m_RootObjects.end())
	{
		m_RootObjects.erase(it);
	}
}

void Scene::Update(float deltaTime)
{
	for (auto obj : m_RootObjects)
	{
		obj->Update(deltaTime);
		obj->UpdateSpriteTimer(deltaTime); // Ensure animation updates happen
	}
}


void Scene::Render(Camera& camera)
{
	Renderer2D::BeginScene(camera);

	for (auto obj : m_RootObjects)
	{
		RenderNode(obj);
	}

	Renderer2D::EndScene();
}

void Scene::RenderNode(GameObject* node)
{
	if (!node->GetRender())
		return;

	// Calculate World Transform
	glm::mat4 transform = node->GetWorldTransform();

	// Submit to Renderer
	if (node->GetTexture())
	{
		if (node->GetSpriteWidth() > 0)
		{
			// Calculate UVs for sprite sheet
			int texWidth = node->GetTexture()->GetWidth();
			int texHeight = node->GetTexture()->GetHeight();
			int spriteWidth = node->GetSpriteWidth();
			
			int framesPerRow = texWidth / spriteWidth;
			int column = node->GetFrame() % framesPerRow;
			
			float u0 = (float)(column * spriteWidth) / texWidth;
			float v0 = 0.0f;
			float u1 = (float)((column + 1) * spriteWidth) / texWidth;
			float v1 = 1.0f;

			glm::vec2 uvs[4] = {
				{ u0, v0 }, // BL
				{ u1, v0 }, // BR
				{ u1, v1 }, // TR
				{ u0, v1 }  // TL
			};
			
			Renderer2D::DrawQuadUV(transform, node->GetTexture(), uvs, glm::vec4(node->GetColor(), 1.0f));
		}
		else
		{
			Renderer2D::DrawQuad(transform, node->GetTexture(), 1.0f, glm::vec4(node->GetColor(), 1.0f));
		}
	}
	else
	{
		Renderer2D::DrawQuad(transform, glm::vec4(node->GetColor(), 1.0f));
	}

	// Recursively render children
	for (auto child : node->GetChildren())
	{
		RenderNode(child);
	}
}
