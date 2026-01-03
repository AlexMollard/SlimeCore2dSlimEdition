#pragma once
#include <glm.hpp>
#include <vector>

#include "Buffer.h"
#include "RefCntAutoPtr.hpp"
#include "Rendering/Renderer2D.h"
#include "ShaderResourceBinding.h"

using namespace Diligent;

class TileMapChunk
{
public:
	struct TileInstance
	{
		float Position[2];
		float Size[2];
		float TexRect[4];
		float Color[4];
		float TexIndex;
		float Rotation;
	};

	TileMapChunk(int offsetX, int offsetY, int width, int height, float tileSize);
	~TileMapChunk();

	void SetTile(int x, int y, int layer, Texture* texturePtr, float u0, float v0, float u1, float v1, float r, float g, float b, float a, float rotation = 0.0f);
	void UpdateMesh();
	void Render(IBuffer* pConstantBuffer);

private:
	int m_OffsetX, m_OffsetY;
	int m_Width, m_Height;
	float m_TileSize;

	struct TileInfo
	{
		Texture* Texture;
		glm::vec4 TexRect; // u0, v0, u1, v1
		glm::vec4 Color;
		float Rotation;
		bool Active;
	};

	// 3 Layers: Terrain, Ore, Structure
	std::vector<TileInfo> m_Layers[3];

	RefCntAutoPtr<IBuffer> m_InstanceBuffer;
	RefCntAutoPtr<IShaderResourceBinding> m_SRB;

	std::vector<TileInstance> m_Instances;

	// Texture management
	std::vector<ITextureView*> m_TextureSlots;

	bool m_Dirty;
	uint32_t m_InstanceCount;

	static RefCntAutoPtr<IPipelineState> s_PSO;
	static RefCntAutoPtr<IBuffer> s_QuadVB;
	static RefCntAutoPtr<IBuffer> s_QuadIB;

	static void InitCommonResources();
};

class TileMap
{
public:
	TileMap(int width, int height, float tileSize);
	~TileMap();

	void SetTile(int x, int y, int layer, Texture* texturePtr, float u0, float v0, float u1, float v1, float r, float g, float b, float a, float rotation = 0.0f);
	void UpdateMesh();
	void Render(const glm::mat4& viewProj);

private:
	int m_Width;
	int m_Height;
	float m_TileSize;

	int m_ChunkWidth = 32;
	int m_ChunkHeight = 32;
	int m_ChunksX;
	int m_ChunksY;

	std::vector<TileMapChunk*> m_Chunks;

	RefCntAutoPtr<IBuffer> m_VSConstants;
	void CreateConstantBuffer();
};
