#pragma once

#include <vector>

#include "glm.hpp"
#include "Rendering/Texture.h"

class GameObject
{
public:
	GameObject();
	virtual ~GameObject();

	// Initialize the object data
	virtual void Create(glm::vec3 pos, glm::vec3 color, glm::vec2 scale, int id);

	// Core Loop Methods
	virtual void Update(float deltaTime);

	// Animation Logic
	void UpdateSpriteTimer(float deltaTime);
	void AdvanceFrame();

	// --- Hierarchy ---
	void AddChild(GameObject* child);
	void RemoveChild(GameObject* child);

	const std::vector<GameObject*>& GetChildren() const
	{
		return m_Children;
	}

	GameObject* GetParent() const
	{
		return m_Parent;
	}

	glm::mat4 GetLocalTransform() const;
	glm::mat4 GetWorldTransform() const;

	// --- Getters & Setters ---

	// Transform
	void SetPos(glm::vec3 pos)
	{
		m_Pos = pos;
	}

	glm::vec3 GetPos() const
	{
		return m_Pos;
	}

	void SetScale(glm::vec2 scale)
	{
		m_Scale = scale;
	}

	glm::vec2 GetScale() const
	{
		return m_Scale;
	}

	void SetLayer(int layer); // Helper to set Z component of Pos
	int GetLayer() const;

	void SetAnchor(glm::vec2 anchor)
	{
		m_Anchor = anchor;
	}

	glm::vec2 GetAnchor() const
	{
		return m_Anchor;
	}

	// Visuals
	float GetRotation() const
	{
		return rotationDegrees;
	}

	void SetRotation(float degrees)
	{
		rotationDegrees = degrees;
	}

	void SetColor(glm::vec3 color)
	{
		m_Color = color;
	}

	void SetColor(float r, float g, float b)
	{
		m_Color = glm::vec3(r, g, b);
	}

	glm::vec3 GetColor() const
	{
		return m_Color;
	}

	void SetTexture(Texture* tex);

	Texture* GetTexture() const
	{
		return m_Texture;
	}

	void SetRender(bool render)
	{
		m_IsActive = render;
	}

	bool GetRender() const
	{
		return m_IsActive;
	}

	// Animation Properties
	void SetFrame(int frame)
	{
		m_Frame = frame;
	}

	int GetFrame() const
	{
		return m_Frame;
	}

	void SetSpriteWidth(int width)
	{
		m_SpriteWidth = width;
	}

	int GetSpriteWidth() const
	{
		return m_SpriteWidth;
	}

	// Helper alias for scripting legacy support
	void SetTextureWidth(int width)
	{
		m_SpriteWidth = width;
	}

	void SetHasAnimation(bool active)
	{
		m_HasAnimation = active;
	}

	void SetFrameRate(float rate)
	{
		m_FrameRate = rate;
	}

	float GetFrameRate() const
	{
		return m_FrameRate;
	}

	// Identity
	int GetID() const
	{
		return m_ID;
	}

	void SetID(int id)
	{
		m_ID = id;
	}

protected:
	int m_ID = -1;
	bool m_IsActive = true;

	// Transform
	glm::vec3 m_Pos = glm::vec3(0.0f);
	glm::vec2 m_Scale = glm::vec2(1.0f);
	glm::vec2 m_Anchor = glm::vec2(0.5f); // 0.5 = Center, 0.0 = Bottom-Left

	// Appearance
	glm::vec3 m_Color = glm::vec3(1.0f);
	Texture* m_Texture = nullptr;
	// Optional: bool m_OwnsTexture = false; // If you want GameObject to delete texture on destruction

	// Animation
	bool m_HasAnimation = false;
	int m_Frame = 0;
	int m_SpriteWidth = 0;     // 0 means full texture
	float m_FrameRate = 12.0f; // Frames per second
	float m_Timer = 0.0f;

	float rotationDegrees = 0.0f;

	// Hierarchy
	GameObject* m_Parent = nullptr;
	std::vector<GameObject*> m_Children;
};
