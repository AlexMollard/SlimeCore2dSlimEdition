#pragma once

#include "GameObject.h"
#include "glm.hpp"
#include "Utils/ObjectManager.h"

class Cell
{
public:
	ObjectId visual = 0;

	void SetColor(const glm::vec3& c)
	{
		if (visual)
		{
			ObjectManager& objMgr = ObjectManager::Get();
			objMgr.Get(visual)->SetColor(c);
		}
	}
};
