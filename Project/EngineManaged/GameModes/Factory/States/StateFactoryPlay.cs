using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryPlay : IGameState<FactoryGame>, IDisposable
{
    
    private Vec2 _cam;
    private Player? _player;
    private float _time;
    private ulong _cameraEntity;
    private ulong _cursorEntity;
    private ulong _cursorDirectionEntity;
    private int _lastGridX = -1;
    private int _lastGridY = -1;
    private bool _wasMouseDown;
    private int _currentTier = 1;
    
    private FactoryStructure _selectedStructure = FactoryStructure.ConveyorBelt;
    private Direction _placementDirection = Direction.North;
    private bool _deleteMode;
    private bool _inputBlockedByUI;

    // UI Elements
    private UIButton? _btnConveyor;
    private UIButton? _btnMiner;
    private UIButton? _btnStorage;
    private UIButton? _btnDelete;
    private UIImage? _toolbarBg;
    private UIText? _tierLabel;
    private UIText? _rotationLabel;
    private UIText? _tooltipLabel;

    public void Enter(FactoryGame game)
    {
        // Prevent accidental clicks when transitioning from Menu
        if (Input.IsMouseDown(Input.MouseButton.Left))
        {
            _inputBlockedByUI = true;
            _wasMouseDown = true;
        }

        FactoryResources.Load();
        game.World?.Initialize(FactoryGame.MAX_VIEW_W, FactoryGame.MAX_VIEW_H);

        // Create Camera
        _cameraEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_cameraEntity);
        Native.Entity_AddComponent_Camera(_cameraEntity);
        Native.Entity_SetPrimaryCamera(_cameraEntity, true);
        Native.Entity_SetCameraSize(_cameraEntity, FactoryGame.VIEW_H);

        // Create Cursor
        _cursorEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_cursorEntity);
        Native.Entity_AddComponent_Sprite(_cursorEntity);
        Native.Entity_SetColor(_cursorEntity, 1.0f, 1.0f, 0.0f); // Yellow highlight
        Native.Entity_SetAlpha(_cursorEntity, 0.4f);
        Native.Entity_SetSize(_cursorEntity, 1.0f, 1.0f);
        Native.Entity_SetLayer(_cursorEntity, 100); // Ensure it's on top

        // Create Cursor Direction Indicator
        _cursorDirectionEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_cursorDirectionEntity);
        Native.Entity_AddComponent_Sprite(_cursorDirectionEntity);
        Native.Entity_SetColor(_cursorDirectionEntity, 1.0f, 1.0f, 0.0f); // Yellow
        Native.Entity_SetSize(_cursorDirectionEntity, 0.2f, 0.2f);
        Native.Entity_SetLayer(_cursorDirectionEntity, 101); // Above cursor

        // Generate World
        var gen = new FactoryWorldGenerator(game.Rng?.Next() ?? 0);
        if (game.World != null)
        {

            //Generate terrain
            gen.Generate(game.World);

            // Create TileMap
            game.TileMap = Native.TileMap_Create(game.World.Width(), game.World.Height(), 1.0f);

            //Create Systems
            game.ConveyorSystem = new ConveyorSystem(game.World.Width(), game.World.Height(), 1.0f);
            game.BuildingSystem = new BuildingSystem(game.World, game.ConveyorSystem);

            // Populate TileMap
            //for (var x = 0; x < game.World.Width(); x++)
            //{
            //    for (var y = 0; y < game.World.Height(); y++)
            //    {
            //        game.World[x, y].UpdateTile(game);
            //    }
            //}
            //Native.TileMap_UpdateMesh(game.TileMap);
            game.World.ManualTick(game, 0f);
        }

        // Center camera on world
        if (game.World != null)
        {
            _cam = new Vec2(game.World.Width() / 2.0f, game.World.Height() / 2.0f);
        }

        // Create player at center
        _player = new Player(_cam);
        game.ActorManager?.Register(_player);
        Sheep.Populate(game, 500);
        Wolf.Populate(game, 100);
        Tree.Populate(game, 600);

        CreateUI();
    }

    private void CreateUI()
    {
        UISystem.Clear();

        // Toolbar Background
        float barW = 36.0f;
        float barH = 4.0f;
        float barY = -13.0f;
        
        var bg = UIImage.Create(0, barY, barW, barH);
        bg.Color(0.1f, 0.1f, 0.1f);
        bg.Anchor(0.5f, 0.5f);
        bg.Layer(50);
        _toolbarBg = bg;

        // Buttons
        float btnW = 8.0f;
        float btnH = 3.0f;
        float gap = 0.5f;
        float startX = -((btnW * 4 + gap * 3) / 2.0f) + btnW / 2.0f; // Centered
        
        // Helper to create styled button
        UIButton CreateBtn(string text, int index, Action onClick)
        {
            float x = startX + index * (btnW + gap);
            var btn = UIButton.Create(text, x, barY, btnW, btnH, 0.3f, 0.3f, 0.3f, 51, 1, false); // Use World Space
            btn.Label.Color(0.9f, 0.9f, 0.9f);
            btn.Clicked += onClick;
            return btn;
        }

        _btnConveyor = CreateBtn("Conveyor", 0, () => { _selectedStructure = FactoryStructure.ConveyorBelt; _deleteMode = false; });
        _btnMiner = CreateBtn("Miner", 1, () => { _selectedStructure = FactoryStructure.Miner; _deleteMode = false; });
        _btnStorage = CreateBtn("Storage", 2, () => { _selectedStructure = FactoryStructure.Storage; _deleteMode = false; });
        _btnDelete = CreateBtn("Delete", 3, () => { _deleteMode = true; });

        // Labels
        var tLabel = UIText.Create("Tier: 1", 1, -17.0f, barY + 2.5f); // Left side of bar
        tLabel.Anchor(0.0f, 0.5f);
        tLabel.Layer(52);
        tLabel.Color(1.0f, 0.8f, 0.2f);
        _tierLabel = tLabel;

        var rLabel = UIText.Create("Rotate: [R]", 1, 17.0f, barY + 2.5f); // Right side of bar
        rLabel.Anchor(1.0f, 0.5f);
        rLabel.Layer(52);
        rLabel.Color(0.8f, 0.8f, 0.8f);
        _rotationLabel = rLabel;
        // Tooltip
        var tt = UIText.Create("", 1, 0, 0);
        tt.Layer(200);
        tt.Color(1.0f, 1.0f, 1.0f);
        tt.IsVisible(false);
        tt.UseScreenSpace(false);
        _tooltipLabel = tt;    }

    private void UpdateUI()
    {
        // Update Button Highlights
        void SetBtnState(UIButton? btn, bool active)
        {
            if (btn == null) return;
            if (active) btn.SetBaseColor(0.5f, 0.5f, 0.6f);
            else btn.SetBaseColor(0.3f, 0.3f, 0.3f);
        }

        SetBtnState(_btnConveyor, !_deleteMode && _selectedStructure == FactoryStructure.ConveyorBelt);
        SetBtnState(_btnMiner, !_deleteMode && _selectedStructure == FactoryStructure.Miner);
        SetBtnState(_btnStorage, !_deleteMode && _selectedStructure == FactoryStructure.Storage);
        SetBtnState(_btnDelete, _deleteMode);

        // Update Tier Label
        if (_tierLabel is { } tierLbl)
        {
            tierLbl.Text($"Tier: {_currentTier} [1-3]");
        }
        
        // Update Rotation Label
        if (_rotationLabel is { } rotLbl)
        {
            string dirStr = _placementDirection.ToString();
             rotLbl.Text($"Rotate: [R] ({dirStr})");
        }
    }

    public void Exit(FactoryGame game)
    {
        Dispose();
        
        // Destroy UI Elements
        _btnConveyor?.Destroy();
        _btnMiner?.Destroy();
        _btnStorage?.Destroy();
        _btnDelete?.Destroy();
        if (_toolbarBg is { } bg) bg.Destroy();
        if (_tierLabel is { } tl) tl.Destroy();
        if (_rotationLabel is { } rl) rl.Destroy();
        if (_tooltipLabel is { } tt) tt.Destroy();

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

        _time += dt;
        game.ActorManager?.Tick(game, dt);
        
        UISystem.Update();
        UpdateUI();
        
        // Update Systems
        game.BuildingSystem?.Update(dt);
        game.ConveyorSystem?.Update(dt, game.BuildingSystem);
        
        // Input for Tier Selection
        if (Input.GetKeyReleased(Keycode.KEY_1)) _currentTier = 1;
        if (Input.GetKeyReleased(Keycode.KEY_2)) _currentTier = 2;
        if (Input.GetKeyReleased(Keycode.KEY_3)) _currentTier = 3;

        // Rotation
        if (Input.GetKeyReleased(Keycode.R))
        {
            _placementDirection = (Direction)(((int)_placementDirection + 1) % 4);
        }
        
        // Camera follows player
        if (_player != null)
        {
            _cam = Vec2.Lerp(_cam, _player.Position, dt * 5.0f);
        }

        // Update Camera Entity
        if (_cameraEntity != 0)
        {
            Native.Entity_SetPosition(_cameraEntity, _cam.X, _cam.Y);
            Native.Entity_SetCameraZoom(_cameraEntity, 1.0f / game.World.Zoom);
        }

        HandleMouse(game);
        game.World?.Tick(game, dt);
    }

    public void Draw(FactoryGame game)
    {
        Render(game);
    }

    private void HandleMouse(FactoryGame game)
    {
        if (game.World == null) return;

        // Zoom
        float scroll = Input.GetScroll();
        if (scroll != 0)
        {
            game.World.Zoom += scroll * 0.05f;
            game.World.Zoom = Math.Clamp(game.World.Zoom, 0.05f, 5.0f);
        }

        var (mx, my) = Input.GetMouseToWorld();
        int gridX = (int)Math.Floor(mx);
        int gridY = (int)Math.Floor(my);

        // Tooltip Logic
        if (_tooltipLabel is { } tt)
        {
            bool showTooltip = false;
            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                var tile = game.World[gridX, gridY];
                if (tile.Structure == FactoryStructure.Storage && game.BuildingSystem != null)
                {
                    var (count, item) = game.BuildingSystem.GetBuildingInventory(gridX, gridY);
                    tt.Text($"{item}: {count}");
                    // Convert world mouse pos to UI pos (relative to camera center)
                    // Since UI camera is at (0,0), we just subtract the main camera position
                    tt.Position = (mx - _cam.X, my - _cam.Y);
                    showTooltip = true;
                }
            }
            tt.IsVisible(showTooltip);
        }

        // Update Cursor Position
        if (_cursorEntity != 0)
        {
            Native.Entity_SetPosition(_cursorEntity, gridX + 0.5f, gridY + 0.5f);

            // Hide cursor if out of bounds
            bool inBounds = gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height();
            Native.Entity_SetRender(_cursorEntity, inBounds);

            // Ghost Visuals
            if (inBounds)
            {
                if (_deleteMode)
                {
                    Native.Entity_SetColor(_cursorEntity, 1.0f, 0.0f, 0.0f); // Red for delete
                    Native.Entity_SetAlpha(_cursorEntity, 0.5f);
                    Native.Entity_SetTexturePtr(_cursorEntity, IntPtr.Zero); // No texture, just color overlay
                }
                else
                {
                    // Check validity
                    bool isValid = true;
                    var tile = game.World[gridX, gridY];
                    if (_selectedStructure == FactoryStructure.Miner && tile.OreType == FactoryOre.None)
                    {
                        isValid = false;
                    }
                    else if (_selectedStructure == FactoryStructure.ConveyorBelt && 
                            (tile.Structure == FactoryStructure.Miner || tile.Structure == FactoryStructure.Storage))
                    {
                        isValid = false;
                    }

                    // Set texture based on selected structure
                    nint tex = FactoryResources.GetStructureTexture(_selectedStructure, _currentTier);
                    
                    // Reset size
                    Native.Entity_SetSize(_cursorEntity, 1.0f, 1.0f);

                    if (_selectedStructure == FactoryStructure.ConveyorBelt)
                    {
                        // Procedural Conveyor Ghost (Default)
                        Native.Entity_SetTexturePtr(_cursorEntity, IntPtr.Zero);
                        Native.Entity_SetSize(_cursorEntity, 0.4f, 0.9f); // Vertical strip to indicate direction
                        
                        if (isValid)
                            Native.Entity_SetColor(_cursorEntity, 0.2f, 0.2f, 0.2f); // Dark Grey
                        else
                            Native.Entity_SetColor(_cursorEntity, 1.0f, 0.0f, 0.0f); // Red invalid
                    }
                    else if (tex != IntPtr.Zero)
                    {
                        Native.Entity_SetTexturePtr(_cursorEntity, tex);
                        if (isValid)
                            Native.Entity_SetColor(_cursorEntity, 1.0f, 1.0f, 1.0f); // Reset color
                        else
                            Native.Entity_SetColor(_cursorEntity, 1.0f, 0.0f, 0.0f); // Red invalid
                    }
                    else
                    {
                        // Fallback for structures without texture (Miner/Storage)
                        Native.Entity_SetTexturePtr(_cursorEntity, IntPtr.Zero);
                        
                        if (!isValid)
                        {
                             Native.Entity_SetColor(_cursorEntity, 1.0f, 0.0f, 0.0f);
                        }
                        else if (_selectedStructure == FactoryStructure.Miner)
                            Native.Entity_SetColor(_cursorEntity, 0.6f, 0.2f, 0.6f);
                        else if (_selectedStructure == FactoryStructure.Storage)
                            Native.Entity_SetColor(_cursorEntity, 0.6f, 0.4f, 0.2f);
                        else
                            Native.Entity_SetColor(_cursorEntity, 1.0f, 1.0f, 0.0f); // Default yellow
                    }

                    // Rotation
                    float rot = 0.0f;
                    switch (_placementDirection)
                    {
                        case Direction.East: rot = -90.0f; break;
                        case Direction.South: rot = 180.0f; break;
                        case Direction.West: rot = 90.0f; break;
                    }
                    Native.Entity_SetRotation(_cursorEntity, rot);
                    Native.Entity_SetAlpha(_cursorEntity, 0.6f); // Ghost transparency
                }
            }

            // Update Direction Indicator
            if (_cursorDirectionEntity != 0)
            {
                bool showArrow = inBounds && _selectedStructure == FactoryStructure.ConveyorBelt && !_deleteMode;
                Native.Entity_SetRender(_cursorDirectionEntity, showArrow);
                
                if (showArrow)
                {
                    float offset = 0.35f;
                    float dx = 0, dy = 0;
                    switch (_placementDirection)
                    {
                        case Direction.North: dy = offset; break;
                        case Direction.East: dx = offset; break;
                        case Direction.South: dy = -offset; break;
                        case Direction.West: dx = -offset; break;
                    }
                    
                    Native.Entity_SetPosition(_cursorDirectionEntity, gridX + 0.5f + dx, gridY + 0.5f + dy);
                }
            }
        }

        bool isMouseDown = Input.IsMouseDown(Input.MouseButton.Left);

        // Handle UI Blocking Logic
        if (isMouseDown && !_wasMouseDown) // Just pressed
        {
            _inputBlockedByUI = UISystem.IsMouseOverUI;
        }
        else if (!isMouseDown) // Released
        {
            _inputBlockedByUI = false;
        }
        
        // Don't place tiles if mouse is over UI or if the click started on UI
        if (isMouseDown && !UISystem.IsMouseOverUI && !_inputBlockedByUI)
        {
            if (!_wasMouseDown)
            {
                // Mouse just pressed
                _lastGridX = -1; 
                _lastGridY = -1;
            }

            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                var tile = game.World[gridX, gridY];
                
                if (_deleteMode)
                {
                    if (tile.Structure != FactoryStructure.None)
                    {
                        game.World.Set(gridX, gridY, o => o.Structure = FactoryStructure.None);
                        game.World.UpdateNeighbors(game, new Vec2i(gridX, gridY));
                    }
                }
                else
                {
                    // Check placement rules
                    if (_selectedStructure == FactoryStructure.Miner && tile.OreType == FactoryOre.None)
                    {
                        // Invalid placement for Miner - must be on Ore
                        return;
                    }

                    if (_selectedStructure == FactoryStructure.ConveyorBelt && 
                        (tile.Structure == FactoryStructure.Miner || tile.Structure == FactoryStructure.Storage))
                    {
                        // Don't overwrite buildings with belts, but update drag position
                        if (gridX != _lastGridX || gridY != _lastGridY)
                        {
                            _lastGridX = gridX;
                            _lastGridY = gridY;
                        }
                        return;
                    }

                    // Determine direction based on drag
                    var newDir = tile.Direction;
                    
                    // If we are dragging, we want to continue the line
                    // But we also want to respect the user's chosen rotation if they just clicked
                    // Logic:
                    // 1. If single click (not dragging), use _placementDirection
                    // 2. If dragging (moved to new tile), infer direction from movement
                    
                    if (!_wasMouseDown) // Just clicked
                    {
                        newDir = _placementDirection;
                    }
                    else if (_lastGridX != -1 && (gridX != _lastGridX || gridY != _lastGridY))
                    {
                        // Dragging logic
                        int dx = gridX - _lastGridX;
                        int dy = gridY - _lastGridY;
                        
                        if (Math.Abs(dx) > Math.Abs(dy))
                        {
                            if (dx > 0) newDir = Direction.East;
                            else newDir = Direction.West;
                        }
                        else
                        {
                            if (dy > 0) newDir = Direction.North;
                            else newDir = Direction.South;
                        }
                        
                        _placementDirection = newDir; // Update global state to match drag
                        
                        // Also update the PREVIOUS tile to point to this one if it was part of the drag
                        // This makes corners work naturally
                        if (game.World[_lastGridX, _lastGridY].Structure == FactoryStructure.ConveyorBelt)
                        {
                            game.World.Set(_lastGridX, _lastGridY, o => o.Direction = newDir);
                        }
                    }
                    else
                    {
                        // Holding mouse on same tile? Keep current direction
                        newDir = _placementDirection;
                    }

                    // Allow overwriting or updating direction
                    game.World.Set(gridX, gridY, o =>
                    {
                        o.Structure = _selectedStructure;
                        o.Direction = newDir;
                        o.Tier = _currentTier;
                    });
                    
                    game.World.UpdateNeighbors(game, new Vec2i(gridX, gridY));
                    // Update last grid pos if we moved
                    if (gridX != _lastGridX || gridY != _lastGridY)
                    {
                        _lastGridX = gridX;
                        _lastGridY = gridY;
                    }
                }
            }
        }
        
        _wasMouseDown = isMouseDown;

        // Right click to remove (Legacy, now we have Delete button, but keep it for convenience)
        if (Input.IsMouseDown(Input.MouseButton.Right))
        {
            if (gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                var tile = game.World[gridX, gridY];
                if (tile.Structure != FactoryStructure.None)
                {
                    game.World.Set(gridX, gridY, o =>
                    {
                        o.Structure = FactoryStructure.None;
                    });
                    
                    game.World.UpdateNeighbors(game, new Vec2i(gridX, gridY));
                }
            }
        }
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
        if (_cameraEntity != 0)
        {
            Native.Entity_Destroy(_cameraEntity);
            _cameraEntity = 0;
        }

        if (_cursorEntity != 0)
        {
            Native.Entity_Destroy(_cursorEntity);
            _cursorEntity = 0;
        }

        if (_cursorDirectionEntity != 0)
        {
            Native.Entity_Destroy(_cursorDirectionEntity);
            _cursorDirectionEntity = 0;
        }
    }

    ~StateFactoryPlay()
    {
        Dispose(false);
    }
}
