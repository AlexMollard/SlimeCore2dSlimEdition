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
}
