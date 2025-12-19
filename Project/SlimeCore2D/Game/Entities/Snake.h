#pragma once
#include <fstream>

#include "Cell.h"
#include "Rendering/Shader.h"
#include "Rendering/Text.h"
#include "Utils/ObjectManager.h"

class Snake
{
public:
	Snake(Camera* cam, Renderer2D* rend, ObjectManager* objMan);

	~Snake();

	void SpawnFood();
	void UpdatePosition();
	void SpawnTail();

	void Update(float deltaTime);

	int GetScore()
	{
		return m_score;
	}

	void Death();
	void Restart();

	void SaveScore(int score);

private:
	Renderer2D* m_renderer = nullptr;
	ObjectManager* m_objManager = nullptr;
	Input* m_inputManager = Input::GetInstance();
	Camera* m_camera = nullptr;

	Cell** m_grid = nullptr;

	glm::vec2 m_foodPos;

	int m_tailLength = 0;
	int m_score = 0;
	std::vector<glm::vec2> m_bodyPos;

	glm::vec2 m_direction = glm::vec2(0);
	glm::vec2 m_lastDirection = glm::vec2(0);
	glm::vec2 m_spawnPos;
	float m_updateLength = 0;

	float m_timer;

	Text* m_testText;
	Shader* m_textShader;

	int m_highScores[10];
	std::fstream m_hsFile;

	bool m_isDead = false;

	//Colours
	glm::vec3 m_gridColor = glm::vec3(0.25f);
	glm::vec3 m_snakeColor = glm::vec3(0.1f, 0.75f, 0.15f);
	glm::vec3 m_foodColor = glm::vec3(0.75f, 0.15f, 0.2f);
};
