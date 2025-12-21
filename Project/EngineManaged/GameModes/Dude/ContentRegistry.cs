using System;
using System.Collections.Generic;

namespace GameModes.Dude
{
	// --- DEFINITIONS ---
	public class UpgradeDef
	{
		public string Id;
		public string Title;
		public string Desc;
		public float R, G, B;

		// The logic to run when you pick the card
		public Action<DudeGame> Apply;

		// Parameterless ctor to support object-initializer usage
		public UpgradeDef() { }

		public UpgradeDef(string t, string d, float r, float g, float b, Action<DudeGame> a)
		{
			Title = t; Desc = d; R = r; G = g; B = b; Apply = a;
		}
	}

	public class PowerupDef
	{
		public string Id;
		public float SpawnWeight; // Higher = more common
		public float R, G, B;     // Color
		public Action<DudeGame, Vec2> OnPickup; // What happens when touched
	}

	// --- THE REGISTRY ---
	public static class ContentRegistry
	{
		public static List<UpgradeDef> Upgrades = new();
		public static List<PowerupDef> Powerups = new();

		public static void Init()
		{
			Upgrades.Clear();
			Powerups.Clear();

			// ==========================================
			// 1. DEFINE UPGRADES
			// ==========================================

			// Simple Stat Upgrades
			RegisterUpgrade("MAGNET", "Range +20%", 0.4f, 0.7f, 1.0f, g => g.Stats.PickupRange *= 1.2f);
			RegisterUpgrade("TURBO", "Speed +10%", 1.0f, 0.5f, 0.2f, g => g.Stats.MoveSpeed *= 1.1f);

			RegisterUpgrade("BOOM", "Kills explode", 1.0f, 0.2f, 0.2f, g =>
			{
				// Hook into the event system
				g.Events.OnEnemyKilled += (game, pos) =>
				{
					// 20% chance to chain reaction
					if (game.Rng.NextDouble() < 0.2)
					{
						game.SpawnExplosion(pos, 10, 1f, 0.5f, 0f);
						// Logic to kill nearby enemies could go here too!
					}
				};
			});

			RegisterUpgrade("TRAIL", "Leaving fire", 1.0f, 0.5f, 0.0f, g =>
			{
				g.Events.OnUpdate += (game, dt) =>
				{
					// Spawn a red particle every frame
					if (game.Rng.NextDouble() < 0.3)
					{
						game.SpawnExplosion(game.DudePos, 1, 1f, 0f, 0f);
					}
				};
			});

			// ==========================================
			// 2. DEFINE POWERUPS
			// ==========================================

			RegisterPowerup("COFFEE", 0.005f, 0.6f, 0.4f, 0.2f, (g, pos) => {
				g.Score += 250;
				g.DudeVel *= 1.2f;
				g.XP += 20 * g.Stats.XPMultiplier;
			});

			RegisterPowerup("SHIELD", 0.001f, 1.0f, 0.5f, 0.8f, (g, pos) => {
				g.ShieldTimer = g.Stats.ShieldDuration;
				g.Score += 100;
			});

			RegisterPowerup("BOMB", 0.0005f, 0.2f, 0.2f, 0.2f, (g, pos) => {
				// Wipe all enemies
				foreach (var h in g.Haters)
				{
					g.SpawnExplosion(h.Pos, 5, 1, 1, 1);
					h.Ent.Destroy();
				}
				g.Haters.Clear();
				g.ShakeAmount += 1.0f;
			});
		}

		// Helpers
		private static void RegisterUpgrade(string title, string desc, float r, float g, float b, Action<DudeGame> apply)
		{
			Upgrades.Add(new UpgradeDef { Id = title, Title = title, Desc = desc, R = r, G = g, B = b, Apply = apply });
		}

		private static void RegisterPowerup(string id, float weight, float r, float g, float b, Action<DudeGame, Vec2> onPickup)
		{
			Powerups.Add(new PowerupDef { Id = id, SpawnWeight = weight, R = r, G = g, B = b, OnPickup = onPickup });
		}

		public static UpgradeDef GetRandomUpgrade(Random rng) => Upgrades[rng.Next(Upgrades.Count)];

		// Weighted Random for Powerups
		public static PowerupDef GetRandomPowerup(Random rng, float luck)
		{
			// Simple approach: try to find one based on weight * luck
			foreach (var p in Powerups)
			{
				if (rng.NextDouble() < p.SpawnWeight * luck) return p;
			}
			return null; // Nothing spawned
		}
	}
}