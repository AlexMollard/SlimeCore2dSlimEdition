#include "Shader.h"
#include "Core/Logger.h"
#include "Core/Window.h"

#include <fstream>
#include <iostream>
#include <sstream>
#include <vector>

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

    // 2. Compile Shaders
    ComPtr<ID3DBlob> vsBlob;
    ComPtr<ID3DBlob> psBlob;
    
    CompileShader(vertexCode, "vs_5_0", &vsBlob);
    CompileShader(fragmentCode, "ps_5_0", &psBlob);
    
    auto device = Window::GetDevice();
    
    if (vsBlob)
    {
        device->CreateVertexShader(vsBlob->GetBufferPointer(), vsBlob->GetBufferSize(), nullptr, &m_VertexShader);
        
        // Create Input Layout
        // Matching BasicVertex.hlsl VS_INPUT
        D3D11_INPUT_ELEMENT_DESC ied[] =
        {
            {"POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0},
            {"COLOR",    0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0},
            {"TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT,    0, 28, D3D11_INPUT_PER_VERTEX_DATA, 0},
            {"TEXCOORD", 1, DXGI_FORMAT_R32_FLOAT,       0, 36, D3D11_INPUT_PER_VERTEX_DATA, 0},
            {"TILING",   0, DXGI_FORMAT_R32_FLOAT,       0, 40, D3D11_INPUT_PER_VERTEX_DATA, 0},
            {"ISTEXT",   0, DXGI_FORMAT_R32_FLOAT,       0, 44, D3D11_INPUT_PER_VERTEX_DATA, 0},
        };
        
        device->CreateInputLayout(ied, 6, vsBlob->GetBufferPointer(), vsBlob->GetBufferSize(), &m_InputLayout);
    }
    
    if (psBlob)
    {
        device->CreatePixelShader(psBlob->GetBufferPointer(), psBlob->GetBufferSize(), nullptr, &m_PixelShader);
    }
    
    CreateConstantBuffer();
}

Shader::~Shader()
{
    // ComPtr handles cleanup
}

Shader::Shader(Shader&& other) noexcept
{
    m_VertexShader = std::move(other.m_VertexShader);
    m_PixelShader = std::move(other.m_PixelShader);
    m_InputLayout = std::move(other.m_InputLayout);
    m_ConstantBuffer = std::move(other.m_ConstantBuffer);
    m_name = std::move(other.m_name);
}

Shader& Shader::operator=(Shader&& other) noexcept
{
    if (this != &other)
    {
        m_VertexShader = std::move(other.m_VertexShader);
        m_PixelShader = std::move(other.m_PixelShader);
        m_InputLayout = std::move(other.m_InputLayout);
        m_ConstantBuffer = std::move(other.m_ConstantBuffer);
        m_name = std::move(other.m_name);
    }
    return *this;
}

void Shader::CompileShader(const std::string& source, const std::string& profile, ID3DBlob** blob)
{
    ComPtr<ID3DBlob> errorBlob;
    HRESULT hr = D3DCompile(
        source.c_str(), source.length(),
        nullptr, nullptr, nullptr,
        "main", profile.c_str(),
        D3DCOMPILE_ENABLE_STRICTNESS | D3DCOMPILE_DEBUG, 0,
        blob, &errorBlob
    );
    
    if (FAILED(hr))
    {
        if (errorBlob)
        {
            Logger::Error("Shader Compile Error (" + profile + "): " + (char*)errorBlob->GetBufferPointer());
        }
    }
}

void Shader::CreateConstantBuffer()
{
    D3D11_BUFFER_DESC bd;
    ZeroMemory(&bd, sizeof(bd));
    bd.Usage = D3D11_USAGE_DEFAULT;
    bd.ByteWidth = sizeof(ConstantBuffer);
    bd.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
    bd.CPUAccessFlags = 0;
    
    Window::GetDevice()->CreateBuffer(&bd, nullptr, &m_ConstantBuffer);
}

void Shader::UpdateConstantBuffer() const
{
    Window::GetContext()->UpdateSubresource(m_ConstantBuffer.Get(), 0, nullptr, &m_CBufferData, 0, 0);
}

void Shader::Use() const
{
    auto context = Window::GetContext();
    context->VSSetShader(m_VertexShader.Get(), nullptr, 0);
    context->PSSetShader(m_PixelShader.Get(), nullptr, 0);
    context->IASetInputLayout(m_InputLayout.Get());
    
    // Bind Constant Buffer to VS slot 0
    context->VSSetConstantBuffers(0, 1, m_ConstantBuffer.GetAddressOf());
    // Bind Constant Buffer to PS slot 0
    context->PSSetConstantBuffers(0, 1, m_ConstantBuffer.GetAddressOf());
}

void Shader::Unbind() const
{
    auto context = Window::GetContext();
    context->VSSetShader(nullptr, nullptr, 0);
    context->PSSetShader(nullptr, nullptr, 0);
}

// Uniform Setters
void Shader::setBool(const std::string& name, bool value) const {}
void Shader::setInt(const std::string& name, int value) const {}
void Shader::setFloat(const std::string& name, float value) const
{
    if (name == "u_Time")
    {
        m_CBufferData.Time = value;
        UpdateConstantBuffer();
    }
}
void Shader::setIntArray(const std::string& name, int* values, uint32_t count) const {}
void Shader::setVec2(const std::string& name, const glm::vec2& value) const {}
void Shader::setVec2(const std::string& name, float x, float y) const {}
void Shader::setVec3(const std::string& name, const glm::vec3& value) const {}
void Shader::setVec3(const std::string& name, float x, float y, float z) const {}
void Shader::setVec4(const std::string& name, const glm::vec4& value) const {}
void Shader::setVec4(const std::string& name, float x, float y, float z, float w) const {}
void Shader::setMat2(const std::string& name, const glm::mat2& mat) const {}
void Shader::setMat3(const std::string& name, const glm::mat3& mat) const {}

void Shader::setMat4(const std::string& name, const glm::mat4& mat) const
{
    if (name == "u_ViewProjection")
    {
        // Transpose matrix for HLSL (Column-Major vs Row-Major)
        m_CBufferData.ViewProjection = glm::transpose(mat);
        UpdateConstantBuffer();
    }
}