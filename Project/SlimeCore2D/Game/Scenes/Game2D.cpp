#include "Game2D.h"

Game2D::Game2D()
{
	Init();
}

Game2D::~Game2D()
{
	delete m_physicsScene;
	m_physicsScene = nullptr;

	delete m_objectManager;
	m_objectManager = nullptr;

	delete m_snakeGame;
	m_snakeGame = nullptr;
}

void Game2D::Init()
{
	m_camera = new Camera(-16, -9, -1, 1);
	m_renderer = new Renderer2D(m_camera);
	m_objectManager = new ObjectManager(m_renderer);
	m_physicsScene = new PhysicsScene();
	Input::GetInstance()->SetCamera(m_camera);

	m_snakeGame = new Snake(m_camera, m_renderer, m_objectManager);
}

void Game2D::Update(float deltaTime)
{
	m_camera->Update(deltaTime);
	m_physicsScene->update(deltaTime);
	m_objectManager->UpdateFrames(deltaTime);
	m_snakeGame->Update(deltaTime);
}

void Game2D::Draw()
{
	m_renderer->Draw();
}
