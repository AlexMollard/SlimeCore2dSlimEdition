#pragma once
#include "ObjectManager.h"
#include "Cell.h"

class Snake
{
public: 

	Snake();
	~Snake(); 

	void SpawnFood(); 

	void SpawnHead(); 

	void UpdatePosition(); 

	void SpawnTail(); 

	void Init();

	void Update(float deltaTime);
	void Draw();


private:
	Renderer2D* _renderer = nullptr;
	ObjectManager* _objManager = nullptr;
	Input* _inputManager = Input::GetInstance();
	Camera* _camera = nullptr;


	Cell** _grid = nullptr;

	glm::vec2 _foodPos; 

	int _tailLength = 0; 
	glm::vec2 _bodyPos[50]; 

	glm::vec2 _direction = glm::vec2(0.0f, 1.0f); 

	float _timer; 
};

