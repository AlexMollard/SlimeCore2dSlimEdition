using System;

namespace SlimeCore.Source.Core;

public static class GameManager
{
    private static IGameMode? _currentMode;

    public static void LoadMode(IGameMode newMode)
    {
        CloseMode();
        _currentMode = newMode;
        _currentMode.Init();
    }

    public static void UpdateCurrentMode(float dt)
    {
        if (_currentMode != null)
        {
            _currentMode.Update(dt);
        }
    }

    /// <summary>
    /// Closes the currently active mode, releasing any associated resources.
    /// </summary>
    /// <remarks>If no mode is currently active, this method performs no action. After calling this method,
    /// the current mode is set to null and cannot be used unless reinitialized.</remarks>
    public static void CloseMode()
    {
        if (_currentMode != null)
        {
            _currentMode.Shutdown();
            if (_currentMode is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _currentMode = null;
        }
    }
}