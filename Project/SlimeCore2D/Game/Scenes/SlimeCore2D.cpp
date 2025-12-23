#include "Core/Window.h"
#include "Game2D.h"
#include "Scripting/DotNetHost.h"
#include "Core/Logger.h"

int main()
{
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

	dotnet.Shutdown();
	return 0;
}
