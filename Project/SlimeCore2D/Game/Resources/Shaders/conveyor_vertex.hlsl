cbuffer ConstantBuffer : register(b0)
{
    matrix u_ViewProjection;
    float u_Time;
    float3 padding;
};

struct VS_INPUT
{
    float3 Position : ATTRIB0;
    float4 Color : ATTRIB1;
    float2 TexCoord : ATTRIB2;
    float IsBelt : ATTRIB3; // Using TEXCOORD1 for IsBelt flag
    float Tiling : ATTRIB4;
    float IsText : ATTRIB5;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float IsBelt : TEXCOORD1;
    float Speed : TEXCOORD2;
};

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;
    output.Pos = mul(float4(input.Position, 1.0f), u_ViewProjection);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    output.IsBelt = input.IsBelt;
    output.Speed = input.IsText;
    return output;
}
