#pragma once

#include <string>
#include <unordered_map>

class Shader;

class ResourceManager
{
public:
	static ResourceManager& GetInstance();

	// Scan a directory for shaders and load matching vertex/fragment pairs.
	// If `dir` is empty, the ResourceManager will try a set of candidate locations
	// including next to the executable (e.g. <exe_dir>\\Game\\Resources\\Shaders) and
	// project-relative paths such as ..\\Shaders or Game\\Resources\\Shaders.
	bool LoadShadersFromDir(const std::string& dir = "");

	// Retrieve a loaded shader by name (case-insensitive)
	Shader* GetShader(const std::string& name);

	// Add a shader explicitly
	void AddShader(const std::string& name, const std::string& vertexPath, const std::string& fragmentPath);

	// Return a full path to a resource given a relative path (e.g. "Fonts\\Chilanka-Regular.ttf").
	// Returns empty string if the resource isn't found.
	std::string GetResourcePath(const std::string& relativePath);

	// Return full path to EngineManaged.runtimeconfig.json in the staged folder.
	// Returns empty string if not found.
	std::string GetManagedRuntimeConfigPath();
	std::string GetScriptingPath(const std::string& relativeToPublishRoot);

	// Remove all loaded resources
	void Clear();

private:
	ResourceManager();
	~ResourceManager();

	std::unordered_map<std::string, Shader*> m_shaders; // key: lowercase name
};
