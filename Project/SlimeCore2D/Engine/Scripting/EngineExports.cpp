#include "Scripting/EngineExports.h"

#include <cstdio>

#include "Utils/ObjectManager.h"
#include "Resources/ResourceManager.h"
#include "Rendering/Text.h"
#include "Rendering/Texture.h"

SLIME_EXPORT void __cdecl Engine_Log(const char* msg)
{
	std::printf("[C#] %s\n", msg ? msg : "<null>");
	std::fflush(stdout);
}

SLIME_EXPORT EntityId __cdecl Entity_CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
{
	if (!ObjectManager::IsCreated())
		return 0;

	ObjectId id = ObjectManager::Get().CreateQuad(glm::vec3(px, py, 0.0f), glm::vec2(sx, sy), glm::vec3(r, g, b));

	return (EntityId) id;
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

SLIME_EXPORT void __cdecl Transform_SetPosition(EntityId id, float x, float y)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;

	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	obj->SetPos(glm::vec3(x, y, 0.0f));
}

SLIME_EXPORT void __cdecl Transform_GetPosition(EntityId id, float* outX, float* outY)
{
	if (!ObjectManager::IsCreated() || id == 0 || !outX || !outY)
		return;

	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	glm::vec3 p = obj->GetPos();

	*outX = p.x;
	*outY = p.y;
}

SLIME_EXPORT void __cdecl Transform_SetSize(EntityId id, float sx, float sy)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;
	obj->SetScale(glm::vec2(sx, sy));
}

SLIME_EXPORT void __cdecl Transform_GetSize(EntityId id, float* outSx, float* outSy)
{
	if (!ObjectManager::IsCreated() || id == 0 || !outSx || !outSy)
		return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;
	glm::vec2 s = obj->GetScale();
	*outSx = s.x;
	*outSy = s.y;
}

SLIME_EXPORT void __cdecl Visual_SetColor(EntityId id, float r, float g, float b)
{
	if (!ObjectManager::IsCreated() || id == 0)
		return;

	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj)
		return;

	obj->SetColor(r, g, b);
}

SLIME_EXPORT void __cdecl Visual_SetLayer(EntityId id, int layer)
{
	if (!ObjectManager::IsCreated() || id == 0) return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj) return;
	obj->SetLayer(layer);
}

SLIME_EXPORT int __cdecl Visual_GetLayer(EntityId id)
{
	if (!ObjectManager::IsCreated() || id == 0) return 0;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj) return 0;
	return obj->GetLayer();
}

SLIME_EXPORT void __cdecl Visual_SetAnchor(EntityId id, float ax, float ay)
{
	if (!ObjectManager::IsCreated() || id == 0) return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj) return;
	obj->SetAnchor(glm::vec2(ax, ay));
}

SLIME_EXPORT void __cdecl Visual_GetAnchor(EntityId id, float* outAx, float* outAy)
{
	if (!ObjectManager::IsCreated() || id == 0 || !outAx || !outAy) return;
	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj) return;
	auto a = obj->GetAnchor();
	*outAx = a.x;
	*outAy = a.y;
}

SLIME_EXPORT bool __cdecl Input_GetKeyDown(int key)
{
	return Input::GetInstance()->GetKeyPress(Keycode(key));
}

SLIME_EXPORT bool __cdecl Input_GetKeyReleased(int key)
{
	return Input::GetInstance()->GetKeyRelease(Keycode(key));
}

SLIME_EXPORT void __cdecl Input_GetMousePos(float* outX, float* outY)
{
	if (!outX || !outY) return;
	auto p = Input::GetMousePos();
	*outX = (float)p.x;
	*outY = (float)p.y;
}

SLIME_EXPORT void __cdecl Input_GetMouseDelta(float* outX, float* outY)
{
	if (!outX || !outY) return;
	auto p = Input::GetInstance()->GetDeltaMouse();
	*outX = (float)p.x;
	*outY = (float)p.y;
}

SLIME_EXPORT bool __cdecl Input_GetMouseDown(int button)
{
	return Input::GetMouseDown(button);
}

SLIME_EXPORT void __cdecl Input_GetMouseToWorldPos(float* outX, float* outY)
{
	if (!outX || !outY) return;
	auto p = Input::GetMouseToWorldPos();
	*outX = (float)p.x;
	*outY = (float)p.y;
}

SLIME_EXPORT void __cdecl Input_GetWindowSize(float* outW, float* outH)
{
	if (!outW || !outH) return;
	auto s = Input::GetInstance()->GetWindowSize();
	*outW = (float)s.x;
	*outH = (float)s.y;
}

SLIME_EXPORT void __cdecl Input_GetAspectRatio(float* outX, float* outY)
{
	if (!outX || !outY) return;
	auto s = Input::GetInstance()->GetAspectRatio();
	*outX = (float)s.x;
	*outY = (float)s.y;
}

SLIME_EXPORT void __cdecl Input_SetViewportRect(int x, int y, int width, int height)
{
	Input::GetInstance()->SetViewportRect(x, y, width, height);
}

SLIME_EXPORT void __cdecl Input_GetViewportRect(int* outX, int* outY, int* outW, int* outH)
{
	if (!outX || !outY || !outW || !outH) return;
	auto v = Input::GetInstance()->GetViewportRect();
	*outX = (int)v.x;
	*outY = (int)v.y;
	*outW = (int)v.z;
	*outH = (int)v.w;
}

SLIME_EXPORT void __cdecl Input_SetScroll(float newScroll)
{
	Input::SetScroll(newScroll);
}

SLIME_EXPORT float __cdecl Input_GetScroll()
{
	return Input::GetScroll();
}

SLIME_EXPORT bool __cdecl Input_GetFocus()
{
	return Input::GetInstance()->GetFocus();
}

SLIME_EXPORT void __cdecl Input_SetFocus(bool focus)
{
	Input::GetInstance()->SetFocus(focus);
}

SLIME_EXPORT unsigned int __cdecl Text_CreateTextureFromFontFile(const char* fontPath, const char* text, int pixelHeight, int* outWidth, int* outHeight)
{
	if (!text) return 0;
	std::string path;
	if (fontPath)
		path = std::string(fontPath);
	else
	{
		// try default resource
		path = ResourceManager::GetInstance().GetResourcePath("Fonts\\Chilanka-Regular.ttf");
		if (path.empty())
			path = "..\\Fonts\\Chilanka-Regular.ttf";
	}

	int w = 0, h = 0;
	unsigned int tex = Text::CreateTextureFromString(path, std::string(text), pixelHeight, w, h);
	if (outWidth) *outWidth = w;
	if (outHeight) *outHeight = h;
	return tex;
}

SLIME_EXPORT void* __cdecl Font_LoadFromFile(const char* path)
{
	if (!path) return nullptr;
	Text::FontHandle* fh = Text::LoadFontFromFile(std::string(path));
	return reinterpret_cast<void*>(fh);
}

SLIME_EXPORT void __cdecl Font_Free(void* font)
{
	if (!font) return;
	Text::FreeFont(reinterpret_cast<Text::FontHandle*>(font));
}

SLIME_EXPORT unsigned int __cdecl Text_RenderToEntity(void* font, EntityId id, const char* text, int pixelHeight)
{
	if (!font || !text || id == 0 || pixelHeight <= 0) return 0;

	Text::FontHandle* fh = reinterpret_cast<Text::FontHandle*>(font);
	int w=0,h=0;
	unsigned int tex = Text::CreateTextureFromLoadedFont(fh, std::string(text), pixelHeight, w, h);
	if (tex == 0) return 0;

	// attach texture to the entity
	if (!ObjectManager::IsCreated()) return tex;
	GameObject* obj = ObjectManager::Get().Get((ObjectId)id);
	if (!obj) return tex;

	unsigned int tu = tex;
	Texture* t = new Texture(&tu);
	if (w > 0) obj->SetTextureWidth(w);
	obj->SetHasAnimation(false);
	obj->SetSpriteWidth(w);
	obj->SetTexture(t);

	return tex;
}

SLIME_EXPORT void __cdecl Entity_SetTexture(EntityId id, unsigned int texId, int width, int height)
{
	if (!ObjectManager::IsCreated() || id == 0 || texId == 0)
		return;

	GameObject* obj = ObjectManager::Get().Get((ObjectId) id);
	if (!obj) return;

	unsigned int tu = texId;
	Texture* t = new Texture(&tu);
	if (width > 0)
		obj->SetTextureWidth(width);
	obj->SetTexture(t);
}
