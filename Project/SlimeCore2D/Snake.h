#pragma once
#include "ObjectManager.h"
#include "Cell.h"

class Snake
{
public: 

	Snake(Camera* cam, Renderer2D* rend, ObjectManager* objMan);

	~Snake(); 

	void SpawnFood(); 

	void UpdatePosition(); 

	void SpawnTail(); 

	void Update(float deltaTime);

	int GetScore() { return _score; }

	void Death();
	void Restart();
private:
	Renderer2D* _renderer = nullptr;
	ObjectManager* _objManager = nullptr;
	Input* _inputManager = Input::GetInstance();
	Camera* _camera = nullptr;


	Cell** _grid = nullptr;

	glm::vec2 _foodPos; 

	int _tailLength = 0; 
	int _score = 0; 
	std::vector<glm::vec2> _bodyPos; 

	glm::vec2 _direction = glm::vec2(0);
	glm::vec2 _lastDirection = glm::vec2(0);
	glm::vec2 spawnPos;
	float updateLength = 0;

	float _timer; 


	bool isDead = false;

	//Colours
	glm::vec3 gridColor = glm::vec3(0.25f);
	glm::vec3 snakeColor = glm::vec3(0.1f,0.75f,0.15f);
	glm::vec3 foodColor = glm::vec3(0.75f,0.15f,0.2f);
};

