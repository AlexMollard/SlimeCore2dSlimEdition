#version 300 es
precision mediump float;

layout(location = 0) out vec4 FragColor;

in vec4 Color;
in vec2 TexCoord;
flat in float TexIndex;

uniform sampler2D Textures[31];

uniform vec3 color;

void main()
{
	int index = int(TexIndex + 0.5);
	vec4 texColor = vec4(1.0);

	switch(index)
	{
		case 0: texColor = texture(Textures[0], TexCoord); break;
		case 1: texColor = texture(Textures[1], TexCoord); break;
		case 2: texColor = texture(Textures[2], TexCoord); break;
		case 3: texColor = texture(Textures[3], TexCoord); break;
		case 4: texColor = texture(Textures[4], TexCoord); break;
		case 5: texColor = texture(Textures[5], TexCoord); break;
		case 6: texColor = texture(Textures[6], TexCoord); break;
		case 7: texColor = texture(Textures[7], TexCoord); break;
		case 8: texColor = texture(Textures[8], TexCoord); break;
		case 9: texColor = texture(Textures[9], TexCoord); break;
		case 10: texColor = texture(Textures[10], TexCoord); break;
		case 11: texColor = texture(Textures[11], TexCoord); break;
		case 12: texColor = texture(Textures[12], TexCoord); break;
		case 13: texColor = texture(Textures[13], TexCoord); break;
		case 14: texColor = texture(Textures[14], TexCoord); break;
		case 15: texColor = texture(Textures[15], TexCoord); break;
		case 16: texColor = texture(Textures[16], TexCoord); break;
		case 17: texColor = texture(Textures[17], TexCoord); break;
		case 18: texColor = texture(Textures[18], TexCoord); break;
		case 19: texColor = texture(Textures[19], TexCoord); break;
		case 20: texColor = texture(Textures[20], TexCoord); break;
		case 21: texColor = texture(Textures[21], TexCoord); break;
		case 22: texColor = texture(Textures[22], TexCoord); break;
		case 23: texColor = texture(Textures[23], TexCoord); break;
		case 24: texColor = texture(Textures[24], TexCoord); break;
		case 25: texColor = texture(Textures[25], TexCoord); break;
		case 26: texColor = texture(Textures[26], TexCoord); break;
		case 27: texColor = texture(Textures[27], TexCoord); break;
		case 28: texColor = texture(Textures[28], TexCoord); break;
		case 29: texColor = texture(Textures[29], TexCoord); break;
		case 30: texColor = texture(Textures[30], TexCoord); break;
	}

	if (texColor.a < 0.01)
		discard;

	FragColor = texColor * Color;
}