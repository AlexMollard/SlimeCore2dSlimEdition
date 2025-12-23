#include "GameObject.h"

#include <iostream>
#include <algorithm>
#include <gtc/matrix_transform.hpp>

GameObject::GameObject()
{
	// Initialize defaults
	m_Pos = glm::vec3(0.0f, 0.0f, 0.0f);
	m_Scale = glm::vec2(1.0f, 1.0f);
	m_Color = glm::vec3(1.0f, 1.0f, 1.0f);
	m_Anchor = glm::vec2(0.5f, 0.5f);
}

GameObject::~GameObject()
{
	// NOTE: GameObject does not strictly own the Texture pointer
	// (usually managed by ResourceManager), so we do not delete m_Texture here.
	// If you created a texture via 'new' specifically for this object in exports,
	// you must ensure it is cleaned up elsewhere or implement an ownership flag.
}

void GameObject::Create(glm::vec3 pos, glm::vec3 color, glm::vec2 scale, int id)
{
	m_Pos = pos;
	m_Color = color;
	m_Scale = scale;
	m_ID = id;
	m_IsActive = true;

	m_Frame = 0;
	m_SpriteWidth = 0;
	m_HasAnimation = false;
}

void GameObject::Update(float deltaTime)
{
	// Base object logic
	if (m_HasAnimation)
	{
		UpdateSpriteTimer(deltaTime);
	}
}

void GameObject::UpdateSpriteTimer(float deltaTime)
{
	if (!m_HasAnimation || !m_Texture)
		return;

	m_Timer += deltaTime;

	// Calculate time required for one frame
	float timePerFrame = (m_FrameRate > 0.0f) ? (1.0f / m_FrameRate) : 0.0f;

	if (timePerFrame > 0.0f && m_Timer >= timePerFrame)
	{
		// Consume timer (keeping remainder for smooth updates)
		while (m_Timer >= timePerFrame)
		{
			m_Timer -= timePerFrame;
			AdvanceFrame();
		}
	}
}

void GameObject::AdvanceFrame()
{
	if (!m_Texture || m_SpriteWidth <= 0)
		return;

	// Determine max frames based on texture width
	int texWidth = m_Texture->GetWidth();
	int maxFrames = texWidth / m_SpriteWidth;

	if (maxFrames <= 0)
		maxFrames = 1;

	m_Frame++;

	// Loop animation
	if (m_Frame >= maxFrames)
	{
		m_Frame = 0;
	}
}

void GameObject::SetLayer(int layer)
{
	// We use the Z coordinate for layering.
	// Standard mapping: 0.1f * layer, or just raw layer if orthogonal projection handles it.
	m_Pos.z = static_cast<float>(layer) * 0.1f;
}

int GameObject::GetLayer() const
{
	// Reverse the logic above
	if (std::abs(m_Pos.z) < 0.0001f)
		return 0;
	return static_cast<int>(m_Pos.z * 10.0f);
}

void GameObject::SetTexture(Texture* tex)
{
	m_Texture = tex;
	// Reset animation frame when texture changes to be safe
	m_Frame = 0;
}

void GameObject::AddChild(GameObject* child)
{
	child->m_Parent = this;
	m_Children.push_back(child);
}

void GameObject::RemoveChild(GameObject* child)
{
	auto it = std::find(m_Children.begin(), m_Children.end(), child);
	if (it != m_Children.end())
	{
		(*it)->m_Parent = nullptr;
		m_Children.erase(it);
	}
}

glm::mat4 GameObject::GetLocalTransform() const
{
	return glm::translate(glm::mat4(1.0f), m_Pos)
		* glm::rotate(glm::mat4(1.0f), glm::radians(rotationDegrees), { 0.0f, 0.0f, 1.0f })
		* glm::scale(glm::mat4(1.0f), { m_Scale.x, m_Scale.y, 1.0f });
}

glm::mat4 GameObject::GetWorldTransform() const
{
	if (m_Parent)
		return m_Parent->GetWorldTransform() * GetLocalTransform();
	return GetLocalTransform();
}

