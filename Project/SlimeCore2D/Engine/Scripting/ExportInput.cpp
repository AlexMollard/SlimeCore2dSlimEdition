#include "Scripting/ExportInput.h"

#include "Core/Input.h"

SLIME_EXPORT bool __cdecl Input_GetKeyDown(int key)
{
	return Input::GetInstance()->GetKey(Keycode(key));
}

SLIME_EXPORT bool __cdecl Input_GetKeyReleased(int key)
{
	return Input::GetInstance()->GetKeyUp(Keycode(key));
}

/// <summary>
/// Is the mouse button held down?
/// </summary>
SLIME_EXPORT bool __cdecl Input_IsMouseButtonDown(int button)
{
	return Input::GetInstance()->GetMouseButton(button);
}

/// <summary>
/// Has the mouse button been pressed this frame? (Does not detect held)
/// </summary>
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
