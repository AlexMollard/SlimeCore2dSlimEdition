#include <cstdio>

#include "pch.h"

// This is a separate DLL, so it might not have access to the main engine's Logger class easily
// unless we link it. For now, let's just keep it as is or use a simple print if it's just for debugging the DLL itself.
// However, the user asked to replace ALL console printing.
// If this DLL is loaded by the engine, it should probably use the engine's exports if possible,
// but that's circular.
// Actually, this file seems to BE the implementation of Engine_Log for some cases?
// Wait, I already updated ExportCore.cpp in the main project.
// Let's see where SlimeScriptingNative is used.
