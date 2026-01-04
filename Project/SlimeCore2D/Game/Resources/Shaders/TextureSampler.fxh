#ifndef NonUniformResourceIndex
#define NonUniformResourceIndex(x) x
#endif

Texture2D u_Textures[32] : register(t0);
SamplerState u_Sampler : register(s0);
SamplerState u_SamplerLinear : register(s1);

float4 SampleTexture(int index, SamplerState sam, float2 uv, float2 dx, float2 dy)
{
    return u_Textures[NonUniformResourceIndex(index)].SampleGrad(sam, uv, dx, dy);
}
