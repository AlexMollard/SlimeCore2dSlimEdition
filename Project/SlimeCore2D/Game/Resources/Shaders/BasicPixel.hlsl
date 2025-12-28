Texture2D u_Textures[32] : register(t0);
SamplerState u_Sampler : register(s0);

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

    switch(index)
    {
        case 0: sampled = u_Textures[0].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 1: sampled = u_Textures[1].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 2: sampled = u_Textures[2].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 3: sampled = u_Textures[3].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 4: sampled = u_Textures[4].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 5: sampled = u_Textures[5].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 6: sampled = u_Textures[6].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 7: sampled = u_Textures[7].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 8: sampled = u_Textures[8].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 9: sampled = u_Textures[9].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 10: sampled = u_Textures[10].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 11: sampled = u_Textures[11].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 12: sampled = u_Textures[12].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 13: sampled = u_Textures[13].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 14: sampled = u_Textures[14].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 15: sampled = u_Textures[15].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 16: sampled = u_Textures[16].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 17: sampled = u_Textures[17].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 18: sampled = u_Textures[18].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 19: sampled = u_Textures[19].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 20: sampled = u_Textures[20].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 21: sampled = u_Textures[21].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 22: sampled = u_Textures[22].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 23: sampled = u_Textures[23].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 24: sampled = u_Textures[24].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 25: sampled = u_Textures[25].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 26: sampled = u_Textures[26].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 27: sampled = u_Textures[27].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 28: sampled = u_Textures[28].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 29: sampled = u_Textures[29].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 30: sampled = u_Textures[30].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
        case 31: sampled = u_Textures[31].Sample(u_Sampler, input.TexCoord * input.Tiling); break;
    }

    if (input.IsText > 0.5)
    {
        float distance = sampled.r;
        float smoothing = 1.0 / 16.0;
        float alpha = smoothstep(0.5 - smoothing, 0.5 + smoothing, distance);
        texColor = float4(input.Color.rgb, input.Color.a * alpha);
    }
    else
    {
        texColor *= sampled;
    }
    
    if (texColor.a < 0.01) discard;
    
    return texColor;
}
