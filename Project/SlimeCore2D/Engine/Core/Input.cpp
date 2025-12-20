#include "Input.h"

#include <iostream>
#include <string>

void window_focus_callback(GLFWwindow* window, int focused);
void scroll_callback(GLFWwindow* window, double xoffset, double yoffset);
float Input::scroll;
double Input::mouseXPos;
double Input::mouseYPos;

std::unordered_map<int, bool> Input::keyPrevState;

Input::Input()
{
	window = glfwGetCurrentContext();
	glfwSetWindowFocusCallback(window, window_focus_callback);
	glfwSetScrollCallback(window, scroll_callback);

	// initialize viewport to current framebuffer size (defaults to full window)
	glfwGetFramebufferSize(window, &viewportWidth, &viewportHeight);
	viewportX = 0;
	viewportY = 0;
}

Input::~Input()
{
}

// Map cursor from framebuffer space into the active viewport and then into world coords
void Input::Update()
{
	if (camera)
	{
		aspectX = -camera->GetAspectRatio().x;
		aspectY = -camera->GetAspectRatio().y;
	}

	deltaMouse = glm::vec2((float) mouseXPos, (float) mouseYPos);

	glfwGetCursorPos(window, &mouseXPos, &mouseYPos);
	glfwGetWindowSize(window, &winWidth, &winHeight);

	// Use viewport rect (fallback to full window if not set)
	int vpX = viewportX;
	int vpY = viewportY;
	int vpW = viewportWidth ? viewportWidth : winWidth;
	int vpH = viewportHeight ? viewportHeight : winHeight;

	double localX = mouseXPos - vpX;
	double localY = mouseYPos - vpY;

	// Clamp to viewport area
	if (localX < 0) localX = 0;
	if (localX > vpW) localX = vpW;
	if (localY < 0) localY = 0;
	if (localY > vpH) localY = vpH;

	mouseXPos = localX;
	mouseYPos = localY;

	// Convert to world coordinates using camera half-extents
	mouseXPos /= (vpW / (aspectX * 2.0f));
	mouseXPos -= aspectX;

	mouseYPos /= (vpH / (aspectY * 2.0f));
	mouseYPos -= aspectY;
	mouseYPos = -mouseYPos;

	deltaMouse -= glm::vec2((float) mouseXPos, (float) mouseYPos);
	Input::scroll = 0.0f;
} 

glm::vec2 Input::GetMousePos()
{
	return glm::vec2(mouseXPos, mouseYPos);
}

glm::vec2 Input::GetDeltaMouse()
{
	return deltaMouse;
}

glm::vec2 Input::GetWindowSize()
{
	return glm::vec2(winWidth, winHeight);
}

glm::vec2 Input::GetAspectRatio()
{
	return glm::vec2(aspectX, aspectY);
}

bool Input::GetMouseDown(int button)
{
	return (glfwGetMouseButton(GetInstance()->window, button));
}

void Input::SetCamera(Camera* cam)
{
	camera = cam;
	if (!camera || !window) return;

	// Compute framebuffer/viewport to keep the camera aspect ratio and center it (letterbox/pillarbox)
	int fbW = 0, fbH = 0;
	glfwGetFramebufferSize(window, &fbW, &fbH);
	if (fbW == 0 || fbH == 0)
	{
		SetViewportRect(0, 0, fbW, fbH);
		glViewport(0, 0, fbW, fbH);
		return;
	}

	glm::vec2 aspect = camera->GetAspectRatio();
	float targetW = fabs(aspect.x * 2.0f);
	float targetH = fabs(aspect.y * 2.0f);
	float targetAR = targetW / targetH;
	float windowAR = fbW / (float)fbH;

	int vpX = 0, vpY = 0, vpW = fbW, vpH = fbH;
	if (windowAR > targetAR)
	{
		// window wider -> pillarbox
		vpH = fbH;
		vpW = (int)(fbH * targetAR);
		vpX = (fbW - vpW) / 2;
	}
	else
	{
		// window taller -> letterbox
		vpW = fbW;
		vpH = (int)(fbW / targetAR);
		vpY = (fbH - vpH) / 2;
	}
	glViewport(vpX, vpY, vpW, vpH);
	SetViewportRect(vpX, vpY, vpW, vpH);
} 

GLFWwindow* Input::GetWindow()
{
	return window;
}

bool Input::GetFocus()
{
	return IsWindowFocused;
}

void Input::SetFocus(bool focus)
{
	IsWindowFocused = focus;
}

glm::vec2 Input::GetMouseToWorldPos()
{
	return GetInstance()->camera->GetPosition() + (glm::vec2(mouseXPos, mouseYPos));
}

void window_focus_callback(GLFWwindow* window, int focused)
{
	Input::GetInstance()->SetFocus(focused);
}

void scroll_callback(GLFWwindow* window, double xoffset, double yoffset)
{
	Input::SetScroll(yoffset);
}

// Viewport helpers
void Input::SetViewportRect(int x, int y, int width, int height)
{
	viewportX = x;
	viewportY = y;
	viewportWidth = width;
	viewportHeight = height;
}

glm::vec4 Input::GetViewportRect()
{
	return glm::vec4(viewportX, viewportY, viewportWidth, viewportHeight);
}

Camera* Input::GetCamera()
{
	return camera;
}

bool Input::GetKeyPress(Keycode key)
{
	int state = glfwGetKey(GetInstance()->window, (int) key);
	if (state == GLFW_PRESS)
	{
		return true;
	}

	return false;
}

bool Input::GetKeyRelease(Keycode key)
{
	// Edge-detect a release: return true only when the key was pressed previously
	// and is not pressed now.
	int state = glfwGetKey(GetInstance()->window, (int) key);
	bool isPressedNow = (state == GLFW_PRESS || state == GLFW_REPEAT);

	bool wasPressed = false;
	auto it = keyPrevState.find((int) key);
	if (it != keyPrevState.end())
		wasPressed = it->second;

	// update saved state for next call
	keyPrevState[(int) key] = isPressedNow;

	return (wasPressed && !isPressedNow);
}

void Input::SetScroll(float newScroll)
{
	scroll = newScroll;
}

float Input::GetScroll()
{
	return Input::scroll;
}
