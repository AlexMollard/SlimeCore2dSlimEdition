#include "Scripting/ExportCore.h"
#include "Core/Memory.h"

SLIME_EXPORT void __cdecl Memory_PushContext(const char* context)
{
    MemoryAllocator::PushContext(context);
}

SLIME_EXPORT void __cdecl Memory_PopContext()
{
    MemoryAllocator::PopContext();
}
