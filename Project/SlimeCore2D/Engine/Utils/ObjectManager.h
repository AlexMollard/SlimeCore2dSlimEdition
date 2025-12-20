#pragma once
#include <cstdint>
#include <unordered_map>
#include <vector>

#include "Rendering/Renderer2D.h"

// Stable handle type for scripting/interop
using ObjectId = std::uint64_t;
static constexpr ObjectId InvalidObjectId = 0;

class ObjectManager
{
public:
	// Singleton lifecycle
	static void Create(Renderer2D* renderer, bool ownsRenderer = false);
	static void Destroy();
	static ObjectManager& Get();
	static bool IsCreated();

	// Non-copyable
	ObjectManager(const ObjectManager&) = delete;
	ObjectManager& operator=(const ObjectManager&) = delete;

	// Object creation (return stable IDs)
	ObjectId CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color);
	ObjectId CreateQuad(glm::vec3 pos, glm::vec2 size = glm::vec2(1), glm::vec3 color = glm::vec3(1));
	ObjectId CreateQuad(glm::vec3 pos, glm::vec2 size, Texture* tex);

	// Destruction / lookup
	void DestroyObject(ObjectId id);
	bool IsAlive(ObjectId id) const;

	GameObject* Get(ObjectId id);
	const GameObject* Get(ObjectId id) const;

	// Frame update
	void Update(float deltaTime);
	void UpdateFrames(float deltaTime);

	// Debug / stats
	int Size() const;
	ObjectId GetIdAtIndex(int index) const;
private:
	ObjectManager(Renderer2D* renderer, bool ownsRenderer);
	~ObjectManager();

	static ObjectManager* s_instance;

private:
	Renderer2D* m_renderer = nullptr;
	bool m_ownsRenderer = false;

	ObjectId m_nextId = 1;

	// Stable ID -> pointer
	std::unordered_map<ObjectId, GameObject*> m_objectsById;

	// Maintain an iteration list (update order); pointers remain valid until deletion.
	std::vector<GameObject*> m_objects;
};
