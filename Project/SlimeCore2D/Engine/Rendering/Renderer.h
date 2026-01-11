#pragma once

#include <glm.hpp>
#include <vector>
#include <string>
#include <memory>

#include "RefCntAutoPtr.hpp"
#include "RenderDevice.h"
#include "DeviceContext.h"
#include "Buffer.h"
#include "Texture.h"
#include "PipelineState.h"
#include "ShaderResourceBinding.h"

#include "Core/Camera.h"

using namespace Diligent;

// Forward declarations
class Texture;
class Font;
struct Mesh; // We'll define a simple Mesh struct for 3D

class Renderer
{
public:
    struct Statistics
    {
        uint32_t DrawCalls = 0;
        uint32_t QuadCount = 0;
        uint32_t VertexCount = 0;
        uint32_t IndexCount = 0;
    };

    static void Init();
    static void Shutdown();

    static void BeginScene(Camera& camera);
    static void BeginScene(const glm::mat4& viewProj);
    static void EndScene();

    // ==============================================================================================
    // 2D Rendering (Batched)
    // ==============================================================================================
    
    static void DrawQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color);
    static void DrawQuad(const glm::vec3& position, const glm::vec2& size, const glm::vec4& color);
    
    static void DrawQuad(const glm::vec2& position, const glm::vec2& size, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));
    static void DrawQuad(const glm::vec3& position, const glm::vec2& size, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));

    static void DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, const glm::vec4& color);
    static void DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, const glm::vec4& color);
    static void DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));
    static void DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));

    static void DrawQuad(const glm::mat4& transform, const glm::vec4& color);
    static void DrawQuad(const glm::mat4& transform, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));
    static void DrawQuadUV(const glm::mat4& transform, Texture* texture, const glm::vec2 uvs[4], const glm::vec4& tintColor = glm::vec4(1.0f));

    static void DrawString(const std::string& text, Font* font, const glm::vec3& position, float scale, const glm::vec4& color, float wrapWidth = 0.0f);

    // ==============================================================================================
    // Scissor / Clipping
    // ==============================================================================================
    
    static void EnableScissor(float x, float y, float w, float h);
    static void DisableScissor();

    // ==============================================================================================
    // 3D Rendering (Forward)
    // ==============================================================================================

    // Simple Mesh struct for 3D
    struct MeshData
    {
        RefCntAutoPtr<IBuffer> VertexBuffer;
        RefCntAutoPtr<IBuffer> IndexBuffer;
        uint32_t IndexCount = 0;
    };

    static void DrawMesh(const MeshData& mesh, const glm::mat4& transform, const glm::vec4& color = glm::vec4(1.0f));
    static void DrawMesh(const MeshData& mesh, const glm::mat4& transform, Texture* texture, const glm::vec4& color = glm::vec4(1.0f));

    // ==============================================================================================
    // Stats & Utils
    // ==============================================================================================

    static Statistics GetStats();
    static void ResetStats();

    static ITextureView* GetWhiteTexture();

private:
    static void Flush();
    static void StartBatch();
    static void NextBatch();
};
