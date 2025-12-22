#pragma once

#include "GameObject.h"

class Quad : public GameObject
{
public:
	Quad();
	virtual ~Quad();

	// In the future, if Quads need specific update logic (different from standard objects),
	// you can override Update here.
	// virtual void Update(float deltaTime) override;
};
