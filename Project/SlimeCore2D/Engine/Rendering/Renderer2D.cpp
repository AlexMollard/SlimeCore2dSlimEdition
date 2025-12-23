#include "Renderer2D.h"

#include <array>
#include <gtc/matrix_transform.hpp>
#include <iostream>

#include "glew.h"
#include "Resources/ResourceManager.h"
#include "Text.h" // Must include Text header to access font data

// Define the static data instance
Renderer2D::Renderer2DData Renderer2D::s_Data;

void Renderer2D::Init()
{
	s_Data.QuadBuffer = new Renderer2DData::QuadVertex[s_Data.MaxVertices];

	glCreateVertexArrays(1, &s_Data.QuadVA);
	glBindVertexArray(s_Data.QuadVA);

	glCreateBuffers(1, &s_Data.QuadVB);
	glBindBuffer(GL_ARRAY_BUFFER, s_Data.QuadVB);
	glBufferData(GL_ARRAY_BUFFER, s_Data.MaxVertices * sizeof(Renderer2DData::QuadVertex), nullptr, GL_DYNAMIC_DRAW);

	// Enable Attributes
	// 0: Position
	glEnableVertexAttribArray(0);
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, sizeof(Renderer2DData::QuadVertex), (const void*) offsetof(Renderer2DData::QuadVertex, Position));

	// 1: Color
	glEnableVertexAttribArray(1);
	glVertexAttribPointer(1, 4, GL_FLOAT, GL_FALSE, sizeof(Renderer2DData::QuadVertex), (const void*) offsetof(Renderer2DData::QuadVertex, Color));

	// 2: TexCoord
	glEnableVertexAttribArray(2);
	glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, sizeof(Renderer2DData::QuadVertex), (const void*) offsetof(Renderer2DData::QuadVertex, TexCoord));

	// 3: TexIndex
	glEnableVertexAttribArray(3);
	glVertexAttribPointer(3, 1, GL_FLOAT, GL_FALSE, sizeof(Renderer2DData::QuadVertex), (const void*) offsetof(Renderer2DData::QuadVertex, TexIndex));

	// 4: TilingFactor
	glEnableVertexAttribArray(4);
	glVertexAttribPointer(4, 1, GL_FLOAT, GL_FALSE, sizeof(Renderer2DData::QuadVertex), (const void*) offsetof(Renderer2DData::QuadVertex, TilingFactor));

	// 5: IsText (For SDF switching in shader)
	glEnableVertexAttribArray(5);
	glVertexAttribPointer(5, 1, GL_FLOAT, GL_FALSE, sizeof(Renderer2DData::QuadVertex), (const void*) offsetof(Renderer2DData::QuadVertex, IsText));

	// Indices
	uint32_t* quadIndices = new uint32_t[s_Data.MaxIndices];
	uint32_t offset = 0;
	for (uint32_t i = 0; i < s_Data.MaxIndices; i += 6)
	{
		quadIndices[i + 0] = offset + 0;
		quadIndices[i + 1] = offset + 1;
		quadIndices[i + 2] = offset + 2;

		quadIndices[i + 3] = offset + 2;
		quadIndices[i + 4] = offset + 3;
		quadIndices[i + 5] = offset + 0;

		offset += 4;
	}

	glCreateBuffers(1, &s_Data.QuadIB);
	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, s_Data.QuadIB);
	glBufferData(GL_ELEMENT_ARRAY_BUFFER, s_Data.MaxIndices * sizeof(uint32_t), quadIndices, GL_STATIC_DRAW);

	delete[] quadIndices;

	// White Texture (1x1)
	glCreateTextures(GL_TEXTURE_2D, 1, &s_Data.WhiteTexture);
	glBindTexture(GL_TEXTURE_2D, s_Data.WhiteTexture);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	uint32_t color = 0xffffffff;
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, 1, 1, 0, GL_RGBA, GL_UNSIGNED_BYTE, &color);

	s_Data.TextureSlots[0] = s_Data.WhiteTexture;
	for (size_t i = 1; i < s_Data.MaxTextureSlots; i++)
		s_Data.TextureSlots[i] = 0;

	ResourceManager::GetInstance().LoadShadersFromDir();
	s_Data.TextureShader = ResourceManager::GetInstance().GetShader("basic");
	if (!s_Data.TextureShader)
	{
		std::cout << "Renderer2D Warning: 'BatchShader' not found. Make sure to load a shader capable of batching." << std::endl;
	}

	// Helper for rotation
	s_Data.QuadVertexPositions[0] = { -0.5f, -0.5f, 0.0f, 1.0f };
	s_Data.QuadVertexPositions[1] = { 0.5f, -0.5f, 0.0f, 1.0f };
	s_Data.QuadVertexPositions[2] = { 0.5f, 0.5f, 0.0f, 1.0f };
	s_Data.QuadVertexPositions[3] = { -0.5f, 0.5f, 0.0f, 1.0f };

	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

	glEnable(GL_DEPTH_TEST);
	glDepthFunc(GL_LEQUAL);
}

void Renderer2D::Shutdown()
{
	delete[] s_Data.QuadBuffer;
	glDeleteVertexArrays(1, &s_Data.QuadVA);
	glDeleteBuffers(1, &s_Data.QuadVB);
	glDeleteBuffers(1, &s_Data.QuadIB);
	glDeleteTextures(1, &s_Data.WhiteTexture);
}

void Renderer2D::BeginScene(Camera& camera)
{
	if (s_Data.TextureShader)
	{
		s_Data.TextureShader->Use();
		// Assuming Camera has GetViewProjection or similar.
		// If you only have GetTransform() (Ortho), use that.
		s_Data.TextureShader->setMat4("u_ViewProjection", camera.GetProjectionMatrix());
	}
	StartBatch();
}

void Renderer2D::BeginScene(const glm::mat4& viewProj)
{
	if (s_Data.TextureShader)
	{
		s_Data.TextureShader->Use();
		s_Data.TextureShader->setMat4("u_ViewProjection", viewProj);
	}
	StartBatch();
}

void Renderer2D::EndScene()
{
	Flush();
}

void Renderer2D::StartBatch()
{
	s_Data.IndexCount = 0;
	s_Data.QuadBufferPtr = s_Data.QuadBuffer;
	s_Data.TextureSlotIndex = 1;
}

void Renderer2D::Flush()
{
	if (s_Data.IndexCount == 0)
		return;

	uint32_t dataSize = (uint32_t) ((uint8_t*) s_Data.QuadBufferPtr - (uint8_t*) s_Data.QuadBuffer);
	glBindBuffer(GL_ARRAY_BUFFER, s_Data.QuadVB);
	glBufferSubData(GL_ARRAY_BUFFER, 0, dataSize, s_Data.QuadBuffer);

	// Bind Textures
	for (uint32_t i = 0; i < s_Data.TextureSlotIndex; i++)
	{
		glActiveTexture(GL_TEXTURE0 + i);
		glBindTexture(GL_TEXTURE_2D, s_Data.TextureSlots[i]);
	}

	// Update Uniforms (sampler array)
	// Usually done once on Shader Init, but if shader changes ensure this is set.
	int samplers[32];
	for (int i = 0; i < 32; i++)
	{
		samplers[i] = i;
	}

	s_Data.TextureShader->setIntArray("u_Textures", samplers, 32);

	glBindVertexArray(s_Data.QuadVA);
	glDrawElements(GL_TRIANGLES, s_Data.IndexCount, GL_UNSIGNED_INT, nullptr);

	s_Data.Stats.DrawCalls++;
	s_Data.Stats.VertexCount += s_Data.IndexCount / 6 * 4;
	s_Data.Stats.IndexCount += s_Data.IndexCount;
}

void Renderer2D::NextBatch()
{
	Flush();
	StartBatch();
}

// -------------------------------------------------------------------------
// Drawing Primitives
// -------------------------------------------------------------------------

void Renderer2D::DrawQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color)
{
	DrawQuad({ position.x, position.y, 0.0f }, size, color);
}

void Renderer2D::DrawQuad(const glm::vec3& position, const glm::vec2& size, const glm::vec4& color)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices)
		NextBatch();

	const float texIndex = 0.0f; // White Texture
	const float tilingFactor = 1.0f;
	const glm::vec2 texCoords[] = {
		{ 0.0f, 0.0f },
        { 1.0f, 0.0f },
        { 1.0f, 1.0f },
        { 0.0f, 1.0f }
	};

	glm::vec3 transformPos;
	// Center-based anchor logic
	// BL, BR, TR, TL
	glm::vec3 offsets[4] = {
		{ -0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x,  0.5f * size.y, 0.0f },
        { -0.5f * size.x,  0.5f * size.y, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = position + offsets[i];
		s_Data.QuadBufferPtr->Color = color;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = texIndex;
		s_Data.QuadBufferPtr->TilingFactor = tilingFactor;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::vec2& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	DrawQuad({ position.x, position.y, 0.0f }, size, texture, tiling, tintColor);
}

void Renderer2D::DrawQuad(const glm::vec3& position, const glm::vec2& size, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == texture->GetID())
		{
			textureIndex = (float) i;
			break;
		}
	}

	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = texture->GetID();
		s_Data.TextureSlotIndex++;
	}

	glm::vec2 texCoords[] = {
		{ 0.0f, 0.0f },
        { 1.0f, 0.0f },
        { 1.0f, 1.0f },
        { 0.0f, 1.0f }
	};

	// Assuming anchor is center
	glm::vec3 offsets[4] = {
		{ -0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x,  0.5f * size.y, 0.0f },
        { -0.5f * size.x,  0.5f * size.y, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = position + offsets[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawRotatedQuad(const glm::vec2& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
	DrawRotatedQuad({ position.x, position.y, 0.0f }, size, rotation, color);
}

void Renderer2D::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, const glm::vec4& color)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices)
		NextBatch();

	const float texIndex = 0.0f;
	const float tiling = 1.0f;
	const glm::vec2 texCoords[] = {
		{ 0.0f, 0.0f },
        { 1.0f, 0.0f },
        { 1.0f, 1.0f },
        { 0.0f, 1.0f }
	};

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::rotate(glm::mat4(1.0f), rotation, { 0.0f, 0.0f, 1.0f }) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = color;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = texIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawRotatedQuad(const glm::vec3& position, const glm::vec2& size, float rotation, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == texture->GetID())
		{
			textureIndex = (float) i;
			break;
		}
	}

	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = texture->GetID();
		s_Data.TextureSlotIndex++;
	}

	glm::mat4 transform = glm::translate(glm::mat4(1.0f), position) * glm::rotate(glm::mat4(1.0f), rotation, { 0.0f, 0.0f, 1.0f }) * glm::scale(glm::mat4(1.0f), { size.x, size.y, 1.0f });

	const glm::vec2 texCoords[] = {
		{ 0.0f, 0.0f },
        { 1.0f, 0.0f },
        { 1.0f, 1.0f },
        { 0.0f, 1.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::mat4& transform, const glm::vec4& color)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices)
		NextBatch();

	const float texIndex = 0.0f;
	const float tiling = 1.0f;
	const glm::vec2 texCoords[] = {
		{ 0.0f, 0.0f },
        { 1.0f, 0.0f },
        { 1.0f, 1.0f },
        { 0.0f, 1.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = color;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = texIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuad(const glm::mat4& transform, Texture* texture, float tiling, const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == texture->GetID())
		{
			textureIndex = (float) i;
			break;
		}
	}

	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = texture->GetID();
		s_Data.TextureSlotIndex++;
	}

	const glm::vec2 texCoords[] = {
		{ 0.0f, 0.0f },
        { 1.0f, 0.0f },
        { 1.0f, 1.0f },
        { 0.0f, 1.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = texCoords[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = tiling;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuadUV(const glm::mat4& transform, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == texture->GetID())
		{
			textureIndex = (float) i;
			break;
		}
	}
	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = texture->GetID();
		s_Data.TextureSlotIndex++;
	}

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = transform * s_Data.QuadVertexPositions[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = uv[i];
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = 1.0f;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

void Renderer2D::DrawQuadUV(const glm::vec3& position, const glm::vec2& size, Texture* texture, const glm::vec2 uv[], const glm::vec4& tintColor)
{
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == texture->GetID())
		{
			textureIndex = (float) i;
			break;
		}
	}
	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = texture->GetID();
		s_Data.TextureSlotIndex++;
	}

	// Assuming UVs are passed as { BL, BR, TR, TL }
	glm::vec3 offsets[4] = {
		{ -0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x, -0.5f * size.y, 0.0f },
        {  0.5f * size.x,  0.5f * size.y, 0.0f },
        { -0.5f * size.x,  0.5f * size.y, 0.0f }
	};

	for (int i = 0; i < 4; i++)
	{
		s_Data.QuadBufferPtr->Position = position + offsets[i];
		s_Data.QuadBufferPtr->Color = tintColor;
		s_Data.QuadBufferPtr->TexCoord = uv[i]; // Use explicit UV
		s_Data.QuadBufferPtr->TexIndex = textureIndex;
		s_Data.QuadBufferPtr->TilingFactor = 1.0f;
		s_Data.QuadBufferPtr->IsText = 0.0f;
		s_Data.QuadBufferPtr++;
	}

	s_Data.IndexCount += 6;
	s_Data.Stats.QuadCount++;
}

// -------------------------------------------------------------------------
// Text Rendering (SDF)
// -------------------------------------------------------------------------

void Renderer2D::DrawString(const std::string& text, Text* font, const glm::vec2& position, float scale, const glm::vec4& color)
{
	DrawString(text, font, { position.x, position.y, 0.0f }, scale, color);
}

void Renderer2D::DrawString(const std::string& text, Text* font, const glm::vec3& position, float scale, const glm::vec4& color)
{
	// Assuming Text class has GetAtlasTexture() and GetCharacters() map
	// You need to ensure your Text class has these accessors.
	Texture* atlas = font->GetAtlasTexture();
	auto& characters = font->GetCharacters();

	// Check texture slot availability
	if (s_Data.IndexCount >= s_Data.MaxIndices || s_Data.TextureSlotIndex > 31)
		NextBatch();

	float textureIndex = 0.0f;
	for (uint32_t i = 1; i < s_Data.TextureSlotIndex; i++)
	{
		if (s_Data.TextureSlots[i] == atlas->GetID())
		{
			textureIndex = (float) i;
			break;
		}
	}
	if (textureIndex == 0.0f)
	{
		if (s_Data.TextureSlotIndex >= s_Data.MaxTextureSlots)
			NextBatch();
		textureIndex = (float) s_Data.TextureSlotIndex;
		s_Data.TextureSlots[s_Data.TextureSlotIndex] = atlas->GetID();
		s_Data.TextureSlotIndex++;
	}

	float x = position.x;
	float y = position.y;
	float z = position.z;

	std::string::const_iterator c;
	for (c = text.begin(); c != text.end(); c++)
	{
		auto it = characters.find(*c);

		if (it == characters.end())
			continue; // Character not found in font atlas, skip it

		Character ch = it->second;

		float xpos = x + ch.Bearing.x * scale;
		float ypos = y - (ch.Size.y - ch.Bearing.y) * scale;
		float w = ch.Size.x * scale;
		float h = ch.Size.y * scale;

		// Skip spaces (no texture area) but advance X
		if (w > 0.0f && h > 0.0f)
		{
			// Check batch limits per character
			if (s_Data.IndexCount >= s_Data.MaxIndices)
				NextBatch();

			// Font Atlas UVs are typically Top-Left to Bottom-Right in data,
			// but we need to map to Quad vertices { BL, BR, TR, TL }
			// Assuming Character struct has uvMin (Top-Left in texture) and uvMax (Bottom-Right)
			// GL coords: V=0 is bottom, V=1 is top. FreeType is Top-Down.
			// Your Atlas UVs should be pre-calculated correctly for GL.

			// If stored as: uvMin = (0,0) [TopLeft], uvMax = (1,1) [BotRight]
			// BL = min.x, max.y
			// BR = max.x, max.y
			// TR = max.x, min.y
			// TL = min.x, min.y

			glm::vec2 uvs[4] = {
				{ ch.uvMin.x, ch.uvMax.y }, // BL
				{ ch.uvMax.x, ch.uvMax.y }, // BR
				{ ch.uvMax.x, ch.uvMin.y }, // TR
				{ ch.uvMin.x, ch.uvMin.y }  // TL
			};

			// Positions relative to cursor (BL, BR, TR, TL). y grows upward.
			glm::vec3 pos[4] = {
				{     xpos,     ypos, z },
                { xpos + w,     ypos, z },
                { xpos + w, ypos + h, z },
                {     xpos, ypos + h, z }
			};

			// Add to buffer
			for (int i = 0; i < 4; i++)
			{
				s_Data.QuadBufferPtr->Position = pos[i];
				s_Data.QuadBufferPtr->Color = color;
				s_Data.QuadBufferPtr->TexCoord = uvs[i];
				s_Data.QuadBufferPtr->TexIndex = textureIndex;
				s_Data.QuadBufferPtr->TilingFactor = 1.0f;
				s_Data.QuadBufferPtr->IsText = 1.0f; // Enable SDF smoothing in shader
				s_Data.QuadBufferPtr++;
			}

			s_Data.IndexCount += 6;
			s_Data.Stats.QuadCount++;
		}

		x += (ch.Advance >> 6) * scale;
	}
}

Renderer2D::Statistics Renderer2D::GetStats()
{
	return s_Data.Stats;
}

void Renderer2D::ResetStats()
{
	memset(&s_Data.Stats, 0, sizeof(Statistics));
}
