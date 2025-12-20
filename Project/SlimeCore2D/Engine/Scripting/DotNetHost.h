#pragma once
#include <string>

class DotNetHost
{
public:
	bool Init();
	void Shutdown();

	void CallInit();
	void CallUpdate(float dt);

private:
	using init_fn = void(__cdecl*)();
	using update_fn = void(__cdecl*)(float);

	init_fn m_init = nullptr;
	update_fn m_update = nullptr;

	void* m_hostfxr = nullptr; // HMODULE
	void* m_context = nullptr; // hostfxr_handle (opaque)
};
