#include "Structures.fxh"

struct VS_INPUT
{
    // Per-Vertex (Slot 0)
    float2 QuadPos : ATTRIB0; // -0.5 to 0.5
    float2 QuadUV  : ATTRIB1; // 0 to 1

    // Per-Instance (Slot 1)
    float2 TilePos   : ATTRIB2; // World Position of center
    float2 TileSize  : ATTRIB3; // Width, Height
    float4 TexRect   : ATTRIB4; // u0, v0, u1, v1
    float4 Color     : ATTRIB5;
    float  TexIndex  : ATTRIB6;
    float  Rotation  : ATTRIB7;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float TexIndex : TEXCOORD1;
};

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;

    // Rotate QuadPos
    float c = cos(input.Rotation);
    float s = sin(input.Rotation);
    float2 rotatedPos;
    rotatedPos.x = input.QuadPos.x * c - input.QuadPos.y * s;
    rotatedPos.y = input.QuadPos.x * s + input.QuadPos.y * c;

    // Scale and Translate
    float2 worldPos = input.TilePos + rotatedPos * input.TileSize;
    
    // Use Z=0.9 to push it to the background (Near=0, Far=1 in NDC)
    output.Pos = mul(u_ViewProjection, float4(worldPos, 0.9, 1.0));
    output.Color = input.Color;
    
    // UVs
    // QuadUV is 0..1
    // TexRect is u0, v0, u1, v1
    // u = lerp(u0, u1, QuadUV.x)
    // v = lerp(v0, v1, QuadUV.y)
    output.TexCoord.x = lerp(input.TexRect.x, input.TexRect.z, input.QuadUV.x);
    output.TexCoord.y = lerp(input.TexRect.y, input.TexRect.w, input.QuadUV.y);
    
    output.TexIndex = input.TexIndex;
    
    return output;
}
