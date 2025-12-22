#include "Text.h"

#include <algorithm>
#include <iostream>

#include "glew.h"

// Note: Ensure you are linking against FreeType 2.11+ for SDF support.
// If your FreeType is older, remove 'FT_RENDER_MODE_SDF' and standard aliasing will apply,
// though the shader logic in Renderer2D will need to know it's not an SDF.

Text::Text(const std::string& fontPath, unsigned int fontSize)
      : m_FontSize(fontSize)
{
	FT_Library ft;
	if (FT_Init_FreeType(&ft))
	{
		std::cout << "ERROR::FREETYPE: Could not init FreeType Library" << std::endl;
		return;
	}

	FT_Face face;
	if (FT_New_Face(ft, fontPath.c_str(), 0, &face))
	{
		std::cout << "ERROR::FREETYPE: Failed to load font: " << fontPath << std::endl;
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

	// Buffer to hold texture data (GL_RED component only)
	std::vector<unsigned char> atlasData(atlasWidth * atlasHeight, 0);

	// Packing variables
	int xOffset = 0;
	int yOffset = 0;
	int rowHeight = 0;

	// Disable byte-alignment restriction
	glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

	// 2. Load and Pack Characters (ASCII 32-126)
	// We iterate through printable characters
	for (unsigned char c = 32; c < 127; c++)
	{
		// Load character glyph with SDF Rendering
		// If FT_RENDER_MODE_SDF is not defined, update your FreeType library or use FT_LOAD_RENDER
		if (FT_Load_Char(face, c, FT_LOAD_RENDER | FT_RENDER_MODE_SDF))
		{
			std::cout << "ERROR::FREETYPE: Failed to load Glyph: " << c << std::endl;
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
			std::cout << "ERROR::TEXT: Font Atlas is too small for this font size!" << std::endl;
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
		// Note: FreeType renders top-down. OpenGL textures are usually bottom-up,
		// but since we upload the buffer directly, the visual data is "flipped" relative to GL's 0,0 bottom-left.
		// However, standard mapping usually assumes (0,0) is top-left of the image data in memory.
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

	// 4. Create OpenGL Texture
	unsigned int textureID;
	glGenTextures(1, &textureID);
	glBindTexture(GL_TEXTURE_2D, textureID);

	// Load data
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RED, atlasWidth, atlasHeight, 0, GL_RED, GL_UNSIGNED_BYTE, atlasData.data());

	// Set options
	// CLAMP_TO_EDGE is important to prevent artifacts at edges of the quad
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

	// SDF requires Linear filtering to allow the shader to interpolate distances
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

	// Create and Upload Texture using the new Class
	m_AtlasTexture = new Texture(atlasWidth, atlasHeight, GL_R8, Texture::Filter::Linear, Texture::Wrap::ClampToEdge);
	
	// Upload the raw bitmap data we generated into the texture
	m_AtlasTexture->SetData(atlasData.data(), atlasData.size());

	glBindTexture(GL_TEXTURE_2D, 0);
}

glm::vec2 Text::CalculateSize(const std::string& text, float scale)
{
	glm::vec2 size(0.0f);
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

	size.x = x;
	size.y = maxY + minY; // Approximate height spanning top bearing to bottom descender

	return size;
}
