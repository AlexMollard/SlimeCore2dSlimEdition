#pragma once
#include "GameObject.h"
#include "glm.hpp"
class Cell
{
public:
	enum class STATE
	{
		EMPTY,
		FOOD,
		HEAD,
		TAIL
	};
	Cell();
	~Cell(); 
	void SetState(STATE newState) { _currentState = newState; }
	STATE GetState() { return _currentState; }
	GameObject* _cell = nullptr;

private:
	STATE _currentState = STATE::EMPTY;

	
};

