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
}

Input::~Input()
{
}

// Most of this is broken because of the camera changing aspect ratio and position
void Input::Update()
{
	aspectX = -camera->GetAspectRatio().x;
	aspectY = -camera->GetAspectRatio().y;

	deltaMouse = glm::vec2((float) mouseXPos, (float) mouseYPos);

	glfwGetCursorPos(window, &mouseXPos, &mouseYPos);
	glfwGetWindowSize(window, &winWidth, &winHeight);

	mouseXPos /= (winWidth / (aspectX * 2.0f));
	mouseXPos -= aspectX;

	mouseYPos /= (winHeight / (aspectY * 2.0f));
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
