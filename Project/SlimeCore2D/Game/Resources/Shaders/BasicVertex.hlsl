#include "Structures.fxh"

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
    nointerpolation float TexIndex : TEXCOORD1;
    nointerpolation float Tiling : TILING;
    nointerpolation float IsText : ISTEXT;
};

PS_INPUT main(VS_INPUT vsInput)
{
    PS_INPUT output;
    
    output.Pos = mul(u_ViewProjection, float4(vsInput.Pos, 1.0));
    output.Color = vsInput.Color;
    output.TexCoord = vsInput.TexCoord;
    output.TexIndex = vsInput.TexIndex;
    output.Tiling = vsInput.Tiling;
    output.IsText = vsInput.IsText;
    
    return output;
}
