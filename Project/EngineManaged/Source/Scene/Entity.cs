using System;

namespace EngineManaged.Scene;

public record Entity
{
	public readonly ulong Id;

	public Entity(ulong id) => Id = id;

	// Valid check
	public bool IsAlive => Id != 0 && Native.Entity_IsAlive(Id);

	// ---------------------------------------------------------------------
	// C# Properties (Syntactic sugar over Native Setters/Getters)
	// ---------------------------------------------------------------------

	public bool IsVisible
	{
		get => Native.Entity_GetRender(Id);
		set => Native.Entity_SetRender(Id, value);
	}

	public int Layer
	{
		get => Native.Entity_GetLayer(Id);
		set => Native.Entity_SetLayer(Id, value);
	}

	public int Frame
	{
		get => Native.Entity_GetFrame(Id);
		set => Native.Entity_SetFrame(Id, value);
	}

	public float FrameRate
	{
		get => Native.Entity_GetFrameRate(Id);
		set => Native.Entity_SetFrameRate(Id, value);
	}

	public int SpriteWidth
	{
		get => Native.Entity_GetSpriteWidth(Id);
		set => Native.Entity_SetSpriteWidth(Id, value);
	}

	public float Rotation
	{
		get => Native.Entity_GetRotation(Id);
		set => Native.Entity_SetRotation(Id, value);
	}

	public IntPtr Texture
	{
		get => Native.Entity_GetTexturePtr(Id);
		set => Native.Entity_SetTexturePtr(Id, value);
	}

	// ---------------------------------------------------------------------
	// Transform Methods
	// ---------------------------------------------------------------------

	public void SetPosition(float x, float y) => Native.Entity_SetPosition(Id, x, y);

	public (float x, float y) GetPosition()
	{
		Native.Entity_GetPosition(Id, out var x, out var y);
		return (x, y);
	}

	public void SetSize(float w, float h) => Native.Entity_SetSize(Id, w, h);

	public (float w, float h) GetSize()
	{
		Native.Entity_GetSize(Id, out var w, out var h);
		return (w, h);
	}

	public void SetAnchor(float ax, float ay) => Native.Entity_SetAnchor(Id, ax, ay);

	public (float x, float y) GetAnchor()
	{
		Native.Entity_GetAnchor(Id, out var x, out var y);
		return (x, y);
	}

	// ---------------------------------------------------------------------
	// Visual Methods
	// ---------------------------------------------------------------------

	public void SetColor(float r, float g, float b) => Native.Entity_SetColor(Id, r, g, b);

	public void SetTexture(uint texId, int width, int height) => Native.Entity_SetTexture(Id, texId, width, height);

	public void SetHasAnimation(bool value) => Native.Entity_SetHasAnimation(Id, value);

	public void AdvanceFrame() => Native.Entity_AdvanceFrame(Id);

	// ---------------------------------------------------------------------
	// Lifecycle
	// ---------------------------------------------------------------------

	/// <summary>
	/// Marks the entity for destruction in the native engine.
	/// </summary>
	public void Destroy()
	{
		if (Id != 0) Native.ObjectManager_Destroy(Id);
	}
}