#version 330 core
out vec4 FragColor;

in vec4 vColor;
in vec2 vTexCoords;
in float vTexIndex;

uniform sampler2D Textures[32];

// Controls the softness. lower = sharper.
// Can be passed as uniform, but 0.1 - 0.2 is usually good for screen space SDF
const float smoothing = 1.0 / 16.0; 

void main()
{
    int index = int(vTexIndex);
    
    // Sample the distance field (stored in Red component)
    float dist = texture(Textures[index], vTexCoords).r;
    
    // Smoothstep creates the anti-aliased edge based on the distance
    // 0.5 is the midpoint (edge of the glyph)
    float alpha = smoothstep(0.5 - smoothing, 0.5 + smoothing, dist);
    
    if(alpha < 0.01) discard; // Optimization
    
    FragColor = vec4(vColor.rgb, vColor.a * alpha);
}