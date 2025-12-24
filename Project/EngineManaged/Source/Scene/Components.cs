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
        get { Native.Entity_GetPosition(EntityId, out var x, out var y); return (x, y); }
        set => Native.Entity_SetPosition(EntityId, value.x, value.y);
    }
    
    public (float w, float h) Scale
    {
        get { Native.Entity_GetSize(EntityId, out var w, out var h); return (w, h); }
        set => Native.Entity_SetSize(EntityId, value.w, value.h);
    }
    
    public float Rotation
    {
        get => Native.Entity_GetRotation(EntityId);
        set => Native.Entity_SetRotation(EntityId, value);
    }
    
    public (float x, float y) Anchor
    {
        get { Native.Entity_GetAnchor(EntityId, out var x, out var y); return (x, y); }
        set => Native.Entity_SetAnchor(EntityId, value.x, value.y);
    }
    
    public int Layer
    {
        get => Native.Entity_GetLayer(EntityId);
        set => Native.Entity_SetLayer(EntityId, value);
    }
}

public record SpriteComponent : IComponent
{
    public ulong EntityId { get; set; }

    public (float r, float g, float b) Color
    {
        get { Native.Entity_GetColor(EntityId, out var r, out var g, out var b); return (r, g, b); }
        set => Native.Entity_SetColor(EntityId, value.r, value.g, value.b);
    }

    public float Alpha
    {
        get => Native.Entity_GetAlpha(EntityId);
        set => Native.Entity_SetAlpha(EntityId, value);
    }
    
    public bool IsVisible
    {
        get => Native.Entity_GetRender(EntityId);
        set => Native.Entity_SetRender(EntityId, value);
    }
    
    public void SetTexture(uint texId, int width, int height) => Native.Entity_SetTexture(EntityId, texId, width, height);
    public IntPtr TexturePtr
    {
        get => Native.Entity_GetTexturePtr(EntityId);
        set => Native.Entity_SetTexturePtr(EntityId, value);
    }
}

public record AnimationComponent : IComponent
{
    public ulong EntityId { get; set; }
    
    public int Frame
    {
        get => Native.Entity_GetFrame(EntityId);
        set => Native.Entity_SetFrame(EntityId, value);
    }
    
    public float FrameRate
    {
        get => Native.Entity_GetFrameRate(EntityId);
        set => Native.Entity_SetFrameRate(EntityId, value);
    }
    
    public int SpriteWidth
    {
        get => Native.Entity_GetSpriteWidth(EntityId);
        set => Native.Entity_SetSpriteWidth(EntityId, value);
    }
    
    public bool HasAnimation
    {
        set => Native.Entity_SetHasAnimation(EntityId, value);
    }
}

public record RigidBodyComponent : IComponent
{
    public ulong EntityId { get; set; }
    
    public (float x, float y) Velocity
    {
        get { Native.Entity_GetVelocity(EntityId, out var x, out var y); return (x, y); }
        set => Native.Entity_SetVelocity(EntityId, value.x, value.y);
    }
    
    public float Mass
    {
        get => Native.Entity_GetMass(EntityId);
        set => Native.Entity_SetMass(EntityId, value);
    }
    
    public bool IsKinematic
    {
        get => Native.Entity_GetKinematic(EntityId);
        set => Native.Entity_SetKinematic(EntityId, value);
    }

    public bool FixedRotation
    {
        get => Native.Entity_GetFixedRotation(EntityId);
        set => Native.Entity_SetFixedRotation(EntityId, value);
    }
}

public record BoxColliderComponent : IComponent
{
    public ulong EntityId { get; set; }
    
    public (float w, float h) Size
    {
        get { Native.Entity_GetColliderSize(EntityId, out var w, out var h); return (w, h); }
        set => Native.Entity_SetColliderSize(EntityId, value.w, value.h);
    }
    
    public (float x, float y) Offset
    {
        get { Native.Entity_GetColliderOffset(EntityId, out var x, out var y); return (x, y); }
        set => Native.Entity_SetColliderOffset(EntityId, value.x, value.y);
    }
    
    public bool IsTrigger
    {
        get => Native.Entity_GetTrigger(EntityId);
        set => Native.Entity_SetTrigger(EntityId, value);
    }
}

public record CameraComponent : IComponent
{
    public ulong EntityId { get; set; }
    
    public float Size
    {
        get => Native.Entity_GetCameraSize(EntityId);
        set => Native.Entity_SetCameraSize(EntityId, value);
    }
    
    public float Zoom
    {
        get => Native.Entity_GetCameraZoom(EntityId);
        set => Native.Entity_SetCameraZoom(EntityId, value);
    }
    
    public bool IsPrimary
    {
        get => Native.Entity_GetPrimaryCamera(EntityId);
        set => Native.Entity_SetPrimaryCamera(EntityId, value);
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
