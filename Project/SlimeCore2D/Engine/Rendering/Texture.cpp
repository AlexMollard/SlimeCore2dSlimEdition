#include "Texture.h"

#include <iostream>

#include "Core/Logger.h"
#include "Core/Window.h"

#ifndef STB_IMAGE_IMPLEMENTATION
#	define STB_IMAGE_IMPLEMENTATION
#endif
#include "stb_image.h"

using namespace Diligent;

Texture::Texture(const std::string& path, Filter filter, Wrap wrap)
      : m_FilePath(path)
{
	stbi_set_flip_vertically_on_load(1);

	int width, height, channels;
	unsigned char* data = stbi_load(path.c_str(), &width, &height, &channels, 4); // Force RGBA

	if (data)
	{
		m_Width = width;
		m_Height = height;
		m_Format = TEX_FORMAT_RGBA8_UNORM;

		TextureDesc TexDesc;
		TexDesc.Name = "Texture";
		TexDesc.Type = RESOURCE_DIM_TEX_2D;
		TexDesc.Width = width;
		TexDesc.Height = height;
		TexDesc.Format = (TEXTURE_FORMAT) m_Format;
		TexDesc.Usage = USAGE_IMMUTABLE;
		TexDesc.BindFlags = BIND_SHADER_RESOURCE;
		TexDesc.MipLevels = 1;

		TextureSubResData Level0Data;
		Level0Data.pData = data;
		Level0Data.Stride = width * 4;

		TextureData InitData;
		InitData.pSubResources = &Level0Data;
		InitData.NumSubresources = 1;

		auto device = Window::GetDevice();
		device->CreateTexture(TexDesc, &InitData, &m_Texture);
		m_View = m_Texture->GetDefaultView(TEXTURE_VIEW_SHADER_RESOURCE);

		// Sampler
		SamplerDesc SampDesc;
		SampDesc.MinFilter = (filter == Filter::Nearest) ? FILTER_TYPE_POINT : FILTER_TYPE_LINEAR;
		SampDesc.MagFilter = (filter == Filter::Nearest) ? FILTER_TYPE_POINT : FILTER_TYPE_LINEAR;
		SampDesc.MipFilter = (filter == Filter::Nearest) ? FILTER_TYPE_POINT : FILTER_TYPE_LINEAR;
		SampDesc.AddressU = (wrap == Wrap::Repeat) ? TEXTURE_ADDRESS_WRAP : TEXTURE_ADDRESS_CLAMP;
		SampDesc.AddressV = (wrap == Wrap::Repeat) ? TEXTURE_ADDRESS_WRAP : TEXTURE_ADDRESS_CLAMP;
		SampDesc.AddressW = TEXTURE_ADDRESS_WRAP;

		device->CreateSampler(SampDesc, &m_Sampler);
		m_View->SetSampler(m_Sampler);

		stbi_image_free(data);
	}
	else
	{
		Logger::Error("ERROR::TEXTURE::LOAD_FAILED: " + path);
	}
}

Texture::Texture(uint32_t width, uint32_t height, TEXTURE_FORMAT format, Filter filter, Wrap wrap)
      : m_Width(width), m_Height(height), m_Format(format)
{
	TextureDesc TexDesc;
	TexDesc.Name = "Texture";
	TexDesc.Type = RESOURCE_DIM_TEX_2D;
	TexDesc.Width = width;
	TexDesc.Height = height;
	TexDesc.Format = format;
	TexDesc.Usage = USAGE_DEFAULT;
	TexDesc.BindFlags = BIND_SHADER_RESOURCE;
	TexDesc.MipLevels = 1;

	auto device = Window::GetDevice();
	device->CreateTexture(TexDesc, nullptr, &m_Texture);
	m_View = m_Texture->GetDefaultView(TEXTURE_VIEW_SHADER_RESOURCE);

	// Sampler
	SamplerDesc SampDesc;
	SampDesc.MinFilter = (filter == Filter::Nearest) ? FILTER_TYPE_POINT : FILTER_TYPE_LINEAR;
	SampDesc.MagFilter = (filter == Filter::Nearest) ? FILTER_TYPE_POINT : FILTER_TYPE_LINEAR;
	SampDesc.MipFilter = (filter == Filter::Nearest) ? FILTER_TYPE_POINT : FILTER_TYPE_LINEAR;
	SampDesc.AddressU = (wrap == Wrap::Repeat) ? TEXTURE_ADDRESS_WRAP : TEXTURE_ADDRESS_CLAMP;
	SampDesc.AddressV = (wrap == Wrap::Repeat) ? TEXTURE_ADDRESS_WRAP : TEXTURE_ADDRESS_CLAMP;
	SampDesc.AddressW = TEXTURE_ADDRESS_WRAP;

	device->CreateSampler(SampDesc, &m_Sampler);
	m_View->SetSampler(m_Sampler);
}

Texture::~Texture()
{
}

Texture::Texture(Texture&& other) noexcept
{
	m_Texture = std::move(other.m_Texture);
	m_View = std::move(other.m_View);
	m_Sampler = std::move(other.m_Sampler);
	m_Width = other.m_Width;
	m_Height = other.m_Height;
	m_FilePath = std::move(other.m_FilePath);
	m_Format = other.m_Format;
}

Texture& Texture::operator=(Texture&& other) noexcept
{
	if (this != &other)
	{
		m_Texture = std::move(other.m_Texture);
		m_View = std::move(other.m_View);
		m_Sampler = std::move(other.m_Sampler);
		m_Width = other.m_Width;
		m_Height = other.m_Height;
		m_FilePath = std::move(other.m_FilePath);
		m_Format = other.m_Format;
	}
	return *this;
}

void Texture::Bind(uint32_t slot) const
{
	// Not used in Diligent batch renderer usually
}

void Texture::Unbind() const
{
}

void Texture::SetData(void* data, uint32_t size)
{
	Box UpdateBox;
	UpdateBox.MinX = 0;
	UpdateBox.MaxX = m_Width;
	UpdateBox.MinY = 0;
	UpdateBox.MaxY = m_Height;

	TextureSubResData SubResData;
	SubResData.pData = data;
	SubResData.Stride = m_Width * 4;
	if (m_Format == TEX_FORMAT_R8_UNORM)
		SubResData.Stride = m_Width;

	Window::GetContext()->UpdateTexture(m_Texture, 0, 0, UpdateBox, SubResData, RESOURCE_STATE_TRANSITION_MODE_TRANSITION, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);
}
