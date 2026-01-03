#include "Game2D.h"

#include <iostream>

#include "Core/Window.h"
#include "gtc/matrix_transform.hpp"
#include "Scripting/DotNetHost.h"

#include "Core/Logger.h"
#include "Core/Memory.h"

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

	// Draw Managed World (TileMap, etc)
	DotNetHost::GetInstance()->CallDraw();
	
	static int frameCount = 0;
	frameCount++;
	if (frameCount % 60 == 0) Logger::Info("Game2D::Draw - After CallDraw");

	// Render Scene Graph
	if (m_scene)
	{
		// Check for primary camera in scene
		Entity camEntity = m_scene->GetPrimaryCameraEntity();
		if (camEntity != NullEntity)
		{
			auto& tc = m_scene->GetRegistry().GetComponent<TransformComponent>(camEntity);
			auto& cc = m_scene->GetRegistry().GetComponent<CameraComponent>(camEntity);

			auto viewport = Input::GetInstance()->GetViewportRect();
			float width = (float) viewport.z;
			float height = (float) viewport.w;
			float aspect = (height > 0) ? width / height : 16.0f / 9.0f;

			m_camera->SetPosition(tc.Position);
			m_camera->SetRotation(tc.Rotation);
			m_camera->SetZoom(cc.ZoomLevel);
			m_camera->SetProjection(cc.OrthographicSize, aspect);
		}

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

		// Clear Depth Buffer for UI overlay
		Window::GetContext()->ClearDepthStencil(Window::GetDepthStencilView(), CLEAR_DEPTH_FLAG, 1.0f, 0, RESOURCE_STATE_TRANSITION_MODE_TRANSITION);

		Renderer2D::BeginScene(uiCamera);
		m_scene->RenderUI(m_camera->GetOrthographicSize());
		Renderer2D::EndScene();
	}
}
