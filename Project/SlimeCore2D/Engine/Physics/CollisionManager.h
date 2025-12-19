#pragma once
#include "RigidBody.h"

class CollisionManager
{
public:
	CollisionManager();
	~CollisionManager();

	static glm::vec2 QuadVsQuad(RigidBody* rbOne, RigidBody* rbTwo);
};
