public readonly struct Entity
{
	public readonly ulong Id;
	public Entity(ulong id) => Id = id;

	public bool IsAlive => Id != 0 && Native.Entity_IsAlive(Id);

	public void SetPosition(float x, float y) => Native.Transform_SetPosition(Id, x, y);

	public (float x, float y) GetPosition()
	{
		Native.Transform_GetPosition(Id, out var x, out var y);
		return (x, y);
	}

	/// <summary>Create a new quad entity (wraps native create).</summary>
	public static Entity CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
		=> new Entity(Native.Entity_CreateQuad(px, py, sx, sy, r, g, b));

	public void Destroy() { if (Id != 0) Native.Entity_Destroy(Id); }

	public void SetSize(float w, float h) => Native.Transform_SetSize(Id, w, h);
	public (float w, float h) GetSize() { Native.Transform_GetSize(Id, out var w, out var h); return (w, h); }

	public void SetColor(float r, float g, float b) => Native.Visual_SetColor(Id, r, g, b);

	public void SetLayer(int layer) => Native.Visual_SetLayer(Id, layer);
	public int GetLayer() => Native.Visual_GetLayer(Id);

	public void SetAnchor(float ax, float ay) => Native.Visual_SetAnchor(Id, ax, ay);
	public (float x, float y) GetAnchor() { Native.Visual_GetAnchor(Id, out var x, out var y); return (x, y); }

	public void SetTexture(uint texId, int width, int height) => Native.Entity_SetTexture(Id, texId, width, height);
}
