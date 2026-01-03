#include "Scripting/ExportEntity.h"

#include "Rendering/Texture.h"
#include "Scene/Scene.h"

// -------------------------------------------------------------------------
// ENTITY LIFECYCLE
// -------------------------------------------------------------------------
SLIME_EXPORT EntityId __cdecl Entity_Create()
{
	if (!Scene::GetActiveScene())
		return 0;
	return (EntityId) Scene::GetActiveScene()->CreateEntity();
}

SLIME_EXPORT EntityId __cdecl Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
{
	if (!Scene::GetActiveScene())
		return 0;
	return (EntityId) Scene::GetActiveScene()->CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(r, g, b));
}

SLIME_EXPORT void __cdecl Entity_Destroy(EntityId id)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	Scene::GetActiveScene()->DestroyObject((ObjectId) id);
}

SLIME_EXPORT bool __cdecl Entity_IsAlive(EntityId id)
{
	if (!Scene::GetActiveScene() || id == 0)
		return false;
	return Scene::GetActiveScene()->IsAlive((ObjectId) id);
}

// -------------------------------------------------------------------------
// ENTITY TRANSFORM
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetPosition(EntityId id, float x, float y)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		t->Position.x = x;
		t->Position.y = y;
	}
}

SLIME_EXPORT void __cdecl Entity_GetPosition(EntityId id, float* outX, float* outY)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		if (outX)
			*outX = t->Position.x;
		if (outY)
			*outY = t->Position.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetSize(EntityId id, float sx, float sy)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		t->Scale = { sx, sy };
	}
}

SLIME_EXPORT void __cdecl Entity_GetSize(EntityId id, float* outSx, float* outSy)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		if (outSx)
			*outSx = t->Scale.x;
		if (outSy)
			*outSy = t->Scale.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetColor(EntityId id, float r, float g, float b)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		s->Color = { r, g, b, 1.0f };
	}
}

SLIME_EXPORT void __cdecl Entity_GetColor(EntityId id, float* outR, float* outG, float* outB)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		if (outR)
			*outR = s->Color.r;
		if (outG)
			*outG = s->Color.g;
		if (outB)
			*outB = s->Color.b;
	}
}

SLIME_EXPORT void __cdecl Entity_SetAlpha(EntityId id, float a)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		s->Color.a = a;
	}
}

SLIME_EXPORT float __cdecl Entity_GetAlpha(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 1.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		return s->Color.a;
	}
	return 1.0f;
}

SLIME_EXPORT void __cdecl Entity_SetLayer(EntityId id, int layer)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		// Use very small step to keep within [-10, 10] range easily
		// Layer 100 -> Z=0.01f.
		t->Position.z = layer * 0.0001f;
	}
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		s->Layer = layer;
	}
}

SLIME_EXPORT int __cdecl Entity_GetLayer(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 0;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		return s->Layer;
	}
	return 0;
}

SLIME_EXPORT void __cdecl Entity_SetRotation(EntityId id, float degrees)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		t->Rotation = degrees;
	}
}

SLIME_EXPORT float __cdecl Entity_GetRotation(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 0.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		return t->Rotation;
	}
	return 0.0f;
}

SLIME_EXPORT void __cdecl Entity_SetAnchor(EntityId id, float ax, float ay)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		t->Anchor = { ax, ay };
	}
}

SLIME_EXPORT void __cdecl Entity_GetAnchor(EntityId id, float* outAx, float* outAy)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity) id))
	{
		if (outAx)
			*outAx = t->Anchor.x;
		if (outAy)
			*outAy = t->Anchor.y;
	}
}

// -------------------------------------------------------------------------
// ENTITY VISUALS & ANIMATION
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		Texture* t = new Texture(width, height);
		s->Texture = t;
	}
	if (width > 0)
	{
		if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
		{
			a->SpriteWidth = width;
		}
	}
}

SLIME_EXPORT void __cdecl Entity_SetTexturePtr(EntityId id, void* texPtr)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		if (texPtr)
		{
			Texture* tex = (Texture*) texPtr;
			s->Texture = tex;
			if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
			{
				a->SpriteWidth = tex->GetWidth();
			}
		}
		else
		{
			s->Texture = nullptr;
		}
	}
}

SLIME_EXPORT void* __cdecl Entity_GetTexturePtr(EntityId id)
{
	if (!Scene::GetActiveScene() || id == 0)
		return nullptr;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		return (void*) s->Texture;
	}
	return nullptr;
}

SLIME_EXPORT void __cdecl Entity_SetRender(EntityId id, bool value)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		s->IsVisible = value;
	}
}

SLIME_EXPORT bool __cdecl Entity_GetRender(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity) id))
	{
		return s->IsVisible;
	}
	return false;
}

SLIME_EXPORT void __cdecl Entity_SetFrame(EntityId id, int frame)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		a->Frame = frame;
	}
}

SLIME_EXPORT int __cdecl Entity_GetFrame(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 0;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		return a->Frame;
	}
	return 0;
}

SLIME_EXPORT void __cdecl Entity_AdvanceFrame(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		a->Frame++;
	}
}

SLIME_EXPORT void __cdecl Entity_SetSpriteWidth(EntityId id, int width)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		a->SpriteWidth = width;
	}
}

SLIME_EXPORT int __cdecl Entity_GetSpriteWidth(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 0;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		return a->SpriteWidth;
	}
	return 0;
}

SLIME_EXPORT void __cdecl Entity_SetHasAnimation(EntityId id, bool value)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		a->HasAnimation = value;
	}
}

SLIME_EXPORT void __cdecl Entity_SetFrameRate(EntityId id, float rate)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		a->FrameRate = rate;
	}
}

SLIME_EXPORT float __cdecl Entity_GetFrameRate(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 0.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity) id))
	{
		return a->FrameRate;
	}
	return 0.0f;
}

// -------------------------------------------------------------------------
// COMPONENT MANAGEMENT
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_AddComponent_Transform(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<TransformComponent>((Entity) id))
		reg.AddComponent<TransformComponent>((Entity) id, TransformComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Transform(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<TransformComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Transform(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<TransformComponent>((Entity) id))
		reg.RemoveComponent<TransformComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Sprite(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<SpriteComponent>((Entity) id))
		reg.AddComponent<SpriteComponent>((Entity) id, SpriteComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Sprite(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<SpriteComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Sprite(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<SpriteComponent>((Entity) id))
		reg.RemoveComponent<SpriteComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Animation(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<AnimationComponent>((Entity) id))
		reg.AddComponent<AnimationComponent>((Entity) id, AnimationComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Animation(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<AnimationComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Animation(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<AnimationComponent>((Entity) id))
		reg.RemoveComponent<AnimationComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Tag(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<TagComponent>((Entity) id))
		reg.AddComponent<TagComponent>((Entity) id, TagComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Tag(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<TagComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Tag(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<TagComponent>((Entity) id))
		reg.RemoveComponent<TagComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Relationship(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<RelationshipComponent>((Entity) id))
		reg.AddComponent<RelationshipComponent>((Entity) id, RelationshipComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Relationship(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<RelationshipComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Relationship(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<RelationshipComponent>((Entity) id))
		reg.RemoveComponent<RelationshipComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_RigidBody(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<RigidBodyComponent>((Entity) id))
		reg.AddComponent<RigidBodyComponent>((Entity) id, RigidBodyComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_RigidBody(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<RigidBodyComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_RigidBody(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<RigidBodyComponent>((Entity) id))
		reg.RemoveComponent<RigidBodyComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_BoxCollider(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<BoxColliderComponent>((Entity) id))
		reg.AddComponent<BoxColliderComponent>((Entity) id, BoxColliderComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_BoxCollider(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<BoxColliderComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_BoxCollider(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<BoxColliderComponent>((Entity) id))
		reg.RemoveComponent<BoxColliderComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_CircleCollider(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<CircleColliderComponent>((Entity) id))
		reg.AddComponent<CircleColliderComponent>((Entity) id, CircleColliderComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_CircleCollider(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<CircleColliderComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_CircleCollider(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<CircleColliderComponent>((Entity) id))
		reg.RemoveComponent<CircleColliderComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Camera(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<CameraComponent>((Entity) id))
		reg.AddComponent<CameraComponent>((Entity) id, CameraComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Camera(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<CameraComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Camera(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<CameraComponent>((Entity) id))
		reg.RemoveComponent<CameraComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_AudioSource(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<AudioSourceComponent>((Entity) id))
		reg.AddComponent<AudioSourceComponent>((Entity) id, AudioSourceComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_AudioSource(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<AudioSourceComponent>((Entity) id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_AudioSource(EntityId id)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<AudioSourceComponent>((Entity) id))
		reg.RemoveComponent<AudioSourceComponent>((Entity) id);
}

// -------------------------------------------------------------------------
// PHYSICS ACCESSORS
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetVelocity(EntityId id, float x, float y)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
		rb->Velocity = { x, y };
}

SLIME_EXPORT void __cdecl Entity_GetVelocity(EntityId id, float* outX, float* outY)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
	{
		if (outX)
			*outX = rb->Velocity.x;
		if (outY)
			*outY = rb->Velocity.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetMass(EntityId id, float mass)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
		rb->Mass = mass;
}

SLIME_EXPORT float __cdecl Entity_GetMass(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 1.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
		return rb->Mass;
	return 1.0f;
}

SLIME_EXPORT void __cdecl Entity_SetKinematic(EntityId id, bool value)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
		rb->IsKinematic = value;
}

SLIME_EXPORT bool __cdecl Entity_GetKinematic(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
		return rb->IsKinematic;
	return false;
}

SLIME_EXPORT void __cdecl Entity_SetFixedRotation(EntityId id, bool value)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
		rb->FixedRotation = value;
}

SLIME_EXPORT bool __cdecl Entity_GetFixedRotation(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity) id))
		return rb->FixedRotation;
	return false;
}

SLIME_EXPORT void __cdecl Entity_SetColliderSize(EntityId id, float w, float h)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity) id))
		bc->Size = { w, h };
}

SLIME_EXPORT void __cdecl Entity_GetColliderSize(EntityId id, float* outW, float* outH)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity) id))
	{
		if (outW)
			*outW = bc->Size.x;
		if (outH)
			*outH = bc->Size.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetColliderOffset(EntityId id, float x, float y)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity) id))
		bc->Offset = { x, y };
}

SLIME_EXPORT void __cdecl Entity_GetColliderOffset(EntityId id, float* outX, float* outY)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity) id))
	{
		if (outX)
			*outX = bc->Offset.x;
		if (outY)
			*outY = bc->Offset.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetTrigger(EntityId id, bool value)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity) id))
		bc->IsTrigger = value;
}

SLIME_EXPORT bool __cdecl Entity_GetTrigger(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity) id))
		return bc->IsTrigger;
	return false;
}

// -------------------------------------------------------------------------
// CAMERA ACCESSORS
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetCameraSize(EntityId id, float size)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity) id))
		c->OrthographicSize = size;
}

SLIME_EXPORT float __cdecl Entity_GetCameraSize(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 10.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity) id))
		return c->OrthographicSize;
	return 10.0f;
}

SLIME_EXPORT void __cdecl Entity_SetCameraZoom(EntityId id, float zoom)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity) id))
		c->ZoomLevel = zoom;
}

SLIME_EXPORT float __cdecl Entity_GetCameraZoom(EntityId id)
{
	if (!Scene::GetActiveScene())
		return 1.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity) id))
		return c->ZoomLevel;
	return 1.0f;
}

SLIME_EXPORT void __cdecl Entity_SetPrimaryCamera(EntityId id, bool value)
{
	if (!Scene::GetActiveScene())
		return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity) id))
		c->IsPrimary = value;
}

SLIME_EXPORT bool __cdecl Entity_GetPrimaryCamera(EntityId id)
{
	if (!Scene::GetActiveScene())
		return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity) id))
		return c->IsPrimary;
	return false;
}
