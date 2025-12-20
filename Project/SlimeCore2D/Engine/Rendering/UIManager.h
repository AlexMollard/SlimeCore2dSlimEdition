#pragma once

#include <string>
#include <unordered_map>

#include "Texture.h"

using UIId = std::uint64_t;

struct UIElement
{
	UIId id = 0;
	std::string text;
	Texture* texture = nullptr;
	int texW = 0, texH = 0;
	float x = 0, y = 0;
	float anchorX = 0.5f, anchorY = 0.5f;
	float r = 1, g = 1, b = 1;
	int layer = 1;
	bool visible = true;
	int fontSize = 24;
};

class UIManager
{
public:
	static UIManager& Get();
	UIId CreateText(const std::string& text, int fontSize, float x, float y);
	void Destroy(UIId id);
	void SetText(UIId id, const std::string& text);
	void SetPosition(UIId id, float x, float y);
	void SetAnchor(UIId id, float ax, float ay);
	void SetColor(UIId id, float r, float g, float b);
	void SetVisible(UIId id, bool visible);
	void SetLayer(UIId id, int layer);

	void Draw();

private:
	UIManager();
	~UIManager();

	UIId m_nextId = 1;
	std::unordered_map<UIId, UIElement> m_elements;
};