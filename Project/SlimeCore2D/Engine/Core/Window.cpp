#include "Window.h"
#include "Scripting/EngineExports.h"
#include "Input.h"

void framebuffer_size_callback(GLFWwindow* window, int width, int height);

Window::Window(int width, int height, char* name)
{
	// Check for Memory Leaks
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

	if (!Window_intit(width, height, name))
	{
		std::cout << "Failed to load window" << std::endl;
	}
}

Window::~Window()
{
	Window_destroy();
}

SLIME_EXPORT void __cdecl Engine_SetClearColor(float r, float g, float b)
{ 
	glClearColor(r, g, b, 1.0f);
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

	glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);

	// initialize viewport for current framebuffer size (will be centered to preserve camera aspect ratio when a camera is set)
	int fbW = 0, fbH = 0;
	glfwGetFramebufferSize(window, &fbW, &fbH);
	framebuffer_size_callback(window, fbW, fbH);

	glClearColor(0.06f, 0.06f, 0.06f, 1.0f);
	glEnable(GL_DEPTH_TEST);
	glEnable(GL_CULL_FACE);
	glCullFace(GL_BACK);
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glfwSwapInterval(1); // V-Sync

	// Initializing Glew
	glewExperimental = GL_TRUE;
	if (glewInit() != GLEW_OK)
	{
		std::cout << "Glew is not havig a good time" << std::endl;
	}

	// Outputting OpenGL Version and build
	std::cout << "OpenGL Version: " << glGetString(GL_VERSION) << std::endl;

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
	// Compute a centered viewport that preserves the camera aspect ratio (letterbox/pillarbox)
	Camera* cam = Input::GetInstance()->GetCamera();
	if (!cam)
	{
		// No camera yet: use full framebuffer
		glViewport(0, 0, width, height);
		Input::GetInstance()->SetViewportRect(0, 0, width, height);
		return;
	}

	glm::vec2 aspect = cam->GetAspectRatio();
	float targetW = fabs(aspect.x * 2.0f);
	float targetH = fabs(aspect.y * 2.0f);
	float targetAR = targetW / targetH;
	float windowAR = width / (float)height;

	int vpX = 0, vpY = 0, vpW = width, vpH = height;
	if (windowAR > targetAR)
	{
		// window wider -> pillarbox
		vpH = height;
		vpW = (int)(height * targetAR);
		vpX = (width - vpW) / 2;
	}
	else
	{
		// window taller -> letterbox
		vpW = width;
		vpH = (int)(width / targetAR);
		vpY = (height - vpH) / 2;
	}

	glViewport(vpX, vpY, vpW, vpH);
	Input::GetInstance()->SetViewportRect(vpX, vpY, vpW, vpH);
}
