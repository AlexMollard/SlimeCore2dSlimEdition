#pragma once
#include <string>
#include <vector>

#include "BoundingBox.h"
#include <glm/glm.hpp> 

class RigidBody
{
public:
	std::string name = "";

	glm::vec2 GetVelocity()
	{
		return velocity;
	};

	void SetPos(glm::vec3 newPos);
	void SetPos(float x, float y, float z);
	glm::vec3 GetPos();

	void SetVelocity(glm::vec2 newVel)
	{
		velocity = newVel;
	};

	void AddVelocity(glm::vec2 newVel)
	{
		velocity += newVel;
	};

	void fixedUpdate(glm::vec2 gravity, float timeStep);

	bool GetKinematic()
	{
		return isKinematic;
	};

	void SetKinematic(bool value)
	{
		isKinematic = value;
	};

	void ApplyDrag(float timeStep);

	float GetMass()
	{
		return mass;
	};

	void SetMass(float newMass)
	{
		mass = newMass;
	};

	void ApplyForceToActor(RigidBody* obj, glm::vec2 force);
	void ApplyForce(glm::vec2 force);

	void ApplyOffSetToActor(RigidBody* obj, glm::vec3 overlap);
	void resolveCollision(RigidBody* actor2);

	bool GetIsColliding(RigidBody* other);

	virtual void SetNormal(glm::vec2 newNormal);
	glm::vec2 GetNormal();

	glm::mat4 GetModel();

	BoundingBox* GetBoundingBox();
	void SetBoundingBox(glm::vec2 offset, glm::vec2 scale);

	glm::vec2 GetScale();
	void SetScale(glm::vec2 newScale);

	RigidBody* GetParent();
	void SetParent(RigidBody* newParent);

	RigidBody* GetChild(int index);
	void AddChild(RigidBody* newChild);

	void UpdatePos();

	RigidBody* GetSurroundTile(int index);

	// Make these private
	bool isKinematic = false;
	int ID = -404;
	bool useBoundingBox = false;

protected:
	glm::vec3 position = glm::vec3(0);
	glm::vec3 localPosition = glm::vec3(0);
	glm::vec2 scale = glm::vec2(1);
	glm::vec2 normal = glm::vec2(1);

	BoundingBox boundingBox;

	RigidBody* parent = nullptr;
	std::vector<RigidBody*> children;

	RigidBody* surroundingTiles[9];

	glm::vec2 velocity = glm::vec2(0);
	glm::mat4 model = glm::mat4(1);
	float rotation = 0.0f;

	float drag = 3.0f;
	float mass = 1.0f;
};
