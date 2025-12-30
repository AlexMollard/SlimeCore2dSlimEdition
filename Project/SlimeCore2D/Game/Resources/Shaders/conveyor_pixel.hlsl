cbuffer ConstantBuffer : register(b0)
{
    matrix u_ViewProjection;
    float u_Time;
    float3 padding;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float IsBelt : TEXCOORD1;
    float Speed : TEXCOORD2;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    float4 color = input.Color;
    
    if (input.IsBelt > 0.5f)
    {
        // Moving arrow effect
        // We can use a procedural arrow pattern
        float2 uv = input.TexCoord;
        
        // Scroll UVs
        uv.y += u_Time * 2.0f * input.Speed; // Speed
        
        // Create arrow pattern
        // Simple chevron pattern
        float y = frac(uv.y);
        float x = abs(uv.x - 0.5f);
        
        // Arrow shape: y > x
        // Repeated
        
        if (y > x && y < x + 0.2f)
        {
            // Arrow color (lighter)
            color += float4(0.2f, 0.2f, 0.2f, 0.0f);
        }
    }
    
    return color;
}
