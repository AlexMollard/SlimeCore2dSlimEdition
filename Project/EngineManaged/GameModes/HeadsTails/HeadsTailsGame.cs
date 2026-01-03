using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.UI;
using SlimeCore.GameModes.Dude.States;
using SlimeCore.GameModes.Test;
using SlimeCore.GameModes.Test.States;
using SlimeCore.Source.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GameModes.Test;

public sealed class HeadsTailsGame : GameMode<HeadsTailsGame>, IGameMode, IDisposable
{
    private bool _isDisposed;
    public override Random? Rng { get; set; } = new();


    public override void Init()
    {
        ChangeState(new StateHeadsTailsPlaying());

    }
    public override void Shutdown()
    {
        UISystem.Clear();
    }

    public override void Update(float dt)
    {

    }
    public void InitializeGame()
    {

    }

    public override bool InView(Vec2 position)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
    }

    ~HeadsTailsGame()
    {
        Dispose(false);
    }
}