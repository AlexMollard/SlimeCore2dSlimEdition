#include <iostream>

#include "glew.h"
#include "Game2D.h"
#include "gtc/matrix_transform.hpp"

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

	// Cleanup Scene
	delete m_scene;
	m_scene = nullptr;

	// Cleanup Singletons
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

	// 3. Initialize Scene
	m_scene = new Scene();

	// 4. Initialize Physics
	m_physicsScene = new PhysicsScene();
}

void Game2D::Update(float deltaTime)
{
	m_timer += deltaTime;

	// Update Physics
	if (m_physicsScene)
		m_physicsScene->update(deltaTime);

	// Update Scene (Handles all GameObjects)
	if (m_scene)
	{
		m_scene->Update(deltaTime);
	}
}

void Game2D::Draw()
{
	// -----------------------------------------------------------
	// PASS 1: WORLD RENDERING
	// -----------------------------------------------------------
	// Reset stats for the new frame
	Renderer2D::ResetStats();

	// Render Scene Graph
	if (m_scene)
	{
		m_scene->Render(*m_camera);
	}

	// -----------------------------------------------------------
	// PASS 2: UI RENDERING
	// -----------------------------------------------------------

	// Draw Scripting UI
	if (m_scene)
	{
		// Create a UI camera that matches the window settings but stays at origin
		// This ensures UI elements (HUD) don't move with the world camera
		Camera uiCamera(m_camera->GetOrthographicSize(), m_camera->GetAspectRatio());
		
		glClear(GL_DEPTH_BUFFER_BIT);
		Renderer2D::BeginScene(uiCamera);
		m_scene->RenderUI();
		Renderer2D::EndScene();
	}
}
