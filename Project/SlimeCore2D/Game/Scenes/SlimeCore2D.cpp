#include "Core/Window.h"
#include "Game2D.h"
#include "Scripting/DotNetHost.h"

Input* Input::instance = 0;

int main()
{
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
	delete Input::GetInstance();

	dotnet.Shutdown();
	return 0;
}
