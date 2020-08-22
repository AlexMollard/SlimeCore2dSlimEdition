#pragma once
#include "ObjectManager.h"
#include "Cell.h"

class Snake
{
public: 
	Snake();
	~Snake(); 

	void SpawnFood(); 

	void SpawnSnake(); 

	void Init();

	void Update(float deltaTime);
	void Draw();


private:
	Renderer2D* _renderer = nullptr;
	ObjectManager* _objManager = nullptr;
	Input* _inputManager = Input::GetInstance();
	Camera* _camera = nullptr;

	GameObject* _snakeHead = nullptr; 
	GameObject** _snakeTails = nullptr; 

	Cell** _grid = nullptr;
};

