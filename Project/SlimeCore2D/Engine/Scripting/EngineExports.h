#pragma once
#include <cstdint>

// DLL Export Macro
#if defined(_WIN32)
#	define SLIME_EXPORT extern "C" __declspec(dllexport)
#else
#	define SLIME_EXPORT extern "C"
#endif

using EntityId = std::uint64_t;

// =================================================================================
// C# SCRIPTING EXPORTS (DLL API)
// =================================================================================

#include "Scripting/ExportCore.h"
#include "Scripting/ExportEntity.h"
#include "Scripting/ExportInput.h"
#include "Scripting/ExportResource.h"
#include "Scripting/ExportScene.h"
#include "Scripting/ExportText.h"
#include "Scripting/ExportUI.h"

