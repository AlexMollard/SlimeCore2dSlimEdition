using System;
using System.Collections.Generic;
using EngineManaged.Scene;
using EngineManaged.UI;
using EngineManaged;

namespace GameModes.Dude
{
	public class StateLevelUp : IDudeState
	{
		private UIText _upgradeTitle;
		private UIText _subTitle;
		private List<UpgradeCard> _cards = new();
		private float _animTime = 0f;

		public void Enter(DudeGame game)
		{
			game.CardBgBackdrop.IsVisible = true;
			game.CardBgBackdrop.SetColor(0.05f, 0.05f, 0.1f);

			_upgradeTitle = UIText.Create("LEVEL UP!", 64, 0, 7.5f);
			_upgradeTitle.SetAnchor(0.5f, 0.5f);
			_upgradeTitle.SetColor(1, 1, 0);

			_subTitle = UIText.Create("Select a Mutation", 32, 0, 6.0f);
			_subTitle.SetAnchor(0.5f, 0.5f);
			_subTitle.SetColor(0.8f, 0.8f, 0.8f);

			SpawnCards(game);
		}

		public void Exit(DudeGame game)
		{
			_upgradeTitle.Destroy();
			_subTitle.Destroy();
			foreach (var c in _cards) c.Destroy();
			_cards.Clear();

			game.CardBgBackdrop.IsVisible = false;

			// Reset Bar Visuals
			game.XPBarFill.SetSize(0, 0.6f);
			game.XPBarFill.SetColor(0.0f, 0.8f, 1.0f);
		}

		public void Update(DudeGame game, float dt)
		{
			_animTime += dt;

			float bob = MathF.Sin(_animTime * 2.0f) * 0.2f;
			_upgradeTitle.SetPosition(0, 7.5f + bob);

			var (mx, my) = Input.GetMousePos();
			bool mouseDown = Input.GetMouseDown(Input.MouseButton.Left);

			for (int i = 0; i < _cards.Count; i++)
			{
				var card = _cards[i];

				// Entrance Animation
				float t = MathF.Min(1.0f, (_animTime * 2.0f) - (i * 0.15f));
				if (t < 0) t = 0;

				float startY = -15.0f;
				float endY = 0.0f;
				float curY = Lerp(startY, endY, EaseOutBack(t));

				// Hover Logic
				bool hovered = t >= 1.0f && card.Contains(mx, my);

				float targetScale = hovered ? 1.15f : 1.0f;
				card.Scale = Lerp(card.Scale, targetScale, dt * 15f);

				// Apply Transforms
				card.SetPosition(card.BaseX, curY);
				card.SetScale(card.Scale);

				// Click
				if (hovered && mouseDown)
				{
					card.Def.Action.Invoke();
					game.ChangeState(new StatePlaying());
					return;
				}
			}
		}

		private float Lerp(float a, float b, float t) => a + (b - a) * t;

		private float EaseOutBack(float x)
		{
			float c1 = 1.70158f;
			float c3 = c1 + 1;
			return 1 + c3 * MathF.Pow(x - 1, 3) + c1 * MathF.Pow(x - 1, 2);
		}

		private void SpawnCards(DudeGame game)
		{
			var pool = new List<UpgradeDef> {
				new UpgradeDef("MAGNET", "+20% Pickup Range", 0.4f, 0.7f, 1.0f, () => game.StatMagnetRange *= 1.2f),
				new UpgradeDef("TURBO", "+10% Max Speed", 1.0f, 0.5f, 0.2f, () => game.StatSpeedMult *= 1.1f),
				new UpgradeDef("REFLEX", "-15% Dash Cooldown", 0.2f, 1.0f, 0.6f, () => game.StatDashCooldown *= 0.85f),
				new UpgradeDef("AEGIS", "+1.0s Shield Duration", 0.9f, 0.4f, 1.0f, () => game.StatShieldDuration += 1.0f),
				new UpgradeDef("GREED", "+20% XP Gain", 1.0f, 0.9f, 0.1f, () => game.StatPickupBonus *= 1.2f)
			};

			float startX = -7.0f;
			float gap = 7.0f;

			for (int i = 0; i < 3; i++)
			{
				var def = pool[game.Rng.Next(pool.Count)];
				float xPos = startX + (i * gap);
				var card = new UpgradeCard(xPos, 0, def);
				_cards.Add(card);
			}
		}

		private struct UpgradeDef
		{
			public string Title;
			public string Desc;
			public float R, G, B;
			public Action Action;
			public UpgradeDef(string t, string d, float r, float g, float b, Action a)
			{
				Title = t; Desc = d; R = r; G = g; B = b; Action = a;
			}
		}

		private class UpgradeCard
		{
			public UpgradeDef Def;
			public float BaseX;
			public float Scale = 1.0f;

			private Entity _shadow;
			private Entity _border;
			private Entity _bg;
			private UIText _title;
			private UIText _desc;

			private const float W = 6.0f;
			private const float H = 9.0f;

			public UpgradeCard(float x, float y, UpgradeDef def)
			{
				BaseX = x;
				Def = def;

				_shadow = SceneFactory.CreateQuad(x + 0.3f, y - 0.3f, W, H, 0f, 0f, 0f, layer: 92);
				_shadow.SetAnchor(0.5f, 0.5f);

				_border = SceneFactory.CreateQuad(x, y, W + 0.25f, H + 0.25f, 1f, 1f, 1f, layer: 93);
				_border.SetAnchor(0.5f, 0.5f);

				_bg = SceneFactory.CreateQuad(x, y, W, H, def.R, def.G, def.B, layer: 94);
				_bg.SetAnchor(0.5f, 0.5f);

				_title = UIText.Create(def.Title, 48, x, y + 2.5f);
				_title.SetAnchor(0.5f, 0.5f);
				_title.SetColor(0.1f, 0.1f, 0.1f);
				_title.SetLayer(95);

				_desc = UIText.Create(def.Desc, 28, x, y - 0.5f);
				_desc.SetAnchor(0.5f, 0.5f);
				_desc.SetColor(0.1f, 0.1f, 0.1f);
				_desc.SetLayer(95);

				// --- FIX: Force initial position off-screen immediately ---
				// This prevents the "flash" at Y=0 before the update loop runs
				SetPosition(x, -15.0f);
			}

			public void SetPosition(float x, float y)
			{
				_shadow.SetPosition(x + 0.3f * Scale, y - 0.3f * Scale);
				_border.SetPosition(x, y);
				_bg.SetPosition(x, y);
				_title.SetPosition(x, y + 2.0f * Scale);
				_desc.SetPosition(x, y - 1.0f * Scale);
			}

			public void SetScale(float s)
			{
				_shadow.SetSize(W * s, H * s);
				_border.SetSize((W + 0.25f) * s, (H + 0.25f) * s);
				_bg.SetSize(W * s, H * s);
			}

			public bool Contains(float mx, float my)
			{
				var (bx, by) = _bg.GetPosition();
				var (w, h) = _bg.GetSize();
				return (mx > bx - w / 2 && mx < bx + w / 2 && my > by - h / 2 && my < by + h / 2);
			}

			public void Destroy()
			{
				_shadow.Destroy();
				_border.Destroy();
				_bg.Destroy();
				_title.Destroy();
				_desc.Destroy();
			}
		}
	}
}