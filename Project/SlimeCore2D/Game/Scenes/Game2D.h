#pragma once
#include "Core/Camera.h"
#include "Core/Input.h"
#include "Physics/PhysicsScene.h"
#include "Rendering/Renderer2D.h"

class Game2D
{
public:
	Game2D();
	~Game2D();

	void Init();

	void Update(float deltaTime);
	void Draw();

private:
	Input* m_inputManager = Input::GetInstance();
	PhysicsScene* m_physicsScene = nullptr;
	Camera* m_camera = nullptr;

	float m_timer = 0.0f;
};
