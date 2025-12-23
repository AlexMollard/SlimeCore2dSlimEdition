#pragma once
#include <algorithm>
#include <cstdint>
#include <unordered_map>
#include <vector>

#include "Entities/GameObject.h"
#include "glm.hpp"
#include "Rendering/Texture.h" // Needed for CreateQuad
#include "Rendering/Text.h"
#include <string>

// Stable handle type for scripting/interop
using ObjectId = std::uint64_t;
static constexpr ObjectId InvalidObjectId = 0;

struct PersistentUIElement
{
	bool IsVisible = true;
	bool IsText = false;
	bool UseScreenSpace = false; // If true, Position is in screen pixels (0,0 = top-left). If false, uses world-space UI coordinates.

	// Transform
	glm::vec2 Position = { 0.0f, 0.0f };
	glm::vec2 Scale = { 1.0f, 1.0f }; // Doubles as FontSize relative to 48px
	glm::vec2 Anchor = { 0.5f, 0.5f }; // 0.0 = top-left, 0.5 = center, 1.0 = bottom-right
	glm::vec4 Color = { 1.0f, 1.0f, 1.0f, 1.0f };
	int Layer = 0;

	// Content
	std::string TextContent;
	Text* Font = nullptr;     // Pointer to SDF Atlas
	Texture* Image = nullptr; // Pointer to standard Texture
};

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

	// UI Creation
	ObjectId CreateUIElement(bool isText);
	PersistentUIElement* GetUIElement(ObjectId id);

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
	void RenderUI();

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
	std::unordered_map<ObjectId, PersistentUIElement> m_uiElements;
};
