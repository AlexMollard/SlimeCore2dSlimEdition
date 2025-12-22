#pragma once

#include <string>
#include <unordered_map>

#include "glew.h"
#include "glfw3.h"
#include "glm.hpp"

class Shader
{
public:
	// Constructor reads and builds the shader
	Shader(const std::string& name, const char* vertexPath, const char* fragmentPath, const char* geometryPath = nullptr);

	// Default constructor (creates empty object)
	Shader() = default;

	~Shader();

	// --- Rule of Five (Prevent Copying, Allow Moving) ---
	// Prevents "Double Free" crashes where two objects try to delete the same GPU ID
	Shader(const Shader&) = delete;
	Shader& operator=(const Shader&) = delete;

	Shader(Shader&& other) noexcept;
	Shader& operator=(Shader&& other) noexcept;
	// ----------------------------------------------------

	void Use() const;
	void Unbind() const;

	unsigned int GetID() const
	{
		return m_shaderID;
	}

	std::string GetName() const
	{
		return m_name;
	}

	// Utility functions
	// ------------------------------------------------------------------------
	void setBool(const std::string& name, bool value) const;
	void setInt(const std::string& name, int value) const;
	void setFloat(const std::string& name, float value) const;

	// Crucial for Batch Rendering (Texture Slots)
	void setIntArray(const std::string& name, int* values, uint32_t count) const;

	// Vectors
	void setVec2(const std::string& name, const glm::vec2& value) const;
	void setVec2(const std::string& name, float x, float y) const;
	void setVec3(const std::string& name, const glm::vec3& value) const;
	void setVec3(const std::string& name, float x, float y, float z) const;
	void setVec4(const std::string& name, const glm::vec4& value) const;
	void setVec4(const std::string& name, float x, float y, float z, float w) const;

	// Matrices
	void setMat2(const std::string& name, const glm::mat2& mat) const;
	void setMat3(const std::string& name, const glm::mat3& mat) const;
	void setMat4(const std::string& name, const glm::mat4& mat) const;

private:
	// Helper to check for compilation/linking errors
	void CheckCompileErrors(unsigned int shader, std::string type);

	// Helper to find uniform location (with caching)
	int GetUniformLocation(const std::string& name) const;

private:
	unsigned int m_shaderID = 0;
	std::string m_name;

	// Cache for uniform locations to avoid slow glGetUniformLocation calls
	mutable std::unordered_map<std::string, int> m_UniformLocationCache;
};
