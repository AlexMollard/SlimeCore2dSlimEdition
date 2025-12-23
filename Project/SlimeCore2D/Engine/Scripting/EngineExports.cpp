#include "Scripting/EngineExports.h"

#include <cstdio>
#include <iostream>
#include <map>
#include <string>
#include <unordered_map>
#include <vector>

#include "Core/Input.h"
#include "Rendering/Renderer2D.h"
#include "Rendering/Text.h"
#include "Rendering/Texture.h"
#include "Resources/ResourceManager.h"
#include "Scene/Scene.h"

// -------------------------------------------------------------------------
// LOGGING
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Engine_Log(const char* msg)
{
	std::cout << "[C#] " << (msg ? msg : "null") << std::endl;
}

// -------------------------------------------------------------------------
// RESOURCES
// -------------------------------------------------------------------------

SLIME_EXPORT void* __cdecl Resources_LoadTexture(const char* name, const char* path)
{
	if (!name)
		return nullptr;
	std::string strName = name;
	std::string strPath = path ? path : ""; // If path is null, it might be resolved by name in RM
	return (void*) ResourceManager::GetInstance().LoadTexture(strName, strPath);
}

SLIME_EXPORT void* __cdecl Resources_GetTexture(const char* name)
{
	if (!name)
		return nullptr;
	return (void*) ResourceManager::GetInstance().GetTexture(name);
}

SLIME_EXPORT void* __cdecl Resources_LoadFont(const char* name, const char* path, int fontSize)
{
	if (!name)
		return nullptr;
	std::string strName = name;
	std::string strPath = path ? path : "";
	return (void*) ResourceManager::GetInstance().LoadFont(strName, strPath, fontSize);
}

// Legacy / Deprecated Wrapper (maintains binary compatibility)
SLIME_EXPORT void* __cdecl Texture_Load(const char* path)
{
	if (!path)
		return nullptr;
	// Use path as the key name
	return (void*) ResourceManager::GetInstance().LoadTexture(path, path);
}

// Legacy / Deprecated Wrapper
SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path)
{
	if (!path)
		return nullptr;
	// Default to 48px for legacy loading
	return (void*) ResourceManager::GetInstance().LoadFont(path, path, 48);
}

SLIME_EXPORT void __cdecl Font_Free(void* font)
{
	// ResourceManager owns the pointers now.
	// We do not delete them here to prevent double-free issues if C# calls this manually.
	// If explicit unloading is required, add UnloadResource to RM.
}

// -------------------------------------------------------------------------
// ENTITY LIFECYCLE (Delegates to Scene)
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
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		t->Position.x = x;
		t->Position.y = y;
	}
}

SLIME_EXPORT void __cdecl Entity_GetPosition(EntityId id, float* outX, float* outY)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		if (outX) *outX = t->Position.x;
		if (outY) *outY = t->Position.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetSize(EntityId id, float sx, float sy)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		t->Scale = { sx, sy };
	}
}

SLIME_EXPORT void __cdecl Entity_GetSize(EntityId id, float* outSx, float* outSy)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		if (outSx) *outSx = t->Scale.x;
		if (outSy) *outSy = t->Scale.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetColor(EntityId id, float r, float g, float b)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		s->Color = { r, g, b, 1.0f };
	}
}

SLIME_EXPORT void __cdecl Entity_GetColor(EntityId id, float* outR, float* outG, float* outB)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		if (outR) *outR = s->Color.r;
		if (outG) *outG = s->Color.g;
		if (outB) *outB = s->Color.b;
	}
}

SLIME_EXPORT void __cdecl Entity_SetLayer(EntityId id, int layer)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		t->Position.z = layer * 0.1f;
	}
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		s->Layer = layer;
	}
}

SLIME_EXPORT int __cdecl Entity_GetLayer(EntityId id)
{
	if (!Scene::GetActiveScene()) return 0;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		return s->Layer;
	}
	return 0;
}

SLIME_EXPORT void __cdecl Entity_SetRotation(EntityId id, float degrees)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		t->Rotation = degrees;
	}
}

SLIME_EXPORT float __cdecl Entity_GetRotation(EntityId id)
{
	if (!Scene::GetActiveScene()) return 0.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		return t->Rotation;
	}
	return 0.0f;
}

SLIME_EXPORT void __cdecl Entity_SetAnchor(EntityId id, float ax, float ay)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		t->Anchor = { ax, ay };
	}
}

SLIME_EXPORT void __cdecl Entity_GetAnchor(EntityId id, float* outAx, float* outAy)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* t = reg.TryGetComponent<TransformComponent>((Entity)id))
	{
		if (outAx) *outAx = t->Anchor.x;
		if (outAy) *outAy = t->Anchor.y;
	}
}

// -------------------------------------------------------------------------
// ENTITY VISUALS & ANIMATION
// -------------------------------------------------------------------------

// Assigns a texture by raw OpenGL ID (Low-level)
SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		// Create temp texture wrapper
		// Note: This leaks if we don't manage it. 
		// Ideally Texture* in SpriteComponent should be managed resource.
		// For now, we just new it as before.
		Texture* t = new Texture(width, height, GL_RGBA8);
		t->SetID(texId, width, height);
		s->Texture = t;
	}
	
	if (width > 0)
	{
		if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
		{
			a->SpriteWidth = width;
		}
	}
}

// Assigns a Texture pointer retrieved via Resources_LoadTexture
SLIME_EXPORT void __cdecl Entity_SetTexturePtr(EntityId id, void* texPtr)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		if (texPtr)
		{
			Texture* tex = (Texture*) texPtr;
			s->Texture = tex;
			
			if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
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
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		return (void*) s->Texture;
	}
	return nullptr;
}

SLIME_EXPORT void __cdecl Entity_SetRender(EntityId id, bool value)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		s->IsVisible = value;
	}
}

SLIME_EXPORT bool __cdecl Entity_GetRender(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* s = reg.TryGetComponent<SpriteComponent>((Entity)id))
	{
		return s->IsVisible;
	}
	return false;
}

SLIME_EXPORT void __cdecl Entity_SetFrame(EntityId id, int frame)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		a->Frame = frame;
	}
}

SLIME_EXPORT int __cdecl Entity_GetFrame(EntityId id)
{
	if (!Scene::GetActiveScene()) return 0;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		return a->Frame;
	}
	return 0;
}

SLIME_EXPORT void __cdecl Entity_AdvanceFrame(EntityId id)
{
	// Manual advance not fully implemented in component logic yet, 
	// but we can just increment frame.
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		a->Frame++;
	}
}

SLIME_EXPORT void __cdecl Entity_SetSpriteWidth(EntityId id, int width)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		a->SpriteWidth = width;
	}
}

SLIME_EXPORT int __cdecl Entity_GetSpriteWidth(EntityId id)
{
	if (!Scene::GetActiveScene()) return 0;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		return a->SpriteWidth;
	}
	return 0;
}

SLIME_EXPORT void __cdecl Entity_SetHasAnimation(EntityId id, bool value)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		a->HasAnimation = value;
	}
}

SLIME_EXPORT void __cdecl Entity_SetFrameRate(EntityId id, float rate)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		a->FrameRate = rate;
	}
}

SLIME_EXPORT float __cdecl Entity_GetFrameRate(EntityId id)
{
	if (!Scene::GetActiveScene()) return 0.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* a = reg.TryGetComponent<AnimationComponent>((Entity)id))
	{
		return a->FrameRate;
	}
	return 0.0f;
}

// -------------------------------------------------------------------------
// INPUT
// -------------------------------------------------------------------------
SLIME_EXPORT bool __cdecl Input_GetKeyDown(int key)
{
	return Input::GetInstance()->GetKey(Keycode(key));
}

SLIME_EXPORT bool __cdecl Input_GetKeyReleased(int key)
{
	return Input::GetInstance()->GetKeyUp(Keycode(key));
}

SLIME_EXPORT bool __cdecl Input_GetMouseDown(int button)
{
	return Input::GetInstance()->GetMouseButtonDown(button);
}

SLIME_EXPORT void __cdecl Input_GetMousePos(float* outX, float* outY)
{
	if (!outX || !outY)
		return;
	auto p = Input::GetInstance()->GetMousePosition();
	*outX = (float) p.x;
	*outY = (float) p.y;
}

SLIME_EXPORT void __cdecl Input_GetMouseToWorldPos(float* outX, float* outY)
{
	if (!outX || !outY)
		return;
	auto p = Input::GetInstance()->GetMousePositionWorld();
	*outX = (float) p.x;
	*outY = (float) p.y;
}

SLIME_EXPORT void __cdecl Input_SetViewportRect(int x, int y, int w, int h)
{
	Input::GetInstance()->SetViewportRect(x, y, w, h);
}

SLIME_EXPORT void __cdecl Input_GetViewportRect(int* x, int* y, int* w, int* h)
{
	if (!x || !y || !w || !h)
		return;
	auto v = Input::GetInstance()->GetViewportRect();
	*x = (int) v.x;
	*y = (int) v.y;
	*w = (int) v.z;
	*h = (int) v.w;
}

SLIME_EXPORT void __cdecl Input_SetScroll(float v, float h)
{
	Input::GetInstance()->SetScrollInternal(v, h);
}

SLIME_EXPORT float __cdecl Input_GetScroll()
{
	return Input::GetInstance()->GetScroll();
}

// -------------------------------------------------------------------------
// UI SYSTEM (Using Persistent Bridge)
// -------------------------------------------------------------------------
SLIME_EXPORT EntityId __cdecl UI_CreateText(const char* text, int fontSize, float x, float y)
{
	if (!Scene::GetActiveScene())
		return 0;

	ObjectId id = Scene::GetActiveScene()->CreateUIElement(true);
	PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement(id);
	if (!el)
		return 0;

	el->TextContent = text ? std::string(text) : " ";
	el->Position = { x, y };

	// Calculate relative scale (SDF is baked at 48px)
	float scale = (float) fontSize / 48.0f;
	el->Scale = { scale, scale };

	// Default font resolution logic
	// Check if a font named "DefaultFont" is loaded
	Text* defaultFont = ResourceManager::GetInstance().GetFont("DefaultFont");
	if (!defaultFont)
	{
		// Try to load a fallback from common locations if not found
		defaultFont = ResourceManager::GetInstance().LoadFont("DefaultFont", "Fonts/Chilanka-Regular.ttf", 48);
	}
	el->Font = defaultFont;

	return (EntityId) id;
}

SLIME_EXPORT void __cdecl UI_Destroy(EntityId id)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	Scene::GetActiveScene()->DestroyObject((ObjectId) id);
}

SLIME_EXPORT void __cdecl UI_SetText(EntityId id, const char* text)
{
	if (!Scene::GetActiveScene() || id == 0 || !text)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->TextContent = text;
	}
}

SLIME_EXPORT void __cdecl UI_SetPosition(EntityId id, float x, float y)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->Position = { x, y };
	}
}

SLIME_EXPORT void __cdecl UI_GetPosition(EntityId id, float* outX, float* outY)
{
	if (!Scene::GetActiveScene() || id == 0 || (!outX && !outY))
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		if (outX)
			*outX = el->Position.x;
		if (outY)
			*outY = el->Position.y;
	}
	else
	{
		if (outX)
			*outX = 0.0f;
		if (outY)
			*outY = 0.0f;
	}
}

SLIME_EXPORT void __cdecl UI_SetColor(EntityId id, float r, float g, float b)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->Color = { r, g, b, 1.0f };
	}
}

SLIME_EXPORT void __cdecl UI_SetVisible(EntityId id, bool visible)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->IsVisible = visible;
	}
}

SLIME_EXPORT void __cdecl UI_SetLayer(EntityId id, int layer)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->Layer = layer;
	}
}

SLIME_EXPORT void __cdecl UI_SetAnchor(EntityId id, float ax, float ay)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->Anchor = { ax, ay };
	}
}

SLIME_EXPORT void __cdecl UI_SetUseScreenSpace(EntityId id, bool useScreenSpace)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->UseScreenSpace = useScreenSpace;
	}
}

SLIME_EXPORT void __cdecl UI_GetTextSize(EntityId id, float* outWidth, float* outHeight)
{
	if (!Scene::GetActiveScene() || id == 0 || (!outWidth && !outHeight))
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		if (el->IsText && el->Font)
		{
			glm::vec2 size = el->Font->CalculateSize(el->TextContent, el->Scale.x);
			if (outWidth)
				*outWidth = size.x;
			if (outHeight)
				*outHeight = size.y;
		}
		else
		{
			if (outWidth)
				*outWidth = 0.0f;
			if (outHeight)
				*outHeight = 0.0f;
		}
	}
}

SLIME_EXPORT float __cdecl UI_GetTextWidth(EntityId id)
{
	if (!Scene::GetActiveScene() || id == 0)
		return 0.0f;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		if (el->IsText && el->Font)
		{
			glm::vec2 size = el->Font->CalculateSize(el->TextContent, el->Scale.x);
			return size.x;
		}
	}
	return 0.0f;
}

SLIME_EXPORT float __cdecl UI_GetTextHeight(EntityId id)
{
	if (!Scene::GetActiveScene() || id == 0)
		return 0.0f;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		if (el->IsText && el->Font)
		{
			glm::vec2 size = el->Font->CalculateSize(el->TextContent, el->Scale.x);
			return size.y;
		}
	}
	return 0.0f;
}

// -------------------------------------------------------------------------
// COMPONENT MANAGEMENT
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_AddComponent_Transform(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<TransformComponent>((Entity)id))
		reg.AddComponent<TransformComponent>((Entity)id, TransformComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Transform(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<TransformComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Transform(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<TransformComponent>((Entity)id))
		reg.RemoveComponent<TransformComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Sprite(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<SpriteComponent>((Entity)id))
		reg.AddComponent<SpriteComponent>((Entity)id, SpriteComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Sprite(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<SpriteComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Sprite(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<SpriteComponent>((Entity)id))
		reg.RemoveComponent<SpriteComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Animation(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<AnimationComponent>((Entity)id))
		reg.AddComponent<AnimationComponent>((Entity)id, AnimationComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Animation(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<AnimationComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Animation(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<AnimationComponent>((Entity)id))
		reg.RemoveComponent<AnimationComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Tag(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<TagComponent>((Entity)id))
		reg.AddComponent<TagComponent>((Entity)id, TagComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Tag(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<TagComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Tag(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<TagComponent>((Entity)id))
		reg.RemoveComponent<TagComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_AddComponent_Relationship(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<RelationshipComponent>((Entity)id))
		reg.AddComponent<RelationshipComponent>((Entity)id, RelationshipComponent());
}

SLIME_EXPORT bool __cdecl Entity_HasComponent_Relationship(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<RelationshipComponent>((Entity)id);
}

SLIME_EXPORT void __cdecl Entity_RemoveComponent_Relationship(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<RelationshipComponent>((Entity)id))
		reg.RemoveComponent<RelationshipComponent>((Entity)id);
}

// -------------------------------------------------------------------------
// NEW COMPONENTS
// -------------------------------------------------------------------------

// RigidBody
SLIME_EXPORT void __cdecl Entity_AddComponent_RigidBody(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<RigidBodyComponent>((Entity)id))
		reg.AddComponent<RigidBodyComponent>((Entity)id, RigidBodyComponent());
}
SLIME_EXPORT bool __cdecl Entity_HasComponent_RigidBody(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<RigidBodyComponent>((Entity)id);
}
SLIME_EXPORT void __cdecl Entity_RemoveComponent_RigidBody(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<RigidBodyComponent>((Entity)id))
		reg.RemoveComponent<RigidBodyComponent>((Entity)id);
}

// BoxCollider
SLIME_EXPORT void __cdecl Entity_AddComponent_BoxCollider(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<BoxColliderComponent>((Entity)id))
		reg.AddComponent<BoxColliderComponent>((Entity)id, BoxColliderComponent());
}
SLIME_EXPORT bool __cdecl Entity_HasComponent_BoxCollider(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<BoxColliderComponent>((Entity)id);
}
SLIME_EXPORT void __cdecl Entity_RemoveComponent_BoxCollider(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<BoxColliderComponent>((Entity)id))
		reg.RemoveComponent<BoxColliderComponent>((Entity)id);
}

// CircleCollider
SLIME_EXPORT void __cdecl Entity_AddComponent_CircleCollider(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<CircleColliderComponent>((Entity)id))
		reg.AddComponent<CircleColliderComponent>((Entity)id, CircleColliderComponent());
}
SLIME_EXPORT bool __cdecl Entity_HasComponent_CircleCollider(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<CircleColliderComponent>((Entity)id);
}
SLIME_EXPORT void __cdecl Entity_RemoveComponent_CircleCollider(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<CircleColliderComponent>((Entity)id))
		reg.RemoveComponent<CircleColliderComponent>((Entity)id);
}

// Camera
SLIME_EXPORT void __cdecl Entity_AddComponent_Camera(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<CameraComponent>((Entity)id))
		reg.AddComponent<CameraComponent>((Entity)id, CameraComponent());
}
SLIME_EXPORT bool __cdecl Entity_HasComponent_Camera(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<CameraComponent>((Entity)id);
}
SLIME_EXPORT void __cdecl Entity_RemoveComponent_Camera(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<CameraComponent>((Entity)id))
		reg.RemoveComponent<CameraComponent>((Entity)id);
}

// AudioSource
SLIME_EXPORT void __cdecl Entity_AddComponent_AudioSource(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (!reg.HasComponent<AudioSourceComponent>((Entity)id))
		reg.AddComponent<AudioSourceComponent>((Entity)id, AudioSourceComponent());
}
SLIME_EXPORT bool __cdecl Entity_HasComponent_AudioSource(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	return reg.HasComponent<AudioSourceComponent>((Entity)id);
}
SLIME_EXPORT void __cdecl Entity_RemoveComponent_AudioSource(EntityId id)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (reg.HasComponent<AudioSourceComponent>((Entity)id))
		reg.RemoveComponent<AudioSourceComponent>((Entity)id);
}

// -------------------------------------------------------------------------
// PHYSICS ACCESSORS
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetVelocity(EntityId id, float x, float y)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity)id))
		rb->Velocity = { x, y };
}

SLIME_EXPORT void __cdecl Entity_GetVelocity(EntityId id, float* outX, float* outY)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity)id))
	{
		if (outX) *outX = rb->Velocity.x;
		if (outY) *outY = rb->Velocity.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetMass(EntityId id, float mass)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity)id))
		rb->Mass = mass;
}

SLIME_EXPORT float __cdecl Entity_GetMass(EntityId id)
{
	if (!Scene::GetActiveScene()) return 1.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity)id))
		return rb->Mass;
	return 1.0f;
}

SLIME_EXPORT void __cdecl Entity_SetKinematic(EntityId id, bool value)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity)id))
		rb->IsKinematic = value;
}

SLIME_EXPORT bool __cdecl Entity_GetKinematic(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* rb = reg.TryGetComponent<RigidBodyComponent>((Entity)id))
		return rb->IsKinematic;
	return false;
}

SLIME_EXPORT void __cdecl Entity_SetColliderSize(EntityId id, float w, float h)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity)id))
		bc->Size = { w, h };
}

SLIME_EXPORT void __cdecl Entity_GetColliderSize(EntityId id, float* outW, float* outH)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity)id))
	{
		if (outW) *outW = bc->Size.x;
		if (outH) *outH = bc->Size.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetColliderOffset(EntityId id, float x, float y)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity)id))
		bc->Offset = { x, y };
}

SLIME_EXPORT void __cdecl Entity_GetColliderOffset(EntityId id, float* outX, float* outY)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity)id))
	{
		if (outX) *outX = bc->Offset.x;
		if (outY) *outY = bc->Offset.y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetTrigger(EntityId id, bool value)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity)id))
		bc->IsTrigger = value;
}

SLIME_EXPORT bool __cdecl Entity_GetTrigger(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* bc = reg.TryGetComponent<BoxColliderComponent>((Entity)id))
		return bc->IsTrigger;
	return false;
}

// -------------------------------------------------------------------------
// CAMERA ACCESSORS
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetCameraSize(EntityId id, float size)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity)id))
		c->OrthographicSize = size;
}

SLIME_EXPORT float __cdecl Entity_GetCameraSize(EntityId id)
{
	if (!Scene::GetActiveScene()) return 10.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity)id))
		return c->OrthographicSize;
	return 10.0f;
}

SLIME_EXPORT void __cdecl Entity_SetCameraZoom(EntityId id, float zoom)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity)id))
		c->ZoomLevel = zoom;
}

SLIME_EXPORT float __cdecl Entity_GetCameraZoom(EntityId id)
{
	if (!Scene::GetActiveScene()) return 1.0f;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity)id))
		return c->ZoomLevel;
	return 1.0f;
}

SLIME_EXPORT void __cdecl Entity_SetPrimaryCamera(EntityId id, bool value)
{
	if (!Scene::GetActiveScene()) return;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity)id))
		c->IsPrimary = value;
}

SLIME_EXPORT bool __cdecl Entity_GetPrimaryCamera(EntityId id)
{
	if (!Scene::GetActiveScene()) return false;
	auto& reg = Scene::GetActiveScene()->GetRegistry();
	if (auto* c = reg.TryGetComponent<CameraComponent>((Entity)id))
		return c->IsPrimary;
	return false;
}

// -------------------------------------------------------------------------
// SCENE WRAPPERS
// -------------------------------------------------------------------------
SLIME_EXPORT EntityId __cdecl Scene_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b)
{
	if (!Scene::GetActiveScene())
		return 0;
	return (EntityId) Scene::GetActiveScene()->CreateGameObject(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(r, g, b));
}

SLIME_EXPORT EntityId __cdecl Scene_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
{
	return Entity_CreateQuad(px, py, sx, sy, r, g, b);
}

SLIME_EXPORT EntityId __cdecl Scene_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId)
{
	if (!Scene::GetActiveScene())
		return 0;
	ObjectId id = Scene::GetActiveScene()->CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(1.0f));
	Entity_SetTexture((EntityId) id, texId, 0, 0);
	return (EntityId) id;
}

SLIME_EXPORT void __cdecl Scene_Destroy(EntityId id)
{
	Entity_Destroy(id);
}

SLIME_EXPORT bool __cdecl Scene_IsAlive(EntityId id)
{
	return Entity_IsAlive(id);
}

SLIME_EXPORT int __cdecl Scene_GetEntityCount()
{
	return Scene::GetActiveScene() ? Scene::GetActiveScene()->GetObjectCount() : 0;
}

SLIME_EXPORT EntityId __cdecl Scene_GetEntityIdAtIndex(int index)
{
	return Scene::GetActiveScene() ? (EntityId) Scene::GetActiveScene()->GetIdAtIndex(index) : 0;
}
