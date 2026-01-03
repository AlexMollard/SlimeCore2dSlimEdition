Texture2D u_Textures[32] : register(t0);
SamplerState u_Sampler : register(s0);
SamplerState u_SamplerLinear : register(s1);

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float TexIndex : TEXCOORD1;
    float Tiling : TILING;
    float IsText : ISTEXT;
};

float4 SampleTexture(int index, SamplerState sam, float2 uv)
{
    switch(index)
    {
        case 0: return u_Textures[0].Sample(sam, uv);
        case 1: return u_Textures[1].Sample(sam, uv);
        case 2: return u_Textures[2].Sample(sam, uv);
        case 3: return u_Textures[3].Sample(sam, uv);
        case 4: return u_Textures[4].Sample(sam, uv);
        case 5: return u_Textures[5].Sample(sam, uv);
        case 6: return u_Textures[6].Sample(sam, uv);
        case 7: return u_Textures[7].Sample(sam, uv);
        case 8: return u_Textures[8].Sample(sam, uv);
        case 9: return u_Textures[9].Sample(sam, uv);
        case 10: return u_Textures[10].Sample(sam, uv);
        case 11: return u_Textures[11].Sample(sam, uv);
        case 12: return u_Textures[12].Sample(sam, uv);
        case 13: return u_Textures[13].Sample(sam, uv);
        case 14: return u_Textures[14].Sample(sam, uv);
        case 15: return u_Textures[15].Sample(sam, uv);
        case 16: return u_Textures[16].Sample(sam, uv);
        case 17: return u_Textures[17].Sample(sam, uv);
        case 18: return u_Textures[18].Sample(sam, uv);
        case 19: return u_Textures[19].Sample(sam, uv);
        case 20: return u_Textures[20].Sample(sam, uv);
        case 21: return u_Textures[21].Sample(sam, uv);
        case 22: return u_Textures[22].Sample(sam, uv);
        case 23: return u_Textures[23].Sample(sam, uv);
        case 24: return u_Textures[24].Sample(sam, uv);
        case 25: return u_Textures[25].Sample(sam, uv);
        case 26: return u_Textures[26].Sample(sam, uv);
        case 27: return u_Textures[27].Sample(sam, uv);
        case 28: return u_Textures[28].Sample(sam, uv);
        case 29: return u_Textures[29].Sample(sam, uv);
        case 30: return u_Textures[30].Sample(sam, uv);
        case 31: return u_Textures[31].Sample(sam, uv);
        default: return u_Textures[0].Sample(sam, uv);
    }
}

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
