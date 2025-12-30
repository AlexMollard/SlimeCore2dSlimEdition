#include "TileMap.h"
#include "Core/Window.h"
#include "Core/Logger.h"
#include "Rendering/Texture.h"
#include <algorithm>
#include <cmath>

// --- TileMapChunk Implementation ---

TileMapChunk::TileMapChunk(int offsetX, int offsetY, int width, int height, float tileSize)
    : m_OffsetX(offsetX), m_OffsetY(offsetY), m_Width(width), m_Height(height), m_TileSize(tileSize), m_Dirty(true), m_IndexCount(0)
{
    int count = width * height;
    m_Layers[0].resize(count, { nullptr, {0,0,0,0}, 0.0f, false });
    m_Layers[1].resize(count, { nullptr, {0,0,0,0}, 0.0f, false });
    m_Layers[2].resize(count, { nullptr, {0,0,0,0}, 0.0f, false });
}

TileMapChunk::~TileMapChunk()
{
    m_VertexBuffer.Reset();
    m_IndexBuffer.Reset();
}

void TileMapChunk::SetTile(int x, int y, int layer, void* texturePtr, float r, float g, float b, float a, float rotation)
{
    // x and y are local to the chunk
    if (x < 0 || x >= m_Width || y < 0 || y >= m_Height || layer < 0 || layer > 2)
        return;

    int index = y * m_Width + x;
    m_Layers[layer][index].Texture = texturePtr;
    m_Layers[layer][index].Color = { r, g, b, a };
    m_Layers[layer][index].Rotation = rotation;
    m_Layers[layer][index].Active = (a > 0.0f);
    m_Dirty = true;
}

void TileMapChunk::UpdateMesh()
{
    if (!m_Dirty) return;

    m_Vertices.clear();
    m_Indices.clear();
    m_TextureSlots.clear();
    m_TextureSlots.push_back(nullptr); // Slot 0 reserved

    auto getTexIndex = [&](void* texPtr) -> float {
        if (!texPtr) return 0.0f;
        
        Texture* tex = (Texture*)texPtr;
        ID3D11ShaderResourceView* srv = tex->GetSRV();

        for (size_t i = 1; i < m_TextureSlots.size(); i++)
        {
            if (m_TextureSlots[i] == srv)
                return (float)i;
        }

        if (m_TextureSlots.size() >= 32)
        {
            return 0.0f; 
        }

        m_TextureSlots.push_back(srv);
        return (float)(m_TextureSlots.size() - 1);
    };

    uint32_t vertexOffset = 0;

    for (int layer = 0; layer < 3; layer++)
    {
        for (int y = 0; y < m_Height; y++)
        {
            for (int x = 0; x < m_Width; x++)
            {
                int index = y * m_Width + x;
                const auto& tile = m_Layers[layer][index];

                if (!tile.Active) continue;

                float texIndex = getTexIndex(tile.Texture);
                
                // Calculate global position
                float xPos = (float)(m_OffsetX + x) * m_TileSize;
                float yPos = (float)(m_OffsetY + y) * m_TileSize;
                
                float halfSize = m_TileSize * 0.5f;
                float cx = xPos + halfSize;
                float cy = yPos + halfSize;

                glm::vec3 pos[4] = {
                    { -halfSize, -halfSize, 0.0f },
                    {  halfSize, -halfSize, 0.0f },
                    {  halfSize,  halfSize, 0.0f },
                    { -halfSize,  halfSize, 0.0f }
                };

                if (tile.Rotation != 0.0f)
                {
                    float c = cos(tile.Rotation);
                    float s = sin(tile.Rotation);
                    for (int i = 0; i < 4; i++)
                    {
                        float rx = pos[i].x * c - pos[i].y * s;
                        float ry = pos[i].x * s + pos[i].y * c;
                        pos[i].x = rx;
                        pos[i].y = ry;
                    }
                }

                TileVertex v[4];
                v[0].Position = { cx + pos[0].x, cy + pos[0].y, 0.0f };
                v[0].TexCoord = { 0.0f, 0.0f };
                
                v[1].Position = { cx + pos[1].x, cy + pos[1].y, 0.0f };
                v[1].TexCoord = { 1.0f, 0.0f };
                
                v[2].Position = { cx + pos[2].x, cy + pos[2].y, 0.0f };
                v[2].TexCoord = { 1.0f, 1.0f };
                
                v[3].Position = { cx + pos[3].x, cy + pos[3].y, 0.0f };
                v[3].TexCoord = { 0.0f, 1.0f };

                for (int i = 0; i < 4; i++)
                {
                    v[i].Color = tile.Color;
                    v[i].TexIndex = texIndex;
                    v[i].TilingFactor = 1.0f;
                    v[i].IsText = 0.0f;
                    m_Vertices.push_back(v[i]);
                }

                m_Indices.push_back(vertexOffset + 0);
                m_Indices.push_back(vertexOffset + 1);
                m_Indices.push_back(vertexOffset + 2);
                m_Indices.push_back(vertexOffset + 2);
                m_Indices.push_back(vertexOffset + 3);
                m_Indices.push_back(vertexOffset + 0);

                vertexOffset += 4;
            }
        }
    }

    m_IndexCount = (uint32_t)m_Indices.size();

    auto device = Window::GetDevice();

    if (m_Vertices.size() > 0)
    {
        D3D11_BUFFER_DESC vbDesc = {};
        vbDesc.ByteWidth = (UINT)(m_Vertices.size() * sizeof(TileVertex));
        vbDesc.Usage = D3D11_USAGE_DEFAULT;
        vbDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
        
        D3D11_SUBRESOURCE_DATA vbData = {};
        vbData.pSysMem = m_Vertices.data();

        device->CreateBuffer(&vbDesc, &vbData, m_VertexBuffer.ReleaseAndGetAddressOf());
    }

    if (m_Indices.size() > 0)
    {
        D3D11_BUFFER_DESC ibDesc = {};
        ibDesc.ByteWidth = (UINT)(m_Indices.size() * sizeof(uint32_t));
        ibDesc.Usage = D3D11_USAGE_DEFAULT;
        ibDesc.BindFlags = D3D11_BIND_INDEX_BUFFER;

        D3D11_SUBRESOURCE_DATA ibData = {};
        ibData.pSysMem = m_Indices.data();

        device->CreateBuffer(&ibDesc, &ibData, m_IndexBuffer.ReleaseAndGetAddressOf());
    }

    m_Dirty = false;
}

bool TileMapChunk::IsVisible(const glm::mat4& viewProj)
{
    float minX = (float)m_OffsetX * m_TileSize;
    float minY = (float)m_OffsetY * m_TileSize;
    float maxX = (float)(m_OffsetX + m_Width) * m_TileSize;
    float maxY = (float)(m_OffsetY + m_Height) * m_TileSize;

    glm::vec4 corners[4] = {
        {minX, minY, 0.0f, 1.0f},
        {maxX, minY, 0.0f, 1.0f},
        {maxX, maxY, 0.0f, 1.0f},
        {minX, maxY, 0.0f, 1.0f}
    };

    bool allLeft = true;
    bool allRight = true;
    bool allBottom = true;
    bool allTop = true;

    for (int i = 0; i < 4; i++)
    {
        glm::vec4 clipPos = viewProj * corners[i];
        
        if (clipPos.x >= -clipPos.w) allLeft = false;
        if (clipPos.x <= clipPos.w) allRight = false;
        if (clipPos.y >= -clipPos.w) allBottom = false;
        if (clipPos.y <= clipPos.w) allTop = false;
    }

    return !(allLeft || allRight || allBottom || allTop);
}

void TileMapChunk::Render(const glm::mat4& viewProj)
{
    if (m_Dirty) UpdateMesh();
    if (m_IndexCount == 0) return;

    auto context = Window::GetContext();
    
    UINT stride = sizeof(TileVertex);
    UINT offset = 0;
    context->IASetVertexBuffers(0, 1, m_VertexBuffer.GetAddressOf(), &stride, &offset);
    context->IASetIndexBuffer(m_IndexBuffer.Get(), DXGI_FORMAT_R32_UINT, 0);
    context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

    std::vector<ID3D11ShaderResourceView*> slots = m_TextureSlots;
    while (slots.size() < 32) slots.push_back(nullptr);
    
    context->PSSetShaderResources(0, (UINT)slots.size(), slots.data());

    context->DrawIndexed(m_IndexCount, 0, 0);
}

// --- TileMap Implementation ---

TileMap::TileMap(int width, int height, float tileSize)
    : m_Width(width), m_Height(height), m_TileSize(tileSize)
{
    m_ChunksX = (int)ceil((float)width / m_ChunkWidth);
    m_ChunksY = (int)ceil((float)height / m_ChunkHeight);

    for (int y = 0; y < m_ChunksY; y++)
    {
        for (int x = 0; x < m_ChunksX; x++)
        {
            int w = std::min(m_ChunkWidth, width - x * m_ChunkWidth);
            int h = std::min(m_ChunkHeight, height - y * m_ChunkHeight);
            m_Chunks.push_back(new TileMapChunk(x * m_ChunkWidth, y * m_ChunkHeight, w, h, tileSize));
        }
    }
}

TileMap::~TileMap()
{
    for (auto chunk : m_Chunks)
        delete chunk;
    m_Chunks.clear();
}

void TileMap::SetTile(int x, int y, int layer, void* texturePtr, float r, float g, float b, float a, float rotation)
{
    if (x < 0 || x >= m_Width || y < 0 || y >= m_Height) return;

    int cx = x / m_ChunkWidth;
    int cy = y / m_ChunkHeight;
    int chunkIndex = cy * m_ChunksX + cx;

    if (chunkIndex >= 0 && chunkIndex < m_Chunks.size())
    {
        int lx = x % m_ChunkWidth;
        int ly = y % m_ChunkHeight;
        m_Chunks[chunkIndex]->SetTile(lx, ly, layer, texturePtr, r, g, b, a, rotation);
    }
}

void TileMap::UpdateMesh()
{
    for (auto chunk : m_Chunks)
        chunk->UpdateMesh();
}

void TileMap::Render(const glm::mat4& viewProj)
{
    Shader* shader = Renderer2D::GetShader();
    if (shader)
    {
        shader->Bind();
        shader->SetMat4("u_ViewProjection", viewProj);
    }

    for (auto chunk : m_Chunks)
    {
        if (chunk->IsVisible(viewProj))
            chunk->Render(viewProj);
    }
}
