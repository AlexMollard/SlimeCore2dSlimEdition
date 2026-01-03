#pragma once
#include <iostream>
#include <map>
#include <mutex>
#include <string>
#include <vector>

struct AllocationInfo
{
	size_t size;
	const char* file;
	int line;
	const char* context;
};

class MemoryAllocator
{
public:
	static void* Allocate(size_t size, const char* file, int line);
	static void* Allocate(size_t size);
	static void Free(void* ptr);
	static void PrintLeaks();
	static void Init();

	static void PushContext(const char* context);
	static void PopContext();
};

// Overload global new/delete
void* operator new(size_t size);
void* operator new(size_t size, const char* file, int line);
void* operator new[](size_t size);
void* operator new[](size_t size, const char* file, int line);
void operator delete(void* ptr) noexcept;
void operator delete(void* ptr, const char* file, int line) noexcept;
void operator delete[](void* ptr) noexcept;
void operator delete[](void* ptr, const char* file, int line) noexcept;

#ifndef SLIME_MEMORY_IMPL
#	define new new (__FILE__, __LINE__)
#endif
