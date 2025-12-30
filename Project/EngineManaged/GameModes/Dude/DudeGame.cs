using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Dude.States;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;

namespace GameModes.Dude;

// --- SHARED DATA STRUCTURES ---
internal enum HaterType { Normal, Chonker }
internal class Hater
{
    public Entity? Ent;
    public Vec2 Pos;
    public HaterType Type;
}
internal class Collectable
{
    public Entity? Ent;
    public Vec2 Pos;
    public PowerupDef? Definition;
}
internal class GhostTrail
{
    public Entity? Ent;
    public float Alpha;
    public float InitW;
    public float InitH;
}

// Refactored to use Vec2 for position and velocity
internal class XPGem
{
    public Entity? Ent;
    public Vec2 Pos;
    public int Value;
}

// --- GAME CONTEXT ---
public class DudeGame : GameMode<DudeGame>, IGameMode, IDisposable
{
    private bool _isDisposed;

    // Shared Data
    internal Entity? Dude;
    internal Vec2 DudePos;
    internal Vec2 DudeVel;
    internal Entity? Bg;
    internal Entity? Camera;
    internal Entity? DarkOverlay;
    internal Entity? CardBgBackdrop;

    // --- STATS CONTAINER ---
    internal DudeStats Stats = new();

    // --- EVENT SYSTEM ---
    internal GameEvents Events { get; set; } = new();

    // Core Values
    internal int Level;
    internal float XP;
    internal float XPToNextLevel;
    internal float Score;
    internal float TimeAlive;

    // Track upgrade counts for UI
    internal Dictionary<string, int> UpgradeCounts = new();

    // Timers
    internal float DashTimer;
    internal float ShieldTimer;
    internal float ChillTimer;
    internal float SpawnTimer;
    internal float DiscoTimer;
    internal float ShakeAmount;
    internal float TrailTimer;

    // Lists
    internal List<Hater> Haters { get; set; } = new();
    internal List<Collectable> Collectables = new();
    internal List<XPGem> Gems = new();
    internal List<GhostTrail> Trails = new();
    internal List<Entity> Boundaries = new();
    internal ParticleSystem? ParticleSys;

    // Resources
    internal IntPtr TexPlayer;
    internal IntPtr TexEnemy { get; set; }
    internal IntPtr TexBg;
    internal IntPtr TexParticle;

    // UI
    internal UIText ScoreText;
    internal UIText LevelText;
    internal UIImage XPBarFill;
    internal UIImage XPBarBg;

    public override Random? Rng { get; set; } = new();

    // Helper to create entities using the new ECS system
    internal Entity CreateSpriteEntity(float x, float y, float w, float h, float r, float g, float b, int layer, IntPtr texture = default)
    {
        var e = Entity.Create();
        e.AddComponent<TransformComponent>();
        e.AddComponent<SpriteComponent>();
        e.AddComponent<AnimationComponent>();

        var transform = e.GetComponent<TransformComponent>();
        transform.Position = (x, y);
        transform.Scale = (w, h);
        transform.Layer = layer;

        var sprite = e.GetComponent<SpriteComponent>();
        sprite.Color = (r, g, b);
        if (texture != IntPtr.Zero)
        {
            sprite.TexturePtr = texture;
        }

        return e;
    }

    public override void Init()
    {
        // 1. Initialize Content Registry
        ContentRegistry.Init();

        // Set Gravity to Zero for Top-Down Game
        Native.Scene_SetGravity(0, 0);

        // Load Textures (Using debug.png as placeholder for now)
        TexPlayer = NativeMethods.Resources_LoadTexture("Player", "Game/Resources/Textures/debug.png");
        TexEnemy = NativeMethods.Resources_LoadTexture("Enemy", "Game/Resources/Textures/debug.png");
        TexBg = NativeMethods.Resources_LoadTexture("Bg", "Game/Resources/Textures/debug.png");
        TexParticle = NativeMethods.Resources_LoadTexture("Particle", "Game/Resources/Textures/debug.png");

        // 2. Reset Data
        Level = 1;
        XP = 0;
        XPToNextLevel = 100;
        Score = 0;
        TimeAlive = 0;
        DudePos = Vec2.Zero;
        DudeVel = Vec2.Zero;

        Stats.Reset();
        Events.Clear();
        UpgradeCounts.Clear();

        ParticleSys = new ParticleSystem(10000);

        // 3. Create Entities
        Camera = Entity.Create();
        Camera.AddComponent<TransformComponent>();
        Camera.AddComponent<CameraComponent>();
        var cam = Camera.GetComponent<CameraComponent>();
        cam.IsPrimary = true;
        cam.Zoom = 1.0f;
        cam.Size = 20.0f; // Zoom out to see the whole arena

        Bg = CreateSpriteEntity(0, 0, 100, 100, 0.05f, 0.05f, 0.1f, -10, TexBg);
        var bgTransform = Bg.GetComponent<TransformComponent>();
        bgTransform.Anchor = (0.5f, 0.5f);

        DarkOverlay = CreateSpriteEntity(0, 0, 100, 100, 0f, 0f, 0f, 90);
        var darkTransform = DarkOverlay.GetComponent<TransformComponent>();
        darkTransform.Anchor = (0.5f, 0.5f);
        var darkSprite = DarkOverlay.GetComponent<SpriteComponent>();
        darkSprite.IsVisible = false;

        CardBgBackdrop = CreateSpriteEntity(0, 0, 100, 100, 0.1f, 0.1f, 0.1f, 91);
        var cardTransform = CardBgBackdrop.GetComponent<TransformComponent>();
        cardTransform.Anchor = (0.5f, 0.5f);
        var cardSprite = CardBgBackdrop.GetComponent<SpriteComponent>();
        cardSprite.IsVisible = false;

        // Create Dude with full ECS components
        Dude = Entity.Create();
        Dude.AddComponent<TransformComponent>();
        Dude.AddComponent<SpriteComponent>();
        Dude.AddComponent<AnimationComponent>();
        Dude.AddComponent<RigidBodyComponent>();
        Dude.AddComponent<BoxColliderComponent>();

        var dudeTransform = Dude.GetComponent<TransformComponent>();
        dudeTransform.Position = (0, 0);
        dudeTransform.Scale = (0.9f, 0.9f);
        dudeTransform.Layer = 20;
        dudeTransform.Anchor = (0.5f, 0.5f);

        var dudeSprite = Dude.GetComponent<SpriteComponent>();
        dudeSprite.Color = (0.2f, 1.0f, 0.2f);
        dudeSprite.TexturePtr = TexPlayer;

        // Init Physics Props
        var dudeRb = Dude.GetComponent<RigidBodyComponent>();
        dudeRb.Mass = 1.0f;
        dudeRb.IsKinematic = false;
        dudeRb.FixedRotation = true;

        var dudeCollider = Dude.GetComponent<BoxColliderComponent>();
        dudeCollider.Size = (0.9f, 0.9f);

        // Create Walls (Physics Boundaries)
        CreateBoundary(0, 10, 30, 1);  // Top
        CreateBoundary(0, -10, 30, 1); // Bottom
        CreateBoundary(-15, 0, 1, 20); // Left
        CreateBoundary(15, 0, 1, 20);  // Right

        // XP Bar (UI System)
        XPBarBg = UIImage.Create(0, -8.0f, 28.0f, 0.6f);
        XPBarBg.Color(0.2f, 0.2f, 0.2f);
        XPBarBg.Anchor(0.5f, 0.5f);
        XPBarBg.Layer(-1); // Behind text

        XPBarFill = UIImage.Create(0.0f, -8.0f, 0f, 0.6f);
        XPBarFill.Color(0.0f, 0.8f, 1.0f);
        XPBarFill.Anchor(0.0f, 0.5f);
        XPBarFill.Position = (-14.0f, -8.0f); // Start from left edge of the bar (Center 0, Width 28 -> Left -14)
        XPBarFill.Layer(0);

        // UI
        ScoreText = UIText.Create("0", 1, -13.5f, 7.5f);
        ScoreText.Anchor(0.0f, 1.0f); // Top-left anchor
        LevelText = UIText.Create("LVL 1", 1, -13.0f, -6.25f);
        LevelText.Anchor(0.0f, 0.0f); // Bottom-left anchor

        // 4. Start Gameplay
        ChangeState(new StatePlaying());
    }

    private void CreateBoundary(float x, float y, float w, float h)
    {
        var wall = Entity.Create();
        wall.AddComponent<TransformComponent>();
        wall.AddComponent<BoxColliderComponent>();
        wall.AddComponent<RigidBodyComponent>();
        wall.AddComponent<SpriteComponent>();

        var t = wall.GetComponent<TransformComponent>();
        t.Position = (x, y);
        t.Scale = (w, h);

        var s = wall.GetComponent<SpriteComponent>();
        s.Color = (0.3f, 0.3f, 0.4f); // Dark grey-blue walls

        var bc = wall.GetComponent<BoxColliderComponent>();
        bc.Size = (w, h);

        var rb = wall.GetComponent<RigidBodyComponent>();
        rb.Mass = 9999999.0f;
        rb.IsKinematic = true;

        Boundaries.Add(wall);
    }

    public override void Update(float dt)
    {
        if (ParticleSys != null) ParticleSys.OnUpdate(dt);
        if (_currentState != null) _currentState.Update(this, dt);
    }

    public override void Shutdown()
    {
        if (_currentState != null) _currentState.Exit(this);

        Bg?.Destroy();
        Camera?.Destroy();
        DarkOverlay?.Destroy();
        CardBgBackdrop?.Destroy();
        Dude?.Destroy();
        XPBarBg.Destroy();
        XPBarFill.Destroy();
        ScoreText.Destroy();
        LevelText.Destroy();

        foreach (var h in Haters) h.Ent?.Destroy(); Haters.Clear();
        foreach (var c in Collectables) c.Ent?.Destroy(); Collectables.Clear();
        foreach (var g in Gems) g.Ent?.Destroy(); Gems.Clear();
        foreach (var t in Trails) t.Ent?.Destroy(); Trails.Clear();
        foreach (var b in Boundaries) b.Destroy(); Boundaries.Clear();

        if (ParticleSys != null)
        {
            ParticleSys.Dispose();
            ParticleSys = null;
        }

        Events.Clear();
        UpgradeCounts.Clear();
    }

    internal void SpawnExplosion(Vec2 pos, int count, float r, float g, float b)
    {
        if (ParticleSys == null || Rng == null) return;

        var props = new ParticleProps();
        props.Position = pos;
        props.VelocityVariation = new Vec2(8.0f, 8.0f); // Speed variation handled by random direction + speed
        props.ColorBegin = new Color(r, g, b, 1.0f);
        props.ColorEnd = new Color(r, g, b, 0.0f);
        props.SizeBegin = 0.5f;
        props.SizeEnd = 0.0f;
        props.SizeVariation = 0.3f;
        props.LifeTime = 1.0f;

        for (int i = 0; i < count; i++)
        {
            float angle = (float)Rng.NextDouble() * 6.28f;
            float speed = (float)Rng.NextDouble() * 8.0f + 2.0f;
            props.Velocity = new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            ParticleSys.Emit(props);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            ParticleSys?.Dispose();
        }

        _isDisposed = true;
    }

    ~DudeGame()
    {
        Dispose(false);
    }

    // --- STAT ACCESSORS ---
    internal float StatMagnetRange { get => Stats.MagnetRange; set => Stats.MagnetRange = value; }
    internal float StatSpeedMult { get => Stats.SpeedMult; set => Stats.SpeedMult = value; }
    internal float StatDashCooldown { get => Stats.DashCooldown; set => Stats.DashCooldown = value; }
    internal float StatShieldDuration { get => Stats.ShieldDuration; set => Stats.ShieldDuration = value; }
    internal float StatPickupBonus { get => Stats.PickupBonus; set => Stats.PickupBonus = value; }
    internal float StatAccelMult { get => Stats.AccelMult; set => Stats.AccelMult = value; }
    internal float StatChillDuration { get => Stats.ChillDuration; set => Stats.ChillDuration = value; }
    internal float StatLuck { get => Stats.Luck; set => Stats.Luck = value; }
    internal float StatSize { get => Stats.PlayerSize; set => Stats.PlayerSize = value; }
}