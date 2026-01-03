#pragma once
#include <glm.hpp>
#include <vector>

#include "Buffer.h"
#include "RefCntAutoPtr.hpp"
#include "Shader.h"
#include "ShaderResourceBinding.h"

using namespace Diligent;

struct ConveyorTile
{
	int Tier = 0;      // 0 = None
	int Direction = 0; // 0=N, 1=E, 2=S, 3=W
	bool Active = false;
};

class ConveyorMap
{
public:
	ConveyorMap(int width, int height, float tileSize);
	~ConveyorMap();

	void SetConveyor(int x, int y, int tier, int direction);
	void RemoveConveyor(int x, int y);

	void UpdateMesh();
	void Render(const glm::mat4& viewProj, float time);

private:
	int m_Width;
	int m_Height;
	float m_TileSize;

	std::vector<ConveyorTile> m_Map;

	struct Vertex
	{
		glm::vec3 Position;
		glm::vec4 Color;
		glm::vec2 TexCoord;
		float IsBelt;       // Maps to TEXCOORD1
		float TilingFactor; // Maps to TILING (Unused but required for layout alignment)
		float Padding;      // Maps to ISTEXT (Unused but required for layout alignment)
	};

	RefCntAutoPtr<IBuffer> m_VertexBuffer;
	RefCntAutoPtr<IBuffer> m_IndexBuffer;
	RefCntAutoPtr<IPipelineState> m_PSO;
	RefCntAutoPtr<IShaderResourceBinding> m_SRB;

	std::vector<Vertex> m_Vertices;
	std::vector<uint32_t> m_Indices;

	Shader* m_Shader;
	uint32_t m_IndexCount = 0;
	uint32_t m_VertexCapacity = 0;
	uint32_t m_IndexCapacity = 0;
	bool m_Dirty = false;

	int GetDirection(int x, int y);
	bool HasConveyor(int x, int y);

	glm::vec4 GetTierColor(int tier);
};
