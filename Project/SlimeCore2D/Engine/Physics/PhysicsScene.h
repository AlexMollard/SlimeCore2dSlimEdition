#pragma once
#include <algorithm>
#include <functional>
#include <vector>

#include "CollisionManager.h"
#include "glm.hpp"
#include "RigidBody.h"

class PhysicsScene
{
public:
	PhysicsScene();
	~PhysicsScene() {};

	void addActor(RigidBody* actor, std::string name, bool isKinematic = false);
	void addActor(std::vector<RigidBody*> actors);
	void addActor(RigidBody** actors, int amount);
	void removeActor(RigidBody* actor);

	void update(float dt);
	void Debug();

	void setGravity(const glm::vec3 gravity)
	{
		this->gravity = gravity;
	}

	glm::vec2 getGravity() const
	{
		return gravity;
	}

	void setTimeStep(const float timeStep)
	{
		this->timeStep = timeStep;
	}

	float getTimeStep() const
	{
		return timeStep;
	}

protected:
	glm::vec2 gravity;
	float timeStep = 0.01f;
	std::vector<RigidBody*> actors;
	std::vector<RigidBody*> dynamicActors;
};
