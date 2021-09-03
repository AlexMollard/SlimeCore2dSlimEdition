#pragma once
#include "ObjectManager.h"
class Node
{
public:
	enum class STATE
	{
		WALKABLE,
		OBSTICAL,
		CLOSED,
		START,
		END
	};

	Node();
	~Node(); 

	void SetState(STATE newState) { _currentState = newState; }
	void SetObject(GameObject* newObject) { _object = newObject; }
private:
	//Distance from starting Node 
	int _gCost = 0;
	//Distance from end node 
	int _hCost = 0;
	//g cost + h cost 
	int _fCost = 0;

	STATE _currentState; 
	GameObject* _object; 
};

