#include "UIManager.h"
#include "Text.h"
#include "Renderer2D.h"
#include "Resources/ResourceManager.h"
#include "Core/Input.h"

#include <algorithm>
#include <iostream>
#include <cstdio>

UIManager::UIManager()
{
}

UIManager::~UIManager()
{
	for (auto &p : m_elements)
	{
		if (p.second.texture) delete p.second.texture;
	}
	m_elements.clear();
}

UIManager& UIManager::Get()
{
	static UIManager inst;
	return inst;
}

UIId UIManager::CreateText(const std::string& text, int fontSize, float x, float y)
{
	UIElement e;
	e.id = m_nextId++;
	e.text = text;
	e.fontSize = fontSize;
	e.x = x;
	e.y = y;
	// create texture
	int w=0,h=0;
	unsigned int tex = Text::CreateTextureFromString(ResourceManager::GetInstance().GetResourcePath("Fonts\\Chilanka-Regular.ttf"), text, fontSize, w, h);
	if (tex != 0)
	{
		unsigned int tu = tex;
		e.texture = new Texture(&tu);
		e.texW = w; e.texH = h;
	}

	m_elements[e.id] = std::move(e);
	return e.id;
}

void UIManager::Destroy(UIId id)
{
	auto it = m_elements.find(id);
	if (it == m_elements.end()) return;
	if (it->second.texture) delete it->second.texture;
	m_elements.erase(it);
}

void UIManager::SetText(UIId id, const std::string& text)
{
	auto it = m_elements.find(id);
	if (it == m_elements.end()) return;

	// free old texture
	if (it->second.texture) delete it->second.texture;

	it->second.text = text;
	int w=0,h=0;
	unsigned int tex = Text::CreateTextureFromString(ResourceManager::GetInstance().GetResourcePath("Fonts\\Chilanka-Regular.ttf"), text, it->second.fontSize, w, h);
	if (tex != 0)
	{
		unsigned int tu = tex;
		it->second.texture = new Texture(&tu);
		it->second.texW = w; it->second.texH = h;
	}
}

void UIManager::SetPosition(UIId id, float x, float y)
{
	auto it = m_elements.find(id);
	if (it == m_elements.end()) return;
	it->second.x = x; it->second.y = y;
}

void UIManager::SetAnchor(UIId id, float ax, float ay)
{
	auto it = m_elements.find(id);
	if (it == m_elements.end()) return;
	it->second.anchorX = ax; it->second.anchorY = ay;
}

void UIManager::SetColor(UIId id, float r, float g, float b)
{
	auto it = m_elements.find(id);
	if (it == m_elements.end()) return;
	it->second.r = r; it->second.g = g; it->second.b = b;
}

void UIManager::SetVisible(UIId id, bool visible)
{
	auto it = m_elements.find(id);
	if (it == m_elements.end()) return;
	it->second.visible = visible;
}

void UIManager::SetLayer(UIId id, int layer)
{
	auto it = m_elements.find(id);
	if (it == m_elements.end()) return;
	it->second.layer = layer;
}

void UIManager::Draw()
{
	// Simple draw: iterate elements sorted by layer and call Renderer2D::DrawUIQuad
	std::vector<UIElement*> elems;
	elems.reserve(m_elements.size());
	for (auto &p : m_elements) elems.push_back(&p.second);

	std::sort(elems.begin(), elems.end(), [](const UIElement* a, const UIElement* b) { return a->layer < b->layer; });

	for (auto e : elems)
	{
		if (!e->visible) continue;
		// Compute size in UI units by mapping texture pixel size to UI-space
		// UI ortho in Renderer2D uses a roughly 32x18 unit space (left/right/top/bottom),
		// so convert pixels -> UI units using the current framebuffer size to avoid
		// upscaling low-resolution glyph textures.
		glm::vec2 win = Input::GetInstance()->GetWindowSize();
		float winW = (win.x > 0.0f) ? win.x : 1920.0f;
		float winH = (win.y > 0.0f) ? win.y : 1080.0f;
		const float UI_UNITS_W = 32.0f; // matches Renderer2D::m_UIMatrix width (-16..16)
		const float UI_UNITS_H = 18.0f; // matches Renderer2D::m_UIMatrix height (-9..9)
		float sizeX = (float)e->texW * (UI_UNITS_W / winW);
		float sizeY = (float)e->texH * (UI_UNITS_H / winH);
		// Per-UI-element debug group
		char dbg[128];
		snprintf(dbg, sizeof(dbg), "UI Element ID=%llu Layer=%d Tex=%d", (unsigned long long)e->id, e->layer, (e->texture ? e->texture->GetID() : 0));
		glPushDebugGroup(GL_DEBUG_SOURCE_APPLICATION, 0, -1, dbg);

		// Adjust for anchor: Renderer2D::DrawUIQuad expects pos in UI coords and layer
		Renderer2D::DrawUIQuad(glm::vec2(e->x, e->y), e->layer, glm::vec2(sizeX, sizeY), glm::vec3(e->r, e->g, e->b), e->texture);

		glPopDebugGroup();
	}
}