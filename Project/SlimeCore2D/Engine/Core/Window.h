#pragma once
#define NOMINMAX
#define GLFW_EXPOSE_NATIVE_WIN32
#include <glfw3.h>
#include <glfw3native.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <wrl/client.h>
#include <iostream>

using Microsoft::WRL::ComPtr;

class Window
{
public:
	Window(int width, int height, char* name);
	~Window();

	// Window Functions
	int Window_intit(int width, int height, char* name);
	void Update_Window();
	int Window_shouldClose();
	void Window_destroy();
    void Resize(int width, int height);

	float GetDeltaTime();

	static ID3D11Device* GetDevice() { return s_Device.Get(); }
	static ID3D11DeviceContext* GetContext() { return s_Context.Get(); }
	static ID3D11DepthStencilView* GetDepthStencilView() { return s_DepthStencilView.Get(); }

protected:
	// Main Window
	GLFWwindow* window;

	// DeltaTime
	double last = 0.0;
	double now = 0.0;
	float delta = 1.0f;

	// DX11
	static ComPtr<ID3D11Device> s_Device;
	static ComPtr<ID3D11DeviceContext> s_Context;
	static ComPtr<ID3D11RenderTargetView> s_RenderTargetView;
	static ComPtr<ID3D11DepthStencilView> s_DepthStencilView;
	
	ComPtr<IDXGISwapChain> m_SwapChain;
	ComPtr<ID3D11Texture2D> m_DepthStencilBuffer;
	ComPtr<ID3D11RasterizerState> m_RasterizerState;
	ComPtr<ID3D11BlendState> m_BlendState;
	ComPtr<ID3D11DepthStencilState> m_DepthStencilState;

	void InitDirectX(HWND hwnd, int width, int height);
	void CreateRenderTarget(int width, int height);
	void CleanupRenderTarget();
};
