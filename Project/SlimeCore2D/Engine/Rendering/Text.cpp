#include "Text.h"
#include "Core/Logger.h"

#include <algorithm>
#include <iostream>
#include <d3d11.h>

// Note: Ensure you are linking against FreeType 2.11+ for SDF support.
// If your FreeType is older, remove 'FT_RENDER_MODE_SDF' and standard aliasing will apply,
// though the shader logic in Renderer2D will need to know it's not an SDF.

Text::Text(const std::string& fontPath, unsigned int fontSize)
      : m_FontSize(fontSize)
{
	FT_Library ft;
	if (FT_Init_FreeType(&ft))
	{
		Logger::Error("ERROR::FREETYPE: Could not init FreeType Library");
		return;
	}

	FT_Face face;
	if (FT_New_Face(ft, fontPath.c_str(), 0, &face))
	{
		Logger::Error("ERROR::FREETYPE: Failed to load font: " + fontPath);
		FT_Done_FreeType(ft);
		return;
	}

	// Set size to load glyphs as.
	// For SDF, 48-64 is a good balance between quality and texture size.
	FT_Set_Pixel_Sizes(face, 0, fontSize);

	// Generate the Texture Atlas
	GenerateAtlas(face);

	// Cleanup
	FT_Done_Face(face);
	FT_Done_FreeType(ft);
}

Text::~Text()
{
	if (m_AtlasTexture)
	{
		delete m_AtlasTexture;
		m_AtlasTexture = nullptr;
	}
}

void Text::GenerateAtlas(FT_Face face)
{
	// 1. Setup Atlas Dimensions
	// 1024x1024 is generally large enough for standard ASCII + some extras at 48px
	const int atlasWidth = 1024;
	const int atlasHeight = 1024;

	// Buffer to hold texture data (R8 component only)
	std::vector<unsigned char> atlasData(atlasWidth * atlasHeight, 0);

	// Packing variables
	int xOffset = 0;
	int yOffset = 0;
	int rowHeight = 0;

	// 2. Load and Pack Characters (ASCII 32-126)
	// We iterate through printable characters
	for (unsigned char c = 32; c < 127; c++)
	{
		// Load glyph outline first (no render) then render with the best mode available.
		if (FT_Load_Char(face, c, FT_LOAD_DEFAULT))
		{
			Logger::Warn("ERROR::FREETYPE: Failed to load Glyph: " + std::to_string(c));
			continue;
		}

		// Prefer SDF if the FreeType build supports it, otherwise fall back to normal AA.
		FT_Render_Mode renderMode = FT_RENDER_MODE_NORMAL;
#ifdef FT_RENDER_MODE_SDF
		renderMode = FT_RENDER_MODE_SDF;
#endif

		if (FT_Render_Glyph(face->glyph, renderMode))
		{
			Logger::Warn("ERROR::FREETYPE: Failed to render Glyph: " + std::to_string(c));
			continue;
		}

		int width = face->glyph->bitmap.width;
		int height = face->glyph->bitmap.rows;

		// Check if we need to move to the next row
		// We add a 1 pixel padding to prevent texture bleeding
		if (xOffset + width + 1 > atlasWidth)
		{
			xOffset = 0;
			yOffset += rowHeight + 1; // +1 padding
			rowHeight = 0;
		}

		// Check if we ran out of space in the texture
		if (yOffset + height > atlasHeight)
		{
			Logger::Error("ERROR::TEXT: Font Atlas is too small for this font size!");
			break;
		}

		// Copy glyph bitmap into the atlas buffer
		for (int row = 0; row < height; ++row)
		{
			for (int col = 0; col < width; ++col)
			{
				// Source buffer is row-major
				unsigned char byte = face->glyph->bitmap.buffer[row * width + col];

				// Destination index in the large atlas
				int index = (yOffset + row) * atlasWidth + (xOffset + col);
				atlasData[index] = byte;
			}
		}

		// 3. Store Character Data
		Character character;
		character.Size = glm::vec2(width, height);
		character.Bearing = glm::vec2(face->glyph->bitmap_left, face->glyph->bitmap_top);
		character.Advance = static_cast<unsigned int>(face->glyph->advance.x);

		// Calculate Normalized UV coordinates (0.0 to 1.0)
		// Note: FreeType renders top-down.
		// We map straightforwardly here, and the Renderer handles the quad vertex UV alignment.

		float uMin = (float) xOffset / atlasWidth;
		float vMin = (float) yOffset / atlasHeight;
		float uMax = (float) (xOffset + width) / atlasWidth;
		float vMax = (float) (yOffset + height) / atlasHeight;

		// Store Top-Left and Bottom-Right UVs
		character.uvMin = glm::vec2(uMin, vMin);
		character.uvMax = glm::vec2(uMax, vMax);

		m_Characters.insert(std::pair<char, Character>(c, character));

		// Update packing cursor
		rowHeight = std::max(rowHeight, height);
		xOffset += width + 1; // +1 padding
	}

	// 4. Create Texture
	// Create and Upload Texture using the new Class
	m_AtlasTexture = new Texture(atlasWidth, atlasHeight, DXGI_FORMAT_R8_UNORM, Texture::Filter::Linear, Texture::Wrap::ClampToEdge);
	
	// Upload the raw bitmap data we generated into the texture
	m_AtlasTexture->SetData(atlasData.data(), (uint32_t)atlasData.size());
}

glm::vec2 Text::CalculateSize(const std::string& text, float scale)
{
	glm::vec3 result = CalculateSizeWithBaseline(text, scale);
	return glm::vec2(result.x, result.y);
}

glm::vec3 Text::CalculateSizeWithBaseline(const std::string& text, float scale)
{
	glm::vec3 result(0.0f);
	float x = 0.0f;
	float maxY = 0.0f;
	float minY = 0.0f;

	std::string::const_iterator c;
	for (c = text.begin(); c != text.end(); c++)
	{
		Character ch = m_Characters[*c];

		float h = ch.Size.y * scale;
		float bearingY = ch.Bearing.y * scale;

		// Track height bounds
		if (bearingY > maxY)
			maxY = bearingY;
		if ((h - bearingY) > minY)
			minY = (h - bearingY);

		x += (ch.Advance >> 6) * scale;
	}

	result.x = x; // width
	result.y = maxY + minY; // total height
	result.z = maxY; // baseline offset (how far above baseline)

	return result;
}
