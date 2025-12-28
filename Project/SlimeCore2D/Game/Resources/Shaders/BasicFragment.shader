#version 330 core
layout (location = 0) out vec4 color;

in vec4 vColor;
in vec2 vTexCoord;
flat in float vTexIndex;
in float vTiling;
in float vIsText;

uniform sampler2D u_Textures[32];

void main()
{
    vec4 texColor = vColor;
    int index = int(vTexIndex + 0.5);
    
    vec4 sampled = vec4(1.0, 1.0, 1.0, 1.0);

    switch(index)
    {
        case 0: sampled = texture(u_Textures[0], vTexCoord * vTiling); break;
        case 1: sampled = texture(u_Textures[1], vTexCoord * vTiling); break;
        case 2: sampled = texture(u_Textures[2], vTexCoord * vTiling); break;
        case 3: sampled = texture(u_Textures[3], vTexCoord * vTiling); break;
        case 4: sampled = texture(u_Textures[4], vTexCoord * vTiling); break;
        case 5: sampled = texture(u_Textures[5], vTexCoord * vTiling); break;
        case 6: sampled = texture(u_Textures[6], vTexCoord * vTiling); break;
        case 7: sampled = texture(u_Textures[7], vTexCoord * vTiling); break;
        case 8: sampled = texture(u_Textures[8], vTexCoord * vTiling); break;
        case 9: sampled = texture(u_Textures[9], vTexCoord * vTiling); break;
        case 10: sampled = texture(u_Textures[10], vTexCoord * vTiling); break;
        case 11: sampled = texture(u_Textures[11], vTexCoord * vTiling); break;
        case 12: sampled = texture(u_Textures[12], vTexCoord * vTiling); break;
        case 13: sampled = texture(u_Textures[13], vTexCoord * vTiling); break;
        case 14: sampled = texture(u_Textures[14], vTexCoord * vTiling); break;
        case 15: sampled = texture(u_Textures[15], vTexCoord * vTiling); break;
        case 16: sampled = texture(u_Textures[16], vTexCoord * vTiling); break;
        case 17: sampled = texture(u_Textures[17], vTexCoord * vTiling); break;
        case 18: sampled = texture(u_Textures[18], vTexCoord * vTiling); break;
        case 19: sampled = texture(u_Textures[19], vTexCoord * vTiling); break;
        case 20: sampled = texture(u_Textures[20], vTexCoord * vTiling); break;
        case 21: sampled = texture(u_Textures[21], vTexCoord * vTiling); break;
        case 22: sampled = texture(u_Textures[22], vTexCoord * vTiling); break;
        case 23: sampled = texture(u_Textures[23], vTexCoord * vTiling); break;
        case 24: sampled = texture(u_Textures[24], vTexCoord * vTiling); break;
        case 25: sampled = texture(u_Textures[25], vTexCoord * vTiling); break;
        case 26: sampled = texture(u_Textures[26], vTexCoord * vTiling); break;
        case 27: sampled = texture(u_Textures[27], vTexCoord * vTiling); break;
        case 28: sampled = texture(u_Textures[28], vTexCoord * vTiling); break;
        case 29: sampled = texture(u_Textures[29], vTexCoord * vTiling); break;
        case 30: sampled = texture(u_Textures[30], vTexCoord * vTiling); break;
        case 31: sampled = texture(u_Textures[31], vTexCoord * vTiling); break;
    }

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