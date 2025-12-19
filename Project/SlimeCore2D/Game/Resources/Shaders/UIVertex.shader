#version 450

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in float aTexIndex;

uniform mat4 OrthoMatrix;
uniform mat4 Model;
uniform vec3 position;

out vec4 Color;
out vec2 TexCoord;
out float TexIndex;

void main()
{
	Color = aColor;
	TexIndex = aTexIndex;

	TexCoord = aTexCoord;
	TexCoord.y = -1 * TexCoord.y;

	gl_Position = OrthoMatrix * Model * vec4(-aPos.xyz, 1.0);
}