using System;

namespace GameModes.Dude;

public class GameEvents
{
	// Called every frame (for active effects like trails or damaging auras)
	public Action<DudeGame, float> OnUpdate;

	// Called when an enemy dies. Passes the position of death.
	public Action<DudeGame, Vec2> OnEnemyKilled;

	// Called when you dash
	public Action<DudeGame> OnDash;

	// Called when you take damage (if we add HP later) or Game Over
	public Action<DudeGame> OnDeath;

	public void Clear()
	{
		OnUpdate = null;
		OnEnemyKilled = null;
		OnDash = null;
		OnDeath = null;
	}
}