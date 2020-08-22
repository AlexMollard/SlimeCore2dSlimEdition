#include "GameObject.h"
#include <string>
#include "gtc/quaternion.hpp"

GameObject::GameObject()
{
}

GameObject::~GameObject()
{
}

void GameObject::Update(float deltaTime)
{
}

void GameObject::Create(glm::vec3 pos, glm::vec3 color, glm::vec2 scale, int id)
{
	SetPos(pos);
	SetSpawn(pos);
	defaultColor = color;
	SetColor(color);
	SetScale(scale);
	SetID(id);
}

void GameObject::Respawn()
{
	SetVelocity(glm::vec2(0));
	SetPos(spawnPoint);
}

void GameObject::SetSpawn(glm::vec3 newSpawn)
{
	spawnPoint = newSpawn;
}

glm::vec3 GameObject::GetColor()
{
	return color;
}

void GameObject::SetColor(glm::vec3 newColor)
{
	color = newColor;
}

void GameObject::SetColor(float r, float g, float b)
{
	SetColor(glm::vec3(r, g, b));
}

void GameObject::SetShader(Shader* newShader)
{
	shader = newShader;
}

Shader* GameObject::GetShader()
{
	return shader;
}

void GameObject::SetRotate(float rotation)
{
	this->rotation = rotation;
	float updatedRotation = rotation * 3.141592f / 180.0f;

	float sin = glm::sin(updatedRotation);
	float cos = glm::cos(updatedRotation);

	float tx = 0;
	float ty = 1;
	normal.x = (cos * tx) - (sin * ty);
	normal.y = (sin * tx) + (cos * ty);

	glm::mat4 tempModel(1);

	tempModel = glm::rotate(tempModel, updatedRotation, glm::vec3(0, 0, 1));

	tempModel[0] *= scale[0];
	tempModel[1] *= scale[1];

	model = tempModel;
	SetPos(position);
}

void GameObject::SetID(int id)
{
	ID = id;
}

int GameObject::GetID()
{
	return ID;
}

Texture* GameObject::GetTexture()
{
	return texture;
}

void GameObject::SetTexture(Texture* tex)
{
	texture = tex;

	if (texture != nullptr)
	{
		if (!hasAnimation)
			SetScale(glm::vec2(texture->GetWidth() / 16, texture->GetHeight() / 16));
		else
			SetScale(glm::vec2(spriteWidth, texture->GetHeight()) / glm::vec2(spriteWidth));

		SetTextureWidth(texture->GetWidth());
	}
}

void GameObject::SetFrame(int Frame)
{
	frame = Frame;
}

void GameObject::AdvanceFrame()
{
	frame++;

	if (frame >= TextureWidth / spriteWidth)
	{
		frame = 0;
	}
}

int GameObject::GetFrame()
{
	return frame;
}

int GameObject::GetSpriteWidth()
{
	return spriteWidth;
}

void GameObject::SetSpriteWidth(int newWidth)
{
	spriteWidth = newWidth;
	if (TextureWidth > spriteWidth)
		hasAnimation = true;
}

int GameObject::GetTextureWidth()
{
	return TextureWidth;
}

void GameObject::SetTextureWidth(int newWidth)
{
	TextureWidth = newWidth;

	if (TextureWidth > spriteWidth)
		hasAnimation = true;
}

bool GameObject::GetHasAnimation()
{
	return hasAnimation;
}

void GameObject::SetHasAnimation(bool value)
{
	hasAnimation = value;
}

void GameObject::SetFrameRate(float frameRate)
{
	this->frameRate = frameRate;
}

float GameObject::GetFrameRate()
{
	return frameRate;
}

void GameObject::UpdateSpriteTimer(float deltaTime)
{
	if (hasAnimation)
	{
		frameRateTimer += deltaTime;

		if (frameRateTimer > frameRate)
		{
			frameRateTimer = 0;
			AdvanceFrame();
		}
	}
}

bool GameObject::GetIsPlayer()
{
	return isPlayer;
}

bool GameObject::GetRender()
{
	return render;
}

void GameObject::SetRender(bool value)
{
	render = value;
}

bool GameObject::GetMouseColliding()
{
	return (Input::GetMousePos().x > GetPos().x - (GetScale().x / 2) && Input::GetMousePos().x < GetPos().x + (GetScale().x / 2)
		&& Input::GetMousePos().y > GetPos().y - (GetScale().y / 2) && Input::GetMousePos().y < GetPos().y + (GetScale().y / 2));
}
