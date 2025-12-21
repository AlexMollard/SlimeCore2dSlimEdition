#include "ResourceManager.h"

#include "Engine/Rendering/Shader.h"

#if defined(_WIN32)
#	include <windows.h>
#else
#	include <dirent.h>
#	include <limits.h>
#	include <sys/stat.h>
#	include <unistd.h>
#endif

#include <algorithm>
#include <cctype>
#include <cerrno>
#include <cstring>
#include <iostream>
#include <string>
#include <vector>

#if defined(_WIN32)
static const char PATH_SEP = '\\';
#else
static const char PATH_SEP = '/';
#endif

static bool FileExists(const std::string& path)
{
#if defined(_WIN32)
	DWORD attrs = GetFileAttributesA(path.c_str());
	return (attrs != INVALID_FILE_ATTRIBUTES) && !(attrs & FILE_ATTRIBUTE_DIRECTORY);
#else
	struct stat st;
	return (stat(path.c_str(), &st) == 0) && !S_ISDIR(st.st_mode);
#endif
}

static std::string ToLower(const std::string& s)
{
	std::string out = s;
	std::transform(out.begin(), out.end(), out.begin(), [](unsigned char c) { return std::tolower(c); });
	return out;
}

ResourceManager::ResourceManager()
{
}

ResourceManager::~ResourceManager()
{
	Clear();
}

ResourceManager& ResourceManager::GetInstance()
{
	static ResourceManager instance;
	return instance;
}

// Helper: check whether a path exists and is a directory
static bool DirectoryExists(const std::string& path)
{
#if defined(_WIN32)
	DWORD attrs = GetFileAttributesA(path.c_str());
	return (attrs != INVALID_FILE_ATTRIBUTES) && (attrs & FILE_ATTRIBUTE_DIRECTORY);
#else
	struct stat st;
	if (stat(path.c_str(), &st) != 0)
		return false;
	return S_ISDIR(st.st_mode);
#endif
}

// Helper: get the directory that contains the running executable
static std::string GetExecutableDir()
{
#if defined(_WIN32)
	char buf[MAX_PATH];
	DWORD len = GetModuleFileNameA(NULL, buf, MAX_PATH);
	if (len == 0 || len == MAX_PATH)
		return std::string();
	std::string p(buf, buf + len);
	auto pos = p.find_last_of("\\/");
	if (pos == std::string::npos)
		return std::string();
	return p.substr(0, pos);
#else
	// Try readlink on /proc/self/exe (Linux). If not available, fall back to getcwd.
	char buf[PATH_MAX];
	ssize_t len = readlink("/proc/self/exe", buf, sizeof(buf) - 1);
	if (len > 0)
	{
		buf[len] = '\0';
		std::string p(buf);
		auto pos = p.find_last_of('/');
		if (pos != std::string::npos)
			return p.substr(0, pos);
	}

	char cwd[PATH_MAX];
	if (getcwd(cwd, sizeof(cwd)))
	{
		return std::string(cwd);
	}

	return std::string();
#endif
}

bool ResourceManager::LoadShadersFromDir(const std::string& dir)
{
	// Build a list of candidate directories in preferred order
	std::vector<std::string> candidates;

	if (!dir.empty())
		candidates.push_back(dir);

	std::string exeDir = GetExecutableDir();
	if (!exeDir.empty())
	{
		candidates.push_back(exeDir + "\\Game\\Resources\\Shaders");
		candidates.push_back(exeDir + "\\Game\\Resources");
		candidates.push_back(exeDir + "\\Shaders");
	}

	// Project-relative fallbacks
	candidates.push_back("..\\Shaders");
	candidates.push_back("Game\\Resources\\Shaders");
	candidates.push_back("Game\\Resources");

	std::string chosen;
	for (auto& c: candidates)
	{
		if (DirectoryExists(c))
		{
			chosen = c;
			break;
		}
	}

	if (chosen.empty())
	{
		std::cout << "ResourceManager: no shader directory found (tried " << candidates.size() << " locations)" << std::endl;
		return false;
	}

	std::cout << "ResourceManager: using shader directory: " << chosen << std::endl;

	std::unordered_map<std::string, std::string> vmap;
	std::unordered_map<std::string, std::string> fmap;

#if defined(_WIN32)
	std::string search = chosen;
	if (!search.empty() && (search.back() == '\\' || search.back() == '/'))
		search.pop_back();
	search += "\\*";

	WIN32_FIND_DATAA findData;
	HANDLE hFind = FindFirstFileA(search.c_str(), &findData);
	if (hFind == INVALID_HANDLE_VALUE)
	{
		std::cout << "ResourceManager: directory exists but no files found: " << chosen << std::endl;
		return false;
	}

	do
	{
		if (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
			continue;

		std::string filename = findData.cFileName;
		auto lower = ToLower(filename);
		std::string filepath = chosen + "\\" + filename;

		if (lower.find("vertex.shader") != std::string::npos || lower.find("vert.shader") != std::string::npos)
		{
			std::string base = filename.substr(0, filename.find_last_of('.'));
			while (true)
			{
				auto lowbase = ToLower(base);
				if (lowbase.size() >= 6 && lowbase.substr(lowbase.size() - 6) == "vertex")
				{
					base = base.substr(0, base.size() - 6);
				}
				else if (lowbase.size() >= 4 && lowbase.substr(lowbase.size() - 4) == "vert")
				{
					base = base.substr(0, base.size() - 4);
				}
				else
					break;
			}
			vmap[ToLower(base)] = filepath;
		}
		else if (lower.find("fragment.shader") != std::string::npos || lower.find("frag.shader") != std::string::npos)
		{
			std::string base = filename.substr(0, filename.find_last_of('.'));
			while (true)
			{
				auto lowbase = ToLower(base);
				if (lowbase.size() >= 8 && lowbase.substr(lowbase.size() - 8) == "fragment")
				{
					base = base.substr(0, base.size() - 8);
				}
				else if (lowbase.size() >= 4 && lowbase.substr(lowbase.size() - 4) == "frag")
				{
					base = base.substr(0, base.size() - 4);
				}
				else
					break;
			}
			fmap[ToLower(base)] = filepath;
		}
	}
	while (FindNextFileA(hFind, &findData));

	FindClose(hFind);
#else
	DIR* dir = opendir(chosen.c_str());
	if (chosen.empty())
	{
		std::cout << "ResourceManager: directory exists but could not open: " << chosen << " (" << strerror(errno) << ")" << std::endl;
		return false;
	}

	struct dirent* ent;
	while ((ent = readdir(dir)) != NULL)
	{
		std::string filename = ent->d_name;
		std::string filepath = chosen + "/" + filename;

		struct stat st;
		if (stat(filepath.c_str(), &st) != 0)
			continue;
		if (S_ISDIR(st.st_mode))
			continue;

		auto lower = ToLower(filename);

		if (lower.find("vertex.shader") != std::string::npos || lower.find("vert.shader") != std::string::npos)
		{
			std::string base = filename.substr(0, filename.find_last_of('.'));
			while (true)
			{
				auto lowbase = ToLower(base);
				if (lowbase.size() >= 6 && lowbase.substr(lowbase.size() - 6) == "vertex")
				{
					base = base.substr(0, base.size() - 6);
				}
				else if (lowbase.size() >= 4 && lowbase.substr(lowbase.size() - 4) == "vert")
				{
					base = base.substr(0, base.size() - 4);
				}
				else
					break;
			}
			vmap[ToLower(base)] = filepath;
		}
		else if (lower.find("fragment.shader") != std::string::npos || lower.find("frag.shader") != std::string::npos)
		{
			std::string base = filename.substr(0, filename.find_last_of('.'));
			while (true)
			{
				auto lowbase = ToLower(base);
				if (lowbase.size() >= 8 && lowbase.substr(lowbase.size() - 8) == "fragment")
				{
					base = base.substr(0, base.size() - 8);
				}
				else if (lowbase.size() >= 4 && lowbase.substr(lowbase.size() - 4) == "frag")
				{
					base = base.substr(0, base.size() - 4);
				}
				else
					break;
			}
			fmap[ToLower(base)] = filepath;
		}
	}

	closedir(dir);
#endif

	for (auto& kv: vmap)
	{
		auto base = kv.first;
		if (fmap.find(base) != fmap.end())
		{
			AddShader(base, vmap[base], fmap[base]);
			std::cout << "ResourceManager: loaded shader: " << base << " (" << vmap[base] << ", " << fmap[base] << ")" << std::endl;
		}
		else
		{
			std::cout << "ResourceManager: vertex shader without fragment: " << kv.second << std::endl;
		}
	}

	return true;
}

void ResourceManager::AddShader(const std::string& name, const std::string& vertexPath, const std::string& fragmentPath)
{
	auto key = ToLower(name);
	if (m_shaders.find(key) != m_shaders.end())
		return; // already have it

	Shader* s = new Shader(name, vertexPath.c_str(), fragmentPath.c_str());
	m_shaders[key] = s;
}

Shader* ResourceManager::GetShader(const std::string& name)
{
	auto key = ToLower(name);
	auto it = m_shaders.find(key);
	if (it != m_shaders.end())
		return it->second;
	return nullptr;
}

// Attempt to locate a resource file by searching candidate directories (exe dir, project fallbacks)
std::string ResourceManager::GetResourcePath(const std::string& relativePath)
{
	if (relativePath.empty())
		return std::string();

	std::vector<std::string> candidates;

	// Prefer the executable directory
	std::string exeDir = GetExecutableDir();
	if (!exeDir.empty())
	{
		candidates.push_back(exeDir + "\\" + relativePath);
		candidates.push_back(exeDir + "\\Game\\Resources\\" + relativePath);
		candidates.push_back(exeDir + "\\Resources\\" + relativePath);
	}

	// Project relative fallbacks
	candidates.push_back(relativePath);
	candidates.push_back("..\\" + relativePath);
	candidates.push_back("Game\\Resources\\" + relativePath);

	for (auto& p: candidates)
	{
#if defined(_WIN32)
		DWORD attrs = GetFileAttributesA(p.c_str());
		if (attrs != INVALID_FILE_ATTRIBUTES && !(attrs & FILE_ATTRIBUTE_DIRECTORY))
		{
			return p;
		}
#else
		struct stat st;
		if (stat(p.c_str(), &st) == 0 && !S_ISDIR(st.st_mode))
		{
			return p;
		}
#endif
	}

	return std::string();
}

std::string ResourceManager::GetManagedRuntimeConfigPath()
{
#if defined(_DEBUG)
	const char* cfg = "Debug";
#else
	const char* cfg = "Release";
#endif

	const std::string rel = std::string("Scripting\\Publish\\") + cfg + "\\EngineManaged.runtimeconfig.json";

	std::vector<std::string> candidates;

	std::string exeDir = GetExecutableDir();
	if (!exeDir.empty())
	{
		// Common layout: <Project>\x64\Debug  -> go up to <Project>
		candidates.push_back(exeDir + "\\..\\..\\" + rel);

		// Fallbacks if output folder differs
		candidates.push_back(exeDir + "\\..\\" + rel);
		candidates.push_back(exeDir + "\\" + rel);

		// If you ever run from a packaged folder where Scripting sits next to exe:
		candidates.push_back(exeDir + "\\Scripting\\Publish\\" + cfg + "\\EngineManaged.runtimeconfig.json");
	}

	// Also allow running from repo root (working dir based)
	candidates.push_back(rel);

	for (const auto& p: candidates)
	{
		if (FileExists(p))
			return p;
	}

	std::cout << "ResourceManager: EngineManaged.runtimeconfig.json not found. Tried:\n";
	for (const auto& p: candidates)
		std::cout << "  " << p << "\n";

	return std::string();
}

void ResourceManager::Clear()
{
	// Clear resource registrations; ResourceManager does not delete shader objects.
	m_shaders.clear();
}
