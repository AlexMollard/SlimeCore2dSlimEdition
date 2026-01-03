#include "Shader.h"

#include <fstream>
#include <iostream>
#include <sstream>
#include <vector>

#include "Core/Logger.h"
#include "Core/Window.h"

using namespace Diligent;

Shader::Shader(const std::string& name, const char* vertexPath, const char* fragmentPath, const char* geometryPath)
      : m_name(name)
{
	// 1. Read files
	std::string vertexCode;
	std::string fragmentCode;
	std::ifstream vShaderFile;
	std::ifstream fShaderFile;

	vShaderFile.exceptions(std::ifstream::failbit | std::ifstream::badbit);
	fShaderFile.exceptions(std::ifstream::failbit | std::ifstream::badbit);

	try
	{
		vShaderFile.open(vertexPath);
		fShaderFile.open(fragmentPath);
		std::stringstream vShaderStream, fShaderStream;
		vShaderStream << vShaderFile.rdbuf();
		fShaderStream << fShaderFile.rdbuf();
		vShaderFile.close();
		fShaderFile.close();
		vertexCode = vShaderStream.str();
		fragmentCode = fShaderStream.str();
	}
	catch (std::ifstream::failure& e)
	{
		Logger::Error("ERROR::SHADER::FILE_NOT_SUCCESFULLY_READ: " + std::string(e.what()));
	}

	auto device = Window::GetDevice();

	ShaderCreateInfo ShaderCI;
	ShaderCI.SourceLanguage = SHADER_SOURCE_LANGUAGE_HLSL;
	// ShaderCI.UseCombinedTextureSamplers = true;

	// Check for GLSL extension
	std::string vPath(vertexPath);
	if (vPath.find(".glsl") != std::string::npos || vPath.find(".vert") != std::string::npos)
	{
		ShaderCI.SourceLanguage = SHADER_SOURCE_LANGUAGE_GLSL;
		// ShaderCI.UseCombinedTextureSamplers = true; // Keep false to allow separate samplers
		m_IsGLSL = true;
	}

	// Create Vertex Shader
	ShaderCI.Desc.ShaderType = SHADER_TYPE_VERTEX;
	ShaderCI.Desc.Name = "Vertex Shader";
	ShaderCI.Source = vertexCode.c_str();
	ShaderCI.EntryPoint = "main";
	device->CreateShader(ShaderCI, &m_VertexShader);

	// Create Pixel Shader
	ShaderCI.Desc.ShaderType = SHADER_TYPE_PIXEL;
	ShaderCI.Desc.Name = "Pixel Shader";
	ShaderCI.Source = fragmentCode.c_str();
	ShaderCI.EntryPoint = "main";
	device->CreateShader(ShaderCI, &m_PixelShader);

	CreateConstantBuffer();
}

Shader::~Shader()
{
}

Shader::Shader(Shader&& other) noexcept
{
	m_VertexShader = std::move(other.m_VertexShader);
	m_PixelShader = std::move(other.m_PixelShader);
	m_ConstantBuffer = std::move(other.m_ConstantBuffer);
	m_name = std::move(other.m_name);
}

Shader& Shader::operator=(Shader&& other) noexcept
{
	if (this != &other)
	{
		m_VertexShader = std::move(other.m_VertexShader);
		m_PixelShader = std::move(other.m_PixelShader);
		m_ConstantBuffer = std::move(other.m_ConstantBuffer);
		m_name = std::move(other.m_name);
	}
	return *this;
}

void Shader::CreateConstantBuffer()
{
	Diligent::BufferDesc CBDesc;
	CBDesc.Name = "Constant Buffer";
	CBDesc.Size = sizeof(ConstantBuffer);
	CBDesc.Usage = USAGE_DYNAMIC;
	CBDesc.BindFlags = BIND_UNIFORM_BUFFER;
	CBDesc.CPUAccessFlags = CPU_ACCESS_WRITE;

	Window::GetDevice()->CreateBuffer(CBDesc, nullptr, &m_ConstantBuffer);
}

void Shader::UpdateConstantBuffer() const
{
	void* pData;
	Window::GetContext()->MapBuffer(m_ConstantBuffer, MAP_WRITE, MAP_FLAG_DISCARD, pData);
	memcpy(pData, &m_CBufferData, sizeof(ConstantBuffer));
	Window::GetContext()->UnmapBuffer(m_ConstantBuffer, MAP_WRITE);
}

void Shader::Use() const
{
	// In Diligent, we don't bind shaders directly.
	// We bind PSO.
	// But we can update the constant buffer here.
}

void Shader::Unbind() const
{
}

// Uniform Setters
void Shader::setBool(const std::string& name, bool value) const
{
}

void Shader::setInt(const std::string& name, int value) const
{
}

void Shader::setFloat(const std::string& name, float value) const
{
	if (name == "u_Time")
	{
		m_CBufferData.Time = value;
		UpdateConstantBuffer();
	}
}

void Shader::setIntArray(const std::string& name, int* values, uint32_t count) const
{
}

void Shader::setVec2(const std::string& name, const glm::vec2& value) const
{
}

void Shader::setVec2(const std::string& name, float x, float y) const
{
}

void Shader::setVec3(const std::string& name, const glm::vec3& value) const
{
}

void Shader::setVec3(const std::string& name, float x, float y, float z) const
{
}

void Shader::setVec4(const std::string& name, const glm::vec4& value) const
{
}

void Shader::setVec4(const std::string& name, float x, float y, float z, float w) const
{
}

void Shader::setMat2(const std::string& name, const glm::mat2& mat) const
{
}

void Shader::setMat3(const std::string& name, const glm::mat3& mat) const
{
}

void Shader::setMat4(const std::string& name, const glm::mat4& mat) const
{
	if (name == "u_ViewProjection")
	{
		// In Diligent/HLSL, matrices are column-major by default (like glm).
		// We should NOT transpose them if we use mul(Matrix, Vector).
		// However, if the shader expects Row-Major (e.g. mul(v, M)), we might need to transpose.
		// But our shaders use mul(M, v).

		// Try transposing again, just in case Diligent defaults to Row-Major packing for cbuffers.
		m_CBufferData.ViewProjection = glm::transpose(mat);

		UpdateConstantBuffer();
	}
}
