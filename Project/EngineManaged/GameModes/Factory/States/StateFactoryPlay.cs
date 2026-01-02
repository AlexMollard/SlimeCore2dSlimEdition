using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;

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
    private bool _wasRightMouseDown;
    private Vec2 _rightClickStart;
    private bool _isRightClickDragging;
    private ulong _selectionBoxEntity;
    private int _currentTier = 1;
    private int _lastTier;

    private FactoryStructure _selectedStructure = FactoryStructure.ConveyorBelt;
    private Direction _placementDirection = Direction.North;
    private bool _deleteMode;
    private bool _inputBlockedByUI;
    private bool _zoomInHeld;
    private bool _zoomOutHeld;

    // Menu Animation
    private bool _menuOpen;
    private float _menuAnimT;
    private const float MenuAnimSpeed = 5.0f;

    // Inventory Animation
    private bool _inventoryOpen;
    private float _inventoryAnimT;
    private UIScrollPanel? _inventoryMenu;
    private UIButton? _btnInventoryToggle;

    // UI Elements
    private UIScrollPanel? _buildMenu;
    private UIButton? _btnBuildToggle;
    private UIButton? _btnConveyor;
    private UIButton? _btnMiner;
    private UIButton? _btnStorage;
    private UIButton? _btnFarm;
    private UIButton? _btnWall;
    private UIButton? _btnTier1;
    private UIButton? _btnTier2;
    private UIButton? _btnTier3;
    // private UIText? _rotationLabel; // Removed
    private UIText? _tooltipTitle;
    private UIText? _tooltipBody;
    private UIImage? _tooltipBg;
    
    private Dictionary<UIButton, ItemDefinition> _inventoryButtonItems = new();

    public void Enter(FactoryGame game)
    {
        // Prevent accidental clicks when transitioning from Menu
        if (Input.IsMouseDown(Input.MouseButton.Left))
        {
            _inputBlockedByUI = true;
            _wasMouseDown = true;
        }

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

        // Create Selection Box
        _selectionBoxEntity = Native.Entity_Create();
        Native.Entity_AddComponent_Transform(_selectionBoxEntity);
        Native.Entity_AddComponent_Sprite(_selectionBoxEntity);
        Native.Entity_SetColor(_selectionBoxEntity, 1.0f, 0.0f, 0.0f); // Red
        Native.Entity_SetAlpha(_selectionBoxEntity, 0.3f);
        Native.Entity_SetLayer(_selectionBoxEntity, 102); // Top
        Native.Entity_SetRender(_selectionBoxEntity, false);

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
            _cam = new Vec2(game.World.Width() / 2.0f, game.World.Height() / 2.0f);
        }

        // Create player at center
        _player = new Player(_cam);
        game.ActorManager?.Register(_player);
        Sheep.Populate(game, 500);
        Wolf.Populate(game, 100);
        Tree.Populate(game, 600);
        var wolfPos = _cam;
        wolfPos.X += 5.0f;
        var sheepPos = _cam;
        sheepPos.X -= 5.0f;

        game.ActorManager?.Register(new Wolf(wolfPos));
        game.ActorManager?.Register(new Sheep(sheepPos));

        _player.Inventory.OnInventoryChanged += RebuildInventoryUI;

        CreateUI();
    }

    private void CreateUI()
    {
        UISystem.Clear();

        // Toggle Button
        float toggleX = -18.0f; // Bottom left (Adjusted inwards)
        float toggleY = -13.0f;
        float toggleW = 8.0f;
        float toggleH = 3.0f;
        
        _btnBuildToggle = UIButton.Create("BUILD", toggleX, toggleY, toggleW, toggleH, 0.2f, 0.25f, 0.3f, 51, 1, false);
        _btnBuildToggle.Label.Color(1.0f, 0.6f, 0.0f); // Orange Text
        _btnBuildToggle.Clicked += () => {
            _menuOpen = !_menuOpen;
            // Toggle button color
            _btnBuildToggle.SetBaseColor(_menuOpen ? 0.3f : 0.2f, _menuOpen ? 0.35f : 0.25f, _menuOpen ? 0.4f : 0.3f);
        };

        // Build Menu Scroll Panel
        float panelW = 10.0f;
        float panelH = 18.0f;
        float panelX = toggleX; 
        // Position panel above the button
        float panelY = toggleY + (toggleH / 2.0f) + (panelH / 2.0f) + 0.5f;
        
        _buildMenu = UIScrollPanel.Create(panelX, panelY, panelW, panelH, 50, false);
        _buildMenu.Background.Color(0.1f, 0.1f, 0.15f); // Dark Blue-ish background
        _buildMenu.SetBorder(0.2f, 0.4f, 0.5f, 0.6f); // Light Blue-Grey Border
        _buildMenu.SetVisible(false); // Start hidden
        
        // Buttons
        float btnSize = 4.0f;
        float gap = 0.5f;
        int cols = 2;
        
        // Calculate start position (top-left of content area)
        // Content is centered in panel.
        // Let's just use relative coordinates from top-left of panel content area.
        // UIScrollPanel centers content vertically based on ContentHeight? No, it uses relative Y.
        // Let's start from top.
        float startY = (panelH / 2.0f) - (btnSize / 2.0f) - gap;
        float startX = -(panelW / 2.0f) + (btnSize / 2.0f) + gap; // Left side

        // Helper to create styled button
        UIButton CreateBtn(string text, FactoryStructure structure, int index, Action onClick)
        {
            int row = index / cols;
            int col = index % cols;

            // Relative position inside the panel
            float x = startX + col * (btnSize + gap); 
            float y = startY - row * (btnSize + gap);
            
            // Create button at (0,0) initially, panel will position it
            var btn = UIButton.Create("", 0, 0, btnSize, btnSize, 0.3f, 0.3f, 0.3f, 51, 1, false); 
            btn.IconCentered = true;
            btn.Clicked += onClick;

            // Set texture
            IntPtr tex = FactoryResources.GetStructureTexture(structure, 1);
            if (tex != IntPtr.Zero)
            {
                btn.SetIcon(tex);
            }
            
            _buildMenu.AddChild(btn, x, y);
            return btn;
        }

        _btnConveyor = CreateBtn("Conveyor", FactoryStructure.ConveyorBelt, 0, () => { _selectedStructure = FactoryStructure.ConveyorBelt; _deleteMode = false; });
        _btnMiner = CreateBtn("Miner", FactoryStructure.Miner, 1, () => { _selectedStructure = FactoryStructure.Miner; _deleteMode = false; });
        _btnStorage = CreateBtn("Storage", FactoryStructure.Storage, 2, () => { _selectedStructure = FactoryStructure.Storage; _deleteMode = false; });
        _btnFarm = CreateBtn("Farm", FactoryStructure.FarmPlot, 3, () => { _selectedStructure = FactoryStructure.FarmPlot; _deleteMode = false; });
        _btnWall = CreateBtn("Wall", FactoryStructure.Wall, 4, () => { _selectedStructure = FactoryStructure.Wall; _deleteMode = false; });

        // Tier Buttons
        float tierBtnSize = 2.0f;
        
        UIButton CreateTierBtn(string text, int tier)
        {
            var btn = UIButton.Create(text, 0, 0, tierBtnSize, tierBtnSize, 0.2f, 0.25f, 0.3f, 51, 1, false);
            btn.Label.Color(1.0f, 1.0f, 1.0f);
            btn.Clicked += () => { _currentTier = tier; };
            btn.SetVisible(false);
            return btn;
        }

        _btnTier1 = CreateTierBtn("1", 1);
        _btnTier2 = CreateTierBtn("2", 2);
        _btnTier3 = CreateTierBtn("3", 3);

        // Set content height for scrolling
        int totalRows = (5 + cols - 1) / cols;
        _buildMenu.ContentHeight = totalRows * (btnSize + gap) + gap * 2;

        // Tooltip
        var ttBg = UIImage.Create(0, 0, 1, 1);
        ttBg.Layer(199);
        ttBg.Color(0.1f, 0.1f, 0.1f);
        ttBg.IsVisible(false);
        ttBg.UseScreenSpace(false);
        _tooltipBg = ttBg;

        var ttTitle = UIText.Create("", 1, 0, 0);
        ttTitle.Layer(200);
        ttTitle.Color(1.0f, 0.9f, 0.2f); // Gold
        ttTitle.IsVisible(false);
        ttTitle.UseScreenSpace(false);
        ttTitle.Anchor(0.5f, 0.5f);
        _tooltipTitle = ttTitle;

        var ttBody = UIText.Create("", 1, 0, 0);
        ttBody.Layer(200);
        ttBody.Color(0.9f, 0.9f, 0.9f); // White
        ttBody.IsVisible(false);
        ttBody.UseScreenSpace(false);
        ttBody.Anchor(0.5f, 0.5f);
        _tooltipBody = ttBody;

        // Inventory Label - Removed
        // var invLabel = UIText.Create("Inventory:", 1, -19.0f, 10.0f); // Top Left
        // invLabel.Anchor(0.0f, 1.0f);
        // invLabel.Layer(52);
        // invLabel.Color(1.0f, 1.0f, 1.0f);
        // _inventoryLabel = invLabel;

        // Inventory Toggle Button
        float invToggleX = 18.0f; // Bottom Right
        float invToggleY = -13.0f;
        float invToggleW = 8.0f;
        float invToggleH = 3.0f;
        
        _btnInventoryToggle = UIButton.Create("INVENTORY", invToggleX, invToggleY, invToggleW, invToggleH, 0.2f, 0.25f, 0.3f, 51, 1, false);
        _btnInventoryToggle.Label.Color(0.0f, 0.8f, 1.0f); // Cyan Text
        _btnInventoryToggle.Clicked += () => {
            _inventoryOpen = !_inventoryOpen;
            _btnInventoryToggle.SetBaseColor(_inventoryOpen ? 0.3f : 0.2f, _inventoryOpen ? 0.35f : 0.25f, _inventoryOpen ? 0.4f : 0.3f);
        };

        // Inventory Scroll Panel
        float invPanelW = 10.0f;
        float invPanelH = 18.0f;
        float invPanelX = invToggleX; 
        float invPanelY = invToggleY + (invToggleH / 2.0f) + (invPanelH / 2.0f) + 0.5f;
        
        _inventoryMenu = UIScrollPanel.Create(invPanelX, invPanelY, invPanelW, invPanelH, 50, false);
        _inventoryMenu.Background.Color(0.15f, 0.1f, 0.1f); // Dark Red-ish background
        _inventoryMenu.SetBorder(0.2f, 0.4f, 0.2f, 0.6f); // Orange-Grey Border, matching thickness
        _inventoryMenu.SetVisible(false); // Start hidden

        RebuildInventoryUI();
    }

    private void RebuildInventoryUI()
    {
        if (_inventoryMenu == null || _player == null) return;
        
        _inventoryMenu.Clear();
        _inventoryButtonItems.Clear();
        
        float btnSize = 2.5f;
        float gap = 0.3f;
        int cols = 3;
        
        float startY = (_inventoryMenu.Height / 2.0f) - (btnSize / 2.0f) - gap;
        float startX = -(_inventoryMenu.Width / 2.0f) + (btnSize / 2.0f) + gap;

        int index = 0;
        foreach (var slot in _player.Inventory.Slots)
        {
            int row = index / cols;
            int col = index % cols;

            float x = startX + col * (btnSize + gap); 
            float y = startY - row * (btnSize + gap);
            
            var btn = UIButton.Create(slot.Count.ToString(), 0, 0, btnSize, btnSize, 0.3f, 0.3f, 0.3f, 51, 1, false);
            btn.IconCentered = true;
            btn.RenderLabelOverIcon = true;
            btn.LabelOffset = (0.0f, -0.8f); // Bottom center
            btn.Label.Color(1.0f, 1.0f, 1.0f);
            
            if (slot.Item.IconTexture != IntPtr.Zero)
            {
                btn.SetIcon(slot.Item.IconTexture);
            }
            
            _inventoryMenu.AddChild(btn, x, y);
            _inventoryButtonItems[btn] = slot.Item;
            index++;
        }
        
        int totalRows = (index + cols - 1) / cols;
        _inventoryMenu.ContentHeight = totalRows * (btnSize + gap) + gap * 2;
    }

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
        SetBtnState(_btnFarm, !_deleteMode && _selectedStructure == FactoryStructure.FarmPlot);
        SetBtnState(_btnWall, !_deleteMode && _selectedStructure == FactoryStructure.Wall);

        // Update Tier Buttons Highlight
        void SetTierBtnState(UIButton? btn, int tier)
        {
            if (btn == null) return;
            if (_currentTier == tier) btn.SetBaseColor(0.5f, 0.5f, 0.6f); // Active
            else btn.SetBaseColor(0.2f, 0.2f, 0.2f); // Inactive
        }
        
        SetTierBtnState(_btnTier1, 1);
        SetTierBtnState(_btnTier2, 2);
        SetTierBtnState(_btnTier3, 3);

        // Update Icons if Tier Changed
        if (_currentTier != _lastTier)
        {
            void UpdateBtnIcon(UIButton? btn, FactoryStructure structure)
            {
                if (btn == null) return;
                IntPtr tex = FactoryResources.GetStructureTexture(structure, _currentTier);
                if (tex != IntPtr.Zero)
                {
                    btn.SetIcon(tex);
                }
            }

            UpdateBtnIcon(_btnConveyor, FactoryStructure.ConveyorBelt);
            UpdateBtnIcon(_btnMiner, FactoryStructure.Miner);
            UpdateBtnIcon(_btnStorage, FactoryStructure.Storage);
            UpdateBtnIcon(_btnFarm, FactoryStructure.FarmPlot);
            UpdateBtnIcon(_btnWall, FactoryStructure.Wall);
            
            _lastTier = _currentTier;
        }
    }

    public void Exit(FactoryGame game)
    {
        Dispose();
        
        // Destroy UI Elements
        _btnConveyor?.Destroy();
        _btnMiner?.Destroy();
        _btnStorage?.Destroy();
        _btnFarm?.Destroy();
        _btnWall?.Destroy();
        _btnTier1?.Destroy();
        _btnTier2?.Destroy();
        _btnTier3?.Destroy();
        _buildMenu?.Destroy();
        _btnBuildToggle?.Destroy();
        _inventoryMenu?.Destroy();
        _btnInventoryToggle?.Destroy();
        if (_tooltipTitle is { } ttt) ttt.Destroy();
        if (_tooltipBody is { } ttb) ttb.Destroy();
        if (_tooltipBg is { } ttbg) ttbg.Destroy();

        if (_player != null)
        {
            _player.Inventory.OnInventoryChanged -= RebuildInventoryUI;
        }

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

        // Menu Animation
        float targetAnim = _menuOpen ? 1.0f : 0.0f;
        if (Math.Abs(_menuAnimT - targetAnim) > 0.001f)
        {
            float dir = targetAnim > _menuAnimT ? 1.0f : -1.0f;
            _menuAnimT += dir * MenuAnimSpeed * dt;
            _menuAnimT = Math.Clamp(_menuAnimT, 0.0f, 1.0f);
            
            if (_buildMenu != null)
            {
                // Layout Constants (Must match CreateUI)
                float toggleY = -13.0f;
                float toggleH = 3.0f;
                float panelH = 18.0f;
                float panelW = 10.0f;
                
                // Target Y (Open)
                float openY = toggleY + (toggleH / 2.0f) + (panelH / 2.0f) + 0.5f;
                // Start Y (Closed)
                float closedY = toggleY; 
                
                // Smoothstep
                float t = _menuAnimT;
                float smoothT = t * t * (3.0f - 2.0f * t);
                
                float currentY = closedY + (openY - closedY) * smoothT;
                
                _buildMenu.SetPosition(_buildMenu.X, currentY);
                _buildMenu.SetAlpha(smoothT);
                
                // Update Tier Buttons Position
                float tierBtnSize = 2.0f;
                float tierGap = 0.2f;
                float tierX = _buildMenu.X + (panelW / 2.0f) + (tierBtnSize / 2.0f) + 0.2f; // Right side
                float tierStartY = currentY + (panelH / 2.0f) - (tierBtnSize / 2.0f) - 1.0f;
                
                if (_btnTier1 != null) _btnTier1.SetPosition(tierX, tierStartY);
                if (_btnTier2 != null) _btnTier2.SetPosition(tierX, tierStartY - (tierBtnSize + tierGap));
                if (_btnTier3 != null) _btnTier3.SetPosition(tierX, tierStartY - (tierBtnSize + tierGap) * 2);

                // Visibility
                if (_menuAnimT > 0.01f)
                {
                    if (!_buildMenu.IsVisible) _buildMenu.SetVisible(true);
                    _btnTier1?.SetVisible(true);
                    _btnTier2?.SetVisible(true);
                    _btnTier3?.SetVisible(true);
                }
                else
                {
                    if (_buildMenu.IsVisible) _buildMenu.SetVisible(false);
                    _btnTier1?.SetVisible(false);
                    _btnTier2?.SetVisible(false);
                    _btnTier3?.SetVisible(false);
                }
                
                // Alpha
                _btnTier1?.SetAlpha(smoothT);
                _btnTier2?.SetAlpha(smoothT);
                _btnTier3?.SetAlpha(smoothT);
            }
        }

        // Inventory Animation
        float targetInvAnim = _inventoryOpen ? 1.0f : 0.0f;
        if (Math.Abs(_inventoryAnimT - targetInvAnim) > 0.001f)
        {
            float dir = targetInvAnim > _inventoryAnimT ? 1.0f : -1.0f;
            _inventoryAnimT += dir * MenuAnimSpeed * dt;
            _inventoryAnimT = Math.Clamp(_inventoryAnimT, 0.0f, 1.0f);
            
            if (_inventoryMenu != null)
            {
                // Layout Constants (Must match CreateUI)
                float toggleY = -13.0f;
                float toggleH = 3.0f;
                float panelH = 18.0f;
                
                // Target Y (Open)
                float openY = toggleY + (toggleH / 2.0f) + (panelH / 2.0f) + 0.5f;
                // Start Y (Closed)
                float closedY = toggleY; 
                
                // Smoothstep
                float t = _inventoryAnimT;
                float smoothT = t * t * (3.0f - 2.0f * t);
                
                float currentY = closedY + (openY - closedY) * smoothT;
                
                _inventoryMenu.SetPosition(_inventoryMenu.X, currentY);
                _inventoryMenu.SetAlpha(smoothT);
                
                // Visibility
                if (_inventoryAnimT > 0.01f)
                {
                    if (!_inventoryMenu.IsVisible) _inventoryMenu.SetVisible(true);
                }
                else
                {
                    if (_inventoryMenu.IsVisible) _inventoryMenu.SetVisible(false);
                }
            }
        }

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

        // Scroll Rotation
        float scroll = Input.GetScroll();
        if (scroll != 0 && !UISystem.IsMouseOverUI)
        {
            if (scroll > 0)
                _placementDirection = (Direction)(((int)_placementDirection + 1) % 4);
            else
            {
                int d = (int)_placementDirection - 1;
                if (d < 0) d = 3;
                _placementDirection = (Direction)d;
            }
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

    private (string itemId, int count)? GetCost(FactoryStructure structure)
    {
        return structure switch
        {
            FactoryStructure.ConveyorBelt => ("iron_ore", 1),
            FactoryStructure.Miner => ("stone", 10),
            FactoryStructure.Storage => ("stone", 5),
            FactoryStructure.FarmPlot => ("stone", 2),
            FactoryStructure.Wall => ("stone", 1),
            _ => null
        };
    }

    private void HandleMouse(FactoryGame game)
    {
        if (game.World == null) return;

        // Zoom Control (Keyboard)
        if (Input.GetKeyDown(Keycode.EQUAL) || Input.GetKeyDown(Keycode.KP_ADD)) _zoomInHeld = true;
        if (Input.GetKeyReleased(Keycode.EQUAL) || Input.GetKeyReleased(Keycode.KP_ADD)) _zoomInHeld = false;

        if (Input.GetKeyDown(Keycode.MINUS) || Input.GetKeyDown(Keycode.KP_SUBTRACT)) _zoomOutHeld = true;
        if (Input.GetKeyReleased(Keycode.MINUS) || Input.GetKeyReleased(Keycode.KP_SUBTRACT)) _zoomOutHeld = false;

        if (_zoomInHeld)
        {
            game.World.Zoom += 0.05f; // Adjust speed as needed
            game.World.Zoom = Math.Clamp(game.World.Zoom, 0.05f, 5.0f);
        }
        if (_zoomOutHeld)
        {
            game.World.Zoom -= 0.05f;
            game.World.Zoom = Math.Clamp(game.World.Zoom, 0.05f, 5.0f);
        }

        var (mx, my) = Input.GetMouseToWorld();
        int gridX = (int)Math.Floor(mx);
        int gridY = (int)Math.Floor(my);

        // Tooltip Logic
        if (_tooltipTitle is { } ttTitle && _tooltipBody is { } ttBody && _tooltipBg is { } ttBg)
        {
            bool showTooltip = false;
            string titleText = "";
            string bodyText = "";
            
            // Check UI Hover first
            if (_buildMenu != null && _buildMenu.IsVisible)
            {
                void CheckBtn(UIButton? btn, string name, FactoryStructure structure)
                {
                    if (btn != null && btn.IsHovered)
                    {
                        titleText = name;
                        var cost = GetCost(structure);
                        bodyText = cost.HasValue ? $"Cost: {cost.Value.count} {cost.Value.itemId}" : "Free";
                        showTooltip = true;
                    }
                }

                CheckBtn(_btnConveyor, "Conveyor Belt", FactoryStructure.ConveyorBelt);
                CheckBtn(_btnMiner, "Miner", FactoryStructure.Miner);
                CheckBtn(_btnStorage, "Storage", FactoryStructure.Storage);
                CheckBtn(_btnFarm, "Farm Plot", FactoryStructure.FarmPlot);
                CheckBtn(_btnWall, "Wall", FactoryStructure.Wall);
            }
            
            // Check Inventory Hover
            if (_inventoryMenu != null && _inventoryMenu.IsVisible)
            {
                foreach (var (btn, item) in _inventoryButtonItems)
                {
                    if (btn.IsHovered)
                    {
                        titleText = item.Name;
                        bodyText = item.Description;
                        showTooltip = true;
                    }
                }
            }

            // If not hovering UI, check world
            if (!showTooltip && gridX >= 0 && gridX < game.World.Width() && gridY >= 0 && gridY < game.World.Height())
            {
                var tile = game.World[gridX, gridY];
                if (tile.Structure == FactoryStructure.Storage && game.BuildingSystem != null)
                {
                    var (count, item) = game.BuildingSystem.GetBuildingInventory(gridX, gridY);
                    titleText = "Storage";
                    bodyText = $"{item}: {count}";
                    showTooltip = true;
                }
                else if (tile.Structure == FactoryStructure.Miner)
                {
                     titleText = "Miner";
                     bodyText = $"Mining: {tile.OreType}";
                     showTooltip = true;
                }
            }

            if (showTooltip)
            {
                ttTitle.Text(titleText);
                ttBody.Text(bodyText);
                
                // Calculate sizes
                var (tw, th) = ttTitle.GetSize();
                var (bw, bh) = ttBody.GetSize();
                
                float spacing = 0.3f;
                float padding = 0.6f;
                
                float totalW = Math.Max(tw, bw);
                float totalH = th + bh + spacing;
                
                // Calculate bounds
                float bgW = totalW + padding * 2;
                float bgH = totalH + padding * 2;

                // Get Viewport Info
                NativeMethods.Input_GetViewportRect(out _, out _, out int vpW, out int vpH);
                float aspect = (float)vpW / vpH;
                float viewH = FactoryGame.VIEW_H;
                float viewW = viewH * aspect;
                
                float screenRight = viewW / 2.0f;
                float screenLeft = -viewW / 2.0f;
                float screenTop = viewH / 2.0f;
                float screenBottom = -viewH / 2.0f;

                // Mouse Pos in UI Space
                float zoom = game.World.Zoom;
                float mouseUiX = (mx - _cam.X) * zoom;
                float mouseUiY = (my - _cam.Y) * zoom;
                
                // Default Position (Bottom-Right of mouse)
                float offset = 1.0f;
                // Position is center of tooltip
                float ttX = mouseUiX + offset + bgW / 2.0f;
                float ttY = mouseUiY - offset - bgH / 2.0f;
                
                // Check Right Edge
                if (ttX + bgW / 2.0f > screenRight)
                {
                    // Flip to Left
                    ttX = mouseUiX - offset - bgW / 2.0f;
                }
                
                // Check Bottom Edge
                if (ttY - bgH / 2.0f < screenBottom)
                {
                    // Flip to Top
                    ttY = mouseUiY + offset + bgH / 2.0f;
                }
                
                // Clamp to ensure it stays on screen even after flip
                float halfW = bgW / 2.0f;
                if (ttX + halfW > screenRight) ttX = screenRight - halfW;
                if (ttX - halfW < screenLeft) ttX = screenLeft + halfW;
                
                float halfH = bgH / 2.0f;
                if (ttY + halfH > screenTop) ttY = screenTop - halfH;
                if (ttY - halfH < screenBottom) ttY = screenBottom + halfH;

                // Update Background
                ttBg.Size = (bgW, bgH);
                ttBg.Position = (ttX, ttY);
                ttBg.Anchor(0.5f, 0.5f);
                
                // Update Text Positions (Centered vertically in their slots)
                // Title Top
                ttTitle.Position = (ttX, ttY + totalH / 2.0f - th / 2.0f);
                // Body Bottom
                ttBody.Position = (ttX, ttY - totalH / 2.0f + bh / 2.0f);
                
                ttTitle.IsVisible(true);
                ttBody.IsVisible(true);
                ttBg.IsVisible(true);
            }
            else
            {
                ttTitle.IsVisible(false);
                ttBody.IsVisible(false);
                ttBg.IsVisible(false);
            }
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
                if (_selectedStructure != FactoryStructure.None)
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
                                (tile.Structure == FactoryStructure.Miner || tile.Structure == FactoryStructure.Storage || tile.Structure == FactoryStructure.FarmPlot))
                        {
                            isValid = false;
                        }
                        
                        // Check Affordability
                        var cost = GetCost(_selectedStructure);
                        if (cost.HasValue && _player != null && !_player.Inventory.HasItem(cost.Value.itemId, cost.Value.count))
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
                else
                {
                    // Default Cursor (Yellow Highlight)
                    Native.Entity_SetTexturePtr(_cursorEntity, IntPtr.Zero);
                    Native.Entity_SetColor(_cursorEntity, 1.0f, 1.0f, 0.0f);
                    Native.Entity_SetAlpha(_cursorEntity, 0.4f);
                    Native.Entity_SetSize(_cursorEntity, 1.0f, 1.0f);
                    Native.Entity_SetRotation(_cursorEntity, 0.0f);
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
        if (isMouseDown && !UISystem.IsMouseOverUI && !_inputBlockedByUI && _selectedStructure != FactoryStructure.None)
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
                    
                    // Check if this is just a rotation (same structure, same tier)
                    bool isRotation = tile.Structure == _selectedStructure && tile.Tier == _currentTier;

                    // Check Cost
                    var cost = GetCost(_selectedStructure);
                    bool canAfford = true;
                    
                    // Only check cost if it's NOT just a rotation
                    if (!isRotation && cost.HasValue && _player != null)
                    {
                        if (!_player.Inventory.HasItem(cost.Value.itemId, cost.Value.count))
                        {
                            canAfford = false;
                        }
                    }

                    if (canAfford)
                    {
                        bool placed = false;
                        game.World.Set(gridX, gridY, o =>
                        {
                            // Only charge if something actually changed (simplified check)
                            if (o.Structure != _selectedStructure || o.Direction != newDir || o.Tier != _currentTier)
                            {
                                o.Structure = _selectedStructure;
                                o.Direction = newDir;
                                o.Tier = _currentTier;
                                placed = true;
                            }
                        });
                        
                        if (placed)
                        {
                            game.World.UpdateNeighbors(game, new Vec2i(gridX, gridY));
                            // Deduct cost only if not rotation
                            if (!isRotation && cost.HasValue && _player != null)
                            {
                                _player.Inventory.RemoveItem(cost.Value.itemId, cost.Value.count);
                            }
                        }
                    }
                    
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

        // Right Click Logic
        bool isRightMouseDown = Input.IsMouseDown(Input.MouseButton.Right);
        
        if (isRightMouseDown)
        {
            if (!_wasRightMouseDown)
            {
                // Just pressed
                _rightClickStart = new Vec2(mx, my);
                _isRightClickDragging = false;
            }
            else
            {
                // Held
                float dist = (new Vec2(mx, my) - _rightClickStart).Length();
                if (dist > 0.5f) // Threshold
                {
                    _isRightClickDragging = true;
                }

                if (_isRightClickDragging)
                {
                    // Draw Selection Box
                    float minX = Math.Min(_rightClickStart.X, mx);
                    float maxX = Math.Max(_rightClickStart.X, mx);
                    float minY = Math.Min(_rightClickStart.Y, my);
                    float maxY = Math.Max(_rightClickStart.Y, my);
                    
                    float w = maxX - minX;
                    float h = maxY - minY;
                    float cx = minX + w / 2.0f;
                    float cy = minY + h / 2.0f;
                    
                    if (_selectionBoxEntity != 0)
                    {
                        Native.Entity_SetPosition(_selectionBoxEntity, cx, cy);
                        Native.Entity_SetSize(_selectionBoxEntity, w, h);
                        Native.Entity_SetRender(_selectionBoxEntity, true);
                    }
                }
            }
        }
        else
        {
            // Released
            if (_wasRightMouseDown)
            {
                if (_isRightClickDragging)
                {
                    // Perform Delete in Area
                    float minX = Math.Min(_rightClickStart.X, mx);
                    float maxX = Math.Max(_rightClickStart.X, mx);
                    float minY = Math.Min(_rightClickStart.Y, my);
                    float maxY = Math.Max(_rightClickStart.Y, my);
                    
                    int startX = (int)Math.Floor(minX);
                    int endX = (int)Math.Floor(maxX);
                    int startY = (int)Math.Floor(minY);
                    int endY = (int)Math.Floor(maxY);
                    
                    // Clamp to world bounds
                    startX = Math.Clamp(startX, 0, game.World.Width() - 1);
                    endX = Math.Clamp(endX, 0, game.World.Width() - 1);
                    startY = Math.Clamp(startY, 0, game.World.Height() - 1);
                    endY = Math.Clamp(endY, 0, game.World.Height() - 1);
                    
                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            var tile = game.World[x, y];
                            if (tile.Structure != FactoryStructure.None)
                            {
                                // Refund
                                var cost = GetCost(tile.Structure);
                                if (cost.HasValue && _player != null)
                                {
                                    var itemDef = ItemRegistry.Get(cost.Value.itemId);
                                    if (itemDef != null)
                                    {
                                        _player.Inventory.AddItem(itemDef, cost.Value.count);
                                    }
                                }
                                
                                // Remove
                                game.World.Set(x, y, o => o.Structure = FactoryStructure.None);
                                game.World.UpdateNeighbors(game, new Vec2i(x, y));
                            }
                        }
                    }
                }
                else
                {
                    // Single Click: Deselect
                    _selectedStructure = FactoryStructure.None;
                    _deleteMode = false;
                }
                
                // Hide Selection Box
                if (_selectionBoxEntity != 0)
                {
                    Native.Entity_SetRender(_selectionBoxEntity, false);
                }
                
                _isRightClickDragging = false;
            }
        }
        
        _wasRightMouseDown = isRightMouseDown;
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

        if (_selectionBoxEntity != 0)
        {
            Native.Entity_Destroy(_selectionBoxEntity);
            _selectionBoxEntity = 0;
        }
    }

    ~StateFactoryPlay()
    {
        Dispose(false);
    }
}
