#pragma once
#include <vector>
#include <d3d11.h>
#include <wrl/client.h>
#include <glm.hpp>
#include "Rendering/Renderer2D.h"

class TileMapChunk
{
public:
    struct TileVertex
    {
        glm::vec3 Position;
        glm::vec4 Color;
        glm::vec2 TexCoord;
        float TexIndex;
        float TilingFactor;
        float IsText;
    };

    TileMapChunk(int offsetX, int offsetY, int width, int height, float tileSize);
    ~TileMapChunk();

    void SetTile(int x, int y, int layer, void* texturePtr, float r, float g, float b, float a, float rotation = 0.0f);
    void UpdateMesh();
    void Render(const glm::mat4& viewProj);
    bool IsVisible(const glm::mat4& viewProj);

private:
    int m_OffsetX, m_OffsetY;
    int m_Width, m_Height;
    float m_TileSize;

    struct TileInfo
    {
        void* Texture;
        glm::vec4 Color;
        float Rotation;
        bool Active;
    };

    // 3 Layers: Terrain, Ore, Structure
    std::vector<TileInfo> m_Layers[3];

    Microsoft::WRL::ComPtr<ID3D11Buffer> m_VertexBuffer;
    Microsoft::WRL::ComPtr<ID3D11Buffer> m_IndexBuffer;
    
    std::vector<TileVertex> m_Vertices;
    std::vector<uint32_t> m_Indices;
    
    // Texture management
    std::vector<ID3D11ShaderResourceView*> m_TextureSlots;
    
    bool m_Dirty;
    uint32_t m_IndexCount;
};

class TileMap
{
public:
    TileMap(int width, int height, float tileSize);
    ~TileMap();

    void SetTile(int x, int y, int layer, void* texturePtr, float r, float g, float b, float a, float rotation = 0.0f);
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
};
