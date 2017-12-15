using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public enum AchievementType { MaxHand, MaxCrib, MostPointsCounted, BigestWin, BiggestLoss };

    public class Achievement
    {
        public AchievementType Type { get; set; }
        public string Description { get; set; }

    }

    
    public sealed partial class AchievementControl : UserControl
    {
        private Achievement[] _achievements = new Achievement[]
        {
            new Achievement() {Type = AchievementType.MaxHand, Description = "Max Hand"},
            new Achievement() {Type = AchievementType.MaxCrib, Description = "Max Crib"},            
            new Achievement() {Type = AchievementType.MostPointsCounted, Description = "Counted Points"},
            new Achievement() {Type = AchievementType.BigestWin, Description = "Margin of Victory"},
            new Achievement() {Type = AchievementType.BiggestLoss, Description = "Loss Margin"},

        };
        
        private string[] _sAchievementNames = new string[] { "Maximum Hand", "Maximum Crib" };

        DispatcherTimer _timer = new DispatcherTimer();
        
        public AchievementControl()
        {
            this.InitializeComponent();
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {

                _timer.Tick += OnTimer_Tick;
                _timer.Interval = new TimeSpan(0, 0, 5);

            }
        }

        private async void OnTimer_Tick(object sender, object e)
        {
            await Show(false);            
        }

        private async Task Show(bool bShow)
        {
            if (bShow)
            {
                _daVerticalStart.To = -(this.ActualHeight);
                _timer.Start();
            }
            else
            {
                _daVerticalStart.To = 0;
                _timer.Stop();
            }
            
            await StaticHelpers.RunStoryBoard(_sbShow, false, 1500, true);

        }

        public string AchivementName 
        {
            get
            {
                return _txtName.Text;
            }
            set
            {
                _txtName.Text = value;
            }
        }
        public string Value
        {
            get
            {
                return _txtValue.Text;
            }
            set
            {
                _txtValue.Text = value;
            }
        }

        public async Task ShowAchievement(AchievementType name, string value)
        {
            double height =  Window.Current.Bounds.Height;
            int index = (int)name;
            _txtName.Text = _achievements[index].Description;
            _txtValue.Text = value;            
            await Show(true);
        }

    }
}
