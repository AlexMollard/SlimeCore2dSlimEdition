#pragma once
#include "Entities/Snake.h"
#include "Physics/PhysicsScene.h"

class Game2D
{
public:
	Game2D();
	~Game2D();

	void Init();

	void Update(float deltaTime);
	void Draw();

private:
	Renderer2D* m_renderer = nullptr;
	Input* m_inputManager = Input::GetInstance();
	PhysicsScene* m_physicsScene = nullptr;
	Camera* m_camera = nullptr;
	GameObject* m_testObject = nullptr;
	Snake* m_snakeGame = nullptr;

	float m_timer = 0.0f;
};
