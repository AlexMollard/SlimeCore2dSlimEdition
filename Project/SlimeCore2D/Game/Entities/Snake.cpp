#include "Snake.h"

#include <iostream>
#include <string>

#include "Resources/ResourceManager.h"

#define GRIDX 25
#define GRIDY 20

Snake::Snake(Camera* cam, Renderer2D* rend, ObjectManager* objMan)
{
	m_camera = cam;
	m_renderer = rend;
	m_objManager = objMan;
	Input::GetInstance()->SetCamera(m_camera);

	//Reading the high scores from a file
	std::string line;
	m_hsFile.open("..\\Txt\\HS.txt");
	if (m_hsFile.is_open())
	{
		int index = 0;
		while (std::getline(m_hsFile, line))
		{
			if (line.compare("") == 0)
				continue;
			m_highScores[index] = std::stoi(line);
			index++;
		}
		m_hsFile.close();
	}
	else
		std::cout << "Failed to load file" << std::endl;

	srand(time_t(0));

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
	m_hsFile.open("..\\Txt\\HS.txt");
	for (int i = 0; i < 10; i++)
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
	int x, y;
	do
	{
		x = rand() % GRIDX;
		y = rand() % GRIDY;
	}
	while (std::find(m_bodyPos.begin(), m_bodyPos.end(), glm::vec2(x, y)) != m_bodyPos.end());

	m_foodPos = glm::vec2(x, y);

	m_grid[x][y].SetState(Cell::STATE::FOOD);
	m_grid[x][y].m_cell->SetColor(m_foodColor);
}

void Snake::UpdatePosition()
{
	int tailEnd = m_tailLength - 1;
	glm::vec2 endOfTailPosFromPrevTic = m_bodyPos.back();

	m_grid[(int) endOfTailPosFromPrevTic.x][(int) endOfTailPosFromPrevTic.y].SetState(Cell::STATE::EMPTY);
	m_grid[(int) endOfTailPosFromPrevTic.x][(int) endOfTailPosFromPrevTic.y].m_cell->SetColor(m_gridColor);

	for (int i = tailEnd; i > 0; i--)
	{
		m_bodyPos[i] = m_bodyPos[i - 1];

		m_grid[(int) m_bodyPos[i].x][(int) m_bodyPos[i].y].SetState(Cell::STATE::TAIL);
		m_grid[(int) m_bodyPos[i].x][(int) m_bodyPos[i].y].m_cell->SetColor(m_snakeColor);
	}

	m_bodyPos[0] += m_direction;

	if (m_bodyPos[0].x >= GRIDX || m_bodyPos[0].x < 0 || m_bodyPos[0].y >= GRIDY || m_bodyPos[0].y < 0)
	{
		Death();
		return;
	}

	m_grid[(int) m_bodyPos[0].x][(int) m_bodyPos[0].y].SetState(Cell::STATE::TAIL);
	m_grid[(int) m_bodyPos[0].x][(int) m_bodyPos[0].y].m_cell->SetColor(m_snakeColor * glm::vec3(1.2f));

	if (m_bodyPos[0] == m_foodPos)
	{
		++m_score;
		SpawnFood();
		SpawnTail();
	}

	for (int i = 1; i < m_bodyPos.size(); i++)
	{
		if (m_bodyPos[0] == m_bodyPos[i])
			Death();
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
		m_bodyPos.push_back(m_bodyPos.back() - (m_bodyPos[m_bodyPos.size() - 1]));
	}

	m_grid[(int) m_bodyPos.back().x][(int) m_bodyPos.back().y].SetState(Cell::STATE::TAIL);
	m_grid[(int) m_bodyPos.back().x][(int) m_bodyPos.back().y].m_cell->SetColor(m_gridColor);
}

void Snake::Update(float deltaTime)
{
	m_timer += deltaTime;

	if (m_inputManager->GetKeyPress(Keycode::LEFT) && m_lastDirection != glm::vec2(1.0f, 0.0f))
	{
		m_direction = glm::vec2(-1.0f, 0.0f);
	}
	else if (m_inputManager->GetKeyPress(Keycode::UP) && m_lastDirection != glm::vec2(0.0f, -1.0f))
	{
		m_direction = glm::vec2(0.0f, 1.0f);
	}
	else if (m_inputManager->GetKeyPress(Keycode::RIGHT) && m_lastDirection != glm::vec2(-1.0f, 0.0f))
	{
		m_direction = glm::vec2(1.0f, 0.0f);
	}
	else if (m_inputManager->GetKeyPress(Keycode::DOWN) && m_lastDirection != glm::vec2(0.0f, 1.0f))
	{
		m_direction = glm::vec2(0.0f, -1.0f);
	}

	if (m_timer > (0.15f - (m_tailLength * 0.002f)))
	{
		m_lastDirection = m_direction;
		UpdatePosition();
		m_timer = 0;
	}

	std::string test = "Score: " + std::to_string(m_score);
	m_testText->RenderText(*m_textShader, test, 25.0f, 25.0f, 1.0f, glm::vec3(0.5f, 0.8f, 0.2f));
	m_testText->RenderText(*m_textShader, "SNAKE", 850.0F, 900.0F, 1.0F, glm::vec3(1.0f, 0.0f, 0.0f));

	for (size_t i = 0; i < 10; i++)
	{
		std::string highScore = "HS" + std::to_string(i + 1) + ":     " + std::to_string(m_highScores[i]);
		m_testText->RenderText(*m_textShader, highScore, 25.0f, 900.0f - (50.0f * i), 1.0f, glm::vec3(0.0f, 1.0f, 0.0f));
	}
}

void Snake::Death()
{
	std::cout << "GAMEOVER \nYour score: " << m_tailLength - 1 << std::endl;
	SaveScore(m_score);
	m_score = 0;
	Restart();
}

void Snake::Restart()
{
	m_tailLength = 0;
	m_bodyPos.clear();
	m_direction = glm::vec2(0);
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
	for (int i = 0; i <= 9; i++)
	{
		if (score <= m_highScores[i])
			continue;

		int savedScore = m_highScores[i];
		m_highScores[i] = score;
		score = savedScore;
	}
}
