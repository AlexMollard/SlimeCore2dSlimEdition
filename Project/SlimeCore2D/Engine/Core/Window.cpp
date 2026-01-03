#include "Window.h"

#include "Camera.h"
#include "EngineFactoryD3D11.h"
#include "EngineFactoryVk.h"
#include "EngineSettings.h"
#include "Input.h"
#include "Logger.h"

#if PLATFORM_WIN32
#	include "Win32NativeWindow.h"
#endif

using namespace Diligent;

RefCntAutoPtr<IEngineFactory> Window::s_EngineFactory;
RefCntAutoPtr<IRenderDevice> Window::s_Device;
RefCntAutoPtr<IDeviceContext> Window::s_Context;
RefCntAutoPtr<ISwapChain> Window::m_SwapChain;

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

	glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
	glfwWindowHint(GLFW_RESIZABLE, GLFW_TRUE);

	window = glfwCreateWindow(width, height, name, NULL, NULL);

	if (!window)
	{
		glfwTerminate();
		return -1;
	}

	glfwSetWindowUserPointer(window, this);

	Input::GetInstance()->Init(window);

	glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);

	InitDiligent(glfwGetWin32Window(window), width, height);

	// initialize viewport for current framebuffer size
	int fbW = 0, fbH = 0;
	glfwGetFramebufferSize(window, &fbW, &fbH);
	framebuffer_size_callback(window, fbW, fbH);

	Logger::Info("Diligent Engine Initialized");

	return 1;
}

void Window::InitDiligent(HWND hwnd, int width, int height)
{
	SwapChainDesc SCDesc;
	SCDesc.ColorBufferFormat = TEX_FORMAT_RGBA8_UNORM;
	SCDesc.DepthBufferFormat = TEX_FORMAT_D24_UNORM_S8_UINT;
	SCDesc.Width = width;
	SCDesc.Height = height;
	SCDesc.Usage = SWAP_CHAIN_USAGE_RENDER_TARGET;

	switch (g_RendererType)
	{
		case RendererType::D3D11:
		{
			auto* pFactoryD3D11 = GetEngineFactoryD3D11();
			s_EngineFactory = pFactoryD3D11;
			EngineD3D11CreateInfo EngineCI;
			EngineCI.Features.SeparablePrograms = DEVICE_FEATURE_STATE_ENABLED;
#ifdef _DEBUG
// EngineCI.DebugFlags |= D3D11_DEBUG_FLAG_CREATE_DEVICE_DEBUG;
#endif
			pFactoryD3D11->CreateDeviceAndContextsD3D11(EngineCI, &s_Device, &s_Context);
			NativeWindow WindowAttribs(hwnd);
			pFactoryD3D11->CreateSwapChainD3D11(s_Device, s_Context, SCDesc, FullScreenModeDesc{}, WindowAttribs, &m_SwapChain);
		}
		break;

		case RendererType::Vulkan:
		{
			auto* pFactoryVk = GetEngineFactoryVk();
			s_EngineFactory = pFactoryVk;
			EngineVkCreateInfo EngineCI;
			EngineCI.Features.SeparablePrograms = DEVICE_FEATURE_STATE_ENABLED;
#ifdef _DEBUG
			EngineCI.EnableValidation = true;
#endif
			pFactoryVk->CreateDeviceAndContextsVk(EngineCI, &s_Device, &s_Context);
			NativeWindow WindowAttribs(hwnd);
			pFactoryVk->CreateSwapChainVk(s_Device, s_Context, SCDesc, WindowAttribs, &m_SwapChain);
		}
		break;
	}
}

void Window::Update_Window()
{
	m_SwapChain->Present(1); // VSync
	glfwPollEvents();

	if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
		glfwSetWindowShouldClose(window, true);

	now = glfwGetTime();
	delta = (float) (now - last);
	last = now;
}

void Window::BeginFrame()
{
	auto* pRTV = m_SwapChain->GetCurrentBackBufferRTV();
	auto* pDSV = m_SwapChain->GetDepthBufferDSV();
	if (!pRTV) Logger::Error("Window::BeginFrame: RTV is null!");
	s_Context->SetRenderTargets(1, &pRTV, pDSV, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

	// Clear
	float color[4] = { 0.06f, 0.06f, 0.06f, 1.0f };
	s_Context->ClearRenderTarget(pRTV, color, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);
	s_Context->ClearDepthStencil(pDSV, CLEAR_DEPTH_FLAG | CLEAR_STENCIL_FLAG, 1.0f, 0, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);
}

int Window::Window_shouldClose()
{
	return glfwWindowShouldClose(window);
}

void Window::Window_destroy()
{
	if (s_Context)
		s_Context->Flush();

	m_SwapChain.Release();
	s_Context.Release();
	s_Device.Release();
	s_EngineFactory.Release();

	glfwDestroyWindow(window);
	glfwTerminate();
}

float Window::GetDeltaTime()
{
	return delta;
}

void Window::Resize(int width, int height)
{
	if (width == 0 || height == 0)
		return;

	if (m_SwapChain)
	{
		m_SwapChain->Resize(width, height);
	}

	// Update Input Viewport
	Input::GetInstance()->SetViewportRect(0, 0, width, height);

	// Update Camera
	Camera* cam = Input::GetInstance()->GetCamera();
	if (cam)
	{
		float aspectRatio = (float) width / (float) height;
		cam->SetProjection(cam->GetOrthographicSize(), aspectRatio);
	}
}

void framebuffer_size_callback(GLFWwindow* window, int width, int height)
{
	if (height == 0)
		height = 1;

	Window* win = (Window*) glfwGetWindowUserPointer(window);
	if (win)
	{
		win->Resize(width, height);
	}
}
