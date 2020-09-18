#include "Snake.h"
#include <iostream> 

#define GRIDX 25
#define GRIDY 20

Snake::Snake(Camera* cam, Renderer2D* rend, ObjectManager* objMan)
{
	_camera = cam;
	_renderer = rend;
	_objManager = objMan;
	Input::GetInstance()->SetCamera(_camera);

	srand(time_t(0)); 

	_grid = new Cell * [GRIDX];
	for (size_t i = 0; i < GRIDX; i++)
	{
		_grid[i] = new Cell[GRIDY];
	}

	spawnPos = glm::vec2(GRIDX/2 - 1, GRIDY/2 - 1);

	float cellSize = 0.5f;
	float cellspacing = cellSize + 0.1f;

	for (size_t x = 0; x < GRIDX; x++)
	{
		for (size_t y = 0; y < GRIDY; y++)
		{
			_grid[x][y].SetState(Cell::STATE::EMPTY);
			_grid[x][y]._cell = _objManager->CreateQuad(glm::vec3((x - (GRIDX * 0.5f)) * cellspacing, (y - (GRIDY * 0.5f)) * cellspacing, 0.0f), glm::vec2(cellSize), gridColor);
		}
	}

	SpawnFood();
	SpawnTail();
}

Snake::~Snake()
{
	for (size_t x = 0; x < GRIDX; x++)
	{
		delete[] _grid[x];
		_grid[x] = nullptr;
	}

	delete[] _grid;
	_grid = nullptr;
}

void Snake::SpawnFood()
{
	int x, y;
	do
	{
		x = rand() % GRIDX;
		y = rand() % GRIDY;

	} while (std::find(_bodyPos.begin(), _bodyPos.end(), glm::vec2(x,y)) != _bodyPos.end());

	_foodPos = glm::vec2(x, y);

	_grid[x][y].SetState(Cell::STATE::FOOD);
	_grid[x][y]._cell->SetColor(foodColor);
}

void Snake::UpdatePosition()
{
	int tailEnd = _tailLength - 1; 
	glm::vec2 endOfTailPosFromPrevTic = _bodyPos.back();

	_grid[(int)endOfTailPosFromPrevTic.x][(int)endOfTailPosFromPrevTic.y].SetState(Cell::STATE::EMPTY);
	_grid[(int)endOfTailPosFromPrevTic.x][(int)endOfTailPosFromPrevTic.y]._cell->SetColor(gridColor);

	for (int i = tailEnd; i > 0; i--)
	{
		_bodyPos[i] = _bodyPos[i - 1];

		_grid[(int)_bodyPos[i].x][(int)_bodyPos[i].y].SetState(Cell::STATE::TAIL);
		_grid[(int)_bodyPos[i].x][(int)_bodyPos[i].y]._cell->SetColor(snakeColor);
	}

	_bodyPos[0] += _direction; 


	if (_bodyPos[0].x >= GRIDX || _bodyPos[0].x < 0 || _bodyPos[0].y >= GRIDY || _bodyPos[0].y < 0)
	{
		Death();
		return; 
	}

	_grid[(int)_bodyPos[0].x][(int)_bodyPos[0].y].SetState(Cell::STATE::TAIL);
	_grid[(int)_bodyPos[0].x][(int)_bodyPos[0].y]._cell->SetColor(snakeColor * glm::vec3(1.2f));

	if (_bodyPos[0] == _foodPos)
	{
		SpawnFood();
		SpawnTail();
	}
}

void Snake::SpawnTail()
{
	++_tailLength;
	++_score; 
	if (_bodyPos.size() == 0)
	{
		_bodyPos.push_back(spawnPos);
	}
	else
	{
		_bodyPos.push_back(_bodyPos.back() - (_bodyPos[_bodyPos.size() - 1]));
	}

	_grid[(int)_bodyPos.back().x][(int)_bodyPos.back().y].SetState(Cell::STATE::TAIL);
	_grid[(int)_bodyPos.back().x][(int)_bodyPos.back().y]._cell->SetColor(gridColor);
}


void Snake::Update(float deltaTime)
{
	_timer += deltaTime;
	
	if (_inputManager->GetKeyPress(Keycode::LEFT) && _lastDirection != glm::vec2(1.0f, 0.0f))
	{
		_direction = glm::vec2(-1.0f, 0.0f);
	}
	else if (_inputManager->GetKeyPress(Keycode::UP) && _lastDirection != glm::vec2(0.0f, -1.0f))
	{
		_direction = glm::vec2(0.0f, 1.0f);
	}
	else if (_inputManager->GetKeyPress(Keycode::RIGHT) && _lastDirection != glm::vec2(-1.0f, 0.0f))
	{
		_direction = glm::vec2(1.0f, 0.0f);
	}
	else if (_inputManager->GetKeyPress(Keycode::DOWN) && _lastDirection != glm::vec2(0.0f, 1.0f))
	{
		_direction = glm::vec2(0.0f, -1.0f);
	}

	if (_timer > (0.15f - (_tailLength * 0.002f)))
	{
		_lastDirection = _direction;
		UpdatePosition();
		_timer = 0;
	}


}

void Snake::Death()
{
	std::cout << "GAMEOVER \nYour score: " << _tailLength - 1 << std::endl;
	Restart();
}

void Snake::Restart()
{
	_tailLength = 0;
	_bodyPos.clear();
	_direction = glm::vec2(0);
	for (size_t x = 0; x < GRIDX; x++)
	{
		for (size_t y = 0; y < GRIDY; y++)
		{
			_grid[x][y].SetState(Cell::STATE::EMPTY);
			_grid[x][y]._cell->SetColor(gridColor);
		}
	}


	SpawnTail();
	SpawnFood();
}