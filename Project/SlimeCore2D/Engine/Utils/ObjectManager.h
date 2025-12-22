#pragma once
#include <algorithm>
#include <cstdint>
#include <unordered_map>
#include <vector>

#include "Entities/GameObject.h"
#include "glm.hpp"
#include "Rendering/Texture.h" // Needed for CreateQuad

// Stable handle type for scripting/interop
using ObjectId = std::uint64_t;
static constexpr ObjectId InvalidObjectId = 0;

class ObjectManager
{
public:
	// Singleton lifecycle
	static void Create();
	static void Destroy();
	static ObjectManager& Get();
	static bool IsCreated();

	// Non-copyable
	ObjectManager(const ObjectManager&) = delete;
	ObjectManager& operator=(const ObjectManager&) = delete;

	// Object creation
	ObjectId CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color);
	ObjectId CreateQuad(glm::vec3 pos, glm::vec2 size = glm::vec2(1), glm::vec3 color = glm::vec3(1));
	ObjectId CreateQuad(glm::vec3 pos, glm::vec2 size, Texture* tex);

	// Destruction
	void DestroyObject(ObjectId id);
	bool IsAlive(ObjectId id) const;

	// Accessors
	GameObject* Get(ObjectId id);
	const GameObject* Get(ObjectId id) const;

	// Logic
	void Update(float deltaTime);
	void UpdateFrames(float deltaTime);

	// Call this inside your Game Loop between Renderer2D::BeginScene and EndScene
	void RenderAll();

	// Stats
	int Size() const;
	ObjectId GetIdAtIndex(int index) const;

private:
	ObjectManager();
	~ObjectManager();

	static ObjectManager* s_instance;

	ObjectId m_nextId = 1;

	// Storage
	std::unordered_map<ObjectId, GameObject*> m_objectsById;
	std::vector<GameObject*> m_objects;
};
