using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Interfaces;

internal interface IGameMode
{
    /// <summary>
    /// Called once within ScriptRuntime.cs
    /// </summary>
    public abstract static void Init();
    /// <summary>
    /// This is called every frame within ScriptRuntime.cs
    /// </summary>
    /// <param name="deltaTime">The time between two frames</param>
    public abstract static void Update(float deltaTime);
    /// <summary>
    /// This is called once when the game is shutting down from ScriptRuntime.cs
    /// </summary>
    public abstract static void Shutdown();
}
