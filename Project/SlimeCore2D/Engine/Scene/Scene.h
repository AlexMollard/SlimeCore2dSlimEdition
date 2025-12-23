#pragma once

#include <vector>
#include <unordered_map>
#include <string>
#include "Registry.h"
#include "Core/Camera.h"
#include "Rendering/Text.h"

// Stable handle type for scripting/interop
using ObjectId = Entity;
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

class Scene
{
public:
	Scene();
	~Scene();

	static Scene* GetActiveScene();

	// --- Object Management ---
	ObjectId CreateEntity();
	ObjectId CreateGameObject(glm::vec3 pos, glm::vec2 size, glm::vec3 color);
	ObjectId CreateQuad(glm::vec3 pos, glm::vec2 size, glm::vec3 color);
	ObjectId CreateQuad(glm::vec3 pos, glm::vec2 size, Texture* tex);
	
	void DestroyObject(ObjectId id);
	bool IsAlive(ObjectId id) const;

	Registry& GetRegistry() { return m_Registry; }

	// --- UI Management ---
	ObjectId CreateUIElement(bool isText);
	PersistentUIElement* GetUIElement(ObjectId id);
	void RenderUI();

	// --- Core Loop ---
	void Update(float deltaTime);
	void Render(Camera& camera);

	// --- Stats ---
	int GetObjectCount() const { return (int)m_ActiveEntities.size(); }
	ObjectId GetIdAtIndex(int index) const;

private:
	static Scene* s_ActiveScene;

	Registry m_Registry;
	std::vector<Entity> m_ActiveEntities; // Maintain list for index access and cleanup
	
	// ID Management
	ObjectId m_NextUIId = 100000; // Start UI IDs high to avoid collision with Entity IDs for now
	std::unordered_map<ObjectId, PersistentUIElement> m_UIElements;
};

