#include "Structures.fxh"
#include "TextureSampler.fxh"

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float TexIndex : TEXCOORD1;
    float Tiling : TILING;
    float IsText : ISTEXT;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    float4 texColor = input.Color;
    int index = (int)(input.TexIndex + 0.5);
    
    float4 sampled = float4(1.0, 1.0, 1.0, 1.0);
    
    if (input.IsText > 0.5)
    {
        // Text (SDF) - Linear Sampling
        sampled = SampleTexture(index, u_SamplerLinear, input.TexCoord * input.Tiling);
        
        float distance = sampled.r;
        float smoothing = fwidth(distance);
        float alpha = smoothstep(0.5 - smoothing, 0.5 + smoothing, distance);
        texColor = float4(input.Color.rgb, input.Color.a * alpha);
    }
    else
    {
        // Sprites - Point Sampling
        sampled = SampleTexture(index, u_Sampler, input.TexCoord * input.Tiling);
        texColor *= sampled;
    }
    
    if (texColor.a < 0.01) discard;

    return texColor;
}
