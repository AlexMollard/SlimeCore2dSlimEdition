#include "Camera.h"

#include <algorithm> // for std::max
#include <gtc/matrix_transform.hpp>

#include "Input.h" // Assuming you have this for OnUpdate

Camera::Camera(float orthoSize, float aspectRatio)
      : m_OrthographicSize(orthoSize), m_AspectRatio(aspectRatio)
{
	m_ProjectionMatrix = glm::mat4(1.0f);
	m_ViewMatrix = glm::mat4(1.0f);
	m_ViewProjectionMatrix = glm::mat4(1.0f);

	SetProjection(orthoSize, aspectRatio);
}

void Camera::SetProjection(float orthoSize, float aspectRatio)
{
	m_OrthographicSize = orthoSize;
	m_AspectRatio = aspectRatio;

	// Formula for centered Orthographic camera:
	// Top = Size * Zoom * 0.5
	// Right = Top * AspectRatio
	float orthoLeft = -m_OrthographicSize * m_AspectRatio * 0.5f * m_ZoomLevel;
	float orthoRight = m_OrthographicSize * m_AspectRatio * 0.5f * m_ZoomLevel;
	float orthoBottom = -m_OrthographicSize * 0.5f * m_ZoomLevel;
	float orthoTop = m_OrthographicSize * 0.5f * m_ZoomLevel;

	// Z-Near and Z-Far usually -1.0 to 1.0 for 2D, or wider if you have layers
	m_ProjectionMatrix = glm::ortho(orthoLeft, orthoRight, orthoBottom, orthoTop, -10.0f, 10.0f);

	m_ViewProjectionMatrix = m_ProjectionMatrix * m_ViewMatrix;
}

void Camera::RecalculateViewMatrix()
{
	// 1. Create Transform: Translate -> Rotate
	glm::mat4 transform = glm::translate(glm::mat4(1.0f), m_Position) * glm::rotate(glm::mat4(1.0f), glm::radians(m_Rotation), glm::vec3(0, 0, 1));

	// 2. View Matrix is the INVERSE of the Camera Transform
	// (Moving camera right = Moving world left)
	m_ViewMatrix = glm::inverse(transform);

	// 3. Update cached result
	m_ViewProjectionMatrix = m_ProjectionMatrix * m_ViewMatrix;
}

void Camera::SetPosition(const glm::vec3& position)
{
	m_Position = position;
	RecalculateViewMatrix();
}

void Camera::SetRotation(float rotation)
{
	m_Rotation = rotation;
	RecalculateViewMatrix();
}

void Camera::SetZoom(float zoom)
{
	// Prevent flipping or div by zero
	m_ZoomLevel = std::max(zoom, 0.1f);
	SetProjection(m_OrthographicSize, m_AspectRatio); // Projection depends on Zoom
}
