using EngineManaged;
using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.Scene;
using EngineManaged.UI;
using GameModes.Idle;
using GameModes.Test;
using SlimeCore.GameModes.Idle;
using SlimeCore.GameModes.Idle.Store;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SlimeCore.GameModes.Idle
{
    public class StateIdlePlaying : IGameState<IdleGame>
    {

        private UIScrollPanel? _storeMenu;
        private UIButton? _storeToggle;
        private Dictionary<string, UIButton>? _storeButtons = new();

        private UIButton? _clickButton;
        
        private UILabel? _clickAmountText;
        private UILabel? _coinsAmount;
        private UILabel? _cpsText;

        private int _clickAmoutPerClick;
        private float _clickAmoutPerClickMult = 1;

        private double _coins;
        private double _cps;

        private UIScrollPanel? _statsPanel;
        
        private bool _wasMouseDown;

        public void Enter(IdleGame game)
        {
            _coins = 0;
            _clickAmoutPerClick = 1;
            _cps = 0;
            _clickAmoutPerClickMult = 1.0f;
            
            // --- Screen Bounds ---
            var bounds = UIHelper.GetScreenBounds();
            
            // --- Stats Panel (Left) ---
            float statsW = 7.0f;
            float statsX = bounds.left + (statsW / 2.0f) + 0.5f; // 0.5f margin from edge
            
            _statsPanel = UIScrollPanel.Create(statsX, 0.0f, statsW, 19.0f, 40, false);
            _statsPanel.Background.Color(0.05f, 0.05f, 0.07f);
            _statsPanel.Background.Alpha(0.9f);
            _statsPanel.SetBorder(0.2f, 0.3f, 0.3f, 0.35f);

            // --- Store Panel (Right) ---
            float storeW = 8.5f;
            float storeXShown = bounds.right - 0.5f - (storeW / 2.0f);

            _storeMenu = UIScrollPanel.Create(storeXShown, 0.0f, storeW, 19.5f, 90, false);
            _storeMenu.Background.Color(0.05f, 0.05f, 0.08f);
            _storeMenu.Background.Alpha(0.95f);
            _storeMenu.SetBorder(0.15f, 0.0f, 0.6f, 0.8f);
            _storeMenu.SetVisible(false);

            // Store Toggle Button
            float toggleW = 1.0f;
            float toggleX_MenuOpen = (storeXShown - storeW / 2.0f) - (toggleW / 2.0f);
            float toggleX_MenuClosed = bounds.right - (toggleW / 2.0f) - 0.2f;

            _storeToggle = UIButton.Create("<", toggleX_MenuClosed, 0.0f, toggleW, 2.5f, 0.0f, 0.6f, 0.8f, 95, 1, false);
            _storeToggle.Label.Scale(0.03f);
            _storeToggle.Label.Color(1.0f, 1.0f, 1.0f);
            _storeToggle.Clicked += () => 
            {
                bool isVisible = !_storeMenu.IsVisible;
                _storeMenu.SetVisible(isVisible);
                if (isVisible)
                {
                    _storeToggle.SetPosition(toggleX_MenuOpen, 0.0f);
                    _storeToggle.SetText(">"); 
                }
                else
                {
                    _storeToggle.SetPosition(toggleX_MenuClosed, 0.0f);
                    _storeToggle.SetText("<");
                }
            };
            
            // Click Power Label
            var clickLabel = UILabel.Create("Click Power", 1, 0, 8.0f); // Rel X=0
            clickLabel.SetScale(0.02f);
            clickLabel.SetAnchor(0.5f, 1.0f);
            clickLabel.SetColor(0.7f, 0.7f, 0.7f);
            _statsPanel.AddChild(clickLabel, 0, 8.0f);

            // Click Amount
            _clickAmountText = UILabel.Create(string.Format("{0}", _clickAmoutPerClick), 1, 0, 7.2f);
            _clickAmountText.SetScale(0.04f);
            _clickAmountText.SetAnchor(0.5f, 1.0f);
            _clickAmountText.SetColor(1.0f, 1.0f, 1.0f);
            _statsPanel.AddChild(_clickAmountText, 0, 7.2f);

            // Center: The Big Button (Center Screen 0,0) - NOT parented to stats
            _clickButton = UIButton.Create("", 0.0f, 0.0f, 6.0f, 6.0f, 1f, 1f, 1f, 5, 1, false);
            _clickButton.Label.Color(1f, 1f, 1f);
            _clickButton.SetTexture(IdleResources.TexIron); 
            _clickButton.Enabled = false; 
            
            // Coins
            var coinsLabel = UILabel.Create("Coins", 1, 0, -2.0f);
            coinsLabel.SetScale(0.02f);
            coinsLabel.SetAnchor(0.5f, 1.0f);
            coinsLabel.SetColor(0.7f, 0.7f, 0.7f);
            _statsPanel.AddChild(coinsLabel, 0, -2.0f);

            _coinsAmount = UILabel.Create("0", 1, 0, -2.8f);
            _coinsAmount.SetScale(0.04f);
            _coinsAmount.SetAnchor(0.5f, 1.0f);
            _coinsAmount.SetColor(1.0f, 1.0f, 1.0f);
            _statsPanel.AddChild(_coinsAmount, 0, -2.8f);

            // CPS
            _cpsText = UILabel.Create(string.Format("{0} CPS", _cps), 1, 0, -5.5f);
            _cpsText.SetScale(0.025f);
            _cpsText.SetAnchor(0.5f, 0.5f);
            _cpsText.SetColor(0.5f, 0.8f, 1.0f);
            _statsPanel.AddChild(_cpsText, 0, -5.5f);

            // Store Header
            var headerBtn = UIButton.Create("UPGRADES", 0.0f, 0.0f, 8.0f, 1.5f, 0.0f, 0.0f, 0.0f, 92, 1, false);
            headerBtn.Background.Alpha(0.0f);
            headerBtn.Label.Scale(0.035f);
            headerBtn.Label.Color(0.0f, 0.8f, 1.0f);
            headerBtn.Enabled = false; 
            _storeMenu.AddChild(headerBtn, 0, 8.5f); 

            var store = StoreRegistry.GetAll().ToList();

            float yPos = 5.5f; // Start below header
            float btnHeight = 4.5f; 
            float btnSpacing = 5.2f;

            foreach (var item in store)
            {
                var btn = UIButton.Create("", 0.0f, 0.0f, 7.5f, btnHeight, 0.15f, 0.15f, 0.2f, 95, 1, false);
                _storeButtons.Add(item.Id, btn);
                
                bool hasIcon = item.Texture != IntPtr.Zero;
                
                btn.Label.Color(0.9f, 0.9f, 0.9f);
                btn.Label.Scale(0.016f); 
                btn.Label.WrapWidth(hasIcon ? 3.8f : 7.0f); 
                btn.Label.Anchor(0.5f, 0.5f);

                void UpdateButtonText()
                {
                    string nameLine = item.Name.ToUpper();
                    string costLine = $"Price: {item.Cost}";
                    string statLine = "";
                    if (item.ClickAdd > 0) statLine += $"+{item.ClickAdd} Click ";
                    if (item.CPS > 0) statLine += $"+{item.CPS} CPS";
                    string ownedLine = $"Owned: {item.Owned}";

                    string text = $"{nameLine}\n{costLine}\n{statLine}\n{ownedLine}";
                    btn.SetText(text);
                }

                UpdateButtonText();

                if (hasIcon)
                {
                    btn.SetIcon(item.Texture);
                    btn.IconCentered = false; 
                }

                btn.Clicked += () =>
                {
                    if (_coins >= item.Cost)
                    {
                        _coins -= item.Cost;
                        _clickAmoutPerClick += item.ClickAdd;
                        _cps += item.CPS;
                        _clickAmoutPerClickMult += item.ClickMult;
                        item.Owned++;
                        item.Cost = CalcNewPrice(item.BaseCost, item.Owned, 0);

                        UpdateText();
                        UpdateButtonText();

                        btn.SetBaseColor(0.2f, 0.6f, 0.2f);
                    }
                    else
                    {
                    }
                };

                _storeMenu.AddChild(btn, 0, yPos);
                yPos -= btnSpacing;
            }
            
            _storeMenu.ContentHeight = Math.Max(19.5f, 8.5f - yPos + 2.0f);
            
            // Ensure the menu and its new children are correctly hidden on start
            _storeMenu.SetVisible(false);
        }

        public void UpdateText()
        {
            _coinsAmount?.SetText($"{_coins:F0}");
            if (_coins > 1000000) _coinsAmount?.SetText($"{_coins/1000000:F2}M");
            else if (_coins > 1000) _coinsAmount?.SetText($"{_coins/1000:F1}k");

            _cpsText?.SetText($"{_cps:F1} CPS");
            _clickAmountText?.SetText($"{(_clickAmoutPerClick * _clickAmoutPerClickMult):F0}");
        }

        public int CalcNewPrice(int baseCost, int amountOwned, int freeAmount)
        {
            return (int)Math.Ceiling(baseCost * Math.Pow(1.15, amountOwned - freeAmount));
        }


        public void Exit(IdleGame game) 
        {
            _statsPanel?.Destroy(); 
            _storeMenu?.Destroy();
            _storeToggle?.Destroy();
            _clickButton?.Destroy();
            _storeButtons?.Clear(); 
        }


        private float secondCounter;
        public void Update(IdleGame game, float dt)
        {
            secondCounter += dt;

            if (secondCounter >= 1.0f)
            {
                _coins += _cps;
                _coinsAmount?.SetText(string.Format("{0}", (int)_coins));
                secondCounter = 0.0f;
            }

            UISystem.Update();
            
            bool isDown = Input.IsMouseDown(Input.MouseButton.Left);
            if (isDown && !_wasMouseDown && !UISystem.IsMouseOverUI)
            {
                var (mx, my) = Input.GetMousePos();
                
                game.ClickEffect(new Vec2(mx, my), 1);
                _coins += _clickAmoutPerClick * _clickAmoutPerClickMult;
                UpdateText();

                if (_clickButton != null)
                {
                    _clickButton.Background.Size = (5.8f, 5.8f);
                }
            }
            _wasMouseDown = isDown;
            
            if (_clickButton != null)
            {
                var currentSize = _clickButton.Background.Size;
                if (currentSize.w < 6.0f)
                {
                    float newS = currentSize.w + dt * 5.0f;
                    if (newS > 6.0f) newS = 6.0f;
                    _clickButton.Background.Size = (newS, newS);
                }
            }
        }

        public void Draw(IdleGame game) { }
    }
}
