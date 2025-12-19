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

	void SetState(STATE newState)
	{
		m_currentState = newState;
	}

	STATE GetState()
	{
		return m_currentState;
	}

	GameObject* m_cell = nullptr;

private:
	STATE m_currentState = STATE::EMPTY;
};
