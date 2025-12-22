#include "Game2D.h"
#include "gtc/matrix_transform.hpp"

#include "Rendering/UIManager.h"
#include "Utils/ObjectManager.h"

Game2D::Game2D()
{
	Init();
}

Game2D::~Game2D()
{
	// Cleanup Physics
	delete m_physicsScene;
	m_physicsScene = nullptr;

	// Cleanup Camera
	delete m_camera;
	m_camera = nullptr;

	// Cleanup Singletons
	ObjectManager::Destroy();
	Renderer2D::Shutdown();
}

void Game2D::Init()
{
	// 1. Initialize Static Renderer
	Renderer2D::Init();

	// 2. Setup Camera
	// Adjust these values based on your desired aspect ratio / zoom
	m_camera = new Camera(18.0f, 16.0f / 9.0f);
	Input::GetInstance()->SetCamera(m_camera);

	// 3. Initialize Object Manager (Logic State)
	ObjectManager::Create();

	// 4. Initialize Physics
	m_physicsScene = new PhysicsScene();

	// 5. Initialize UI (Optional, if UIManager does setup)
	UIManager::Get().Init();
}

void Game2D::Update(float deltaTime)
{
	m_timer += deltaTime;

	// Update Physics
	if (m_physicsScene)
		m_physicsScene->update(deltaTime);

	// Update Game Objects (Scripts, Logic, Animation)
	if (ObjectManager::IsCreated())
	{
		ObjectManager::Get().Update(deltaTime);       // General Update
		ObjectManager::Get().UpdateFrames(deltaTime); // Sprite Animation
	}
}

void Game2D::Draw()
{
	// -----------------------------------------------------------
	// PASS 1: WORLD RENDERING
	// -----------------------------------------------------------
	// Reset stats for the new frame
	Renderer2D::ResetStats();

	// Begin World Batch (Uses Camera View Projection)
	Renderer2D::BeginScene(*m_camera);

	// Submit all game objects to the renderer
	if (ObjectManager::IsCreated())
	{
		ObjectManager::Get().RenderAll();
	}

	// Flush world batch
	Renderer2D::EndScene();

	// -----------------------------------------------------------
	// PASS 2: UI RENDERING
	// -----------------------------------------------------------

	// 1. Draw C++ internal UI (if any)
	UIManager::Get().Draw();
	
}
