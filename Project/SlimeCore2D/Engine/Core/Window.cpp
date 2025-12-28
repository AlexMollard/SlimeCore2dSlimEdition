#include "Window.h"
#include "Input.h"
#include "Camera.h"
#include "Logger.h"

ComPtr<ID3D11Device> Window::s_Device;
ComPtr<ID3D11DeviceContext> Window::s_Context;
ComPtr<ID3D11RenderTargetView> Window::s_RenderTargetView;
ComPtr<ID3D11DepthStencilView> Window::s_DepthStencilView;

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
	
	Input::GetInstance()->Init(window);

	glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);

    InitDirectX(glfwGetWin32Window(window), width, height);

	// initialize viewport for current framebuffer size
	int fbW = 0, fbH = 0;
	glfwGetFramebufferSize(window, &fbW, &fbH);
	framebuffer_size_callback(window, fbW, fbH);

	Logger::Info("DirectX 11 Initialized");

	return 1;
}

void Window::InitDirectX(HWND hwnd, int width, int height)
{
    DXGI_SWAP_CHAIN_DESC scd;
    ZeroMemory(&scd, sizeof(DXGI_SWAP_CHAIN_DESC));
    scd.BufferCount = 1;
    scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    scd.BufferDesc.Width = width;
    scd.BufferDesc.Height = height;
    scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    scd.OutputWindow = hwnd;
    scd.SampleDesc.Count = 1;
    scd.SampleDesc.Quality = 0;
    scd.Windowed = TRUE;
    scd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

    UINT createDeviceFlags = 0;
#ifdef _DEBUG
    createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

    D3D11CreateDeviceAndSwapChain(
        nullptr,
        D3D_DRIVER_TYPE_HARDWARE,
        nullptr,
        createDeviceFlags,
        nullptr,
        0,
        D3D11_SDK_VERSION,
        &scd,
        &m_SwapChain,
        &s_Device,
        nullptr,
        &s_Context
    );

    CreateRenderTarget(width, height);

    // Rasterizer State
    D3D11_RASTERIZER_DESC rd;
    ZeroMemory(&rd, sizeof(D3D11_RASTERIZER_DESC));
    rd.FillMode = D3D11_FILL_SOLID;
    rd.CullMode = D3D11_CULL_NONE;
    rd.FrontCounterClockwise = FALSE;
    rd.DepthClipEnable = TRUE;
    s_Device->CreateRasterizerState(&rd, &m_RasterizerState);
    s_Context->RSSetState(m_RasterizerState.Get());

    // Blend State
    D3D11_BLEND_DESC bd;
    ZeroMemory(&bd, sizeof(D3D11_BLEND_DESC));
    bd.RenderTarget[0].BlendEnable = TRUE;
    bd.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
    bd.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
    bd.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
    bd.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
    bd.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
    bd.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
    bd.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
    s_Device->CreateBlendState(&bd, &m_BlendState);
    float blendFactor[] = { 0.0f, 0.0f, 0.0f, 0.0f };
    s_Context->OMSetBlendState(m_BlendState.Get(), blendFactor, 0xffffffff);
    
    // Depth Stencil State
    D3D11_DEPTH_STENCIL_DESC dsd;
    ZeroMemory(&dsd, sizeof(D3D11_DEPTH_STENCIL_DESC));
    dsd.DepthEnable = TRUE;
    dsd.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
    dsd.DepthFunc = D3D11_COMPARISON_LESS_EQUAL;
    s_Device->CreateDepthStencilState(&dsd, &m_DepthStencilState);
    s_Context->OMSetDepthStencilState(m_DepthStencilState.Get(), 0);
}

void Window::CreateRenderTarget(int width, int height)
{
    ComPtr<ID3D11Texture2D> backBuffer;
    m_SwapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&backBuffer);
    s_Device->CreateRenderTargetView(backBuffer.Get(), nullptr, &s_RenderTargetView);

    // Depth Buffer
    D3D11_TEXTURE2D_DESC dsd;
    ZeroMemory(&dsd, sizeof(dsd));
    dsd.Width = width;
    dsd.Height = height;
    dsd.MipLevels = 1;
    dsd.ArraySize = 1;
    dsd.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
    dsd.SampleDesc.Count = 1;
    dsd.SampleDesc.Quality = 0;
    dsd.Usage = D3D11_USAGE_DEFAULT;
    dsd.BindFlags = D3D11_BIND_DEPTH_STENCIL;
    s_Device->CreateTexture2D(&dsd, nullptr, &m_DepthStencilBuffer);
    s_Device->CreateDepthStencilView(m_DepthStencilBuffer.Get(), nullptr, &s_DepthStencilView);

    s_Context->OMSetRenderTargets(1, s_RenderTargetView.GetAddressOf(), s_DepthStencilView.Get());
    
    D3D11_VIEWPORT vp;
    vp.Width = (float)width;
    vp.Height = (float)height;
    vp.MinDepth = 0.0f;
    vp.MaxDepth = 1.0f;
    vp.TopLeftX = 0;
    vp.TopLeftY = 0;
    s_Context->RSSetViewports(1, &vp);
}

void Window::CleanupRenderTarget()
{
    if (s_Context) s_Context->OMSetRenderTargets(0, 0, 0);
    s_RenderTargetView.Reset();
    s_DepthStencilView.Reset();
    m_DepthStencilBuffer.Reset();
}

void Window::Update_Window()
{
    m_SwapChain->Present(1, 0); // VSync
	glfwPollEvents();

    // Clear
    float color[4] = { 0.06f, 0.06f, 0.06f, 1.0f };
    s_Context->ClearRenderTargetView(s_RenderTargetView.Get(), color);
    s_Context->ClearDepthStencilView(s_DepthStencilView.Get(), D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
    
    // Re-bind Render Target (just in case something unbound it)
    s_Context->OMSetRenderTargets(1, s_RenderTargetView.GetAddressOf(), s_DepthStencilView.Get());

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
    CleanupRenderTarget();
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

	Input::GetInstance()->SetViewportRect(0, 0, width, height);

    // Update DX11 Viewport
    if (Window::GetContext())
    {
        D3D11_VIEWPORT vp;
        vp.Width = (float)width;
        vp.Height = (float)height;
        vp.MinDepth = 0.0f;
        vp.MaxDepth = 1.0f;
        vp.TopLeftX = 0;
        vp.TopLeftY = 0;
        Window::GetContext()->RSSetViewports(1, &vp);
    }

	Camera* cam = Input::GetInstance()->GetCamera();
	if (cam)
	{
		float aspectRatio = (float) width / (float) height;
		cam->SetProjection(cam->GetOrthographicSize(), aspectRatio);
	}
}
