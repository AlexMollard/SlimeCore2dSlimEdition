#pragma once

#include <map>
#include <string>
#include <vector>

// FreeType Headers
#include <ft2build.h>
#include FT_FREETYPE_H

#include "glm.hpp"
#include "Texture.h"

// Struct to hold information about a single character in the atlas
struct Character
{
	glm::vec2 Size;       // Size of glyph (width & height)
	glm::vec2 Bearing;    // Offset from baseline to left/top of glyph
	unsigned int Advance; // Offset to advance to next glyph

	// Texture Coordinates in the Atlas
	glm::vec2 uvMin; // Top-Left UV
	glm::vec2 uvMax; // Bottom-Right UV
};

class Font
{
public:
	// Constructor automatically loads the font and generates the atlas
	Font(const std::string& fontPath, unsigned int fontSize = 48);
	~Font();

	// Getters for Renderer
	Texture* GetAtlasTexture() const
	{
		return m_AtlasTexture;
	}

	const std::map<char, Character>& GetCharacters() const
	{
		return m_Characters;
	}

	unsigned int GetFontSize() const
	{
		return m_FontSize;
	}

	// Utility: Calculate the width/height of a string without rendering it
	glm::vec2 CalculateSize(const std::string& text, float scale, float wrapWidth = 0.0f);

	// Calculate text bounds with baseline offset info
	// Returns: (width, height, baselineOffset)
	// baselineOffset: how far above the baseline the text extends (maxY)
	glm::vec3 CalculateSizeWithBaseline(const std::string& text, float scale, float wrapWidth = 0.0f);

private:
	Texture* m_AtlasTexture = nullptr;
	std::map<char, Character> m_Characters;
	unsigned int m_FontSize;

	void GenerateAtlas(FT_Face face);
};
