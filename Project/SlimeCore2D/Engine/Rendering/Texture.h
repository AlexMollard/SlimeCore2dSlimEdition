#pragma once

#include <string>

#include "RefCntAutoPtr.hpp"
#include "RenderDevice.h"

using namespace Diligent;

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

	Texture(const std::string& path, Filter filter = Filter::Nearest, Wrap wrap = Wrap::Repeat);
	Texture(uint32_t width, uint32_t height, TEXTURE_FORMAT format = TEX_FORMAT_RGBA8_UNORM, Filter filter = Filter::Nearest, Wrap wrap = Wrap::ClampToEdge);
	~Texture();

	Texture(const Texture&) = delete;
	Texture& operator=(const Texture&) = delete;

	Texture(Texture&& other) noexcept;
	Texture& operator=(Texture&& other) noexcept;

	void Bind(uint32_t slot = 0) const;
	void Unbind() const;

	void SetData(void* data, uint32_t size);

	inline uint32_t GetWidth() const
	{
		return m_Width;
	}

	inline uint32_t GetHeight() const
	{
		return m_Height;
	}

	ITextureView* GetSRV() const
	{
		return m_View;
	}

	ISampler* GetSampler() const
	{
		return m_Sampler;
	}

	ITexture* GetTexture() const
	{
		return m_Texture;
	}

	// Legacy ID support (returns 0, use GetSRV)
	uint32_t GetID() const
	{
		return 0;
	}

	bool operator==(const Texture& other) const
	{
		return m_View == other.m_View;
	}

private:
	RefCntAutoPtr<ITexture> m_Texture;
	RefCntAutoPtr<ITextureView> m_View;
	RefCntAutoPtr<ISampler> m_Sampler;

	std::string m_FilePath;
	uint32_t m_Width = 0;
	uint32_t m_Height = 0;
	TEXTURE_FORMAT m_Format;
};
