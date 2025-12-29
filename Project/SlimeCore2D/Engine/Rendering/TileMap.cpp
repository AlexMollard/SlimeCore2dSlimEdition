#include "TileMap.h"
#include "Core/Window.h"
#include "Core/Logger.h"
#include "Rendering/Texture.h"
#include <algorithm>

TileMap::TileMap(int width, int height, float tileSize)
    : m_Width(width), m_Height(height), m_TileSize(tileSize), m_Dirty(true), m_IndexCount(0)
{
    int count = width * height;
    m_Layers[0].resize(count, { nullptr, {0,0,0,0}, false });
    m_Layers[1].resize(count, { nullptr, {0,0,0,0}, false });
    m_Layers[2].resize(count, { nullptr, {0,0,0,0}, false });
}

TileMap::~TileMap()
{
    m_VertexBuffer.Reset();
    m_IndexBuffer.Reset();
}

void TileMap::SetTile(int x, int y, int layer, void* texturePtr, float r, float g, float b, float a)
{
    if (x < 0 || x >= m_Width || y < 0 || y >= m_Height || layer < 0 || layer > 2)
        return;

    int index = y * m_Width + x;
    m_Layers[layer][index].Texture = texturePtr;
    m_Layers[layer][index].Color = { r, g, b, a };
    m_Layers[layer][index].Active = (a > 0.0f);
    m_Dirty = true;
}

void TileMap::UpdateMesh()
{
    if (!m_Dirty) return;

    m_Vertices.clear();
    m_Indices.clear();
    m_TextureSlots.clear();
    m_TextureSlots.push_back(nullptr); // Slot 0 reserved (usually white texture in Renderer2D, but we handle it)

    // Helper to find or add texture
    auto getTexIndex = [&](void* texPtr) -> float {
        if (!texPtr) return 0.0f; // Use slot 0 (white) or handle null
        
        Texture* tex = (Texture*)texPtr;
        ID3D11ShaderResourceView* srv = tex->GetSRV();

        for (size_t i = 1; i < m_TextureSlots.size(); i++)
        {
            if (m_TextureSlots[i] == srv)
                return (float)i;
        }

        if (m_TextureSlots.size() >= 32)
        {
            // Fallback or flush? For now, just clamp to 0 to avoid crash
            // Ideally we split draw calls, but for this game we expect < 32 textures
            return 0.0f; 
        }

        m_TextureSlots.push_back(srv);
        return (float)(m_TextureSlots.size() - 1);
    };

    // Get White Texture SRV from Renderer2D or create one?
    // Renderer2D has a private white texture. We can just use nullptr for slot 0 and handle it in shader?
    // Or better, let's just assume slot 0 is white.
    // We need to get a valid SRV for slot 0 if we want to support "no texture" tiles.
    // For now, let's just use the first texture we find as slot 0 if needed, or rely on the fact that 
    // we only draw textured tiles or colored tiles.
    // Actually, Renderer2D binds a white texture to slot 0. We should try to do the same.
    // But we don't have access to Renderer2D's private data.
    // We can create a 1x1 white texture here or just ignore it if we always have textures.
    // The factory game uses textures for everything (grass, ore, etc).

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
                
                float xPos = (float)x * m_TileSize;
                float yPos = (float)y * m_TileSize;
                
                // Quad Vertices
                TileVertex v[4];
                v[0].Position = { xPos, yPos, 0.0f }; // BL
                v[0].TexCoord = { 0.0f, 0.0f };
                
                v[1].Position = { xPos + m_TileSize, yPos, 0.0f }; // BR
                v[1].TexCoord = { 1.0f, 0.0f };
                
                v[2].Position = { xPos + m_TileSize, yPos + m_TileSize, 0.0f }; // TR
                v[2].TexCoord = { 1.0f, 1.0f };
                
                v[3].Position = { xPos, yPos + m_TileSize, 0.0f }; // TL
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

    // Upload to GPU
    auto device = Window::GetDevice();

    // Vertex Buffer
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

    // Index Buffer
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

void TileMap::Render(const glm::mat4& viewProj)
{
    if (m_Dirty) UpdateMesh();
    if (m_IndexCount == 0) return;

    auto context = Window::GetContext();
    Shader* shader = Renderer2D::GetShader();

    if (shader)
    {
        shader->Bind();
        shader->SetMat4("u_ViewProjection", viewProj);
    }

    // Bind Buffers
    UINT stride = sizeof(TileVertex);
    UINT offset = 0;
    context->IASetVertexBuffers(0, 1, m_VertexBuffer.GetAddressOf(), &stride, &offset);
    context->IASetIndexBuffer(m_IndexBuffer.Get(), DXGI_FORMAT_R32_UINT, 0);
    context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

    // Bind Textures
    // We need to fill the slots up to 32 to avoid warnings or issues, or just bind what we have
    // Renderer2D binds an array of 32.
    std::vector<ID3D11ShaderResourceView*> slots = m_TextureSlots;
    while (slots.size() < 32) slots.push_back(nullptr);
    
    context->PSSetShaderResources(0, (UINT)slots.size(), slots.data());

    // Draw
    context->DrawIndexed(m_IndexCount, 0, 0);
}
