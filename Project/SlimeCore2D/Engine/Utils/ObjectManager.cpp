#include "ObjectManager.h"

#include <algorithm>=
ObjectManager* ObjectManager::s_instance = nullptr;

void ObjectManager::Create(Renderer2D* renderer, bool ownsRenderer)
{
	if (s_instance)
		return; // already created

	s_instance = new ObjectManager(renderer, ownsRenderer);
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

ObjectManager::ObjectManager(Renderer2D* renderer, bool ownsRenderer)
      : m_renderer(renderer), m_ownsRenderer(ownsRenderer)
{
}

ObjectManager::~ObjectManager()
{
	// Delete all objects
	for (auto* obj: m_objects)
	{
		// Your code deletes as (Quad*) always, which is risky if not all are Quad.
		// We'll delete as GameObject* and rely on virtual destructor.
		delete obj;
	}
	m_objects.clear();
	m_objectsById.clear();

	if (m_ownsRenderer)
	{
		delete m_renderer;
		m_renderer = nullptr;
	}
}

ObjectId ObjectManager::CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	ObjectId id = m_nextId++;

	GameObject* go = new GameObject();
	go->Create(pos, color, size, (int) id); // assumes your Create takes an "index/id" int

	m_renderer->AddObject(go);

	m_objects.push_back(go);
	m_objectsById[id] = go;

	return id;
}

ObjectId ObjectManager::CreateQuad(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	ObjectId id = m_nextId++;

	Quad* go = new Quad();
	go->Create(pos, color, size, (int) id);

	m_renderer->AddObject(go);

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

	m_renderer->AddObject(go);

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

	// Renderer removal first (your renderer takes pointer)
	m_renderer->RemoveQuad(obj);

	// Remove from iteration list
	m_objects.erase(std::remove(m_objects.begin(), m_objects.end(), obj), m_objects.end());

	// Remove from map and delete
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

int ObjectManager::Size() const
{
	return (int) m_objects.size();
}
