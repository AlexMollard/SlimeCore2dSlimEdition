#pragma once
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>

class Camera
{
public:
	Camera(float aspectX, float aspectY, float near, float far);
	~Camera();

	void Update(float deltaTime);
	void CameraMoveMent(float deltaTime);

	void SetPosition(glm::vec2 newPos);
	glm::vec2 GetPosition();

	glm::mat4 GetTransform();
	void UpdateTransform();

	void SetAspectRatio(glm::vec2 newAspectRatio);
	glm::vec2 GetAspectRatio();

	void SetClippingPlane(float newNear, float newFar);
	glm::vec2 GetClippingPlane();

	float GetFOV();
	void SetFOV(float newFOV);

private:
	glm::vec2 position = glm::vec2(0);
	glm::mat4 defaultTransform = glm::mat4(1);
	glm::mat4 transform = glm::mat4(1);
	glm::vec2 aspectRatio = glm::vec2(1);
	glm::vec2 aspectRatioBeforeFieldOfView = glm::vec2(1);

	float nearPlane = 0;
	float farPlane = 0;

	// 1.0f is default
	float fieldOfView = 1.0f;
};
