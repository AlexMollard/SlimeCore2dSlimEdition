#include "CollisionManager.h"

CollisionManager::CollisionManager()
{
}

CollisionManager::~CollisionManager()
{
}

glm::vec2 CollisionManager::QuadVsQuad(RigidBody* rbOne, RigidBody* rbTwo)
{
	glm::vec2 overlapVector = { 0, 0 };
	float overlap = 999999999999.0f;

	glm::vec2 posOne = (rbOne->useBoundingBox) ? rbOne->GetBoundingBox()->GetPos(rbOne->GetPos()) : rbOne->GetPos();
	glm::vec2 posTwo = (rbTwo->useBoundingBox) ? rbTwo->GetBoundingBox()->GetPos(rbTwo->GetPos()) : rbTwo->GetPos();

	glm::vec2 scaleOne = (rbOne->useBoundingBox) ? rbOne->GetBoundingBox()->GetScale() : rbOne->GetScale();
	glm::vec2 scaleTwo = (rbTwo->useBoundingBox) ? rbTwo->GetBoundingBox()->GetScale() : rbTwo->GetScale();

	glm::vec2 one_Min = posOne - (scaleOne * 0.5f);
	glm::vec2 one_Max = posOne + (scaleOne * 0.5f);
	glm::vec2 two_Min = posTwo - (scaleTwo * 0.5f);
	glm::vec2 two_Max = posTwo + (scaleTwo * 0.5f);

	if (one_Max.x > two_Min.x)
	{
		float localOverLap = abs(one_Max.x - two_Min.x);
		if (localOverLap < overlap)
		{
			overlap = localOverLap;
			overlapVector = glm::vec2(-overlap, 0);
		}
	}
	else
		return glm::vec2(0.0f);

	if (two_Max.x > one_Min.x)
	{
		float localOverLap = abs(two_Max.x - one_Min.x);
		if (localOverLap < overlap)
		{
			overlap = localOverLap;
			overlapVector = glm::vec2(overlap, 0);
		}
	}
	else
		return glm::vec2(0.0f);

	if (one_Max.y > two_Min.y)
	{
		float localOverLap = abs(one_Max.y - two_Min.y);
		if (localOverLap < overlap)
		{
			overlap = localOverLap;
			overlapVector = glm::vec2(0, -overlap);
		}
	}
	else
		return glm::vec2(0.0f);

	if (two_Max.y > one_Min.y)
	{
		float localOverLap = abs(two_Max.y - one_Min.y);
		if (localOverLap < overlap)
		{
			overlap = localOverLap;
			overlapVector = glm::vec2(0, overlap);
		}
	}
	else
		return glm::vec2(0.0f);

	return overlapVector;
}
