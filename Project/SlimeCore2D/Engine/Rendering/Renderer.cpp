#include "Renderer.h"

#include <array>
#include <gtc/matrix_transform.hpp>
#include <iostream>

#include "Core/Window.h"
#include "Core/Logger.h"
#include "Resources/ResourceManager.h"
#include "Font.h"
#include "DiligentCore/Graphics/GraphicsTools/interface/MapHelper.hpp"
#include "Shader.h"

using namespace Diligent;

struct RendererData
{
    // ==============================================================================================
    // 2D Batching Data
    // ==============================================================================================
    static const uint32_t MaxQuads = 1000; // Reduced batch size to prevent dynamic heap exhaustion
    static const uint32_t MaxVertices = MaxQuads * 4;
    static const uint32_t MaxIndices = MaxQuads * 6;
    static const uint32_t MaxTextureSlots = 32; // REDUCED from 1024 to 32 to fix descriptor heap issues

    RefCntAutoPtr<IPipelineState> QuadPSO;
    RefCntAutoPtr<IShaderResourceBinding> QuadSRB;

    RefCntAutoPtr<IPipelineState> TextPSO;
    RefCntAutoPtr<IShaderResourceBinding> TextSRB;

    RefCntAutoPtr<IBuffer> QuadVB; // Dynamic Vertex Buffer (Interleaved)
    RefCntAutoPtr<IBuffer> QuadIB; // Static Index Buffer

    // Vertex Structure for 2D
    struct QuadVertex
    {
        glm::vec3 Position;
        glm::vec4 Color;
        glm::vec2 TexCoord;
        float TexIndex;
        float Tiling;
        float IsText; // 0.0 = Sprite, 1.0 = Text
    };

    QuadVertex* QuadBufferBase = nullptr;
    QuadVertex* QuadBufferPtr = nullptr;

    uint32_t QuadIndexCount = 0;
    
    std::array<ITextureView*, MaxTextureSlots> TextureSlots;
    uint32_t TextureSlotIndex = 1; // 0 = White Texture

    RefCntAutoPtr<ITextureView> WhiteTexture;

    // ==============================================================================================
    // 3D Rendering Data
    // ==============================================================================================
    RefCntAutoPtr<IPipelineState> MeshPSO;
    RefCntAutoPtr<IShaderResourceBinding> MeshSRB;
    
    struct MeshConstants
    {
        glm::mat4 World;
        glm::vec4 Color;
        float UseTexture; // 0.0 = No, 1.0 = Yes
        float Padding[3];
    };
    RefCntAutoPtr<IBuffer> MeshConstantBuffer;

    // ==============================================================================================
    // Global Data
    // ==============================================================================================
    struct GlobalConstants
    {
        glm::mat4 ViewProjection;
    };
    RefCntAutoPtr<IBuffer> GlobalConstantBuffer;

    Renderer::Statistics Stats;

    enum class PipelineType { None, Quad, Text, Mesh };
    PipelineType CurrentPipeline = PipelineType::None;
};

static RendererData s_Data;

void Renderer::Init()
{
    auto device = Window::GetDevice();
    auto& ResMgr = ResourceManager::GetInstance();

    // Ensure shaders are loaded
    ResMgr.LoadShadersFromDir();

    // 1. Initialize White Texture
    {
        TextureDesc TexDesc;
        TexDesc.Name = "Renderer White Texture";
        TexDesc.Type = RESOURCE_DIM_TEX_2D;
        TexDesc.Width = 1;
        TexDesc.Height = 1;
        TexDesc.Format = TEX_FORMAT_RGBA8_UNORM;
        TexDesc.Usage = USAGE_IMMUTABLE;
        TexDesc.BindFlags = BIND_SHADER_RESOURCE;
        TexDesc.MipLevels = 1;

        uint32_t whiteColor = 0xffffffff;
        TextureSubResData Level0Data;
        Level0Data.pData = &whiteColor;
        Level0Data.Stride = 4;
        TextureData InitData;
        InitData.pSubResources = &Level0Data;
        InitData.NumSubresources = 1;

        RefCntAutoPtr<ITexture> texture;
        device->CreateTexture(TexDesc, &InitData, &texture);
        s_Data.WhiteTexture = texture->GetDefaultView(TEXTURE_VIEW_SHADER_RESOURCE);
        
        s_Data.TextureSlots[0] = s_Data.WhiteTexture;
    }

    // 2. Initialize Global Constant Buffer
    {
        BufferDesc CBDesc;
        CBDesc.Name = "Renderer Global CB";
        CBDesc.Size = sizeof(RendererData::GlobalConstants);
        CBDesc.Usage = USAGE_DYNAMIC;
        CBDesc.BindFlags = BIND_UNIFORM_BUFFER;
        CBDesc.CPUAccessFlags = CPU_ACCESS_WRITE;
        device->CreateBuffer(CBDesc, nullptr, &s_Data.GlobalConstantBuffer);
    }

    // 3. Initialize 2D Pipeline (Quad Batcher)
    {
        // Create Dynamic Vertex Buffer
        BufferDesc VBDesc;
        VBDesc.Name = "Renderer2D Dynamic VB";
        VBDesc.Size = s_Data.MaxVertices * sizeof(RendererData::QuadVertex);
        VBDesc.Usage = USAGE_DYNAMIC;
        VBDesc.BindFlags = BIND_VERTEX_BUFFER;
        VBDesc.CPUAccessFlags = CPU_ACCESS_WRITE;
        device->CreateBuffer(VBDesc, nullptr, &s_Data.QuadVB);

        s_Data.QuadBufferBase = new RendererData::QuadVertex[s_Data.MaxVertices];

        // Create Static Index Buffer
        uint32_t* indices = new uint32_t[s_Data.MaxIndices];
        uint32_t offset = 0;
        for (uint32_t i = 0; i < s_Data.MaxIndices; i += 6)
        {
            indices[i + 0] = offset + 0;
            indices[i + 1] = offset + 1;
            indices[i + 2] = offset + 2;

            indices[i + 3] = offset + 2;
            indices[i + 4] = offset + 3;
            indices[i + 5] = offset + 0;

            offset += 4;
        }

        BufferDesc IBDesc;
        IBDesc.Name = "Renderer2D Static IB";
        IBDesc.Size = s_Data.MaxIndices * sizeof(uint32_t);
        IBDesc.Usage = USAGE_IMMUTABLE;
        IBDesc.BindFlags = BIND_INDEX_BUFFER;
        
        BufferData IBData;
        IBData.pData = indices;
        IBData.DataSize = s_Data.MaxIndices * sizeof(uint32_t);
        device->CreateBuffer(IBDesc, &IBData, &s_Data.QuadIB);
        delete[] indices;

        // Create PSO
        GraphicsPipelineStateCreateInfo PSOCreateInfo;
        PSOCreateInfo.PSODesc.Name = "Renderer2D PSO";
        PSOCreateInfo.PSODesc.PipelineType = PIPELINE_TYPE_GRAPHICS;
        PSOCreateInfo.GraphicsPipeline.NumRenderTargets = 1;
        PSOCreateInfo.GraphicsPipeline.RTVFormats[0] = TEX_FORMAT_RGBA8_UNORM;
        PSOCreateInfo.GraphicsPipeline.DSVFormat = TEX_FORMAT_D24_UNORM_S8_UINT;
        PSOCreateInfo.GraphicsPipeline.PrimitiveTopology = PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
        
        // Blending
        PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].BlendEnable = true;
        PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].SrcBlend = BLEND_FACTOR_SRC_ALPHA;
        PSOCreateInfo.GraphicsPipeline.BlendDesc.RenderTargets[0].DestBlend = BLEND_FACTOR_INV_SRC_ALPHA;

        // Rasterizer
        PSOCreateInfo.GraphicsPipeline.RasterizerDesc.CullMode = CULL_MODE_NONE;
        PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthEnable = false;

        // Input Layout
        LayoutElement LayoutElems[] = {
            LayoutElement{ 0, 0, 3, VT_FLOAT32, False }, // Position
            LayoutElement{ 1, 0, 4, VT_FLOAT32, False }, // Color
            LayoutElement{ 2, 0, 2, VT_FLOAT32, False }, // TexCoord
            LayoutElement{ 3, 0, 1, VT_FLOAT32, False }, // TexIndex
            LayoutElement{ 4, 0, 1, VT_FLOAT32, False }, // Tiling
            LayoutElement{ 5, 0, 1, VT_FLOAT32, False }  // IsText
        };
        PSOCreateInfo.GraphicsPipeline.InputLayout.LayoutElements = LayoutElems;
        PSOCreateInfo.GraphicsPipeline.InputLayout.NumElements = _countof(LayoutElems);

        // Shaders (We need a new shader that accepts these attributes)
        // For now, we assume "renderer2d" shader exists or we use "basic" and modify it.
        // Let's assume we have a shader that matches this layout.
        auto& ResMgr = ResourceManager::GetInstance();
        
        Shader* basicShader = ResMgr.GetShader("basic");
        if (!basicShader)
        {
            Logger::Error("CRITICAL: 'basic' shader not found! Renderer cannot initialize.");
            return;
        }

        // NOTE: You will need to ensure 'renderer2d_vertex.hlsl' and 'renderer2d_pixel.hlsl' exist
        // and match the layout above.
        IShader* pVS = basicShader->GetVertexShader(); // Fallback
        IShader* pPS = basicShader->GetPixelShader(); // Fallback

        if (ResMgr.GetShader("renderer2d"))
        {
            pVS = ResMgr.GetShader("renderer2d")->GetVertexShader();
            pPS = ResMgr.GetShader("renderer2d")->GetPixelShader();
        }

        PSOCreateInfo.pVS = pVS;
        PSOCreateInfo.pPS = pPS;

        // Variables
        ShaderResourceVariableDesc Vars[] = {
            { SHADER_TYPE_PIXEL, "u_Textures", SHADER_RESOURCE_VARIABLE_TYPE_DYNAMIC },
            { SHADER_TYPE_VERTEX, "GlobalConstants", SHADER_RESOURCE_VARIABLE_TYPE_STATIC }
        };
        PSOCreateInfo.PSODesc.ResourceLayout.Variables = Vars;
        PSOCreateInfo.PSODesc.ResourceLayout.NumVariables = _countof(Vars);

        // Samplers
        SamplerDesc SamLinear;
        SamLinear.MinFilter = FILTER_TYPE_LINEAR;
        SamLinear.MagFilter = FILTER_TYPE_LINEAR;
        SamLinear.MipFilter = FILTER_TYPE_LINEAR;
        SamLinear.AddressU = TEXTURE_ADDRESS_WRAP;
        SamLinear.AddressV = TEXTURE_ADDRESS_WRAP;

        ImmutableSamplerDesc ImtblSamplers[] = {
            { SHADER_TYPE_PIXEL, "u_Sampler", SamLinear },
            { SHADER_TYPE_PIXEL, "u_SamplerLinear", SamLinear }
        };
        PSOCreateInfo.PSODesc.ResourceLayout.ImmutableSamplers = ImtblSamplers;
        PSOCreateInfo.PSODesc.ResourceLayout.NumImmutableSamplers = _countof(ImtblSamplers);

        // --- Quad PSO ---
        {
            IShader* pVS = ResMgr.GetShader("basic")->GetVertexShader(); // Fallback
            IShader* pPS = ResMgr.GetShader("basic")->GetPixelShader(); // Fallback

            if (ResMgr.GetShader("renderer2d"))
            {
                pVS = ResMgr.GetShader("renderer2d")->GetVertexShader();
                pPS = ResMgr.GetShader("renderer2d")->GetPixelShader();
            }

            PSOCreateInfo.pVS = pVS;
            PSOCreateInfo.pPS = pPS;
            PSOCreateInfo.PSODesc.Name = "Renderer2D Quad PSO";

            device->CreateGraphicsPipelineState(PSOCreateInfo, &s_Data.QuadPSO);
            
            if (auto* pVar = s_Data.QuadPSO->GetStaticVariableByName(SHADER_TYPE_VERTEX, "GlobalConstants"))
                pVar->Set(s_Data.GlobalConstantBuffer);

            s_Data.QuadPSO->CreateShaderResourceBinding(&s_Data.QuadSRB, true);
        }

        // --- Text PSO ---
        {
            IShader* pVS = ResMgr.GetShader("basic")->GetVertexShader(); // Fallback
            IShader* pPS = ResMgr.GetShader("basic")->GetPixelShader(); // Fallback

            if (ResMgr.GetShader("text"))
            {
                pVS = ResMgr.GetShader("text")->GetVertexShader();
                pPS = ResMgr.GetShader("text")->GetPixelShader();
            }

            PSOCreateInfo.pVS = pVS;
            PSOCreateInfo.pPS = pPS;
            PSOCreateInfo.PSODesc.Name = "Renderer2D Text PSO";

            device->CreateGraphicsPipelineState(PSOCreateInfo, &s_Data.TextPSO);
            
            if (auto* pVar = s_Data.TextPSO->GetStaticVariableByName(SHADER_TYPE_VERTEX, "GlobalConstants"))
                pVar->Set(s_Data.GlobalConstantBuffer);

            s_Data.TextPSO->CreateShaderResourceBinding(&s_Data.TextSRB, true);
        }
    }

    // 4. Initialize 3D Pipeline
    {
        // Create Mesh Constant Buffer
        BufferDesc CBDesc;
        CBDesc.Name = "Renderer Mesh CB";
        CBDesc.Size = sizeof(RendererData::MeshConstants);
        CBDesc.Usage = USAGE_DYNAMIC;
        CBDesc.BindFlags = BIND_UNIFORM_BUFFER;
        CBDesc.CPUAccessFlags = CPU_ACCESS_WRITE;
        device->CreateBuffer(CBDesc, nullptr, &s_Data.MeshConstantBuffer);

        GraphicsPipelineStateCreateInfo PSOCreateInfo;
        PSOCreateInfo.PSODesc.Name = "Renderer3D PSO";
        PSOCreateInfo.PSODesc.PipelineType = PIPELINE_TYPE_GRAPHICS;
        PSOCreateInfo.GraphicsPipeline.NumRenderTargets = 1;
        PSOCreateInfo.GraphicsPipeline.RTVFormats[0] = TEX_FORMAT_RGBA8_UNORM;
        PSOCreateInfo.GraphicsPipeline.DSVFormat = TEX_FORMAT_D24_UNORM_S8_UINT;
        PSOCreateInfo.GraphicsPipeline.PrimitiveTopology = PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
        
        PSOCreateInfo.GraphicsPipeline.RasterizerDesc.CullMode = CULL_MODE_BACK;
        PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthEnable = true;
        PSOCreateInfo.GraphicsPipeline.DepthStencilDesc.DepthWriteEnable = true;

        // Standard Mesh Layout: Pos(3), Normal(3), UV(2)
        LayoutElement LayoutElems[] = {
            LayoutElement{ 0, 0, 3, VT_FLOAT32, False }, // Position
            LayoutElement{ 1, 0, 3, VT_FLOAT32, False }, // Normal
            LayoutElement{ 2, 0, 2, VT_FLOAT32, False }  // UV
        };
        PSOCreateInfo.GraphicsPipeline.InputLayout.LayoutElements = LayoutElems;
        PSOCreateInfo.GraphicsPipeline.InputLayout.NumElements = _countof(LayoutElems);

        // Use a simple 3D shader
        auto& ResMgr = ResourceManager::GetInstance();
        Shader* basic3DShader = ResMgr.GetShader("basic3d");
        if (!basic3DShader)
        {
             Logger::Warn("Renderer: 'basic3d' shader not found. 3D rendering may fail.");
             // Try fallback to basic if available, though layout might differ
             basic3DShader = ResMgr.GetShader("basic");
        }

        if (basic3DShader)
        {
            IShader* pVS = basic3DShader->GetVertexShader(); 
            IShader* pPS = basic3DShader->GetPixelShader();

            PSOCreateInfo.pVS = pVS;
            PSOCreateInfo.pPS = pPS;
        }
        else
        {
             Logger::Error("Renderer: No suitable shader for 3D pipeline.");
             return;
        }

        ShaderResourceVariableDesc Vars[] = {
            { SHADER_TYPE_PIXEL, "u_Texture", SHADER_RESOURCE_VARIABLE_TYPE_DYNAMIC },
            { SHADER_TYPE_VERTEX, "GlobalConstants", SHADER_RESOURCE_VARIABLE_TYPE_STATIC },
            { SHADER_TYPE_VERTEX | SHADER_TYPE_PIXEL, "MeshConstants", SHADER_RESOURCE_VARIABLE_TYPE_STATIC }
        };
        PSOCreateInfo.PSODesc.ResourceLayout.Variables = Vars;
        PSOCreateInfo.PSODesc.ResourceLayout.NumVariables = _countof(Vars);

        SamplerDesc SamLinear;
        SamLinear.MinFilter = FILTER_TYPE_LINEAR;
        SamLinear.MagFilter = FILTER_TYPE_LINEAR;
        SamLinear.MipFilter = FILTER_TYPE_LINEAR;

        ImmutableSamplerDesc ImtblSamplers[] = {
            { SHADER_TYPE_PIXEL, "u_Sampler", SamLinear },
            { SHADER_TYPE_PIXEL, "u_SamplerLinear", SamLinear }
        };
        PSOCreateInfo.PSODesc.ResourceLayout.ImmutableSamplers = ImtblSamplers;
        PSOCreateInfo.PSODesc.ResourceLayout.NumImmutableSamplers = _countof(ImtblSamplers);

        device->CreateGraphicsPipelineState(PSOCreateInfo, &s_Data.MeshPSO);
        
        if (auto* pVar = s_Data.MeshPSO->GetStaticVariableByName(SHADER_TYPE_VERTEX, "GlobalConstants"))
            pVar->Set(s_Data.GlobalConstantBuffer);
        if (auto* pVar = s_Data.MeshPSO->GetStaticVariableByName(SHADER_TYPE_VERTEX, "MeshConstants"))
            pVar->Set(s_Data.MeshConstantBuffer);
        if (auto* pVar = s_Data.MeshPSO->GetStaticVariableByName(SHADER_TYPE_PIXEL, "MeshConstants"))
            pVar->Set(s_Data.MeshConstantBuffer);

        s_Data.MeshPSO->CreateShaderResourceBinding(&s_Data.MeshSRB, true);
    }
}

void Renderer::Shutdown()
{
    delete[] s_Data.QuadBufferBase;
    s_Data.QuadVB.Release();
    s_Data.QuadIB.Release();
    s_Data.QuadPSO.Release();
    s_Data.QuadSRB.Release();
    s_Data.TextPSO.Release();
    s_Data.TextSRB.Release();
    s_Data.MeshPSO.Release();
    s_Data.MeshSRB.Release();
    s_Data.WhiteTexture.Release();
    s_Data.GlobalConstantBuffer.Release();
    s_Data.MeshConstantBuffer.Release();
}

void Renderer::BeginScene(Camera& camera)
{
    // Update Global Constants
    {
        MapHelper<RendererData::GlobalConstants> CBData(Window::GetContext(), s_Data.GlobalConstantBuffer, MAP_WRITE, MAP_FLAG_DISCARD);
        CBData->ViewProjection = camera.GetViewProjectionMatrix();
    }

    StartBatch();
}

void Renderer::BeginScene(const glm::mat4& viewProj)
{
    // Update Global Constants
    {
        MapHelper<RendererData::GlobalConstants> CBData(Window::GetContext(), s_Data.GlobalConstantBuffer, MAP_WRITE, MAP_FLAG_DISCARD);
        CBData->ViewProjection = viewProj;
    }

    StartBatch();
}

void Renderer::EndScene()
{
    Flush();
}

void Renderer::StartBatch()
{
    s_Data.QuadIndexCount = 0;
    s_Data.QuadBufferPtr = s_Data.QuadBufferBase;
    s_Data.TextureSlotIndex = 1;
    s_Data.CurrentPipeline = RendererData::PipelineType::None;
}

void Renderer::NextBatch()
{
    Flush();
    StartBatch();
}

void Renderer::Flush()
{
    if (s_Data.QuadIndexCount == 0 || !s_Data.QuadPSO || !s_Data.QuadVB || !s_Data.QuadIB)
        return;

    auto context = Window::GetContext();

    // 1. Update Vertex Buffer
    uint32_t dataSize = (uint32_t)((uint8_t*)s_Data.QuadBufferPtr - (uint8_t*)s_Data.QuadBufferBase);
    {
        MapHelper<RendererData::QuadVertex> VBData(context, s_Data.QuadVB, MAP_WRITE, MAP_FLAG_DISCARD);
        if (!VBData)
        {
            Logger::Error("Renderer: Failed to map vertex buffer. Dynamic heap might be exhausted.");
            return;
        }
            
        memcpy(VBData, s_Data.QuadBufferBase, dataSize);
    }

    // 2. Bind Pipeline
    IPipelineState* pPSO = nullptr;
    IShaderResourceBinding* pSRB = nullptr;

    if (s_Data.CurrentPipeline == RendererData::PipelineType::Text)
    {
        pPSO = s_Data.TextPSO;
        pSRB = s_Data.TextSRB;
    }
    else
    {
        pPSO = s_Data.QuadPSO;
        pSRB = s_Data.QuadSRB;
    }

    context->SetPipelineState(pPSO);

    // 3. Bind Textures
    if (auto* pVar = pSRB->GetVariableByName(SHADER_TYPE_PIXEL, "u_Textures"))
    {
        std::vector<IDeviceObject*> pViews(s_Data.MaxTextureSlots);
        for (uint32_t i = 0; i < s_Data.TextureSlotIndex; ++i)
            pViews[i] = s_Data.TextureSlots[i];
        for (uint32_t i = s_Data.TextureSlotIndex; i < s_Data.MaxTextureSlots; ++i)
            pViews[i] = s_Data.WhiteTexture;

        pVar->SetArray(pViews.data(), 0, s_Data.MaxTextureSlots);
    }
    context->CommitShaderResources(pSRB, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

    // 4. Draw
    IBuffer* pVBs[] = { s_Data.QuadVB };
    Uint64 offsets[] = { 0 };
    context->SetVertexBuffers(0, 1, pVBs, offsets, RESOURCE_STATE_TRANSITION_MODE_TRANSITION, SET_VERTEX_BUFFERS_FLAG_RESET);
    context->SetIndexBuffer(s_Data.QuadIB, 0, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

    DrawIndexedAttribs DrawAttrs;
    DrawAttrs.NumIndices = s_Data.QuadIndexCount;
    DrawAttrs.IndexType = VT_UINT32;
    DrawAttrs.Flags = DRAW_FLAG_VERIFY_ALL;
    context->DrawIndexed(DrawAttrs);

    s_Data.Stats.DrawCalls++;
}

// ==============================================================================================
// 2D Implementation
// ==============================================================================================

void Renderer::DrawQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color)
{
    DrawQuad({ position.x, position.y, 0.0f }, size, color);
}

void Renderer::DrawQuad(const glm::vec3& position, const glm::vec2& size, const glm::vec4& color)
{
    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.CurrentPipeline == RendererData::PipelineType::Text)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Quad;

    const float texIndex = 0.0f; // White Texture
    const float tiling = 1.0f;

    glm::vec3 p0 = { position.x - size.x * 0.5f, position.y - size.y * 0.5f, position.z };
    glm::vec3 p1 = { position.x + size.x * 0.5f, position.y - size.y * 0.5f, position.z };
    glm::vec3 p2 = { position.x + size.x * 0.5f, position.y + size.y * 0.5f, position.z };
    glm::vec3 p3 = { position.x - size.x * 0.5f, position.y + size.y * 0.5f, position.z };

    s_Data.QuadBufferPtr->Position = p0;
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = p1;
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = p2;
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = p3;
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadIndexCount += 6;
    s_Data.Stats.QuadCount++;
}

void Renderer::DrawQuad(const glm::vec2& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
    DrawQuad({ position.x, position.y, 0.0f }, size, texture, tiling, tintColor);
}

void Renderer::DrawQuad(const glm::vec3& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots || s_Data.CurrentPipeline == RendererData::PipelineType::Text)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Quad;

    float textureIndex = 0.0f;
    if (texture)
    {
        ITextureView* srv = texture->GetSRV();
        for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
        {
            if (s_Data.TextureSlots[i] == srv)
            {
                textureIndex = (float)i;
                break;
            }
        }

        if (textureIndex == 0.0f)
        {
            if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
                NextBatch();
            textureIndex = (float)s_Data.TextureSlotIndex;
            s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
            s_Data.TextureSlotIndex++;
        }
    }

    glm::vec3 p0 = { position.x - size.x * 0.5f, position.y - size.y * 0.5f, position.z };
    glm::vec3 p1 = { position.x + size.x * 0.5f, position.y - size.y * 0.5f, position.z };
    glm::vec3 p2 = { position.x + size.x * 0.5f, position.y + size.y * 0.5f, position.z };
    glm::vec3 p3 = { position.x - size.x * 0.5f, position.y + size.y * 0.5f, position.z };

    s_Data.QuadBufferPtr->Position = p0;
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = p1;
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = p2;
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = p3;
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadIndexCount += 6;
    s_Data.Stats.QuadCount++;
}

void Renderer::DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
    DrawRotatedQuad({ position.x, position.y, 0.0f }, size, rotation, color);
}

void Renderer::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.CurrentPipeline == RendererData::PipelineType::Text)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Quad;

    const float texIndex = 0.0f; // White Texture
    const float tiling = 1.0f;

    glm::mat4 transform = glm::translate(glm::mat4(1.0f), position)
        * glm::rotate(glm::mat4(1.0f), glm::radians(rotation), { 0.0f, 0.0f, 1.0f })
        * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

    glm::vec4 p0 = transform * glm::vec4(-0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p1 = transform * glm::vec4( 0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p2 = transform * glm::vec4( 0.5f,  0.5f, 0.0f, 1.0f);
    glm::vec4 p3 = transform * glm::vec4(-0.5f,  0.5f, 0.0f, 1.0f);

    s_Data.QuadBufferPtr->Position = { p0.x, p0.y, p0.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p1.x, p1.y, p1.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p2.x, p2.y, p2.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p3.x, p3.y, p3.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadIndexCount += 6;
    s_Data.Stats.QuadCount++;
}

void Renderer::DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, Texture* texture, float tiling, const glm::vec4& tintColor)
{
    DrawRotatedQuad({ position.x, position.y, 0.0f }, size, rotation, texture, tiling, tintColor);
}

void Renderer::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, Texture* texture, float tiling, const glm::vec4& tintColor)
{
    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots || s_Data.CurrentPipeline == RendererData::PipelineType::Text)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Quad;

    float textureIndex = 0.0f;
    if (texture)
    {
        ITextureView* srv = texture->GetSRV();
        for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
        {
            if (s_Data.TextureSlots[i] == srv)
            {
                textureIndex = (float)i;
                break;
            }
        }

        if (textureIndex == 0.0f)
        {
            if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
                NextBatch();
            textureIndex = (float)s_Data.TextureSlotIndex;
            s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
            s_Data.TextureSlotIndex++;
        }
    }

    glm::mat4 transform = glm::translate(glm::mat4(1.0f), position)
        * glm::rotate(glm::mat4(1.0f), glm::radians(rotation), { 0.0f, 0.0f, 1.0f })
        * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

    glm::vec4 p0 = transform * glm::vec4(-0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p1 = transform * glm::vec4( 0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p2 = transform * glm::vec4( 0.5f,  0.5f, 0.0f, 1.0f);
    glm::vec4 p3 = transform * glm::vec4(-0.5f,  0.5f, 0.0f, 1.0f);

    s_Data.QuadBufferPtr->Position = { p0.x, p0.y, p0.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p1.x, p1.y, p1.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p2.x, p2.y, p2.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p3.x, p3.y, p3.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadIndexCount += 6;
    s_Data.Stats.QuadCount++;
}

void Renderer::DrawString(const std::string& text, Font* font, const glm::vec3& position, float scale, const glm::vec4& color)
{
    if (!font) return;

    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots || s_Data.CurrentPipeline == RendererData::PipelineType::Quad)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Text;

    const auto& characters = font->GetCharacters();
    ITextureView* srv = font->GetAtlasTexture()->GetSRV();
    float textureIndex = 0.0f;

    // Find or add texture
    for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
    {
        if (s_Data.TextureSlots[i] == srv)
        {
            textureIndex = (float)i;
            break;
        }
    }

    if (textureIndex == 0.0f)
    {
        if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
            NextBatch();
        textureIndex = (float)s_Data.TextureSlotIndex;
        s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
        s_Data.TextureSlotIndex++;
    }

    float x = position.x;
    float y = position.y;
    float z = position.z;

    for (char c : text)
    {
        if (characters.find(c) == characters.end())
            continue;

        const Character& ch = characters.at(c);

        float xpos = x + ch.Bearing.x * scale;
        float ypos = y - (ch.Size.y - ch.Bearing.y) * scale;

        float w = ch.Size.x * scale;
        float h = ch.Size.y * scale;

        // Check batch capacity
        if (s_Data.QuadIndexCount >= s_Data.MaxIndices)
            NextBatch();

        s_Data.QuadBufferPtr->Position = { xpos, ypos, z };
        s_Data.QuadBufferPtr->Color = color;
        s_Data.QuadBufferPtr->TexCoord = { ch.uvMin.x, ch.uvMax.y }; // BL
        s_Data.QuadBufferPtr->TexIndex = textureIndex;
        s_Data.QuadBufferPtr->Tiling = 1.0f;
        s_Data.QuadBufferPtr->IsText = 1.0f;
        s_Data.QuadBufferPtr++;

        s_Data.QuadBufferPtr->Position = { xpos + w, ypos, z };
        s_Data.QuadBufferPtr->Color = color;
        s_Data.QuadBufferPtr->TexCoord = { ch.uvMax.x, ch.uvMax.y }; // BR
        s_Data.QuadBufferPtr->TexIndex = textureIndex;
        s_Data.QuadBufferPtr->Tiling = 1.0f;
        s_Data.QuadBufferPtr->IsText = 1.0f;
        s_Data.QuadBufferPtr++;

        s_Data.QuadBufferPtr->Position = { xpos + w, ypos + h, z };
        s_Data.QuadBufferPtr->Color = color;
        s_Data.QuadBufferPtr->TexCoord = { ch.uvMax.x, ch.uvMin.y }; // TR
        s_Data.QuadBufferPtr->TexIndex = textureIndex;
        s_Data.QuadBufferPtr->Tiling = 1.0f;
        s_Data.QuadBufferPtr->IsText = 1.0f;
        s_Data.QuadBufferPtr++;

        s_Data.QuadBufferPtr->Position = { xpos, ypos + h, z };
        s_Data.QuadBufferPtr->Color = color;
        s_Data.QuadBufferPtr->TexCoord = { ch.uvMin.x, ch.uvMin.y }; // TL
        s_Data.QuadBufferPtr->TexIndex = textureIndex;
        s_Data.QuadBufferPtr->Tiling = 1.0f;
        s_Data.QuadBufferPtr->IsText = 1.0f;
        s_Data.QuadBufferPtr++;

        s_Data.QuadIndexCount += 6;
        s_Data.Stats.QuadCount++;

        x += (ch.Advance >> 6) * scale;
    }
}

void Renderer::DrawQuad(const glm::mat4& transform, const glm::vec4& color)
{
    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.CurrentPipeline == RendererData::PipelineType::Text)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Quad;

    const float texIndex = 0.0f; // White Texture
    const float tiling = 1.0f;

    glm::vec4 p0 = transform * glm::vec4(-0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p1 = transform * glm::vec4( 0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p2 = transform * glm::vec4( 0.5f,  0.5f, 0.0f, 1.0f);
    glm::vec4 p3 = transform * glm::vec4(-0.5f,  0.5f, 0.0f, 1.0f);

    s_Data.QuadBufferPtr->Position = { p0.x, p0.y, p0.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p1.x, p1.y, p1.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p2.x, p2.y, p2.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p3.x, p3.y, p3.z };
    s_Data.QuadBufferPtr->Color = color;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = texIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadIndexCount += 6;
    s_Data.Stats.QuadCount++;
}

void Renderer::DrawQuad(const glm::mat4& transform, Texture* texture, float tiling, const glm::vec4& tintColor)
{
    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots || s_Data.CurrentPipeline == RendererData::PipelineType::Text)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Quad;

    float textureIndex = 0.0f;
    if (texture)
    {
        ITextureView* srv = texture->GetSRV();
        for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
        {
            if (s_Data.TextureSlots[i] == srv)
            {
                textureIndex = (float)i;
                break;
            }
        }

        if (textureIndex == 0.0f)
        {
            if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
                NextBatch();
            textureIndex = (float)s_Data.TextureSlotIndex;
            s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
            s_Data.TextureSlotIndex++;
        }
    }

    glm::vec4 p0 = transform * glm::vec4(-0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p1 = transform * glm::vec4( 0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p2 = transform * glm::vec4( 0.5f,  0.5f, 0.0f, 1.0f);
    glm::vec4 p3 = transform * glm::vec4(-0.5f,  0.5f, 0.0f, 1.0f);

    s_Data.QuadBufferPtr->Position = { p0.x, p0.y, p0.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p1.x, p1.y, p1.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 0.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p2.x, p2.y, p2.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 1.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p3.x, p3.y, p3.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = { 0.0f, 1.0f };
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = tiling;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadIndexCount += 6;
    s_Data.Stats.QuadCount++;
}

void Renderer::DrawQuadUV(const glm::mat4& transform, Texture* texture, const glm::vec2 uvs[4], const glm::vec4& tintColor)
{
    if (s_Data.QuadIndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots || s_Data.CurrentPipeline == RendererData::PipelineType::Text)
        NextBatch();

    s_Data.CurrentPipeline = RendererData::PipelineType::Quad;

    float textureIndex = 0.0f;
    if (texture)
    {
        ITextureView* srv = texture->GetSRV();
        for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
        {
            if (s_Data.TextureSlots[i] == srv)
            {
                textureIndex = (float)i;
                break;
            }
        }

        if (textureIndex == 0.0f)
        {
            if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
                NextBatch();
            textureIndex = (float)s_Data.TextureSlotIndex;
            s_Data.TextureSlots[s_Data.TextureSlotIndex] = srv;
            s_Data.TextureSlotIndex++;
        }
    }

    glm::vec4 p0 = transform * glm::vec4(-0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p1 = transform * glm::vec4( 0.5f, -0.5f, 0.0f, 1.0f);
    glm::vec4 p2 = transform * glm::vec4( 0.5f,  0.5f, 0.0f, 1.0f);
    glm::vec4 p3 = transform * glm::vec4(-0.5f,  0.5f, 0.0f, 1.0f);

    s_Data.QuadBufferPtr->Position = { p0.x, p0.y, p0.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = uvs[0];
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = 1.0f;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p1.x, p1.y, p1.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = uvs[1];
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = 1.0f;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p2.x, p2.y, p2.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = uvs[2];
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = 1.0f;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadBufferPtr->Position = { p3.x, p3.y, p3.z };
    s_Data.QuadBufferPtr->Color = tintColor;
    s_Data.QuadBufferPtr->TexCoord = uvs[3];
    s_Data.QuadBufferPtr->TexIndex = textureIndex;
    s_Data.QuadBufferPtr->Tiling = 1.0f;
    s_Data.QuadBufferPtr->IsText = 0.0f;
    s_Data.QuadBufferPtr++;

    s_Data.QuadIndexCount += 6;
    s_Data.Stats.QuadCount++;
}

// ==============================================================================================
// 3D Implementation
// ==============================================================================================

void Renderer::DrawMesh(const MeshData& mesh, const glm::mat4& transform, const glm::vec4& color)
{
    DrawMesh(mesh, transform, nullptr, color);
}

void Renderer::DrawMesh(const MeshData& mesh, const glm::mat4& transform, Texture* texture, const glm::vec4& color)
{
    // Flush 2D batch if any, to preserve order (though 3D usually draws before 2D UI)
    // But if we mix them, we should flush.
    Flush();

    auto context = Window::GetContext();

    // Update Mesh Constants
    {
        MapHelper<RendererData::MeshConstants> CBData(context, s_Data.MeshConstantBuffer, MAP_WRITE, MAP_FLAG_DISCARD);
        CBData->World = transform;
        CBData->Color = color;
        CBData->UseTexture = texture ? 1.0f : 0.0f;
    }

    context->SetPipelineState(s_Data.MeshPSO);

    // Bind Texture
    if (auto* pVar = s_Data.MeshSRB->GetVariableByName(SHADER_TYPE_PIXEL, "u_Texture"))
    {
        if (texture)
            pVar->Set(texture->GetSRV());
        else
            pVar->Set(s_Data.WhiteTexture);
    }
    context->CommitShaderResources(s_Data.MeshSRB, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

    // Bind Buffers
    IBuffer* pVBs[] = { mesh.VertexBuffer };
    Uint64 offsets[] = { 0 };
    context->SetVertexBuffers(0, 1, pVBs, offsets, RESOURCE_STATE_TRANSITION_MODE_TRANSITION, SET_VERTEX_BUFFERS_FLAG_RESET);
    context->SetIndexBuffer(mesh.IndexBuffer, 0, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

    DrawIndexedAttribs DrawAttrs;
    DrawAttrs.NumIndices = mesh.IndexCount;
    DrawAttrs.IndexType = VT_UINT32;
    DrawAttrs.Flags = DRAW_FLAG_VERIFY_ALL;
    context->DrawIndexed(DrawAttrs);

    s_Data.Stats.DrawCalls++;
}

Renderer::Statistics Renderer::GetStats()
{
    return s_Data.Stats;
}

void Renderer::ResetStats()
{
    memset(&s_Data.Stats, 0, sizeof(Statistics));
}

ITextureView* Renderer::GetWhiteTexture()
{
    return s_Data.WhiteTexture;
}
