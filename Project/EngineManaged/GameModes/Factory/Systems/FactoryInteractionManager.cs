using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.Buildings;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.GameModes.Factory.UI;
using SlimeCore.GameModes.Factory.World;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Linq;

namespace SlimeCore.GameModes.Factory.Systems;

public class FactoryInteractionManager : IDisposable
{
    private ulong _cursorEntity;
    private ulong _cursorDirectionEntity;
    private ulong _selectionBoxEntity;
    
    private int _lastGridX = -1;
    private int _lastGridY = -1;
    private bool _wasMouseDown;
    private bool _wasRightMouseDown;
    private Vec2 _rightClickStart;
    private bool _isRightClickDragging;
    
    private Direction _placementDirection = Direction.North;
    private bool _inputBlockedByUI;
    private bool _zoomInHeld;
    private bool _zoomOutHeld;

    public void Initialize()
    {
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
    }

    public void Update(FactoryGame game, float dt, FactoryGameUI ui, Player? player, Vec2 camPos)
    {
        if (game.World == null) return;

        HandleRotation();
        HandleZoom(game);
        
        var (mx, my) = Input.GetMouseToWorld();
        int gridX = (int)Math.Floor(mx);
        int gridY = (int)Math.Floor(my);

        ui.UpdateTooltip(game, camPos, mx, my);

        UpdateCursorVisuals(game, ui, player, gridX, gridY);
        HandleLeftClick(game, ui, player, gridX, gridY);
        HandleRightClick(game, ui, player, mx, my);
    }

    private void HandleRotation()
    {
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
    }

    private void HandleZoom(FactoryGame game)
    {
        if (game.World == null) return;

        // Zoom Control (Keyboard)
        if (Input.GetKeyDown(Keycode.EQUAL) || Input.GetKeyDown(Keycode.KP_ADD)) _zoomInHeld = true;
        if (Input.GetKeyReleased(Keycode.EQUAL) || Input.GetKeyReleased(Keycode.KP_ADD)) _zoomInHeld = false;

        if (Input.GetKeyDown(Keycode.MINUS) || Input.GetKeyDown(Keycode.KP_SUBTRACT)) _zoomOutHeld = true;
        if (Input.GetKeyReleased(Keycode.MINUS) || Input.GetKeyReleased(Keycode.KP_SUBTRACT)) _zoomOutHeld = false;

        if (_zoomInHeld)
        {
            game.World.Zoom += 0.05f; 
            game.World.Zoom = Math.Clamp(game.World.Zoom, 0.05f, 5.0f);
        }
        if (_zoomOutHeld)
        {
            game.World.Zoom -= 0.05f;
            game.World.Zoom = Math.Clamp(game.World.Zoom, 0.05f, 5.0f);
        }
    }

    private void UpdateCursorVisuals(FactoryGame game, FactoryGameUI ui, Player? player, int gridX, int gridY)
    {
        if (game.World == null) return;

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
                if (!string.IsNullOrEmpty(ui.SelectedBuildingId))
                {
                    if (ui.DeleteMode)
                    {
                        Native.Entity_SetColor(_cursorEntity, 1.0f, 0.0f, 0.0f); // Red for delete
                        Native.Entity_SetAlpha(_cursorEntity, 0.5f);
                        Native.Entity_SetTexturePtr(_cursorEntity, IntPtr.Zero); // No texture, just color overlay
                    }
                    else
                    {
                        var def = BuildingRegistry.Get(ui.SelectedBuildingId);
                        
                        // Check validity
                        bool isValid = true;
                        var tile = game.World[gridX, gridY];
                        
                        // Miner check
                        if (def != null && def.Components.Any(c => c.Type == "Miner") && tile.OreType == FactoryOre.None)
                        {
                            isValid = false;
                        }
                        
                        // Conveyor check (don't overwrite buildings)
                        if (ui.SelectedBuildingId == "conveyor" && 
                                (!string.IsNullOrEmpty(tile.BuildingId) && tile.BuildingId != "conveyor"))
                        {
                            isValid = false;
                        }
                        
                        // Check Affordability
                        if (def != null && player != null)
                        {
                            foreach (var cost in def.Cost)
                            {
                                if (!player.Inventory.HasItem(cost.Key, cost.Value))
                                {
                                    isValid = false;
                                    break;
                                }
                            }
                        }

                        // Set texture based on selected structure
                        nint tex = def?.Texture ?? IntPtr.Zero;
                        
                        // Reset size
                        Native.Entity_SetSize(_cursorEntity, 1.0f, 1.0f);

                        if (ui.SelectedBuildingId == "conveyor")
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
                            // Fallback
                            Native.Entity_SetTexturePtr(_cursorEntity, IntPtr.Zero);
                            if (!isValid)
                                Native.Entity_SetColor(_cursorEntity, 1.0f, 0.0f, 0.0f);
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
                bool showArrow = inBounds && ui.SelectedBuildingId == "conveyor" && !ui.DeleteMode;
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
    }

    private void HandleLeftClick(FactoryGame game, FactoryGameUI ui, Player? player, int gridX, int gridY)
    {
        if (game.World == null) return;

        bool isMouseDown = Input.IsMouseDown(Input.MouseButton.Left);

        // Handle UI Blocking Logic
        if (isMouseDown && !_wasMouseDown) // Just pressed
        {
            _inputBlockedByUI = ui.IsMouseOverUI;
        }
        else if (!isMouseDown) // Released
        {
            _inputBlockedByUI = false;
        }
        
        // Don't place tiles if mouse is over UI or if the click started on UI
        if (isMouseDown && !ui.IsMouseOverUI && !_inputBlockedByUI && !string.IsNullOrEmpty(ui.SelectedBuildingId))
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
                
                if (ui.DeleteMode)
                {
                    if (!string.IsNullOrEmpty(tile.BuildingId))
                    {
                        game.World.Set(gridX, gridY, o => {
                            o.BuildingId = null;
                        });
                        game.World.UpdateNeighbors(game, new Vec2i(gridX, gridY));
                    }
                }
                else
                {
                    var def = BuildingRegistry.Get(ui.SelectedBuildingId);
                    if (def == null) return;

                    // Check placement rules
                    if (def.Components.Any(c => c.Type == "Miner") && tile.OreType == FactoryOre.None)
                    {
                        return;
                    }

                    if (ui.SelectedBuildingId == "conveyor" && 
                        (!string.IsNullOrEmpty(tile.BuildingId) && tile.BuildingId != "conveyor"))
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
                        if (game.World[_lastGridX, _lastGridY].BuildingId == "conveyor")
                        {
                            game.World.Set(_lastGridX, _lastGridY, o => o.Direction = newDir);
                        }
                    }
                    else
                    {
                        newDir = _placementDirection;
                    }

                    // Check if this is just a rotation
                    bool isRotation = tile.BuildingId == ui.SelectedBuildingId;
                    
                    // Check Cost
                    bool canAfford = true;
                    if (!isRotation && player != null)
                    {
                        foreach (var cost in def.Cost)
                        {
                            if (!player.Inventory.HasItem(cost.Key, cost.Value))
                            {
                                canAfford = false;
                                break;
                            }
                        }
                    }

                    if (canAfford)
                    {
                        bool placed = false;
                        game.World.Set(gridX, gridY, o =>
                        {
                            // Only charge if something actually changed
                            if (o.BuildingId != ui.SelectedBuildingId || o.Direction != newDir)
                            {
                                o.BuildingId = ui.SelectedBuildingId;
                                o.Direction = newDir;
                                placed = true;
                            }
                        });
                        
                        if (placed)
                        {
                            game.World.UpdateNeighbors(game, new Vec2i(gridX, gridY));
                            // Deduct cost
                            if (!isRotation && player != null)
                            {
                                foreach (var cost in def.Cost)
                                {
                                    player.Inventory.RemoveItem(cost.Key, cost.Value);
                                }
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
    }

    private void HandleRightClick(FactoryGame game, FactoryGameUI ui, Player? player, float mx, float my)
    {
        if (game.World == null) return;

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
                            string? idToRefund = tile.BuildingId;

                            if (!string.IsNullOrEmpty(tile.BuildingId))
                            {
                                // Refund
                                if (!string.IsNullOrEmpty(idToRefund))
                                {
                                    var def = BuildingRegistry.Get(idToRefund);
                                    if (def != null && player != null)
                                    {
                                        foreach (var cost in def.Cost)
                                        {
                                            var itemDef = ItemRegistry.Get(cost.Key);
                                            if (itemDef != null)
                                            {
                                                player.Inventory.AddItem(itemDef, cost.Value);
                                            }
                                        }
                                    }
                                }
                                
                                // Remove
                                game.World.Set(x, y, o => {
                                    o.BuildingId = null;
                                });
                                game.World.UpdateNeighbors(game, new Vec2i(x, y));
                            }
                        }
                    }
                }
                else
                {
                    // Single Click: Deselect
                    ui.ClearSelection();
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
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
}
