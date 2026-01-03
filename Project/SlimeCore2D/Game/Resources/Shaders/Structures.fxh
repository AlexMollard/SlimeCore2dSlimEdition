cbuffer ConstantBuffer : register(b0)
{
    float4x4 u_ViewProjection;
    float u_Time;
    float3 padding;
};
