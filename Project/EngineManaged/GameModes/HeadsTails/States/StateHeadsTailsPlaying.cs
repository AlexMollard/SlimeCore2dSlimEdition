using EngineManaged;
using EngineManaged.Numeric;
using EngineManaged.Rendering;
using EngineManaged.Scene;
using EngineManaged.UI;
using GameModes.Test;
using SlimeCore.Source.Common;
using SlimeCore.Source.Core;
using SlimeCore.Source.Input;
using System;
namespace SlimeCore.GameModes.Test.States
{
    public class StateHeadsTailsPlaying : IGameState<HeadsTailsGame>
    {
        private UIText _resultText;
        private UIText _highscoreText;
        private UIText _scoreText;
        private UIButton? _headsButton;
        private UIButton? _tailsButton;
        private int _score;
        private int _highScore;

       
        public void Enter(HeadsTailsGame game)
        {

            _resultText = UIText.Create("TRY YOUR LUCK", 1, 0, 0);
            _resultText.Anchor(0.5f, 0.5f); // Center anchor
            _resultText.Layer(100);
            _resultText.Color(1.0f, 1.0f, 1.0f);

            _scoreText = UIText.Create("0", 1, 0, 2);
            _scoreText.Anchor(0.5f, 0.5f); // Center anchor
            _scoreText.Layer(100);
            _scoreText.Color(1.0f, 1.0f, 1.0f);

            _highscoreText = UIText.Create("", 1, 0, 4);
            _highscoreText.Anchor(0.5f, 0.5f); // Center anchor
            _highscoreText.Layer(100);
            _highscoreText.Color(1.0f, 1.0f, 1.0f);


            _headsButton = UIButton.Create("Heads", -5.0f, -3.0f, 8.0f, 3.0f, 0.2f, 0.2f, 0.2f, 100, 1, false);
            _headsButton.Label.Color(0.1f, 0.1f, 0.1f);

            _headsButton.Clicked += () =>
            {
                SetOutcome(game, 0);
            };


            _tailsButton = UIButton.Create("Tails", 5.0f, -3.0f, 8.0f, 3.0f, 1f, 0f, 1f, 100, 1, false);
            _tailsButton.Label.Color(0.1f, 0.1f, 0.1f);

            _tailsButton.Clicked += () =>
            {
                SetOutcome(game, 1);
            };
        }

        private void SetOutcome(HeadsTailsGame game, int winningResult)
        {
            string resultString = winningResult == 0 ? "HEADS" : "TAILS";
            string losingString = winningResult != 0 ? "TAILS" : "HEADS";
            int value = game.Rng.Next(2);
            if (value == winningResult)
            {
                _resultText.Text(resultString);
                _score++;
                _scoreText.Text(string.Format("{0}", _score));
            }
            else
            {
                _resultText.Text(losingString);
                if (_score > _highScore)
                {
                    _highScore = _score;
                    _highscoreText.Text(string.Format("{0}", _highScore));
                }
                _score = 0;
                _scoreText.Text(string.Format("{0}", _score));
            }
        }

        public void Exit(HeadsTailsGame game) { }
        public void Update(HeadsTailsGame game, float dt) { }
        public void Draw(HeadsTailsGame game) { }
    }
}
