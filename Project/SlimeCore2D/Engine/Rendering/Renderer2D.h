#pragma once

#define NOMINMAX
#include <array>
#include <glm.hpp>
#include <string>
#include <vector>

#include "Buffer.h"
#include "Core/Camera.h"
#include "DeviceContext.h"
#include "PipelineState.h"
#include "RefCntAutoPtr.hpp"
#include "RenderDevice.h"
#include "Sampler.h"
#include "Shader.h"
#include "ShaderResourceBinding.h"
#include "Texture.h"

using namespace Diligent;

// Forward Declaration for your Text/Font class
class Text;

class Renderer2D
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

	// Begin a new frame/batch. Call this before drawing.
	static void BeginScene(Camera& camera);
	// For custom orthographic projections (e.g. UI)
	static void BeginScene(const glm::mat4& viewProj);
	static void EndScene();
	static void Flush();

	// --- Primitives ---

	// Flat Color
	static void DrawQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color);
	static void DrawQuad(const glm::vec3& position, const glm::vec2& size, const glm::vec4& color);

	// Texture
	static void DrawQuad(const glm::vec2& position, const glm::vec2& size, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));
	static void DrawQuad(const glm::vec3& position, const glm::vec2& size, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));

	// Rotated (Rotation in radians)
	static void DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, const glm::vec4& color);
	static void DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, const glm::vec4& color);
	static void DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));

	// Matrix Transform (For Scene Graph)
	static void DrawQuad(const glm::mat4& transform, const glm::vec4& color);
	static void DrawQuad(const glm::mat4& transform, Texture* texture, float tiling = 1.0f, const glm::vec4& tintColor = glm::vec4(1.0f));
	static void DrawQuadUV(const glm::mat4& transform, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor = glm::vec4(1.0f));

	// SubTexture / Explicit UVs (Useful for Sprite Sheets and Font Atlases)
	// uv[] must be an array of 4 vec2s: { BL, BR, TR, TL }
	static void DrawQuadUV(const glm::vec3& position, const glm::vec2& size, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor = glm::vec4(1.0f));

	// --- Text Rendering (SDF) ---
	// Draws a string using the provided Text object (which contains the font atlas)
	static void DrawString(const std::string& text, Text* font, const glm::vec2& position, float scale, const glm::vec4& color);
	static void DrawString(const std::string& text, Text* font, const glm::vec3& position, float scale, const glm::vec4& color);

	// Stats
	static Statistics GetStats();
	static void ResetStats();

	// Global default shader access
	static Shader* GetShader()
	{
		return s_Data.TextureShader;
	}

	static IPipelineState* GetPSO()
	{
		return s_Data.PSO;
	}

	static ITextureView* GetWhiteTextureSRV()
	{
		return s_Data.WhiteTextureSRV;
	}

private:
	static void StartBatch();
	static void NextBatch();

	// Internal storage structure (PIMPL-style static)
	struct Renderer2DData
	{
		const uint32_t MaxQuads = 10000;
		// Instancing: 1 instance per quad
		const uint32_t MaxInstances = MaxQuads;
		const uint32_t MaxIndices = MaxQuads * 6;
		static const uint32_t MaxTextureSlots = 32;

		RefCntAutoPtr<IBuffer> QuadVB;     // Static Geometry (0..1)
		RefCntAutoPtr<IBuffer> InstanceVB; // Dynamic Instance Data
		RefCntAutoPtr<IBuffer> QuadIB;     // Static Indices

		RefCntAutoPtr<IPipelineState> PSO;
		RefCntAutoPtr<IShaderResourceBinding> SRB;
		RefCntAutoPtr<ITextureView> WhiteTextureSRV;

		uint32_t QuadCount = 0;

		// Instance Data Definition
		struct InstanceData
		{
			glm::mat4 Transform;
			glm::vec4 Color;
			glm::vec4 UVRect; // x=minU, y=minV, z=maxU, w=maxV
			float TexIndex;
			float Tiling;
			float IsText;
			float Padding; // Align to 16 bytes if needed, though 4 floats is 16 bytes.
		};

		InstanceData* InstanceBuffer = nullptr;
		InstanceData* InstanceBufferPtr = nullptr;

		std::array<ITextureView*, MaxTextureSlots> TextureSlots;
		uint32_t TextureSlotIndex = 1; // 0 is white texture

		Shader* TextureShader = nullptr;

		Statistics Stats;
	};

	static Renderer2DData s_Data;
};
