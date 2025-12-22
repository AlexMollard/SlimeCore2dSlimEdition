#include "UIManager.h"

#include <gtc/matrix_transform.hpp>
#include "Scripting/EngineExports.h"

#include "Renderer2D.h"
#include "Core/Input.h"

static UIManager* s_Instance = nullptr;

UIManager& UIManager::Get()
{
	if (!s_Instance)
		s_Instance = new UIManager();
	return *s_Instance;
}

void UIManager::Init()
{
	// Initialize with a default; updated each frame from the viewport in Draw().
	m_OrthoMatrix = glm::ortho(0.0f, 1920.0f, 0.0f, 1080.0f, -1.0f, 1.0f);
}

void UIManager::Draw()
{
	// Rebuild UI projection each frame: centered origin, same unit scale as world camera.
	// Default UI height matches the game camera (18 units), width derives from viewport aspect.
	auto viewport = Input::GetInstance()->GetViewportRect();
	float vpW = viewport.z > 0 ? (float) viewport.z : 1920.0f;
	float vpH = viewport.w > 0 ? (float) viewport.w : 1080.0f;
	float aspect = (vpH > 0.0f) ? (vpW / vpH) : (16.0f / 9.0f);

	const float uiHeight = 18.0f; // match Camera ortho size used in Game2D
	const float uiWidth = uiHeight * aspect;

	m_OrthoMatrix = glm::ortho(-uiWidth * 0.5f, uiWidth * 0.5f,
	        -uiHeight * 0.5f, uiHeight * 0.5f,
	        -1.0f, 1.0f);

	// 1. Start a new scene with UI projection
	Renderer2D::BeginScene(m_OrthoMatrix);

	// 2. Render all submitted UI elements
	EngineExports_RenderUI();

	// Draw a quad where the pointer is predicted to be
	glm::vec2 mouseWorldPos = Input::GetInstance()->GetMousePositionWorld();
	float mouseX = mouseWorldPos.x * 1920.0f / 32.0f + 960.0f;
	float mouseY = mouseWorldPos.y * 1080.0f / 18.0f + 540.0f;

	Renderer2D::DrawQuad({mouseX, mouseY, 1.0f}, { 10.0f, 10.0f }, { 0.0f, 1.0f, 0.0f, 1.0f });

	Renderer2D::EndScene();
}

void UIManager::DrawUIQuad(const glm::vec2& position, const glm::vec2& size, const glm::vec4& color)
{
	// Pass through to Renderer, assuming BeginScene(UI) was called
	Renderer2D::DrawQuad(position, size, color);
}
