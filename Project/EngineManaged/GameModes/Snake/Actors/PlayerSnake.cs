using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.GameModes.Snake.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SlimeCore.GameModes.Snake.Actors;

public record PlayerSnake : Actor<SnakeActors, SnakeGame>, IControllable
{
    public static readonly Vec3 COL_SNAKE = new(0.00f, 1.00f, 0.50f);
    public static readonly Vec3 COL_SNAKE_SPRINT = new(0.30f, 0.80f, 1.00f);
    private const int START_FORWARD_CLEAR = 3;

    public override SnakeActors Kind => SnakeActors.Snake;

    public required float HeadSize { get; set; }
    
    public float SpeedBoostTimer { get; set; }

    /// <summary>
    /// Contains all body segment positions, head is at index 0
    /// </summary>
    public List<Vec2i> Body { get; set; } = new();
    /// <summary>
    /// Is the snake dead?
    /// </summary>
    public bool IsDead { get; set; }
    /// <summary>
    /// Is the snake currently sprinting?
    /// </summary>
    public bool IsSprinting { get; set; }
    /// <summary>
    /// Current movement direction
    /// </summary>
    public Vec2i Direction { get; set; } = new(1, 0);
    /// <summary>
    /// Next movement direction
    /// </summary>
    public Vec2i NextDirection { get; set; } = new(1, 0);
    /// <summary>
    /// Previous movement direction
    /// </summary>
    public Vec2i PreviousDirection { get; set; } = new(1, 0);
    /// <summary>
    /// How many segments to grow this update
    /// </summary>
    public int Grow { get; set; }


    public Entity[] Eyes { get; set; } = new Entity[2];
    public Entity? Compass { get; set; }
    public Entity? Head { get; set; }

    [SetsRequiredMembers]
    public PlayerSnake(float headSize)
    {
       HeadSize = headSize;
    }

    public void Initialize()
    {
        Head = SceneFactory.CreateQuad(0, 0, HeadSize, HeadSize, COL_SNAKE.X, COL_SNAKE.Y, COL_SNAKE.Z, layer: 5);
        var headTransform = Head.GetComponent<TransformComponent>();
        headTransform.Anchor = (0.5f, 0.5f);
        var headSprite = Head.GetComponent<SpriteComponent>();
        headSprite.IsVisible = true;

        Eyes[0] = SceneFactory.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, layer: 10);
        var eye0Transform = Eyes[0].GetComponent<TransformComponent>();
        eye0Transform.Anchor = (0.5f, 0.5f);
        var eye0Sprite = Eyes[0].GetComponent<SpriteComponent>();
        eye0Sprite.IsVisible = true;

        Eyes[1] = SceneFactory.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, layer: 10);
        var eye1Transform = Eyes[1].GetComponent<TransformComponent>();
        eye1Transform.Anchor = (0.5f, 0.5f);
        var eye1Sprite = Eyes[1].GetComponent<SpriteComponent>();
        eye1Sprite.IsVisible = true;

        Compass = SceneFactory.CreateQuad(0, 0, 0.1f, 0.1f, 1f, 1f, 0f, layer: 20);
        var compassTransform = Compass.GetComponent<TransformComponent>();
        compassTransform.Anchor = (0.5f, 0.5f);
        var compassSprite = Compass.GetComponent<SpriteComponent>();
        compassSprite.IsVisible = true;
    }

    public override void Destroy()
    {
        Head.Destroy();
        Compass.Destroy();
        Eyes[0].Destroy();
        Eyes[1].Destroy();
    }

    protected override float ActionInterval { get; }

    public void RecieveInput(bool IgnoreInput)
    {
        if (IgnoreInput) return;

        IsSprinting = Input.GetKeyDown(Keycode.LEFT_SHIFT);
        if (IsSprinting)
        {
            return;
        }
        if ((Input.GetKeyDown(Keycode.W) || Input.GetKeyDown(Keycode.UP)) && Direction.Y == 0) NextDirection = new Vec2i(0, 1);
        if ((Input.GetKeyDown(Keycode.S) || Input.GetKeyDown(Keycode.DOWN)) && Direction.Y == 0) NextDirection = new Vec2i(0, -1);
        if ((Input.GetKeyDown(Keycode.A) || Input.GetKeyDown(Keycode.LEFT)) && Direction.X == 0) NextDirection = new Vec2i(-1, 0);
        if ((Input.GetKeyDown(Keycode.D) || Input.GetKeyDown(Keycode.RIGHT)) && Direction.X == 0) NextDirection = new Vec2i(1, 0);
    }

    public Vec2i this[int x]
    {
        get => Body[x];
        set => Body[x] = value;
    }
    public void Add(Vec2i position) => Body.Add(position);
    public void Insert(int index, Vec2i position) => Body.Insert(index, position);
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Body.Count)
        {
            return;
        }
        Body.RemoveAt(index);
    }
    public void Clear() => Body.Clear();

    public int GetBodyIndexFromWorldPosition(int x, int y)
    {
        for (var i = 0; i < Body.Count; i++)
        {
            if (Body[i].X == x && Body[i].Y == y)
            {
                return i;
            }
        }
        return -1;
    }

    public void Kill(SnakeGame game)
    {
        var nextRaw = Body[0] + Direction;
        var next = new Vec2i(SnakeGame.Wrap(nextRaw.X, game._world.Width()), SnakeGame.Wrap(nextRaw.Y, game._world.Height()));
        IsDead = true;
        game._shake = 0.4f;
        game.SpawnExplosion(next, 50, new Vec3(1.0f, 0.2f, 0.2f));
    }

    public override bool TakeAction(SnakeGame mode, float deltaTime)
    {
        //Snake is not implemented as an Actor in this context... yet, could be useful for less reactive behaviors.
        throw new NotImplementedException();
    }

    public void Reset(SnakeGame game)
    {
        game._snake.IsSprinting = false;
        SpeedBoostTimer = 0f;

        var center = new Vec2i(game._world.Width() / 2, game._world.Height() / 2);
        var (start, dir) = FindSafeStart(game, center, 60, START_FORWARD_CLEAR);

        Clear();
        Add(start);
        Add(new Vec2i(SnakeGame.Wrap(start.X - dir.X, game._world.Width()), SnakeGame.Wrap(start.Y - dir.Y, game._world.Height())));
        Direction = dir;
        NextDirection = Direction;
        Grow = 4;
        IsDead = false;
        game._cam = start.ToVec2();
    }

    private (Vec2i head, Vec2i dir) FindSafeStart(SnakeGame game, Vec2i center, int maxRadius = 60, int requiredForwardClear = 3)
    {
        var dirs = new[] { new Vec2i(1, 0), new Vec2i(0, 1), new Vec2i(-1, 0), new Vec2i(0, -1) };
        for (var r = 0; r <= maxRadius; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;

                    var x = SnakeGame.Wrap(center.X + dx, game._world.Width());
                    var y = SnakeGame.Wrap(center.Y + dy, game._world.Height());

                    if (game._world[x, y].Blocked) continue;

                    foreach (var d in dirs)
                    {
                        var tx = SnakeGame.Wrap(x - d.X, game._world.Width());
                        var ty = SnakeGame.Wrap(y - d.Y, game._world.Height());
                        if (game._world[tx, ty].Blocked) continue;

                        var ok = true;
                        for (var i = 1; i <= requiredForwardClear; i++)
                        {
                            var checkPos = new Vec2i(x, y) + (d * i);
                            if (game._world[SnakeGame.Wrap(checkPos.X, game._world.Width()), SnakeGame.Wrap(checkPos.Y, game._world.Height())].Blocked)
                            {
                                ok = false; break;
                            }
                        }
                        if (ok) return (new Vec2i(x, y), d);
                    }
                }
            }
        }
        return (center, new Vec2i(1, 0));
    }
}
