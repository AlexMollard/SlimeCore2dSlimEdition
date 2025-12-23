#include "Window.h"
#include "Input.h"
#include "Camera.h"
#include "Logger.h"

void framebuffer_size_callback(GLFWwindow* window, int width, int height);

Window::Window(int width, int height, char* name)
{
	// Check for Memory Leaks
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

	if (!Window_intit(width, height, name))
	{
		Logger::Error("Failed to load window");
	}
}

Window::~Window()
{
	Window_destroy();
}

int Window::Window_intit(int width, int height, char* name)
{
	if (!glfwInit())
	{
		return -1;
	}

	glfwWindowHint(GLFW_RESIZABLE, GL_TRUE);

	window = glfwCreateWindow(width, height, name, NULL, NULL);
	glfwMakeContextCurrent(window);

	if (!window)
	{
		glfwTerminate();
		return -1;
	}
	
	Input::GetInstance()->Init(window);

	glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);

	// initialize viewport for current framebuffer size (will be centered to preserve camera aspect ratio when a camera is set)
	int fbW = 0, fbH = 0;
	glfwGetFramebufferSize(window, &fbW, &fbH);
	framebuffer_size_callback(window, fbW, fbH);

	glClearColor(0.06f, 0.06f, 0.06f, 1.0f);
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glfwSwapInterval(1); // V-Sync

	// Initializing Glew
	glewExperimental = GL_TRUE;
	if (glewInit() != GLEW_OK)
	{
		Logger::Error("Glew is not having a good time");
	}

	// Outputting OpenGL Version and build
	Logger::Info("OpenGL Version: " + std::string((char*)glGetString(GL_VERSION)));

	return 1;
}

void Window::Update_Window()
{
	glfwSwapBuffers(window);
	glfwPollEvents();

	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

	if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
		glfwSetWindowShouldClose(window, true);

	now = glfwGetTime();
	delta = (float) (now - last);
	last = now;
}

int Window::Window_shouldClose()
{
	return glfwWindowShouldClose(window);
}

void Window::Window_destroy()
{
	glfwDestroyWindow(window);
	glfwTerminate();
}

float Window::GetDeltaTime()
{
	return delta;
}

void framebuffer_size_callback(GLFWwindow* window, int width, int height)
{
	if (height == 0)
		height = 1;

	glViewport(0, 0, width, height);
	Input::GetInstance()->SetViewportRect(0, 0, width, height);

	Camera* cam = Input::GetInstance()->GetCamera();
	if (cam)
	{
		float aspectRatio = (float) width / (float) height;

		// This automatically handles the "Fixed Height" logic
		// The camera keeps its OrthoSize (e.g., 10 units high)
		// and just expands the width based on the new aspect ratio.
		cam->SetProjection(cam->GetOrthographicSize(), aspectRatio);
	}
}
