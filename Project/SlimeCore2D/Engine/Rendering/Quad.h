#pragma once
#include "Game/Entities/GameObject.h"

class Quad : public GameObject
{
public:
	Quad();
	~Quad();

	void Update(float deltaTime);
};
