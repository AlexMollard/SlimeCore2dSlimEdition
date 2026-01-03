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
        private UIText _clickAmountText;
        private int _clickAmoutPerClick;
        private float _clickAmoutPerClickMult = 1;

        private UIText _coinsAmount;
        private double _coins;

        private UIText _cpsText;
        private double _cps;


        //private int _menuTopIndex;
        public void Enter(IdleGame game)
        {
            _coins = 0;

            _storeMenu = UIScrollPanel.Create(13.5f, 0.0f, 8, 19.5f, 1, false);
            _storeMenu.Background.Color(0.1f, 0.1f, 0.15f); // Dark Blue-ish background
            _storeMenu.SetBorder(0.2f, 0.4f, 0.5f, 0.6f); // Light Blue-Grey Border
            _storeMenu.SetVisible(false); // Start hidden

            _storeToggle = UIButton.Create("<", 17.0f, -0.0f, 2.0f, 2.0f, 0.2f, 0.2f, 0.2f, 1, 1, false);
            _storeToggle.Clicked += () =>
            {
                _storeMenu.SetVisible(!_storeMenu.IsVisible);
                _storeToggle.SetPosition(_storeMenu.IsVisible ? 8.5f : 17f, 0.0f);
                _storeToggle.SetText(_storeMenu.IsVisible ? ">" : "<");
            };

            _clickAmoutPerClick = 1;
            _clickAmountText = UIText.Create(string.Format("{0}", _clickAmoutPerClick), 1, -8, -5);
            _clickAmountText.Anchor(0.5f, 0.5f); // Center anchor
            _clickAmountText.Layer(100);
            _clickAmountText.Color(1.0f, 1.0f, 1.0f);

            _coinsAmount = UIText.Create(string.Format("{0}", _coins), 1, -8, 5);
            _coinsAmount.Anchor(0.5f, 0.5f); // Center anchor
            _coinsAmount.Layer(100);
            _coinsAmount.Color(1.0f, 1.0f, 1.0f);

            _cpsText = UIText.Create(string.Format("CPS - {0}", _cps), 1, -8, -7);
            _cpsText.Anchor(0.5f, 0.5f); // Center anchor
            _cpsText.Layer(100);
            _cpsText.Color(1.0f, 1.0f, 1.0f);

            _clickButton = UIButton.Create("", -8.0f, -0.0f, 8.0f, 8.0f, 1f, 1f, 1f, 1, 1, false);
            _clickButton.Label.Color(1f, 1f, 1f);
            _clickButton.SetTexture(IdleResources.TexIron);

            _clickButton.Clicked += () =>
            {
                var (mx, my) = Input.GetMousePos();
                game.ClickEffect(new Vec2(mx, my), 1);
                _coins += _clickAmoutPerClick * _clickAmoutPerClickMult;
                UpdateText();
            };


            var store = StoreRegistry.GetAll().ToList();

            float yPos = 8.0f;
            foreach (var item in store)
            {
                var btn = UIButton.Create(string.Format("{0}", item.BaseCost), 08.0f, -0.0f, 7.0f, 4.0f, 1f, 1f, 1f, 2, 1, false);
                _storeButtons.Add(item.Id, btn);
                btn.Label.Color(0.8f, 0.8f, 0.8f);

                if (item.Texture != IntPtr.Zero)
                {
                    btn.SetTexture(item.Texture);
                }

                btn.Clicked += () =>
                {
                    if (_coins >= item.Cost)
                    {
                        // Purchase
                        _coins -= item.Cost;
                        // Update Stats
                        _clickAmoutPerClick += item.ClickAdd;
                        _cps += item.CPS;
                        _clickAmoutPerClickMult += item.ClickMult;
                        // Update Owned
                        item.Owned++;
                        // Recalculate Cost
                        item.Cost = CalcNewPrice(item.BaseCost, item.Owned, 0);

                        UpdateText();
                        btn.SetText(string.Format("{0}", item.Cost));
                    }
                };

                _storeMenu.AddChild(btn, 0, yPos);
                _storeMenu.ContentHeight += 4.5f;
                yPos -= 4.5f;
            }
        }

        public void UpdateText()
        {
            _coinsAmount.Text($"{_coins:F0}");
            _cpsText.Text($"CPS - {_cps:F1}");
            _clickAmountText.Text($"{(_clickAmoutPerClick * _clickAmoutPerClickMult):F0}");
        }


        public int CalcNewPrice(int baseCost, int amountOwned, int freeAmount)
        {
            return (int)Math.Ceiling(baseCost * Math.Pow(1.15, amountOwned - freeAmount));
        }


        public void Exit(IdleGame game) { }


        //used for CPS
        private float secondCounter;
        public void Update(IdleGame game, float dt)
        {
            secondCounter += dt;

            if (secondCounter >= 1.0f)
            {
                _coins += _cps;
                _coinsAmount.Text(string.Format("{0}", (int)_coins));
                secondCounter = 0.0f;
            }
            UISystem.Update();
        }

        public void Draw(IdleGame game) { }
    }
}