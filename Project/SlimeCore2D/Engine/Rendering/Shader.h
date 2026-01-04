#pragma once

#include <string>
#include <unordered_map>

#include "glm.hpp"
#include "RefCntAutoPtr.hpp"
#include "RenderDevice.h"

using namespace Diligent;

class Shader
{
public:
	Shader(const std::string& name, const char* vertexPath, const char* fragmentPath, const char* geometryPath = nullptr);
	Shader() = default;
	~Shader();

	Shader(const Shader&) = delete;
	Shader& operator=(const Shader&) = delete;

	Shader(Shader&& other) noexcept;
	Shader& operator=(Shader&& other) noexcept;

	void Use() const;

	void Bind() const
	{
		Use();
	}

	void Unbind() const;

	void setBool(const std::string& name, bool value) const;
	void setInt(const std::string& name, int value) const;
	void setFloat(const std::string& name, float value) const;
	void setIntArray(const std::string& name, int* values, uint32_t count) const;
	void setVec2(const std::string& name, const glm::vec2& value) const;
	void setVec2(const std::string& name, float x, float y) const;
	void setVec3(const std::string& name, const glm::vec3& value) const;
	void setVec3(const std::string& name, float x, float y, float z) const;
	void setVec4(const std::string& name, const glm::vec4& value) const;
	void setVec4(const std::string& name, float x, float y, float z, float w) const;
	void setMat2(const std::string& name, const glm::mat2& mat) const;
	void setMat3(const std::string& name, const glm::mat3& mat) const;
	void setMat4(const std::string& name, const glm::mat4& mat) const;

	void SetMat4(const std::string& name, const glm::mat4& mat) const
	{
		setMat4(name, mat);
	}

	void UploadConstants() const;

	IShader* GetVertexShader() const
	{
		return m_VertexShader;
	}

	IShader* GetPixelShader() const
	{
		return m_PixelShader;
	}

	IBuffer* GetConstantBuffer() const
	{
		return m_ConstantBuffer;
	}

private:
	struct ConstantBuffer
	{
		glm::mat4 ViewProjection;
		float Time;
		float Padding[3];
	};

	mutable ConstantBuffer m_CBufferData;
	mutable RefCntAutoPtr<IBuffer> m_ConstantBuffer;

	RefCntAutoPtr<IShader> m_VertexShader;
	RefCntAutoPtr<IShader> m_PixelShader;

	std::string m_name;
	bool m_IsGLSL = false;

	void CreateConstantBuffer();
	void UpdateConstantBuffer() const;
};
