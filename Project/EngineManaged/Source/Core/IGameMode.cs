using System;

namespace SlimeCore.Source.Core
{
    /// <summary>
    /// Represents a distinct game state or mode (e.g. Main Menu, Snake Level, Platformer Level).
    /// The GameHost uses this interface to drive the logic without knowing the specific class.
    /// </summary>
    public interface IGameMode : IDisposable
    {
        /// <summary>
        /// Called once when the game mode is first loaded.
        /// Use this to spawn entities, setup UI, and initialize variables.
        /// </summary>
        void Init();

        /// <summary>
        /// Called every frame by the host.
        /// </summary>
        /// <param name="dt">Delta time in seconds (time since last frame).</param>
        void Update(float dt);

        /// <summary>
        /// Called when switching to a different mode or closing the engine.
        /// Use this to destroy entities, clear UI, and unsubscribe from events.
        /// </summary>
        void Shutdown();

        public Random Rng { get; set; }
    }
}