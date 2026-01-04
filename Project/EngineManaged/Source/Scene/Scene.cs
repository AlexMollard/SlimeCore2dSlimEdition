namespace EngineManaged.Scene;

public static class Scene
{
    // ---------------------------------------------------------------------
    // Lifecycle Management
    // ---------------------------------------------------------------------

    /// <summary>
    /// Destroys the entity in the native engine.
    /// </summary>
    public static void Destroy(Entity e)
    {
        if (e.Id != 0)
        {
            NativeMethods.Scene_Destroy(e.Id);
        }
    }

    /// <summary>
    /// Checks if the Entity ID is still valid in the native engine.
    /// </summary>
    public static bool IsAlive(Entity e)
    {
        return e.Id != 0 && NativeMethods.Scene_IsAlive(e.Id);
    }

    // ---------------------------------------------------------------------
    // Iteration & Queries
    // ---------------------------------------------------------------------

    /// <summary>
    /// Gets the total count of native objects currently active.
    /// </summary>
    public static int Count => NativeMethods.Scene_GetEntityCount();

    /// <summary>
    /// Gets an entity wrapper for the object at the specific native index.
    /// </summary>
    public static Entity GetAt(int index)
    {
        ulong id = NativeMethods.Scene_GetEntityIdAtIndex(index);
        return new Entity(id);
    }

    /// <summary>
    /// Iterates over all active entities in the native scene.
    /// </summary>
    public static EntityEnumerator Enumerate()
    {
        return new EntityEnumerator();
    }

    public struct EntityEnumerator
    {
        private int _index;
        private readonly int _count;

        public EntityEnumerator()
        {
            _index = -1;
            _count = NativeMethods.Scene_GetEntityCount();
        }

        public Entity Current => Scene.GetAt(_index);

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        public EntityEnumerator GetEnumerator() => this;
    }
}