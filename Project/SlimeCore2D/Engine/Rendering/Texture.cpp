#include "Texture.h"
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"
#include <string>

Texture::Texture(std::string dir)
{
	// Create and bind texture ID
	glGenTextures(1, &m_textureID);
	glBindTexture(GL_TEXTURE_2D, m_textureID);

	// Set Wrapping mode
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

	// Set texture filtering
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);

	// Load Image and generate mipmaps
	unsigned char* data = stbi_load(dir.c_str(), &m_width, &m_height, &m_nrChannels, 0);

	if (data)
	{
		glTexImage2D(GL_TEXTURE_2D, 0, m_nrChannels != 4 ? GL_RGB : GL_RGBA, m_width, m_height, 0, m_nrChannels != 4 ? GL_RGB : GL_RGBA, GL_UNSIGNED_BYTE, data);
	}
	else
	{
		printf("Failed to load texture\n");
	}
	stbi_image_free(data);
}

Texture::Texture(unsigned int* id)
{
	this->m_textureID = *id;

	// Get texture properties
	glBindTexture(GL_TEXTURE_2D, m_textureID);
	glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_WIDTH, &m_width);
	glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_HEIGHT, &m_height);
	GLint format;
	glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_INTERNAL_FORMAT, &format);
	if (format == GL_RGB)
		m_nrChannels = 3;
	else if (format == GL_RGBA)
		m_nrChannels = 4;
	else
		m_nrChannels = 0;


	// Name texture so render doc has info
	std::string name = "Copied Texture " + std::to_string(m_textureID);
	glObjectLabel(GL_TEXTURE, m_textureID, -1, name.c_str());

}

Texture::~Texture()
{
	if (m_textureID != 0)
		glDeleteTextures(1, &m_textureID);
	m_textureID = 0;
}

void Texture::load(std::string dir)
{
	// Create and bind texture ID
	glGenTextures(1, &m_textureID);
	glBindTexture(GL_TEXTURE_2D, m_textureID);

	// Set Wrapping mode
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

	// Set texture filtering
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

	// Load Image and generate mipmaps
	unsigned char* data = stbi_load(dir.c_str(), &m_width, &m_height, &m_nrChannels, 0);

	if (data)
	{
		glTexImage2D(GL_TEXTURE_2D, 0, m_nrChannels != 4 ? GL_RGB : GL_RGBA, m_width, m_height, 0, m_nrChannels != 4 ? GL_RGB : GL_RGBA, GL_UNSIGNED_BYTE, data);
	}
	else
	{
		printf("Failed to load texture: %c\n", dir.c_str());
	}
	stbi_image_free(data);
}

int Texture::GetWidth()
{
	return m_width;
}

int Texture::GetHeight()
{
	return m_height;
}
