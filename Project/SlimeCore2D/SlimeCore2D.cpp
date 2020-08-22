#include "Window.h"
#include "Game2D.h"

Input* Input::instance = 0;

int main()
{
	Window* app = new Window(1536, 852, (char*)"SlimeCore2D");
	Game2D* game = new Game2D();
	Input* inputManager = Input::GetInstance();

	while (!app->Window_shouldClose())
	{
		inputManager->Update();
		app->Update_Window();

		game->Update(app->GetDeltaTime());
		game->Draw();
		//std::cout << "DeltaTime: " << app->GetDeltaTime() << "ms" << std::endl;
	}

	delete app;
	app = nullptr;

	delete game;
	game = nullptr;

	delete Input::GetInstance();

	return 0;
}