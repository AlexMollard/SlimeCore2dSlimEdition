using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.Buildings;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.GameModes.Factory.UI;
using SlimeCore.GameModes.Factory.Systems;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryPlay : IGameState<FactoryGame>, IDisposable
{
    private Player? _player;
    private float _time;
    private ulong _cameraEntity;

    private FactoryGameUI _ui = new();
    private FactoryInteractionManager _interaction = new();

    public void Enter(FactoryGame game)
    {
        game.World?.Initialize(FactoryGame.MAX_VIEW_W, FactoryGame.MAX_VIEW_H);

        // Create Camera
        _cameraEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_cameraEntity);
        Native.Entity_AddComponent_Camera(_cameraEntity);
        Native.Entity_SetPrimaryCamera(_cameraEntity, true);
        Native.Entity_SetCameraSize(_cameraEntity, FactoryGame.VIEW_H);

        _interaction.Initialize();

        // Generate World
        var gen = new FactoryWorldGenerator(game.Rng?.Next() ?? 0);
        if (game.World != null)
        {

            //Generate terrain
            gen.Generate(game.World);
            game.World.CalculateAllBitmasks();

            // Create TileMap
            game.TileMap = Native.TileMap_Create(game.World.Width(), game.World.Height(), 1.0f);

            //Create Systems
            game.ConveyorSystem = new ConveyorSystem(game.World.Width(), game.World.Height(), 1.0f);
            game.BuildingSystem = new BuildingSystem(game, game.World, game.ConveyorSystem);

            // Flush full worldgen
            game.World.ManualTick(game, 0f);
        }

        // Center camera on world
        if (game.World != null)
        {
            game.Camera = new Vec2(game.World.Width() / 2.0f, game.World.Height() / 2.0f);
        }

        // Create player at center
        _player = new Player(game.Camera);
        game.ActorManager?.Register(_player);
        Sheep.Populate(game, 500);
        Wolf.Populate(game, 100);
        Tree.Populate(game, 600);
        var wolfPos = game.Camera;
        wolfPos.X += 5.0f;
        var sheepPos = game.Camera;
        sheepPos.X -= 5.0f;

        game.ActorManager?.Register(new Wolf(wolfPos));
        game.ActorManager?.Register(new Sheep(sheepPos));

        _ui.Initialize(_player);
    }

    public void Exit(FactoryGame game)
    {
        Dispose();

        UISystem.Clear();
        game.World?.Destroy();
        game.ActorManager?.Destroy();
        if (game.TileMap != IntPtr.Zero)
        {
            Native.TileMap_Destroy(game.TileMap);
            game.TileMap = IntPtr.Zero;
        }

        if (game.ConveyorSystem != null)
        {
            game.ConveyorSystem.Dispose();
            game.ConveyorSystem = null;
        }

        if (game.BuildingSystem != null)
        {
            game.BuildingSystem.Dispose();
            game.BuildingSystem = null;
        }
    }

    public void Update(FactoryGame game, float dt)
    {
        if (game.World == null) return;

        _ui.Update(dt);

        _time += dt;
        game.ActorManager?.Tick(game, dt);
        
        // Update Systems
        game.BuildingSystem?.Update(dt);
        game.ConveyorSystem?.Update(dt, game.BuildingSystem);
        
        // Input for Tier Selection
        if (Input.GetKeyReleased(Keycode.KEY_1)) { _ui.SetTier(1); }
        if (Input.GetKeyReleased(Keycode.KEY_2)) { _ui.SetTier(2); }
        if (Input.GetKeyReleased(Keycode.KEY_3)) { _ui.SetTier(3); }

        // Camera follows player
        if (_player != null)
        {
            game.Camera = Vec2.Lerp(game.Camera, _player.Position, dt * 5.0f);
        }

        // Update Camera Entity
        if (_cameraEntity != 0)
        {
            Native.Entity_SetPosition(_cameraEntity, game.Camera.X, game.Camera.Y);
            Native.Entity_SetCameraZoom(_cameraEntity, 1.0f / game.World.Zoom);
        }

        _interaction.Update(game, dt, _ui, _player, game.Camera);
        game.World?.Tick(game, dt);
    }

    public void Draw(FactoryGame game)
    {
        Render(game);
    }

    private void Render(FactoryGame game)
    {
        if (game.TileMap != IntPtr.Zero)
        {
            Native.TileMap_Render(game.TileMap);
        }
        
        if (game.ConveyorSystem != null)
        {
            game.ConveyorSystem.Render(_time);
        }

        // Update player visual position
        if (_player != null)
        {
            var pTransform = _player.Entity.GetComponent<TransformComponent>();
            pTransform.Position = (_player.Position.X, _player.Position.Y);
            pTransform.Scale = (_player.Size, _player.Size);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ui.Dispose();
            _interaction.Dispose();
        }

        if (_cameraEntity != 0)
        {
            Native.Entity_Destroy(_cameraEntity);
            _cameraEntity = 0;
        }
    }

    ~StateFactoryPlay()
    {
        Dispose(false);
    }
}
