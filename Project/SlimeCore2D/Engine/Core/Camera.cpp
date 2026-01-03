#include "Camera.h"

#include <algorithm> // for std::max
#define GLM_FORCE_DEPTH_ZERO_TO_ONE
#define GLM_FORCE_LEFT_HANDED
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
	float orthoLeft = -m_OrthographicSize * m_AspectRatio * 0.5f * m_ZoomLevel;
	float orthoRight = m_OrthographicSize * m_AspectRatio * 0.5f * m_ZoomLevel;
	float orthoBottom = -m_OrthographicSize * 0.5f * m_ZoomLevel;
	float orthoTop = m_OrthographicSize * 0.5f * m_ZoomLevel;

	// DX11 uses 0 to 1 depth range and Left-Handed coordinates.
	// We use a range of -100 to 100 to ensure objects are visible and not clipped.
	m_ProjectionMatrix = glm::orthoLH_ZO(orthoLeft, orthoRight, orthoBottom, orthoTop, -100.0f, 100.0f);

	m_ViewProjectionMatrix = m_ProjectionMatrix * m_ViewMatrix;
}

void Camera::RecalculateViewMatrix()
{
	// 1. Create Transform: Translate -> Rotate
	glm::mat4 transform = glm::translate(glm::mat4(1.0f), m_Position) * glm::rotate(glm::mat4(1.0f), glm::radians(m_Rotation), glm::vec3(0, 0, 1));

	// 2. View Matrix is the INVERSE of the Camera Transform
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
