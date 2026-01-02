using SlimeCore.GameModes.Factory;
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
		UnsafeNativeMethods.Memory_PushContext("Managed::Init");
        //GameManager.LoadMode(new SnakeGame(new SnakeSettings()));
        //GameManager.LoadMode(new DudeGame());
        GameManager.LoadMode(new FactoryGame(new FactorySettings()));
		UnsafeNativeMethods.Memory_PopContext();
    }

    // The C++ Engine calls this every frame
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Update(float dt)
    {
		UnsafeNativeMethods.Memory_PushContext("Managed::Update");
        // Update Input/UI before game logic
        EngineManaged.UI.UISystem.Update();

        // Update Game
        GameManager.UpdateCurrentMode(dt);
		UnsafeNativeMethods.Memory_PopContext();
    }

    // The C++ Engine calls this every frame during Draw phase
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Draw()
    {
		UnsafeNativeMethods.Memory_PushContext("Managed::Draw");
        GameManager.DrawCurrentMode();
		UnsafeNativeMethods.Memory_PopContext();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void ForceGC()
    {
        GameManager.CloseMode();
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }
}