#pragma once
#include <ft2build.h>
#include FT_FREETYPE_H
#include <map>

#include "glm.hpp"
#include "Shader.h"

struct Character
{
	unsigned int TextureID; // ID handle of the glyph texture
	glm::ivec2 Size;        // Size of glyph
	glm::ivec2 Bearing;     // Offset from baseline to left/top of glyph
	unsigned int Advance;   // Offset to advance to next glyph
};

class Text
{
public:
	Text();

	void RenderText(Shader& s, std::string text, float x, float y, float scale, glm::vec3 color);

protected:
	unsigned int VAO, VBO;

	glm::mat4 _projection;
	std::map<GLchar, Character> Characters;
};
