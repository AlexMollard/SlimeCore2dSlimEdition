Texture2D    u_Texture;
SamplerState u_Sampler;

cbuffer MeshConstants
{
    float4x4 World;
    float4 Color;
    float4 UseTexture; // x = UseTexture, yzw = Padding
};

struct PSInput
{
    float4 Pos      : SV_POSITION;
    float4 Color    : COLOR0;
    float2 UV       : TEXCOORD0;
    float3 Normal   : NORMAL;
    float3 WorldPos : TEXCOORD1;
};

struct PSOutput
{
    float4 Color : SV_TARGET;
};

void main(in PSInput PSIn, out PSOutput PSOut)
{
    float4 texColor = float4(1.0, 1.0, 1.0, 1.0);
    
    if (UseTexture.x > 0.5)
    {
        texColor = u_Texture.Sample(u_Sampler, PSIn.UV);
    }

    // Simple directional light (hardcoded for "basic" 3D)
    float3 lightDir = normalize(float3(-0.5, -1.0, -0.5));
    float diff = max(dot(PSIn.Normal, -lightDir), 0.0);
    float3 ambient = float3(0.3, 0.3, 0.3);
    float3 diffuse = float3(1.0, 1.0, 1.0) * diff;
    float3 lighting = ambient + diffuse;

    PSOut.Color = texColor * PSIn.Color * float4(lighting, 1.0);
}
