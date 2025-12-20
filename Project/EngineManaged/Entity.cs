public readonly struct Entity
{
	public readonly ulong Id;
	public Entity(ulong id) => Id = id;

	public bool IsAlive => Id != 0 && Native.Entity_IsAlive(Id);

	public void SetPosition(float x, float y) => Native.Entity_SetPosition(Id, x, y);

	public (float x, float y) GetPosition()
	{
		Native.Entity_GetPosition(Id, out var x, out var y);
		return (x, y);
	}

	/// <summary>Create a new quad entity (wraps native create).</summary>
	public static Entity CreateQuad(float px, float py, float sx, float sy, float r, float g, float b)
		=> new Entity(Native.Entity_CreateQuad(px, py, sx, sy, r, g, b));

	public static Entity CreateQuad(float px, float py, float sx, float sy)
		=> CreateQuad(px, py, sx, sy, 1f, 1f, 1f);

	public static Entity CreateQuad(float px, float py, float sx, float sy, int layer)
	{
		var e = CreateQuad(px, py, sx, sy);
		e.SetLayer(layer);
		return e;
	}

	public static Entity CreateQuad(float px, float py, float sx, float sy, float r, float g, float b, float anchorX, float anchorY, int layer = 0)
	{
		var e = CreateQuad(px, py, sx, sy, r, g, b);
		e.SetAnchor(anchorX, anchorY);
		e.SetLayer(layer);
		return e;
	}

	public static Entity CreateQuadWithTexture(float px, float py, float sx, float sy, uint texId, int layer = 0, float anchorX = 0.5f, float anchorY = 0.5f)
	{
		var e = new Entity(Native.ObjectManager_CreateQuadWithTexture(px, py, sx, sy, texId));
		e.SetLayer(layer);
		e.SetAnchor(anchorX, anchorY);
		return e;
	}

	public void Destroy() { if (Id != 0) Native.Entity_Destroy(Id); }

	public void SetSize(float w, float h) => Native.Entity_SetSize(Id, w, h);
	public (float w, float h) GetSize() { Native.Entity_GetSize(Id, out var w, out var h); return (w, h); }

	public void SetColor(float r, float g, float b) => Native.Entity_SetColor(Id, r, g, b);

	public void SetLayer(int layer) => Native.Entity_SetLayer(Id, layer);
	public int GetLayer() => Native.Entity_GetLayer(Id);

	public void SetAnchor(float ax, float ay) => Native.Entity_SetAnchor(Id, ax, ay);
	public (float x, float y) GetAnchor() { Native.Entity_GetAnchor(Id, out var x, out var y); return (x, y); }

	public void SetTexture(uint texId, int width, int height) => Native.Entity_SetTexture(Id, texId, width, height);

	public void SetVisible(bool value) => Native.Entity_SetRender(Id, value);
	public bool GetVisible() => Native.Entity_GetRender(Id);

	public void SetFrame(int frame) => Native.Entity_SetFrame(Id, frame);
	public int GetFrame() => Native.Entity_GetFrame(Id);
	public void AdvanceFrame() => Native.Entity_AdvanceFrame(Id);

	public void SetSpriteWidth(int width) => Native.Entity_SetSpriteWidth(Id, width);
	public int GetSpriteWidth() => Native.Entity_GetSpriteWidth(Id);

	public void SetHasAnimation(bool value) => Native.Entity_SetHasAnimation(Id, value);
	public void SetFrameRate(float fr) => Native.Entity_SetFrameRate(Id, fr);
	public float GetFrameRate() => Native.Entity_GetFrameRate(Id);
}
