#include "Renderer2D.h"

#include <algorithm>
#include <array>
#include <cstdio>
#include <iostream>
#include <map>

#include "Math.h"
#include "Resources/ResourceManager.h"
#include "UIManager.h"

std::vector<glm::vec2> Renderer2D::m_UVs;
Camera* Renderer2D::m_camera;
Shader* Renderer2D::basicShader;
Shader* Renderer2D::m_UIShader;

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

	m_UIShader = ResourceManager::GetInstance().GetShader("ui");
	if (!m_UIShader)
		m_UIShader = new Shader("UI Shader", "..\\Shaders\\UIVertex.shader", "..\\Shaders\\UIFragment.shader");

	this->m_camera = camera;

	basicShader->Use();

	auto loc = glGetUniformLocation(basicShader->GetID(), "Textures");
	int samplers[maxTextures];
	for (int i = 0; i < maxTextures; i++)
		samplers[i] = i;

	glUniform1iv(loc, maxTextures, samplers);

	// Label shader program for RenderDoc
	if (basicShader)
		glObjectLabel(GL_PROGRAM, basicShader->GetID(), -1, "Renderer2D BasicShader");

	m_UIShader->Use();

	loc = glGetUniformLocation(m_UIShader->GetID(), "Textures");

	glUniform1iv(loc, maxTextures, samplers);

	if (m_UIShader)
		glObjectLabel(GL_PROGRAM, m_UIShader->GetID(), -1, "Renderer2D UIShader");

	Init();
}

Renderer2D::~Renderer2D()
{
	ShutDown();
	for (int i = 0; i < m_texturePool.size(); i++)
	{
		delete m_texturePool[i];
		m_texturePool[i] = nullptr;
	}

	delete basicShader;
	basicShader = nullptr;

	delete m_UIShader;
	m_UIShader = nullptr;

	delete m_camera;
	m_camera = nullptr;
}

void Renderer2D::AddObject(GameObject* newObject)
{
	std::vector<GameObject*>::iterator it = find(m_objectPool.begin(), m_objectPool.end(), newObject);

	if (it != m_objectPool.end())
	{
		std::cout << "GameObject already in Renderer: " << *it << '\n';
		return;
	}

	GameObject* go = newObject;
	go->SetID(m_objectPool.size());
	go->SetShader(basicShader);

	if (m_objectPool.size() > 0)
	{
		if (m_objectPool.back()->GetPos().z <= go->GetPos().z)
		{
			m_objectPool.push_back(go);
			return;
		}

		for (int i = 0; i < m_objectPool.size(); i++)
		{
			if (m_objectPool[i]->GetPos().z >= go->GetPos().z)
			{
				m_objectPool.insert(m_objectPool.begin() + i, go);
				return;
			}
		}
	}
	else
		m_objectPool.push_back(go);
}

Texture* Renderer2D::LoadTexture(std::string dir)
{
	Texture* tempTex = new Texture(dir);

	m_texturePool.push_back(tempTex);

	// Name texture for RenderDoc debugging
	if (tempTex && tempTex->GetID() != 0)
	{
		std::string name = "Texture: " + dir;
		glObjectLabel(GL_TEXTURE, tempTex->GetID(), -1, name.c_str());
	}

	return tempTex;
}

void Renderer2D::Draw()
{
	// Debug group for world draw
	glPushDebugGroup(GL_DEBUG_SOURCE_APPLICATION, 0, -1, "Renderer2D::Draw - World");

	BeginBatch();

	basicShader->Use();

	basicShader->setMat4("OrthoMatrix", m_camera->GetTransform());
	basicShader->setMat4("Model", glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, 0.0f)));
	basicShader->setVec4("SunColor", glm::vec4(1.0f));

	glm::vec2 camPos = m_camera->GetPosition();

	// Sort objects by layer first, then by z (stable to preserve relative order within equal keys)
	std::stable_sort(m_objectPool.begin(),
	        m_objectPool.end(),
	        [](GameObject* a, GameObject* b)
	        {
		        if (a->GetLayer() != b->GetLayer())
			        return a->GetLayer() > b->GetLayer();
		        return a->GetPos().z < b->GetPos().z;
	        });

	for (int i = 0; i < m_objectPool.size(); i++)
	{
		if (m_objectPool[i]->GetRender() == false)
			continue;

		if (m_objectPool[i]->GetTexture() == nullptr)
		{
			DrawQuad(m_objectPool[i]->GetPos(), m_objectPool[i]->GetScale(), { m_objectPool[i]->GetColor(), 1.0f });
		}
		else if (m_objectPool[i]->GetTexture() != nullptr)
		{
			DrawQuad(m_objectPool[i]->GetPos(), m_objectPool[i]->GetScale(), { m_objectPool[i]->GetColor(), 1.0f }, m_objectPool[i]->GetTexture(), m_objectPool[i]->GetFrame(), m_objectPool[i]->GetSpriteWidth());
		}
	}

	EndBatch();
	Flush();
	// End world draw debug group
	glPopDebugGroup();

	DrawUI();
}

void Renderer2D::DrawUI()
{
	// Debug group for UI draw
	glPushDebugGroup(GL_DEBUG_SOURCE_APPLICATION, 0, -1, "Renderer2D::Draw - UI");

	// Save GL depth, stencil, and cull state so we can restore after UI draw
	GLboolean depthTestEnabled = glIsEnabled(GL_DEPTH_TEST);
	GLboolean stencilTestEnabled = glIsEnabled(GL_STENCIL_TEST);
	GLboolean cullFaceEnabled = glIsEnabled(GL_CULL_FACE);
	GLboolean prevDepthMask = GL_TRUE;
	glGetBooleanv(GL_DEPTH_WRITEMASK, &prevDepthMask);

	// Disable depth/stencil/culling for UI so it always appears on top
	if (depthTestEnabled)
		glDisable(GL_DEPTH_TEST);
	glDepthMask(GL_FALSE);
	if (stencilTestEnabled)
		glDisable(GL_STENCIL_TEST);
	if (cullFaceEnabled)
		glDisable(GL_CULL_FACE);

	BeginBatch();

	m_UIShader->Use();

	m_UIShader->setMat4("OrthoMatrix", m_UIMatrix);
	m_UIShader->setMat4("Model", glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, 0.0f)));

	// Let the UIManager draw all UI elements
	UIManager::Get().Draw();

	EndBatch();
	Flush();

	// restore GL state
	if (prevDepthMask)
		glDepthMask(GL_TRUE);
	else
		glDepthMask(GL_FALSE);
	if (depthTestEnabled)
		glEnable(GL_DEPTH_TEST);
	if (stencilTestEnabled)
		glEnable(GL_STENCIL_TEST);
	if (cullFaceEnabled)
		glEnable(GL_CULL_FACE);

	glPopDebugGroup();
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

void Renderer2D::DrawQuad(glm::vec3 position, glm::vec2 size, glm::vec4 color, glm::vec2 anchor)
{
	if (data.indexCount >= maxIndexCount)
	{
		EndBatch();
		Flush();
		BeginBatch();
	}

	float textureIndex = 0.0f;

	// Anchor is normalized (0..1). position is the anchor point in world space.
	// corners: (0,0) bottom-left, (1,0) bottom-right, (1,1) top-right, (0,1) top-left
	glm::vec3 positions[4] = { glm::vec3(position.x + (0.0f - anchor.x) * size.x, position.y + (0.0f - anchor.y) * size.y, position.z),
		glm::vec3(position.x + (1.0f - anchor.x) * size.x, position.y + (0.0f - anchor.y) * size.y, position.z),
		glm::vec3(position.x + (1.0f - anchor.x) * size.x, position.y + (1.0f - anchor.y) * size.y, position.z),
		glm::vec3(position.x + (0.0f - anchor.x) * size.x, position.y + (1.0f - anchor.y) * size.y, position.z) };

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

void Renderer2D::DrawQuad(glm::vec3 position, glm::vec2 size, glm::vec4 color, Texture* texture, int frame, int spriteWidth, glm::vec2 anchor)
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

	// Anchor is normalized (0..1).
	glm::vec3 positions[4] = { glm::vec3(position.x + (0.0f - anchor.x) * size.x, position.y + (0.0f - anchor.y) * size.y, position.z),
		glm::vec3(position.x + (1.0f - anchor.x) * size.x, position.y + (0.0f - anchor.y) * size.y, position.z),
		glm::vec3(position.x + (1.0f - anchor.x) * size.x, position.y + (1.0f - anchor.y) * size.y, position.z),
		glm::vec3(position.x + (0.0f - anchor.x) * size.x, position.y + (1.0f - anchor.y) * size.y, position.z) };

	for (int i = 0; i < 4; i++)
	{
		data.quadBufferPtr->position = positions[i];
		data.quadBufferPtr->color = color;
		data.quadBufferPtr->texCoords = (useBasicUVS) ? basicUVS[i] : m_UVs[i];
		data.quadBufferPtr->texIndex = textureIndex;
		data.quadBufferPtr++;
	}

	data.indexCount += 6;
}

void Renderer2D::RemoveQuad(GameObject* object)
{
	m_objectPool.erase(m_objectPool.begin() + GetObjectIndex(object));
}

int Renderer2D::GetObjectIndex(GameObject* object)
{
	for (int i = 0; i < m_objectPool.size(); i++)
	{
		if (m_objectPool[i] == object)
		{
			return i;
		}
	}
	return -404;
}

void Renderer2D::setActiveRegion(Texture* texture, int regionIndex, int spriteWidth)
{
	m_UVs.clear();

	//					  (int) textureSize / spriteWidth;
	int numberOfRegions = texture->GetWidth() / spriteWidth;

	float uv_x = (regionIndex % numberOfRegions) / (float) numberOfRegions;
	float uv_y = (regionIndex / (float) numberOfRegions) * (float) numberOfRegions;

	glm::vec2 uv_down_left = glm::vec2(uv_x, uv_y);
	glm::vec2 uv_down_right = glm::vec2(uv_x + 1.0f / numberOfRegions, uv_y);
	glm::vec2 uv_up_right = glm::vec2(uv_x + 1.0f / numberOfRegions, (uv_y + 1.0f));
	glm::vec2 uv_up_left = glm::vec2(uv_x, (uv_y + 1.0f));

	m_UVs.push_back(uv_down_left);
	m_UVs.push_back(uv_down_right);
	m_UVs.push_back(uv_up_right);
	m_UVs.push_back(uv_up_left);
}

void Renderer2D::Init()
{
	data.quadBuffer = new Vertex[maxVertexCount];

	glCreateVertexArrays(1, &data.quadVA);
	glBindVertexArray(data.quadVA);
	// Label VAO for RenderDoc
	glObjectLabel(GL_VERTEX_ARRAY, data.quadVA, -1, "Renderer2D Quad VA");

	glCreateBuffers(1, &data.quadVB);
	glBindBuffer(GL_ARRAY_BUFFER, data.quadVB);
	glBufferData(GL_ARRAY_BUFFER, maxVertexCount * sizeof(Vertex), nullptr, GL_DYNAMIC_DRAW);
	// Label vertex buffer
	glObjectLabel(GL_BUFFER, data.quadVB, -1, "Renderer2D Quad VB");

	glEnableVertexArrayAttrib(data.quadVA, 0);
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, sizeof(Vertex), (const void*) offsetof(Vertex, position));

	glEnableVertexArrayAttrib(data.quadVA, 1);
	glVertexAttribPointer(1, 4, GL_FLOAT, GL_FALSE, sizeof(Vertex), (const void*) offsetof(Vertex, color));

	glEnableVertexArrayAttrib(data.quadVA, 2);
	glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, sizeof(Vertex), (const void*) offsetof(Vertex, texCoords));

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
	// Label index buffer
	glObjectLabel(GL_BUFFER, data.quadIB, -1, "Renderer2D Quad IB");

	glCreateTextures(GL_TEXTURE_2D, 1, &data.whiteTexture);
	glBindTexture(GL_TEXTURE_2D, data.whiteTexture);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

	uint32_t color = 0xffffffff;
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, 1, 1, 0, GL_RGBA, GL_UNSIGNED_BYTE, &color);
	// Label white texture
	glObjectLabel(GL_TEXTURE, data.whiteTexture, -1, "Renderer2D WhiteTexture");

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
	shader->setMat4("OrthoMatrix", m_camera->GetTransform());
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
	// Label and report the upcoming draw to the debug stream
	char dbg[128];
	snprintf(dbg, sizeof(dbg), "Renderer2D::Flush - indices=%u textures=%u", (uint32_t) data.indexCount, (uint32_t) data.textureSlotIndex);
	glPushDebugGroup(GL_DEBUG_SOURCE_APPLICATION, 0, -1, dbg);

	for (uint32_t i = 0; i < data.textureSlotIndex; i++)
	{
		uint32_t tid = data.textureSlots[i];
		if (tid != 0)
		{
			// ensure texture has a label for RenderDoc
			char tname[64];
			snprintf(tname, sizeof(tname), "Renderer2D TextureSlot %u - ID %u", i, tid);
			glObjectLabel(GL_TEXTURE, tid, -1, tname);
		}
		glBindTextureUnit(i, tid);
	}

	glBindVertexArray(data.quadVA);
	glDrawElements(GL_TRIANGLES, data.indexCount, GL_UNSIGNED_INT, nullptr);

	glPopDebugGroup();

	data.indexCount = 0;
	data.textureSlotIndex = 1;
}
