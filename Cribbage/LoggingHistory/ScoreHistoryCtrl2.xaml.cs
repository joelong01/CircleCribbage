using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CribbageService;
using System.Threading.Tasks;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public class ScoreHistoryData
    {
        public string Count { get; set; }
        public string Player { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Number { get; set; }
        public string Score { get; set; }
        public string Total { get; set; }
    }

    public sealed partial class ScoreHistoryCtrl2 : UserControl
    {
        ObservableCollection<ScoreHistoryData> _scoreList = new ObservableCollection<ScoreHistoryData>();
        private bool _mouseCaptured = false;
        private Point _pointMouseDown;
        private int _count = 0;


        public ScoreHistoryCtrl2()
        {
            this.InitializeComponent();
            _listScores.ItemsSource = _scoreList;
        }

        public void AddScore(PlayerType player, ScoreCollection score, string total)
        {

            
            ScoreHistoryData data = new ScoreHistoryData();            
            data.Count = (++_count).ToString();
            data.Player = player.ToString();
            data.Type = score.ScoreType.ToString();                        
            _scoreList.Add(data);

            foreach (ScoreInstance s in score.Scores)
            {
                data = new ScoreHistoryData();
                data.Description = s.Description;
                data.Number = s.Count.ToString();
                data.Score = String.Format("{0} of {1}", s.Score, score.Total);
                _scoreList.Add(data);

            }

            data = new ScoreHistoryData();
            data.Total = total;
            _scoreList.Add(data);

        }

        public double GrabBarWidth
        {
            get
            {
                int columnCount = LayoutRoot.ColumnDefinitions.Count;
                double handleWidth = LayoutRoot.ColumnDefinitions[columnCount - 1].ActualWidth + LayoutRoot.ColumnDefinitions[columnCount - 2].ActualWidth;
                return handleWidth;
            }

        }


        int _open = 0;
        public async Task ToggleOpen()
        {
            if (_open == 0)
            {
                
                _xAnimation.Value = this.ActualWidth - this.GrabBarWidth - LayoutRoot.ColumnDefinitions[0].ActualWidth;
            }
            else
            {
                _xAnimation.Value = 0;
            }

            await StaticHelpers.RunStoryBoard(ScoreHistoryAnimatePosition, false, 1000, false);

            _open = 1 - _open;
        }



        private void LayoutRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

            _pointMouseDown = e.GetCurrentPoint(this).Position;
            _mouseCaptured = ((UIElement)sender).CapturePointer(e.Pointer);
            e.Handled = true;

        }

        private void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_mouseCaptured)
                return;


        }

        private async void LayoutRoot_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;



            if (_mouseCaptured)
            {
                _mouseCaptured = false;
                this.ReleasePointerCapture(e.Pointer);
                await ToggleOpen();

            }
        }

        public void Reset()
        {
            _scoreList.Clear();
            _count = 0;
        }
    }
}
