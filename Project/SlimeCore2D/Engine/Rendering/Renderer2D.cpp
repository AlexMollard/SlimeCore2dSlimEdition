#include "Renderer2D.h"

#include <algorithm>
#include <array>
#include <iostream>
#include <map>

#include "Math.h"
#include "Resources/ResourceManager.h"

std::vector<glm::vec2> Renderer2D::UVs;
Camera* Renderer2D::camera;
Shader* Renderer2D::basicShader;
Shader* Renderer2D::UIShader;

static const size_t maxQuadCount = 2000;
static const size_t maxVertexCount = maxQuadCount * 4;
static const size_t maxIndexCount = maxQuadCount * 6;
static const size_t maxTextures = 31;

struct Vertex
{
	glm::vec3 position;
	glm::vec4 color;
	glm::vec2 texCoords;
	float texIndex;
};

struct RendererData
{
	GLuint quadVA = 0;
	GLuint quadVB = 0;
	GLuint quadIB = 0;

	GLuint whiteTexture = 0;
	uint32_t whiteTextureSlot = 0;

	uint32_t indexCount = 0;

	Vertex* quadBuffer = nullptr;
	Vertex* quadBufferPtr = nullptr;

	std::array<uint32_t, maxTextures> textureSlots = {};
	uint32_t textureSlotIndex = 1;
};

static RendererData data;

static glm::vec2 basicUVS[4] = { glm::vec2(0.0f, 0.0f), glm::vec2(1.0f, 0.0f), glm::vec2(1.0f, 1.0f), glm::vec2(0.0f, 1.0f) };

Renderer2D::Renderer2D(Camera* camera)
{
	// Load shaders from resource directories
	ResourceManager::GetInstance().LoadShadersFromDir();

	basicShader = ResourceManager::GetInstance().GetShader("basic");
	if (!basicShader)
		basicShader = new Shader("Basic Shader", "..\\Shaders\\BasicVertex.shader", "..\\Shaders\\BasicFragment.shader");

	UIShader = ResourceManager::GetInstance().GetShader("ui");
	if (!UIShader)
		UIShader = new Shader("UI Shader", "..\\Shaders\\UIVertex.shader", "..\\Shaders\\UIFragment.shader");

	this->camera = camera;

	basicShader->Use();

	auto loc = glGetUniformLocation(basicShader->GetID(), "Textures");
	int samplers[maxTextures];
	for (int i = 0; i < maxTextures; i++)
		samplers[i] = i;

	glUniform1iv(loc, maxTextures, samplers);

	UIShader->Use();

	loc = glGetUniformLocation(UIShader->GetID(), "Textures");

	glUniform1iv(loc, maxTextures, samplers);

	Init();
}

Renderer2D::~Renderer2D()
{
	ShutDown();
	for (int i = 0; i < texturePool.size(); i++)
	{
		delete texturePool[i];
		texturePool[i] = nullptr;
	}

	delete basicShader;
	basicShader = nullptr;

	delete UIShader;
	UIShader = nullptr;

	delete camera;
	camera = nullptr;
}

void Renderer2D::AddObject(GameObject* newObject)
{
	std::vector<GameObject*>::iterator it = find(objectPool.begin(), objectPool.end(), newObject);

	if (it != objectPool.end())
	{
		std::cout << "GameObject already in Renderer: " << *it << '\n';
		return;
	}

	GameObject* go = newObject;
	go->SetID(objectPool.size());
	go->SetShader(basicShader);

	if (objectPool.size() > 0)
	{
		if (objectPool.back()->GetPos().z <= go->GetPos().z)
		{
			objectPool.push_back(go);
			return;
		}

		for (int i = 0; i < objectPool.size(); i++)
		{
			if (objectPool[i]->GetPos().z >= go->GetPos().z)
			{
				objectPool.insert(objectPool.begin() + i, go);
				return;
			}
		}
	}
	else
		objectPool.push_back(go);
}

Texture* Renderer2D::LoadTexture(std::string dir)
{
	Texture* tempTex = new Texture(dir);

	texturePool.push_back(tempTex);
	return tempTex;
}

void Renderer2D::Draw()
{
	BeginBatch();

	basicShader->Use();

	basicShader->setMat4("OrthoMatrix", camera->GetTransform());
	basicShader->setMat4("Model", glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, 0.0f)));
	basicShader->setVec4("SunColor", glm::vec4(1.0f));

	glm::vec2 camPos = camera->GetPosition();

	float distanceFromCenter = -camera->GetAspectRatio().x + 6;

	for (int i = 0; i < objectPool.size(); i++)
	{
		if (objectPool[i]->GetRender() == false)
			continue;

		if (glm::distance(camPos, glm::vec2(objectPool[i]->GetPos())) > distanceFromCenter)
		{
			continue;
		}

		if (objectPool[i]->GetTexture() == nullptr)
		{
			DrawQuad(objectPool[i]->GetPos(), objectPool[i]->GetScale(), { objectPool[i]->GetColor(), 1.0f });
		}
		else if (objectPool[i]->GetTexture() != nullptr)
		{
			DrawQuad(objectPool[i]->GetPos(), objectPool[i]->GetScale(), { objectPool[i]->GetColor(), 1.0f }, objectPool[i]->GetTexture(), objectPool[i]->GetFrame(), objectPool[i]->GetSpriteWidth());
		}
	}

	EndBatch();
	Flush();
	DrawUI();
}

void Renderer2D::DrawUI()
{
	BeginBatch();

	UIShader->Use();

	UIShader->setMat4("OrthoMatrix", UIMatrix);
	UIShader->setMat4("Model", glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, 0.0f)));

	//DrawUIQuad();

	EndBatch();
	Flush();
}

Shader* Renderer2D::GetBasicShader()
{
	return basicShader;
}

void Renderer2D::DrawUIQuad(glm::vec2 pos, int layer, glm::vec2 size, glm::vec3 color, Texture* texture)
{
	if (texture == nullptr)
		DrawQuad(glm::vec3(pos.x, pos.y, 2 + layer * 0.01f), size, { color, 1.0f });
	else
		DrawQuad(glm::vec3(pos.x, pos.y, 2 + layer * 0.01f), size, { color, 1.0f }, texture, 0, texture->GetWidth());
}

void Renderer2D::DrawQuad(glm::vec3 position, glm::vec2 size, glm::vec4 color)
{
	if (data.indexCount >= maxIndexCount)
	{
		EndBatch();
		Flush();
		BeginBatch();
	}

	float textureIndex = 0.0f;

	glm::vec3 positions[4] = { glm::vec3(position.x - size.x / 2, position.y - size.y / 2, position.z), glm::vec3(position.x + size.x / 2, position.y - size.y / 2, position.z), glm::vec3(position.x + size.x / 2, position.y + size.y / 2, position.z), glm::vec3(position.x - size.x / 2, position.y + size.y / 2, position.z) };

	for (int i = 0; i < 4; i++)
	{
		data.quadBufferPtr->position = positions[i];
		data.quadBufferPtr->color = color;
		data.quadBufferPtr->texCoords = basicUVS[i];
		data.quadBufferPtr->texIndex = textureIndex;
		data.quadBufferPtr++;
	}

	data.indexCount += 6;
}

void Renderer2D::DrawQuad(glm::vec3 position, glm::vec2 size, glm::vec4 color, Texture* texture, int frame, int spriteWidth)
{
	if (data.indexCount >= maxIndexCount || data.textureSlotIndex >= maxTextures)
	{
		EndBatch();
		Flush();
		BeginBatch();
	}

	float textureIndex = 0.0f;
	for (uint32_t i = 1; i < data.textureSlotIndex; i++)
	{
		if (data.textureSlots[i] == texture->GetID())
		{
			textureIndex = (float) i;
			break;
		}
	}

	if (textureIndex == 0.0f)
	{
		textureIndex = (float) data.textureSlotIndex;
		data.textureSlots[data.textureSlotIndex] = texture->GetID();
		data.textureSlotIndex++;
	}

	static bool useBasicUVS = false;

	if (texture->GetWidth() == 16)
	{
		useBasicUVS = true;
	}
	else
	{
		useBasicUVS = false;
		setActiveRegion(texture, frame, spriteWidth);
	}

	glm::vec3 positions[4] = { glm::vec3(position.x - size.x / 2, position.y - size.y / 2, position.z), glm::vec3(position.x + size.x / 2, position.y - size.y / 2, position.z), glm::vec3(position.x + size.x / 2, position.y + size.y / 2, position.z), glm::vec3(position.x - size.x / 2, position.y + size.y / 2, position.z) };

	for (int i = 0; i < 4; i++)
	{
		data.quadBufferPtr->position = positions[i];
		data.quadBufferPtr->color = color;
		data.quadBufferPtr->texCoords = (useBasicUVS) ? basicUVS[i] : UVs[i];
		data.quadBufferPtr->texIndex = textureIndex;
		data.quadBufferPtr++;
	}

	data.indexCount += 6;
}

void Renderer2D::RemoveQuad(GameObject* object)
{
	objectPool.erase(objectPool.begin() + GetObjectIndex(object));
}

int Renderer2D::GetObjectIndex(GameObject* object)
{
	for (int i = 0; i < objectPool.size(); i++)
	{
		if (objectPool[i] == object)
		{
			return i;
		}
	}
	return -404;
}

void Renderer2D::setActiveRegion(Texture* texture, int regionIndex, int spriteWidth)
{
	UVs.clear();

	//					  (int) textureSize / spriteWidth;
	int numberOfRegions = texture->GetWidth() / spriteWidth;

	float uv_x = (regionIndex % numberOfRegions) / (float) numberOfRegions;
	float uv_y = (regionIndex / (float) numberOfRegions) * (float) numberOfRegions;

	glm::vec2 uv_down_left = glm::vec2(uv_x, uv_y);
	glm::vec2 uv_down_right = glm::vec2(uv_x + 1.0f / numberOfRegions, uv_y);
	glm::vec2 uv_up_right = glm::vec2(uv_x + 1.0f / numberOfRegions, (uv_y + 1.0f));
	glm::vec2 uv_up_left = glm::vec2(uv_x, (uv_y + 1.0f));

	UVs.push_back(uv_down_left);
	UVs.push_back(uv_down_right);
	UVs.push_back(uv_up_right);
	UVs.push_back(uv_up_left);
}

void Renderer2D::Init()
{
	data.quadBuffer = new Vertex[maxVertexCount];

	glCreateVertexArrays(1, &data.quadVA);
	glBindVertexArray(data.quadVA);

	glCreateBuffers(1, &data.quadVB);
	glBindBuffer(GL_ARRAY_BUFFER, data.quadVB);
	glBufferData(GL_ARRAY_BUFFER, maxVertexCount * sizeof(Vertex), nullptr, GL_DYNAMIC_DRAW);

	glEnableVertexArrayAttrib(data.quadVA, 0);
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, sizeof(Vertex), (const void*) offsetof(Vertex, position));

	glEnableVertexArrayAttrib(data.quadVA, 1);
	glVertexAttribPointer(1, 4, GL_FLOAT, GL_FALSE, sizeof(Vertex), (const void*) offsetof(Vertex, color));

	glEnableVertexArrayAttrib(data.quadVA, 2);
	glVertexAttribPointer(2, 3, GL_FLOAT, GL_FALSE, sizeof(Vertex), (const void*) offsetof(Vertex, texCoords));

	glEnableVertexArrayAttrib(data.quadVA, 3);
	glVertexAttribPointer(3, 1, GL_FLOAT, GL_FALSE, sizeof(Vertex), (const void*) offsetof(Vertex, texIndex));

	uint32_t indices[maxIndexCount];
	uint32_t offset = 0;
	for (int i = 0; i < maxIndexCount; i += 6)
	{
		indices[i + 0] = 0 + offset;
		indices[i + 1] = 1 + offset;
		indices[i + 2] = 2 + offset;

		indices[i + 3] = 2 + offset;
		indices[i + 4] = 3 + offset;
		indices[i + 5] = 0 + offset;

		offset += 4;
	}

	glCreateBuffers(1, &data.quadIB);
	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, data.quadIB);
	glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);

	glCreateTextures(GL_TEXTURE_2D, 1, &data.whiteTexture);
	glBindTexture(GL_TEXTURE_2D, data.whiteTexture);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

	uint32_t color = 0xffffffff;
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, 1, 1, 0, GL_RGBA, GL_UNSIGNED_BYTE, &color);

	data.textureSlots[0] = data.whiteTexture;
	for (size_t i = 1; i < maxTextures; i++)
	{
		data.textureSlots[i] = 0;
	}
}

void Renderer2D::ShutDown()
{
	glDeleteVertexArrays(1, &data.quadVA);
	glDeleteBuffers(1, &data.quadVB);
	glDeleteBuffers(1, &data.quadIB);

	glDeleteTextures(1, &data.whiteTexture);

	delete[] data.quadBuffer;
}

void Renderer2D::BeginBatch()
{
	data.quadBufferPtr = data.quadBuffer;
}

void Renderer2D::SetShader(Shader* shader)
{
	shader->Use();
	shader->setMat4("OrthoMatrix", camera->GetTransform());
	shader->setMat4("Model", glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, 0.0f)));
	shader->setVec4("SunColor", glm::vec4(1.0f));
}

void Renderer2D::EndBatch()
{
	GLsizeiptr size = (uint8_t*) data.quadBufferPtr - (uint8_t*) data.quadBuffer;
	glBindBuffer(GL_ARRAY_BUFFER, data.quadVB);
	glBufferSubData(GL_ARRAY_BUFFER, 0, size, data.quadBuffer);
}

void Renderer2D::Flush()
{
	for (uint32_t i = 0; i < data.textureSlotIndex; i++)
		glBindTextureUnit(i, data.textureSlots[i]);

	glBindVertexArray(data.quadVA);
	glDrawElements(GL_TRIANGLES, data.indexCount, GL_UNSIGNED_INT, nullptr);

	data.indexCount = 0;
	data.textureSlotIndex = 1;
}
