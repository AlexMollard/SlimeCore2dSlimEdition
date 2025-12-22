#include "Scripting/EngineExports.h"

#include <cstdio>
#include <iostream>
#include <map>
#include <string>
#include <vector>

#include "Core/Input.h"
#include "Rendering/Renderer2D.h"
#include "Rendering/Text.h"
#include "Rendering/Texture.h"
#include "Rendering/UIManager.h" // Optional, if you still use it for setup
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

// Global storage for UI and Fonts
static std::map<int, PersistentUIElement> s_UIElements;
static std::vector<Text*> s_LoadedFonts;
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
		// NOTE: For text, this is tricky as we need text size.
		// For now, we pass direct position and let Renderer2D/Text handle alignment if implemented.
		// Or, strictly use Center alignment for now.

		glm::vec3 drawPos = glm::vec3(element.Position.x, element.Position.y, 0.9f + (element.Layer * 0.001f));

		if (element.IsText && element.Font)
		{
			// SDF Text Render
			Renderer2D::DrawString(element.TextContent,
			        element.Font,
			        glm::vec2(drawPos.x, drawPos.y),
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
// ENTITY TRANSFORM & VISUALS
// -------------------------------------------------------------------------
SLIME_EXPORT void __cdecl Entity_SetPosition(EntityId id, float x, float y)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		glm::vec3 p = obj->GetPos();
		obj->SetPos(glm::vec3(x, y, p.z));
	}
}

SLIME_EXPORT void __cdecl Entity_GetPosition(EntityId id, float* outX, float* outY)
{
	if (!ObjectManager::IsCreated() || id == 0 || !outX || !outY)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		*outX = obj->GetPos().x;
		*outY = obj->GetPos().y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetSize(EntityId id, float sx, float sy)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetScale(glm::vec2(sx, sy));
}

SLIME_EXPORT void __cdecl Entity_GetSize(EntityId id, float* outSx, float* outSy)
{
	if (!ObjectManager::IsCreated() || id == 0 || !outSx || !outSy)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		*outSx = obj->GetScale().x;
		*outSy = obj->GetScale().y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetColor(EntityId id, float r, float g, float b)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetColor(r, g, b);
}

SLIME_EXPORT void __cdecl Entity_SetLayer(EntityId id, int layer)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetLayer(layer);
}

SLIME_EXPORT int __cdecl Entity_GetLayer(EntityId id)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return 0;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		return obj->GetLayer();
	return 0;
}

SLIME_EXPORT void __cdecl Entity_SetAnchor(EntityId id, float ax, float ay)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
		obj->SetAnchor(glm::vec2(ax, ay));
}

SLIME_EXPORT void __cdecl Entity_GetAnchor(EntityId id, float* outAx, float* outAy)
{
	if (!ObjectManager::IsCreated() || id == 0 || !outAx || !outAy)
		return;
	if (GameObject* obj = ObjectManager::Get().Get((ObjectId) id))
	{
		*outAx = obj->GetAnchor().x;
		*outAy = obj->GetAnchor().y;
	}
}

SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	// In the new system, Texture objects manage the ID.
	// We create a new Texture wrapper for the GameObject.
	// NOTE: This assumes ownership is transferred to the GameObject or it's managed properly.
	// Since we are creating 'new Texture', GameObject's destructor usually doesn't delete textures
	// (as they are usually resources). This is a potential small leak if C# creates textures rapidly.
	// A better way is to check if obj already has a texture with this ID.

	Texture* t = new Texture(width, height, GL_RGBA8);
	t->SetID(texId, width, height);

	if (width > 0)
		obj->SetSpriteWidth(width);
	obj->SetTexture(t);
}

// -------------------------------------------------------------------------
// TEXT & FONT HANDLING (SDF)
// -------------------------------------------------------------------------
SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path)
{
	if (!path)
		return nullptr;
	// Load as SDF (default size 48)
	Text* font = new Text(std::string(path), 48);
	s_LoadedFonts.push_back(font);
	return (void*) font;
}

SLIME_EXPORT void __cdecl Font_Free(void* font)
{
	if (!font)
		return;
	Text* t = (Text*) font;

	// Remove from tracking
	for (auto it = s_LoadedFonts.begin(); it != s_LoadedFonts.end(); ++it)
	{
		if (*it == t)
		{
			s_LoadedFonts.erase(it);
			break;
		}
	}
	delete t;
}

// Deprecated legacy function wrapper
SLIME_EXPORT unsigned int __cdecl Text_CreateTextureFromFontFile(const char* fontPath, const char* text, int pixelHeight, int* outWidth, int* outHeight)
{
	Engine_Log("WARNING: Text_CreateTextureFromFontFile is deprecated in SDF Renderer. Use UI_CreateText.");
	return 0;
}

// Deprecated legacy function wrapper
SLIME_EXPORT unsigned int __cdecl Text_RenderToEntity(void* font, EntityId id, const char* text, int pixelHeight)
{
	Engine_Log("WARNING: Text_RenderToEntity is deprecated. Use UI_CreateText for text rendering.");
	return 0;
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

	// Default font if none provided (Basic fallbacks)
	static Text* defaultFont = nullptr;
	if (!defaultFont)
	{
		std::string path = ResourceManager::GetInstance().GetResourcePath("Fonts\\Chilanka-Regular.ttf");
		if (path.empty())
			path = "..\\Fonts\\Chilanka-Regular.ttf";
		defaultFont = new Text(path, 48);
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
// INPUT (Pass-through)
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
// ANIMATION & FRAME CONTROL
// -------------------------------------------------------------------------
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
	return (EntityId) Entity_CreateQuad(px, py, sx, sy, r, g, b);
}

SLIME_EXPORT EntityId __cdecl ObjectManager_CreateQuadWithTexture(float px, float py, float sx, float sy, unsigned int texId)
{
	if (!ObjectManager::IsCreated())
		return 0;
	// Create with default color, then assign texture
	ObjectId id = ObjectManager::Get().CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(1.0f));
	Entity_SetTexture((EntityId) id, texId, 0, 0); // Reuse SetTexture logic
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
