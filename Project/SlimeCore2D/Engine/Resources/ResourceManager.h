#pragma once

#include <string>
#include <unordered_map>

// Forward declarations to avoid including full headers here
class Shader;
class Texture;
class Text;

class ResourceManager
{
public:
	// Singleton Access
	static ResourceManager& GetInstance();

	// -------------------------------------------------------------------------
	// SHADER MANAGEMENT
	// -------------------------------------------------------------------------

	// Scan a directory for shaders and load matching vertex/fragment pairs.
	// If `dir` is empty, it searches default locations (e.g., "Shaders/", "Game/Resources/Shaders").
	bool LoadShadersFromDir(const std::string& dir = "");

	// Retrieve a loaded shader by name (case-insensitive)
	Shader* GetShader(const std::string& name);

	// Add a shader explicitly
	void AddShader(const std::string& name, const std::string& vertexPath, const std::string& fragmentPath);

	// -------------------------------------------------------------------------
	// TEXTURE MANAGEMENT
	// -------------------------------------------------------------------------

	// Loads a texture from disk.
	// 'name': The unique key to access this texture later (case-insensitive).
	// 'relativePath': The file path. If empty, 'name' is used as the path.
	// Returns the loaded Texture pointer, or nullptr on failure.
	Texture* LoadTexture(const std::string& name, const std::string& relativePath = "");

	// Retrieve a loaded texture by name. Returns nullptr if not found.
	Texture* GetTexture(const std::string& name);

	// -------------------------------------------------------------------------
	// FONT MANAGEMENT (SDF)
	// -------------------------------------------------------------------------

	// Loads a TrueType font and generates an SDF atlas.
	// 'name': The unique key.
	// 'relativePath': File path.
	// 'fontSize': The size to render the SDF atlas (default 48).
	Text* LoadFont(const std::string& name, const std::string& relativePath = "", int fontSize = 48);

	// Retrieve a loaded font by name.
	Text* GetFont(const std::string& name);

	// -------------------------------------------------------------------------
	// UTILITIES
	// -------------------------------------------------------------------------

	// Return a full absolute path to a resource given a relative path.
	// Searches executable directory, project directory, and "Resources" subfolders.
	// Returns empty string if not found.
	std::string GetResourcePath(const std::string& relativePath);

	// Return full path to 'EngineManaged.runtimeconfig.json' for C# hosting.
	std::string GetManagedRuntimeConfigPath();

	// Return a path relative to the .NET publish directory (where runtimeconfig.json lives).
	std::string GetScriptingPath(const std::string& relativeToPublishRoot);

	// Unload all resources (Shaders, Textures, Fonts) and free memory.
	void Clear();

private:
	ResourceManager();
	~ResourceManager();

	// Prevent copying
	ResourceManager(const ResourceManager&) = delete;
	ResourceManager& operator=(const ResourceManager&) = delete;

	// Resource Storage (Keys are stored in lowercase)
	std::unordered_map<std::string, Shader*> m_shaders;
	std::unordered_map<std::string, Texture*> m_textures;
	std::unordered_map<std::string, Text*> m_fonts;
};
