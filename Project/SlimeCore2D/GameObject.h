#pragma once
#include "RigidBody.h"
#include "Shader.h"
#include "Texture.h"

class GameObject : public RigidBody
{
public:
	GameObject();
	virtual ~GameObject();

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


protected:
	glm::vec3 color = glm::vec3(1);
	glm::vec3 defaultColor = glm::vec3(1);
	glm::vec3 spawnPoint = glm::vec3(0);

	bool render = true;
	bool isPlayer = false;

	Texture* texture = nullptr;
	Shader* shader = nullptr;


	// Sprite Stuff
	int frame = 0;
	int spriteWidth = 16;
	int TextureWidth = 16;
	float frameRate = 0.5f;
	float frameRateTimer = 0.0f;
	bool hasAnimation = false;
};
