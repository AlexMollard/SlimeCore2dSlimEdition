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

	void Bind()
	{
		glBindTexture(GL_TEXTURE_2D, m_textureID);
	};

	unsigned int GetID()
	{
		return m_textureID;
	};

	void SetID(unsigned int newID)
	{
		m_textureID = newID;
	};

	int GetWidth();
	int GetHeight();

protected:
	unsigned int m_textureID = 0;
	int m_width = 0;
	int m_height = 0;
	int m_nrChannels = 4;
};
