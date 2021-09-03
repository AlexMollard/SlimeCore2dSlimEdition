#include "AStar.h"

#define GRIDX 25
#define GRIDY 20

AStar::AStar(Camera* cam, Renderer2D* rend, ObjectManager* objMan)
{
	//Initializing Class Members 
	_camera = cam; 
	_renderer = rend; 
	_objManager = objMan; 
	Input::GetInstance()->SetCamera(_camera); 

	//Creating the grid 
	_grid = new Node * [GRIDX]; 
	
	for (size_t i = 0; i < GRIDX; i++)
	{
		_grid[i] = new Node[GRIDY];
	}

	float cellSize = 0.5f;
	float cellSpacing = cellSize + 0.1f; 

	for (size_t x = 0; x < GRIDX; x++)
	{
		for (size_t y = 0; y < GRIDY; y++)
		{
			//Setting everything to walkable 
			_grid[x][y].SetState(Node::STATE::WALKABLE);
			_grid[x][y].SetObject(_objManager->CreateQuad(glm::vec3((x - (GRIDX * 0.5f)) * cellSpacing, (y - (GRIDY * 0.5f)) * cellSpacing, 0.0f), glm::vec2(cellSize), _gridColor));
		}
	}
}

AStar::~AStar()
{
	for (size_t x = 0; x < GRIDX; x++)
	{
		delete[] _grid[x];
		_grid[x] = nullptr;
	}

	delete[] _grid;
	_grid = nullptr;
}