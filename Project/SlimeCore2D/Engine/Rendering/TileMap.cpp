#include "TileMap.h"

#include <algorithm>
#include <cmath>
#include <fstream>
#include <sstream>

#include "Core/Window.h"
#include "Rendering/Renderer2D.h"
#include "Rendering/Shader.h"
#include "Resources/ResourceManager.h"

using namespace Diligent;

// ==========================================
// TileMapChunk
// ==========================================

RefCntAutoPtr<IPipelineState> TileMapChunk::s_PSO;
RefCntAutoPtr<IBuffer> TileMapChunk::s_QuadVB;
RefCntAutoPtr<IBuffer> TileMapChunk::s_QuadIB;

struct QuadVertex
{
	float Pos[2];
	float UV[2];
};

void TileMapChunk::InitCommonResources()
{
	if (s_PSO)
		return;

	auto device = Window::GetDevice();

	// 1. Create Static Quad VB/IB
	{
		QuadVertex quadVerts[] = {
			{ { -0.5f, -0.5f }, { 0.0f, 1.0f } },
			{ { 0.5f, -0.5f }, { 1.0f, 1.0f } },
			{ { 0.5f, 0.5f }, { 1.0f, 0.0f } },
			{ { -0.5f, 0.5f }, { 0.0f, 0.0f } }
		};

		BufferDesc vbDesc;
		vbDesc.Name = "TileMap Quad VB";
		vbDesc.Size = sizeof(quadVerts);
		vbDesc.Usage = USAGE_IMMUTABLE;
		vbDesc.BindFlags = BIND_VERTEX_BUFFER;
		
		BufferData vbData;
		vbData.pData = quadVerts;
		vbData.DataSize = sizeof(quadVerts);
		
		device->CreateBuffer(vbDesc, &vbData, &s_QuadVB);

		uint32_t indices[] = { 0, 1, 2, 2, 3, 0 };
		
		BufferDesc ibDesc;
		ibDesc.Name = "TileMap Quad IB";
		ibDesc.Size = sizeof(indices);
		ibDesc.Usage = USAGE_IMMUTABLE;
		ibDesc.BindFlags = BIND_INDEX_BUFFER;

		BufferData ibData;
		ibData.pData = indices;
		ibData.DataSize = sizeof(indices);

		device->CreateBuffer(ibDesc, &ibData, &s_QuadIB);
	}

	// 2. Create PSO
	{
		ShaderCreateInfo ShaderCI;
		ShaderCI.SourceLanguage = SHADER_SOURCE_LANGUAGE_HLSL;

		auto& ResMgr = ResourceManager::GetInstance();
		std::string vsPath = ResMgr.GetResourcePath("Game/Resources/Shaders/tilemap_vertex.hlsl");
		
		// Extract directory for factory
		std::string shaderDir = vsPath.substr(0, vsPath.find_last_of("/\\"));
		
		RefCntAutoPtr<IShaderSourceInputStreamFactory> pShaderSourceFactory;
		Window::GetEngineFactory()->CreateDefaultShaderSourceStreamFactory(shaderDir.c_str(), &pShaderSourceFactory);
		ShaderCI.pShaderSourceStreamFactory = pShaderSourceFactory;

		RefCntAutoPtr<IShader> pVS;
		{
			ShaderCI.Desc.ShaderType = SHADER_TYPE_VERTEX;
			ShaderCI.EntryPoint = "main";
			ShaderCI.Desc.Name = "TileMap VS";
			ShaderCI.FilePath = "tilemap_vertex.hlsl";
			device->CreateShader(ShaderCI, &pVS);
		}

		RefCntAutoPtr<IShader> pPS;
		{
			ShaderCI.Desc.ShaderType = SHADER_TYPE_PIXEL;
			ShaderCI.EntryPoint = "main";
			ShaderCI.Desc.Name = "TileMap PS";
			ShaderCI.FilePath = "tilemap_pixel.hlsl";
			device->CreateShader(ShaderCI, &pPS);
		}

		// Input Layout
		// Slot 0: Per-Vertex (Quad)
		// Slot 1: Per-Instance (Tile Data)
		LayoutElement LayoutElems[] = {
			// Slot 0 - Static Quad
			LayoutElement{ 0, 0, 2, VT_FLOAT32, False }, // Pos
			LayoutElement{ 1, 0, 2, VT_FLOAT32, False }, // UV

			// Slot 1 - Per Instance
			LayoutElement{ 2, 1, 2, VT_FLOAT32, False, INPUT_ELEMENT_FREQUENCY_PER_INSTANCE }, // TilePos
			LayoutElement{ 3, 1, 2, VT_FLOAT32, False, INPUT_ELEMENT_FREQUENCY_PER_INSTANCE }, // TileSize
			LayoutElement{ 4, 1, 4, VT_FLOAT32, False, INPUT_ELEMENT_FREQUENCY_PER_INSTANCE }, // TexRect
			LayoutElement{ 5, 1, 4, VT_FLOAT32, False, INPUT_ELEMENT_FREQUENCY_PER_INSTANCE }, // Color
			LayoutElement{ 6, 1, 1, VT_FLOAT32, False, INPUT_ELEMENT_FREQUENCY_PER_INSTANCE }, // TexIndex
			LayoutElement{ 7, 1, 1, VT_FLOAT32, False, INPUT_ELEMENT_FREQUENCY_PER_INSTANCE }  // Rotation
		};

		GraphicsPipelineStateCreateInfo PSOCreateInfo;
		PSOCreateInfo.PSODesc.Name = "TileMap PSO";
		PSOCreateInfo.PSODesc.PipelineType = PIPELINE_TYPE_GRAPHICS;
		PSOCreateInfo.GraphicsPipeline.NumRenderTargets = 1;
		PSOCreateInfo.GraphicsPipeline.RTVFormats[0] = Window::GetSwapChain()->GetDesc().ColorBufferFormat;
		PSOCreateInfo.GraphicsPipeline.DSVFormat = Window::GetSwapChain()->GetDesc().DepthBufferFormat;
		PSOCreateInfo.GraphicsPipeline.PrimitiveTopology = PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
		PSOCreateInfo.GraphicsPipeline.RasterizerDesc.CullMode = CULL_MODE_NONE;
		PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthEnable = False;
		PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthWriteEnable = False;

		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendEnable = True;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].SrcBlend = BLEND_FACTOR_SRC_ALPHA;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].DestBlend = BLEND_FACTOR_INV_SRC_ALPHA;

		PSOCreateInfo.GraphicsPipeline.InputLayout.LayoutElements = LayoutElems;
		PSOCreateInfo.GraphicsPipeline.InputLayout.NumElements = _countof(LayoutElems);

		PSOCreateInfo.pVS = pVS;
		PSOCreateInfo.pPS = pPS;

		PSOCreateInfo.PSODesc.ResourceLayout.DefaultVariableType = SHADER_RESOURCE_VARIABLE_TYPE_STATIC;

		ShaderResourceVariableDesc Vars[] = {
			{  SHADER_TYPE_PIXEL,     "u_Textures", SHADER_RESOURCE_VARIABLE_TYPE_MUTABLE },
			{ SHADER_TYPE_VERTEX, "ConstantBuffer", SHADER_RESOURCE_VARIABLE_TYPE_MUTABLE }
		};
		PSOCreateInfo.PSODesc.ResourceLayout.Variables = Vars;
		PSOCreateInfo.PSODesc.ResourceLayout.NumVariables = _countof(Vars);

		SamplerDesc SamPointClamp;
		SamPointClamp.MinFilter = FILTER_TYPE_POINT;
		SamPointClamp.MagFilter = FILTER_TYPE_POINT;
		SamPointClamp.MipFilter = FILTER_TYPE_POINT;
		SamPointClamp.AddressU = TEXTURE_ADDRESS_CLAMP;
		SamPointClamp.AddressV = TEXTURE_ADDRESS_CLAMP;

		SamplerDesc SamLinearClamp;
		SamLinearClamp.MinFilter = FILTER_TYPE_LINEAR;
		SamLinearClamp.MagFilter = FILTER_TYPE_LINEAR;
		SamLinearClamp.MipFilter = FILTER_TYPE_LINEAR;
		SamLinearClamp.AddressU = TEXTURE_ADDRESS_CLAMP;
		SamLinearClamp.AddressV = TEXTURE_ADDRESS_CLAMP;

		ImmutableSamplerDesc ImtblSamplers[] = {
			{ SHADER_TYPE_PIXEL,       "u_Sampler",  SamPointClamp },
			{ SHADER_TYPE_PIXEL, "u_SamplerLinear", SamLinearClamp }
		};
		PSOCreateInfo.PSODesc.ResourceLayout.ImmutableSamplers = ImtblSamplers;
		PSOCreateInfo.PSODesc.ResourceLayout.NumImmutableSamplers = _countof(ImtblSamplers);

		device->CreateGraphicsPipelineState(PSOCreateInfo, &s_PSO);
	}
}

TileMapChunk::TileMapChunk(int offsetX, int offsetY, int width, int height, float tileSize)
      : m_OffsetX(offsetX), m_OffsetY(offsetY), m_Width(width), m_Height(height), m_TileSize(tileSize), m_Dirty(true), m_InstanceCount(0)
{
	InitCommonResources();

	int count = width * height;
	for (int i = 0; i < 3; i++)
	{
		m_Layers[i].resize(count);
		for (auto& tile: m_Layers[i])
		{
			tile.Active = false;
			tile.Texture = nullptr;
			tile.Color = glm::vec4(1.0f);
			tile.Rotation = 0.0f;
		}
	}

	if (s_PSO)
	{
		s_PSO->CreateShaderResourceBinding(&m_SRB, true);
	}
}

TileMapChunk::~TileMapChunk()
{
	m_InstanceBuffer.Release();
	m_SRB.Release();
}

void TileMapChunk::SetTile(int x, int y, int layer, Texture* texturePtr, float u0, float v0, float u1, float v1, float r, float g, float b, float a, float rotation)
{
	if (x < 0 || x >= m_Width || y < 0 || y >= m_Height || layer < 0 || layer >= 3)
		return;

	int idx = y * m_Width + x;
	TileInfo& tile = m_Layers[layer][idx];

	tile.Texture = texturePtr;
	tile.TexRect = glm::vec4(u0, v0, u1, v1);
	tile.Color = glm::vec4(r, g, b, a);
	tile.Rotation = rotation;
	tile.Active = (texturePtr != nullptr);

	m_Dirty = true;
}

void TileMapChunk::UpdateMesh()
{
	if (!m_Dirty)
		return;

	m_Instances.clear();
	m_TextureSlots.clear();

	// Always ensure we have at least the white texture at slot 0
	if (auto* pWhite = Renderer2D::GetWhiteTextureSRV())
	{
		m_TextureSlots.push_back(pWhite);
	}
	else
	{
		m_TextureSlots.push_back(nullptr);
	}

	for (int l = 0; l < 3; l++)
	{
		for (int y = 0; y < m_Height; y++)
		{
			for (int x = 0; x < m_Width; x++)
			{
				int idx = y * m_Width + x;
				const TileInfo& tile = m_Layers[l][idx];

				if (!tile.Active)
					continue;

				float texIndex = 0.0f;
				if (tile.Texture)
				{
					ITextureView* pView = tile.Texture->GetSRV();
					auto it = std::find(m_TextureSlots.begin(), m_TextureSlots.end(), pView);
					if (it != m_TextureSlots.end())
					{
						texIndex = (float) std::distance(m_TextureSlots.begin(), it);
					}
					else
					{
						if (m_TextureSlots.size() < 32)
						{
							m_TextureSlots.push_back(pView);
							texIndex = (float) (m_TextureSlots.size() - 1);
						}
						else
						{
							texIndex = 0.0f;
						}
					}
				}

				float worldX = (m_OffsetX + x) * m_TileSize;
				float worldY = (m_OffsetY + y) * m_TileSize;
				float cx = worldX + m_TileSize * 0.5f;
				float cy = worldY + m_TileSize * 0.5f;

				TileInstance inst;
				inst.Position[0] = cx;
				inst.Position[1] = cy;
				inst.Size[0] = m_TileSize;
				inst.Size[1] = m_TileSize;
				inst.TexRect[0] = tile.TexRect.x;
				inst.TexRect[1] = tile.TexRect.y;
				inst.TexRect[2] = tile.TexRect.z;
				inst.TexRect[3] = tile.TexRect.w;
				inst.Color[0] = tile.Color.r;
				inst.Color[1] = tile.Color.g;
				inst.Color[2] = tile.Color.b;
				inst.Color[3] = tile.Color.a;
				inst.TexIndex = texIndex;
				inst.Rotation = tile.Rotation;

				m_Instances.push_back(inst);
			}
		}
	}

	m_InstanceCount = (uint32_t) m_Instances.size();
	m_Dirty = false;

	if (m_Instances.empty())
		return;

	auto device = Window::GetDevice();
	auto context = Window::GetContext();

	if (!m_InstanceBuffer || m_Instances.size() * sizeof(TileInstance) > m_InstanceBuffer->GetDesc().Size)
	{
		BufferDesc vbDesc;
		vbDesc.Name = "TileMapChunk Instance VB";
		vbDesc.Size = (Uint64) (m_Instances.size() * sizeof(TileInstance) + 4096); // Extra space
		vbDesc.Usage = USAGE_DYNAMIC;
		vbDesc.BindFlags = BIND_VERTEX_BUFFER;
		vbDesc.CPUAccessFlags = CPU_ACCESS_WRITE;
		device->CreateBuffer(vbDesc, nullptr, &m_InstanceBuffer);
	}

	void* pData;
	context->MapBuffer(m_InstanceBuffer, MAP_WRITE, MAP_FLAG_DISCARD, pData);
	memcpy(pData, m_Instances.data(), m_Instances.size() * sizeof(TileInstance));
	context->UnmapBuffer(m_InstanceBuffer, MAP_WRITE);
}

void TileMapChunk::Render(IBuffer* pConstantBuffer)
{
	if (m_Dirty)
		UpdateMesh();
	if (m_InstanceCount == 0 || !m_InstanceBuffer || !s_QuadVB || !s_QuadIB)
		return;

	auto context = Window::GetContext();

	if (s_PSO && m_SRB)
	{
		if (auto* pVar = m_SRB->GetVariableByName(SHADER_TYPE_PIXEL, "u_Textures"))
		{
			std::vector<IDeviceObject*> pTexObjs;
			for (auto* pView: m_TextureSlots)
			{
				pTexObjs.push_back(pView);
			}
			while (pTexObjs.size() < 32)
			{
				pTexObjs.push_back(Renderer2D::GetWhiteTextureSRV());
			}
			pVar->SetArray(pTexObjs.data(), 0, (Uint32) pTexObjs.size(), SET_SHADER_RESOURCE_FLAG_ALLOW_OVERWRITE);
		}

		if (pConstantBuffer)
		{
			if (auto* pVar = m_SRB->GetVariableByName(SHADER_TYPE_VERTEX, "ConstantBuffer"))
				pVar->Set(pConstantBuffer);
		}

		context->SetPipelineState(s_PSO);
		context->CommitShaderResources(m_SRB, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

		IBuffer* pVBs[] = { s_QuadVB, m_InstanceBuffer };
		Uint64 offsets[] = { 0, 0 };
		context->SetVertexBuffers(0, 2, pVBs, offsets, RESOURCE_STATE_TRANSITION_MODE_TRANSITION, SET_VERTEX_BUFFERS_FLAG_RESET);
		context->SetIndexBuffer(s_QuadIB, 0, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

		DrawIndexedAttribs DrawAttrs;
		DrawAttrs.NumIndices = 6;
		DrawAttrs.NumInstances = m_InstanceCount;
		DrawAttrs.IndexType = VT_UINT32;
		DrawAttrs.Flags = DRAW_FLAG_VERIFY_ALL;
		context->DrawIndexed(DrawAttrs);
	}
}

// ==========================================
// TileMap
// ==========================================

struct TileMapConstantBuffer
{
	glm::mat4 ViewProjection;
	float Time;
	float Padding[3];
};

TileMap::TileMap(int width, int height, float tileSize)
      : m_Width(width), m_Height(height), m_TileSize(tileSize)
{
	CreateConstantBuffer();

	m_ChunksX = (width + m_ChunkWidth - 1) / m_ChunkWidth;
	m_ChunksY = (height + m_ChunkHeight - 1) / m_ChunkHeight;

	m_Chunks.resize(m_ChunksX * m_ChunksY);

	for (int y = 0; y < m_ChunksY; y++)
	{
		for (int x = 0; x < m_ChunksX; x++)
		{
			int cw = std::min(m_ChunkWidth, width - x * m_ChunkWidth);
			int ch = std::min(m_ChunkHeight, height - y * m_ChunkHeight);
			m_Chunks[y * m_ChunksX + x] = new TileMapChunk(x * m_ChunkWidth, y * m_ChunkHeight, cw, ch, tileSize);
		}
	}
}

void TileMap::CreateConstantBuffer()
{
	BufferDesc CBDesc;
	CBDesc.Name = "TileMap Constant Buffer";
	CBDesc.Size = sizeof(TileMapConstantBuffer);
	CBDesc.Usage = USAGE_DYNAMIC;
	CBDesc.BindFlags = BIND_UNIFORM_BUFFER;
	CBDesc.CPUAccessFlags = CPU_ACCESS_WRITE;

	Window::GetDevice()->CreateBuffer(CBDesc, nullptr, &m_VSConstants);
}

TileMap::~TileMap()
{
	for (auto* chunk: m_Chunks)
		delete chunk;
	m_Chunks.clear();
}

void TileMap::SetTile(int x, int y, int layer, Texture* texturePtr, float u0, float v0, float u1, float v1, float r, float g, float b, float a, float rotation)
{
	if (x < 0 || x >= m_Width || y < 0 || y >= m_Height)
		return;

	int cx = x / m_ChunkWidth;
	int cy = y / m_ChunkHeight;
	int lx = x % m_ChunkWidth;
	int ly = y % m_ChunkHeight;

	if (cx >= 0 && cx < m_ChunksX && cy >= 0 && cy < m_ChunksY)
	{
		m_Chunks[cy * m_ChunksX + cx]->SetTile(lx, ly, layer, texturePtr, u0, v0, u1, v1, r, g, b, a, rotation);
	}
}

void TileMap::UpdateMesh()
{
	for (auto* chunk: m_Chunks)
		chunk->UpdateMesh();
}

void TileMap::Render(const glm::mat4& viewProj)
{
	if (m_VSConstants)
	{
		TileMapConstantBuffer cb;
		cb.ViewProjection = viewProj; // Column-major by default in GLM
		cb.Time = 0.0f; // TODO: Pass time if needed

		void* pData;
		Window::GetContext()->MapBuffer(m_VSConstants, MAP_WRITE, MAP_FLAG_DISCARD, pData);
		memcpy(pData, &cb, sizeof(TileMapConstantBuffer));
		Window::GetContext()->UnmapBuffer(m_VSConstants, MAP_WRITE);
	}

	for (auto* chunk: m_Chunks)
		chunk->Render(m_VSConstants);
}
