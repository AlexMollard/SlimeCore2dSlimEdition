#include "Text.h"
#include <iostream>
#include "glfw3.h"
#include "glew.h"
#include "Renderer2D.h"

Text::Text()
{
	if (FT_Init_FreeType(&_ftLib))
		std::cout << "ERROR::FREETYPE: Could not init FreeType Library" << std::endl;


	if (FT_New_Face(_ftLib, "font/Cabin-Regular.ttf", 0, &_ftFace))
		std::cout << "ERROR::FREETYPE: Faild to load font" << std::endl;

	FT_Set_Pixel_Sizes(_ftFace, 0, 48);

	if (FT_Load_Char(_ftFace, 'X', FT_LOAD_RENDER))
		std::cout << "ERROR::FREETYPE: Failed to load Glyph" << std::endl;

	glPixelStorei(GL_UNPACK_ALIGNMENT, 1); //Disable byte-alighment restirction

	for (unsigned char c = 0; c < 128; c++)
	{
		// load character glyph 
		if (FT_Load_Char((FT_Face)_ftFace, c, FT_LOAD_RENDER))
		{
			std::cout << "ERROR::FREETYTPE: Failed to load Glyph" << std::endl;
			continue;
		}
		// generate texture
		unsigned int texture;
		glGenTextures(1, &texture);
		glBindTexture(GL_TEXTURE_2D, texture);
		glTexImage2D(
			GL_TEXTURE_2D,
			0,
			GL_RED,
			_ftFace->glyph->bitmap.width,
			_ftFace->glyph->bitmap.rows,
			0,
			GL_RED,
			GL_UNSIGNED_BYTE,
			_ftFace->glyph->bitmap.buffer
		);
		// set texture options
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		// now store character for later use
		Character character = {
			texture,
			glm::ivec2(_ftFace->glyph->bitmap.width, _ftFace->glyph->bitmap.rows),
			glm::ivec2(_ftFace->glyph->bitmap_left, _ftFace->glyph->bitmap_top),
			_ftFace->glyph->advance.x
		};
		_characters.insert(std::pair<char, Character>(c, character));

		glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

		FT_Done_Face(_ftFace);
		FT_Done_FreeType(_ftLib);
	}
}

void Text::RenderText(Shader& s, std::string text, float x, float y, float scale, glm::vec3 colour)
{
	// activate corresponding render state	
	s.Use();
	s.setVec3("textColour", glm::vec3(colour.x, colour.y, colour.z)); 
	glActiveTexture(GL_TEXTURE0);
	glBindVertexArray(VAO);

	// iterate through all characters
	std::string::const_iterator c;
	for (c = text.begin(); c != text.end(); c++)
	{
		Character ch = _characters[*c];

		float xpos = x + ch.Bearing.x * scale;
		float ypos = y - (ch.Size.y - ch.Bearing.y) * scale;

		float w = ch.Size.x * scale;
		float h = ch.Size.y * scale;
		// update VBO for each character
		float vertices[6][4] = {
			{ xpos,     ypos + h,   0.0f, 0.0f },
			{ xpos,     ypos,       0.0f, 1.0f },
			{ xpos + w, ypos,       1.0f, 1.0f },

			{ xpos,     ypos + h,   0.0f, 0.0f },
			{ xpos + w, ypos,       1.0f, 1.0f },
			{ xpos + w, ypos + h,   1.0f, 0.0f }
		};
		// render glyph texture over quad
		glBindTexture(GL_TEXTURE_2D, ch.textureID);
		// update content of VBO memory
		glBindBuffer(GL_ARRAY_BUFFER, VBO);
		glBufferSubData(GL_ARRAY_BUFFER, 0, sizeof(vertices), vertices);
		glBindBuffer(GL_ARRAY_BUFFER, 0);
		// render quad
		glDrawArrays(GL_TRIANGLES, 0, 6);
		// now advance cursors for next glyph (note that advance is number of 1/64 pixels)
		x += (ch.Advance >> 6) * scale; // bitshift by 6 to get value in pixels (2^6 = 64)
	}
	glBindVertexArray(0);
	glBindTexture(GL_TEXTURE_2D, 0);
}