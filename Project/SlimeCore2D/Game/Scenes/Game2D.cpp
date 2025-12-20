#include "Game2D.h"
#include "Utils/ObjectManager.h"

Game2D::Game2D()
{
	Init();
}

Game2D::~Game2D()
{
	delete m_physicsScene;
	m_physicsScene = nullptr;

	ObjectManager::Destroy();
}

void Game2D::Init()
{
	m_camera = new Camera(-16, -9, -1, 1);
	m_renderer = new Renderer2D(m_camera);
	m_physicsScene = new PhysicsScene();
	Input::GetInstance()->SetCamera(m_camera);

	ObjectManager::Create(m_renderer, true);
}

void Game2D::Update(float deltaTime)
{
	m_camera->Update(deltaTime);
	m_physicsScene->update(deltaTime);
	ObjectManager::Get().UpdateFrames(deltaTime);
}

void Game2D::Draw()
{
	m_renderer->Draw();
}
