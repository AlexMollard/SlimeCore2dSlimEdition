#include "DotNetHost.h"

#include "EngineExports.h"
#include "Core/Logger.h"

#include <cassert>
#include <cstdio>
#include <filesystem>
#include <vector>
#include <windows.h>

#include "coreclr_delegates.h"
#include "hostfxr.h"
#include "nethost.h"

#include "Resources/ResourceManager.h"


static std::wstring ToWide(const std::string& s)
{
	if (s.empty())
		return {};

	int len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, nullptr, 0);
	if (len <= 0)
		return {};

	std::wstring out;
	out.resize((size_t) len - 1);
	MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, out.data(), len);
	return out;
}

static std::wstring GetRuntimeConfigPath()
{
	std::string p = ResourceManager::GetInstance().GetManagedRuntimeConfigPath();
	return ToWide(p);
}

static void LogLastError(const char* what)
{
	DWORD e = GetLastError();
	char buf[512];
	std::snprintf(buf, sizeof(buf), "%s failed. GetLastError=%lu (0x%08lX)", what, e, e);
	OutputDebugStringA(buf);
	Logger::Error(buf);
}

static void LogRc(const char* what, int rc)
{
	char buf[512];
	std::snprintf(buf, sizeof(buf), "%s failed. rc=%d (0x%08X)", what, rc, (unsigned) rc);
	OutputDebugStringA(buf);
	Logger::Error(buf);
}

static hostfxr_initialize_for_runtime_config_fn init_fptr = nullptr;
static hostfxr_get_runtime_delegate_fn get_delegate_fptr = nullptr;
static hostfxr_close_fn close_fptr = nullptr;

static load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;

static void* load_library_w(const wchar_t* path)
{
	return (void*) ::LoadLibraryW(path);
}

static void* get_export(void* h, const char* name)
{
	return (void*) ::GetProcAddress((HMODULE) h, name);
}

static bool load_hostfxr()
{
	wchar_t hostfxrPath[MAX_PATH];
	size_t size = _countof(hostfxrPath);

	int rc = get_hostfxr_path(hostfxrPath, &size, nullptr);
	if (rc != 0)
	{
		LogRc("get_hostfxr_path", rc);
		return false;
	}

	HMODULE lib = LoadLibraryW(hostfxrPath);
	if (!lib)
	{
		LogLastError("LoadLibraryW(hostfxr)");
		return false;
	}

	init_fptr = (hostfxr_initialize_for_runtime_config_fn) GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
	get_delegate_fptr = (hostfxr_get_runtime_delegate_fn) GetProcAddress(lib, "hostfxr_get_runtime_delegate");
	close_fptr = (hostfxr_close_fn) GetProcAddress(lib, "hostfxr_close");

	if (!init_fptr || !get_delegate_fptr || !close_fptr)
	{
		LogLastError("GetProcAddress(hostfxr exports)");
		return false;
	}

	return true;
}

DotNetHost* DotNetHost::s_Instance = nullptr;

DotNetHost* DotNetHost::GetInstance()
{
	return s_Instance;
}

bool DotNetHost::Init()
{
	s_Instance = this;
	const std::wstring& runtimeConfigPath = GetRuntimeConfigPath();

	if (!std::filesystem::exists(runtimeConfigPath))
	{
		Logger::Error("runtimeconfig missing!");
		return false;
	}

	if (!load_hostfxr())
		return false;

	hostfxr_handle cxt = nullptr;
	int rc = init_fptr(runtimeConfigPath.c_str(), nullptr, &cxt);
	if (rc != 0 || cxt == nullptr)
	{
		LogRc("hostfxr_initialize_for_runtime_config", rc);
		return false;
	}

	m_context = cxt;

	// Get the delegate for loading an assembly and retrieving a function pointer.
	void* load_fptr = nullptr;
	rc = get_delegate_fptr(cxt, hdt_load_assembly_and_get_function_pointer, &load_fptr);
	if (rc != 0 || load_fptr == nullptr)
	{
		LogRc("hostfxr_get_runtime_delegate", rc);
		return false;
	}

	load_assembly_and_get_function_pointer = (load_assembly_and_get_function_pointer_fn) load_fptr;

	// We can close the context now; runtime stays initialized.
	close_fptr(cxt);
	m_context = nullptr;

	// Build paths: derive EngineManaged.dll path from runtimeconfig path
	// Example: ...\EngineManaged.runtimeconfig.json -> ...\EngineManaged.dll
	std::wstring managedDllPath = runtimeConfigPath;
	const std::wstring suffix = L".runtimeconfig.json";
	if (managedDllPath.size() >= suffix.size() && managedDllPath.compare(managedDllPath.size() - suffix.size(), suffix.size(), suffix) == 0)
	{
		managedDllPath.erase(managedDllPath.size() - suffix.size());
		managedDllPath += L".dll";
	}
	else
	{
		return false;
	}

	// Get ScriptRuntime_Init
	rc = load_assembly_and_get_function_pointer(managedDllPath.c_str(), L"SlimeCore.GameHost, EngineManaged", L"Init", UNMANAGEDCALLERSONLY_METHOD, nullptr, (void**) &m_init);

	if (rc != 0 || !m_init)
	{
		LogRc("load_assembly_and_get_function_pointer init", rc);
		return false;
	}

	// Get ScriptRuntime_Update
	rc = load_assembly_and_get_function_pointer(managedDllPath.c_str(), L"SlimeCore.GameHost, EngineManaged", L"Update", UNMANAGEDCALLERSONLY_METHOD, nullptr, (void**) &m_update);

	if (rc != 0 || !m_update)
	{
		LogRc("load_assembly_and_get_function_pointer update", rc);
		return false;
	}

	// Get ScriptRuntime_Draw
	rc = load_assembly_and_get_function_pointer(managedDllPath.c_str(), L"SlimeCore.GameHost, EngineManaged", L"Draw", UNMANAGEDCALLERSONLY_METHOD, nullptr, (void**) &m_draw);

	if (rc != 0 || !m_draw)
	{
		LogRc("load_assembly_and_get_function_pointer draw", rc);
		return false;
	}

	// Get ScriptRuntime_ForceGC
	rc = load_assembly_and_get_function_pointer(managedDllPath.c_str(), L"SlimeCore.GameHost, EngineManaged", L"ForceGC", UNMANAGEDCALLERSONLY_METHOD, nullptr, (void**) &m_forceGC);

	if (rc != 0 || !m_forceGC)
	{
		LogRc("load_assembly_and_get_function_pointer ForceGC", rc);
		return false;
	}

	return true;
}

void DotNetHost::Shutdown()
{
	// hostfxr doesn't provide a "shutdown runtime" API for this hosting path.
	// Typically you let process shutdown handle it.
	m_init = nullptr;
	m_update = nullptr;
	m_draw = nullptr;
	m_forceGC = nullptr;
}

void DotNetHost::CallInit()
{
	if (m_init)
		m_init();
}

void DotNetHost::CallUpdate(float dt)
{
	if (m_update)
		m_update(dt);
}

void DotNetHost::CallDraw()
{
	if (m_draw)
		m_draw();
}

void DotNetHost::CallForceGC()
{
	if (m_forceGC)
		m_forceGC();
}
