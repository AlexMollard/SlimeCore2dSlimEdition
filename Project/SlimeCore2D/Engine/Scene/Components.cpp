#include "Components.h"
#include <gtc/matrix_transform.hpp>

glm::mat4 TransformComponent::GetTransform() const
{
	glm::mat4 transform = glm::translate(glm::mat4(1.0f), Position);
	transform = glm::rotate(transform, glm::radians(Rotation), { 0.0f, 0.0f, 1.0f });
	transform = glm::scale(transform, { Scale.x, Scale.y, 1.0f });
	return transform;
}
