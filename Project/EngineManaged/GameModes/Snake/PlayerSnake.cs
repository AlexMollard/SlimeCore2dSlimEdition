using EngineManaged.Numeric;
using EngineManaged.Scene;
using SlimeCore.Core.Grid;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SlimeCore.GameModes.Snake;

public record PlayerSnake
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
    public int Grow { get; set; } = 0;

    
    public Entity[] Eyes { get; set; } = new Entity[2];
    public Entity Compass { get; set; }
    public Entity Head { get; set; }

    public void Initialize(float cell_size)
    {
        Head = SceneFactory.CreateQuad(0, 0, cell_size * 1.15f, cell_size * 1.15f, COL_SNAKE.X, COL_SNAKE.Y, COL_SNAKE.Z, layer: 5);
        Head.GetComponent<TransformComponent>().Anchor = (0.5f, 0.5f);
        Head.GetComponent<SpriteComponent>().IsVisible = true;

        Eyes[0] = SceneFactory.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, layer: 10);
        Eyes[0].GetComponent<TransformComponent>().Anchor = (0.5f, 0.5f);
        Eyes[0].GetComponent<SpriteComponent>().IsVisible = true;

        Eyes[1] = SceneFactory.CreateQuad(0, 0, 0.08f, 0.08f, 0f, 0f, 0f, layer: 10);
        Eyes[1].GetComponent<TransformComponent>().Anchor = (0.5f, 0.5f);
        Eyes[1].GetComponent<SpriteComponent>().IsVisible = true;

        Compass = SceneFactory.CreateQuad(0, 0, 0.1f, 0.1f, 1f, 1f, 0f, layer: 20);
        Compass.GetComponent<TransformComponent>().Anchor = (0.5f, 0.5f);
        Compass.GetComponent<SpriteComponent>().IsVisible = true;
    }

    public void Destroy()
    {
        Head.Destroy();
        Compass.Destroy();
        Eyes[0].Destroy();
        Eyes[1].Destroy();
    }

    public Vec2i this[int x]
    {
        get => Body[x];
        set => Body[x] = value;
    }
    public void Add(Vec2i position) => Body.Add(position);
    public void Insert(int index, Vec2i position) => Body.Insert(index, position);
    public void RemoveAt(int index) => Body.RemoveAt(index);
    public void Clear() => Body.Clear();

    public int GetBodyIndexFromWorldPosition(int x, int y)
    {
        for (int i = 0; i < Body.Count; i++)
        {
            if (Body[i].X == x && Body[i].Y == y)
            {
                return i;
            }
        }
        return -1;
    }

    public void RenderSnake()
    {
        
    }

}
