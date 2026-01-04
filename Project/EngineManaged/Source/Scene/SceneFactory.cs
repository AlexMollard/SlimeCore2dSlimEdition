namespace EngineManaged.Scene; // Adjust namespace as needed

/// <summary>
/// The specific place to spawn new objects into the world.
/// This replaces the creation methods inside ObjectManager and Entity.
/// </summary>
public static class SceneFactory
{
    /// <summary>
    /// Creates a simple colored rectangle.
    /// </summary>
    public static Entity CreateQuad(float x, float y, float width, float height, float r = 1f, float g = 1f, float b = 1f, int layer = 0)
    {
        // We use the Scene_ native call to ensure the engine tracks this entity in its list
        ulong id = NativeMethods.Scene_CreateQuad(x, y, width, height, r, g, b);

        var entity = new Entity(id);
        if (layer != 0)
        {
            var transform = entity.GetComponent<TransformComponent>();
            transform.Layer = layer;
        }

        return entity;
    }

    /// <summary>
    /// Creates a textured entity (Sprite). 
    /// Default anchor is centered (0.5, 0.5).
    /// </summary>
    public static Entity CreateSprite(float x, float y, float width, float height, uint textureId, int layer = 0, float anchorX = 0.5f, float anchorY = 0.5f)
    {
        ulong id = NativeMethods.Scene_CreateQuadWithTexture(x, y, width, height, textureId);

        var entity = new Entity(id);
        var transform = entity.GetComponent<TransformComponent>();
        transform.Layer = layer;
        transform.Anchor = (anchorX, anchorY);

        return entity;
    }

    /// <summary>
    /// Creates a generic game object (useful for invisible logic controllers or parents).
    /// </summary>
    public static Entity CreateGameObject(float x, float y, float r = 1f, float g = 1f, float b = 1f)
    {
        // Assuming standard size 1x1 or 0x0 for generic objects, customizable later
        ulong id = NativeMethods.Scene_CreateGameObject(x, y, 1f, 1f, r, g, b);
        return new Entity(id);
    }
}