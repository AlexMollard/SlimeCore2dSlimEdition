#include "Shader.h"
#include "Core/Logger.h"

#include <fstream>
#include <iostream>
#include <sstream>
#include <vector>

Shader::Shader(const std::string& name, const char* vertexPath, const char* fragmentPath, const char* geometryPath)
      : m_name(name)
{
	// 1. retrieve the vertex/fragment source code from filePath
	std::string vertexCode;
	std::string fragmentCode;
	std::string geometryCode;

	std::ifstream vShaderFile;
	std::ifstream fShaderFile;
	std::ifstream gShaderFile;

	// ensure ifstream objects can throw exceptions:
	vShaderFile.exceptions(std::ifstream::failbit | std::ifstream::badbit);
	fShaderFile.exceptions(std::ifstream::failbit | std::ifstream::badbit);
	gShaderFile.exceptions(std::ifstream::failbit | std::ifstream::badbit);

	try
	{
		// open files
		vShaderFile.open(vertexPath);
		fShaderFile.open(fragmentPath);
		std::stringstream vShaderStream, fShaderStream;

		// read file's buffer contents into streams
		vShaderStream << vShaderFile.rdbuf();
		fShaderStream << fShaderFile.rdbuf();

		// close file handlers
		vShaderFile.close();
		fShaderFile.close();

		// convert stream into string
		vertexCode = vShaderStream.str();
		fragmentCode = fShaderStream.str();

		// if geometry shader path is present, also load a geometry shader
		if (geometryPath != nullptr)
		{
			gShaderFile.open(geometryPath);
			std::stringstream gShaderStream;
			gShaderStream << gShaderFile.rdbuf();
			gShaderFile.close();
			geometryCode = gShaderStream.str();
		}
	}
	catch (std::ifstream::failure& e)
	{
		Logger::Error("ERROR::SHADER::FILE_NOT_SUCCESFULLY_READ: " + std::string(e.what()));
	}

	const char* vShaderCode = vertexCode.c_str();
	const char* fShaderCode = fragmentCode.c_str();

	// 2. compile shaders
	unsigned int vertex, fragment;

	// vertex shader
	vertex = glCreateShader(GL_VERTEX_SHADER);
	glShaderSource(vertex, 1, &vShaderCode, NULL);
	glCompileShader(vertex);
	CheckCompileErrors(vertex, "VERTEX");

	// fragment Shader
	fragment = glCreateShader(GL_FRAGMENT_SHADER);
	glShaderSource(fragment, 1, &fShaderCode, NULL);
	glCompileShader(fragment);
	CheckCompileErrors(fragment, "FRAGMENT");

	// if geometry shader is given, compile geometry shader
	unsigned int geometry = 0;
	/*
	if (geometryPath != nullptr)
	{
		const char* gShaderCode = geometryCode.c_str();
		geometry = glCreateShader(GL_GEOMETRY_SHADER);
		glShaderSource(geometry, 1, &gShaderCode, NULL);
		glCompileShader(geometry);
		CheckCompileErrors(geometry, "GEOMETRY");
	}
	*/

	// shader Program
	m_shaderID = glCreateProgram();
	glAttachShader(m_shaderID, vertex);
	glAttachShader(m_shaderID, fragment);
	/*
	if (geometryPath != nullptr)
		glAttachShader(m_shaderID, geometry);
	*/

	glLinkProgram(m_shaderID);
	CheckCompileErrors(m_shaderID, "PROGRAM");

	// delete the shaders as they're linked into our program now and no longer necessary
	glDeleteShader(vertex);
	glDeleteShader(fragment);
	/*
	if (geometryPath != nullptr)
		glDeleteShader(geometry);
	*/

	// Label for Debugging
	// glObjectLabel(GL_PROGRAM, m_shaderID, -1, name.c_str());
}

Shader::~Shader()
{
	if (m_shaderID != 0)
		glDeleteProgram(m_shaderID);
}

// Move Constructor
Shader::Shader(Shader&& other) noexcept
{
	m_shaderID = other.m_shaderID;
	m_name = other.m_name;
	m_UniformLocationCache = std::move(other.m_UniformLocationCache);

	other.m_shaderID = 0; // Invalidate source
}

// Move Assignment
Shader& Shader::operator=(Shader&& other) noexcept
{
	if (this != &other)
	{
		if (m_shaderID != 0)
			glDeleteProgram(m_shaderID);

		m_shaderID = other.m_shaderID;
		m_name = other.m_name;
		m_UniformLocationCache = std::move(other.m_UniformLocationCache);

		other.m_shaderID = 0;
	}
	return *this;
}

void Shader::Use() const
{
	glUseProgram(m_shaderID);
}

void Shader::Unbind() const
{
	glUseProgram(0);
}

void Shader::CheckCompileErrors(unsigned int shader, std::string type)
{
	GLint success;
	GLchar infoLog[1024];
	if (type != "PROGRAM")
	{
		glGetShaderiv(shader, GL_COMPILE_STATUS, &success);
		if (!success)
		{
			glGetShaderInfoLog(shader, 1024, NULL, infoLog);
			Logger::Error("ERROR::SHADER_COMPILATION_ERROR of type: " + type + "\n" + infoLog + "\n -- --------------------------------------------------- -- ");
		}
	}
	else
	{
		glGetProgramiv(shader, GL_LINK_STATUS, &success);
		if (!success)
		{
			glGetProgramInfoLog(shader, 1024, NULL, infoLog);
			Logger::Error("ERROR::PROGRAM_LINKING_ERROR of type: " + type + "\n" + infoLog + "\n -- --------------------------------------------------- -- ");
		}
	}
}

// ------------------------------------------------------------------------
// Uniform Caching System
// ------------------------------------------------------------------------
int Shader::GetUniformLocation(const std::string& name) const
{
	if (m_UniformLocationCache.find(name) != m_UniformLocationCache.end())
		return m_UniformLocationCache[name];

	int location = glGetUniformLocation(m_shaderID, name.c_str());
	if (location == -1)
	{
		// Warning: useful for debugging, but spammy if you have unused uniforms in shader
		// std::cout << "Warning: Uniform '" << name << "' doesn't exist or was optimized out in shader: " << m_name << std::endl;
	}

	m_UniformLocationCache[name] = location;
	return location;
}

// ------------------------------------------------------------------------
// Uniform Setters
// ------------------------------------------------------------------------

void Shader::setBool(const std::string& name, bool value) const
{
	glUniform1i(GetUniformLocation(name), (int) value);
}

void Shader::setInt(const std::string& name, int value) const
{
	glUniform1i(GetUniformLocation(name), value);
}

void Shader::setFloat(const std::string& name, float value) const
{
	glUniform1f(GetUniformLocation(name), value);
}

// THE NEW FUNCTION FOR BATCH RENDERING
void Shader::setIntArray(const std::string& name, int* values, uint32_t count) const
{
	glUniform1iv(GetUniformLocation(name), count, values);
}

void Shader::setVec2(const std::string& name, const glm::vec2& value) const
{
	glUniform2fv(GetUniformLocation(name), 1, &value[0]);
}

void Shader::setVec2(const std::string& name, float x, float y) const
{
	glUniform2f(GetUniformLocation(name), x, y);
}

void Shader::setVec3(const std::string& name, const glm::vec3& value) const
{
	glUniform3fv(GetUniformLocation(name), 1, &value[0]);
}

void Shader::setVec3(const std::string& name, float x, float y, float z) const
{
	glUniform3f(GetUniformLocation(name), x, y, z);
}

void Shader::setVec4(const std::string& name, const glm::vec4& value) const
{
	glUniform4fv(GetUniformLocation(name), 1, &value[0]);
}

void Shader::setVec4(const std::string& name, float x, float y, float z, float w) const
{
	glUniform4f(GetUniformLocation(name), x, y, z, w);
}

void Shader::setMat2(const std::string& name, const glm::mat2& mat) const
{
	glUniformMatrix2fv(GetUniformLocation(name), 1, GL_FALSE, &mat[0][0]);
}

void Shader::setMat3(const std::string& name, const glm::mat3& mat) const
{
	glUniformMatrix3fv(GetUniformLocation(name), 1, GL_FALSE, &mat[0][0]);
}

void Shader::setMat4(const std::string& name, const glm::mat4& mat) const
{
	glUniformMatrix4fv(GetUniformLocation(name), 1, GL_FALSE, &mat[0][0]);
}
