using CribbageService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public enum UserChoice { Continue, Muggins };
    public interface IShowInstructionsAndHistoryController
    {
        void  SetMessage(string message);
       
        Task<UserChoice> ShowAndWait(string message);
        void ShowAsync(bool show, bool closeWithTimerm, string message);
        void InsertScoreSummary(ScoreType scoreType, int playerScore, int computerScore);

        Task Show(bool show, bool closeWithTimer, string message);

        Task InsertEndOfHandSummary(PlayerType dealer, int cribScore, List<CardView> list, int nComputerCountingPoint, int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn, HandsFromServer Hfs);

        void ResetScoreHistory();

        Task AddToHistory(List<CardView> fullHand, ScoreCollection scores, PlayerType player, Deck Deck, string score);

        string Save();

        Task<bool> Load(string s);
    }

   

    public sealed partial class HintWindow : UserControl, IShowInstructionsUi
    {
      
        ObservableCollection<UserControl> _scoreHistoryList = new ObservableCollection<UserControl>();

        
        private const int SCROLLBAR_WIDTH = 35;
        private const double HEIGHT_WIDTH_RATIO = 140.0 / 240.0;
        private const double HEIGHT_WIDTH_RATIO_HAND_SUMMARY = 400.0 / 310.0;

        TaskCompletionSource<object> _tcs = null;

        public bool IsOpen { get; set; }

        public HintWindow()
        {
            this.InitializeComponent();
            
          
            IsOpen = true;
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                _listHistory.Items.Clear();
            }

            _listHistory.ItemsSource = _scoreHistoryList;

        }

        
        public string Message
        {
            get
            {
                return _tbMessage.Text;
            }
            set
            {
                _tbMessage.Text = value;

            }
        }

        
        
    

        private void ScoreHistoryView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
            foreach (Control view in _scoreHistoryList)
            {
                if (_listHistory.ActualWidth - SCROLLBAR_WIDTH > 0)
                {
                    view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;
                    
                    Type type = view.GetType();
                    if (type == typeof(ScoreHistoryView))
                    {
                        view.Height = (view.Width * HEIGHT_WIDTH_RATIO);
                    }
                    else if (type == typeof(ScoreSummaryView))
                    {
                        view.Height = (view.Width * HEIGHT_WIDTH_RATIO) * 0.5;
                    }
                    else if (view.GetType() == typeof(OneHandHistoryCtrl))
                    {
                        view.Height = (view.Width * HEIGHT_WIDTH_RATIO_HAND_SUMMARY);
                    }
                    
                }
                else
                {
                  //  Debug.WriteLine("_listHistory is too small! ActualWidth:{0} Width:{1}", _listHistory.ActualWidth, _listHistory.Width);
                }
            }
        }


       

      

        public void RemoveScoreDetails()
        {
            for (int i = _scoreHistoryList.Count - 1; i >= 0; i-- )
            {
                Control view = _scoreHistoryList[i];
                if (view.GetType() != typeof(OneHandHistoryCtrl))
                {
                    _scoreHistoryList.RemoveAt(i);
                }
            }
        }

        
        void IShowInstructionsUi.SetMessage(string message)
        {
            _tbMessage.Text = message;
        }

       
     

        void IShowInstructionsUi.ShowAsync(bool show, bool closeWithTimerm, string message)
        {
            Message = message;
        }

        void IShowInstructionsUi.InsertScoreSummary(ScoreType scoreType, int playerScore, int computerScore)
        {
            ScoreSummaryView view = new ScoreSummaryView();
            view.Initialize(scoreType, playerScore, computerScore);
            view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;
            view.Height = (view.Width * HEIGHT_WIDTH_RATIO) * .50;
            _scoreHistoryList.Insert(0, view);
        }

       async Task IShowInstructionsUi.Show(bool show, bool closeWithTimer, string message)
        {
           this.Message = message;  
           await  Task.Delay(0);
        }

        async Task IShowInstructionsUi.InsertEndOfHandSummary(PlayerType dealer, int cribScore, List<CardView> crib, 
                                        int nComputerCountingPoint, int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn, HandsFromServer hfs)
        {

            RemoveScoreDetails();
            OneHandHistoryCtrl view = new OneHandHistoryCtrl();
            await view.SetPlayerCards(hfs.PlayerCards);
            await view.SetComputerHand(hfs.ComputerCards);
            await view.SetSharedCard(hfs.SharedCard);
            await view.SetCribHand(crib, dealer);
            view.SetCountScores(nPlayerCountingPoint, nComputerCountingPoint);
            view.SetCribScore(cribScore);
            view.SetComputerHandScore(ComputerPointsThisTurn);
            view.SetPlayerHandScore(PlayerPointsThisTurn);
            view.Width = _listHistory.ActualWidth - SCROLLBAR_WIDTH;
            view.Height = (view.Width * HEIGHT_WIDTH_RATIO_HAND_SUMMARY);
            _scoreHistoryList.Insert(0, view);

            //
            //  Counting stats
            MainPage.Current.StatsView.Stats.Stat(StatName.CountingMostPoints).UpdateStatistic(PlayerType.Player, nPlayerCountingPoint);
            MainPage.Current.StatsView.Stats.Stat(StatName.CountingMostPoints).UpdateStatistic(PlayerType.Computer, nComputerCountingPoint);
            MainPage.Current.StatsView.Stats.Stat(StatName.CountingTotalPoints).UpdateStatistic(PlayerType.Player, nPlayerCountingPoint);
            MainPage.Current.StatsView.Stats.Stat(StatName.CountingTotalPoints).UpdateStatistic(PlayerType.Computer, nComputerCountingPoint);
            
            //
            //  Hand Stats
            MainPage.Current.StatsView.Stats.Stat(StatName.HandMostPoints).UpdateStatistic(PlayerType.Player, PlayerPointsThisTurn);
            MainPage.Current.StatsView.Stats.Stat(StatName.HandMostPoints).UpdateStatistic(PlayerType.Computer, ComputerPointsThisTurn);
            MainPage.Current.StatsView.Stats.Stat(StatName.HandTotalPoints).UpdateStatistic(PlayerType.Player, PlayerPointsThisTurn);
            MainPage.Current.StatsView.Stats.Stat(StatName.HandTotalPoints).UpdateStatistic(PlayerType.Computer, ComputerPointsThisTurn);
            MainPage.Current.StatsView.Stats.Stat(StatName.HandAveragePoints).UpdateStatistic(PlayerType.Player, 0);
            MainPage.Current.StatsView.Stats.Stat(StatName.HandAveragePoints).UpdateStatistic(PlayerType.Computer, 0);

            //
            // Crib stats
            MainPage.Current.StatsView.Stats.Stat(StatName.CribMostPoints).UpdateStatistic(dealer, cribScore);
            MainPage.Current.StatsView.Stats.Stat(StatName.CribTotalPoints).UpdateStatistic(dealer, cribScore);
            MainPage.Current.StatsView.Stats.Stat(StatName.CribAveragePoints).UpdateStatistic(dealer, 0);
            if (cribScore == 0) MainPage.Current.StatsView.Stats.Stat(StatName.Crib0Points).UpdateStatistic(dealer, 1);


            //
            // keeping track of 0's
            if (PlayerPointsThisTurn == 0)
                MainPage.Current.StatsView.Stats.Stat(StatName.Hand0Points).UpdateStatistic(PlayerType.Player, 1);

            if (ComputerPointsThisTurn == 0)
                MainPage.Current.StatsView.Stats.Stat(StatName.Hand0Points).UpdateStatistic(PlayerType.Computer, 1);

            if (nComputerCountingPoint == 0)
                MainPage.Current.StatsView.Stats.Stat(StatName.Counting0Points).UpdateStatistic(PlayerType.Computer, 1);

            if (nPlayerCountingPoint == 0)
                MainPage.Current.StatsView.Stats.Stat(StatName.Counting0Points).UpdateStatistic(PlayerType.Player, 1);

        }

        async Task IShowInstructionsUi.WaitForContinue(string message)
        {
            _tcs = new TaskCompletionSource<object>();
            _tbMessage.Text = message;            
            _btnContinue.Visibility = Visibility.Visible;
            this.UpdateLayout();
            await _tcs.Task;
            _btnContinue.Visibility = Visibility.Collapsed;
            _tcs = null;

        }

        void IShowInstructionsUi.ResetScoreHistory()
        {
            _scoreHistoryList.Clear();
        }

        void IShowInstructionsUi.AddToHistory(ScoreHistoryView shv)
        {
            _scoreHistoryList.Insert(0, shv); 
        }


        double IShowInstructionsUi.HistoryViewWidth
        {
            get 
            {
                return _listHistory.ActualWidth - SCROLLBAR_WIDTH;
            }
        }


        ObservableCollection<UserControl> IShowInstructionsUi.HistoryList
        {
            get { return _scoreHistoryList; }
        }

        //
        //  returns the top left point that we want to animate the score views to -- in this case the (0,0) point of the listView
        //  in the coordinates of the parent...
        UIElement IShowInstructionsUi.AnimationPointTo
        {
            get 
            {
                return _listHistory;                
            }
        }


        void IShowInstructionsUi.Bounce()
        {
          //
          //    do nothing 
        }


        bool IShowInstructionsUi.IsRightSide
        {
            get { return true; }
        }


        Storyboard IShowInstructionsUi.BounceAnimation()
        {
            return null;
        }

        private void OnContinue(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(null);
        }

      
    }
}
