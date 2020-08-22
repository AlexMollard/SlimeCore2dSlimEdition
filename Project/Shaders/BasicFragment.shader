#version 450

layout(location = 0) out vec4 FragColor;

in vec4 Color;
in vec2 TexCoord;
in float TexIndex;

uniform sampler2D Textures[31];

uniform vec3 color;

uniform vec4 SunColor;

void main()
{
	int index = int(TexIndex);

	if (texture(Textures[index], TexCoord).a < 0.01)
		discard;

	FragColor = (texture(Textures[index], TexCoord) * Color) * SunColor;
}