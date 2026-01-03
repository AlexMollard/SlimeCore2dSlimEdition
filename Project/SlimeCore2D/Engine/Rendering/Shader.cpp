#include "Shader.h"

#include <fstream>
#include <iostream>
#include <sstream>
#include <vector>

#include "Core/Logger.h"
#include "Core/Window.h"
#include "DiligentCore/Graphics/GraphicsTools/interface/MapHelper.hpp"
#include "EngineFactory.h"

using namespace Diligent;

Shader::Shader(const std::string& name, const char* vertexPath, const char* fragmentPath, const char* geometryPath)
      : m_name(name)
{
	auto device = Window::GetDevice();
	auto factory = Window::GetEngineFactory();

	// Extract directory from vertexPath
	std::string vPathStr(vertexPath);
	std::string vDirectory;
	std::string vFileName;
	size_t vLastSlash = vPathStr.find_last_of("/\\");
	if (vLastSlash != std::string::npos)
	{
		vDirectory = vPathStr.substr(0, vLastSlash);
		vFileName = vPathStr.substr(vLastSlash + 1);
	}
	else
	{
		vFileName = vPathStr;
	}

	// Extract filename from fragmentPath
	std::string fPathStr(fragmentPath);
	std::string fDirectory;
	std::string fFileName;
	size_t fLastSlash = fPathStr.find_last_of("/\\");
	if (fLastSlash != std::string::npos)
	{
		fDirectory = fPathStr.substr(0, fLastSlash);
		fFileName = fPathStr.substr(fLastSlash + 1);
	}
	else
	{
		fFileName = fPathStr;
	}

	std::string searchDirs = vDirectory;
	if (!fDirectory.empty() && fDirectory != vDirectory)
	{
		searchDirs += ";" + fDirectory;
	}

	RefCntAutoPtr<IShaderSourceInputStreamFactory> pShaderSourceFactory;
	factory->CreateDefaultShaderSourceStreamFactory(searchDirs.c_str(), &pShaderSourceFactory);

	ShaderCreateInfo ShaderCI;
	ShaderCI.pShaderSourceStreamFactory = pShaderSourceFactory;
	ShaderCI.SourceLanguage = SHADER_SOURCE_LANGUAGE_HLSL;
	// ShaderCI.UseCombinedTextureSamplers = true;

	// Check for GLSL extension
	if (vPathStr.find(".glsl") != std::string::npos || vPathStr.find(".vert") != std::string::npos)
	{
		ShaderCI.SourceLanguage = SHADER_SOURCE_LANGUAGE_GLSL;
		// ShaderCI.UseCombinedTextureSamplers = true; // Keep false to allow separate samplers
		m_IsGLSL = true;
	}

	// Create Vertex Shader
	ShaderCI.Desc.ShaderType = SHADER_TYPE_VERTEX;
	ShaderCI.Desc.Name = "Vertex Shader";
	ShaderCI.FilePath = vFileName.c_str();
	ShaderCI.EntryPoint = "main";
	device->CreateShader(ShaderCI, &m_VertexShader);

	if (!m_VertexShader)
	{
		Logger::Error("Failed to create vertex shader: " + vFileName);
	}

	// Create Pixel Shader
	ShaderCI.Desc.ShaderType = SHADER_TYPE_PIXEL;
	ShaderCI.Desc.Name = "Pixel Shader";
	ShaderCI.FilePath = fFileName.c_str();
	ShaderCI.EntryPoint = "main";
	device->CreateShader(ShaderCI, &m_PixelShader);

	if (!m_PixelShader)
	{
		Logger::Error("Failed to create pixel shader: " + fFileName);
	}

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
	MapHelper<ConstantBuffer> CBData(Window::GetContext(), m_ConstantBuffer, MAP_WRITE, MAP_FLAG_DISCARD);
	*CBData = m_CBufferData;
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
		m_CBufferData.ViewProjection = mat;

		UpdateConstantBuffer();
	}
}
