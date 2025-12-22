#pragma once

#include <vector>

#include "glm.hpp"

// You can expand this struct to be a full UI Class hierarchy
struct UIElement
{
	glm::vec2 Position;
	glm::vec2 Size;
	glm::vec4 Color;
	// Texture* Image = nullptr;
	// std::string Text;
};

class UIManager
{
public:
	// Singleton access if desired, or just static
	static UIManager& Get();

	void Init();
	void Draw();

	// Add elements to the list to be drawn this frame (Immediate mode)
	void DrawUIQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color);
	// Add other UI widgets here...

private:
	UIManager() = default;

	glm::mat4 m_OrthoMatrix;
	// Temporary storage if you want to delay rendering,
	// OR you can just pass through to Renderer2D immediately if called inside a UI pass.
};
