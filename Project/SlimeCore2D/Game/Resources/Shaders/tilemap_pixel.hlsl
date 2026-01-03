#include "Structures.fxh"
#include "TextureSampler.fxh"

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float TexIndex : TEXCOORD1;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    float4 texColor = input.Color;
    int index = (int)(input.TexIndex + 0.5);
    
    float4 sampled = SampleTexture(index, u_Sampler, input.TexCoord);
    texColor *= sampled;
    
    if (texColor.a < 0.01) discard;
    
    return texColor;
}
