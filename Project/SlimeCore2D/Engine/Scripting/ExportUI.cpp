#include "Scripting/ExportUI.h"
#include "Scene/Scene.h"
#include "Resources/ResourceManager.h"
#include "Rendering/Text.h"
#include <string>

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

	float scale = (float) fontSize / 48.0f;
	el->Scale = { scale, scale };

	Text* defaultFont = ResourceManager::GetInstance().GetFont("DefaultFont");
	if (!defaultFont)
	{
		defaultFont = ResourceManager::GetInstance().LoadFont("DefaultFont", "Fonts/Chilanka-Regular.ttf", 48);
	}
	el->Font = defaultFont;

	return (EntityId) id;
}

SLIME_EXPORT EntityId __cdecl UI_CreateImage(float x, float y, float w, float h)
{
	if (!Scene::GetActiveScene())
		return 0;

	ObjectId id = Scene::GetActiveScene()->CreateUIElement(false);
	PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement(id);
	if (!el)
		return 0;

	el->Position = { x, y };
	el->Scale = { w, h };
	el->IsText = false;

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
		if (outX) *outX = el->Position.x;
		if (outY) *outY = el->Position.y;
	}
}

SLIME_EXPORT void __cdecl UI_SetSize(EntityId id, float w, float h)
{
	if (!Scene::GetActiveScene() || id == 0)
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		el->Scale = { w, h };
	}
}

SLIME_EXPORT void __cdecl UI_GetSize(EntityId id, float* outW, float* outH)
{
	if (!Scene::GetActiveScene() || id == 0 || (!outW && !outH))
		return;
	if (PersistentUIElement* el = Scene::GetActiveScene()->GetUIElement((ObjectId) id))
	{
		if (outW) *outW = el->Scale.x;
		if (outH) *outH = el->Scale.y;
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
			if (outWidth) *outWidth = size.x;
			if (outHeight) *outHeight = size.y;
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
			return el->Font->CalculateSize(el->TextContent, el->Scale.x).x;
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
			return el->Font->CalculateSize(el->TextContent, el->Scale.x).y;
		}
	}
	return 0.0f;
}
