#pragma once
#include <GL/glew.h>
#include <GLFW/glfw3.h>
#include <glm/glm.hpp> 
#include "Camera.h"
#include "Keycode.h"
#include <unordered_map>

class Input
{
public:
	~Input();

	static Input* GetInstance()
	{
		if (!instance)
			instance = new Input;
		return instance;
	};

	void Update();

	GLFWwindow* GetWindow();

	static glm::vec2 GetMousePos();
	glm::vec2 GetDeltaMouse();

	glm::vec2 GetWindowSize();
	glm::vec2 GetAspectRatio();
	static bool GetMouseDown(int button);

	void SetCamera(Camera* cam);
	
	// Viewport rectangle used for rendering when preserving aspect ratio
	void SetViewportRect(int x, int y, int width, int height);
	glm::vec4 GetViewportRect();
	Camera* GetCamera();

	static bool GetKeyPress(Keycode key);
	static bool GetKeyRelease(Keycode key);

	static void SetScroll(float newScroll);
	static float GetScroll();

	bool GetFocus();
	void SetFocus(bool focus);

	static glm::vec2 GetMouseToWorldPos();

private:
	static Input* instance;

	Input();

	GLFWwindow* window;

	static double mouseXPos;
	static double mouseYPos;

    static std::unordered_map<int, bool> keyPrevState;

	int winWidth = 0;
	int winHeight = 0;

	double aspectX = 32;
	double aspectY = 18;

	bool IsWindowFocused = true;

	static float scroll;
	glm::vec2 deltaMouse = glm::vec2();

	Camera* camera = nullptr;

	// Current viewport rect inside the framebuffer (x, y, width, height)
	int viewportX = 0;
	int viewportY = 0;
	int viewportWidth = 0;
	int viewportHeight = 0;
};
