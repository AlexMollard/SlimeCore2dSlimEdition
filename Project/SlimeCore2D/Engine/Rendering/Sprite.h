#pragma once
#include "Texture.h"

class Sprite : public Texture
{
	Sprite(std::string dir, int SpriteWidth, int SpriteHeight);
	~Sprite();
};
