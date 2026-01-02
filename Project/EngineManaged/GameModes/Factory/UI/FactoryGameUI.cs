using EngineManaged.Numeric;
using EngineManaged.UI;
using SlimeCore.GameModes.Factory.Actors;
using SlimeCore.GameModes.Factory.Buildings;
using SlimeCore.GameModes.Factory.Items;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using SlimeCore.Source.World.Actors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlimeCore.GameModes.Factory.UI;

public class FactoryGameUI : IDisposable
{
    // State
    public string? SelectedBuildingId { get; private set; }
    public bool DeleteMode { get; set; }
    public int CurrentTier { get; private set; } = 1;
    
    private string? _selectedCategory;
    
    // Menu Animation
    public bool MenuOpen { get; set; }
    private float _menuAnimT;
    private const float MenuAnimSpeed = 5.0f;

    // Inventory Animation
    public bool InventoryOpen { get; set; }
    private float _inventoryAnimT;
    private UIScrollPanel? _inventoryMenu;
    private UIButton? _btnInventoryToggle;

    // UI Elements
    private UIScrollPanel? _buildMenu;
    private UIButton? _btnBuildToggle;
    private UIButton? _btnTier1;
    private UIButton? _btnTier2;
    private UIButton? _btnTier3;
    
    private Dictionary<UIButton, string> _buildingButtons = new();
    
    private UIText? _tooltipTitle;
    private UIText? _tooltipBody;
    private UIImage? _tooltipBg;
    
    private Dictionary<UIButton, ItemDefinition> _inventoryButtonItems = new();

    private Player? _player;

    public bool IsMouseOverUI => UISystem.IsMouseOverUI;
    public bool IsBuildMenuVisible => _buildMenu?.IsVisible ?? false;
    public bool IsInventoryMenuVisible => _inventoryMenu?.IsVisible ?? false;

    public void Initialize(Player player)
    {
        _player = player;
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
            MenuOpen = !MenuOpen;
            // Toggle button color
            _btnBuildToggle.SetBaseColor(MenuOpen ? 0.3f : 0.2f, MenuOpen ? 0.35f : 0.25f, MenuOpen ? 0.4f : 0.3f);
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
        
        float startY = (panelH / 2.0f) - (btnSize / 2.0f) - gap;
        float startX = -(panelW / 2.0f) + (btnSize / 2.0f) + gap; // Left side

        var buildings = BuildingRegistry.GetAll().ToList();
        // Group by Category
        var categories = buildings.Select(b => b.Category ?? b.Id).Distinct().ToList();
        
        _buildingButtons.Clear();

        for (int i = 0; i < categories.Count; i++)
        {
            string cat = categories[i];
            int row = i / cols;
            int col = i % cols;

            // Relative position inside the panel
            float x = startX + col * (btnSize + gap); 
            float y = startY - row * (btnSize + gap);
            
            // Create button at (0,0) initially, panel will position it
            var btn = UIButton.Create("", 0, 0, btnSize, btnSize, 0.3f, 0.3f, 0.3f, 51, 1, false); 
            btn.IconCentered = true;
            btn.Clicked += () => { 
                _selectedCategory = cat;
                UpdateSelectedBuilding();
                DeleteMode = false; 
            };

            // Set texture (use Tier 1 or first available)
            var def = buildings.FirstOrDefault(b => (b.Category ?? b.Id) == cat && b.Tier == 1) 
                      ?? buildings.FirstOrDefault(b => (b.Category ?? b.Id) == cat);
            
            if (def != null && def.Texture != IntPtr.Zero)
            {
                btn.SetIcon(def.Texture);
            }
            
            _buildMenu.AddChild(btn, x, y);
            _buildingButtons[btn] = cat;
        }

        // Tier Buttons
        float tierBtnSize = 2.0f;
        
        UIButton CreateTierBtn(string text, int tier)
        {
            var btn = UIButton.Create(text, 0, 0, tierBtnSize, tierBtnSize, 0.2f, 0.25f, 0.3f, 51, 1, false);
            btn.Label.Color(1.0f, 1.0f, 1.0f);
            btn.Clicked += () => { 
                CurrentTier = tier; 
                UpdateSelectedBuilding();
            };
            btn.SetVisible(false);
            return btn;
        }

        _btnTier1 = CreateTierBtn("1", 1);
        _btnTier2 = CreateTierBtn("2", 2);
        _btnTier3 = CreateTierBtn("3", 3);

        // Set content height for scrolling
        int totalRows = (categories.Count + cols - 1) / cols;
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

        // Inventory Toggle Button
        float invToggleX = 18.0f; // Bottom Right
        float invToggleY = -13.0f;
        float invToggleW = 8.0f;
        float invToggleH = 3.0f;
        
        _btnInventoryToggle = UIButton.Create("INVENTORY", invToggleX, invToggleY, invToggleW, invToggleH, 0.2f, 0.25f, 0.3f, 51, 1, false);
        _btnInventoryToggle.Label.Color(0.0f, 0.8f, 1.0f); // Cyan Text
        _btnInventoryToggle.Clicked += () => {
            InventoryOpen = !InventoryOpen;
            _btnInventoryToggle.SetBaseColor(InventoryOpen ? 0.3f : 0.2f, InventoryOpen ? 0.35f : 0.25f, InventoryOpen ? 0.4f : 0.3f);
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

    public void UpdateSelectedBuilding()
    {
        if (string.IsNullOrEmpty(_selectedCategory))
        {
            SelectedBuildingId = null;
            return;
        }

        var buildings = BuildingRegistry.GetAll();
        // Try to find exact match
        var match = buildings.FirstOrDefault(b => (b.Category ?? b.Id) == _selectedCategory && b.Tier == CurrentTier);
        
        if (match != null)
        {
            SelectedBuildingId = match.Id;
        }
        else
        {
            // Fallback to Tier 1 or any available
            match = buildings.FirstOrDefault(b => (b.Category ?? b.Id) == _selectedCategory && b.Tier == 1)
                    ?? buildings.FirstOrDefault(b => (b.Category ?? b.Id) == _selectedCategory);
            
            if (match != null)
            {
                SelectedBuildingId = match.Id;
            }
            else
            {
                SelectedBuildingId = null;
            }
        }
    }

    public void ClearSelection()
    {
        _selectedCategory = null;
        UpdateSelectedBuilding();
        DeleteMode = false;
    }

    public void SetTier(int tier)
    {
        CurrentTier = tier;
        UpdateSelectedBuilding();
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
        foreach (var kvp in _buildingButtons)
        {
            var btn = kvp.Key;
            string cat = kvp.Value;
            bool active = !DeleteMode && _selectedCategory == cat;
            
            if (active) btn.SetBaseColor(0.5f, 0.5f, 0.6f);
            else btn.SetBaseColor(0.3f, 0.3f, 0.3f);

            // Update Icon based on current tier
            var buildings = BuildingRegistry.GetAll();
            var def = buildings.FirstOrDefault(b => (b.Category ?? b.Id) == cat && b.Tier == CurrentTier)
                      ?? buildings.FirstOrDefault(b => (b.Category ?? b.Id) == cat);
            
            if (def != null && def.Texture != IntPtr.Zero)
            {
                btn.SetIcon(def.Texture);
            }
        }

        // Update Tier Buttons
        void SetTierBtnState(UIButton? btn, int tier)
        {
            if (btn == null) return;
            if (CurrentTier == tier) btn.SetBaseColor(0.5f, 0.5f, 0.6f); // Active
            else btn.SetBaseColor(0.2f, 0.2f, 0.2f); // Inactive
        }
        
        SetTierBtnState(_btnTier1, 1);
        SetTierBtnState(_btnTier2, 2);
        SetTierBtnState(_btnTier3, 3);
    }

    public void Update(float dt)
    {
        // Menu Animation
        float targetAnim = MenuOpen ? 1.0f : 0.0f;
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
        float targetInvAnim = InventoryOpen ? 1.0f : 0.0f;
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
        
        UISystem.Update();
        UpdateUI();
    }

    public void UpdateTooltip(FactoryGame game, Vec2 cam, float mx, float my)
    {
        if (game.World == null) return;
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
                foreach (var kvp in _buildingButtons)
                {
                    var btn = kvp.Key;
                    string cat = kvp.Value;
                    if (btn.IsHovered)
                    {
                        var buildings = BuildingRegistry.GetAll();
                        var def = buildings.FirstOrDefault(b => (b.Category ?? b.Id) == cat && b.Tier == CurrentTier)
                                  ?? buildings.FirstOrDefault(b => (b.Category ?? b.Id) == cat);

                        if (def != null)
                        {
                            titleText = def.Name;
                            if (def.Cost.Count > 0)
                            {
                                bodyText = "Cost: " + string.Join(", ", def.Cost.Select(c => $"{c.Value} {c.Key}"));
                            }
                            else
                            {
                                bodyText = "Free";
                            }
                            showTooltip = true;
                        }
                    }
                }
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
                if (!string.IsNullOrEmpty(tile.BuildingId))
                {
                    var def = BuildingRegistry.Get(tile.BuildingId);
                    if (def != null)
                    {
                        titleText = def.Name;
                        
                        // Show inventory if available
                        if (game.BuildingSystem != null)
                        {
                            var inventory = game.BuildingSystem.GetBuildingInventory(gridX, gridY);
                            if (inventory.Count > 0)
                            {
                                bodyText = string.Join("\n", inventory.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                            }
                            else if (def.Components.Any(c => c.Type == "Storage"))
                            {
                                bodyText = "Empty";
                            }
                        }
                        
                        if (def.Components.Any(c => c.Type == "Miner"))
                        {
                             if (!string.IsNullOrEmpty(bodyText)) bodyText += "\n";
                             bodyText += $"Mining: {tile.OreType}";
                        }
                        
                        showTooltip = true;
                    }
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
                float mouseUiX = (mx - cam.X) * zoom;
                float mouseUiY = (my - cam.Y) * zoom;
                
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
            // Destroy UI Elements
            foreach (var btn in _buildingButtons.Keys)
            {
                btn.Destroy();
            }
            _buildingButtons.Clear();
            
            _buildMenu?.Destroy();
            _btnBuildToggle?.Destroy();
            _btnTier1?.Destroy();
            _btnTier2?.Destroy();
            _btnTier3?.Destroy();
            _inventoryMenu?.Destroy();
            _btnInventoryToggle?.Destroy();
            if (_tooltipTitle is { } ttt) ttt.Destroy();
            if (_tooltipBody is { } ttb) ttb.Destroy();
            if (_tooltipBg is { } ttbg) ttbg.Destroy();

            if (_player != null)
            {
                _player.Inventory.OnInventoryChanged -= RebuildInventoryUI;
            }
        }
    }
}
