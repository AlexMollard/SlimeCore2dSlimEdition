#include "ConveyorMap.h"
#include "Core/Window.h"
#include "Resources/ResourceManager.h"
#include <gtc/matrix_transform.hpp>

ConveyorMap::ConveyorMap(int width, int height, float tileSize)
    : m_Width(width), m_Height(height), m_TileSize(tileSize)
{
    m_Map.resize(width * height);
    
    // Try to load a custom shader, otherwise fallback (though fallback won't have the moving effect)
    // We assume "conveyor" shader exists or we use "basic" and accept no movement
    m_Shader = ResourceManager::GetInstance().GetShader("conveyor");
    if (!m_Shader) m_Shader = ResourceManager::GetInstance().GetShader("conveyor_"); // Try with underscore from file naming

    if (!m_Shader)
    {
        // Try to load it explicitly if auto-scan missed it or naming mismatch
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
        // Create it if not found? For now, fallback to basic
        m_Shader = ResourceManager::GetInstance().GetShader("basic");
    }
}

ConveyorMap::~ConveyorMap()
{
}

void ConveyorMap::SetConveyor(int x, int y, int tier, int direction)
{
    if (x < 0 || x >= m_Width || y < 0 || y >= m_Height) return;
    
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
    if (x < 0 || x >= m_Width || y < 0 || y >= m_Height) return;
    
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
    if (x < 0 || x >= m_Width || y < 0 || y >= m_Height) return false;
    return m_Map[y * m_Width + x].Active;
}

int ConveyorMap::GetDirection(int x, int y)
{
    if (x < 0 || x >= m_Width || y < 0 || y >= m_Height) return -1;
    return m_Map[y * m_Width + x].Direction;
}

glm::vec4 ConveyorMap::GetTierColor(int tier)
{
    switch (tier)
    {
        case 1: return glm::vec4(1.0f, 0.8f, 0.2f, 1.0f); // Yellow
        case 2: return glm::vec4(1.0f, 0.2f, 0.2f, 1.0f); // Red
        case 3: return glm::vec4(0.2f, 0.2f, 1.0f, 1.0f); // Blue
        default: return glm::vec4(0.5f, 0.5f, 0.5f, 1.0f); // Grey
    }
}

void ConveyorMap::UpdateMesh()
{
    if (!m_Dirty) return;
    
    m_Vertices.clear();
    m_Indices.clear();
    
    glm::vec4 beltColor = glm::vec4(0.2f, 0.2f, 0.2f, 1.0f); // Dark Grey
    
    for (int y = 0; y < m_Height; y++)
    {
        for (int x = 0; x < m_Width; x++)
        {
            ConveyorTile& tile = m_Map[y * m_Width + x];
            if (!tile.Active) continue;
            
            // Offset by half tile size to center in grid cell
            glm::vec3 pos(x * m_TileSize + m_TileSize * 0.5f, y * m_TileSize + m_TileSize * 0.5f, 0.0f);
            glm::vec4 railColor = GetTierColor(tile.Tier);
            
            float speed = 1.0f;
            if (tile.Tier == 2) speed = 2.0f;
            else if (tile.Tier == 3) speed = 4.0f;
            
            // Determine rotation based on direction
            // 0=N, 1=E, 2=S, 3=W
            float rotation = 0.0f;
            if (tile.Direction == 1) rotation = -90.0f; // East
            else if (tile.Direction == 2) rotation = 180.0f; // South
            else if (tile.Direction == 3) rotation = 90.0f; // West
            
            // Check neighbors for connections (Marching Squares / Auto-tiling logic)
            // We need to know if neighbors are pointing INTO this tile.
            
            bool inN = false, inE = false, inS = false, inW = false;
            
            // North Neighbor (y+1)
            if (HasConveyor(x, y+1) && GetDirection(x, y+1) == 2) inN = true; // North neighbor pointing South
            // East Neighbor (x+1)
            if (HasConveyor(x+1, y) && GetDirection(x+1, y) == 3) inE = true; // East neighbor pointing West
            // South Neighbor (y-1)
            if (HasConveyor(x, y-1) && GetDirection(x, y-1) == 0) inS = true; // South neighbor pointing North
            // West Neighbor (x-1)
            if (HasConveyor(x-1, y) && GetDirection(x-1, y) == 1) inW = true; // West neighbor pointing East
            
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
                if (inW) inputLeft = true;
                if (inE) inputRight = true;
                if (inS) inputBack = true;
            }
            else if (tile.Direction == 1) // East
            {
                if (inN) inputLeft = true;
                if (inS) inputRight = true;
                if (inW) inputBack = true;
            }
            else if (tile.Direction == 2) // South
            {
                if (inE) inputLeft = true;
                if (inW) inputRight = true;
                if (inN) inputBack = true;
            }
            else if (tile.Direction == 3) // West
            {
                if (inS) inputLeft = true;
                if (inN) inputRight = true;
                if (inE) inputBack = true;
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
            
            glm::mat4 transform = glm::translate(glm::mat4(1.0f), pos) * 
                                  glm::rotate(glm::mat4(1.0f), rotation, glm::vec3(0,0,1));
            
            auto AddCornerMesh = [&](bool isLeft, bool withOuterRail) {
                glm::vec3 center;
                float startAngle, endAngle;
                
                if (isLeft) {
                    center = glm::vec3(-0.5f * m_TileSize, 0.5f * m_TileSize, 0.0f);
                    startAngle = -glm::half_pi<float>();
                    endAngle = 0.0f;
                } else {
                    center = glm::vec3(0.5f * m_TileSize, 0.5f * m_TileSize, 0.0f);
                    startAngle = -glm::half_pi<float>();
                    endAngle = -glm::pi<float>();
                }

                float innerR = 0.2f * m_TileSize;
                float outerR = 0.8f * m_TileSize;
                float railW = 0.1f * m_TileSize;
                int segments = 8;

                // Belt
                for (int i = 0; i < segments; i++) {
                    float t0 = (float)i / segments;
                    float t1 = (float)(i + 1) / segments;
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

                    uint32_t baseIndex = (uint32_t)m_Vertices.size();
                    Vertex v[4];
                    
                    if (isLeft) {
                        v[0].Position = p0_out; v[0].TexCoord = glm::vec2(0.0f, 1.0f - t0);
                        v[1].Position = p0_in;  v[1].TexCoord = glm::vec2(1.0f, 1.0f - t0);
                        v[2].Position = p1_in;  v[2].TexCoord = glm::vec2(1.0f, 1.0f - t1);
                        v[3].Position = p1_out; v[3].TexCoord = glm::vec2(0.0f, 1.0f - t1);
                    } else {
                        v[0].Position = p0_in;  v[0].TexCoord = glm::vec2(0.0f, 1.0f - t0);
                        v[1].Position = p0_out; v[1].TexCoord = glm::vec2(1.0f, 1.0f - t0);
                        v[2].Position = p1_out; v[2].TexCoord = glm::vec2(1.0f, 1.0f - t1);
                        v[3].Position = p1_in;  v[3].TexCoord = glm::vec2(0.0f, 1.0f - t1);
                    }
                    
                    for(int k=0; k<4; k++) { v[k].Color = beltColor; v[k].IsBelt = 1.0f; v[k].TilingFactor = 1.0f; v[k].Padding = speed; m_Vertices.push_back(v[k]); }
                    m_Indices.push_back(baseIndex + 0); m_Indices.push_back(baseIndex + 1); m_Indices.push_back(baseIndex + 2);
                    m_Indices.push_back(baseIndex + 2); m_Indices.push_back(baseIndex + 3); m_Indices.push_back(baseIndex + 0);
                }

                // Inner Rail
                for (int i = 0; i < segments; i++) {
                    float t0 = (float)i / segments;
                    float t1 = (float)(i + 1) / segments;
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
                    
                    uint32_t baseIndex = (uint32_t)m_Vertices.size();
                    Vertex v[4];
                    v[0].Position = p0_out; v[0].TexCoord = glm::vec2(0,0);
                    v[1].Position = p0_in;  v[1].TexCoord = glm::vec2(1,0);
                    v[2].Position = p1_in;  v[2].TexCoord = glm::vec2(1,1);
                    v[3].Position = p1_out; v[3].TexCoord = glm::vec2(0,1);
                    
                    for(int k=0; k<4; k++) { v[k].Color = railColor; v[k].IsBelt = 0.0f; v[k].TilingFactor = 1.0f; v[k].Padding = 0.0f; m_Vertices.push_back(v[k]); }
                    m_Indices.push_back(baseIndex + 0); m_Indices.push_back(baseIndex + 1); m_Indices.push_back(baseIndex + 2);
                    m_Indices.push_back(baseIndex + 2); m_Indices.push_back(baseIndex + 3); m_Indices.push_back(baseIndex + 0);
                }

                // Outer Rail
                if (withOuterRail) {
                    for (int i = 0; i < segments; i++) {
                        float t0 = (float)i / segments;
                        float t1 = (float)(i + 1) / segments;
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
                        
                        uint32_t baseIndex = (uint32_t)m_Vertices.size();
                        Vertex v[4];
                        v[0].Position = p0_out; v[0].TexCoord = glm::vec2(0,0);
                        v[1].Position = p0_in;  v[1].TexCoord = glm::vec2(1,0);
                        v[2].Position = p1_in;  v[2].TexCoord = glm::vec2(1,1);
                        v[3].Position = p1_out; v[3].TexCoord = glm::vec2(0,1);
                        
                        for(int k=0; k<4; k++) { v[k].Color = railColor; v[k].IsBelt = 0.0f; v[k].TilingFactor = 1.0f; v[k].Padding = 0.0f; m_Vertices.push_back(v[k]); }
                        m_Indices.push_back(baseIndex + 0); m_Indices.push_back(baseIndex + 1); m_Indices.push_back(baseIndex + 2);
                        m_Indices.push_back(baseIndex + 2); m_Indices.push_back(baseIndex + 3); m_Indices.push_back(baseIndex + 0);
                    }
                }
            };

            auto AddMergeMesh = [&](bool isLeft) {
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
                    
                    uint32_t baseIndex = (uint32_t)m_Vertices.size();
                    Vertex v[4];
                    
                    if (isLeft) {
                        // Flow East. U=0 at Top (Left side of flow). V=1 at Start.
                        v[0].Position = p0; v[0].TexCoord = glm::vec2(0.0f, 1.0f);
                        v[1].Position = p1; v[1].TexCoord = glm::vec2(0.0f, 0.8f);
                        v[2].Position = p2; v[2].TexCoord = glm::vec2(1.0f, 0.8f);
                        v[3].Position = p3; v[3].TexCoord = glm::vec2(1.0f, 1.0f);
                    } else {
                        // Flow West. U=0 at Bottom (Left side of flow). V=1 at Start.
                        v[0].Position = p3; v[0].TexCoord = glm::vec2(0.0f, 1.0f);
                        v[1].Position = p2; v[1].TexCoord = glm::vec2(0.0f, 0.8f);
                        v[2].Position = p1; v[2].TexCoord = glm::vec2(1.0f, 0.8f);
                        v[3].Position = p0; v[3].TexCoord = glm::vec2(1.0f, 1.0f);
                    }
                    
                    for(int k=0; k<4; k++) { v[k].Color = beltColor; v[k].IsBelt = 1.0f; v[k].TilingFactor = 1.0f; v[k].Padding = speed; m_Vertices.push_back(v[k]); }
                    m_Indices.push_back(baseIndex + 0); m_Indices.push_back(baseIndex + 1); m_Indices.push_back(baseIndex + 2);
                    m_Indices.push_back(baseIndex + 2); m_Indices.push_back(baseIndex + 3); m_Indices.push_back(baseIndex + 0);
                }
                
                // Rails
                auto AddRailRect = [&](float yMin, float yMax) {
                    glm::vec3 p0(xStart, yMax, 0);
                    glm::vec3 p1(xEnd, yMax, 0);
                    glm::vec3 p2(xEnd, yMin, 0);
                    glm::vec3 p3(xStart, yMin, 0);
                    
                    p0 = glm::vec3(transform * glm::vec4(p0, 1.0f));
                    p1 = glm::vec3(transform * glm::vec4(p1, 1.0f));
                    p2 = glm::vec3(transform * glm::vec4(p2, 1.0f));
                    p3 = glm::vec3(transform * glm::vec4(p3, 1.0f));
                    
                    uint32_t baseIndex = (uint32_t)m_Vertices.size();
                    Vertex v[4];
                    v[0].Position = p0; v[0].TexCoord = glm::vec2(0,0);
                    v[1].Position = p1; v[1].TexCoord = glm::vec2(1,0);
                    v[2].Position = p2; v[2].TexCoord = glm::vec2(1,1);
                    v[3].Position = p3; v[3].TexCoord = glm::vec2(0,1);
                    
                    for(int k=0; k<4; k++) { v[k].Color = railColor; v[k].IsBelt = 0.0f; v[k].TilingFactor = 1.0f; v[k].Padding = 0.0f; m_Vertices.push_back(v[k]); }
                    m_Indices.push_back(baseIndex + 0); m_Indices.push_back(baseIndex + 1); m_Indices.push_back(baseIndex + 2);
                    m_Indices.push_back(baseIndex + 2); m_Indices.push_back(baseIndex + 3); m_Indices.push_back(baseIndex + 0);
                };
                
                AddRailRect(0.3f * m_TileSize, 0.4f * m_TileSize);
                AddRailRect(-0.4f * m_TileSize, -0.3f * m_TileSize);
            };

            auto AddStraightRail = [&](bool isLeft, float yMin, float yMax) {
                float w = m_TileSize * 0.1f;
                float xOffset = isLeft ? -m_TileSize * 0.35f : m_TileSize * 0.35f;
                
                glm::vec3 offsets[4] = {
                    { xOffset - 0.5f * w, yMin, 0.0f },
                    { xOffset + 0.5f * w, yMin, 0.0f },
                    { xOffset + 0.5f * w, yMax, 0.0f },
                    { xOffset - 0.5f * w, yMax, 0.0f }
                };
                
                glm::vec2 uvs[4] = { {0, 0}, {1, 0}, {1, 1}, {0, 1} };
                
                uint32_t baseIndex = (uint32_t)m_Vertices.size();
                for(int i=0; i<4; i++) {
                    Vertex vert;
                    vert.Position = glm::vec3(transform * glm::vec4(offsets[i], 1.0f));
                    vert.Color = railColor;
                    vert.TexCoord = uvs[i];
                    vert.IsBelt = 0.0f;
                    vert.TilingFactor = 1.0f;
                    vert.Padding = 0.0f;
                    m_Vertices.push_back(vert);
                }
                m_Indices.push_back(baseIndex + 0); m_Indices.push_back(baseIndex + 3); m_Indices.push_back(baseIndex + 2);
                m_Indices.push_back(baseIndex + 0); m_Indices.push_back(baseIndex + 2); m_Indices.push_back(baseIndex + 1);
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
                    
                    glm::vec2 uvs[4] = { {0, 1}, {1, 1}, {1, 0}, {0, 0} };
                    
                    uint32_t baseIndex = (uint32_t)m_Vertices.size();
                    
                    for(int i=0; i<4; i++)
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
                if (isCornerLeft) {
                    AddCornerMesh(true, true);
                }
                
                if (isCornerRight) {
                    AddCornerMesh(false, true);
                }
            }
        }
    }
    
    m_IndexCount = (uint32_t)m_Indices.size();
    m_Dirty = false;

    if (m_Vertices.empty()) return;

    // Upload to GPU
    auto device = Window::GetDevice();
    auto context = Window::GetContext();
    
    // Vertex Buffer
    if (!m_VertexBuffer || m_Vertices.size() > m_VertexCapacity)
    {
        m_VertexCapacity = (uint32_t)m_Vertices.size() + 500;
        
        D3D11_BUFFER_DESC vbDesc = {};
        vbDesc.ByteWidth = (UINT)(m_VertexCapacity * sizeof(Vertex));
        vbDesc.Usage = D3D11_USAGE_DYNAMIC;
        vbDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
        vbDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        
        device->CreateBuffer(&vbDesc, nullptr, m_VertexBuffer.ReleaseAndGetAddressOf());
    }
    
    D3D11_MAPPED_SUBRESOURCE ms;
    if (SUCCEEDED(context->Map(m_VertexBuffer.Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &ms)))
    {
        memcpy(ms.pData, m_Vertices.data(), m_Vertices.size() * sizeof(Vertex));
        context->Unmap(m_VertexBuffer.Get(), 0);
    }
    
    // Index Buffer
    if (!m_IndexBuffer || m_Indices.size() > m_IndexCapacity)
    {
        m_IndexCapacity = (uint32_t)m_Indices.size() + 500;
        
        D3D11_BUFFER_DESC ibDesc = {};
        ibDesc.ByteWidth = (UINT)(m_IndexCapacity * sizeof(uint32_t));
        ibDesc.Usage = D3D11_USAGE_DYNAMIC;
        ibDesc.BindFlags = D3D11_BIND_INDEX_BUFFER;
        ibDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        
        device->CreateBuffer(&ibDesc, nullptr, m_IndexBuffer.ReleaseAndGetAddressOf());
    }
    
    if (SUCCEEDED(context->Map(m_IndexBuffer.Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &ms)))
    {
        memcpy(ms.pData, m_Indices.data(), m_Indices.size() * sizeof(uint32_t));
        context->Unmap(m_IndexBuffer.Get(), 0);
    }
}

void ConveyorMap::Render(const glm::mat4& viewProj, float time)
{
    if (m_Dirty) UpdateMesh();
    if (m_IndexCount == 0 || !m_VertexBuffer) return;
    
    auto context = Window::GetContext();
    
    if (m_Shader)
    {
        m_Shader->Bind();
        m_Shader->SetMat4("u_ViewProjection", viewProj);
        m_Shader->setFloat("u_Time", time);
    }
    
    UINT stride = sizeof(Vertex);
    UINT offset = 0;
    context->IASetVertexBuffers(0, 1, m_VertexBuffer.GetAddressOf(), &stride, &offset);
    context->IASetIndexBuffer(m_IndexBuffer.Get(), DXGI_FORMAT_R32_UINT, 0);
    context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    
    context->DrawIndexed(m_IndexCount, 0, 0);
}
