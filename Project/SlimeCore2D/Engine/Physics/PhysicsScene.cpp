#include "PhysicsScene.h"
#include "Core/Logger.h"

#include <iostream>
#include <sstream>
#include <tuple>
#include <unordered_map>

#include "Rendering/Renderer2D.h"

PhysicsScene::PhysicsScene()
      : timeStep(0.01f), gravity(glm::vec2(0, -3.24f))
{
}

void PhysicsScene::addActor(RigidBody* actor, std::string name, bool isKinematic)
{
	for (int i = 0; i < actors.size(); i++)
	{
		if (actors[i] == actor)
		{
			Logger::Warn("Actor already exists in physics scene");
			break;
		}
	}
	actor->name = name;
	actor->SetKinematic(isKinematic);

	if (isKinematic)
		actor->SetMass(999999999999999.99f);
	else
		dynamicActors.push_back(actor);

	actors.push_back(actor);
}

void PhysicsScene::addActor(std::vector<RigidBody*> actors)
{
	addActor(actors.data(), actors.size());
}

void PhysicsScene::addActor(RigidBody** actors, int amount)
{
	for (int i = 0; i < amount; i++)
		this->actors.push_back(actors[i]);
}

void PhysicsScene::removeActor(RigidBody* actor)
{
	auto location = std::find(actors.begin(), actors.end(), actor);
	actors.erase(location);
}

void PhysicsScene::update(float dt)
{
	// update physics at a fixed time step
	static float accumulatedTime = 0.0f;
	accumulatedTime += dt;
	while (accumulatedTime >= timeStep)
	{
		for (auto pActor: actors)
		{
			pActor->fixedUpdate(gravity, timeStep);
		}
		accumulatedTime -= timeStep;
	}

	for (int i = 0; i < actors.size(); i++)
	{
		RigidBody* object = actors[i];
		for (int y = i + 1; y < actors.size(); y++)
		{
			RigidBody* other = actors[y];

			if (other->GetKinematic() && object->GetKinematic())
				continue;

			auto result = CollisionManager::QuadVsQuad(object, other);

			if (glm::length(result) > 0.01)
			{
				other->SetNormal(result);

				other->ApplyOffSetToActor(object, glm::vec3(result, 0));
				other->resolveCollision(object);
			}
		}
	}
}

void PhysicsScene::Debug()
{
	//Renderer2D::BeginBatch();
	//Renderer2D::SetShader(Renderer2D::basicShader);

	//for (int i = 0; i < actors.size(); i++)
	//{
	//	glm::vec2 pos = (actors[i]->useBoundingBox) ? actors[i]->GetBoundingBox()->GetPos(actors[i]->GetPos()) : actors[i]->GetPos();
	//	glm::vec2 scale = (actors[i]->useBoundingBox) ? actors[i]->GetBoundingBox()->GetScale() : actors[i]->GetScale();
	//	Renderer2D::DrawQuad(glm::vec3(pos.x, pos.y, 0), scale, glm::vec4(1, 0, 0, 1));
	//}

	//Renderer2D::EndBatch();
	//Renderer2D::Flush();
}
