#pragma once
#include "ObjectManager.h"
#include "Node.h"
#include <vector>
class AStar
{
public: 
	AStar(Camera* cam, Renderer2D* rend, ObjectManager* objMan); 

	~AStar(); 

private:
	Node** _grid = nullptr; 

	Renderer2D* _renderer = nullptr;
	ObjectManager* _objManager = nullptr;
	Input* _inputManager = Input::GetInstance();
	Camera* _camera = nullptr;

	//Colours
	glm::vec3 _gridColor = glm::vec3(0.5f);
};

