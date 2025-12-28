#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in float aTexIndex;
layout (location = 4) in float aTiling;
layout (location = 5) in float aIsText;

out vec4 vColor;
out vec2 vTexCoord;
flat out float vTexIndex;
out float vTiling;
out float vIsText;

uniform mat4 u_ViewProjection;

void main()
{
    vColor = aColor;
    vTexCoord = aTexCoord;
    vTexIndex = aTexIndex;
    vTiling = aTiling;
    vIsText = aIsText;
    gl_Position = u_ViewProjection * vec4(aPos, 1.0);
}