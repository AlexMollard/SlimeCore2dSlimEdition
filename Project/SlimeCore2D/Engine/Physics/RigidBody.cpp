#include "RigidBody.h"
#include <iostream>

void RigidBody::SetPos(glm::vec3 newPos)
{
	SetPos(newPos.x, newPos.y, newPos.z);
}

void RigidBody::SetPos(float x, float y, float z)
{
	localPosition.x = x;
	localPosition.y = y;
	localPosition.z = z;

	position = (parent == nullptr) ? localPosition : localPosition + parent->GetPos();

	if (children.size() > 0)
	{
		for (int i = 0; i < children.size(); i++)
		{
			children[i]->UpdatePos();
		}
	}
}

glm::vec3 RigidBody::GetPos()
{
	return position;
}

void RigidBody::ApplyForceToActor(RigidBody* obj, glm::vec2 force)
{
	if (!this->GetKinematic())
		this->ApplyForce(-force);

	if (obj->GetKinematic())
		return;

	obj->ApplyForce(force);
}

void RigidBody::ApplyForce(glm::vec2 force)
{
	velocity += force / mass;
}

void RigidBody::ApplyOffSetToActor(RigidBody* obj, glm::vec3 overlap)
{
	if (!(this->GetKinematic() || obj->GetKinematic()))
	{
		this->SetPos(GetPos() - overlap * 0.5f);
		obj->SetPos(obj->GetPos() + overlap * 0.5f);
		return;
	}

	if (this->GetKinematic())
	{
		obj->SetPos(obj->GetPos() + overlap);
		return;
	}

	this->SetPos(this->GetPos() - overlap);
	return;
}

glm::vec2 RigidBody::GetScale()
{
	return scale;
}

void RigidBody::SetScale(glm::vec2 newScale)
{
	scale = newScale;

	model[0] *= scale[0];
	model[1] *= scale[1];
}

RigidBody* RigidBody::GetParent()
{
	return parent;
}

void RigidBody::SetParent(RigidBody* newParent)
{
	newParent->AddChild(this);
	parent = newParent;
}

RigidBody* RigidBody::GetChild(int index)
{
	if (index < 0 || index > children.size())
	{
		std::cout << "Child dosent exist" << std::endl;
		return nullptr;
	}
	return children[index];
}

void RigidBody::AddChild(RigidBody* newChild)
{
	children.push_back(newChild);
}

void RigidBody::UpdatePos()
{
	position = localPosition + parent->GetPos();
}

RigidBody* RigidBody::GetSurroundTile(int index)
{
	return surroundingTiles[index];
}

bool RigidBody::GetIsColliding(RigidBody* other)
{
	return (GetBoundingBox()->GetIsColliding(*other->GetBoundingBox()));
}

void RigidBody::SetNormal(glm::vec2 newNormal)
{
	normal = newNormal;
}

glm::vec2 RigidBody::GetNormal()
{
	return normal;
}

glm::mat4 RigidBody::GetModel()
{
	return model;
}

void RigidBody::fixedUpdate(glm::vec2 gravity, float timeStep)
{
	ApplyForce(gravity * timeStep);
	position += glm::vec3(velocity, 0) * timeStep;

	SetPos(position);
}

void RigidBody::ApplyDrag(float timeStep)
{
	float dragX = (GetVelocity().x > 0) ? -drag : drag;
	float dragY = (GetVelocity().y > 0) ? -drag : drag;
	ApplyForce(glm::vec2(dragX, dragY) * timeStep);
}

BoundingBox* RigidBody::GetBoundingBox()
{
	return &boundingBox;
}

void RigidBody::SetBoundingBox(glm::vec2 offset, glm::vec2 scale)
{
	boundingBox.offset = offset;
	boundingBox.scale = scale;
	useBoundingBox = true;
}

void RigidBody::resolveCollision(RigidBody* actor2)
{
	if (normal == glm::vec2(0))
		return;

	normal = glm::normalize(normal);

	glm::vec2 relativeVelocity = actor2->GetVelocity() - GetVelocity();
	float elasticity = 0.90f;
	float j = glm::dot(-(1 + elasticity) * (relativeVelocity), normal) / glm::dot(normal, normal * ((1 / GetMass()) + (1 / actor2->GetMass())));
	glm::vec2 force = normal * j;
	ApplyForceToActor(actor2, force);
}