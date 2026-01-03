#include "ConveyorMap.h"

#include <gtc/matrix_transform.hpp>

#include "Core/Window.h"
#include "Rendering/Renderer2D.h"
#include "Resources/ResourceManager.h"

using namespace Diligent;

ConveyorMap::ConveyorMap(int width, int height, float tileSize)
      : m_Width(width), m_Height(height), m_TileSize(tileSize)
{
	m_Map.resize(width * height);

	m_Shader = ResourceManager::GetInstance().GetShader("conveyor");
	if (!m_Shader)
		m_Shader = ResourceManager::GetInstance().GetShader("conveyor_");

	if (!m_Shader)
	{
		std::string vsPath = ResourceManager::GetInstance().GetResourcePath("Game/Resources/Shaders/conveyor_vertex.hlsl");
		std::string psPath = ResourceManager::GetInstance().GetResourcePath("Game/Resources/Shaders/conveyor_pixel.hlsl");

		if (!vsPath.empty() && !psPath.empty())
		{
			ResourceManager::GetInstance().AddShader("conveyor", vsPath, psPath);
			m_Shader = ResourceManager::GetInstance().GetShader("conveyor");
		}
	}

	if (!m_Shader)
	{
		m_Shader = ResourceManager::GetInstance().GetShader("basic");
	}

	if (m_Shader)
	{
		// Create PSO
		GraphicsPipelineStateCreateInfo PSOCreateInfo;
		PSOCreateInfo.PSODesc.Name = "ConveyorMap PSO";
		PSOCreateInfo.PSODesc.PipelineType = PIPELINE_TYPE_GRAPHICS;
		PSOCreateInfo.GraphicsPipeline.NumRenderTargets = 1;
		PSOCreateInfo.GraphicsPipeline.RTVFormats[0] = TEX_FORMAT_RGBA8_UNORM;
		PSOCreateInfo.GraphicsPipeline.DSVFormat = TEX_FORMAT_D24_UNORM_S8_UINT;
		PSOCreateInfo.GraphicsPipeline.PrimitiveTopology = PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;

		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendEnable = true;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].SrcBlend = BLEND_FACTOR_SRC_ALPHA;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].DestBlend = BLEND_FACTOR_INV_SRC_ALPHA;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendOp = BLEND_OPERATION_ADD;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].SrcBlendAlpha = BLEND_FACTOR_ONE;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].DestBlendAlpha = BLEND_FACTOR_ZERO;
		PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendOpAlpha = BLEND_OPERATION_ADD;

		PSOCreateInfo.GraphicsPipeline.RasterizerDesc.CullMode = CULL_MODE_NONE;
		PSOCreateInfo.GraphicsPipeline.RasterizerDesc.FillMode = FILL_MODE_SOLID;
		PSOCreateInfo.GraphicsPipeline.RasterizerDesc.FrontCounterClockwise = false;

		PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthEnable = false;
		PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthWriteEnable = false;
		PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthFunc = COMPARISON_FUNC_ALWAYS;

		LayoutElement LayoutElems[] = {
			LayoutElement{ 0, 0, 3, VT_FLOAT32, False }, // Position -> ATTRIB0
			LayoutElement{ 1, 0, 4, VT_FLOAT32, False }, // Color -> ATTRIB1
			LayoutElement{ 2, 0, 2, VT_FLOAT32, False }, // TexCoord -> ATTRIB2
			LayoutElement{ 3, 0, 1, VT_FLOAT32, False }, // IsBelt -> ATTRIB3
			LayoutElement{ 4, 0, 1, VT_FLOAT32, False }, // TilingFactor -> ATTRIB4
			LayoutElement{ 5, 0, 1, VT_FLOAT32, False }  // Padding -> ATTRIB5
		};
		PSOCreateInfo.GraphicsPipeline.InputLayout.LayoutElements = LayoutElems;
		PSOCreateInfo.GraphicsPipeline.InputLayout.NumElements = _countof(LayoutElems);

		PSOCreateInfo.pVS = m_Shader->GetVertexShader();
		PSOCreateInfo.pPS = m_Shader->GetPixelShader();

		ShaderResourceVariableDesc Vars[] = {
			{			          SHADER_TYPE_PIXEL,     "g_Textures", SHADER_RESOURCE_VARIABLE_TYPE_DYNAMIC },
            { SHADER_TYPE_VERTEX | SHADER_TYPE_PIXEL, "GlobalConstants", SHADER_RESOURCE_VARIABLE_TYPE_MUTABLE }
		};
		PSOCreateInfo.PSODesc.ResourceLayout.Variables = Vars;
		PSOCreateInfo.PSODesc.ResourceLayout.NumVariables = _countof(Vars);

		Window::GetDevice()->CreateGraphicsPipelineState(PSOCreateInfo, &m_PSO);
		m_PSO->CreateShaderResourceBinding(&m_SRB, true);
	}
}

ConveyorMap::~ConveyorMap()
{
	m_VertexBuffer.Release();
	m_IndexBuffer.Release();
	m_PSO.Release();
	m_SRB.Release();
}

void ConveyorMap::SetConveyor(int x, int y, int tier, int direction)
{
	if (x < 0 || x >= m_Width || y < 0 || y >= m_Height)
		return;

	ConveyorTile& tile = m_Map[y * m_Width + x];
	if (tile.Tier != tier || tile.Direction != direction)
	{
		tile.Tier = tier;
		tile.Direction = direction;
		tile.Active = true;
		m_Dirty = true;
	}
}

void ConveyorMap::RemoveConveyor(int x, int y)
{
	if (x < 0 || x >= m_Width || y < 0 || y >= m_Height)
		return;

	ConveyorTile& tile = m_Map[y * m_Width + x];
	if (tile.Active)
	{
		tile.Active = false;
		tile.Tier = 0;
		m_Dirty = true;
	}
}

bool ConveyorMap::HasConveyor(int x, int y)
{
	if (x < 0 || x >= m_Width || y < 0 || y >= m_Height)
		return false;
	return m_Map[y * m_Width + x].Active;
}

int ConveyorMap::GetDirection(int x, int y)
{
	if (x < 0 || x >= m_Width || y < 0 || y >= m_Height)
		return -1;
	return m_Map[y * m_Width + x].Direction;
}

glm::vec4 ConveyorMap::GetTierColor(int tier)
{
	switch (tier)
	{
		case 1:
			return glm::vec4(1.0f, 0.8f, 0.2f, 1.0f); // Yellow
		case 2:
			return glm::vec4(1.0f, 0.2f, 0.2f, 1.0f); // Red
		case 3:
			return glm::vec4(0.2f, 0.2f, 1.0f, 1.0f); // Blue
		default:
			return glm::vec4(0.5f, 0.5f, 0.5f, 1.0f); // Grey
	}
}

void ConveyorMap::UpdateMesh()
{
	if (!m_Dirty)
		return;

	m_Vertices.clear();
	m_Indices.clear();

	glm::vec4 beltColor = glm::vec4(0.2f, 0.2f, 0.2f, 1.0f); // Dark Grey

	for (int y = 0; y < m_Height; y++)
	{
		for (int x = 0; x < m_Width; x++)
		{
			ConveyorTile& tile = m_Map[y * m_Width + x];
			if (!tile.Active)
				continue;

			// Offset by half tile size to center in grid cell
			glm::vec3 pos(x * m_TileSize + m_TileSize * 0.5f, y * m_TileSize + m_TileSize * 0.5f, 0.0f);
			glm::vec4 railColor = GetTierColor(tile.Tier);

			float speed = 1.0f;
			if (tile.Tier == 2)
				speed = 2.0f;
			else if (tile.Tier == 3)
				speed = 4.0f;

			// Determine rotation based on direction
			// 0=N, 1=E, 2=S, 3=W
			float rotation = 0.0f;
			if (tile.Direction == 1)
				rotation = -90.0f; // East
			else if (tile.Direction == 2)
				rotation = 180.0f; // South
			else if (tile.Direction == 3)
				rotation = 90.0f; // West

			// Check neighbors for connections (Marching Squares / Auto-tiling logic)
			// We need to know if neighbors are pointing INTO this tile.

			bool inN = false, inE = false, inS = false, inW = false;

			// North Neighbor (y+1)
			if (HasConveyor(x, y + 1) && GetDirection(x, y + 1) == 2)
				inN = true; // North neighbor pointing South
			// East Neighbor (x+1)
			if (HasConveyor(x + 1, y) && GetDirection(x + 1, y) == 3)
				inE = true; // East neighbor pointing West
			// South Neighbor (y-1)
			if (HasConveyor(x, y - 1) && GetDirection(x, y - 1) == 0)
				inS = true; // South neighbor pointing North
			// West Neighbor (x-1)
			if (HasConveyor(x - 1, y) && GetDirection(x - 1, y) == 1)
				inW = true; // West neighbor pointing East

			// Transform inputs based on our rotation to be relative (Front, Right, Back, Left)
			// Relative to our direction:
			// If we face North (0): N=Front(Output), E=Right, S=Back(Input), W=Left
			// We only care about inputs from Left, Right, Back.
			// Actually, standard belt is straight (Back -> Front).
			// If we have input from Left or Right, we might need a corner.

			bool inputLeft = false;
			bool inputRight = false;
			bool inputBack = false;

			if (tile.Direction == 0) // North
			{
				if (inW)
					inputLeft = true;
				if (inE)
					inputRight = true;
				if (inS)
					inputBack = true;
			}
			else if (tile.Direction == 1) // East
			{
				if (inN)
					inputLeft = true;
				if (inS)
					inputRight = true;
				if (inW)
					inputBack = true;
			}
			else if (tile.Direction == 2) // South
			{
				if (inE)
					inputLeft = true;
				if (inW)
					inputRight = true;
				if (inN)
					inputBack = true;
			}
			else if (tile.Direction == 3) // West
			{
				if (inS)
					inputLeft = true;
				if (inN)
					inputRight = true;
				if (inE)
					inputBack = true;
			}

			// Logic for shapes
			// We want to draw a straight belt (possibly with merges) unless it is a pure corner.
			// Pure Corner Left: Input Left only (no Back, no Right).
			// Pure Corner Right: Input Right only (no Back, no Left).

			bool isCornerLeft = !inputBack && inputLeft && !inputRight;
			bool isCornerRight = !inputBack && !inputLeft && inputRight;
			bool drawStraight = !isCornerLeft && !isCornerRight;

			// Convert rotation to radians
			rotation = glm::radians(rotation);

			glm::mat4 transform = glm::translate(glm::mat4(1.0f), pos) * glm::rotate(glm::mat4(1.0f), rotation, glm::vec3(0, 0, 1));

			auto AddCornerMesh = [&](bool isLeft, bool withOuterRail)
			{
				glm::vec3 center;
				float startAngle, endAngle;

				if (isLeft)
				{
					center = glm::vec3(-0.5f * m_TileSize, 0.5f * m_TileSize, 0.0f);
					startAngle = -glm::half_pi<float>();
					endAngle = 0.0f;
				}
				else
				{
					center = glm::vec3(0.5f * m_TileSize, 0.5f * m_TileSize, 0.0f);
					startAngle = -glm::half_pi<float>();
					endAngle = -glm::pi<float>();
				}

				float innerR = 0.2f * m_TileSize;
				float outerR = 0.8f * m_TileSize;
				float railW = 0.1f * m_TileSize;
				int segments = 8;

				// Belt
				for (int i = 0; i < segments; i++)
				{
					float t0 = (float) i / segments;
					float t1 = (float) (i + 1) / segments;
					float a0 = glm::mix(startAngle, endAngle, t0);
					float a1 = glm::mix(startAngle, endAngle, t1);

					glm::vec3 p0_in = center + glm::vec3(cos(a0), sin(a0), 0.0f) * innerR;
					glm::vec3 p0_out = center + glm::vec3(cos(a0), sin(a0), 0.0f) * outerR;
					glm::vec3 p1_in = center + glm::vec3(cos(a1), sin(a1), 0.0f) * innerR;
					glm::vec3 p1_out = center + glm::vec3(cos(a1), sin(a1), 0.0f) * outerR;

					p0_in = glm::vec3(transform * glm::vec4(p0_in, 1.0f));
					p0_out = glm::vec3(transform * glm::vec4(p0_out, 1.0f));
					p1_in = glm::vec3(transform * glm::vec4(p1_in, 1.0f));
					p1_out = glm::vec3(transform * glm::vec4(p1_out, 1.0f));

					uint32_t baseIndex = (uint32_t) m_Vertices.size();
					Vertex v[4];

					if (isLeft)
					{
						v[0].Position = p0_out;
						v[0].TexCoord = glm::vec2(0.0f, 1.0f - t0);
						v[1].Position = p0_in;
						v[1].TexCoord = glm::vec2(1.0f, 1.0f - t0);
						v[2].Position = p1_in;
						v[2].TexCoord = glm::vec2(1.0f, 1.0f - t1);
						v[3].Position = p1_out;
						v[3].TexCoord = glm::vec2(0.0f, 1.0f - t1);
					}
					else
					{
						v[0].Position = p0_in;
						v[0].TexCoord = glm::vec2(0.0f, 1.0f - t0);
						v[1].Position = p0_out;
						v[1].TexCoord = glm::vec2(1.0f, 1.0f - t0);
						v[2].Position = p1_out;
						v[2].TexCoord = glm::vec2(1.0f, 1.0f - t1);
						v[3].Position = p1_in;
						v[3].TexCoord = glm::vec2(0.0f, 1.0f - t1);
					}

					for (int k = 0; k < 4; k++)
					{
						v[k].Color = beltColor;
						v[k].IsBelt = 1.0f;
						v[k].TilingFactor = 1.0f;
						v[k].Padding = speed;
						m_Vertices.push_back(v[k]);
					}
					m_Indices.push_back(baseIndex + 0);
					m_Indices.push_back(baseIndex + 1);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 3);
					m_Indices.push_back(baseIndex + 0);
				}

				// Inner Rail
				for (int i = 0; i < segments; i++)
				{
					float t0 = (float) i / segments;
					float t1 = (float) (i + 1) / segments;
					float a0 = glm::mix(startAngle, endAngle, t0);
					float a1 = glm::mix(startAngle, endAngle, t1);

					float r0 = innerR - railW;
					float r1 = innerR;

					glm::vec3 p0_in = center + glm::vec3(cos(a0), sin(a0), 0.0f) * r0;
					glm::vec3 p0_out = center + glm::vec3(cos(a0), sin(a0), 0.0f) * r1;
					glm::vec3 p1_in = center + glm::vec3(cos(a1), sin(a1), 0.0f) * r0;
					glm::vec3 p1_out = center + glm::vec3(cos(a1), sin(a1), 0.0f) * r1;

					p0_in = glm::vec3(transform * glm::vec4(p0_in, 1.0f));
					p0_out = glm::vec3(transform * glm::vec4(p0_out, 1.0f));
					p1_in = glm::vec3(transform * glm::vec4(p1_in, 1.0f));
					p1_out = glm::vec3(transform * glm::vec4(p1_out, 1.0f));

					uint32_t baseIndex = (uint32_t) m_Vertices.size();
					Vertex v[4];
					v[0].Position = p0_out;
					v[0].TexCoord = glm::vec2(0, 0);
					v[1].Position = p0_in;
					v[1].TexCoord = glm::vec2(1, 0);
					v[2].Position = p1_in;
					v[2].TexCoord = glm::vec2(1, 1);
					v[3].Position = p1_out;
					v[3].TexCoord = glm::vec2(0, 1);

					for (int k = 0; k < 4; k++)
					{
						v[k].Color = railColor;
						v[k].IsBelt = 0.0f;
						v[k].TilingFactor = 1.0f;
						v[k].Padding = 0.0f;
						m_Vertices.push_back(v[k]);
					}
					m_Indices.push_back(baseIndex + 0);
					m_Indices.push_back(baseIndex + 1);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 3);
					m_Indices.push_back(baseIndex + 0);
				}

				// Outer Rail
				if (withOuterRail)
				{
					for (int i = 0; i < segments; i++)
					{
						float t0 = (float) i / segments;
						float t1 = (float) (i + 1) / segments;
						float a0 = glm::mix(startAngle, endAngle, t0);
						float a1 = glm::mix(startAngle, endAngle, t1);

						float r0 = outerR;
						float r1 = outerR + railW;

						glm::vec3 p0_in = center + glm::vec3(cos(a0), sin(a0), 0.0f) * r0;
						glm::vec3 p0_out = center + glm::vec3(cos(a0), sin(a0), 0.0f) * r1;
						glm::vec3 p1_in = center + glm::vec3(cos(a1), sin(a1), 0.0f) * r0;
						glm::vec3 p1_out = center + glm::vec3(cos(a1), sin(a1), 0.0f) * r1;

						p0_in = glm::vec3(transform * glm::vec4(p0_in, 1.0f));
						p0_out = glm::vec3(transform * glm::vec4(p0_out, 1.0f));
						p1_in = glm::vec3(transform * glm::vec4(p1_in, 1.0f));
						p1_out = glm::vec3(transform * glm::vec4(p1_out, 1.0f));

						uint32_t baseIndex = (uint32_t) m_Vertices.size();
						Vertex v[4];
						v[0].Position = p0_out;
						v[0].TexCoord = glm::vec2(0, 0);
						v[1].Position = p0_in;
						v[1].TexCoord = glm::vec2(1, 0);
						v[2].Position = p1_in;
						v[2].TexCoord = glm::vec2(1, 1);
						v[3].Position = p1_out;
						v[3].TexCoord = glm::vec2(0, 1);

						for (int k = 0; k < 4; k++)
						{
							v[k].Color = railColor;
							v[k].IsBelt = 0.0f;
							v[k].TilingFactor = 1.0f;
							v[k].Padding = 0.0f;
							m_Vertices.push_back(v[k]);
						}
						m_Indices.push_back(baseIndex + 0);
						m_Indices.push_back(baseIndex + 1);
						m_Indices.push_back(baseIndex + 2);
						m_Indices.push_back(baseIndex + 2);
						m_Indices.push_back(baseIndex + 3);
						m_Indices.push_back(baseIndex + 0);
					}
				}
			};

			auto AddMergeMesh = [&](bool isLeft)
			{
				float xStart = isLeft ? -0.5f * m_TileSize : 0.5f * m_TileSize;
				float xEnd = isLeft ? -0.3f * m_TileSize : 0.3f * m_TileSize;

				// Belt
				{
					float yTop = 0.3f * m_TileSize;
					float yBottom = -0.3f * m_TileSize;

					glm::vec3 p0(xStart, yTop, 0);
					glm::vec3 p1(xEnd, yTop, 0);
					glm::vec3 p2(xEnd, yBottom, 0);
					glm::vec3 p3(xStart, yBottom, 0);

					p0 = glm::vec3(transform * glm::vec4(p0, 1.0f));
					p1 = glm::vec3(transform * glm::vec4(p1, 1.0f));
					p2 = glm::vec3(transform * glm::vec4(p2, 1.0f));
					p3 = glm::vec3(transform * glm::vec4(p3, 1.0f));

					uint32_t baseIndex = (uint32_t) m_Vertices.size();
					Vertex v[4];

					if (isLeft)
					{
						// Flow East. U=0 at Top (Left side of flow). V=1 at Start.
						v[0].Position = p0;
						v[0].TexCoord = glm::vec2(0.0f, 1.0f);
						v[1].Position = p1;
						v[1].TexCoord = glm::vec2(0.0f, 0.8f);
						v[2].Position = p2;
						v[2].TexCoord = glm::vec2(1.0f, 0.8f);
						v[3].Position = p3;
						v[3].TexCoord = glm::vec2(1.0f, 1.0f);
					}
					else
					{
						// Flow West. U=0 at Bottom (Left side of flow). V=1 at Start.
						v[0].Position = p3;
						v[0].TexCoord = glm::vec2(0.0f, 1.0f);
						v[1].Position = p2;
						v[1].TexCoord = glm::vec2(0.0f, 0.8f);
						v[2].Position = p1;
						v[2].TexCoord = glm::vec2(1.0f, 0.8f);
						v[3].Position = p0;
						v[3].TexCoord = glm::vec2(1.0f, 1.0f);
					}

					for (int k = 0; k < 4; k++)
					{
						v[k].Color = beltColor;
						v[k].IsBelt = 1.0f;
						v[k].TilingFactor = 1.0f;
						v[k].Padding = speed;
						m_Vertices.push_back(v[k]);
					}
					m_Indices.push_back(baseIndex + 0);
					m_Indices.push_back(baseIndex + 1);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 3);
					m_Indices.push_back(baseIndex + 0);
				}

				// Rails
				auto AddRailRect = [&](float yMin, float yMax)
				{
					glm::vec3 p0(xStart, yMax, 0);
					glm::vec3 p1(xEnd, yMax, 0);
					glm::vec3 p2(xEnd, yMin, 0);
					glm::vec3 p3(xStart, yMin, 0);

					p0 = glm::vec3(transform * glm::vec4(p0, 1.0f));
					p1 = glm::vec3(transform * glm::vec4(p1, 1.0f));
					p2 = glm::vec3(transform * glm::vec4(p2, 1.0f));
					p3 = glm::vec3(transform * glm::vec4(p3, 1.0f));

					uint32_t baseIndex = (uint32_t) m_Vertices.size();
					Vertex v[4];
					v[0].Position = p0;
					v[0].TexCoord = glm::vec2(0, 0);
					v[1].Position = p1;
					v[1].TexCoord = glm::vec2(1, 0);
					v[2].Position = p2;
					v[2].TexCoord = glm::vec2(1, 1);
					v[3].Position = p3;
					v[3].TexCoord = glm::vec2(0, 1);

					for (int k = 0; k < 4; k++)
					{
						v[k].Color = railColor;
						v[k].IsBelt = 0.0f;
						v[k].TilingFactor = 1.0f;
						v[k].Padding = 0.0f;
						m_Vertices.push_back(v[k]);
					}
					m_Indices.push_back(baseIndex + 0);
					m_Indices.push_back(baseIndex + 1);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 3);
					m_Indices.push_back(baseIndex + 0);
				};

				AddRailRect(0.3f * m_TileSize, 0.4f * m_TileSize);
				AddRailRect(-0.4f * m_TileSize, -0.3f * m_TileSize);
			};

			auto AddStraightRail = [&](bool isLeft, float yMin, float yMax)
			{
				float w = m_TileSize * 0.1f;
				float xOffset = isLeft ? -m_TileSize * 0.35f : m_TileSize * 0.35f;

				glm::vec3 offsets[4] = {
					{ xOffset - 0.5f * w, yMin, 0.0f },
                    { xOffset + 0.5f * w, yMin, 0.0f },
                    { xOffset + 0.5f * w, yMax, 0.0f },
                    { xOffset - 0.5f * w, yMax, 0.0f }
				};

				glm::vec2 uvs[4] = {
					{ 0, 0 },
                    { 1, 0 },
                    { 1, 1 },
                    { 0, 1 }
				};

				uint32_t baseIndex = (uint32_t) m_Vertices.size();
				for (int i = 0; i < 4; i++)
				{
					Vertex vert;
					vert.Position = glm::vec3(transform * glm::vec4(offsets[i], 1.0f));
					vert.Color = railColor;
					vert.TexCoord = uvs[i];
					vert.IsBelt = 0.0f;
					vert.TilingFactor = 1.0f;
					vert.Padding = 0.0f;
					m_Vertices.push_back(vert);
				}
				m_Indices.push_back(baseIndex + 0);
				m_Indices.push_back(baseIndex + 3);
				m_Indices.push_back(baseIndex + 2);
				m_Indices.push_back(baseIndex + 0);
				m_Indices.push_back(baseIndex + 2);
				m_Indices.push_back(baseIndex + 1);
			};

			if (drawStraight)
			{
				// Straight Belt (Center)
				{
					Vertex v[4];
					float w = m_TileSize * 0.6f;
					float h = m_TileSize;

					glm::vec3 offsets[4] = {
						{ -0.5f * w, -0.5f * h, 0.0f },
                        {  0.5f * w, -0.5f * h, 0.0f },
                        {  0.5f * w,  0.5f * h, 0.0f },
                        { -0.5f * w,  0.5f * h, 0.0f }
					};

					glm::vec2 uvs[4] = {
						{ 0, 1 },
                        { 1, 1 },
                        { 1, 0 },
                        { 0, 0 }
					};

					uint32_t baseIndex = (uint32_t) m_Vertices.size();

					for (int i = 0; i < 4; i++)
					{
						Vertex vert;
						vert.Position = glm::vec3(transform * glm::vec4(offsets[i], 1.0f));
						vert.Color = beltColor;
						vert.TexCoord = uvs[i];
						vert.IsBelt = 1.0f;
						vert.TilingFactor = 1.0f;
						vert.Padding = speed;
						m_Vertices.push_back(vert);
					}

					m_Indices.push_back(baseIndex + 0);
					m_Indices.push_back(baseIndex + 3);
					m_Indices.push_back(baseIndex + 2);

					m_Indices.push_back(baseIndex + 0);
					m_Indices.push_back(baseIndex + 2);
					m_Indices.push_back(baseIndex + 1);
				}

				// Left Rail
				if (!inputLeft)
				{
					AddStraightRail(true, -0.5f * m_TileSize, 0.5f * m_TileSize);
				}
				else
				{
					// Merge from Left
					AddMergeMesh(true);
					// Fill corners
					AddStraightRail(true, 0.3f * m_TileSize, 0.5f * m_TileSize);
					AddStraightRail(true, -0.5f * m_TileSize, -0.3f * m_TileSize);
				}

				// Right Rail
				if (!inputRight)
				{
					AddStraightRail(false, -0.5f * m_TileSize, 0.5f * m_TileSize);
				}
				else
				{
					// Merge from Right
					AddMergeMesh(false);
					// Fill corners
					AddStraightRail(false, 0.3f * m_TileSize, 0.5f * m_TileSize);
					AddStraightRail(false, -0.5f * m_TileSize, -0.3f * m_TileSize);
				}
			}
			else
			{
				if (isCornerLeft)
				{
					AddCornerMesh(true, true);
				}

				if (isCornerRight)
				{
					AddCornerMesh(false, true);
				}
			}
		}
	}

	m_IndexCount = (uint32_t) m_Indices.size();
	m_Dirty = false;

	if (m_Vertices.empty())
		return;

	// Upload to GPU
	auto device = Window::GetDevice();
	auto context = Window::GetContext();

	// Vertex Buffer
	if (!m_VertexBuffer || m_Vertices.size() > m_VertexCapacity)
	{
		m_VertexCapacity = (uint32_t) m_Vertices.size() + 500;

		Diligent::BufferDesc vbDesc;
		vbDesc.Name = "ConveyorMap VB";
		vbDesc.Size = (Uint64) (m_VertexCapacity * sizeof(Vertex));
		vbDesc.Usage = USAGE_DYNAMIC;
		vbDesc.BindFlags = BIND_VERTEX_BUFFER;
		vbDesc.CPUAccessFlags = CPU_ACCESS_WRITE;

		device->CreateBuffer(vbDesc, nullptr, &m_VertexBuffer);
	}

	void* pData;
	context->MapBuffer(m_VertexBuffer, MAP_WRITE, MAP_FLAG_DISCARD, pData);
	memcpy(pData, m_Vertices.data(), m_Vertices.size() * sizeof(Vertex));
	context->UnmapBuffer(m_VertexBuffer, MAP_WRITE);

	// Index Buffer
	if (!m_IndexBuffer || m_Indices.size() > m_IndexCapacity)
	{
		m_IndexCapacity = (uint32_t) m_Indices.size() + 500;

		Diligent::BufferDesc ibDesc;
		ibDesc.Name = "ConveyorMap IB";
		ibDesc.Size = (Uint64) (m_IndexCapacity * sizeof(uint32_t));
		ibDesc.Usage = USAGE_DYNAMIC;
		ibDesc.BindFlags = BIND_INDEX_BUFFER;
		ibDesc.CPUAccessFlags = CPU_ACCESS_WRITE;

		device->CreateBuffer(ibDesc, nullptr, &m_IndexBuffer);
	}

	context->MapBuffer(m_IndexBuffer, MAP_WRITE, MAP_FLAG_DISCARD, pData);
	memcpy(pData, m_Indices.data(), m_Indices.size() * sizeof(uint32_t));
	context->UnmapBuffer(m_IndexBuffer, MAP_WRITE);
}

void ConveyorMap::Render(const glm::mat4& viewProj, float time)
{
	if (m_Dirty)
		UpdateMesh();
	if (m_IndexCount == 0 || !m_VertexBuffer)
		return;

	auto context = Window::GetContext();

	if (m_Shader && m_PSO)
	{
		m_Shader->setFloat("u_Time", time);
		m_Shader->SetMat4("u_ViewProjection", viewProj);

		// Update Constant Buffer in SRB
		if (auto* pCB = m_Shader->GetConstantBuffer())
		{
			if (auto* pVar = m_SRB->GetVariableByName(SHADER_TYPE_VERTEX, "GlobalConstants"))
				pVar->Set(pCB);
			if (auto* pVar = m_SRB->GetVariableByName(SHADER_TYPE_PIXEL, "GlobalConstants"))
				pVar->Set(pCB);
		}

		// Bind Textures (Fallback to white texture if needed)
		if (auto* pVar = m_SRB->GetVariableByName(SHADER_TYPE_PIXEL, "g_Textures"))
		{
			if (Renderer2D::GetWhiteTextureSRV())
			{
				IDeviceObject* pTex[] = { Renderer2D::GetWhiteTextureSRV() };
				pVar->SetArray(pTex, 0, 1);
			}
		}

		context->SetPipelineState(m_PSO);
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
