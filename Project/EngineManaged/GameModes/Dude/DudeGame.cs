using System;
using System.Collections.Generic;
using SlimeCore.Interfaces;
using EngineManaged.Scene;
using EngineManaged.UI;
using EngineManaged.Numeric;

namespace GameModes.Dude;

	// --- SHARED DATA STRUCTURES ---
	internal enum HaterType { Normal, Chonker }
	internal class Hater { public Entity Ent; public Vec2 Pos; public HaterType Type; }
	internal class Collectable { public Entity Ent; public Vec2 Pos; public PowerupDef Definition; }
	internal class GhostTrail { public Entity Ent; public float Alpha; public float InitW; public float InitH; }

	// Refactored to use Vec2 for position and velocity
	internal class Particle { public Entity Ent; public Vec2 Pos; public Vec2 Vel; public float Life; public float InitSize; }
	internal class XPGem { public Entity Ent; public Vec2 Pos; public int Value; }

// --- GAME CONTEXT ---
public class DudeGame : IGameMode
{
	// State Machine
	private IDudeState _currentState;

	// Shared Data
	internal Entity Dude;
	internal Vec2 DudePos;
	internal Vec2 DudeVel;
	internal Entity Bg;
	internal Entity DarkOverlay;
	internal Entity CardBgBackdrop;

		// --- STATS CONTAINER ---
		internal DudeStats Stats = new DudeStats();

		// --- EVENT SYSTEM ---
		internal GameEvents Events = new GameEvents();

	// Core Values
	internal int Level;
	internal float XP;
	internal float XPToNextLevel;
	internal float Score;
	internal float TimeAlive;

	// Track upgrade counts for UI
	internal Dictionary<string, int> UpgradeCounts = new();

	// Timers
	internal float DashTimer;
	internal float ShieldTimer;
	internal float ChillTimer;
	internal float SpawnTimer;
	internal float DiscoTimer;
	internal float ShakeAmount;

	// Lists
	internal List<Hater> Haters = new();
	internal List<Collectable> Collectables = new();
	internal List<XPGem> Gems = new();
	internal List<GhostTrail> Trails = new();
	internal List<Particle> Particles = new();

	// UI
	internal UIText ScoreText;
	internal UIText LevelText;
	internal Entity XPBarFill;
	internal Entity XPBarBg;

	internal Random Rng = new Random();

		public void Init()
		{
			// 1. Initialize Content Registry
			ContentRegistry.Init();

		// 2. Reset Data
		Level = 1;
		XP = 0;
		XPToNextLevel = 100;
		Score = 0;
		TimeAlive = 0;
		DudePos = Vec2.Zero;
		DudeVel = Vec2.Zero;

		Stats.Reset();
		Events.Clear();
		UpgradeCounts.Clear();

		// 3. Create Entities
		Bg = SceneFactory.CreateQuad(0, 0, 100, 100, 0.05f, 0.05f, 0.1f, layer: -10);
		Bg.SetAnchor(0.5f, 0.5f);

		DarkOverlay = SceneFactory.CreateQuad(0, 0, 100, 100, 0f, 0f, 0f, layer: 90);
		DarkOverlay.SetAnchor(0.5f, 0.5f);
		DarkOverlay.IsVisible = false;

		CardBgBackdrop = SceneFactory.CreateQuad(0, 0, 100, 100, 0.1f, 0.1f, 0.1f, layer: 91);
		CardBgBackdrop.SetAnchor(0.5f, 0.5f);
		CardBgBackdrop.IsVisible = false;

		Dude = SceneFactory.CreateQuad(0, 0, 0.9f, 0.9f, 0.2f, 1.0f, 0.2f, layer: 20);
		Dude.SetAnchor(0.5f, 0.5f);

		// XP Bar
		XPBarBg = SceneFactory.CreateQuad(0, -7.0f, 28.0f, 0.6f, 0.2f, 0.2f, 0.2f, layer: 80);
		XPBarBg.SetAnchor(0.5f, 0.5f);
		XPBarFill = SceneFactory.CreateQuad(0.0f, -7.0f, 0f, 0.6f, 0.0f, 0.8f, 1.0f, layer: 81);
		XPBarFill.SetAnchor(0.0f, 0.5f);

		// UI
		ScoreText = UIText.Create("0", 48, -13.5f, 7.5f);
		LevelText = UIText.Create("LVL 1", 32, -13.0f, -6.25f);

		// 4. Start Gameplay
		ChangeState(new StatePlaying());
	}

	public void ChangeState(IDudeState newState)
	{
		if (_currentState != null) _currentState.Exit(this);
		_currentState = newState;
		if (_currentState != null) _currentState.Enter(this);
	}

	public void Update(float dt)
	{
		if (_currentState != null) _currentState.Update(this, dt);
	}

	public void Shutdown()
	{
		if (_currentState != null) _currentState.Exit(this);

		Bg.Destroy();
		DarkOverlay.Destroy();
		CardBgBackdrop.Destroy();
		Dude.Destroy();
		XPBarBg.Destroy();
		XPBarFill.Destroy();
		ScoreText.Destroy();
		LevelText.Destroy();

		foreach (var h in Haters) h.Ent.Destroy(); Haters.Clear();
		foreach (var c in Collectables) c.Ent.Destroy(); Collectables.Clear();
		foreach (var g in Gems) g.Ent.Destroy(); Gems.Clear();
		foreach (var t in Trails) t.Ent.Destroy(); Trails.Clear();
		foreach (var p in Particles) p.Ent.Destroy(); Particles.Clear();

		Events.Clear();
		UpgradeCounts.Clear();
	}

		internal void SpawnExplosion(Vec2 pos, int count, float r, float g, float b)
		{
			for (int i = 0; i < count; i++)
			{
				float size = (float)Rng.NextDouble() * 0.4f + 0.1f;
				float angle = (float)Rng.NextDouble() * 6.28f;
				float speed = (float)Rng.NextDouble() * 8.0f + 2.0f;

				Vec2 vel = new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

				var ent = SceneFactory.CreateQuad(pos.X, pos.Y, size, size, r, g, b, layer: 30);
				ent.SetAnchor(0.5f, 0.5f);
				Particles.Add(new Particle { Ent = ent, Pos = pos, Vel = vel, Life = 1.0f, InitSize = size });
			}
		}

		// --- STAT ACCESSORS ---
		internal float StatMagnetRange { get => Stats.MagnetRange; set => Stats.MagnetRange = value; }
		internal float StatSpeedMult { get => Stats.SpeedMult; set => Stats.SpeedMult = value; }
		internal float StatDashCooldown { get => Stats.DashCooldown; set => Stats.DashCooldown = value; }
		internal float StatShieldDuration { get => Stats.ShieldDuration; set => Stats.ShieldDuration = value; }
		internal float StatPickupBonus { get => Stats.PickupBonus; set => Stats.PickupBonus = value; }
		internal float StatAccelMult { get => Stats.AccelMult; set => Stats.AccelMult = value; }
		internal float StatChillDuration { get => Stats.ChillDuration; set => Stats.ChillDuration = value; }
		internal float StatLuck { get => Stats.Luck; set => Stats.Luck = value; }
		internal float StatSize { get => Stats.PlayerSize; set => Stats.PlayerSize = value; }
	}

	// --- STAT ACCESSORS (Used by upgrade lambdas) ---
	internal float StatMagnetRange { get => Stats.MagnetRange; set => Stats.MagnetRange = value; }
	internal float StatSpeedMult { get => Stats.SpeedMult; set => Stats.SpeedMult = value; }
	internal float StatDashCooldown { get => Stats.DashCooldown; set => Stats.DashCooldown = value; }
	internal float StatShieldDuration { get => Stats.ShieldDuration; set => Stats.ShieldDuration = value; }
	internal float StatPickupBonus { get => Stats.PickupBonus; set => Stats.PickupBonus = value; }
	internal float StatAccelMult { get => Stats.AccelMult; set => Stats.AccelMult = value; }
	internal float StatChillDuration { get => Stats.ChillDuration; set => Stats.ChillDuration = value; }
	internal float StatLuck { get => Stats.Luck; set => Stats.Luck = value; }
	internal float StatSize { get => Stats.PlayerSize; set => Stats.PlayerSize = value; }
}

public interface IDudeState
{
	void Enter(DudeGame game);
	void Update(DudeGame game, float dt);
	void Exit(DudeGame game);
}