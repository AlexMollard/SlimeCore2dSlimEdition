#include "Texture.h"
#include "Core/Logger.h"
#include "Core/Window.h"

#include <iostream>

#ifndef STB_IMAGE_IMPLEMENTATION
#	define STB_IMAGE_IMPLEMENTATION
#endif
#include "stb_image.h"

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
        m_Format = DXGI_FORMAT_R8G8B8A8_UNORM;

        D3D11_TEXTURE2D_DESC desc;
        ZeroMemory(&desc, sizeof(desc));
        desc.Width = width;
        desc.Height = height;
        desc.MipLevels = 1;
        desc.ArraySize = 1;
        desc.Format = m_Format;
        desc.SampleDesc.Count = 1;
        desc.Usage = D3D11_USAGE_DEFAULT;
        desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
        desc.CPUAccessFlags = 0;

        D3D11_SUBRESOURCE_DATA initData;
        ZeroMemory(&initData, sizeof(initData));
        initData.pSysMem = data;
        initData.SysMemPitch = width * 4;

        auto device = Window::GetDevice();
        device->CreateTexture2D(&desc, &initData, &m_Texture);
        device->CreateShaderResourceView(m_Texture.Get(), nullptr, &m_View);

        // Sampler
        D3D11_SAMPLER_DESC sampDesc;
        ZeroMemory(&sampDesc, sizeof(sampDesc));
        sampDesc.Filter = (filter == Filter::Nearest) ? D3D11_FILTER_MIN_MAG_MIP_POINT : D3D11_FILTER_MIN_MAG_MIP_LINEAR;
        sampDesc.AddressU = (wrap == Wrap::Repeat) ? D3D11_TEXTURE_ADDRESS_WRAP : D3D11_TEXTURE_ADDRESS_CLAMP;
        sampDesc.AddressV = (wrap == Wrap::Repeat) ? D3D11_TEXTURE_ADDRESS_WRAP : D3D11_TEXTURE_ADDRESS_CLAMP;
        sampDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
        sampDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
        sampDesc.MinLOD = 0;
        sampDesc.MaxLOD = D3D11_FLOAT32_MAX;
        
        device->CreateSamplerState(&sampDesc, &m_Sampler);

		stbi_image_free(data);
	}
	else
	{
		Logger::Error("ERROR::TEXTURE::LOAD_FAILED: " + path);
	}
}

Texture::Texture(uint32_t width, uint32_t height, DXGI_FORMAT format, Filter filter, Wrap wrap)
      : m_Width(width), m_Height(height), m_Format(format)
{
    D3D11_TEXTURE2D_DESC desc;
    ZeroMemory(&desc, sizeof(desc));
    desc.Width = width;
    desc.Height = height;
    desc.MipLevels = 1;
    desc.ArraySize = 1;
    desc.Format = format;
    desc.SampleDesc.Count = 1;
    desc.Usage = D3D11_USAGE_DEFAULT;
    desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    desc.CPUAccessFlags = 0;

    auto device = Window::GetDevice();
    device->CreateTexture2D(&desc, nullptr, &m_Texture);
    device->CreateShaderResourceView(m_Texture.Get(), nullptr, &m_View);

    // Sampler
    D3D11_SAMPLER_DESC sampDesc;
    ZeroMemory(&sampDesc, sizeof(sampDesc));
    sampDesc.Filter = (filter == Filter::Nearest) ? D3D11_FILTER_MIN_MAG_MIP_POINT : D3D11_FILTER_MIN_MAG_MIP_LINEAR;
    sampDesc.AddressU = (wrap == Wrap::Repeat) ? D3D11_TEXTURE_ADDRESS_WRAP : D3D11_TEXTURE_ADDRESS_CLAMP;
    sampDesc.AddressV = (wrap == Wrap::Repeat) ? D3D11_TEXTURE_ADDRESS_WRAP : D3D11_TEXTURE_ADDRESS_CLAMP;
    sampDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
    sampDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
    sampDesc.MinLOD = 0;
    sampDesc.MaxLOD = D3D11_FLOAT32_MAX;
    
    device->CreateSamplerState(&sampDesc, &m_Sampler);
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
    auto context = Window::GetContext();
    context->PSSetShaderResources(slot, 1, m_View.GetAddressOf());
    context->PSSetSamplers(slot, 1, m_Sampler.GetAddressOf());
}

void Texture::Unbind() const
{
}

void Texture::SetData(void* data, uint32_t size)
{
    UINT pitch = m_Width * 4;
    if (m_Format == DXGI_FORMAT_R8_UNORM) pitch = m_Width;
    
    Window::GetContext()->UpdateSubresource(m_Texture.Get(), 0, nullptr, data, pitch, 0);
}
