#include "pch.h"

#include <cstdio>

extern "C" __declspec(dllexport) void __cdecl Engine_Log(const char* msg)
{
	std::printf("[C#] %s\n", msg ? msg : "<null>");
	std::fflush(stdout);
}
