#include "Text.h"

#include <iostream>

#include <GL/glew.h>
#include <GLFW/glfw3.h>
#include <glm/glm.hpp> 
#define GLM_ENABLE_EXPERIMENTAL
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
#include <glm/gtx/transform.hpp>
#include "Renderer2D.h"
#include "Engine/Resources/ResourceManager.h"

Text::Text()
{
	// FreeType
	// --------
	FT_Library ft;
	// All functions return a value different than 0 whenever an error occurred
	if (FT_Init_FreeType(&ft))
	{
		std::cout << "ERROR::FREETYPE: Could not init FreeType Library" << std::endl;
		return;
	}

	// find path to font
	std::string font_name = ResourceManager::GetInstance().GetResourcePath("Fonts\\Chilanka-Regular.ttf");
	if (font_name.empty())
	{
		font_name = "..\\Fonts\\Chilanka-Regular.ttf";
	}

	// load font as face
	FT_Face face;
	if (FT_New_Face(ft, font_name.c_str(), 0, &face))
	{
		std::cout << "ERROR::FREETYPE: Failed to load font: " << font_name << std::endl;
		return;
	}
	else
	{
		// set size to load glyphs as
		FT_Set_Pixel_Sizes(face, 0, 48);

		// disable byte-alignment restriction
		glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

		// load first 128 characters of ASCII set
		for (unsigned char c = 0; c < 128; c++)
		{
			// Load character glyph
			if (FT_Load_Char(face, c, FT_LOAD_RENDER))
			{
				std::cout << "ERROR::FREETYTPE: Failed to load Glyph" << std::endl;
				continue;
			}
			// generate texture
			unsigned int texture;
			glGenTextures(1, &texture);
			glBindTexture(GL_TEXTURE_2D, texture);
			glTexImage2D(GL_TEXTURE_2D, 0, GL_RED, face->glyph->bitmap.width, face->glyph->bitmap.rows, 0, GL_RED, GL_UNSIGNED_BYTE, face->glyph->bitmap.buffer);
			// set texture options
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
			// now store character for later use
			Character character = { texture, glm::ivec2(face->glyph->bitmap.width, face->glyph->bitmap.rows), glm::ivec2(face->glyph->bitmap_left, face->glyph->bitmap_top), static_cast<unsigned int>(face->glyph->advance.x) };
			m_characters.insert(std::pair<char, Character>(c, character));
		}
		glBindTexture(GL_TEXTURE_2D, 0);
	}
	// destroy FreeType once we're finished
	FT_Done_Face(face);
	FT_Done_FreeType(ft);

	// configure VAO/VBO for texture quads
	// -----------------------------------
	glGenVertexArrays(1, &VAO);
	glGenBuffers(1, &VBO);
	glBindVertexArray(VAO);
	glBindBuffer(GL_ARRAY_BUFFER, VBO);
	glBufferData(GL_ARRAY_BUFFER, sizeof(float) * 6 * 4, NULL, GL_DYNAMIC_DRAW);
	glEnableVertexAttribArray(0);
	glVertexAttribPointer(0, 4, GL_FLOAT, GL_FALSE, 4 * sizeof(float), 0);
	glBindBuffer(GL_ARRAY_BUFFER, 0);
	glBindVertexArray(0);
}

void Text::RenderText(Shader& shader, std::string text, float x, float y, float scale, glm::vec3 color)
{
	// activate corresponding render state
	shader.Use();
	// ensure the sampler uses texture unit 0
	shader.setInt("text", 0);

	glm::mat4 projection = glm::ortho(0.0f, static_cast<float>(1920), 0.0f, static_cast<float>(1080));
	shader.setMat4("projection", projection);
	shader.setVec3("textColor", glm::vec3(color.x, color.y, color.z));

	glActiveTexture(GL_TEXTURE0);
	glBindVertexArray(VAO);

	// iterate through all characters
	std::string::const_iterator c;
	for (c = text.begin(); c != text.end(); c++)
	{
		Character ch = m_characters[*c];

		float xpos = x + ch.Bearing.x * scale;
		float ypos = y - (ch.Size.y - ch.Bearing.y) * scale;

		float w = ch.Size.x * scale;
		float h = ch.Size.y * scale;
		// update VBO for each character
		float vertices[6][4] = {
			{     xpos, ypos + h, 0.0f, 0.0f },
			{     xpos,     ypos, 0.0f, 1.0f },
			{ xpos + w,     ypos, 1.0f, 1.0f },

			{     xpos, ypos + h, 0.0f, 0.0f },
			{ xpos + w,     ypos, 1.0f, 1.0f },
			{ xpos + w, ypos + h, 1.0f, 0.0f }
		};
		// render glyph texture over quad
		glBindTexture(GL_TEXTURE_2D, ch.TextureID);
		// update content of VBO memory
		glBindBuffer(GL_ARRAY_BUFFER, VBO);
		glBufferSubData(GL_ARRAY_BUFFER, 0, sizeof(vertices), vertices); // be sure to use glBufferSubData and not glBufferData

		glBindBuffer(GL_ARRAY_BUFFER, 0);
		// render quad
		glDrawArrays(GL_TRIANGLES, 0, 6);
		// now advance cursors for next glyph (note that advance is number of 1/64 pixels)
		x += (ch.Advance >> 6) * scale; // bitshift by 6 to get value in pixels (2^6 = 64 (divide amount of 1/64th pixels by 64 to get amount of pixels))
	}
	glBindVertexArray(0);
	glBindTexture(GL_TEXTURE_2D, 0);
}

// Helper: create a white (255,255,255) RGBA texture with glyph alpha composed into alpha channel
unsigned int Text::CreateTextureFromString(const std::string& fontPath, const std::string& text, int pixelHeight, int& outW, int& outH)
{
	if (text.empty() || pixelHeight <= 0)
		return 0;

	// This implementation delegates to a memory-based font load to reuse the same code path
	FontHandle* fh = LoadFontFromFile(fontPath);
	if (!fh) return 0;

	int w = 0, h = 0;
	unsigned int tex = CreateTextureFromLoadedFont(fh, text, pixelHeight, w, h);
	outW = w; outH = h;
	FreeFont(fh);
	return tex;
}

// FontHandle implementation
struct Text::FontHandle
{
	std::vector<unsigned char> buffer;
	FT_Library ft = nullptr;
	FT_Face face = nullptr;
	bool valid = false;
};

Text::FontHandle* Text::LoadFontFromFile(const std::string& path)
{
	// read file
	FILE* f = nullptr;
#if defined(_MSC_VER)
	if (fopen_s(&f, path.c_str(), "rb") != 0 || !f) return nullptr;
#else
	f = std::fopen(path.c_str(), "rb");
	if (!f) return nullptr;
#endif
	std::fseek(f, 0, SEEK_END);
	long sz = std::ftell(f);
	std::fseek(f, 0, SEEK_SET);
	if (sz <= 0) { std::fclose(f); return nullptr; }

	Text::FontHandle* fh = new Text::FontHandle();
	fh->buffer.resize(sz);
	if (std::fread(fh->buffer.data(), 1, sz, f) != (size_t)sz) { std::fclose(f); delete fh; return nullptr; }
	std::fclose(f);

	if (FT_Init_FreeType(&fh->ft)) { delete fh; return nullptr; }
	if (FT_New_Memory_Face(fh->ft, fh->buffer.data(), (FT_Long)fh->buffer.size(), 0, &fh->face)) { FT_Done_FreeType(fh->ft); delete fh; return nullptr; }

	fh->valid = true;
	return fh;
}

void Text::FreeFont(Text::FontHandle* f)
{
	if (!f) return;
	if (f->face) FT_Done_Face(f->face);
	if (f->ft) FT_Done_FreeType(f->ft);
	delete f;
}

unsigned int Text::CreateTextureFromLoadedFont(Text::FontHandle* f, const std::string& text, int pixelHeight, int& outW, int& outH)
{
	if (!f || !f->valid || text.empty() || pixelHeight <= 0) return 0;

	FT_Face face = f->face;

	// Generate at a higher internal resolution to improve quality when the
	// texture is later sampled and scaled down on-screen (e.g. 2x)
	const int HIGHRES_SCALE = 2; // set to 3 for even higher detail
	int genPixelHeight = pixelHeight * HIGHRES_SCALE;
	FT_Set_Pixel_Sizes(face, 0, genPixelHeight);

	int width = 0;
	int ascent = 0, descent = 0;

	for (const unsigned char* p = (const unsigned char*)text.c_str(); *p; )
	{
		int codepoint = 0;
		if (*p < 0x80) { codepoint = *p; ++p; }
		else { codepoint = '?'; ++p; }
		if (FT_Load_Char(face, codepoint, FT_LOAD_RENDER)) continue;
		width += (face->glyph->advance.x >> 6);
	}

	FT_Fixed a = face->size->metrics.ascender;
	FT_Fixed d = face->size->metrics.descender;
	ascent = (a >> 6);
	descent = (-(d >> 6));

	width = std::max(1, width);
	int height = std::max(1, ascent + descent);

	// The generated texture is HIGHRES_SCALE times larger in each dimension. Report
	// logical (requested) pixel size back to callers so UI layout doesn't need to
	// change: outW/outH are in the requested pixel coordinate space.
	outW = width / HIGHRES_SCALE; outH = height / HIGHRES_SCALE;

	std::vector<unsigned char> alpha(width * height);
	std::memset(alpha.data(), 0, alpha.size());

	int penX = 0;
	int baseline = ascent;

	for (const unsigned char* p = (const unsigned char*)text.c_str(); *p; )
	{
		int codepoint = 0;
		if (*p < 0x80) { codepoint = *p; ++p; }
		else { codepoint = '?'; ++p; }

		if (FT_Load_Char(face, codepoint, FT_LOAD_RENDER)) continue;

		FT_GlyphSlot g = face->glyph;
		int gbW = g->bitmap.width;
		int gbH = g->bitmap.rows;
		int gbX = g->bitmap_left;
		int gbY = -g->bitmap_top;

		int x0 = penX + gbX;
		int y0 = baseline + gbY;

		for (int gy = 0; gy < gbH; ++gy)
		{
			for (int gx = 0; gx < gbW; ++gx)
			{
				int tx = x0 + gx;
				int ty = y0 + gy;
				if (tx >= 0 && tx < width && ty >= 0 && ty < height)
				{
					unsigned char v = g->bitmap.buffer[gy * gbW + gx];
					alpha[ty * width + tx] = std::max(alpha[ty * width + tx], v);
				}
			}
		}

		penX += (g->advance.x >> 6);
	}

	std::vector<unsigned char> rgba(width * height * 4);
	for (int i = 0; i < width * height; ++i)
	{
		rgba[i * 4 + 0] = 255;
		rgba[i * 4 + 1] = 255;
		rgba[i * 4 + 2] = 255;
		rgba[i * 4 + 3] = alpha[i];
	}

	GLuint tex;
	glGenTextures(1, &tex);
	glBindTexture(GL_TEXTURE_2D, tex);
	// Use trilinear filtering and generate mipmaps for better downscaling
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, rgba.data());
	glGenerateMipmap(GL_TEXTURE_2D);

	return (unsigned int)tex;
}
