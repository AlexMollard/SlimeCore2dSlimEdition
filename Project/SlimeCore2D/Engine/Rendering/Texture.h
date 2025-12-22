#pragma once

#include <string>

#include "glew.h"

class Texture
{
public:
	enum class Filter
	{
		Nearest,
		Linear
	};
	enum class Wrap
	{
		Repeat,
		ClampToEdge,
		ClampToBorder
	};

	// 1. Load from file
	Texture(const std::string& path, Filter filter = Filter::Linear, Wrap wrap = Wrap::Repeat);

	// 2. Create empty (for Framebuffers or manual data upload like Fonts)
	// format: GL_RGBA8, GL_R8, etc.
	Texture(uint32_t width, uint32_t height, GLenum internalFormat = GL_RGBA8, Filter filter = Filter::Linear, Wrap wrap = Wrap::ClampToEdge);

	// 3. Destructor
	~Texture();

	// --- Rule of Five (Prevent Copying, Allow Moving) ---
	Texture(const Texture&) = delete;            // Delete Copy Constructor
	Texture& operator=(const Texture&) = delete; // Delete Copy Assignment

	Texture(Texture&& other) noexcept;            // Move Constructor
	Texture& operator=(Texture&& other) noexcept; // Move Assignment
	// ----------------------------------------------------

	void Bind(uint32_t slot = 0) const;
	void Unbind() const;

	// Upload data manually (useful for Font Atlas)
	void SetData(void* data, uint32_t size);

	// Getters
	inline uint32_t GetWidth() const
	{
		return m_Width;
	}

	inline uint32_t GetHeight() const
	{
		return m_Height;
	}

	inline uint32_t GetID() const
	{
		return m_RendererID;
	}

	// For compatibility with manual creation (like in your Text.cpp if needed)
	void SetID(uint32_t id, uint32_t width, uint32_t height);

	// Equality operator
	bool operator==(const Texture& other) const
	{
		return m_RendererID == other.m_RendererID;
	}

private:
	uint32_t m_RendererID = 0;
	std::string m_FilePath;
	uint32_t m_Width = 0;
	uint32_t m_Height = 0;
	GLenum m_InternalFormat = 0; // How GPU stores it (GL_RGBA8)
	GLenum m_DataFormat = 0;     // How we upload it (GL_RGBA)
};
