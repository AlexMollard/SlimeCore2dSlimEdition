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
#include "Rendering/UIManager.h"
#include "Resources/ResourceManager.h"
#include "Utils/ObjectManager.h"

// -------------------------------------------------------------------------
// INTERNAL STATE BRIDGE (Retained Mode -> Immediate Mode)
// -------------------------------------------------------------------------

// We need this struct to store UI state between frames because C# sets it once,
// but Renderer2D needs it drawn every frame.
struct PersistentUIElement
{
	bool IsVisible = true;
	bool IsText = false;

	// Transform
	glm::vec2 Position = { 0.0f, 0.0f };
	glm::vec2 Scale = { 1.0f, 1.0f }; // Doubles as FontSize relative to 48px
	glm::vec2 Anchor = { 0.5f, 0.5f };
	glm::vec4 Color = { 1.0f, 1.0f, 1.0f, 1.0f };
	int Layer = 0;

	// Content
	std::string TextContent;
	Text* Font = nullptr;     // Pointer to SDF Atlas
	Texture* Image = nullptr; // Pointer to standard Texture
};

// Global storage for UI
static std::map<int, PersistentUIElement> s_UIElements;
static int s_NextUIID = 1;

// -------------------------------------------------------------------------
// PUBLIC API FOR GAME LOOP (Call this in your C++ Main Loop!)
// -------------------------------------------------------------------------
void EngineExports_RenderUI()
{
	// Iterate over all persistent UI elements and submit them to the immediate renderer
	for (auto& [id, element]: s_UIElements)
	{
		if (!element.IsVisible)
			continue;

		// Calculate Draw Position based on Anchor
		// Renderer2D draws at center/position. We need to offset based on anchor.
		glm::vec3 drawPos = glm::vec3(element.Position.x, element.Position.y, 0.9f + (element.Layer * 0.001f));

		if (element.IsText && element.Font)
		{
			// Anchor-aware positioning for text: measure and offset
			glm::vec2 textSize = element.Font->CalculateSize(element.TextContent, element.Scale.x);
			glm::vec3 finalPos = drawPos;
			finalPos.x += (0.5f - element.Anchor.x) * textSize.x;
			finalPos.y += (0.5f - element.Anchor.y) * textSize.y;

			// SDF Text Render
			Renderer2D::DrawString(element.TextContent,
			        element.Font,
			        glm::vec2(finalPos.x, finalPos.y),
			        element.Scale.x, // Scale 1.0 = 48px
			        element.Color);
		}
		else if (!element.IsText)
		{
			// UI Quad Render
			// Apply anchor offset logic for Quads
			// (0.5 - Anchor) * Scale
			float offX = (0.5f - element.Anchor.x) * element.Scale.x;
			float offY = (0.5f - element.Anchor.y) * element.Scale.y;

			glm::vec3 finalPos = drawPos;
			finalPos.x += offX;
			finalPos.y += offY;

			if (element.Image)
			{
				Renderer2D::DrawQuad(finalPos, element.Scale, element.Image, 1.0f, element.Color);
			}
			else
			{
				Renderer2D::DrawQuad(finalPos, element.Scale, element.Color);
			}
		}
	}
}

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
// ENTITY LIFECYCLE (Delegates to ObjectManager)
// -------------------------------------------------------------------------
SLIME_EXPORT EntityId __cdecl Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
{
	if (!ObjectManager::IsCreated())
		return 0;
	return (EntityId) ObjectManager::Get().CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(r, g, b));
}

SLIME_EXPORT void __cdecl Entity_Destroy(EntityId id)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	ObjectManager::Get().DestroyObject((ObjectId) id);
}

SLIME_EXPORT bool __cdecl Entity_IsAlive(EntityId id)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return false;
	return ObjectManager::Get().IsAlive((ObjectId) id);
}

// -------------------------------------------------------------------------
// ENTITY TRANSFORM
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetPosition(EntityId id, float x, float y)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		glm::vec3 p = obj->GetPos();
		obj->SetPos(glm::vec3(x, y, p.z));
	}
}

SLIME_EXPORT void __cdecl Entity_GetPosition(EntityId id, float* outX, float* outY)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		if (outX)
			*outX = obj->GetPos().x;
		if (outY)
			*outY = obj->GetPos().y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetSize(EntityId id, float sx, float sy)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetScale(glm::vec2(sx, sy));
}

SLIME_EXPORT void __cdecl Entity_GetSize(EntityId id, float* outSx, float* outSy)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		if (outSx)
			*outSx = obj->GetScale().x;
		if (outSy)
			*outSy = obj->GetScale().y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetColor(EntityId id, float r, float g, float b)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetColor(r, g, b);
}

SLIME_EXPORT void __cdecl Entity_SetLayer(EntityId id, int layer)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetLayer(layer);
}

SLIME_EXPORT int __cdecl Entity_GetLayer(EntityId id)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		return obj->GetLayer();
	return 0;
}

SLIME_EXPORT void __cdecl Entity_SetRotation(EntityId id, float degrees)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetRotation(degrees);
}

SLIME_EXPORT float __cdecl Entity_GetRotation(EntityId id)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		return obj->GetRotation();
	return 0.0f;
}

SLIME_EXPORT void __cdecl Entity_SetAnchor(EntityId id, float ax, float ay)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetAnchor(glm::vec2(ax, ay));
}

SLIME_EXPORT void __cdecl Entity_GetAnchor(EntityId id, float* outAx, float* outAy)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		if (outAx)
			*outAx = obj->GetAnchor().x;
		if (outAy)
			*outAy = obj->GetAnchor().y;
	}
}

// -------------------------------------------------------------------------
// ENTITY VISUALS & ANIMATION
// -------------------------------------------------------------------------

// Assigns a texture by raw OpenGL ID (Low-level)
SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	// Note: We create a loose Texture object wrapper here.
	// This is a temporary wrapper just so the GameObject can hold the ID.
	Texture* t = new Texture(width, height, GL_RGBA8);
	t->SetID(texId, width, height);

	if (width > 0)
		obj->SetSpriteWidth(width);
	obj->SetTexture(t);
}

// Assigns a Texture pointer retrieved via Resources_LoadTexture
SLIME_EXPORT void __cdecl Entity_SetTexturePtr(EntityId id, void* texPtr)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	if (texPtr)
	{
		Texture* tex = (Texture*) texPtr;
		obj->SetTexture(tex);
		// Automatically update the sprite width to match the texture
		obj->SetSpriteWidth(tex->GetWidth());
	}
	else
	{
		obj->SetTexture(nullptr);
	}
}

SLIME_EXPORT void* __cdecl Entity_GetTexturePtr(EntityId id)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return nullptr;

	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return nullptr;

	return (void*) obj->GetTexture();
}

SLIME_EXPORT void __cdecl Entity_SetRender(EntityId id, bool value)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetRender(value);
}

SLIME_EXPORT bool __cdecl Entity_GetRender(EntityId id)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		return obj->GetRender();
	return false;
}

SLIME_EXPORT void __cdecl Entity_SetFrame(EntityId id, int frame)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetFrame(frame);
}

SLIME_EXPORT int __cdecl Entity_GetFrame(EntityId id)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		return obj->GetFrame();
	return 0;
}

SLIME_EXPORT void __cdecl Entity_AdvanceFrame(EntityId id)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->AdvanceFrame();
}

SLIME_EXPORT void __cdecl Entity_SetSpriteWidth(EntityId id, int width)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetSpriteWidth(width);
}

SLIME_EXPORT int __cdecl Entity_GetSpriteWidth(EntityId id)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		return obj->GetSpriteWidth();
	return 0;
}

SLIME_EXPORT void __cdecl Entity_SetHasAnimation(EntityId id, bool value)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetHasAnimation(value);
}

SLIME_EXPORT void __cdecl Entity_SetFrameRate(EntityId id, float rate)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetFrameRate(rate);
}

SLIME_EXPORT float __cdecl Entity_GetFrameRate(EntityId id)
{
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		return obj->GetFrameRate();
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
	int id = s_NextUIID++;

	PersistentUIElement el;
	el.IsText = true;
	el.TextContent = text ? std::string(text) : " ";
	el.Position = { x, y };

	// Calculate relative scale (SDF is baked at 48px)
	float scale = (float) fontSize / 48.0f;
	el.Scale = { scale, scale };

	// Default font resolution logic
	// Check if a font named "DefaultFont" is loaded
	Text* defaultFont = ResourceManager::GetInstance().GetFont("DefaultFont");
	if (!defaultFont)
	{
		// Try to load a fallback from common locations if not found
		defaultFont = ResourceManager::GetInstance().LoadFont("DefaultFont", "Fonts/Chilanka-Regular.ttf", 48);
	}
	el.Font = defaultFont;

	s_UIElements[id] = el;
	return (EntityId) id;
}

SLIME_EXPORT void __cdecl UI_Destroy(EntityId id)
{
	if (id == 0)
		return;
	s_UIElements.erase((int) id);
}

SLIME_EXPORT void __cdecl UI_SetText(EntityId id, const char* text)
{
	if (id == 0 || !text)
		return;
	auto it = s_UIElements.find((int) id);
	if (it != s_UIElements.end())
	{
		it->second.TextContent = text;
	}
}

SLIME_EXPORT void __cdecl UI_SetPosition(EntityId id, float x, float y)
{
	if (id == 0)
		return;
	auto it = s_UIElements.find((int) id);
	if (it != s_UIElements.end())
	{
		it->second.Position = { x, y };
	}
}

SLIME_EXPORT void __cdecl UI_SetColor(EntityId id, float r, float g, float b)
{
	if (id == 0)
		return;
	auto it = s_UIElements.find((int) id);
	if (it != s_UIElements.end())
	{
		it->second.Color = { r, g, b, 1.0f };
	}
}

SLIME_EXPORT void __cdecl UI_SetVisible(EntityId id, bool visible)
{
	if (id == 0)
		return;
	auto it = s_UIElements.find((int) id);
	if (it != s_UIElements.end())
	{
		it->second.IsVisible = visible;
	}
}

SLIME_EXPORT void __cdecl UI_SetLayer(EntityId id, int layer)
{
	if (id == 0)
		return;
	auto it = s_UIElements.find((int) id);
	if (it != s_UIElements.end())
	{
		it->second.Layer = layer;
	}
}

SLIME_EXPORT void __cdecl UI_SetAnchor(EntityId id, float ax, float ay)
{
	if (id == 0)
		return;
	auto it = s_UIElements.find((int) id);
	if (it != s_UIElements.end())
	{
		it->second.Anchor = { ax, ay };
	}
}

// -------------------------------------------------------------------------
// OBJECT MANAGER WRAPPERS
// -------------------------------------------------------------------------
SLIME_EXPORT EntityId __cdecl ObjectManager_CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b)
{
	if (!ObjectManager::IsCreated())
		return 0;
	return (EntityId) ObjectManager::Get().CreateGameObject(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(r, g, b));
}

SLIME_EXPORT EntityId __cdecl ObjectManager_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
{
	return Entity_CreateQuad(px, py, sx, sy, r, g, b);
}

SLIME_EXPORT EntityId __cdecl ObjectManager_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId)
{
	if (!ObjectManager::IsCreated())
		return 0;
	ObjectId id = ObjectManager::Get().CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(1.0f));
	Entity_SetTexture((EntityId) id, texId, 0, 0);
	return (EntityId) id;
}

SLIME_EXPORT void __cdecl ObjectManager_Destroy(EntityId id)
{
	Entity_Destroy(id);
}

SLIME_EXPORT bool __cdecl ObjectManager_IsAlive(EntityId id)
{
	return Entity_IsAlive(id);
}

SLIME_EXPORT int __cdecl ObjectManager_GetSize()
{
	return ObjectManager::IsCreated() ? ObjectManager::Get().Size() : 0;
}

SLIME_EXPORT EntityId __cdecl ObjectManager_GetIdAtIndex(int index)
{
	return ObjectManager::IsCreated() ? (EntityId) ObjectManager::Get().GetIdAtIndex(index) : 0;
}
