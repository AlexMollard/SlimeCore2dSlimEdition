#include "Game2D.h"

Game2D::Game2D()
{
	Init();

}

Game2D::~Game2D()
{
	delete physicsScene;
	physicsScene = nullptr;

	delete objectManager;
	objectManager = nullptr;

	delete snakeGame;
	snakeGame = nullptr;
}

void Game2D::Init()
{
	camera = new Camera(-16, -9, -1, 1);
	renderer = new Renderer2D(camera);
	objectManager = new ObjectManager(renderer);
	physicsScene = new PhysicsScene();
	Input::GetInstance()->SetCamera(camera);

	snakeGame = new Snake(camera, renderer, objectManager);
}

void Game2D::Update(float deltaTime)
{
	camera->Update(deltaTime);
	physicsScene->update(deltaTime);
	objectManager->UpdateFrames(deltaTime);
	snakeGame->Update(deltaTime);

}

void Game2D::Draw()
{
	renderer->Draw();
}