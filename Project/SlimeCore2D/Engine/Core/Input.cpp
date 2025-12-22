#include "Input.h"

#include "Camera.h"
#include "glew.h"
#include "glfw3.h"

Input* Input::s_Instance = nullptr;

// GLFW Callbacks
void ScrollCallback(GLFWwindow* window, double xoffset, double yoffset)
{
	if (Input::GetInstance())
		Input::GetInstance()->SetScrollInternal((float) xoffset, (float) yoffset);
}

Input* Input::GetInstance()
{
	if (!s_Instance)
		s_Instance = new Input();
	return s_Instance;
}

void Input::Init(GLFWwindow* window)
{
	m_Window = window;
	glfwSetScrollCallback(window, ScrollCallback);

	// Initialize viewport to window size initially
	int w, h;
	glfwGetWindowSize(window, &w, &h);
	m_ViewportRect = { 0, 0, w, h };
}

void Input::Update()
{
	// 1. Reset per-frame data
	m_ScrollX = 0.0f;
	m_ScrollY = 0.0f;

	// 2. Poll Events (updates GLFW internal state)
	glfwPollEvents();

	// 3. Update Mouse Position
	double x, y;
	glfwGetCursorPos(m_Window, &x, &y);
	m_MousePos = { (float) x, (float) y };

	// 4. Update Key States for "Just Pressed" logic
	// We copy current state to 'Last' before reading new state is tricky with GLFW immediate mode.
	// Better approach for polled input:

	// Copy current frame map to last frame map
	m_KeyDataLast = m_KeyData;
	m_MouseDataLast = m_MouseData;

	// Update current frame map (This checks purely if key is physically down right now)
	// We only track keys that we care about or have been pressed to save performance,
	// but iterating all keycodes is safer. For now, we update on query or demand.
	// Actually, to support GetKeyDown properly, we need to know the state of specific keys
	// stored in our map.
	for (auto& [key, pressed]: m_KeyData)
	{
		int state = glfwGetKey(m_Window, key);
		m_KeyData[key] = (state == GLFW_PRESS || state == GLFW_REPEAT);
	}

	// Do the same for mouse buttons (0-7)
	for (int i = 0; i < 8; i++)
	{
		int state = glfwGetMouseButton(m_Window, i);
		m_MouseData[i] = (state == GLFW_PRESS);
	}
}

// --- Keyboard ---

bool Input::GetKey(Keycode key)
{
	// Direct polling
	int state = glfwGetKey(m_Window, (int) key);
	// Update internal map for Next Frame's "GetKeyDown" check
	m_KeyData[(int) key] = (state == GLFW_PRESS || state == GLFW_REPEAT);
	return m_KeyData[(int) key];
}

bool Input::GetKeyDown(Keycode key)
{
	bool isDown = GetKey(key); // Also updates m_KeyData
	bool wasDown = m_KeyDataLast[(int) key];
	return isDown && !wasDown;
}

bool Input::GetKeyUp(Keycode key)
{
	bool isDown = GetKey(key);
	bool wasDown = m_KeyDataLast[(int) key];
	return !isDown && wasDown;
}

// --- Mouse Buttons ---

bool Input::GetMouseButton(int button)
{
	int state = glfwGetMouseButton(m_Window, button);
	m_MouseData[button] = (state == GLFW_PRESS);
	return m_MouseData[button];
}

bool Input::GetMouseButtonDown(int button)
{
	bool isDown = GetMouseButton(button);
	bool wasDown = m_MouseDataLast[button];
	return isDown && !wasDown;
}

bool Input::GetMouseButtonUp(int button)
{
	bool isDown = GetMouseButton(button);
	bool wasDown = m_MouseDataLast[button];
	return !isDown && wasDown;
}

// --- Mouse Position Math ---

glm::vec2 Input::GetMousePosition()
{
	// Raw Window Pixels (Top-Left 0,0)
	return m_MousePos;
}

glm::vec2 Input::GetMousePositionWorld()
{
	if (!m_Camera)
		return { 0.0f, 0.0f };

	// 1. Get Mouse relative to the Viewport (handle letterboxing)
	float x = m_MousePos.x - m_ViewportRect.x;
	float y = m_MousePos.y - m_ViewportRect.y;

	// 2. Normalize to NDC (-1.0 to 1.0)
	// Note: OpenGL Y is Bottom-Up, GLFW Y is Top-Down. We flip Y here.
	float ndcX = (2.0f * x) / m_ViewportRect.z - 1.0f;
	float ndcY = 1.0f - (2.0f * y) / m_ViewportRect.w;

	glm::vec4 ndcCoords(ndcX, ndcY, 0.0f, 1.0f);

	// 3. Unproject: Inverse(Projection * View) * NDC
	// The new Camera class has GetViewProjectionMatrix()
	glm::mat4 invVP = glm::inverse(m_Camera->GetViewProjectionMatrix());
	glm::vec4 worldCoords = invVP * ndcCoords;

	return { worldCoords.x, worldCoords.y };
}

// --- Setters ---

void Input::SetCamera(Camera* camera)
{
	m_Camera = camera;
}

void Input::SetViewportRect(int x, int y, int width, int height)
{
	m_ViewportRect = { (float) x, (float) y, (float) width, (float) height };
}

void Input::SetScrollInternal(float x, float y)
{
	m_ScrollX = x;
	m_ScrollY = y;
}
