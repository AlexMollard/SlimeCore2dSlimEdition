#include "ObjectManager.h"

#include <algorithm>
#include <iostream>

#include "Entities/Quad.h" // Assuming Quad is a subclass of GameObject
#include "Rendering/Renderer2D.h"

ObjectManager* ObjectManager::s_instance = nullptr;

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

void ObjectManager::DestroyObject(ObjectId id)
{
	auto it = m_objectsById.find(id);
	if (it == m_objectsById.end())
		return;

	GameObject* obj = it->second;

	m_objects.erase(std::remove(m_objects.begin(), m_objects.end(), obj), m_objects.end());

	// Remove from map
	m_objectsById.erase(it);

	delete obj;
}

bool ObjectManager::IsAlive(ObjectId id) const
{
	return m_objectsById.find(id) != m_objectsById.end();
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
