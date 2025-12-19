#pragma once
#include "glm.hpp"
#include "Core/Input.h"

class BoundingBox
{
public:
	BoundingBox();
	~BoundingBox();

	void UpdateQuadBoundingBox(glm::vec2 pos, glm::vec2 size);

	bool GetIsColliding(BoundingBox& other);

	bool GetMouseColliding();

	glm::vec2& GetMax();
	glm::vec2& GetMin();

	glm::vec2 GetPos(glm::vec3 objectPos);
	glm::vec2 GetScale();

	glm::vec2 offset = glm::vec2(0);
	glm::vec2 scale = glm::vec2(0);
private:

	Input* inputManager = Input::GetInstance();

	glm::vec2 max = glm::vec2(0);
	glm::vec2 min = glm::vec2(0);
};
