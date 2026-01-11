#include "Window.h"

#include "Camera.h"
#include "EngineFactoryD3D11.h"
#include "EngineFactoryD3D12.h"
#include "EngineFactoryOpenGL.h"
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

void DiligentMessageCallback(DEBUG_MESSAGE_SEVERITY Severity, const Char* Message, const Char* Function, const Char* File, int Line)
{
	// Filter out verbose Vulkan extension logs
	if (Message && (strstr(Message, "Available Vulkan instance layers") || strstr(Message, "Supported Vulkan instance extensions") || strstr(Message, "Extensions supported")))
		return;

	if (Message && strstr(Message, "Allocated new descriptor pool"))
	{
		int x = 0;
	}

	std::string msg = "";
	if (Function)
		msg += std::string(Function) + " ";
	msg += Message;
	if (File)
		msg += " (" + std::string(File) + ":" + std::to_string(Line) + ")";

	switch (Severity)
	{
		case DEBUG_MESSAGE_SEVERITY_INFO:
			Logger::LogCustom("DILIGENT", 11, msg); // 11 = Light Cyan
			break;
		case DEBUG_MESSAGE_SEVERITY_WARNING:
			Logger::LogCustom("DILIGENT", 14, "WARNING: " + msg); // 14 = Yellow
			break;
		case DEBUG_MESSAGE_SEVERITY_ERROR:
		case DEBUG_MESSAGE_SEVERITY_FATAL_ERROR:
			Logger::LogCustom("DILIGENT", 12, "ERROR: " + msg); // 12 = Light Red
			break;
	}
}

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
		case RendererType::D3D12:
		{
			auto* pFactoryD3D12 = GetEngineFactoryD3D12();
			pFactoryD3D12->SetMessageCallback(DiligentMessageCallback);
			s_EngineFactory = pFactoryD3D12;

			EngineD3D12CreateInfo EngineCI;
			EngineCI.Features.SeparablePrograms = DEVICE_FEATURE_STATE_ENABLED;
			EngineCI.Features.BindlessResources = DEVICE_FEATURE_STATE_ENABLED;

			// Increase GPU descriptor heap size for D3D12 to handle large texture arrays
			// We need a large dynamic heap because we are using DYNAMIC shader variables for texture arrays (1024 slots)
			EngineCI.GPUDescriptorHeapSize[0] = 65536;         // 64k descriptors (CBV/SRV/UAV)
			EngineCI.GPUDescriptorHeapDynamicSize[0] = 32768;  // 32k for dynamic allocations
			EngineCI.DynamicDescriptorAllocationChunkSize[0] = 2048; // Ensure chunks are large enough for our 1024 array

			EngineCI.GPUDescriptorHeapSize[1] = 2048;          // Samplers
			EngineCI.GPUDescriptorHeapDynamicSize[1] = 1024; 
			EngineCI.DynamicDescriptorAllocationChunkSize[1] = 64;

			Logger::Info("Initializing D3D12 Device...");
			pFactoryD3D12->CreateDeviceAndContextsD3D12(EngineCI, &s_Device, &s_Context);
			Logger::Info("D3D12 Device Initialized.");
			NativeWindow WindowAttribs(hwnd);
			pFactoryD3D12->CreateSwapChainD3D12(s_Device, s_Context, SCDesc, FullScreenModeDesc{}, WindowAttribs, &m_SwapChain);
		}
		break;

		case RendererType::Vulkan:
		{
			auto* pFactoryVk = GetEngineFactoryVk();
			pFactoryVk->SetMessageCallback(DiligentMessageCallback);
			s_EngineFactory = pFactoryVk;
			EngineVkCreateInfo EngineCI;
			EngineCI.Features.SeparablePrograms = DEVICE_FEATURE_STATE_ENABLED;
			EngineCI.Features.BindlessResources = DEVICE_FEATURE_STATE_ENABLED;
			EngineCI.DynamicHeapSize = 256 * 1024 * 1024; // 256 MB

			// Increase descriptor pool size to avoid frequent reallocations with many TileMap chunks
			EngineCI.MainDescriptorPoolSize.MaxDescriptorSets = 32768;
			EngineCI.MainDescriptorPoolSize.NumSeparateSamplerDescriptors = 32768;
			EngineCI.MainDescriptorPoolSize.NumSampledImageDescriptors = 32768 * 32; // 32 textures per chunk
			EngineCI.MainDescriptorPoolSize.NumUniformBufferDescriptors = 32768;

			pFactoryVk->CreateDeviceAndContextsVk(EngineCI, &s_Device, &s_Context);
			NativeWindow WindowAttribs(hwnd);
			pFactoryVk->CreateSwapChainVk(s_Device, s_Context, SCDesc, WindowAttribs, &m_SwapChain);
		}
		break;
		
		default:
			Logger::Error("Unsupported Renderer Type");
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
	if (!pRTV)
		Logger::Error("Window::BeginFrame: RTV is null!");
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
