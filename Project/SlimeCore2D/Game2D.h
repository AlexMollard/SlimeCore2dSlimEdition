#pragma once
#include "ObjectManager.h"
#include "PhysicsScene.h"
#include "Snake.h"
#include "AStar.h"

class Game2D
{
public:
	Game2D();
	~Game2D();

	void Init();

	void Update(float deltaTime);
	void Draw();

private:
	Renderer2D* renderer = nullptr;
	ObjectManager* objectManager = nullptr;
	Input* inputManager = Input::GetInstance();
	PhysicsScene* physicsScene = nullptr;
	Camera* camera = nullptr;
	GameObject* testObject = nullptr;
	Snake* snakeGame = nullptr;
	AStar* aStartTest = nullptr; 



	float timer = 0.0f;
};
