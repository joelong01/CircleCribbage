using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Cribbage
{

    public class SetScoreEventArgs : EventArgs
    {
        public int Score { get; set; }
        public bool Hide { get; set; }
        public SetScoreEventArgs(int score)
        {
            Score = score;
            Hide = false;
        }
    }

    public delegate Task SetScoreDelegate(object sender, SetScoreEventArgs e);

    public class UiData : INotifyPropertyChanged
    {
        int _score = 8;

        public int Score
        {
            get 
            { 
                return _score; 
            }
            set 
            {
                if (value > 31) value = 31;
                if (value < 0) value = 0;

                if (value != _score)
                {                    
                    _score = value;
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

    public sealed partial class PlayerSetScoreControl : UserControl, IPlayerSetScore
    {

        UiData _uiData = new UiData();

       
        public event SetScoreDelegate OnPlayerSetScore;

        public PlayerSetScoreControl()
        {
            this.InitializeComponent();
            _txtScore.DataContext = _uiData;
        }

        public int Score
        {
            get { return _uiData.Score; }
            set { _uiData.Score = value; }
        }

        public string UserGuess
        {
            get
            {
                return _txtScore.Text;
            }
            set
            {
                _txtScore.Text = value;
                _uiData.Score = Convert.ToInt32(value);
            }

        }

        public  async Task Show()
        {
            Animation_X_RollIn.To = this.ActualWidth;            
            Animation_Angle_RollIn.To = 360;
            await StaticHelpers.RunStoryBoard(RollInScore, false);
        }

        public async Task Hide()
        {
            Animation_X_RollIn.To = 0;
            Animation_Angle_RollIn.To = 0;            
            await StaticHelpers.RunStoryBoard(RollInScore, false);
        }

        public void HideAsync()
        {
            Animation_X_RollIn.To = 0;
            Animation_Angle_RollIn.To = 0;
            RollInScore.Begin();
        }

        private void ButtonUpScore_Click(object sender, RoutedEventArgs e)
        {
            _uiData.Score++;            
        }

        private void ButtonDownScore_Click(object sender, RoutedEventArgs e)
        {
            _uiData.Score--;                        
        }

        private async void OnSetScore(object sender, RoutedEventArgs e)
        {

            SetScoreEventArgs args = new SetScoreEventArgs(_uiData.Score);
            await OnPlayerSetScore(this, args);
            if (args.Hide)
                await Hide();
        }

        public async Task<int> ShowAndWaitForContinue(int guessScore = 32)
        {
            if (guessScore < 32)
            {
                _uiData.Score = guessScore;                
            }

            await Show();
            
            var tcs = new TaskCompletionSource<object>();

            RoutedEventHandler OnCompletion = (_, args) =>
            {
                tcs.SetResult(null);
            };

            try
            {
                _btnSetScore.Click += OnCompletion;
                await tcs.Task;
            }
            finally
            {
                _btnSetScore.Click -= OnCompletion;
               
            }

            return _uiData.Score;
        }
       

    }
}
