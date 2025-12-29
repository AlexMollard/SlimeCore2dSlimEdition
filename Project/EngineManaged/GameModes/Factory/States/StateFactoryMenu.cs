using EngineManaged.UI;
using SlimeCore.Source.Core;

namespace SlimeCore.GameModes.Factory.States;

public class StateFactoryMenu : IGameState<FactoryGame>
{
    private UIText _gameLabel;
    private UIButton? _startBtn;

    public void Enter(FactoryGame game)
    {
        _gameLabel = UIText.Create("FACTORY GAME", 2, 0, 6.0f);
        _gameLabel.Color(1.0f, 1.0f, 1.0f);
        _gameLabel.Anchor(0.5f, 0.5f);

        _startBtn = UIButton.Create("PLAY", 0, 0, 8.0f, 1.5f, 0.5f, 0.5f, 0.5f, layer: 96, fontSize: 1);
        _startBtn.Label.Color(1.0f, 1.0f, 1.0f);

        _startBtn.Clicked += () =>
        {
            game.ChangeState(new StateFactoryPlay());
            // Console.WriteLine("Play clicked");
        };
    }

    public void Exit(FactoryGame game)
    {
        _gameLabel.Destroy();
        _startBtn?.Destroy();
    }

    public void Update(FactoryGame game, float dt)
    {
    }

    public void Draw(FactoryGame game)
    {
    }
}
