#pragma once

#include <string>
#include <unordered_map>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <wrl/client.h>
#include "glm.hpp"

using Microsoft::WRL::ComPtr;

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
	void Bind() const { Use(); }
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
	void SetMat4(const std::string& name, const glm::mat4& mat) const { setMat4(name, mat); }

    ID3D11InputLayout* GetInputLayout() const { return m_InputLayout.Get(); }

private:
    struct ConstantBuffer
    {
        glm::mat4 ViewProjection;
    };
    mutable ConstantBuffer m_CBufferData;
    mutable ComPtr<ID3D11Buffer> m_ConstantBuffer;

    ComPtr<ID3D11VertexShader> m_VertexShader;
    ComPtr<ID3D11PixelShader> m_PixelShader;
    ComPtr<ID3D11InputLayout> m_InputLayout;
    
	std::string m_name;
    
    void CompileShader(const std::string& source, const std::string& profile, ID3DBlob** blob);
    void CreateConstantBuffer();
    void UpdateConstantBuffer() const;
};
