#pragma once
#include <string>

class DotNetHost
{
public:
	static DotNetHost* GetInstance();

	bool Init();
	void Shutdown();

	void CallInit();
	void CallUpdate(float dt);
	void CallDraw();
	void CallForceGC();

private:
	static DotNetHost* s_Instance;

	using init_fn = void(__cdecl*)();
	using update_fn = void(__cdecl*)(float);
	using draw_fn = void(__cdecl*)();
	using force_gc_fn = void(__cdecl*)();

	init_fn m_init = nullptr;
	update_fn m_update = nullptr;
	draw_fn m_draw = nullptr;
	force_gc_fn m_forceGC = nullptr;

	void* m_hostfxr = nullptr; // HMODULE
	void* m_context = nullptr; // hostfxr_handle (opaque)
};
