using SlimeCore.Interfaces;

namespace SlimeCore.Source.Core;

public static class GameManager
{
    private static IGameMode? _currentMode;

    public static void LoadMode(IGameMode newMode)
    {
        if (_currentMode != null)
            _currentMode.Shutdown();

        _currentMode = newMode;
        _currentMode.Init();
    }

    public static void UpdateCurrentMode(float dt)
    {
        if (_currentMode != null)
            _currentMode.Update(dt);
    }
}