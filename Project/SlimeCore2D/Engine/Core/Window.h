#pragma once
#define NOMINMAX
#define GLFW_EXPOSE_NATIVE_WIN32
#include <glfw3.h>
#include <glfw3native.h>
#include <iostream>

#include "DeviceContext.h"
#include "RefCntAutoPtr.hpp"
#include "RenderDevice.h"
#include "SwapChain.h"
#include "EngineFactory.h"

using namespace Diligent;

class Window
{
public:
	Window(int width, int height, char* name);
	~Window();

	// Window Functions
	int Window_intit(int width, int height, char* name);
	void BeginFrame();
	void Update_Window();
	int Window_shouldClose();
	void Window_destroy();
	void Resize(int width, int height);

	float GetDeltaTime();

	static IRenderDevice* GetDevice()
	{
		return s_Device;
	}

	static IDeviceContext* GetContext()
	{
		return s_Context;
	}

	static IEngineFactory* GetEngineFactory()
	{
		return s_EngineFactory;
	}

	static ITextureView* GetDepthStencilView()
	{
		return m_SwapChain ? m_SwapChain->GetDepthBufferDSV() : nullptr;
	}

	static ISwapChain* GetSwapChain()
	{
		return m_SwapChain;
	}

protected:
	// Main Window
	GLFWwindow* window;

	// DeltaTime
	double last = 0.0;
	double now = 0.0;
	float delta = 1.0f;

	// Diligent Engine
	static RefCntAutoPtr<IEngineFactory> s_EngineFactory;
	static RefCntAutoPtr<IRenderDevice> s_Device;
	static RefCntAutoPtr<IDeviceContext> s_Context;
	static RefCntAutoPtr<ISwapChain> m_SwapChain;

	void InitDiligent(HWND hwnd, int width, int height);
};
