cbuffer GlobalConstants
{
    float4x4 ViewProjection;
};

cbuffer MeshConstants
{
    float4x4 World;
    float4 Color;
    float4 UseTexture; // x = UseTexture, yzw = Padding
};

struct VSInput
{
    float3 Pos    : ATTRIB0;
    float3 Normal : ATTRIB1;
    float2 UV     : ATTRIB2;
};

struct PSInput
{
    float4 Pos      : SV_POSITION;
    float4 Color    : COLOR0;
    float2 UV       : TEXCOORD0;
    float3 Normal   : NORMAL;
    float3 WorldPos : TEXCOORD1;
};

void main(in VSInput VSIn, out PSInput PSOut)
{
    // Calculate World Position
    float4 worldPos = mul(World, float4(VSIn.Pos, 1.0));
    PSOut.WorldPos = worldPos.xyz;

    // Calculate Clip Space Position
    PSOut.Pos = mul(ViewProjection, worldPos);

    // Pass through Data
    PSOut.UV = VSIn.UV;
    
    // Transform Normal to World Space (assuming uniform scaling for now)
    PSOut.Normal = normalize(mul((float3x3)World, VSIn.Normal));
    
    PSOut.Color = Color;
}
