#include "Snake.h"

#include <algorithm>
#include <iostream>
#include <random>
#include <string>

#include "Resources/ResourceManager.h"

#define GRIDX 25
#define GRIDY 20

namespace
{
	std::mt19937& Rng()
	{
		static std::mt19937 rng{ std::random_device{}() };
		return rng;
	}

	int RandInt(int minInclusive, int maxInclusive)
	{
		std::uniform_int_distribution<int> dist(minInclusive, maxInclusive);
		return dist(Rng());
	}
} // namespace

Snake::Snake(Camera* cam, Renderer2D* rend, ObjectManager* objMan)
{
	m_camera = cam;
	m_renderer = rend;
	m_objManager = objMan;
	Input::GetInstance()->SetCamera(m_camera);

	//Reading the high scores from a file
	std::string line;
	m_hsFile.open("..\\Txt\\HS.txt", std::ios::in);
	if (m_hsFile.is_open())
	{
		int index = 0;
		while (std::getline(m_hsFile, line))
		{
			if (line.compare("") == 0)
				continue;
			if (index >= (int) m_highScores.size())
				break;
			m_highScores[index] = std::stoi(line);
			++index;
		}
		m_hsFile.close();
	}
	else
		std::cout << "Failed to load file" << std::endl;

	m_timer = 0.0f;
	m_foodPos = glm::vec2(-1.0f, -1.0f);

	m_grid = new Cell*[GRIDX];
	for (size_t i = 0; i < GRIDX; i++)
	{
		m_grid[i] = new Cell[GRIDY];
	}

	m_spawnPos = glm::vec2(GRIDX / 2 - 1, GRIDY / 2 - 1);

	float cellSize = 0.5f;
	float cellspacing = cellSize + 0.1f;

	for (size_t x = 0; x < GRIDX; x++)
	{
		for (size_t y = 0; y < GRIDY; y++)
		{
			m_grid[x][y].SetState(Cell::STATE::EMPTY);
			m_grid[x][y].m_cell = m_objManager->CreateQuad(glm::vec3((x - (GRIDX * 0.5f)) * cellspacing, (y - (GRIDY * 0.5f)) * cellspacing, 0.0f), glm::vec2(cellSize), m_gridColor);
		}
	}

	m_textShader = ResourceManager::GetInstance().GetShader("text");
	if (!m_textShader)
		m_textShader = new Shader("Text Shader", "textVert.shader", "textFrag.shader");

	m_testText = new Text();

	SpawnFood();
	SpawnTail();
}

Snake::~Snake()
{
	m_hsFile.open("..\\Txt\\HS.txt", std::ios::out | std::ios::trunc);
	for (size_t i = 0; i < m_highScores.size(); i++)
	{
		m_hsFile << std::to_string(m_highScores[i]) << std::endl;
	}
	m_hsFile.close();

	for (size_t x = 0; x < GRIDX; x++)
	{
		delete[] m_grid[x];
		m_grid[x] = nullptr;
	}

	delete[] m_grid;
	m_grid = nullptr;

	delete m_textShader;
	m_textShader = nullptr;

	delete m_testText;
	m_testText = nullptr;
}

void Snake::SpawnFood()
{
	// Clear old food (if any)
	if (m_foodPos.x >= 0 && m_foodPos.y >= 0 && m_foodPos.x < GRIDX && m_foodPos.y < GRIDY)
	{
		if (m_grid[(int) m_foodPos.x][(int) m_foodPos.y].GetState() == Cell::STATE::FOOD)
		{
			m_grid[(int) m_foodPos.x][(int) m_foodPos.y].SetState(Cell::STATE::EMPTY);
			m_grid[(int) m_foodPos.x][(int) m_foodPos.y].m_cell->SetColor(m_gridColor);
		}
	}

	int x, y;
	do
	{
		x = RandInt(0, GRIDX - 1);
		y = RandInt(0, GRIDY - 1);
	}
	while (std::find(m_bodyPos.begin(), m_bodyPos.end(), glm::vec2(x, y)) != m_bodyPos.end());

	m_foodPos = glm::vec2(x, y);

	m_grid[x][y].SetState(Cell::STATE::FOOD);
	m_grid[x][y].m_cell->SetColor(m_foodColor);
}

void Snake::UpdatePosition()
{
	if (m_bodyPos.empty())
		return;

	const glm::vec2 oldTail = m_bodyPos.back();
	glm::vec2 newHead = m_bodyPos[0] + m_direction;

	// Wall behaviour
	if (m_wrapWalls)
	{
		if (newHead.x >= GRIDX)
			newHead.x = 0;
		if (newHead.x < 0)
			newHead.x = (float) (GRIDX - 1);
		if (newHead.y >= GRIDY)
			newHead.y = 0;
		if (newHead.y < 0)
			newHead.y = (float) (GRIDY - 1);
	}
	else
	{
		if (newHead.x >= GRIDX || newHead.x < 0 || newHead.y >= GRIDY || newHead.y < 0)
		{
			Death();
			return;
		}
	}

	const bool ateFood = (newHead == m_foodPos);

	// Self collision (allow moving into the old tail cell if we're not growing)
	for (size_t i = 0; i < m_bodyPos.size(); ++i)
	{
		if (!ateFood && i == m_bodyPos.size() - 1 && newHead == oldTail)
			continue;
		if (newHead == m_bodyPos[i])
		{
			Death();
			return;
		}
	}

	// Clear tail only if we didn't eat
	if (!ateFood)
	{
		m_grid[(int) oldTail.x][(int) oldTail.y].SetState(Cell::STATE::EMPTY);
		m_grid[(int) oldTail.x][(int) oldTail.y].m_cell->SetColor(m_gridColor);
	}

	// Shift body
	for (int i = (int) m_bodyPos.size() - 1; i > 0; --i)
		m_bodyPos[i] = m_bodyPos[i - 1];
	m_bodyPos[0] = newHead;

	// Grow
	if (ateFood)
	{
		++m_score;
		++m_tailLength;
		m_bodyPos.push_back(oldTail);
		SpawnFood();
	}

	// Repaint snake cells (head slightly brighter)
	for (size_t i = 0; i < m_bodyPos.size(); ++i)
	{
		const glm::vec2 p = m_bodyPos[i];
		m_grid[(int) p.x][(int) p.y].SetState(Cell::STATE::TAIL);
		m_grid[(int) p.x][(int) p.y].m_cell->SetColor(i == 0 ? (m_snakeColor * glm::vec3(1.2f)) : m_snakeColor);
	}
}

void Snake::SpawnTail()
{
	++m_tailLength;

	if (m_bodyPos.size() == 0)
	{
		m_bodyPos.push_back(m_spawnPos);
	}
	else
	{
		// Add a new segment on top of the current tail.
		m_bodyPos.push_back(m_bodyPos.back());
	}

	m_grid[(int) m_bodyPos.back().x][(int) m_bodyPos.back().y].SetState(Cell::STATE::TAIL);
	m_grid[(int) m_bodyPos.back().x][(int) m_bodyPos.back().y].m_cell->SetColor(m_snakeColor * glm::vec3(1.2f));
}

void Snake::Update(float deltaTime)
{
	m_timer += deltaTime;

	// Global controls
	if (Input::GetKeyRelease(Keycode::P))
		TogglePause();
	if (Input::GetKeyRelease(Keycode::M))
		ToggleWallWrap();
	if (Input::GetKeyRelease(Keycode::R))
		Restart();

	// Movement input (arrows + WASD)
	const bool left = Input::GetKeyPress(Keycode::LEFT) || Input::GetKeyPress(Keycode::A);
	const bool up = Input::GetKeyPress(Keycode::UP) || Input::GetKeyPress(Keycode::W);
	const bool right = Input::GetKeyPress(Keycode::RIGHT) || Input::GetKeyPress(Keycode::D);
	const bool down = Input::GetKeyPress(Keycode::DOWN) || Input::GetKeyPress(Keycode::S);

	if (left && m_lastDirection != glm::vec2(1.0f, 0.0f))
	{
		m_direction = glm::vec2(-1.0f, 0.0f);
	}
	else if (up && m_lastDirection != glm::vec2(0.0f, -1.0f))
	{
		m_direction = glm::vec2(0.0f, 1.0f);
	}
	else if (right && m_lastDirection != glm::vec2(-1.0f, 0.0f))
	{
		m_direction = glm::vec2(1.0f, 0.0f);
	}
	else if (down && m_lastDirection != glm::vec2(0.0f, 1.0f))
	{
		m_direction = glm::vec2(0.0f, -1.0f);
	}

	if (m_state == GameState::Paused)
	{
		m_testText->RenderText(*m_textShader, "PAUSED (P to resume)", 320.0f, 520.0f, 1.2f, glm::vec3(1.0f));
		return;
	}

	if (m_state == GameState::GameOver)
	{
		m_testText->RenderText(*m_textShader, "GAME OVER", 360.0f, 560.0f, 1.6f, glm::vec3(1.0f, 0.25f, 0.25f));
		m_testText->RenderText(*m_textShader, "Press R to restart", 330.0f, 520.0f, 1.0f, glm::vec3(1.0f));
		return;
	}

	if (m_state == GameState::Ready)
	{
		// Start once a direction is chosen
		if (m_direction != glm::vec2(0))
			m_state = GameState::Playing;
		else
			m_testText->RenderText(*m_textShader, "Move to start (Arrows/WASD)", 260.0f, 520.0f, 1.0f, glm::vec3(1.0f));
	}

	const float tick = std::max(m_minTick, (m_baseTick - (m_tailLength * 0.002f)));
	if (m_state == GameState::Playing && m_direction != glm::vec2(0) && m_timer > tick)
	{
		m_lastDirection = m_direction;
		UpdatePosition();
		m_timer = 0;
	}

	std::string test = "Score: " + std::to_string(m_score);
	m_testText->RenderText(*m_textShader, test, 25.0f, 25.0f, 1.0f, glm::vec3(0.5f, 0.8f, 0.2f));
	m_testText->RenderText(*m_textShader, "SNAKE", 850.0F, 900.0F, 1.0F, glm::vec3(1.0f, 0.0f, 0.0f));

	std::string mode = std::string("Walls: ") + (m_wrapWalls ? "WRAP (M)" : "DEATH (M)");
	m_testText->RenderText(*m_textShader, mode, 25.0f, 65.0f, 0.8f, glm::vec3(0.8f));
	m_testText->RenderText(*m_textShader, "P: Pause   R: Restart", 25.0f, 95.0f, 0.8f, glm::vec3(0.8f));

	for (size_t i = 0; i < m_highScores.size(); i++)
	{
		std::string highScore = "HS" + std::to_string(i + 1) + ":     " + std::to_string(m_highScores[i]);
		m_testText->RenderText(*m_textShader, highScore, 25.0f, 900.0f - (50.0f * i), 1.0f, glm::vec3(0.0f, 1.0f, 0.0f));
	}
}

void Snake::Death()
{
	std::cout << "GAMEOVER \nYour score: " << m_score << std::endl;
	SaveScore(m_score);
	m_state = GameState::GameOver;
}

void Snake::Restart()
{
	// Clear board
	m_tailLength = 0;
	m_bodyPos.clear();
	m_direction = glm::vec2(0);
	m_lastDirection = glm::vec2(0);
	m_score = 0;
	m_timer = 0.0f;
	m_state = GameState::Ready;

	for (size_t x = 0; x < GRIDX; x++)
	{
		for (size_t y = 0; y < GRIDY; y++)
		{
			m_grid[x][y].SetState(Cell::STATE::EMPTY);
			m_grid[x][y].m_cell->SetColor(m_gridColor);
		}
	}

	SpawnTail();
	SpawnFood();
}

void Snake::SaveScore(int score)
{
	for (size_t i = 0; i < m_highScores.size(); i++)
	{
		if (score <= m_highScores[i])
			continue;

		int savedScore = m_highScores[i];
		m_highScores[i] = score;
		score = savedScore;
	}
}

void Snake::TogglePause()
{
	if (m_state == GameState::Playing)
		m_state = GameState::Paused;
	else if (m_state == GameState::Paused)
		m_state = (m_direction == glm::vec2(0)) ? GameState::Ready : GameState::Playing;
}

void Snake::ToggleWallWrap()
{
	m_wrapWalls = !m_wrapWalls;
}
