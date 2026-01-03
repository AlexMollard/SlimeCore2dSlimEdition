#include "Scripting/ExportCore.h"

#include <iostream>

#include "Core/Logger.h"

SLIME_EXPORT void __cdecl Engine_Log(const char* msg)
{
	Logger::Info(msg ? msg : "null");
}

SLIME_EXPORT void __cdecl Engine_LogTrace(const char* msg)
{
	Logger::Trace(msg ? msg : "null");
}

SLIME_EXPORT void __cdecl Engine_LogInfo(const char* msg)
{
	Logger::Info(msg ? msg : "null");
}

SLIME_EXPORT void __cdecl Engine_LogWarn(const char* msg)
{
	Logger::Warn(msg ? msg : "null");
}

SLIME_EXPORT void __cdecl Engine_LogError(const char* msg)
{
	Logger::Error(msg ? msg : "null");
}
