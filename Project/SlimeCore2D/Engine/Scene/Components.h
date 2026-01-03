#pragma once

#include <glm.hpp>
#include <string>
#include <vector>

#include "Rendering/Texture.h"

// Entity ID type
using Entity = std::uint64_t;
static constexpr Entity NullEntity = 0;

struct TagComponent
{
	std::string Name;
};

struct TransformComponent
{
	glm::vec3 Position = { 0.0f, 0.0f, 0.0f };
	glm::vec2 Scale = { 1.0f, 1.0f };
	float Rotation = 0.0f; // Degrees
	glm::vec2 Anchor = { 0.5f, 0.5f };

	// Helper to get matrix
	glm::mat4 GetTransform() const;
};

struct SpriteComponent
{
	glm::vec4 Color = { 1.0f, 1.0f, 1.0f, 1.0f };
	Texture* Texture = nullptr;
	float TilingFactor = 1.0f;
	bool IsVisible = true;
	int Layer = 0; // Helper for Z-sorting if needed, though Z in Transform handles it too
};

struct AnimationComponent
{
	bool HasAnimation = false;
	int Frame = 0;
	int SpriteWidth = 0; // 0 = full texture
	float FrameRate = 12.0f;
	float Timer = 0.0f;
};

struct RelationshipComponent
{
	Entity Parent = NullEntity;
	std::vector<Entity> Children;
};

struct RigidBodyComponent
{
	glm::vec2 Velocity = { 0.0f, 0.0f };
	float Mass = 1.0f;
	float Drag = 0.0f;
	bool IsKinematic = false;
	bool FixedRotation = false;
	void* RuntimeBody = nullptr;
};

struct BoxColliderComponent
{
	glm::vec2 Offset = { 0.0f, 0.0f };
	glm::vec2 Size = { 1.0f, 1.0f };
	bool IsTrigger = false;
	bool IsColliding = false; // Runtime state
};

struct CircleColliderComponent
{
	glm::vec2 Offset = { 0.0f, 0.0f };
	float Radius = 0.5f;
	bool IsTrigger = false;
};

struct CameraComponent
{
	bool IsPrimary = true;
	float OrthographicSize = 10.0f;
	float ZoomLevel = 1.0f;
	float AspectRatio = 1.77f;
};

struct AudioSourceComponent
{
	std::string ClipName;
	bool PlayOnAwake = false;
	bool Loop = false;
	float Volume = 1.0f;
	float Pitch = 1.0f;
	bool IsPlaying = false;
};
