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

	// Create an RGBA GL texture containing `text` rendered at `pixelHeight` pixels high.
	// Returns GL texture id (0 on failure). outW/outH set on success.
	static unsigned int CreateTextureFromString(const std::string& fontPath, const std::string& text, int pixelHeight, int& outW, int& outH);

	// Font handle API: load a font into memory for repeated rendering operations
	struct FontHandle;
	static FontHandle* LoadFontFromFile(const std::string& path);
	static void FreeFont(FontHandle* f);
	static unsigned int CreateTextureFromLoadedFont(FontHandle* f, const std::string& text, int pixelHeight, int& outW, int& outH);

protected:
	unsigned int VAO, VBO;

	glm::mat4 m_projection;
	std::map<GLchar, Character> m_characters;
};
