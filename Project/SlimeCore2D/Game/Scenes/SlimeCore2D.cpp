#include "Core/Window.h"
#include "Game2D.h"
#include "Scripting/DotNetHost.h"
#include "Core/Logger.h"
#include "Core/Memory.h"
#include "Resources/ResourceManager.h"

int main()
{
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
		app->Update_Window();

		float dt = app->GetDeltaTime();

		dotnet.CallUpdate(dt);

		game->Update(dt);
		game->Draw();
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
