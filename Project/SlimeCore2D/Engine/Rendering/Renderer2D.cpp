#include "Renderer2D.h"

#include <array>
#include <gtc/matrix_transform.hpp>
#include <iostream>

#include "Core/Logger.h"
#include "Core/Window.h"
#include "Resources/ResourceManager.h"
#include "Text.h"
#include "DiligentCore/Graphics/GraphicsTools/interface/MapHelper.hpp"

using namespace Diligent;

// Define the static data instance
Renderer2D::Renderer2DData Renderer2D::s_Data;

void Renderer2D::Init()
{
	Logger::Info("Renderer2D: InstanceData Size: " + std::to_string(sizeof(Renderer2DData::InstanceData)));
	s_Data.InstanceBuffer = new Renderer2DData::InstanceData[s_Data.MaxInstances];

	auto device = Window::GetDevice();

	// 1. Create Quad Vertex Buffer (Static Geometry)
	// Vertices: Pos(3), UV(2)
	float quadVertices[] = {
		-0.5f,
		-0.5f,
		0.0f,
		0.0f,
		0.0f, // BL
		0.5f,
		-0.5f,
		0.0f,
		1.0f,
		0.0f, // BR
		0.5f,
		0.5f,
		0.0f,
		1.0f,
		1.0f, // TR
		-0.5f,
		0.5f,
		0.0f,
		0.0f,
		1.0f // TL
	};

	Diligent::BufferDesc QuadVBDesc;
	QuadVBDesc.Name = "Quad Geometry Buffer";
	QuadVBDesc.Size = sizeof(quadVertices);
	QuadVBDesc.Usage = USAGE_IMMUTABLE;
	QuadVBDesc.BindFlags = BIND_VERTEX_BUFFER;

	BufferData QuadVBData;
	QuadVBData.pData = quadVertices;
	QuadVBData.DataSize = sizeof(quadVertices);
	device->CreateBuffer(QuadVBDesc, &QuadVBData, &s_Data.QuadVB);

	// 2. Create Instance Buffer (Dynamic)
	Diligent::BufferDesc InstVBDesc;
	InstVBDesc.Name = "Quad Instance Buffer";
	InstVBDesc.Size = s_Data.MaxInstances * sizeof(Renderer2DData::InstanceData);
	InstVBDesc.Usage = USAGE_DYNAMIC;
	InstVBDesc.BindFlags = BIND_VERTEX_BUFFER;
	InstVBDesc.CPUAccessFlags = CPU_ACCESS_WRITE;
	device->CreateBuffer(InstVBDesc, nullptr, &s_Data.InstanceVB);

	if (!s_Data.InstanceVB)
	{
		Logger::Error("Renderer2D: Failed to create Instance Buffer!");
	}

	// 3. Create Index Buffer (Static)
	uint32_t quadIndices[] = { 0, 1, 2, 2, 3, 0 };

	Diligent::BufferDesc IBDesc;
	IBDesc.Name = "Quad Index Buffer";
	IBDesc.Size = sizeof(quadIndices);
	IBDesc.Usage = USAGE_IMMUTABLE;
	IBDesc.BindFlags = BIND_INDEX_BUFFER;

	BufferData IBData;
	IBData.pData = quadIndices;
	IBData.DataSize = sizeof(quadIndices);
	device->CreateBuffer(IBDesc, &IBData, &s_Data.QuadIB);

	// 4. Create White Texture (1x1)
	TextureDesc TexDesc;
	TexDesc.Name = "White Texture";
	TexDesc.Type = RESOURCE_DIM_TEX_2D;
	TexDesc.Width = 1;
	TexDesc.Height = 1;
	TexDesc.Format = TEX_FORMAT_RGBA8_UNORM;
	TexDesc.Usage = USAGE_IMMUTABLE;
	TexDesc.BindFlags = BIND_SHADER_RESOURCE;
	TexDesc.MipLevels = 1;

	uint32_t whiteColor = 0xffffffff;
	TextureSubResData Level0Data;
	Level0Data.pData = &whiteColor;
	Level0Data.Stride = 4;
	TextureData InitData;
	InitData.pSubResources = &Level0Data;
	InitData.NumSubresources = 1;

	RefCntAutoPtr<ITexture> whiteTexture;
	device->CreateTexture(TexDesc, &InitData, &whiteTexture);
	s_Data.WhiteTextureSRV = whiteTexture->GetDefaultView(TEXTURE_VIEW_SHADER_RESOURCE);

	// Initialize Texture Slots
	s_Data.TextureSlots[0] = s_Data.WhiteTextureSRV;
	for (size_t i = 1; i < s_Data.MaxTextureSlots; i++)
		s_Data.TextureSlots[i] = nullptr;

	// Load Shaders
	ResourceManager::GetInstance().LoadShadersFromDir();
	s_Data.TextureShader = ResourceManager::GetInstance().GetShader("basic");
	if (!s_Data.TextureShader)
	{
		Logger::Warn("Renderer2D Warning: 'basic' shader not found.");
		return;
	}

	// Create PSO
	GraphicsPipelineStateCreateInfo PSOCreateInfo;
	PSOCreateInfo.PSODesc.Name = "Renderer2D PSO";
	PSOCreateInfo.PSODesc.PipelineType = PIPELINE_TYPE_GRAPHICS;
	PSOCreateInfo.GraphicsPipeline.NumRenderTargets = 1;
	PSOCreateInfo.GraphicsPipeline.RTVFormats[0] = TEX_FORMAT_RGBA8_UNORM;
	PSOCreateInfo.GraphicsPipeline.DSVFormat = TEX_FORMAT_D24_UNORM_S8_UINT;
	PSOCreateInfo.GraphicsPipeline.PrimitiveTopology = PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;

	// Blend State
	PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendEnable = true;
	PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].SrcBlend = BLEND_FACTOR_SRC_ALPHA;
	PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].DestBlend = BLEND_FACTOR_INV_SRC_ALPHA;
	PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendOp = BLEND_OPERATION_ADD;
	PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].SrcBlendAlpha = BLEND_FACTOR_ONE;
	PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].DestBlendAlpha = BLEND_FACTOR_ZERO;
	PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendOpAlpha = BLEND_OPERATION_ADD;

	// Rasterizer State
	PSOCreateInfo.GraphicsPipeline.RasterizerDesc.CullMode = CULL_MODE_NONE;
	PSOCreateInfo.GraphicsPipeline.RasterizerDesc.FillMode = FILL_MODE_SOLID;
	PSOCreateInfo.GraphicsPipeline.RasterizerDesc.FrontCounterClockwise = false;

	// Depth Stencil State
	PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthEnable = false;
	PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthWriteEnable = false;
	PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthFunc = COMPARISON_FUNC_ALWAYS; // Disable depth test for 2D

	// Input Layout
	LayoutElement LayoutElems[] = {
		// Slot 0 - Geometry
		LayoutElement{ "ATTRIB", 0, 0, 3, VT_FLOAT32, False, 0, 5 * sizeof(float) }, // Position
		LayoutElement{ "ATTRIB", 1, 0, 2, VT_FLOAT32, False, 3 * sizeof(float), 5 * sizeof(float) }, // TexCoord

		// Slot 1 - Instance Data
		// Transform Row 0
		LayoutElement{ "ATTRIB", 2, 1, 4, VT_FLOAT32, False, 0, sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },
		// Transform Row 1
		LayoutElement{ "ATTRIB", 3, 1, 4, VT_FLOAT32, False, 4 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },
		// Transform Row 2
		LayoutElement{ "ATTRIB", 4, 1, 4, VT_FLOAT32, False, 8 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },
		// Transform Row 3
		LayoutElement{ "ATTRIB", 5, 1, 4, VT_FLOAT32, False, 12 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },

		// Color
		LayoutElement{ "ATTRIB", 6, 1, 4, VT_FLOAT32, False, 16 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },
		// UVRect
		LayoutElement{ "ATTRIB", 7, 1, 4, VT_FLOAT32, False, 20 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },
		// TexIndex
		LayoutElement{ "ATTRIB", 8, 1, 1, VT_FLOAT32, False, 24 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },
		// Tiling
		LayoutElement{ "ATTRIB", 9, 1, 1, VT_FLOAT32, False, 25 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE },
		// IsText
		LayoutElement{ "ATTRIB", 10, 1, 1, VT_FLOAT32, False, 26 * sizeof(float), sizeof(Renderer2DData::InstanceData), INPUT_ELEMENT_FREQUENCY_PER_INSTANCE }
	};
	PSOCreateInfo.GraphicsPipeline.InputLayout.LayoutElements = LayoutElems;
	PSOCreateInfo.GraphicsPipeline.InputLayout.NumElements = _countof(LayoutElems);

	// Shaders
	PSOCreateInfo.pVS = s_Data.TextureShader->GetVertexShader();
	PSOCreateInfo.pPS = s_Data.TextureShader->GetPixelShader();

	// Shader Variables
	ShaderResourceVariableDesc Vars[] = {
		{		              SHADER_TYPE_PIXEL,     "u_Textures", SHADER_RESOURCE_VARIABLE_TYPE_DYNAMIC },
        { SHADER_TYPE_VERTEX | SHADER_TYPE_PIXEL, "ConstantBuffer", SHADER_RESOURCE_VARIABLE_TYPE_MUTABLE }
	};
	PSOCreateInfo.PSODesc.ResourceLayout.Variables = Vars;
	PSOCreateInfo.PSODesc.ResourceLayout.NumVariables = _countof(Vars);

	// Immutable Samplers
	SamplerDesc SamLinear;
	SamLinear.MinFilter = FILTER_TYPE_LINEAR;
	SamLinear.MagFilter = FILTER_TYPE_LINEAR;
	SamLinear.MipFilter = FILTER_TYPE_LINEAR;
	SamLinear.AddressU = TEXTURE_ADDRESS_WRAP;
	SamLinear.AddressV = TEXTURE_ADDRESS_WRAP;
	SamLinear.AddressW = TEXTURE_ADDRESS_WRAP;

	SamplerDesc SamPoint;
	SamPoint.MinFilter = FILTER_TYPE_POINT;
	SamPoint.MagFilter = FILTER_TYPE_POINT;
	SamPoint.MipFilter = FILTER_TYPE_POINT;
	SamPoint.AddressU = TEXTURE_ADDRESS_WRAP;
	SamPoint.AddressV = TEXTURE_ADDRESS_WRAP;
	SamPoint.AddressW = TEXTURE_ADDRESS_WRAP;

	ImmutableSamplerDesc ImtblSamplers[] = {
		{ SHADER_TYPE_PIXEL,       "u_Sampler",  SamPoint },
        { SHADER_TYPE_PIXEL, "u_SamplerLinear", SamLinear }
	};
	PSOCreateInfo.PSODesc.ResourceLayout.ImmutableSamplers = ImtblSamplers;
	PSOCreateInfo.PSODesc.ResourceLayout.NumImmutableSamplers = _countof(ImtblSamplers);

	device->CreateGraphicsPipelineState(PSOCreateInfo, &s_Data.PSO);

	// Create SRB
	s_Data.PSO->CreateShaderResourceBinding(&s_Data.SRB, true);

	// Bind Constant Buffer
	if (auto* pCB = s_Data.TextureShader->GetConstantBuffer())
	{
		if (auto* pVar = s_Data.SRB->GetVariableByName(SHADER_TYPE_VERTEX, "ConstantBuffer"))
			pVar->Set(pCB);
		if (auto* pVar = s_Data.SRB->GetVariableByName(SHADER_TYPE_PIXEL, "ConstantBuffer"))
			pVar->Set(pCB);
	}
}

void Renderer2D::Shutdown()
{
	delete[] s_Data.InstanceBuffer;
	s_Data.QuadVB.Release();
	s_Data.InstanceVB.Release();
	s_Data.QuadIB.Release();
	s_Data.WhiteTextureSRV.Release();
	s_Data.PSO.Release();
	s_Data.SRB.Release();
}

void Renderer2D::BeginScene(Camera& camera)
{
	if (s_Data.TextureShader)
	{
		s_Data.TextureShader->SetMat4("u_ViewProjection", camera.GetViewProjectionMatrix());
	}
	StartBatch();
}

void Renderer2D::BeginScene(const glm::mat4& viewProj)
{
	if (s_Data.TextureShader)
	{
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
	s_Data.QuadCount = 0;
	s_Data.InstanceBufferPtr = s_Data.InstanceBuffer;
	s_Data.TextureSlotIndex = 1;
	// Logger::Info("Renderer2D::StartBatch - Reset QuadCount");
}

void Renderer2D::Flush()
{
	if (s_Data.QuadCount == 0)
	{
		// Logger::Info("Renderer2D::Flush - QuadCount is 0, skipping");
		return;
	}

	static int frameCount = 0;
	frameCount++;
	if (frameCount % 60 == 0) Logger::Info("Renderer2D::Flush - QuadCount: " + std::to_string(s_Data.QuadCount));

	auto context = Window::GetContext();

	// 1. Update Instance Buffer
	uint32_t dataSize = (uint32_t) ((uint8_t*) s_Data.InstanceBufferPtr - (uint8_t*) s_Data.InstanceBuffer);

	MapHelper<Renderer2DData::InstanceData> InstanceData(context, s_Data.InstanceVB, MAP_WRITE, MAP_FLAG_DISCARD);
	memcpy(InstanceData, s_Data.InstanceBuffer, dataSize);

	// 2. Bind States
	context->SetPipelineState(s_Data.PSO);

	// 3. Bind Buffers
	// Slot 0: Quad Geometry (Static)
	// Slot 1: Instance Data (Dynamic)
	if (s_Data.InstanceVB)
	{
		IBuffer* pVBs[] = { s_Data.QuadVB, s_Data.InstanceVB };
		Uint64 offsets[] = { 0, 0 };
		context->SetVertexBuffers(0, 2, pVBs, offsets, RESOURCE_STATE_TRANSITION_MODE_TRANSITION, SET_VERTEX_BUFFERS_FLAG_RESET);
	}
	else
	{
		Logger::Error("Renderer2D: Instance Buffer is null!");
		return;
	}

	context->SetIndexBuffer(s_Data.QuadIB, 0, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

	// 4. Bind Textures
	if (auto* pVar = s_Data.SRB->GetVariableByName(SHADER_TYPE_PIXEL, "u_Textures"))
	{
		std::vector<IDeviceObject*> pViews(s_Data.MaxTextureSlots);
		for (uint32_t i = 0; i < s_Data.TextureSlotIndex; ++i)
			pViews[i] = s_Data.TextureSlots[i];

		// Fill the rest with white texture
		for (uint32_t i = s_Data.TextureSlotIndex; i < s_Data.MaxTextureSlots; ++i)
			pViews[i] = s_Data.WhiteTextureSRV;

		pVar->SetArray(pViews.data(), 0, s_Data.MaxTextureSlots);
	}

	context->CommitShaderResources(s_Data.SRB, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

	// 5. Draw Instanced
	DrawIndexedAttribs DrawAttrs;
	DrawAttrs.NumIndices = 6; // Always 6 indices per quad
	DrawAttrs.NumInstances = s_Data.QuadCount;
	DrawAttrs.IndexType = VT_UINT32;
	DrawAttrs.Flags = DRAW_FLAG_VERIFY_ALL;
	context->DrawIndexed(DrawAttrs);

	s_Data.Stats.DrawCalls++;
	s_Data.Stats.QuadCount += s_Data.QuadCount;
	s_Data.Stats.VertexCount += s_Data.QuadCount * 4;
	s_Data.Stats.IndexCount += s_Data.QuadCount * 6;
}

void Renderer2D::NextBatch()
{
	Flush();
	StartBatch();
}

void Renderer2D::DrawQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color)
{
	DrawQuad({ position.x, position.y, 0.0f }, size, color);
}

void Renderer2D::DrawQuad(const glm::vec3& position, const glm::vec2& size, const glm::vec4& color)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances)
		NextBatch();

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = color;
	s_Data.InstanceBufferPtr->UVRect = { 0.0f, 0.0f, 1.0f, 1.0f }; // Default UVs
	s_Data.InstanceBufferPtr->TexIndex = 0.0f;                     // White Texture
	s_Data.InstanceBufferPtr->Tiling = 1.0f;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::vec2& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	DrawQuad({ position.x, position.y, 0.0f }, size, texture, tiling, tintColor);
}

void Renderer2D::DrawQuad(const glm::vec3& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ITextureView* srv = texture->GetSRV();

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

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = tintColor;
	s_Data.InstanceBufferPtr->UVRect = { 0.0f, 0.0f, 1.0f, 1.0f };
	s_Data.InstanceBufferPtr->TexIndex = textureIndex;
	s_Data.InstanceBufferPtr->Tiling = tiling;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;
}

void Renderer2D::DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
	DrawRotatedQuad({ position.x, position.y, 0.0f }, size, rotation, color);
}

void Renderer2D::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances)
		NextBatch();

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::rotate(glm::mat4(1.0f), rotation, { 0.0f, 0.0f, 1.0f }) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = color;
	s_Data.InstanceBufferPtr->UVRect = { 0.0f, 0.0f, 1.0f, 1.0f };
	s_Data.InstanceBufferPtr->TexIndex = 0.0f;
	s_Data.InstanceBufferPtr->Tiling = 1.0f;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;
}

void Renderer2D::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ITextureView* srv = texture->GetSRV();

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

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = tintColor;
	s_Data.InstanceBufferPtr->UVRect = { 0.0f, 0.0f, 1.0f, 1.0f };
	s_Data.InstanceBufferPtr->TexIndex = textureIndex;
	s_Data.InstanceBufferPtr->Tiling = tiling;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::mat4& transform, const glm::vec4& color)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances)
		NextBatch();

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = color;
	s_Data.InstanceBufferPtr->UVRect = { 0.0f, 0.0f, 1.0f, 1.0f };
	s_Data.InstanceBufferPtr->TexIndex = 0.0f;
	s_Data.InstanceBufferPtr->Tiling = 1.0f;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;

	static int logCount = 0;
	if (logCount++ < 5) Logger::Info("Renderer2D::DrawQuad (Color) - Count: " + std::to_string(s_Data.QuadCount));
}

void Renderer2D::DrawQuad(const glm::mat4& transform, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	if (texture == nullptr)
	{
		Logger::Error("Renderer2D::DrawQuad - Texture is null!");
		return;
	}

	ITextureView* srv = texture->GetSRV();

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

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = tintColor;
	s_Data.InstanceBufferPtr->UVRect = { 0.0f, 0.0f, 1.0f, 1.0f };
	s_Data.InstanceBufferPtr->TexIndex = textureIndex;
	s_Data.InstanceBufferPtr->Tiling = tiling;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;
	
	static int logCount = 0;
	if (logCount++ < 5) Logger::Info("Renderer2D::DrawQuad (Tex) - Count: " + std::to_string(s_Data.QuadCount));
}

void Renderer2D::DrawQuadUV(const glm::mat4& transform, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	if (texture == nullptr)
	{
		Logger::Error("Renderer2D::DrawQuadUV - Texture is null!");
		return;
	}

	ITextureView* srv = texture->GetSRV();

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

	// Calculate Min/Max UVs
	float minU = uv[0].x, minV = uv[0].y;
	float maxU = uv[0].x, maxV = uv[0].y;
	for (int i = 1; i < 4; ++i)
	{
		if (uv[i].x < minU)
			minU = uv[i].x;
		if (uv[i].y < minV)
			minV = uv[i].y;
		if (uv[i].x > maxU)
			maxU = uv[i].x;
		if (uv[i].y > maxV)
			maxV = uv[i].y;
	}

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = tintColor;
	s_Data.InstanceBufferPtr->UVRect = { minU, minV, maxU, maxV };
	s_Data.InstanceBufferPtr->TexIndex = textureIndex;
	s_Data.InstanceBufferPtr->Tiling = 1.0f;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;

	static int logCount = 0;
	if (logCount++ < 5) Logger::Info("Renderer2D::DrawQuadUV - Count: " + std::to_string(s_Data.QuadCount));
}

void Renderer2D::DrawQuadUV(const glm::vec3& position, const glm::vec2& size, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor)
{
	if (s_Data.QuadCount >= s_Data.MaxInstances || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ITextureView* srv = texture->GetSRV();

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

	// Calculate Min/Max UVs
	float minU = uv[0].x, minV = uv[0].y;
	float maxU = uv[0].x, maxV = uv[0].y;
	for (int i = 1; i < 4; ++i)
	{
		if (uv[i].x < minU)
			minU = uv[i].x;
		if (uv[i].y < minV)
			minV = uv[i].y;
		if (uv[i].x > maxU)
			maxU = uv[i].x;
		if (uv[i].y > maxV)
			maxV = uv[i].y;
	}

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	s_Data.InstanceBufferPtr->Transform = transform;
	s_Data.InstanceBufferPtr->Color = tintColor;
	s_Data.InstanceBufferPtr->UVRect = { minU, minV, maxU, maxV };
	s_Data.InstanceBufferPtr->TexIndex = textureIndex;
	s_Data.InstanceBufferPtr->Tiling = 1.0f;
	s_Data.InstanceBufferPtr->IsText = 0.0f;
	s_Data.InstanceBufferPtr++;

	s_Data.QuadCount++;
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

	if (s_Data.QuadCount >= s_Data.MaxInstances || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	ITextureView* srv = atlas->GetSRV();

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
			if (s_Data.QuadCount >= s_Data.MaxInstances)
				NextBatch();

			// Calculate center position for transform
			float centerX = xpos + w * 0.5f;
			float centerY = ypos + h * 0.5f;

			glm::mat4 transform = glm::translate(glm::mat4(1.0f), { centerX, centerY, z }) * glm::scale(glm::mat4(1.0f), { w, h, 1.0f });

			s_Data.InstanceBufferPtr->Transform = transform;
			s_Data.InstanceBufferPtr->Color = color;
			s_Data.InstanceBufferPtr->UVRect = { ch.uvMin.x, ch.uvMax.y, ch.uvMax.x, ch.uvMin.y };
			s_Data.InstanceBufferPtr->TexIndex = textureIndex;
			s_Data.InstanceBufferPtr->Tiling = 1.0f;
			s_Data.InstanceBufferPtr->IsText = 1.0f;
			s_Data.InstanceBufferPtr++;

			s_Data.QuadCount++;
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
