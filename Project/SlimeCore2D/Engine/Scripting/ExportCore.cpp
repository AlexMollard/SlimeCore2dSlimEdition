#include "Scripting/ExportCore.h"
#include <iostream>

SLIME_EXPORT void __cdecl Engine_Log(const char* msg)
{
	std::cout << "[C#] " << (msg ? msg : "null") << std::endl;
}
