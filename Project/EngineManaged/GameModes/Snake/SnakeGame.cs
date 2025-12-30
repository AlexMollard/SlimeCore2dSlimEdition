using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.UI;
using SlimeCore.GameModes.Snake.Actors;
using SlimeCore.GameModes.Snake.States;
using SlimeCore.GameModes.Snake.World;
using SlimeCore.Source.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SlimeCore.GameModes.Snake;

public sealed class SnakeGame : GameMode<SnakeGame>, IGameMode, IDisposable
{
    private bool _isDisposed;
    public override Random? Rng { get; set; }

    public const int VIEW_W = 100;
    public const int VIEW_H = 75;

    public SnakeGrid? _world { get; set; }

    public static float _cellSpacing = 0.4f;

    // Logic constants
    public const float TICK_NORMAL = 0.12f;
    public const float TICK_SPRINT = 0.05f;

    // Game Logic

    public float _shake;

    // Camera & Smoothing
    public Vec2 _cam;


    public PlayerSnake? _snake { get; set; }
    private ParticleSystem? _particleSys;
    public SnakeActorManager? ActorManager { get; set; }

    public int _currentScore { get; set; }

    public float SpawnTimer;
    public float ChillTimer;

    public SnakeGameEvents Events = new();
    public SnakeSettings Settings { get; set; }

    [SetsRequiredMembers]
    public SnakeGame(SnakeSettings settings)
    {
        Settings = settings;
        InitializeGame();
    }

    public void InitializeGame()
    {
        // Initialize or re-initialize game subsystems. These may be null until InitializeGame() has run.
        _snake = new(Settings.InitialZoom * 1.25f);
        _world = new(Settings.WorldWidth, Settings.WorldHeight, Settings.BaseTerrain, Settings.InitialZoom);
        _particleSys = new(5000);
        ActorManager = new SnakeActorManager(Settings.ActorSingleFrameBudget,
            NpcSnakeHunter.HandleUpdateBehaviour);
    }

    public IntPtr TexEnemy;

    public override void Init()
    {
        ChangeState(new StateSnakeMenu());
    }

    public override void Shutdown()
    {
        UISystem.Clear();

        _snake?.Destroy();
        _particleSys?.Dispose();
        Events.Clear();
        _world?.Destroy();
        ActorManager?.Destroy();
    }

    public override void Update(float dt)
    {
        _particleSys?.OnUpdate(dt);
        _shake = Math.Max(0, _shake - dt * 2.0f);
        if (_currentState != null) _currentState.Update(this, dt);
    }

    public void SpawnExplosion(Vec2 worldPos, int count, Vec3 color)
    {
        // Guard if world or particles not initialized
        if (_world == null || _particleSys == null) return;

        // Calculate relative position to camera
        var dx = worldPos.X - _cam.X;
        var dy = worldPos.Y - _cam.Y;

        // Handle wrapping
        if (dx > _world.Width() / 2f) dx -= _world.Width();
        else if (dx < -_world.Width() / 2f) dx += _world.Width();

        if (dy > _world.Height() / 2f) dy -= _world.Height();
        else if (dy < -_world.Height() / 2f) dy += _world.Height();

        var px = dx * _cellSpacing;
        var py = dy * _cellSpacing;

        var props = new ParticleProps();
        props.Position = new Vec2(px, py);
        props.VelocityVariation = new Vec2(2.0f, 2.0f);
        props.ColorBegin = new Color(color.X, color.Y, color.Z, 1.0f);
        props.ColorEnd = new Color(color.X, color.Y, color.Z, 0.0f);
        props.SizeBegin = 0.3f;
        props.SizeEnd = 0.0f;
        props.SizeVariation = 0.1f;
        props.LifeTime = 0.8f;

        var rng = Rng ?? new Random();

        for (var i = 0; i < count; i++)
        {
            var angle = (float)rng.NextDouble() * 6.28f;
            var speed = (float)rng.NextDouble() * 2.0f + 0.5f;
            props.Velocity = new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

            _particleSys.Emit(props);
        }
    }

    public bool IsSnakeAt(int x, int y) => _snake != null && _snake.GetBodyIndexFromWorldPosition(x, y) != -1;
    public static int Wrap(int v, int m) => (v % m + m) % m;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _particleSys?.Dispose();
        }

        _isDisposed = true;
    }

    ~SnakeGame()
    {
        Dispose(false);
    }
}