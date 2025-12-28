#include "Texture.h"
#include "Core/Logger.h"

#include <iostream>

// STB Image setup
// Make sure this is the only place STB_IMAGE_IMPLEMENTATION is defined in your project!
#ifndef STB_IMAGE_IMPLEMENTATION
#	define STB_IMAGE_IMPLEMENTATION
#endif
#include "stb_image.h"

Texture::Texture(const std::string& path, Filter filter, Wrap wrap)
      : m_FilePath(path), m_InternalFormat(0), m_DataFormat(0)
{
	// OpenGL expects 0.0 to be at the bottom, images usually have 0.0 at the top
	stbi_set_flip_vertically_on_load(1);

	// Ensure tight packing for image data (fixes skewing for non-power-of-4 widths)
	glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

	int width, height, channels;
	unsigned char* data = stbi_load(path.c_str(), &width, &height, &channels, 0);

	if (data)
	{
		m_Width = width;
		m_Height = height;

		// Determine formats
		if (channels == 4)
		{
			m_InternalFormat = GL_RGBA8;
			m_DataFormat = GL_RGBA;
		}
		else if (channels == 3)
		{
			m_InternalFormat = GL_RGB8;
			m_DataFormat = GL_RGB;
		}
		else if (channels == 1)
		{
			m_InternalFormat = GL_R8;
			m_DataFormat = GL_RED;
		}

		// Create Texture
		glGenTextures(1, &m_RendererID);
		glBindTexture(GL_TEXTURE_2D, m_RendererID);

		// Upload data
		glTexImage2D(GL_TEXTURE_2D, 0, m_InternalFormat, m_Width, m_Height, 0, m_DataFormat, GL_UNSIGNED_BYTE, data);

		// Filtering
		GLint glFilter = (filter == Filter::Nearest) ? GL_NEAREST : GL_LINEAR;
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, glFilter);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, glFilter); // Magnification usually looks best Nearest for pixel art, Linear for HD

		// Wrapping
		GLint glWrap = (wrap == Wrap::Repeat) ? GL_REPEAT : GL_CLAMP_TO_EDGE;
		if (wrap == Wrap::ClampToBorder)
			glWrap = GL_CLAMP_TO_BORDER;

		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, glWrap);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, glWrap);

		// Mipmaps (optional, good for 3D or scaled down sprites)
		if (filter == Filter::Linear)
		{
			glGenerateMipmap(GL_TEXTURE_2D);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
		}

// Debug Label
#ifdef _DEBUG
		std::string label = "Texture: " + path;
		glObjectLabel(GL_TEXTURE, m_RendererID, -1, label.c_str());
#endif

		stbi_image_free(data);
	}
	else
	{
		Logger::Error("ERROR::TEXTURE::LOAD_FAILED: " + path);
	}
}

Texture::Texture(uint32_t width, uint32_t height, GLenum internalFormat, Filter filter, Wrap wrap)
      : m_Width(width), m_Height(height), m_InternalFormat(internalFormat)
{
	// Determine data format from internal format
	if (internalFormat == GL_RGBA8)
		m_DataFormat = GL_RGBA;
	else if (internalFormat == GL_RGB8)
		m_DataFormat = GL_RGB;
	else if (internalFormat == GL_R8)
		m_DataFormat = GL_RED;
	else
		m_DataFormat = GL_RGBA; // Default

	glGenTextures(1, &m_RendererID);
	glBindTexture(GL_TEXTURE_2D, m_RendererID);

	// Filtering
	GLint glFilter = (filter == Filter::Nearest) ? GL_NEAREST : GL_LINEAR;
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, glFilter);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, glFilter);

	// Wrapping
	GLint glWrap = (wrap == Wrap::Repeat) ? GL_REPEAT : GL_CLAMP_TO_EDGE;
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, glWrap);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, glWrap);

	// Allocate memory but don't upload data yet
	glTexImage2D(GL_TEXTURE_2D, 0, m_InternalFormat, m_Width, m_Height, 0, m_DataFormat, GL_UNSIGNED_BYTE, nullptr);
}

Texture::~Texture()
{
	if (m_RendererID != 0)
		glDeleteTextures(1, &m_RendererID);
}

// Move Constructor
Texture::Texture(Texture&& other) noexcept
{
	m_RendererID = other.m_RendererID;
	m_Width = other.m_Width;
	m_Height = other.m_Height;
	m_FilePath = other.m_FilePath;
	m_InternalFormat = other.m_InternalFormat;
	m_DataFormat = other.m_DataFormat;

	// Invalidate source
	other.m_RendererID = 0;
}

// Move Assignment
Texture& Texture::operator=(Texture&& other) noexcept
{
	if (this != &other)
	{
		// Delete our current data
		if (m_RendererID != 0)
			glDeleteTextures(1, &m_RendererID);

		// Move data
		m_RendererID = other.m_RendererID;
		m_Width = other.m_Width;
		m_Height = other.m_Height;
		m_FilePath = other.m_FilePath;
		m_InternalFormat = other.m_InternalFormat;
		m_DataFormat = other.m_DataFormat;

		// Invalidate source
		other.m_RendererID = 0;
	}
	return *this;
}

void Texture::Bind(uint32_t slot) const
{
	glActiveTexture(GL_TEXTURE0 + slot);
	glBindTexture(GL_TEXTURE_2D, m_RendererID);
}

void Texture::Unbind() const
{
	glBindTexture(GL_TEXTURE_2D, 0);
}

void Texture::SetData(void* data, uint32_t size)
{
	// Safety check (basic) for RGBA
	// uint32_t bpp = m_DataFormat == GL_RGBA ? 4 : 3;
	// if (size != m_Width * m_Height * bpp) ...

	glBindTexture(GL_TEXTURE_2D, m_RendererID);
	glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, m_Width, m_Height, m_DataFormat, GL_UNSIGNED_BYTE, data);
}

void Texture::SetID(uint32_t id, uint32_t width, uint32_t height)
{
	if (m_RendererID != 0)
		glDeleteTextures(1, &m_RendererID);

	m_RendererID = id;
	m_Width = width;
	m_Height = height;
}
