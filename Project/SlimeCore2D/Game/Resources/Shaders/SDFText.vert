#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in float aTexIndex;

out vec4 vColor;
out vec2 vTexCoords;
out float vTexIndex;

uniform mat4 OrthoMatrix;
uniform mat4 Model;

void main()
{
    vColor = aColor;
    vTexCoords = aTexCoords;
    vTexIndex = aTexIndex;
    gl_Position = OrthoMatrix * Model * vec4(aPos, 1.0);
}