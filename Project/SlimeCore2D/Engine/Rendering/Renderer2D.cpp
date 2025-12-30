#include "Renderer2D.h"
#include "Core/Logger.h"
#include "Core/Window.h"

#include <array>
#include <gtc/matrix_transform.hpp>
#include <iostream>

#include "Resources/ResourceManager.h"
#include "Text.h" 

// Define the static data instance
Renderer2D::Renderer2DData Renderer2D::s_Data;

void Renderer2D::Init()
{
	s_Data.QuadBuffer = new Renderer2DData::QuadVertex[s_Data.MaxVertices];

	auto device = Window::GetDevice();
	auto context = Window::GetContext();

	// 1. Create Vertex Buffer (Dynamic)
	D3D11_BUFFER_DESC vbDesc = {};
	vbDesc.ByteWidth = s_Data.MaxVertices * sizeof(Renderer2DData::QuadVertex);
	vbDesc.Usage = D3D11_USAGE_DYNAMIC;
	vbDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
	vbDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;

	HRESULT hr = device->CreateBuffer(&vbDesc, nullptr, s_Data.QuadVB.GetAddressOf());
	if (FAILED(hr))
	{
		Logger::Error("Renderer2D: Failed to create Vertex Buffer!");
		return;
	}

	// 2. Create Index Buffer (Static)
	uint32_t* quadIndices = new uint32_t[s_Data.MaxIndices];
	uint32_t offset = 0;
	for (uint32_t i = 0; i < s_Data.MaxIndices; i += 6)
	{
		quadIndices[i + 0] = offset + 0;
		quadIndices[i + 1] = offset + 1;
		quadIndices[i + 2] = offset + 2;

		quadIndices[i + 3] = offset + 2;
		quadIndices[i + 4] = offset + 3;
		quadIndices[i + 5] = offset + 0;

		offset += 4;
	}

	D3D11_BUFFER_DESC ibDesc = {};
	ibDesc.ByteWidth = s_Data.MaxIndices * sizeof(uint32_t);
	ibDesc.Usage = D3D11_USAGE_DEFAULT;
	ibDesc.BindFlags = D3D11_BIND_INDEX_BUFFER;
	ibDesc.CPUAccessFlags = 0;

	D3D11_SUBRESOURCE_DATA ibData = {};
	ibData.pSysMem = quadIndices;

	hr = device->CreateBuffer(&ibDesc, &ibData, s_Data.QuadIB.GetAddressOf());
	if (FAILED(hr))
	{
		Logger::Error("Renderer2D: Failed to create Index Buffer!");
	}
	delete[] quadIndices;

	// 3. Create White Texture (1x1)
	D3D11_TEXTURE2D_DESC texDesc = {};
	texDesc.Width = 1;
	texDesc.Height = 1;
	texDesc.MipLevels = 1;
	texDesc.ArraySize = 1;
	texDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	texDesc.SampleDesc.Count = 1;
	texDesc.Usage = D3D11_USAGE_DEFAULT;
	texDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;

	uint32_t whiteColor = 0xffffffff;
	D3D11_SUBRESOURCE_DATA texData = {};
	texData.pSysMem = &whiteColor;
	texData.SysMemPitch = 4;

	Microsoft::WRL::ComPtr<ID3D11Texture2D> whiteTexture;
	hr = device->CreateTexture2D(&texDesc, &texData, whiteTexture.GetAddressOf());
	if (FAILED(hr))
	{
		Logger::Error("Renderer2D: Failed to create White Texture!");
	}
	else
	{
		hr = device->CreateShaderResourceView(whiteTexture.Get(), nullptr, s_Data.WhiteTextureSRV.GetAddressOf());
		if (FAILED(hr))
			Logger::Error("Renderer2D: Failed to create White Texture SRV!");
	}

	// 4. Create Sampler State
	D3D11_SAMPLER_DESC samplerDesc = {};
	samplerDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
	samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	samplerDesc.MinLOD = 0;
	samplerDesc.MaxLOD = D3D11_FLOAT32_MAX;

	hr = device->CreateSamplerState(&samplerDesc, s_Data.TextureSampler.GetAddressOf());
	if (FAILED(hr))
		Logger::Error("Renderer2D: Failed to create Sampler State!");

	// Create Linear Sampler
	samplerDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	hr = device->CreateSamplerState(&samplerDesc, s_Data.TextureSamplerLinear.GetAddressOf());
	if (FAILED(hr))
		Logger::Error("Renderer2D: Failed to create Linear Sampler State!");

	// 5. Create Blend State
	D3D11_BLEND_DESC blendDesc = {};
	blendDesc.RenderTarget[0].BlendEnable = TRUE;
	blendDesc.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
	blendDesc.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
	blendDesc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
	blendDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
	blendDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;

	hr = device->CreateBlendState(&blendDesc, s_Data.BlendState.GetAddressOf());
	if (FAILED(hr))
		Logger::Error("Renderer2D: Failed to create Blend State!");

	// 6. Create Rasterizer State (Cull None for 2D usually, or Back)
	D3D11_RASTERIZER_DESC rasterDesc = {};
	rasterDesc.FillMode = D3D11_FILL_SOLID;
	rasterDesc.CullMode = D3D11_CULL_NONE; // Don't cull for 2D
	rasterDesc.FrontCounterClockwise = FALSE; // Default
	rasterDesc.DepthClipEnable = TRUE;

	hr = device->CreateRasterizerState(&rasterDesc, s_Data.RasterizerState.GetAddressOf());
	if (FAILED(hr))
		Logger::Error("Renderer2D: Failed to create Rasterizer State!");

	// 7. Create Depth Stencil State
	D3D11_DEPTH_STENCIL_DESC depthDesc = {};
	depthDesc.DepthEnable = TRUE;
	depthDesc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
	depthDesc.DepthFunc = D3D11_COMPARISON_LESS_EQUAL;

	hr = device->CreateDepthStencilState(&depthDesc, s_Data.DepthStencilState.GetAddressOf());
	if (FAILED(hr))
		Logger::Error("Renderer2D: Failed to create Depth Stencil State!");

	// Initialize Texture Slots
	s_Data.TextureSlots[0] = s_Data.WhiteTextureSRV.Get();
	for (size_t i = 1; i < s_Data.MaxTextureSlots; i++)
		s_Data.TextureSlots[i] = nullptr;

	// Load Shaders
	ResourceManager::GetInstance().LoadShadersFromDir();
	s_Data.TextureShader = ResourceManager::GetInstance().GetShader("basic");
	if (!s_Data.TextureShader)
	{
		Logger::Warn("Renderer2D Warning: 'basic' shader not found.");
	}

	// Helper for rotation
	s_Data.QuadVertexPositions[0] = { -0.5f, -0.5f, 0.0f, 1.0f };
	s_Data.QuadVertexPositions[1] = { 0.5f, -0.5f, 0.0f, 1.0f };
	s_Data.QuadVertexPositions[2] = { 0.5f, 0.5f, 0.0f, 1.0f };
	s_Data.QuadVertexPositions[3] = { -0.5f, 0.5f, 0.0f, 1.0f };
}

void Renderer2D::Shutdown()
{
	delete[] s_Data.QuadBuffer;
	s_Data.QuadVB.Reset();
	s_Data.QuadIB.Reset();
	s_Data.WhiteTextureSRV.Reset();
	s_Data.TextureSampler.Reset();
	s_Data.TextureSamplerLinear.Reset();
	s_Data.BlendState.Reset();
	s_Data.RasterizerState.Reset();
	s_Data.DepthStencilState.Reset();
}

void Renderer2D::BeginScene(Camera& camera)
{
	if (s_Data.TextureShader)
	{
		s_Data.TextureShader->Bind();
		s_Data.TextureShader->SetMat4("u_ViewProjection", camera.GetViewProjectionMatrix());
	}
	StartBatch();
}

void Renderer2D::BeginScene(const glm::mat4& viewProj)
{
	if (s_Data.TextureShader)
	{
		s_Data.TextureShader->Bind();
		s_Data.TextureShader->SetMat4("u_ViewProjection", viewProj);
	}
	StartBatch();
}

void Renderer2D::EndScene()
{
	Flush();
}

void Renderer2D::StartBatch()
{
	s_Data.IndexCount = 0;
	s_Data.QuadBufferPtr = s_Data.QuadBuffer;
	s_Data.TextureSlotIndex = 1;
}

void Renderer2D::Flush()
{
	if (s_Data.IndexCount == 0)
		return;

	auto context = Window::GetContext();

	// 1. Update Vertex Buffer
	uint32_t dataSize = (uint32_t)((uint8_t*)s_Data.QuadBufferPtr - (uint8_t*)s_Data.QuadBuffer);
	
	D3D11_MAPPED_SUBRESOURCE mappedRes;
	HRESULT hr = context->Map(s_Data.QuadVB.Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &mappedRes);
	if (SUCCEEDED(hr))
	{
		memcpy(mappedRes.pData, s_Data.QuadBuffer, dataSize);
		context->Unmap(s_Data.QuadVB.Get(), 0);
	}

	// 2. Bind States
	float blendFactor[4] = { 0.0f, 0.0f, 0.0f, 0.0f };
	context->OMSetBlendState(s_Data.BlendState.Get(), blendFactor, 0xffffffff);
	context->OMSetDepthStencilState(s_Data.DepthStencilState.Get(), 0);
	context->RSSetState(s_Data.RasterizerState.Get());

	// 3. Bind Buffers
	UINT stride = sizeof(Renderer2DData::QuadVertex);
	UINT offset = 0;
	context->IASetVertexBuffers(0, 1, s_Data.QuadVB.GetAddressOf(), &stride, &offset);
	context->IASetIndexBuffer(s_Data.QuadIB.Get(), DXGI_FORMAT_R32_UINT, 0);
	context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

	// 4. Bind Textures
	context->PSSetShaderResources(0, s_Data.TextureSlotIndex, s_Data.TextureSlots.data());
	ID3D11SamplerState* samplers[] = { s_Data.TextureSampler.Get(), s_Data.TextureSamplerLinear.Get() };
	context->PSSetSamplers(0, 2, samplers);

	// 5. Draw
	context->DrawIndexed(s_Data.IndexCount, 0, 0);

	s_Data.Stats.DrawCalls++;
	s_Data.Stats.VertexCount += s_Data.IndexCount / 6 * 4;
	s_Data.Stats.IndexCount += s_Data.IndexCount;
}

void Renderer2D::NextBatch()
{
	Flush();
	StartBatch();
}

// -------------------------------------------------------------------------
// Drawing Primitives
// -------------------------------------------------------------------------

void Renderer2D::DrawQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color)
{
	DrawQuad({ position.x, position.y, 0.0f }, size, color);
}

void Renderer2D::DrawQuad(const glm::vec3& position, const glm::vec2& size, const glm::vec4& color)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices)
		NextBatch();

	const float texIndex = 0.0f; // White Texture
	const float tilingFactor = 1.0f;
	const glm::vec2 texCoords[] = {
		{ 0.0f, 1.0f }, // BL
        { 1.0f, 1.0f }, // BR
        { 1.0f, 0.0f }, // TR
        { 0.0f, 0.0f }  // TL
	};

	glm::vec3 transformPos;
	// Center-based anchor logic
	glm::vec3 offsets[4] = {
		{ -0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x,  0.5f * size.y, 0.0f },
        { -0.5f * size.x,  0.5f * size.y, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = position + offsets[i];
		s_Data.QuadBufferPtr->Color = color;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = texIndex;
		s_Data.QuadBufferPtr->TilingFactor = tilingFactor;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::vec2& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	DrawQuad({ position.x, position.y, 0.0f }, size, texture, tiling, tintColor);
}

void Renderer2D::DrawQuad(const glm::vec3& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ID3D11ShaderResourceView* srv = texture->GetSRV();

	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == srv)
		{
			textureIndex = (float) i;
			break;
		}
	}

	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
		s_Data.TextureSlotIndex++;
	}

	glm::vec2 texCoords[] = {
		{ 0.0f, 1.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f },
        { 0.0f, 0.0f }
	};

	glm::vec3 offsets[4] = {
		{ -0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x,  0.5f * size.y, 0.0f },
        { -0.5f * size.x,  0.5f * size.y, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = position + offsets[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
	DrawRotatedQuad({ position.x, position.y, 0.0f }, size, rotation, color);
}

void Renderer2D::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices)
		NextBatch();

	const float texIndex = 0.0f;
	const float tiling = 1.0f;
	const glm::vec2 texCoords[] = {
		{ 0.0f, 1.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f },
        { 0.0f, 0.0f }
	};

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::rotate(glm::mat4(1.0f), rotation, { 0.0f, 0.0f, 1.0f }) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = color;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = texIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ID3D11ShaderResourceView* srv = texture->GetSRV();

	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == srv)
		{
			textureIndex = (float) i;
			break;
		}
	}

	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
		s_Data.TextureSlotIndex++;
	}

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::rotate(glm::mat4(1.0f), rotation, { 0.0f, 0.0f, 1.0f }) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	const glm::vec2 texCoords[] = {
		{ 0.0f, 1.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f },
        { 0.0f, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::mat4& transform, const glm::vec4& color)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices)
		NextBatch();

	const float texIndex = 0.0f;
	const float tiling = 1.0f;
	const glm::vec2 texCoords[] = {
		{ 0.0f, 1.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f },
        { 0.0f, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = color;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = texIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::mat4& transform, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ID3D11ShaderResourceView* srv = texture->GetSRV();

	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == srv)
		{
			textureIndex = (float) i;
			break;
		}
	}

	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
		s_Data.TextureSlotIndex++;
	}

	const glm::vec2 texCoords[] = {
		{ 0.0f, 1.0f },
        { 1.0f, 1.0f },
        { 1.0f, 0.0f },
        { 0.0f, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuadUV(const glm::mat4& transform, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ID3D11ShaderResourceView* srv = texture->GetSRV();

	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == srv)
		{
			textureIndex = (float) i;
			break;
		}
	}
	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
		s_Data.TextureSlotIndex++;
	}

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = uv[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = 1.0f;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuadUV(const glm::vec3& position, const glm::vec2& size, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ID3D11ShaderResourceView* srv = texture->GetSRV();

	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == srv)
		{
			textureIndex = (float) i;
			break;
		}
	}
	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
		s_Data.TextureSlotIndex++;
	}

	// Assuming UVs are passed as { BL, BR, TR, TL }
	glm::vec3 offsets[4] = {
		{ -0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x,  0.5f * size.y, 0.0f },
        { -0.5f * size.x,  0.5f * size.y, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = position + offsets[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = uv[i]; // Use explicit UV
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = 1.0f;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

// -------------------------------------------------------------------------
// Text Rendering (SDF)
// -------------------------------------------------------------------------

void Renderer2D::DrawString(const std::string& text, Text* font, const glm::vec2& position, float scale, const glm::vec4& color)
{
	DrawString(text, font, { position.x, position.y, 0.0f }, scale, color);
}

void Renderer2D::DrawString(const std::string& text, Text* font, const glm::vec3& position, float scale, const glm::vec4& color)
{
	Texture* atlas = font->GetAtlasTexture();
	auto& characters = font->GetCharacters();

	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ID3D11ShaderResourceView* srv = atlas->GetSRV();

	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == srv)
		{
			textureIndex = (float) i;
			break;
		}
	}
	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
		s_Data.TextureSlotIndex++;
	}

	float x = position.x;
	float y = position.y;
	float z = position.z;

	std::string::const_iterator c;
	for (c = text.begin(); c != text.end(); c++)
	{
		auto it = characters.find(*c);

		if (it == characters.end())
			continue; 

		Character ch = it->second;

		float xpos = x + ch.Bearing.x * scale;
		float ypos = y - (ch.Size.y - ch.Bearing.y) * scale;
		float w = ch.Size.x * scale;
		float h = ch.Size.y * scale;

		if (w > 0.0f && h > 0.0f)
		{
			if (s_Data.IndexCount >= s_Data.MaxIndices)
				NextBatch();

			// Quad order: BL, BR, TR, TL
			glm::vec2 uvs[4] = {
				{ ch.uvMin.x, ch.uvMax.y }, // BL
				{ ch.uvMax.x, ch.uvMax.y }, // BR
				{ ch.uvMax.x, ch.uvMin.y }, // TR
				{ ch.uvMin.x, ch.uvMin.y }  // TL
			};

			glm::vec3 pos[4] = {
				{     xpos,     ypos, z },
                { xpos + w,     ypos, z },
                { xpos + w, ypos + h, z },
                {     xpos, ypos + h, z }
			};

			for (int i = 0; i < 4; i++)
			{
				s_Data.QuadBufferPtr->Position = pos[i];
				s_Data.QuadBufferPtr->Color = color;
				s_Data.QuadBufferPtr->TexCoord = uvs[i];
				s_Data.QuadBufferPtr->TexIndex = textureIndex;
				s_Data.QuadBufferPtr->TilingFactor = 1.0f;
				s_Data.QuadBufferPtr->IsText = 1.0f; 
				s_Data.QuadBufferPtr++;
			}

			s_Data.IndexCount += 6;
			s_Data.Stats.QuadCount++;
		}

		x += (ch.Advance >> 6) * scale;
	}
}

Renderer2D::Statistics Renderer2D::GetStats()
{
	return s_Data.Stats;
}

void Renderer2D::ResetStats()
{
	memset(&s_Data.Stats, 0, sizeof(Statistics));
}
