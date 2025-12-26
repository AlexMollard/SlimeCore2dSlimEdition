using System;
using EngineManaged;
using EngineManaged.Scene;
using EngineManaged.UI;
using EngineManaged.Numeric;
using EngineManaged.Rendering;
using SlimeCore.Source.Input;
using GameModes.Dude;
using SlimeCore.Source.Core;

namespace SlimeCore.GameModes.Dude.States;

public class StatePlaying : IGameState<DudeGame>
{
    private UIText _controlsText;

    public void Enter(DudeGame game)
    {
        var darkSprite = game.DarkOverlay.GetComponent<SpriteComponent>();
        darkSprite.IsVisible = false;
        var cardSprite = game.CardBgBackdrop.GetComponent<SpriteComponent>();
        cardSprite.IsVisible = false;

        game.ScoreText.IsVisible(true);
        game.LevelText.IsVisible(true);
        game.XPBarBg.IsVisible(true);
        game.XPBarFill.IsVisible(true);

        _controlsText = UIText.Create("WASD Move | SPACE Dash", 1, 0, -8.0f);
        _controlsText.Anchor(0.5f, 0.5f); // Center anchor
        _controlsText.Layer(100);
    }

    public void Exit(DudeGame game)
    {
        _controlsText.Destroy();
    }

    public void Update(DudeGame game, float dt)
    {
        // 1. EVENT HOOK
        game.Events.OnUpdate?.Invoke(game, dt);

        UpdateDiscoLights(game, dt);
        UpdateShake(game, dt);
        UpdateTrails(game, dt);
        UpdateAmbientParticles(game, dt);

        game.TimeAlive += dt;
        game.Score += dt * 10;
        game.ScoreText.Text($"{(int)game.Score}");

        UpdateXPBar(game, dt);
        UpdateTimers(game, dt);
        HandleInput(game, dt);
        MoveDude(game, dt);
        HandleHaters(game, dt);
        HandleCollectables(game, dt);
        HandleGems(game, dt);
    }

    private void UpdateAmbientParticles(DudeGame game, float dt)
    {
        if (game.Rng.NextDouble() < 0.1)
        {
            var props = new ParticleProps();
            props.Position = new Vec2((float)(game.Rng.NextDouble() * 30 - 15), (float)(game.Rng.NextDouble() * 20 - 10));
            props.Velocity = new Vec2(0, 0.5f);
            props.VelocityVariation = new Vec2(0.2f, 0.2f);
            props.ColorBegin = new Color(1f, 1f, 1f, 0.2f);
            props.ColorEnd = new Color(1f, 1f, 1f, 0.0f);
            props.SizeBegin = 0.1f;
            props.SizeEnd = 0.0f;
            props.LifeTime = 3.0f;
            game.ParticleSys.Emit(props);
        }
    }

    private void UpdateXPBar(DudeGame game, float dt)
    {
        var targetWidth = (game.XP / game.XPToNextLevel) * 28.0f;
        if (targetWidth > 28) targetWidth = 28;
        var (curW, curH) = game.XPBarFill.Size;
        var newW = Ease.Lerp(curW, targetWidth, 5.0f * dt);
        game.XPBarFill.Size = (newW, 0.6f);
    }

    private void UpdateTimers(DudeGame game, float dt)
    {
        if (game.DashTimer > 0) game.DashTimer -= dt;

        if (game.ChillTimer > 0)
        {
            game.ChillTimer -= dt;
            var bgSprite = game.Bg.GetComponent<SpriteComponent>();
            bgSprite.Color = (0.1f, 0.3f, 0.4f);
        }

        if (game.ShieldTimer > 0)
        {
            game.ShieldTimer -= dt;
            var flash = 0.5f + 0.5f * MathF.Sin(game.TimeAlive * 25);
            var dudeSprite = game.Dude.GetComponent<SpriteComponent>();
            dudeSprite.Color = (flash, flash, flash);
            dudeSprite.IsVisible = game.ShieldTimer > 1.0f || (int)(game.TimeAlive * 15) % 2 == 0;
        }
        else
        {
            var dudeSprite = game.Dude.GetComponent<SpriteComponent>();
            dudeSprite.Color = (0.2f, 1.0f, 0.2f);
            dudeSprite.IsVisible = true;
        }
    }

    private void HandleInput(DudeGame game, float dt)
    {
        var input = Vec2.Zero;
        if (Input.GetKeyDown(Keycode.W)) input.Y += 1;
        if (Input.GetKeyDown(Keycode.S)) input.Y -= 1;
        if (Input.GetKeyDown(Keycode.A)) input.X -= 1;
        if (Input.GetKeyDown(Keycode.D)) input.X += 1;

        if (input.LengthSquared() > 0) input = input.Normalized();

        // Physics Movement
        var rb = game.Dude.GetComponent<RigidBodyComponent>();
        var (vx, vy) = rb.Velocity;
        var currentVel = new Vec2(vx, vy);

        // Apply Acceleration
        var targetVel = input * (game.Stats.MoveSpeed * game.Stats.SpeedMult);
        var newVel = Vec2.Lerp(currentVel, targetVel, 10.0f * dt); // Smooth acceleration

        if (Input.GetKeyDown(Keycode.SPACE) && game.DashTimer <= 0)
        {
            // 2. EVENT HOOK
            game.Events.OnDash?.Invoke(game);

            // Use vector arithmetic
            var dashDir = input.LengthSquared() > 0 ? input : new Vec2(1, 0);
            newVel += dashDir * 40.0f;

            game.DashTimer = game.Stats.DashCooldown;
            game.ShakeAmount = 0.3f;
            SpawnTrail(game, 1.0f);
        }

        rb.Velocity = (newVel.X, newVel.Y);
        game.DudeVel = newVel; // Sync for other logic
    }

    private void MoveDude(DudeGame game, float dt)
    {
        // Sync Position from Physics Engine
        var t = game.Dude.GetComponent<TransformComponent>();
        var (px, py) = t.Position;
        game.DudePos = new Vec2(px, py);

        // Rotate towards movement direction
        if (game.DudeVel.LengthSquared() > 0.1f)
        {
            var angle = MathF.Atan2(game.DudeVel.Y, game.DudeVel.X) * (180.0f / MathF.PI);
            t.Rotation = angle;
        }

        // Squash & Stretch
        var speed = game.DudeVel.Length();
        var stretch = 1.0f + (speed * 0.02f);
        var squash = 1.0f / stretch;
        var baseScale = 0.9f * game.Stats.PlayerSize;

        if (MathF.Abs(game.DudeVel.X) > MathF.Abs(game.DudeVel.Y))
            t.Scale = (stretch * baseScale, squash * baseScale);
        else
            t.Scale = (squash * baseScale, stretch * baseScale);

        // Trail Logic
        if (speed > 15.0f)
        {
            game.TrailTimer -= dt;
            if (game.TrailTimer <= 0)
            {
                SpawnTrail(game, 0.6f);
                game.TrailTimer = 0.05f; // Spawn every 50ms
            }
        }
    }

    private void HandleHaters(DudeGame game, float dt)
    {
        game.SpawnTimer -= dt;
        if (game.SpawnTimer <= 0)
        {
            SpawnHater(game);
            var ramp = MathF.Min(1.5f, game.TimeAlive * 0.02f);
            game.SpawnTimer = (1.2f - ramp) / (game.ChillTimer > 0 ? 0.5f : 1.0f);
            if (game.SpawnTimer < 0.2f) game.SpawnTimer = 0.2f;
        }

        var timeScale = game.ChillTimer > 0 ? 0.3f : 1.0f;

        for (var i = game.Haters.Count - 1; i >= 0; i--)
        {
            var me = game.Haters[i];

            // Vectorized Direction
            var toPlayer = (game.DudePos - me.Pos).Normalized();

            var separation = Vec2.Zero;
            var neighbors = 0;
            var myRad = me.Type == HaterType.Chonker ? 2.5f : 1.2f;

            foreach (var other in game.Haters)
            {
                if (me == other) continue;

                var diff = me.Pos - other.Pos;
                var distSq = diff.LengthSquared();

                if (distSq < myRad * myRad && distSq > 0.001f)
                {
                    var pushStrength = me.Type == HaterType.Chonker ? 8.0f : 4.0f;
                    separation += diff.Normalized() * pushStrength;
                    neighbors++;
                }
            }

            var force = toPlayer * 2.0f;
            if (neighbors > 0)
            {
                force += separation * 1.5f;
            }

            var speed = (me.Type == HaterType.Chonker ? 2.0f : 4.5f) * timeScale;
            me.Pos += force.Normalized() * speed * dt;

            var haterTransform = me.Ent.GetComponent<TransformComponent>();
            haterTransform.Position = (me.Pos.X, me.Pos.Y);

            var pulseSpeed = me.Type == HaterType.Chonker ? 5.0f : 15.0f;
            var baseSize = me.Type == HaterType.Chonker ? 2.2f : 0.8f;
            var pulse = baseSize + 0.1f * MathF.Sin(game.TimeAlive * pulseSpeed + i);
            haterTransform.Scale = (pulse, pulse);

            // Distance check with Vector method
            // Increased kill distance slightly to account for physics collision radius preventing overlap
            var killDist = (me.Type == HaterType.Chonker ? 1.5f : 0.7f) * game.Stats.PlayerSize * 1.3f;

            if (Vec2.Distance(game.DudePos, me.Pos) < killDist)
            {
                if (game.ShieldTimer > 0)
                {
                    game.Events.OnEnemyKilled?.Invoke(game, me.Pos);
                    SpawnGem(game, me.Pos, me.Type == HaterType.Chonker ? 50 : 10);
                    me.Ent.Destroy();
                    game.Haters.RemoveAt(i);
                    game.Score += 100;
                    game.ShakeAmount += 0.3f;
                    game.SpawnExplosion(me.Pos, 10, 1f, 0f, 0f);
                }
                else
                {
                    game.Events.OnDeath?.Invoke(game);
                    game.ChangeState(new StateGameOver());
                }
            }
        }
    }

    private void HandleCollectables(DudeGame game, float dt)
    {
        // Use Registry + Luck Stat
        var def = ContentRegistry.GetRandomPowerup(game.Rng, game.Stats.Luck);
        if (def != null) SpawnCollectable(game, def);

        for (var i = game.Collectables.Count - 1; i >= 0; i--)
        {
            var c = game.Collectables[i];
            var distSq = Vec2.Distance(game.DudePos, c.Pos); // Distance is actual float dist, not squared.

            // Use Stat: Magnet Range
            if (distSq < game.Stats.MagnetRange)
            {
                var dir = (game.DudePos - c.Pos).Normalized();
                c.Pos += dir * 10.0f * dt;
            }

            var collectableTransform = c.Ent.GetComponent<TransformComponent>();
            collectableTransform.Position = (c.Pos.X, c.Pos.Y);
            collectableTransform.Scale = (0.6f + 0.1f * MathF.Sin(game.TimeAlive * 8), 0.6f);

            // Use Stat: Player Size for pickup radius
            // Use LengthSquared for cheaper check if not needed
            if ((game.DudePos - c.Pos).LengthSquared() < 1.0f * game.Stats.PlayerSize)
            {
                c.Definition.OnPickup(game, c.Pos);
                game.ShakeAmount += 0.1f;
                game.SpawnExplosion(c.Pos, 8, c.Definition.R, c.Definition.G, c.Definition.B);
                c.Ent.Destroy();
                game.Collectables.RemoveAt(i);
                CheckLevelUp(game);
            }
        }
    }

    private void HandleGems(DudeGame game, float dt)
    {
        for (var i = game.Gems.Count - 1; i >= 0; i--)
        {
            var g = game.Gems[i];
            var distSq = (game.DudePos - g.Pos).LengthSquared();

            if (distSq < game.Stats.MagnetRange * game.Stats.MagnetRange)
            {
                g.Pos += (game.DudePos - g.Pos).Normalized() * 18.0f * dt;
            }
            var gemTransform = g.Ent.GetComponent<TransformComponent>();
            gemTransform.Position = (g.Pos.X, g.Pos.Y);

            if (distSq < 0.5f * game.Stats.PlayerSize)
            {
                game.XP += g.Value * game.Stats.PickupBonus;
                g.Ent.Destroy();
                game.Gems.RemoveAt(i);
                CheckLevelUp(game);
            }
        }
    }

    private void CheckLevelUp(DudeGame game)
    {
        if (game.XP >= game.XPToNextLevel)
        {
            game.XP -= game.XPToNextLevel;
            game.Level++;
            game.XPToNextLevel *= 1.35f;
            game.LevelText.Text($"LVL {game.Level}");
            game.XPBarFill.Size = (28.0f, 0.6f);
            game.ChangeState(new StateLevelUp());
        }
    }

    // --- SPAWNERS ---

    private void SpawnTrail(DudeGame game, float alphaStart)
    {
        // Get Player Visuals
        var playerSprite = game.Dude.GetComponent<SpriteComponent>();
        var (r, g, b) = playerSprite.Color;

        var ghost = game.CreateSpriteEntity(game.DudePos.X, game.DudePos.Y, 0.9f, 0.9f, r, g, b, 15, game.TexPlayer);
        var ghostTransform = ghost.GetComponent<TransformComponent>();
        ghostTransform.Anchor = (0.5f, 0.5f);

        // Copy Rotation & Scale
        var playerTransform = game.Dude.GetComponent<TransformComponent>();
        ghostTransform.Rotation = playerTransform.Rotation;
        ghostTransform.Scale = playerTransform.Scale;

        // Set Initial Alpha
        var ghostSprite = ghost.GetComponent<SpriteComponent>();
        ghostSprite.Alpha = alphaStart;

        // Copy Animation Frame (if any)
        var playerAnim = game.Dude.GetComponent<AnimationComponent>();
        var ghostAnim = ghost.GetComponent<AnimationComponent>();
        ghostAnim.Frame = playerAnim.Frame;
        ghostAnim.SpriteWidth = playerAnim.SpriteWidth;

        var (w, h) = playerTransform.Scale;
        game.Trails.Add(new GhostTrail { Ent = ghost, Alpha = alphaStart, InitW = w, InitH = h });
    }

    private void SpawnHater(DudeGame game)
    {
        var type = game.Rng.NextDouble() < 0.10 ? HaterType.Chonker : HaterType.Normal;
        Vec2 pos;
        if (game.Rng.Next(2) == 0) { pos.X = game.Rng.Next(2) == 0 ? -16 : 16; pos.Y = (game.Rng.NextSingle() * 20) - 10; }
        else { pos.X = (game.Rng.NextSingle() * 28) - 14; pos.Y = game.Rng.Next(2) == 0 ? -11 : 11; }
        var size = type == HaterType.Chonker ? 2.2f : 0.8f;
        var r = type == HaterType.Chonker ? 0.6f : 1.0f;
        var b = type == HaterType.Chonker ? 0.8f : 0.2f;
        var ent = game.CreateSpriteEntity(pos.X, pos.Y, size, size, r, 0f, b, 5, game.TexEnemy);

        // Add Physics (Kinematic so we can control position manually but still collide with player)
        ent.AddComponent<RigidBodyComponent>();
        ent.AddComponent<BoxColliderComponent>();
        var rb = ent.GetComponent<RigidBodyComponent>();
        rb.IsKinematic = true;
        var bc = ent.GetComponent<BoxColliderComponent>();
        bc.Size = (size * 0.8f, size * 0.8f); // Slightly smaller hitbox

        var haterTransform = ent.GetComponent<TransformComponent>();
        haterTransform.Anchor = (0.5f, 0.5f);
        game.Haters.Add(new Hater { Ent = ent, Pos = pos, Type = type });
    }

    private void SpawnCollectable(DudeGame game, PowerupDef def)
    {
        var pos = new Vec2((game.Rng.NextSingle() * 24) - 12, (game.Rng.NextSingle() * 14) - 7);
        var ent = game.CreateSpriteEntity(pos.X, pos.Y, 0.6f, 0.6f, def.R, def.G, def.B, 8, game.TexParticle);
        var collectableTransform = ent.GetComponent<TransformComponent>();
        collectableTransform.Anchor = (0.5f, 0.5f);
        game.Collectables.Add(new Collectable { Ent = ent, Pos = pos, Definition = def });
    }

    private void SpawnGem(DudeGame game, Vec2 pos, int value)
    {
        var r = value > 10 ? 0.2f : 0.4f;
        var g = value > 10 ? 0.2f : 1.0f;
        var b = value > 10 ? 1.0f : 0.8f;
        var size = value > 10 ? 0.45f : 0.3f;
        var ent = game.CreateSpriteEntity(pos.X, pos.Y, size, size, r, g, b, 9, game.TexParticle);
        var gemTransform = ent.GetComponent<TransformComponent>();
        gemTransform.Anchor = (0.5f, 0.5f);
        game.Gems.Add(new XPGem { Ent = ent, Pos = pos, Value = value });
    }

    // --- EFFECTS ---

    private void UpdateTrails(DudeGame game, float dt)
    {
        for (var i = game.Trails.Count - 1; i >= 0; i--)
        {
            var t = game.Trails[i];
            t.Alpha -= dt * 2.0f; // Slower fade for smoother look
            if (t.Alpha <= 0) { t.Ent.Destroy(); game.Trails.RemoveAt(i); }
            else
            {
                var sprite = t.Ent.GetComponent<SpriteComponent>();
                sprite.Alpha = t.Alpha;

                // Optional: Tint it slightly green/blue as it fades?
                // sprite.Color = (0.5f, 1.0f, 0.5f); 

                // Expand slightly as it dissipates (smoke effect)
                var transform = t.Ent.GetComponent<TransformComponent>();
                var expansion = 1.0f + (1.0f - t.Alpha) * 0.2f;
                transform.Scale = (t.InitW * expansion, t.InitH * expansion);
            }
        }
    }

    private void UpdateShake(DudeGame game, float dt)
    {
        if (game.ShakeAmount > 0) { game.ShakeAmount -= dt * 2.0f; if (game.ShakeAmount < 0) game.ShakeAmount = 0; }

        // Apply shake to Camera
        if (game.Camera.IsAlive)
        {
            var t = game.Camera.GetComponent<TransformComponent>();
            if (game.ShakeAmount > 0)
            {
                var shakeX = (float)(game.Rng.NextDouble() - 0.5) * game.ShakeAmount;
                var shakeY = (float)(game.Rng.NextDouble() - 0.5) * game.ShakeAmount;
                t.Position = (shakeX, shakeY);
            }
            else
            {
                t.Position = (0, 0);
            }
        }
    }

    private void UpdateDiscoLights(DudeGame game, float dt)
    {
        game.DiscoTimer += dt;
        if (game.ChillTimer > 0) return;
        var r = 0.1f + 0.05f * MathF.Sin(game.DiscoTimer * 2f);
        var g = 0.1f + 0.05f * MathF.Sin(game.DiscoTimer * 1.5f);
        var b = 0.15f + 0.05f * MathF.Sin(game.DiscoTimer * 0.8f);
        var bgSprite = game.Bg.GetComponent<SpriteComponent>();
        bgSprite.Color = (r, g, b);
    }
}