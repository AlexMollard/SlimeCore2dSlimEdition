#pragma once
#include "Engine/Physics/RigidBody.h"
#include "Engine/Rendering/Shader.h"
#include "Engine/Rendering/Texture.h"

class GameObject : public RigidBody
{
public:
	GameObject() = default;
	virtual ~GameObject() = default;

	virtual void Update(float deltaTime);
	virtual void Create(glm::vec3 pos, glm::vec3 color, glm::vec2 scale, int id);

	void Respawn();
	void SetSpawn(glm::vec3 newSpawn);

	glm::vec3 GetColor();
	void SetColor(glm::vec3 newColor);
	void SetColor(float r, float g, float b);

	void SetShader(Shader* newShader);
	Shader* GetShader();

	void SetID(int id);
	int GetID();

	Texture* GetTexture();
	void SetTexture(Texture* tex);

	void SetRotate(float rotation);

	// Sprite Stuff (Probably should be in own class)
	void SetFrame(int Frame);
	void AdvanceFrame();
	int GetFrame();

	int GetSpriteWidth();
	void SetSpriteWidth(int newWidth);

	int GetTextureWidth();
	void SetTextureWidth(int newWidth);

	bool GetHasAnimation();
	void SetHasAnimation(bool value);

	void SetFrameRate(float frameRate);
	float GetFrameRate();

	void UpdateSpriteTimer(float deltaTime);

	bool GetIsPlayer();

	bool GetRender();
	void SetRender(bool value);

	bool GetMouseColliding();

	void SetLayer(int layer);
	int GetLayer();

	void SetAnchor(glm::vec2 anchor);
	glm::vec2 GetAnchor();

protected:
	glm::vec3 m_color = glm::vec3(1);
	glm::vec3 m_defaultColor = glm::vec3(1);
	glm::vec3 m_spawnPoint = glm::vec3(0);

	int m_layer = 0;
	glm::vec2 m_anchor = glm::vec2(0.5f, 0.5f);

	bool m_render = true;
	bool m_isPlayer = false;

	Texture* m_texture = nullptr;
	Shader* m_shader = nullptr;

	// Sprite Stuff
	int m_frame = 0;
	int m_spriteWidth = 16;
	int m_textureWidth = 16;
	float m_frameRate = 0.5f;
	float m_frameRateTimer = 0.0f;
	bool m_hasAnimation = false;
};
