#pragma once

#include "GameObject.h"
#include "glm.hpp"

class Cell
{
public:
	GameObject* visual = nullptr;

	void SetColor(const glm::vec3& c)
	{
		if (visual) visual->SetColor(c);
	}
};
