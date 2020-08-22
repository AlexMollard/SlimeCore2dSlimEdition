#include "Snake.h"
#include <iostream> 

#define GRIDX 10
#define GRIDY 5

Snake::Snake()
{
	Init();

	_grid = new Cell* [GRIDX];
	for (size_t i = 0; i < GRIDX; i++)
	{
		_grid[i] = new Cell[GRIDY];
	}

	for (size_t x = 0; x < GRIDX; x++)
	{
		for (size_t y = 0; y < GRIDY; y++)
		{
			_grid[x][y].SetState(Cell::STATE::EMPTY);
			_grid[x][y]._cell = _objManager->CreateQuad(glm::vec3(x - (GRIDX * 0.5f), y - (GRIDY * 0.5f), 0.0f), glm::vec2(0.9f), glm::vec3(0.5f));
		}
	}

	SpawnFood(); 
	SpawnSnake(); 
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

	delete _objManager;
	_objManager = nullptr;
}

void Snake::SpawnFood()
{
	int x = rand() % GRIDX;
	int y = rand() % GRIDY;

	if (_grid[x][y].GetState() == Cell::STATE::HEAD || _grid[x][y].GetState() == Cell::STATE::TAIL)
		SpawnFood(); 

	_grid[x][y].SetState(Cell::STATE::FOOD);
	_grid[x][y]._cell->SetColor(glm::vec3(0.75f, 0.0f, 0.0f));
}

void Snake::SpawnSnake()
{
	int x = rand() % GRIDX;
	int y = rand() % GRIDY;

	if (_grid[x][y].GetState() == Cell::STATE::FOOD)
		SpawnSnake();

	_grid[x][y].SetState(Cell::STATE::FOOD);
	_grid[x][y]._cell->SetColor(glm::vec3(0.0f, 0.75f, 0.0f));
}

void Snake::Update(float deltaTime)
{
	_camera->Update(deltaTime);
	_objManager->UpdateFrames(deltaTime);
}
void Snake::Draw()
{
	_renderer->Draw();
}

void Snake::Init()
{
	_camera = new Camera(-16, -9, -1, 1);
	_renderer = new Renderer2D(_camera);
	_objManager = new ObjectManager(_renderer);
	Input::GetInstance()->SetCamera(_camera);
}