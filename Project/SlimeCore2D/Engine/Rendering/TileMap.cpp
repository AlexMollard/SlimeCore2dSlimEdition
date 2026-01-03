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

TileMapChunk::TileMapChunk(int offsetX, int offsetY, int width, int height, float tileSize)
      : m_OffsetX(offsetX), m_OffsetY(offsetY), m_Width(width), m_Height(height), m_TileSize(tileSize), m_Dirty(true), m_IndexCount(0)
{
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

	// Initialize Static PSO if needed
	if (!s_PSO)
	{
		auto device = Window::GetDevice();

		ShaderCreateInfo ShaderCI;
		ShaderCI.SourceLanguage = SHADER_SOURCE_LANGUAGE_HLSL;
		// ShaderCI.UseCombinedTextureSamplers = true;

		auto& ResMgr = ResourceManager::GetInstance();
		std::string vsPath = ResMgr.GetResourcePath("Game/Resources/Shaders/tilemap_vertex.hlsl");
		std::string psPath = ResMgr.GetResourcePath("Game/Resources/Shaders/BasicPixel.hlsl");

		std::string vsCode, psCode;

		auto ReadFile = [](const std::string& path) -> std::string
		{
			std::ifstream file(path);
			if (!file.is_open())
				return "";
			std::stringstream ss;
			ss << file.rdbuf();
			return ss.str();
		};

		vsCode = ReadFile(vsPath);
		psCode = ReadFile(psPath);

		// Create Vertex Shader
		RefCntAutoPtr<IShader> pVS;
		{
			ShaderCI.Desc.ShaderType = SHADER_TYPE_VERTEX;
			ShaderCI.EntryPoint = "main";
			ShaderCI.Desc.Name = "TileMap VS";
			ShaderCI.Source = vsCode.c_str();
			device->CreateShader(ShaderCI, &pVS);
		}

		// Create Pixel Shader
		RefCntAutoPtr<IShader> pPS;
		{
			ShaderCI.Desc.ShaderType = SHADER_TYPE_PIXEL;
			ShaderCI.EntryPoint = "main";
			ShaderCI.Desc.Name = "TileMap PS";
			ShaderCI.Source = psCode.c_str();
			device->CreateShader(ShaderCI, &pPS);
		}

		LayoutElement LayoutElems[] = {
			LayoutElement{ 0, 0, 3, VT_FLOAT32, False }, // Pos
			LayoutElement{ 1, 0, 4, VT_FLOAT32, False }, // Color
			LayoutElement{ 2, 0, 2, VT_FLOAT32, False }, // TexCoord
			LayoutElement{ 3, 0, 1, VT_FLOAT32, False }, // TexIndex
			LayoutElement{ 4, 0, 1, VT_FLOAT32, False }, // Tiling
			LayoutElement{ 5, 0, 1, VT_FLOAT32, False }  // IsText
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
		PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthFunc = COMPARISON_FUNC_ALWAYS;

		// Blending
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendEnable = True;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].SrcBlend = BLEND_FACTOR_SRC_ALPHA;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].DestBlend = BLEND_FACTOR_INV_SRC_ALPHA;

		PSOCreateInfo.GraphicsPipeline.InputLayout.LayoutElements = LayoutElems;
		PSOCreateInfo.GraphicsPipeline.InputLayout.NumElements = _countof(LayoutElems);

		PSOCreateInfo.pVS = pVS;
		PSOCreateInfo.pPS = pPS;

		PSOCreateInfo.PSODesc.ResourceLayout.DefaultVariableType = SHADER_RESOURCE_VARIABLE_TYPE_STATIC;

		// Variables
		ShaderResourceVariableDesc Vars[] = {
			{  SHADER_TYPE_PIXEL,     "u_Textures", SHADER_RESOURCE_VARIABLE_TYPE_MUTABLE },
            { SHADER_TYPE_VERTEX, "ConstantBuffer", SHADER_RESOURCE_VARIABLE_TYPE_MUTABLE }
		};
		PSOCreateInfo.PSODesc.ResourceLayout.Variables = Vars;
		PSOCreateInfo.PSODesc.ResourceLayout.NumVariables = _countof(Vars);

		// Samplers
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

	// Create SRB from TileMap PSO
	if (s_PSO)
	{
		s_PSO->CreateShaderResourceBinding(&m_SRB, true);
	}
}

TileMapChunk::~TileMapChunk()
{
	m_VertexBuffer.Release();
	m_IndexBuffer.Release();
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

	m_Vertices.clear();
	m_Indices.clear();
	m_TextureSlots.clear();

	// Always ensure we have at least the white texture at slot 0
	if (auto* pWhite = Renderer2D::GetWhiteTextureSRV())
	{
		m_TextureSlots.push_back(pWhite);
	}
	else
	{
		m_TextureSlots.push_back(nullptr); // Placeholder
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
							// Slot overflow, map to 0 (white) or handle differently
							texIndex = 0.0f;
						}
					}
				}

				float worldX = (m_OffsetX + x) * m_TileSize;
				float worldY = (m_OffsetY + y) * m_TileSize;

				// Rotate around center
				float cx = worldX + m_TileSize * 0.5f;
				float cy = worldY + m_TileSize * 0.5f;

				glm::vec3 p0(-m_TileSize * 0.5f, -m_TileSize * 0.5f, 0.0f);
				glm::vec3 p1(m_TileSize * 0.5f, -m_TileSize * 0.5f, 0.0f);
				glm::vec3 p2(m_TileSize * 0.5f, m_TileSize * 0.5f, 0.0f);
				glm::vec3 p3(-m_TileSize * 0.5f, m_TileSize * 0.5f, 0.0f);

				if (tile.Rotation != 0.0f)
				{
					float c = cos(tile.Rotation);
					float s = sin(tile.Rotation);

					auto Rotate = [&](glm::vec3& p)
					{
						float tx = p.x * c - p.y * s;
						float ty = p.x * s + p.y * c;
						p.x = tx;
						p.y = ty;
					};
					Rotate(p0);
					Rotate(p1);
					Rotate(p2);
					Rotate(p3);
				}

				p0 += glm::vec3(cx, cy, 0.0f);
				p1 += glm::vec3(cx, cy, 0.0f);
				p2 += glm::vec3(cx, cy, 0.0f);
				p3 += glm::vec3(cx, cy, 0.0f);

				uint32_t baseIndex = (uint32_t) m_Vertices.size();

				TileVertex v[4];
				v[0].Position = p0;
				v[0].TexCoord = glm::vec2(tile.TexRect.x, tile.TexRect.y); // u0, v0
				v[1].Position = p1;
				v[1].TexCoord = glm::vec2(tile.TexRect.z, tile.TexRect.y); // u1, v0
				v[2].Position = p2;
				v[2].TexCoord = glm::vec2(tile.TexRect.z, tile.TexRect.w); // u1, v1
				v[3].Position = p3;
				v[3].TexCoord = glm::vec2(tile.TexRect.x, tile.TexRect.w); // u0, v1

				for (int i = 0; i < 4; i++)
				{
					v[i].Color = tile.Color;
					v[i].TexIndex = texIndex;
					v[i].TilingFactor = 1.0f;
					v[i].IsText = 0.0f;
					m_Vertices.push_back(v[i]);
				}

				m_Indices.push_back(baseIndex + 0);
				m_Indices.push_back(baseIndex + 1);
				m_Indices.push_back(baseIndex + 2);
				m_Indices.push_back(baseIndex + 2);
				m_Indices.push_back(baseIndex + 3);
				m_Indices.push_back(baseIndex + 0);
			}
		}
	}

	m_IndexCount = (uint32_t) m_Indices.size();
	m_Dirty = false;

	if (m_Vertices.empty())
		return;

	auto device = Window::GetDevice();
	auto context = Window::GetContext();

	// Vertex Buffer
	if (!m_VertexBuffer || m_Vertices.size() * sizeof(TileVertex) > m_VertexBuffer->GetDesc().Size)
	{
		BufferDesc vbDesc;
		vbDesc.Name = "TileMapChunk VB";
		vbDesc.Size = (Uint64) (m_Vertices.size() * sizeof(TileVertex) + 1024); // Extra space
		vbDesc.Usage = USAGE_DYNAMIC;
		vbDesc.BindFlags = BIND_VERTEX_BUFFER;
		vbDesc.CPUAccessFlags = CPU_ACCESS_WRITE;
		device->CreateBuffer(vbDesc, nullptr, &m_VertexBuffer);
	}

	void* pData;
	context->MapBuffer(m_VertexBuffer, MAP_WRITE, MAP_FLAG_DISCARD, pData);
	memcpy(pData, m_Vertices.data(), m_Vertices.size() * sizeof(TileVertex));
	context->UnmapBuffer(m_VertexBuffer, MAP_WRITE);

	// Index Buffer
	if (!m_IndexBuffer || m_Indices.size() * sizeof(uint32_t) > m_IndexBuffer->GetDesc().Size)
	{
		BufferDesc ibDesc;
		ibDesc.Name = "TileMapChunk IB";
		ibDesc.Size = (Uint64) (m_Indices.size() * sizeof(uint32_t) + 1024);
		ibDesc.Usage = USAGE_DYNAMIC;
		ibDesc.BindFlags = BIND_INDEX_BUFFER;
		ibDesc.CPUAccessFlags = CPU_ACCESS_WRITE;
		device->CreateBuffer(ibDesc, nullptr, &m_IndexBuffer);
	}

	context->MapBuffer(m_IndexBuffer, MAP_WRITE, MAP_FLAG_DISCARD, pData);
	memcpy(pData, m_Indices.data(), m_Indices.size() * sizeof(uint32_t));
	context->UnmapBuffer(m_IndexBuffer, MAP_WRITE);
}

void TileMapChunk::Render(const glm::mat4& viewProj)
{
	if (m_Dirty)
		UpdateMesh();
	if (m_IndexCount == 0 || !m_VertexBuffer || !m_IndexBuffer)
		return;

	auto context = Window::GetContext();

	if (s_PSO && m_SRB)
	{
		// Update Textures
		if (auto* pVar = m_SRB->GetVariableByName(SHADER_TYPE_PIXEL, "u_Textures"))
		{
			std::vector<IDeviceObject*> pTexObjs;
			for (auto* pView: m_TextureSlots)
			{
				pTexObjs.push_back(pView);
			}
			// Fill remaining with white texture
			while (pTexObjs.size() < 32)
			{
				pTexObjs.push_back(Renderer2D::GetWhiteTextureSRV());
			}

			pVar->SetArray(pTexObjs.data(), 0, (Uint32) pTexObjs.size(), SET_SHADER_RESOURCE_FLAG_ALLOW_OVERWRITE);
		}

		if (auto* pShader = Renderer2D::GetShader())
		{
			if (auto* pCB = pShader->GetConstantBuffer())
			{
				if (auto* pVar = m_SRB->GetVariableByName(SHADER_TYPE_VERTEX, "ConstantBuffer"))
					pVar->Set(pCB);
			}
		}

		context->SetPipelineState(s_PSO);
		context->CommitShaderResources(m_SRB, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

		IBuffer* pVBs[] = { m_VertexBuffer };
		Uint64 offsets[] = { 0 };
		context->SetVertexBuffers(0, 1, pVBs, offsets, RESOURCE_STATE_TRANSITION_MODE_TRANSITION, SET_VERTEX_BUFFERS_FLAG_RESET);
		context->SetIndexBuffer(m_IndexBuffer, 0, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

		DrawIndexedAttribs DrawAttrs;
		DrawAttrs.NumIndices = m_IndexCount;
		DrawAttrs.IndexType = VT_UINT32;
		DrawAttrs.Flags = DRAW_FLAG_VERIFY_ALL;
		context->DrawIndexed(DrawAttrs);
	}
}

// ==========================================
// TileMap
// ==========================================

TileMap::TileMap(int width, int height, float tileSize)
      : m_Width(width), m_Height(height), m_TileSize(tileSize)
{
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
	if (auto* pShader = Renderer2D::GetShader())
	{
		pShader->SetMat4("u_ViewProjection", viewProj);
	}

	for (auto* chunk: m_Chunks)
		chunk->Render(viewProj);
}
