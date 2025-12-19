#pragma once
#include "Camera.h"
#include "glew.h"
#include "glfw3.h"
#include "glm.hpp"
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
};
