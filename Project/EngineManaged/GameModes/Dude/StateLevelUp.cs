using EngineManaged;
using EngineManaged.Numeric;
using EngineManaged.Scene;
using EngineManaged.UI;
using SlimeCore.Core.Grid;
using System;
using System.Collections.Generic;

namespace GameModes.Dude;

public class StateLevelUp : IDudeState
{
	private UIText _upgradeTitle;
	private UIText _subTitle;
	private List<UpgradeCard> _cards = new();

	// Sidebar UI
	private List<UIText> _sidebarText = new();
	private UIText _sidebarHeader;

	private float _animTime = 0f;

	public void Enter(DudeGame game)
	{
		var cardBgSprite = game.CardBgBackdrop.GetComponent<SpriteComponent>();
		cardBgSprite.IsVisible = true;
		cardBgSprite.Color = (0.05f, 0.05f, 0.1f);

		_upgradeTitle = UIText.Create("LEVEL UP!", 1, -10, 7.5f);
		_upgradeTitle.Anchor = (0.5f, 0.5f);
		_upgradeTitle.Color = (1, 1, 0);

		_subTitle = UIText.Create("Select a Mutation", 1, -10, 6.0f);
		_subTitle.Anchor = (0.5f, 0.5f);
		_subTitle.Color = (0.8f, 0.8f, 0.8f);

		CreateSidebar(game);
		SpawnCards(game);
	}

	public void Exit(DudeGame game)
	{
		_upgradeTitle.Destroy();
		_subTitle.Destroy();
		foreach (var c in _cards) c.Destroy();
		_cards.Clear();

		_sidebarHeader.Destroy();
		foreach (var t in _sidebarText) t.Destroy();
		_sidebarText.Clear();

		var cardBgSprite = game.CardBgBackdrop.GetComponent<SpriteComponent>();
		cardBgSprite.IsVisible = false;

		// Reset Bar Visuals
		game.XPBarFill.Size = (0, 0.6f);
		game.XPBarFill.Color = (0.0f, 0.8f, 1.0f);
	}

	public void Update(DudeGame game, float dt)
	{
		_animTime += dt;

		// Title Animation
		float bob = MathF.Sin(_animTime * 2.0f) * 0.2f;
		_upgradeTitle.Position = (-10, 7.5f + bob);

		// Note: Input.GetMousePos returns tuple (float, float), not Vec2, so we keep this as is.
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
			float curY = Ease.Lerp(startY, endY, Ease.OutBack(t));

			// Hover Logic
			bool hovered = t >= 1.0f && card.Contains(mx, my);

			float targetScale = hovered ? 1.15f : 1.0f;
			card.Scale = Ease.Lerp(card.Scale, targetScale, dt * 15f);

			// Apply Transforms
			card.SetPosition(card.BaseX, curY);
			card.SetScale(card.Scale);

			// Click Logic
			if (hovered && mouseDown)
			{
				// Update Stats
				card.Def.Apply(game);

				// Update Counts
				if (!game.UpgradeCounts.ContainsKey(card.Def.Title))
					game.UpgradeCounts[card.Def.Title] = 0;
				game.UpgradeCounts[card.Def.Title]++;

				game.ChangeState(new StatePlaying());
				return;
			}
		}
	}

	private void CreateSidebar(DudeGame game)
	{
		float xPos = 12.5f;
		float yStart = 5.0f;

		_sidebarHeader = UIText.Create("Equiped", 1, xPos, yStart);
		_sidebarHeader.Anchor = (0.5f, 0.5f);
		_sidebarHeader.Color = (0.6f, 1.0f, 0.6f);

		int i = 0;
		foreach (var kvp in game.UpgradeCounts)
		{
			string text = $"{kvp.Key} x{kvp.Value}";
			var ui = UIText.Create(text, 1, xPos, yStart - 1.5f - (i * 1.0f));
			ui.Anchor = (0.5f, 0.5f);
			ui.Color = (1, 1, 1);
			_sidebarText.Add(ui);
			i++;
		}
	}

	private void SpawnCards(DudeGame game)
	{
		float startX = -7.0f;
		float gap = 7.0f;

		for (int i = 0; i < 3; i++)
		{
			// Get from Registry
			var def = ContentRegistry.GetRandomUpgrade(game.Rng);

			float xPos = startX + (i * gap);
			int currentLvl = game.UpgradeCounts.ContainsKey(def.Title) ? game.UpgradeCounts[def.Title] : 0;

			// Pass def to card
			var card = new UpgradeCard(xPos, 0, def, currentLvl + 1);
			_cards.Add(card);
		}
	}

	// --- UPGRADE CARD UI ---
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
		private UIText _lvlLabel;

		private const float W = 6.0f;
		private const float H = 9.0f;

		public UpgradeCard(float x, float y, UpgradeDef def, int nextLevel)
		{
			BaseX = x;
			Def = def;

			_shadow = SceneFactory.CreateQuad(x + 0.3f, y - 0.3f, W, H, 0f, 0f, 0f, layer: 92);
			var shadowTransform = _shadow.GetComponent<TransformComponent>();
			shadowTransform.Anchor = (0.5f, 0.5f);

			_border = SceneFactory.CreateQuad(x, y, W + 0.25f, H + 0.25f, 1f, 1f, 1f, layer: 93);
			var borderTransform = _border.GetComponent<TransformComponent>();
			borderTransform.Anchor = (0.5f, 0.5f);

			_bg = SceneFactory.CreateQuad(x, y, W, H, def.R, def.G, def.B, layer: 94);
			var bgTransform = _bg.GetComponent<TransformComponent>();
			bgTransform.Anchor = (0.5f, 0.5f);

			_title = UIText.Create(def.Title, 1, x, y + 2.5f);
			_title.Anchor = (0.5f, 0.5f); // Center anchor for title
			_title.Layer = 95;

			_desc = UIText.Create(def.Desc, 1, x, y - 0.5f);
			_desc.Anchor = (0.5f, 0.5f);
			_desc.Color = (0.1f, 0.1f, 0.1f);
			_desc.Layer = 95;

			_lvlLabel = UIText.Create($"LVL {nextLevel}", 1, x, y - 3.0f);
			_lvlLabel.Anchor = (0.5f, 0.5f);
			_lvlLabel.Color = (0.1f, 0.1f, 0.1f);
			_lvlLabel.Layer = 95;

			// Start offscreen
			SetPosition(x, -15.0f);
		}

		public void SetPosition(float x, float y)
		{
			var shadowTransform = _shadow.GetComponent<TransformComponent>();
			shadowTransform.Position = (x + 0.3f * Scale, y - 0.3f * Scale);
			var borderTransform = _border.GetComponent<TransformComponent>();
			borderTransform.Position = (x, y);
			var bgTransform = _bg.GetComponent<TransformComponent>();
			bgTransform.Position = (x, y);
			// Text positions are centered at x (since anchor is 0.5, 0.5)
			_title.Position = (x, y + 2.0f * Scale);
			_desc.Position = (x, y - 0.5f * Scale);
			_lvlLabel.Position = (x, y - 3.5f * Scale);
		}

		public void SetScale(float s)
		{
			var shadowTransform = _shadow.GetComponent<TransformComponent>();
			shadowTransform.Scale = (W * s, H * s);
			var borderTransform = _border.GetComponent<TransformComponent>();
			borderTransform.Scale = ((W + 0.25f) * s, (H + 0.25f) * s);
			var bgTransform = _bg.GetComponent<TransformComponent>();
			bgTransform.Scale = (W * s, H * s);
		}

		public bool Contains(float mx, float my)
		{
			var (bx, by) = _bg.GetComponent<TransformComponent>().Position;
			var (w, h) = _bg.GetComponent<TransformComponent>().Scale;
			return (mx > bx - w / 2 && mx < bx + w / 2 && my > by - h / 2 && my < by + h / 2);
		}

		public void Destroy()
		{
			_shadow.Destroy();
			_border.Destroy();
			_bg.Destroy();
			_title.Destroy();
			_desc.Destroy();
			_lvlLabel.Destroy();
		}
	}
}
