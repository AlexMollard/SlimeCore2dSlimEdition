#include "Texture.h"

#include <iostream>

#include "Core/Logger.h"
#include "Core/Window.h"

#include "DiligentTools/TextureLoader/interface/TextureUtilities.h"

using namespace Diligent;

Texture::Texture(const std::string& path, Filter filter, Wrap wrap)
      : m_FilePath(path)
{
	TextureLoadInfo loadInfo;
	loadInfo.IsSRGB = false;
	loadInfo.FlipVertically = true;

	auto device = Window::GetDevice();

	CreateTextureFromFile(path.c_str(), loadInfo, device, &m_Texture);

	if (m_Texture)
	{
		const auto& Desc = m_Texture->GetDesc();
		m_Width = Desc.Width;
		m_Height = Desc.Height;
		m_Format = Desc.Format;

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
