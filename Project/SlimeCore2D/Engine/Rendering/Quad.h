#pragma once
#include "Entities/GameObject.h"

class Quad : public GameObject
{
public:
	Quad();
	~Quad();

	void Update(float deltaTime);
};
