struct VS_INPUT
{
    float3 Pos : ATTRIB0;
    float4 Color : ATTRIB1;
    float2 TexCoord : ATTRIB2;
    float TexIndex : ATTRIB3;
    float Tiling : ATTRIB4;
    float IsText : ATTRIB5;
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

cbuffer ConstantBuffer : register(b0)
{
    float4x4 u_ViewProjection;
    float u_Time;
};

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;
    
    output.Pos = mul(u_ViewProjection, float4(input.Pos, 1.0));
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    output.TexIndex = input.TexIndex;
    output.Tiling = input.Tiling;
    output.IsText = input.IsText;
    
    return output;
}
