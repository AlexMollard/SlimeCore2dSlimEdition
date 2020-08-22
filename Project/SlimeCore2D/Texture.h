#pragma once
#include <string>
#include "glew.h"
#include "glfw3.h"

class Texture
{
public:
	Texture(std::string dir);
	Texture(unsigned int* id);
	Texture() {};
	~Texture();

	void load(std::string dir);
	void Bind() { glBindTexture(GL_TEXTURE_2D, textureID); };
	unsigned int GetID() { return textureID; };
	void SetID(unsigned int newID) { textureID = newID; };

	int GetWidth();
	int GetHeight();

protected:
	unsigned int textureID = 0;
	int width = 0;
	int height = 0;
	int nrChannels = 4;
};
