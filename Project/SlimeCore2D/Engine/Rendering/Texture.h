#pragma once

#define NOMINMAX
#include <string>
#include <d3d11.h>
#include <wrl/client.h>

using Microsoft::WRL::ComPtr;

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
	Texture(uint32_t width, uint32_t height, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM, Filter filter = Filter::Nearest, Wrap wrap = Wrap::ClampToEdge);
	~Texture();

	Texture(const Texture&) = delete;
	Texture& operator=(const Texture&) = delete;

	Texture(Texture&& other) noexcept;
	Texture& operator=(Texture&& other) noexcept;

	void Bind(uint32_t slot = 0) const;
	void Unbind() const;

	void SetData(void* data, uint32_t size);

	inline uint32_t GetWidth() const { return m_Width; }
	inline uint32_t GetHeight() const { return m_Height; }
    
    ID3D11ShaderResourceView* GetSRV() const { return m_View.Get(); }
    ID3D11SamplerState* GetSampler() const { return m_Sampler.Get(); }

	// Legacy ID support (returns 0, use GetSRV)
	uint32_t GetID() const { return 0; }

	bool operator==(const Texture& other) const
	{
		return m_View.Get() == other.m_View.Get();
	}

private:
    ComPtr<ID3D11Texture2D> m_Texture;
    ComPtr<ID3D11ShaderResourceView> m_View;
    ComPtr<ID3D11SamplerState> m_Sampler;
    
	std::string m_FilePath;
	uint32_t m_Width = 0;
	uint32_t m_Height = 0;
    DXGI_FORMAT m_Format;
};
