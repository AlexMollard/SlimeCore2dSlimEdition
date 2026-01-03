#include "ResourceManager.h"

#include "Core/Logger.h"
#include "Rendering/Shader.h"
#include "Rendering/Text.h"
#include "Rendering/Texture.h"
//#include "Core/Memory.h"

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

// -----------------------------------------------------------------------------
// HELPER FUNCTIONS (Platform Abstraction)
// -----------------------------------------------------------------------------

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

static std::string ToLower(const std::string& s)
{
	std::string out = s;
	std::transform(out.begin(), out.end(), out.begin(), [](unsigned char c) { return std::tolower(c); });
	return out;
}

// Get the directory containing the running executable
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

// -----------------------------------------------------------------------------
// RESOURCE MANAGER IMPLEMENTATION
// -----------------------------------------------------------------------------

ResourceManager::ResourceManager()
{
}

ResourceManager::~ResourceManager()
{
}

ResourceManager& ResourceManager::GetInstance()
{
	static ResourceManager instance;
	return instance;
}

// -----------------------------------------------------------------------------
// SHADERS
// -----------------------------------------------------------------------------

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
		candidates.push_back(exeDir + "\\Resources\\Shaders");
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
		Logger::Error("ResourceManager: no shader directory found (tried " + std::to_string(candidates.size()) + " locations)");
		return false;
	}

	Logger::Info("ResourceManager: using shader directory: " + chosen);

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
		Logger::Warn("ResourceManager: directory exists but no files found: " + chosen);
		return false;
	}

	do
	{
		if (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
			continue;

		std::string filename = findData.cFileName;
		auto lower = ToLower(filename);
		std::string filepath = chosen + "\\" + filename;

		// Simple heuristic to pair .vert and .frag files (and now .hlsl and .glsl)
		if (lower.find("vertex.shader") != std::string::npos || lower.find("vert.shader") != std::string::npos || lower.find("vertex.hlsl") != std::string::npos || lower.find("vertex.glsl") != std::string::npos)
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
		else if (lower.find("fragment.shader") != std::string::npos || lower.find("frag.shader") != std::string::npos || lower.find("pixel.hlsl") != std::string::npos || lower.find("frag.hlsl") != std::string::npos || lower.find("pixel.glsl") != std::string::npos || lower.find("frag.glsl") != std::string::npos)
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
				else if (lowbase.size() >= 5 && lowbase.substr(lowbase.size() - 5) == "pixel")
				{
					base = base.substr(0, base.size() - 5);
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
	DIR* dirPtr = opendir(chosen.c_str());
	if (!dirPtr)
	{
		Logger::Error("ResourceManager: directory exists but could not open: " + chosen + " (" + strerror(errno) + ")");
		return false;
	}

	struct dirent* ent;
	while ((ent = readdir(dirPtr)) != NULL)
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
					base = base.substr(0, base.size() - 6);
				else if (lowbase.size() >= 4 && lowbase.substr(lowbase.size() - 4) == "vert")
					base = base.substr(0, base.size() - 4);
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
					base = base.substr(0, base.size() - 8);
				else if (lowbase.size() >= 4 && lowbase.substr(lowbase.size() - 4) == "frag")
					base = base.substr(0, base.size() - 4);
				else
					break;
			}
			fmap[ToLower(base)] = filepath;
		}
	}

	closedir(dirPtr);
#endif

	for (auto& kv: vmap)
	{
		auto base = kv.first;
		if (fmap.find(base) != fmap.end())
		{
			AddShader(base, vmap[base], fmap[base]);
			Logger::Info("ResourceManager: loaded shader: " + base);
		}
		else
		{
			Logger::Warn("ResourceManager: vertex shader without fragment: " + kv.second);
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

// -----------------------------------------------------------------------------
// TEXTURES
// -----------------------------------------------------------------------------

Texture* ResourceManager::LoadTexture(const std::string& name, const std::string& relativePath)
{
	std::string key = ToLower(name);

	// 1. Check if already loaded
	auto it = m_textures.find(key);
	if (it != m_textures.end())
	{
		return it->second;
	}

	// 2. Resolve Path
	// If relativePath is empty, we assume 'name' is the path
	std::string searchPath = relativePath.empty() ? name : relativePath;
	std::string fullPath = GetResourcePath(searchPath);

	if (fullPath.empty())
	{
		Logger::Error("ResourceManager: Failed to locate texture: " + searchPath);
		return nullptr;
	}

	// 3. Load Texture
	// Using default settings (Nearest Neighbor, Clamp) - could be parameterized if needed
	Texture* tex = new Texture(fullPath, Texture::Filter::Nearest, Texture::Wrap::ClampToEdge);

	// Check for load failure (usually width 0)
	if (tex->GetWidth() == 0)
	{
		Logger::Error("ResourceManager: Failed to load texture data from: " + fullPath);
		delete tex;
		return nullptr;
	}

	// 4. Store
	m_textures[key] = tex;
	Logger::Info("ResourceManager: Loaded Texture '" + key + "' (" + std::to_string(tex->GetWidth()) + "x" + std::to_string(tex->GetHeight()) + ")");

	return tex;
}

Texture* ResourceManager::GetTexture(const std::string& name)
{
	std::string key = ToLower(name);
	auto it = m_textures.find(key);
	if (it != m_textures.end())
		return it->second;
	return nullptr;
}

// -----------------------------------------------------------------------------
// FONTS (SDF)
// -----------------------------------------------------------------------------

Text* ResourceManager::LoadFont(const std::string& name, const std::string& relativePath, int fontSize)
{
	std::string key = ToLower(name);

	// 1. Check if already loaded
	auto it = m_fonts.find(key);
	if (it != m_fonts.end())
	{
		return it->second;
	}

	// 2. Resolve Path
	std::string searchPath = relativePath.empty() ? name : relativePath;
	std::string fullPath = GetResourcePath(searchPath);

	if (fullPath.empty())
	{
		Logger::Error("ResourceManager: Failed to locate font: " + searchPath);
		return nullptr;
	}

	// 3. Load Font
	// This generates the SDF atlas
	Text* font = new Text(fullPath, fontSize);

	// 4. Store
	m_fonts[key] = font;
	Logger::Info("ResourceManager: Loaded Font '" + key + "' from " + fullPath);

	return font;
}

Text* ResourceManager::GetFont(const std::string& name)
{
	std::string key = ToLower(name);
	auto it = m_fonts.find(key);
	if (it != m_fonts.end())
		return it->second;
	return nullptr;
}

#include <fstream>
#include <sstream>

// -----------------------------------------------------------------------------
// TEXT DATA MANAGEMENT
// -----------------------------------------------------------------------------

const char* ResourceManager::LoadText(const std::string& name, const std::string& relativePath)
{
	std::string key = ToLower(name);

	// 1. Check if already loaded
	auto it = m_textFiles.find(key);
	if (it != m_textFiles.end())
	{
		return it->second.c_str();
	}

	// 2. Resolve Path
	std::string searchPath = relativePath.empty() ? name : relativePath;
	std::string fullPath = GetResourcePath(searchPath);

	if (fullPath.empty())
	{
		Logger::Error("ResourceManager: Failed to locate text file: " + searchPath);
		return nullptr;
	}

	// 3. Load File
	std::ifstream file(fullPath);
	if (!file.is_open())
	{
		Logger::Error("ResourceManager: Failed to open text file: " + fullPath);
		return nullptr;
	}

	std::stringstream buffer;
	buffer << file.rdbuf();
	std::string content = buffer.str();

	// 4. Store
	m_textFiles[key] = content;
	Logger::Info("ResourceManager: Loaded Text '" + key + "' from " + fullPath);

	// Return pointer to the string stored in the map
	return m_textFiles[key].c_str();
}

// -----------------------------------------------------------------------------
// PATH RESOLUTION
// -----------------------------------------------------------------------------

std::string ResourceManager::GetResourcePath(const std::string& relativePath)
{
	if (relativePath.empty())
		return std::string();

	// If absolute path, return immediately if exists
#if defined(_WIN32)
	// Simple check for drive letter (C:\)
	if (relativePath.length() > 1 && relativePath[1] == ':')
	{
		if (FileExists(relativePath))
			return relativePath;
	}
#else
	if (relativePath.length() > 0 && relativePath[0] == '/')
	{
		if (FileExists(relativePath))
			return relativePath;
	}
#endif

	std::vector<std::string> candidates;
	std::string exeDir = GetExecutableDir();

	// 1. Executable / staged directories
	if (!exeDir.empty())
	{
		candidates.push_back(exeDir + PATH_SEP + relativePath);
		candidates.push_back(exeDir + PATH_SEP + "Game" + PATH_SEP + "Resources" + PATH_SEP + relativePath);
		candidates.push_back(exeDir + PATH_SEP + "Resources" + PATH_SEP + relativePath);
	}

	// 2. Project relative (Development mode)
	candidates.push_back(relativePath); // Working directory
	candidates.push_back(".." + std::string(1, PATH_SEP) + relativePath);
	candidates.push_back("Game" + std::string(1, PATH_SEP) + "Resources" + std::string(1, PATH_SEP) + relativePath);

	for (auto& p: candidates)
	{
		if (FileExists(p))
		{
			return p;
		}
	}

	// Not found
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

	Logger::Error("ResourceManager: EngineManaged.runtimeconfig.json not found. Tried:");
	for (const auto& p: candidates)
		Logger::Error("  " + p);

	return std::string();
}

std::string ResourceManager::GetScriptingPath(const std::string& relativeToPublishRoot)
{
	// Helper to find the base publish directory, then append the file
	std::string configPath = GetManagedRuntimeConfigPath();
	if (configPath.empty())
		return std::string();

	// Strip filename to get directory
	size_t lastSlash = configPath.find_last_of("/\\");
	std::string dir = (lastSlash == std::string::npos) ? "" : configPath.substr(0, lastSlash);

	return dir + PATH_SEP + relativeToPublishRoot;
}

// -----------------------------------------------------------------------------
// CLEANUP
// -----------------------------------------------------------------------------

void ResourceManager::Clear()
{
	// 1. Shaders
	for (auto& kv: m_shaders)
	{
		delete kv.second;
	}
	m_shaders.clear();

	// 2. Textures
	for (auto& kv: m_textures)
	{
		delete kv.second;
	}
	m_textures.clear();

	// 3. Fonts
	for (auto& kv: m_fonts)
	{
		delete kv.second;
	}
	m_fonts.clear();

	// 4. Text Files
	m_textFiles.clear();

	Logger::Info("ResourceManager: Resources cleared.");
}
