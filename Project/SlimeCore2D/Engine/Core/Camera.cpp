#include "Camera.h"
#include "Input.h"

Camera::Camera(float aspectX, float aspectY, float near, float far)
{
	transform = glm::ortho<float>(-aspectX, aspectX, -aspectY, aspectY, near, far);
	defaultTransform = transform;

	aspectRatio = glm::vec2(aspectX, aspectY);
	aspectRatioBeforeFieldOfView = aspectRatio;
	nearPlane = near;
	farPlane = far;
}

Camera::~Camera()
{
}

void Camera::Update(float deltaTime)
{
	CameraMoveMent(deltaTime);
}

void Camera::CameraMoveMent(float deltaTime)
{
	float speed = (Input::GetKeyPress(Keycode::LEFT_SHIFT)) ? 10.0f : 4.0f;

	float moveSpeed = speed * deltaTime;

	if (Input::GetScroll() > 0)
		SetFOV(fieldOfView - moveSpeed);
	
	if (Input::GetScroll() < 0)
		SetFOV(fieldOfView + moveSpeed);

	UpdateTransform();
}

void Camera::SetPosition(glm::vec2 newPos)
{
	position = newPos;
	UpdateTransform();
}

glm::vec2 Camera::GetPosition()
{
	return position;
}

glm::mat4 Camera::GetTransform()
{
	return  transform;
}

void Camera::UpdateTransform()
{
	transform = defaultTransform;

	transform = glm::translate(transform, { position , 0.0f });
}

void Camera::SetAspectRatio(glm::vec2 newAspectRatio)
{
	aspectRatio = newAspectRatio;
	aspectRatioBeforeFieldOfView = aspectRatio;
	aspectRatio *= glm::vec2(fieldOfView);

	transform = glm::ortho<float>(-aspectRatio.x, aspectRatio.x, -aspectRatio.y, aspectRatio.y, nearPlane, farPlane);
	defaultTransform = transform;
}

glm::vec2 Camera::GetAspectRatio()
{
	return aspectRatio;
}

void Camera::SetClippingPlane(float newNear, float newFar)
{
	nearPlane = newNear;
	farPlane = newFar;

	transform = glm::ortho<float>(-aspectRatio.x, aspectRatio.x, -aspectRatio.y, aspectRatio.y, nearPlane, farPlane);
	defaultTransform = transform;
}

glm::vec2 Camera::GetClippingPlane()
{
	return glm::vec2(nearPlane, farPlane);
}

float Camera::GetFOV()
{
	return fieldOfView;
}

void Camera::SetFOV(float newFOV)
{
	fieldOfView = (newFOV >= 0.01f) ? newFOV : 0.01f;
	SetAspectRatio(aspectRatioBeforeFieldOfView);
}