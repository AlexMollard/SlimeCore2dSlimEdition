#include "Game2D.h"

Game2D::Game2D()
{
	Init();

	testObject = objectManager->CreateQuad(glm::vec3(1,1,0), glm::vec2(2), glm::vec3(0.3f, 0.8f, 0.34f));
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

	if (testObject->GetMouseColliding())
	{
		testObject->SetColor(glm::vec3(0.1f, 0.5f, 0.14f));
		if (inputManager->GetMouseDown(0))
		{
			testObject->SetPos(glm::vec3(inputManager->GetMousePos(),0));
		}
	}
	else
	{
		testObject->SetColor(glm::vec3(0.3f, 0.8f, 0.34f));
	}
}

void Game2D::Draw()
{
	if (Input::GetKeyPress(Keycode::SPACE))
		physicsScene->Debug();
	else
		renderer->Draw();
}