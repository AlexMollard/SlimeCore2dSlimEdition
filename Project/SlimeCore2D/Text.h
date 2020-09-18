#pragma once
#include "glm.hpp"
#include <map>
#include "Shader.h"
#include <ft2build.h>
#include FT_FREETYPE_H

struct Character
{
	unsigned int TextureID;  // ID handle of the glyph texture
	glm::ivec2   Size;       // Size of glyph
	glm::ivec2   Bearing;    // Offset from baseline to left/top of glyph
	unsigned int Advance;    // Offset to advance to next glyph
};

class Text
{
public:
	Text(); 
	
	void RenderText(Shader& s, std::string text, float x, float y, float scale, glm::vec3 colour);
protected:
	unsigned int VAO, VBO; 
	FT_Library _ftLib;
	FT_Face _ftFace; 

	std::map<char, Character> _characters; 
};

