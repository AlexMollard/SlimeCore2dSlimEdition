using System;

namespace EngineManaged.Scene;

public interface IComponent
{
    ulong EntityId { get; set; }
}

public record TransformComponent : IComponent
{
    public ulong EntityId { get; set; }

    public (float x, float y) Position
    {
        get { NativeMethods.Entity_GetPosition(EntityId, out float x, out float y); return (x, y); }
        set => NativeMethods.Entity_SetPosition(EntityId, value.x, value.y);
    }

    public (float w, float h) Scale
    {
        get { NativeMethods.Entity_GetSize(EntityId, out float w, out float h); return (w, h); }
        set => NativeMethods.Entity_SetSize(EntityId, value.w, value.h);
    }

    public float Rotation
    {
        get => NativeMethods.Entity_GetRotation(EntityId);
        set => NativeMethods.Entity_SetRotation(EntityId, value);
    }

    public (float x, float y) Anchor
    {
        get { NativeMethods.Entity_GetAnchor(EntityId, out float x, out float y); return (x, y); }
        set => NativeMethods.Entity_SetAnchor(EntityId, value.x, value.y);
    }

    public int Layer
    {
        get => NativeMethods.Entity_GetLayer(EntityId);
        set => NativeMethods.Entity_SetLayer(EntityId, value);
    }
}

public record SpriteComponent : IComponent
{
    public ulong EntityId { get; set; }

    public (float r, float g, float b) Color
    {
        get { NativeMethods.Entity_GetColor(EntityId, out float r, out float g, out float b); return (r, g, b); }
        set => NativeMethods.Entity_SetColor(EntityId, value.r, value.g, value.b);
    }

    public float Alpha
    {
        get => NativeMethods.Entity_GetAlpha(EntityId);
        set => NativeMethods.Entity_SetAlpha(EntityId, value);
    }

    public bool IsVisible
    {
        get => NativeMethods.Entity_GetRender(EntityId);
        set => NativeMethods.Entity_SetRender(EntityId, value);
    }

    public void SetTexture(uint texId, int width, int height) => NativeMethods.Entity_SetTexture(EntityId, texId, width, height);
    public IntPtr TexturePtr
    {
        get => NativeMethods.Entity_GetTexturePtr(EntityId);
        set => NativeMethods.Entity_SetTexturePtr(EntityId, value);
    }
}

public record AnimationComponent : IComponent
{
    public ulong EntityId { get; set; }

    public int Frame
    {
        get => NativeMethods.Entity_GetFrame(EntityId);
        set => NativeMethods.Entity_SetFrame(EntityId, value);
    }

    public float FrameRate
    {
        get => NativeMethods.Entity_GetFrameRate(EntityId);
        set => NativeMethods.Entity_SetFrameRate(EntityId, value);
    }

    public int SpriteWidth
    {
        get => NativeMethods.Entity_GetSpriteWidth(EntityId);
        set => NativeMethods.Entity_SetSpriteWidth(EntityId, value);
    }

    public void HasAnimation(bool val) => NativeMethods.Entity_SetHasAnimation(EntityId, val);
}

public record RigidBodyComponent : IComponent
{
    public ulong EntityId { get; set; }

    public (float x, float y) Velocity
    {
        get { NativeMethods.Entity_GetVelocity(EntityId, out float x, out float y); return (x, y); }
        set => NativeMethods.Entity_SetVelocity(EntityId, value.x, value.y);
    }

    public float Mass
    {
        get => NativeMethods.Entity_GetMass(EntityId);
        set => NativeMethods.Entity_SetMass(EntityId, value);
    }

    public bool IsKinematic
    {
        get => NativeMethods.Entity_GetKinematic(EntityId);
        set => NativeMethods.Entity_SetKinematic(EntityId, value);
    }

    public bool FixedRotation
    {
        get => NativeMethods.Entity_GetFixedRotation(EntityId);
        set => NativeMethods.Entity_SetFixedRotation(EntityId, value);
    }
}

public record BoxColliderComponent : IComponent
{
    public ulong EntityId { get; set; }

    public (float w, float h) Size
    {
        get { NativeMethods.Entity_GetColliderSize(EntityId, out float w, out float h); return (w, h); }
        set => NativeMethods.Entity_SetColliderSize(EntityId, value.w, value.h);
    }

    public (float x, float y) Offset
    {
        get { NativeMethods.Entity_GetColliderOffset(EntityId, out float x, out float y); return (x, y); }
        set => NativeMethods.Entity_SetColliderOffset(EntityId, value.x, value.y);
    }

    public bool IsTrigger
    {
        get => NativeMethods.Entity_GetTrigger(EntityId);
        set => NativeMethods.Entity_SetTrigger(EntityId, value);
    }
}

public record CameraComponent : IComponent
{
    public ulong EntityId { get; set; }

    public float Size
    {
        get => NativeMethods.Entity_GetCameraSize(EntityId);
        set => NativeMethods.Entity_SetCameraSize(EntityId, value);
    }

    public float Zoom
    {
        get => NativeMethods.Entity_GetCameraZoom(EntityId);
        set => NativeMethods.Entity_SetCameraZoom(EntityId, value);
    }

    public bool IsPrimary
    {
        get => NativeMethods.Entity_GetPrimaryCamera(EntityId);
        set => NativeMethods.Entity_SetPrimaryCamera(EntityId, value);
    }
}

public record AudioSourceComponent : IComponent
{
    public ulong EntityId { get; set; }
}

public record TagComponent : IComponent
{
    public ulong EntityId { get; set; }
}

public record RelationshipComponent : IComponent
{
    public ulong EntityId { get; set; }
}

public record CircleColliderComponent : IComponent
{
    public ulong EntityId { get; set; }
}
