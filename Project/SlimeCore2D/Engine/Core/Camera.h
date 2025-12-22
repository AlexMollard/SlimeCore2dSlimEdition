#pragma once

#include <glm.hpp>

class Camera
{
public:
	// orthoSize: How many units high the camera sees (e.g., 10.0f).
	// aspectRatio: Width / Height.
	Camera(float orthoSize, float aspectRatio);
	~Camera() = default;

	// --- Core Logic ---
	// Call this when the window resizes
	void SetProjection(float orthoSize, float aspectRatio);

	// Internal update for matrices (call this if you move the camera manually without setters)
	void RecalculateViewMatrix();

	// --- Getters & Setters ---

	const glm::vec3& GetPosition() const
	{
		return m_Position;
	}

	// Z-value allows simple layering (e.g. -10 to 10)
	void SetPosition(const glm::vec3& position);

	void SetPosition(const glm::vec2& position)
	{
		SetPosition(glm::vec3(position.x, position.y, m_Position.z));
	}

	float GetRotation() const
	{
		return m_Rotation;
	}

	void SetRotation(float rotation); // In Degrees

	float GetZoom() const
	{
		return m_ZoomLevel;
	}

	void SetZoom(float zoom);

	// The Matrix sent to the Shader (u_ViewProjection)
	const glm::mat4& GetViewProjectionMatrix() const
	{
		return m_ViewProjectionMatrix;
	}

	const glm::mat4& GetProjectionMatrix() const
	{
		return m_ProjectionMatrix;
	}

	const glm::mat4& GetViewMatrix() const
	{
		return m_ViewMatrix;
	}

	// Utility to get world bounds
	float GetOrthographicSize() const
	{
		return m_OrthographicSize;
	}

	float GetAspectRatio() const
	{
		return m_AspectRatio;
	}

private:
	glm::mat4 m_ProjectionMatrix;
	glm::mat4 m_ViewMatrix;
	glm::mat4 m_ViewProjectionMatrix;

	glm::vec3 m_Position = { 0.0f, 0.0f, 0.0f };
	float m_Rotation = 0.0f; // Degrees

	float m_OrthographicSize = 10.0f; // Height in World Units
	float m_AspectRatio = 1.77f;      // 16:9 approx
	float m_ZoomLevel = 1.0f;
};
