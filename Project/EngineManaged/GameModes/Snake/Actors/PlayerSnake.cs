using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.Core.World;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace SlimeCore.GameModes.Snake.Actors;

public record PlayerSnake : Actor<Terrain>, IControllable
{
    public static readonly Vec3 COL_SNAKE = new(0.00f, 1.00f, 0.50f);
    public static readonly Vec3 COL_SNAKE_SPRINT = new(0.30f, 0.80f, 1.00f);

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

    public void Initialize(float cell_size)
    {
        Head = SceneFactory.CreateQuad(0, 0, cell_size * 1.15f, cell_size * 1.15f, COL_SNAKE.X, COL_SNAKE.Y, COL_SNAKE.Z, layer: 5);
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

    public void Destroy()
    {
        Head.Destroy();
        Compass.Destroy();
        Eyes[0].Destroy();
        Eyes[1].Destroy();
    }

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
        var next = new Vec2i(SnakeGame.Wrap(nextRaw.X, SnakeGame.WORLD_W), SnakeGame.Wrap(nextRaw.Y, SnakeGame.WORLD_H));
        IsDead = true;
        game._shake = 0.4f;
        game.SpawnExplosion(next, 50, new Vec3(1.0f, 0.2f, 0.2f));
    }

    public void RenderSnake()
    {

    }

}
