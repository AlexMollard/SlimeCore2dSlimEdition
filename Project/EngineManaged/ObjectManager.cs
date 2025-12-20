using System.Collections.Generic;

internal static class ObjectManager
{
	public static Entity CreateQuad(float px, float py, float sx, float sy, float r = 1f, float g = 1f, float b = 1f)
	{
		ulong id = Native.ObjectManager_CreateQuad(px, py, sx, sy, r, g, b);
		return new Entity(id);
	}

	public static Entity CreateQuadWithTexture(float px, float py, float sx, float sy, uint texId)
	{
		ulong id = Native.ObjectManager_CreateQuadWithTexture(px, py, sx, sy, texId);
		return new Entity(id);
	}

	public static Entity CreateGameObject(float px, float py, float sx, float sy, float r, float g, float b)
	{
		ulong id = Native.ObjectManager_CreateGameObject(px, py, sx, sy, r, g, b);
		return new Entity(id);
	}

	public static void Destroy(Entity e) { if (e.Id != 0) Native.ObjectManager_Destroy(e.Id); }
	public static bool IsAlive(Entity e) => e.Id != 0 && Native.ObjectManager_IsAlive(e.Id);

	public static int Size() => Native.ObjectManager_GetSize();
	public static Entity GetAt(int index) { ulong id = Native.ObjectManager_GetIdAtIndex(index); return new Entity(id); }

	public static IEnumerable<Entity> Enumerate()
	{
		int s = Size();
		for (int i = 0; i < s; i++) yield return GetAt(i);
	}
}