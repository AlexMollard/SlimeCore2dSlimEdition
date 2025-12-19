#include "BoundingBox.h"

BoundingBox::BoundingBox()
{
}

BoundingBox::~BoundingBox()
{
}

void BoundingBox::UpdateQuadBoundingBox(glm::vec2 pos, glm::vec2 size)
{
	max = glm::vec2(pos.x + (size.x * 0.5f), pos.y + (size.y * 0.5f));
	min = glm::vec2(pos.x - (size.x * 0.5f), pos.y - (size.y * 0.5f));
}

bool BoundingBox::GetIsColliding(BoundingBox& other)
{
	bool collisionX = (other.min.x > max.x || other.max.x < min.x);

	bool collisionY = (other.min.y > max.y || other.max.y < min.y);

	return !(collisionX || collisionY);
}

bool BoundingBox::GetMouseColliding()
{
	glm::vec2 mousePos = inputManager->GetMousePos();

	bool collisionX = (mousePos.x > min.x && max.x > mousePos.x);

	bool collisionY = (mousePos.y > min.y && max.y > mousePos.y);

	return (collisionX && collisionY);
}

glm::vec2& BoundingBox::GetMax()
{
	return max;
}

glm::vec2& BoundingBox::GetMin()
{
	return min;
}

glm::vec2 BoundingBox::GetPos(glm::vec3 objectPos)
{
	return objectPos + glm::vec3(offset, 0);
}

glm::vec2 BoundingBox::GetScale()
{
	return scale;
}
