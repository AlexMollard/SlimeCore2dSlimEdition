#include "ObjectManager.h"

#include <algorithm>
#include <iostream>

#include "Core/Input.h"
#include "Entities/Quad.h" // Assuming Quad is a subclass of GameObject
#include "Rendering/Renderer2D.h"

ObjectManager* ObjectManager::s_instance = nullptr;

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

void ObjectManager::Create()
{
	if (s_instance)
		return;
	s_instance = new ObjectManager();
}

void ObjectManager::Destroy()
{
	delete s_instance;
	s_instance = nullptr;
}

ObjectManager& ObjectManager::Get()
{
	return *s_instance;
}

bool ObjectManager::IsCreated()
{
	return s_instance != nullptr;
}

ObjectManager::ObjectManager()
{
}

ObjectManager::~ObjectManager()
{
	for (auto* obj: m_objects)
	{
		delete obj;
	}
	m_objects.clear();
	m_objectsById.clear();
	m_uiElements.clear();
}

ObjectId ObjectManager::CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	ObjectId id = m_nextId++;

	GameObject* go = new GameObject();
	// Ensure your GameObject::Create doesn't try to access the old renderer!
	go->Create(pos, color, size, (int) id);

	m_objects.push_back(go);
	m_objectsById[id] = go;

	return id;
}

ObjectId ObjectManager::CreateQuad(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	ObjectId id = m_nextId++;

	Quad* go = new Quad();
	go->Create(pos, color, size, (int) id);

	m_objects.push_back(go);
	m_objectsById[id] = go;

	return id;
}

ObjectId ObjectManager::CreateQuad(glm::vec3 pos, glm::vec2 size, Texture* tex)
{
	ObjectId id = m_nextId++;

	Quad* go = new Quad();
	go->Create(pos, glm::vec3(1), size, (int) id);
	go->SetTexture(tex);

	m_objects.push_back(go);
	m_objectsById[id] = go;

	return id;
}

ObjectId ObjectManager::CreateUIElement(bool isText)
{
	ObjectId id = m_nextId++;
	PersistentUIElement el;
	el.IsText = isText;
	m_uiElements[id] = el;
	return id;
}

PersistentUIElement* ObjectManager::GetUIElement(ObjectId id)
{
	auto it = m_uiElements.find(id);
	if (it != m_uiElements.end())
		return &it->second;
	return nullptr;
}

void ObjectManager::DestroyObject(ObjectId id)
{
	auto it = m_objectsById.find(id);
	if (it != m_objectsById.end())
	{
		GameObject* obj = it->second;

		m_objects.erase(std::remove(m_objects.begin(), m_objects.end(), obj), m_objects.end());

		// Remove from map
		m_objectsById.erase(it);

		delete obj;
		return;
	}

	auto itUI = m_uiElements.find(id);
	if (itUI != m_uiElements.end())
	{
		m_uiElements.erase(itUI);
	}
}

bool ObjectManager::IsAlive(ObjectId id) const
{
	if (m_objectsById.find(id) != m_objectsById.end())
		return true;
	if (m_uiElements.find(id) != m_uiElements.end())
		return true;
	return false;
}

GameObject* ObjectManager::Get(ObjectId id)
{
	auto it = m_objectsById.find(id);
	return (it != m_objectsById.end()) ? it->second : nullptr;
}

const GameObject* ObjectManager::Get(ObjectId id) const
{
	auto it = m_objectsById.find(id);
	return (it != m_objectsById.end()) ? it->second : nullptr;
}

void ObjectManager::Update(float deltaTime)
{
	for (auto* obj: m_objects)
		obj->Update(deltaTime);
}

void ObjectManager::UpdateFrames(float deltaTime)
{
	for (auto* obj: m_objects)
		obj->UpdateSpriteTimer(deltaTime);
}

// -----------------------------------------------------------------------
// THE BRIDGE: Connects your persistent objects to the new Renderer
// -----------------------------------------------------------------------
void ObjectManager::RenderAll()
{
	// Optional: Sort by Z-Index here if you need proper transparency handling
	// std::sort(m_objects.begin(), m_objects.end(), [](GameObject* a, GameObject* b) {
	//    return a->GetPos().z < b->GetPos().z;
	// });

	for (auto* obj: m_objects)
	{
		// Skip invisible objects
		if (!obj->GetRender())
			continue;

		Texture* tex = obj->GetTexture();

		// Case 1: Textured Object
		if (tex)
		{
			// Check if it's an animated sprite (SpriteSheet)
			if (obj->GetSpriteWidth() > 0 && obj->GetSpriteWidth() < tex->GetWidth())
			{
				// Calculate UVs for the specific frame
				// This logic was previously hidden in Renderer2D::setActiveRegion
				int frame = obj->GetFrame();
				int spriteW = obj->GetSpriteWidth();
				int texW = tex->GetWidth();

				// Basic 1D strip assumption (like previous code implied)
				// If you have 2D grid sheets, you need rows/cols logic here.
				int cols = texW / spriteW;
				if (cols == 0)
					cols = 1;

				// Map frame to UVs
				int col = frame % cols;
				int row = frame / cols; // Assuming row 0 is top

				// In new Texture class, Image is flipped?
				// Usually sprite sheets are read Top-Left to Bottom-Right.
				// Renderer2D DrawQuadUV expects { BL, BR, TR, TL }

				float u0 = (float) (col * spriteW) / texW;
				float u1 = (float) ((col + 1) * spriteW) / texW;

				// V calculation depends on your texture coordinate system (0 at bottom or top)
				// Assuming standard GL (0 bottom), but images loaded flipped?
				// Simplest logic for a single row strip:
				float v0 = 0.0f;
				float v1 = 1.0f;

				glm::vec2 uvs[4] = {
					{ u0, v0 }, // BL
					{ u1, v0 }, // BR
					{ u1, v1 }, // TR
					{ u0, v1 }  // TL
				};

				Renderer2D::DrawQuadUV(obj->GetPos(), obj->GetScale(), tex, uvs, { obj->GetColor(), 1.0f });
			}
			else
			{
				if (obj->GetRotation() != 0.0f)
				{
					Renderer2D::DrawRotatedQuad(obj->GetPos(), obj->GetScale(), glm::radians(obj->GetRotation()), tex, 1.0f, { obj->GetColor(), 1.0f });
				}
				else
				{
					// Standard Static Texture
					Renderer2D::DrawQuad(obj->GetPos(), obj->GetScale(), tex, 1.0f, { obj->GetColor(), 1.0f });
				}
			}
		}
		// Case 2: Flat Color Object
		else
		{
			Renderer2D::DrawQuad(obj->GetPos(), obj->GetScale(), { obj->GetColor(), 1.0f });
		}
	}
}

void ObjectManager::RenderUI()
{
	// Iterate over all persistent UI elements and submit them to the immediate renderer
	for (auto& [id, element]: m_uiElements)
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
			                       glm::vec2(finalPos.x, finalPos.y),
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

int ObjectManager::Size() const
{
	return (int) m_objects.size();
}


ObjectId ObjectManager::GetIdAtIndex(int index) const
{
	if (index < 0 || index >= (int) m_objects.size())
		return InvalidObjectId;
	GameObject* obj = m_objects[index];
	if (!obj)
		return InvalidObjectId;
	return static_cast<ObjectId>(obj->GetID());
}
