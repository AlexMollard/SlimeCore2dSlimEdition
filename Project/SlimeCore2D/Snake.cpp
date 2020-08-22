#include "Snake.h"
#include <iostream> 

#define GRIDX 10
#define GRIDY 5

Snake::Snake()
{
	srand(3245); 
	Init();

	_grid = new Cell * [GRIDX];
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

	delete _objManager;
	_objManager = nullptr;
}

void Snake::SpawnFood()
{
	int x = rand() % GRIDX;
	int y = rand() % GRIDY;

	if (_grid[x][y].GetState() == Cell::STATE::TAIL)
		SpawnFood();

	_foodPos = glm::vec2(x, y);

	_grid[x][y].SetState(Cell::STATE::FOOD);
	_grid[x][y]._cell->SetColor(glm::vec3(0.75f, 0.0f, 0.0f));
}

void Snake::SpawnHead()
{
	int x = rand() % GRIDX;
	int y = rand() % GRIDY;

	if (_grid[x][y].GetState() == Cell::STATE::HEAD)
		SpawnHead();

	//_headPos = glm::vec2(x, y); 

	_grid[x][y].SetState(Cell::STATE::HEAD);
	_grid[x][y]._cell->SetColor(glm::vec3(0.0f, 0.75f, 0.0f));
}

void Snake::UpdatePosition()
{
	int tailIndex = _tailLength - 1; 
	glm::vec2 endOfTailPosFromPrevTic = _bodyPos[tailIndex]; 

	_grid[(int)endOfTailPosFromPrevTic.x][(int)endOfTailPosFromPrevTic.y].SetState(Cell::STATE::EMPTY);
	_grid[(int)endOfTailPosFromPrevTic.x][(int)endOfTailPosFromPrevTic.y]._cell->SetColor(glm::vec3(0.5f));

	for (int i = tailIndex; i > 0; i--)
	{
		_bodyPos[i] = _bodyPos[i - 1];

		_grid[(int)_bodyPos[i].x][(int)_bodyPos[i].y].SetState(Cell::STATE::TAIL);
		_grid[(int)_bodyPos[i].x][(int)_bodyPos[i].y]._cell->SetColor(0.0f, 0.0f, 0.75f);
	}

	_bodyPos[0] += _direction; 
	_grid[(int)_bodyPos[0].x][(int)_bodyPos[0].y].SetState(Cell::STATE::TAIL);
	_grid[(int)_bodyPos[0].x][(int)_bodyPos[0].y]._cell->SetColor(0.0f, 0.0f, 0.75f);

	if (_bodyPos[0] == _foodPos)
	{
		SpawnFood();
		SpawnTail();
	}
}

void Snake::SpawnTail()
{
	++_tailLength;
	int tailIndex = _tailLength - 1;

	if (tailIndex == 0)
	{
		_bodyPos[tailIndex] = glm::vec2(5.0f, 0.0f);
	}
	else
	{
		_bodyPos[tailIndex] = (_bodyPos[tailIndex - 1] - _direction);
	}

	_grid[(int)_bodyPos[tailIndex].x][(int)_bodyPos[tailIndex].y].SetState(Cell::STATE::TAIL);
	_grid[(int)_bodyPos[tailIndex].x][(int)_bodyPos[tailIndex].y]._cell->SetColor(glm::vec3(0.0f, 0.0f, 0.75f));
}

void Snake::Update(float deltaTime)
{
	_camera->Update(deltaTime);
	_objManager->UpdateFrames(deltaTime);

	_timer += deltaTime;

	if (_inputManager->GetKeyPress(Keycode::LEFT) && _direction != glm::vec2(1.0f, 0.0f))
	{
		_direction = glm::vec2(-1.0f, 0.0f);
	}
	else if (_inputManager->GetKeyPress(Keycode::UP) && _direction != glm::vec2(0.0f, -1.0f))
	{
		_direction = glm::vec2(0.0f, 1.0f);
	}
	else if (_inputManager->GetKeyPress(Keycode::RIGHT) && _direction != glm::vec2(-1.0f, 0.0f))
	{
		_direction = glm::vec2(1.0f, 0.0f);
	}
	else if (_inputManager->GetKeyPress(Keycode::DOWN) && _direction != glm::vec2(0.0f, 1.0f))
	{
		_direction = glm::vec2(0.0f, -1.0f);
	}

	if (_timer > 0.5f)
	{
		_timer = 0;
		UpdatePosition();
	}
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