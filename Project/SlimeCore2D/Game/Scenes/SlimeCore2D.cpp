#include <string>

#include "Core/EngineSettings.h"
#include "Core/Logger.h"
#include "Core/Window.h"
#include "Game2D.h"
#include "Resources/ResourceManager.h"
#include "Scripting/DotNetHost.h"
#include "Core/Memory.h"

int main(int argc, char** argv)
{
	for (int i = 1; i < argc; ++i)
	{
		std::string arg = argv[i];
		if (arg == "--vulkan" || arg == "-vk")
		{
			g_RendererType = RendererType::Vulkan;
		}
		else if (arg == "--d3d11" || arg == "-dx11")
		{
			g_RendererType = RendererType::D3D11;
		}
	}

	MemoryAllocator::Init();
	Logger::Init();
	Logger::Info("Engine Initializing...");

	Window* app = new Window(1536, 852, (char*) "SlimeCore2D");
	Game2D* game = new Game2D();
	Input* inputManager = Input::GetInstance();

	DotNetHost dotnet;
	if (!dotnet.Init())
	{
		// log, MessageBox, etc
		return -1;
	}

	dotnet.CallInit();

	while (!app->Window_shouldClose())
	{
		inputManager->Update();
		app->BeginFrame();

		float dt = app->GetDeltaTime();

		dotnet.CallUpdate(dt);

		game->Update(dt);
		game->Draw();

		app->Update_Window();
	}

	delete app;
	delete game;

	dotnet.CallForceGC();
	dotnet.Shutdown();

	// Call the resource manager clean up
	ResourceManager::GetInstance().Clear();
	MemoryAllocator::PrintLeaks();

	return 0;
}
