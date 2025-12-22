#pragma once

#include <glm.hpp>
#include <unordered_map>

#include "Keycode.h"

// Forward Declarations
struct GLFWwindow;
class Camera;

class Input
{
public:
	// Singleton Access
	static Input* GetInstance();

	// Lifecycle
	void Init(GLFWwindow* window);
	void Update(); // Call this at the START of every frame

	// --- Keyboard ---
	// Returns true while the key is held down
	bool GetKey(Keycode key);
	// Returns true only on the frame the key was pressed
	bool GetKeyDown(Keycode key);
	// Returns true only on the frame the key was released
	bool GetKeyUp(Keycode key);

	// --- Mouse ---
	// Returns true while the button is held down (0=Left, 1=Right, 2=Middle)
	bool GetMouseButton(int button);
	bool GetMouseButtonDown(int button);
	bool GetMouseButtonUp(int button);

	// Screen Coordinates (Pixels, 0,0 is Top-Left usually, but we normalize to Bottom-Left for GL)
	glm::vec2 GetMousePosition();

	// World Coordinates (Units, based on Camera)
	glm::vec2 GetMousePositionWorld();

	float GetScroll() const
	{
		return m_ScrollY;
	}

	// --- Window / Viewport ---
	void SetCamera(Camera* camera);

	Camera* GetCamera() const
	{
		return m_Camera;
	}

	void SetViewportRect(int x, int y, int width, int height);

	glm::vec4 GetViewportRect() const
	{
		return m_ViewportRect;
	}

	// Callbacks (Internal use by GLFW)
	void SetScrollInternal(float x, float y);

private:
	Input() = default;
	~Input() = default;

	// Singleton instance
	static Input* s_Instance;

	GLFWwindow* m_Window = nullptr;
	Camera* m_Camera = nullptr;

	// Mouse State
	glm::vec2 m_MousePos = { 0.0f, 0.0f };
	float m_ScrollX = 0.0f;
	float m_ScrollY = 0.0f;

	// Viewport (X, Y, Width, Height)
	glm::vec4 m_ViewportRect = { 0, 0, 1920, 1080 };

	// Input State Maps (Current and Previous Frame)
	// We use two maps to detect "Down" vs "Held" events
	std::unordered_map<int, bool> m_KeyData;
	std::unordered_map<int, bool> m_KeyDataLast;

	std::unordered_map<int, bool> m_MouseData;
	std::unordered_map<int, bool> m_MouseDataLast;
};
