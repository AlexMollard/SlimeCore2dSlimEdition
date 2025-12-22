#version 330 core
layout (location = 0) out vec4 color;

in vec4 vColor;
in vec2 vTexCoord;
in float vTexIndex;
in float vTiling;
in float vIsText;

uniform sampler2D u_Textures[32];

void main()
{
    vec4 texColor = vColor;
    int index = int(vTexIndex);
    
    // Sample texture
    // Note: older GLSL requires switch/case for sampler arrays if indexing is not supported
    vec4 sampled = texture(u_Textures[index], vTexCoord * vTiling);

    if (vIsText > 0.5) 
    {
        // SDF Logic
        float distance = sampled.r;
        float smoothing = 1.0 / 16.0; // Adjust for sharpness
        float alpha = smoothstep(0.5 - smoothing, 0.5 + smoothing, distance);
        texColor = vec4(vColor.rgb, vColor.a * alpha);
    }
    else
    {
        // Standard Sprite Logic
        texColor *= sampled;
    }

    if (texColor.a < 0.01) discard;
    color = texColor;
}