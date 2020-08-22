#include "Game2D.h"

Game2D::Game2D()
{
	Init();

	GameObject* lastSquare = nullptr;

	for (int x = 0; x < 10; x++)
	{
		for (int y = 0; y < 10; y++)
		{
			lastSquare = objectManager->CreateQuad(glm::vec3(x - 5,y - 5,0),glm::vec2(1),glm::vec3(x / 10.0f,y / 10.0f,0.5f));
			physicsScene->addActor(lastSquare,"Square: " + x);
		}
	}

	lastSquare = objectManager->CreateQuad(glm::vec3(0, -8, 0), glm::vec2(15,1), glm::vec3(0.5f));
	physicsScene->addActor(lastSquare, "PlatForm",true);

}

Game2D::~Game2D()
{
	delete physicsScene;
	physicsScene = nullptr;

	delete objectManager;
	objectManager = nullptr;
}

void Game2D::Init()
{
	camera = new Camera(-16, -9, -1, 1);
	renderer = new Renderer2D(camera);
	objectManager = new ObjectManager(renderer);
	physicsScene = new PhysicsScene();
	Input::GetInstance()->SetCamera(camera);
}

void Game2D::Update(float deltaTime)
{
	camera->Update(deltaTime);
	physicsScene->update(deltaTime);
	objectManager->UpdateFrames(deltaTime);
}

void Game2D::Draw()
{
	if (Input::GetKeyPress(Keycode::SPACE))
		physicsScene->Debug();
	else
		renderer->Draw();
}