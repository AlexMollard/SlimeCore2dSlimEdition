#include "ObjectManager.h"

ObjectManager::ObjectManager(Renderer2D* renderer)
{
	this->m_renderer = renderer;
}

ObjectManager::~ObjectManager()
{
	for (int i = 0; i < m_objects.size(); i++)
	{
		delete (Quad*) m_objects[i];

		m_objects[i] = nullptr;
	}

	delete m_renderer;
	m_renderer = nullptr;
}

GameObject* ObjectManager::CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	GameObject* go = new GameObject();
	go->Create(pos, color, size, m_objects.size());

	m_renderer->AddObject(go);
	m_objects.push_back(go);

	return go;
}

GameObject* ObjectManager::CreateQuad(glm::vec3 pos, glm::vec2 size, glm::vec3 color)
{
	Quad* go = new Quad();
	go->Create(pos, color, size, m_objects.size());

	m_renderer->AddObject(go);
	m_objects.push_back(go);

	return go;
}

GameObject* ObjectManager::CreateQuad(glm::vec3 pos, glm::vec2 size, Texture* tex)
{
	Quad* go = new Quad();
	go->Create(pos, glm::vec3(1), size, m_objects.size());
	go->SetTexture(tex);

	m_renderer->AddObject(go);
	m_objects.push_back(go);

	return go;
}

void ObjectManager::RemoveQuad(GameObject* object)
{
	m_objects.erase(m_objects.begin() + GetObjectIndex(object));
	m_renderer->RemoveQuad(object);
}

int ObjectManager::GetObjectIndex(GameObject* object)
{
	for (int i = 0; i < m_objects.size(); i++)
	{
		if (m_objects[i] == object)
		{
			return i;
		}
	}
	return -404;
}

void ObjectManager::Update(float deltaTime)
{
	for (int i = 0; i < m_objects.size(); i++)
	{
		m_objects[i]->Update(deltaTime);
	}
}

void ObjectManager::UpdateFrames(float deltaTime)
{
	for (int i = 0; i < m_objects.size(); i++)
	{
		m_objects[i]->UpdateSpriteTimer(deltaTime);
	}
}

GameObject* ObjectManager::Get(int index)
{
	return m_objects[index];
}

int ObjectManager::Size()
{
	return m_objects.size();
}
