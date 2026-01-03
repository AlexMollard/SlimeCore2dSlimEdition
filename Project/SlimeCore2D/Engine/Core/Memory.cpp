#define SLIME_MEMORY_IMPL
#include "Memory.h"

#include <cstring>
#include <iomanip>
#include <iostream>
#include <map>
#include <mutex>
#include <sstream>
#include <vector>

// ! Do not use the logger in this file to avoid recursion !

template<typename T>
struct MallocAllocator
{
	using value_type = T;
	MallocAllocator() = default;

	template<typename U>
	MallocAllocator(const MallocAllocator<U>&)
	{
	}

	T* allocate(size_t n)
	{
		if (n > std::size_t(-1) / sizeof(T))
			throw std::bad_alloc();
		if (auto p = static_cast<T*>(std::malloc(n * sizeof(T))))
			return p;
		throw std::bad_alloc();
	}

	void deallocate(T* p, size_t)
	{
		std::free(p);
	}
};

static std::map<void*, AllocationInfo, std::less<void*>, MallocAllocator<std::pair<void* const, AllocationInfo>>> s_Allocations;
static std::mutex s_Mutex;
static bool s_Initialized = false;
static bool s_InsideAllocator = false;
static thread_local std::vector<const char*> s_ContextStack;

static std::string FormatBytes(size_t bytes)
{
	static const char* suffixes[] = { "B", "KB", "MB", "GB", "TB" };
	int index = 0;
	double size = static_cast<double>(bytes);

	while (size >= 1024.0 && index < 4)
	{
		size /= 1024.0;
		index++;
	}

	std::stringstream ss;
	ss << std::fixed << std::setprecision(2) << size << " " << suffixes[index];
	return ss.str();
}

void MemoryAllocator::Init()
{
	s_Initialized = true;
}

void MemoryAllocator::PushContext(const char* context)
{
	if (context)
	{
		s_ContextStack.push_back(_strdup(context));
	}
}

void MemoryAllocator::PopContext()
{
	if (!s_ContextStack.empty())
	{
		const char* ctx = s_ContextStack.back();
		s_ContextStack.pop_back();
		free((void*) ctx);
	}
}

void* MemoryAllocator::Allocate(size_t size, const char* file, int line)
{
	void* ptr = malloc(size);
	if (s_Initialized && !s_InsideAllocator)
	{
		s_InsideAllocator = true;
		{
			std::lock_guard<std::mutex> lock(s_Mutex);
			char* ctx = nullptr;
			if (!s_ContextStack.empty())
			{
				// Calculate total length
				size_t totalLen = 0;
				for (const auto& c: s_ContextStack)
				{
					totalLen += strlen(c) + 4; // " -> "
				}

				ctx = (char*) malloc(totalLen + 1);
				if (ctx)
				{
					ctx[0] = '\0';
					for (size_t i = 0; i < s_ContextStack.size(); ++i)
					{
						strcat_s(ctx, totalLen + 1, s_ContextStack[i]);
						if (i < s_ContextStack.size() - 1)
						{
							strcat_s(ctx, totalLen + 1, " -> ");
						}
					}
				}
			}
			s_Allocations[ptr] = { size, file, line, ctx };
		}
		s_InsideAllocator = false;
	}
	return ptr;
}

void* MemoryAllocator::Allocate(size_t size)
{
	return Allocate(size, "Unknown", 0);
}

void MemoryAllocator::Free(void* ptr)
{
	if (!ptr)
		return;

	if (s_Initialized && !s_InsideAllocator)
	{
		s_InsideAllocator = true;
		{
			std::lock_guard<std::mutex> lock(s_Mutex);
			auto it = s_Allocations.find(ptr);
			if (it != s_Allocations.end())
			{
				if (it->second.context)
				{
					free((void*) it->second.context);
				}
				s_Allocations.erase(it);
			}
		}
		s_InsideAllocator = false;
	}
	free(ptr);
}

void MemoryAllocator::PrintLeaks()
{
	// Disable tracking during print to avoid noise if print allocates
	bool wasInitialized = s_Initialized;
	s_Initialized = false;

	std::lock_guard<std::mutex> lock(s_Mutex);
	if (s_Allocations.empty())
	{
		std::cout << "No memory leaks detected." << std::endl;
	}
	else
	{
		std::cout << "Memory Leaks Detected:" << std::endl;
		size_t totalSize = 0;
		for (const auto& kv: s_Allocations)
		{
			const auto& alloc = kv.second;
			std::cout << "Leak at " << kv.first << ": " << FormatBytes(alloc.size);
			if (alloc.file && std::string(alloc.file) != "Unknown")
			{
				std::cout << " in " << alloc.file << ":" << alloc.line;
			}
			if (alloc.context)
			{
				std::cout << " [Context: " << alloc.context << "]";
			}
			std::cout << std::endl;
			totalSize += alloc.size;
		}
		std::cout << "Total Leaked: " << FormatBytes(totalSize) << " (" << totalSize << " bytes)" << std::endl;
	}

	s_Initialized = wasInitialized;
}

void* operator new(size_t size)
{
	return MemoryAllocator::Allocate(size);
}

void* operator new(size_t size, const char* file, int line)
{
	return MemoryAllocator::Allocate(size, file, line);
}

void* operator new[](size_t size)
{
	return MemoryAllocator::Allocate(size);
}

void* operator new[](size_t size, const char* file, int line)
{
	return MemoryAllocator::Allocate(size, file, line);
}

void operator delete(void* ptr) noexcept
{
	MemoryAllocator::Free(ptr);
}

void operator delete(void* ptr, const char* file, int line) noexcept
{
	MemoryAllocator::Free(ptr);
}

void operator delete[](void* ptr) noexcept
{
	MemoryAllocator::Free(ptr);
}

void operator delete[](void* ptr, const char* file, int line) noexcept
{
	MemoryAllocator::Free(ptr);
}
