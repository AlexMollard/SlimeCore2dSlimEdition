#include "Structures.fxh"

struct VS_INPUT
{
    float3 Pos : ATTRIB0;
    float2 TexCoord : ATTRIB1;
    float4 Transform0 : ATTRIB2;
    float4 Transform1 : ATTRIB3;
    float4 Transform2 : ATTRIB4;
    float4 Transform3 : ATTRIB5;
    float4 Color : ATTRIB6;
    float4 UVRect : ATTRIB7;
    float TexIndex : ATTRIB8;
    float Tiling : ATTRIB9;
    float IsText : ATTRIB10;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float TexIndex : TEXCOORD1;
    float Tiling : TILING;
    float IsText : ISTEXT;
};

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;
    
    // Reconstruct the matrix from the 4 attribute vectors
    // Note: HLSL matrices are column-major by default.
    // If we sent row-major data from C++, we might need to transpose or construct carefully.
    // In Renderer2D.cpp, we copy glm::mat4 (column-major) directly.
    // So Transform0 is Column 0.
    // float4x4(c0, c1, c2, c3) constructs a matrix from columns in HLSL.
    
    float4x4 transform = float4x4(
        input.Transform0,
        input.Transform1,
        input.Transform2,
        input.Transform3
    );
    
    // Fix: The matrix constructed above treats the input vectors as rows, effectively transposing the matrix.
    // To apply the transform correctly, we multiply the vector by the transposed matrix: v * M^T = (M * v)^T
    float4 worldPos = mul(float4(input.Pos, 1.0), transform);
    output.Pos = mul(u_ViewProjection, worldPos);
    output.Color = input.Color;
    
    // UV Calculation
    float2 uv = input.TexCoord;
    // Map 0..1 to UVRect (xy = min, zw = max)
    uv = uv * (input.UVRect.zw - input.UVRect.xy) + input.UVRect.xy;
    output.TexCoord = uv;
    
    output.TexIndex = input.TexIndex;
    output.Tiling = input.Tiling;
    output.IsText = input.IsText;
    
    return output;
}
