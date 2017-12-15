using CribbageService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cribbage
{
  

    public class UIState : INotifyPropertyChanged
    {

        //
        //  bound UI elements
        string _uisPlayerTurnReminder = "Your Turn";
        string _uisPlayerScore = "Score: 0";
        string _uisComputerScore = "Score: 0";
        string _uisPlayerCrib = "Your Crib";
        string _uisComputerCrib = "";
        int _scorePlayer = 0;
        int _scoreComputer = 0;
        ICribbageBoardUi _callback = null;

      

        private UIState() { }
        public UIState(ICribbageBoardUi cb)
        {
            _callback = cb;
            Reset();
        }

        public void Reset()
        {
            _scorePlayer = 0;
            _scoreComputer = 0;
            AddScoreAsync(PlayerType.Computer, 0);
            AddScoreAsync(PlayerType.Player, "0");
            UIString_PlayerTurnReminder = "Touch 'New Game' to start";
            UIString_PlayerCrib = "";
            UIString_ComputerCrib = "";
            _callback.Reset();


        }



        public PlayerType Turn
        {
            set
            {
                if (value == PlayerType.Computer)
                {
                    UIString_PlayerTurnReminder = "";
                }
                else
                {
                    UIString_PlayerTurnReminder = "Your Turn";
                }
            }
        }

        public PlayerType Crib
        {
            set
            {
                if (value == PlayerType.Computer)
                {
                    UIString_PlayerCrib = "Computer's Crib";
                    UIString_ComputerCrib = "Computer's Crib";

                }
                else
                {
                    UIString_PlayerCrib = "Player's Crib";
                    UIString_ComputerCrib = "Players's Crib";
                }
            }
        }

        public void AddScoreAsync(PlayerType player, object score)
        {


            int scoreToAdd = 0;
            if (score.GetType() == typeof(int))
            {
                scoreToAdd = (int)score;
            }
            else if (score.GetType() == typeof(string))
            {
                scoreToAdd = Convert.ToInt32((string)score);

            }
            else
                throw new FormatException("Unexpected type passed to SetScore");


            if (scoreToAdd == 0) return;



            if (player == PlayerType.Player)
            {
                _scorePlayer += scoreToAdd;
                UIString_PlayerScore = String.Format("Score: {0}", _scorePlayer);
            }
            else
            {
                _scoreComputer += scoreToAdd;
                UIString_ComputerScore = String.Format("Score: {0}", _scoreComputer);
            }

            _callback.AnimateScoreAsync(player, scoreToAdd);

        }

        public async Task AddScore(PlayerType player, object score)
        {


            int scoreToAdd = 0;
            if (score.GetType() == typeof(int))
            {
                scoreToAdd = (int)score;
            }
            else if (score.GetType() == typeof(string))
            {
                scoreToAdd = Convert.ToInt32((string)score);

            }
            else
                throw new FormatException("Unexpected type passed to SetScore");


            if (scoreToAdd == 0) return;



            if (player == PlayerType.Player)
            {
                _scorePlayer += scoreToAdd;
                UIString_PlayerScore = String.Format("Score: {0}", _scorePlayer);
            }
            else
            {
                _scoreComputer += scoreToAdd;
                UIString_ComputerScore = String.Format("Score: {0}", _scoreComputer);
            }

           await  _callback.AnimateScore(player, scoreToAdd);

        }

        public string UIString_PlayerTurnReminder
        {
            get { return _uisPlayerTurnReminder; }
            set
            {
                if (_uisPlayerTurnReminder != value)
                {
                    _uisPlayerTurnReminder = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string UIString_PlayerScore
        {
            get { return _uisPlayerScore; }
            set
            {
                if (_uisPlayerScore != value)
                {
                    _uisPlayerScore = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string UIString_ComputerScore
        {
            get { return _uisComputerScore; }
            set
            {
                if (_uisComputerScore != value)
                {
                    _uisComputerScore = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string UIString_PlayerCrib
        {
            get { return _uisPlayerCrib; }
            set
            {
                if (_uisPlayerCrib != value)
                {
                    _uisPlayerCrib = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string UIString_ComputerCrib
        {
            get { return _uisComputerCrib; }
            set
            {
                if (_uisComputerCrib != value)
                {
                    _uisComputerCrib = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
