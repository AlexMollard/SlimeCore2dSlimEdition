using System;
using System.Runtime.InteropServices;

internal static partial class NativeMethods
{
    // -----------------------------
    // Entity lifecycle (Object-level)
    // -----------------------------
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong Entity_Create();

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_Destroy(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_IsAlive(ulong id);


    // -----------------------------
    // Entity transform & visual API (single, consistent surface)
    // -----------------------------
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetPosition(ulong id, float x, float y);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_GetPosition(ulong id, out float x, out float y);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetSize(ulong id, float w, float h);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_GetSize(ulong id, out float w, out float h);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetColor(ulong id, float r, float g, float b);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_GetColor(ulong id, out float r, out float g, out float b);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetAlpha(ulong id, float a);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern float Entity_GetAlpha(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetLayer(ulong id, int layer);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Entity_GetLayer(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetAnchor(ulong id, float ax, float ay);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_GetAnchor(ulong id, out float ax, out float ay);

    // -----------------------------
    // Entity visual helpers (texture / visibility / animation)
    // -----------------------------
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetTexture(ulong id, uint texId, int width, int height);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetRender(ulong id, bool value);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_GetRender(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetFrame(ulong id, int frame);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Entity_GetFrame(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AdvanceFrame(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetSpriteWidth(ulong id, int width);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Entity_GetSpriteWidth(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetHasAnimation(ulong id, bool value);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetFrameRate(ulong id, float frameRate);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern float Entity_GetFrameRate(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern IntPtr Texture_Load(string path);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Entity_SetTexturePtr(ulong id, IntPtr texPtr);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Entity_GetTexturePtr(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    public static extern float Entity_GetRotation(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Entity_SetRotation(ulong id, float rotation);

    // -----------------------------
    // Component Management
    // -----------------------------
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_Transform(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_Transform(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_Transform(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_Sprite(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_Sprite(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_Sprite(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_Animation(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_Animation(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_Animation(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_Tag(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_Tag(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_Tag(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_Relationship(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_Relationship(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_Relationship(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_RigidBody(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_RigidBody(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_RigidBody(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_BoxCollider(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_BoxCollider(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_BoxCollider(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_CircleCollider(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_CircleCollider(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_CircleCollider(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_Camera(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_Camera(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_Camera(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_AddComponent_AudioSource(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_HasComponent_AudioSource(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_RemoveComponent_AudioSource(ulong id);

    // -----------------------------
    // Physics Accessors
    // -----------------------------
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetVelocity(ulong id, float x, float y);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_GetVelocity(ulong id, out float x, out float y);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetMass(ulong id, float mass);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern float Entity_GetMass(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetKinematic(ulong id, bool value);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_GetKinematic(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetFixedRotation(ulong id, bool value);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_GetFixedRotation(ulong id);

    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetColliderSize(ulong id, float w, float h);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_GetColliderSize(ulong id, out float w, out float h);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetColliderOffset(ulong id, float x, float y);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_GetColliderOffset(ulong id, out float x, out float y);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetTrigger(ulong id, bool value);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_GetTrigger(ulong id);

    // -----------------------------
    // Camera Accessors
    // -----------------------------
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetCameraSize(ulong id, float size);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern float Entity_GetCameraSize(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetCameraZoom(ulong id, float zoom);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern float Entity_GetCameraZoom(ulong id);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Entity_SetPrimaryCamera(ulong id, bool value);
    [DllImport("SlimeCore2D.exe", CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Entity_GetPrimaryCamera(ulong id);
}