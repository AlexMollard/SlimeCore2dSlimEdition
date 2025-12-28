using GameModes.Dude;
using SlimeCore.GameModes.Snake;
using SlimeCore.Source.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SlimeCore;

public static class GameHost
{
    // The C++ Engine calls this once on startup
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init()
    {
        GameManager.LoadMode(new SnakeGame(new SnakeSettings()));
        //GameManager.LoadMode(new DudeGame());
    }

    // The C++ Engine calls this every frame
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Update(float dt)
    {
        // Update Input/UI before game logic
        EngineManaged.UI.UISystem.Update();

        // Update Game
        GameManager.UpdateCurrentMode(dt);
    }
}