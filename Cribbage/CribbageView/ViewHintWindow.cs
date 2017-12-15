using Cribbage.Common;
using CribbageService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;


namespace Cribbage
{
    public interface IShowInstructionsUi
    {

        void SetMessage(string message);

        void ShowAsync(bool show, bool closeWithTimerm, string message);
        void InsertScoreSummary(ScoreType scoreType, int playerScore, int computerScore);

        Task Show(bool show, bool closeWithTimer, string message);

        Task InsertEndOfHandSummary(PlayerType dealer, int cribScore, List<CardView> list, int nComputerCountingPoint, int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn, HandsFromServer Hfs);

        void ResetScoreHistory();

        void AddToHistory(ScoreHistoryView shv);



        double HistoryViewWidth { get; }

        ObservableCollection<UserControl> HistoryList { get; }



        UIElement AnimationPointTo { get; }

        void Bounce();

        Storyboard BounceAnimation();

        bool IsRightSide { get;  }

        Task WaitForContinue(string message);
    }

    /// <summary>
    ///     Helper class.  it takes a list of objects that have individual scores in it and 
    ///     another control (FlyingScore) that has the total score for the card that was just played.
    ///     it animates the FlyingScore near the container of the history views.  That "bounces"
    ///     and then the list is added to the history view.  Finally, the FlyingScore is removed from the gridf
    /// 
    /// </summary>
    public class FlyoutScoreAnimationHelper
    {
        List<ScoreHistoryView> _shv = new List<ScoreHistoryView>();
        FlyingScore _fs = null;
        IShowInstructionsUi _ui = null;

        public FlyoutScoreAnimationHelper(List<ScoreHistoryView> list, FlyingScore flyingScore, IShowInstructionsUi ui)
        {
            _shv = list;
            _fs = flyingScore;
            _fs.Storyboard.Completed += Storyboard_Completed;
            _ui = ui;
        }

        void Storyboard_Completed(object sender, object e)
        {
            try
            {
                _ui.Bounce();
            }
            catch (NotImplementedException) { }

            Panel p = (Panel)_fs.Parent;
            p.Children.Remove(_fs);

            foreach (ScoreHistoryView score in _shv)
            {
                _ui.AddToHistory(score);
            }
        }

        public void Animate(double ms)
        {
            _fs.Animate(ms, 0);
        }
    }

    //
    //  this is the view that the "base views" (CircleLayoutPage and SquarePage) delegate their logic too for shared implementation
    public partial class CribbageView : IShowInstructionsAndHistoryController
    {
        private const double HEIGHT_WIDTH_RATIO = 140.0 / 240.0;
        private double ANIMATION_SPEED_FLYING_SCORE = 1000;
        Queue<FlyoutScoreAnimationHelper> _scoreQueue = new Queue<FlyoutScoreAnimationHelper>();
        DispatcherTimer _timerForFlyingScore = new DispatcherTimer();

        void IShowInstructionsAndHistoryController.SetMessage(string message)
        {
            ShowInstructionsUi.SetMessage(message);
        }
        async Task<UserChoice> IShowInstructionsAndHistoryController.ShowAndWait(string message)
        {


            await ShowInstructionsUi.WaitForContinue(message);


            //UserChoice choice = UserChoice.Continue;
            //PromptUserDlg dlg = new PromptUserDlg();
            //choice = await dlg.ShowAndWait(message);
            return UserChoice.Continue;
        }
        void IShowInstructionsAndHistoryController.ShowAsync(bool show, bool closeWithTimer, string message)
        {
            ShowInstructionsUi.ShowAsync(show, closeWithTimer, message);
        }
        void IShowInstructionsAndHistoryController.InsertScoreSummary(ScoreType scoreType, int playerScore, int computerScore)
        {
            ShowInstructionsUi.InsertScoreSummary(scoreType, playerScore, computerScore);
        }
        Task IShowInstructionsAndHistoryController.Show(bool show, bool closeWithTimer, string message)
        {
            return ShowInstructionsUi.Show(show, closeWithTimer, message);
        }
        Task IShowInstructionsAndHistoryController.InsertEndOfHandSummary(PlayerType dealer, int cribScore, List<CardView> list, int nComputerCountingPoint, int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn, HandsFromServer Hfs)
        {
            return ShowInstructionsUi.InsertEndOfHandSummary(dealer, cribScore, list, nComputerCountingPoint, nPlayerCountingPoint, ComputerPointsThisTurn, PlayerPointsThisTurn, Hfs);
        }
        void IShowInstructionsAndHistoryController.ResetScoreHistory()
        {
            ShowInstructionsUi.ResetScoreHistory();
        }
        async Task IShowInstructionsAndHistoryController.AddToHistory(List<CardView> fullHand, ScoreCollection scores, PlayerType player, Deck Deck, string gameScore)
        {

            bool isOnRight = ShowInstructionsUi.IsRightSide;
            double y = 800;
            double x = 100;

            
            Point to = new Point(x, y);

            double angle = 45;
            if (!isOnRight)
            {
                angle = -90;
                y = 200;
            }

            double scoreDiameter = 75.0;


            Point p = GetCenterPointForScoreAnimation(to, isOnRight);

            if (_scoreQueue.Count == 0) _timerForFlyingScore.Interval = TimeSpan.FromTicks(1);
            List<ScoreHistoryView> list = new List<ScoreHistoryView>();
            foreach (ScoreInstance score in scores.Scores)
            {
                ScoreHistoryView view = new ScoreHistoryView();
                await view.PopulateGrid(fullHand, score, player, scores.ScoreType, ShowInstructionsUi.HistoryList.Count + 1, scores.Total, scores.ActualScore, gameScore);
                view.Width = ShowInstructionsUi.HistoryViewWidth;
                view.Height = view.Width * HEIGHT_WIDTH_RATIO;
                view.HorizontalAlignment = HorizontalAlignment.Left;
                view.VerticalAlignment = VerticalAlignment.Top;
                list.Add(view);
            }

            FlyingScore flyingScore = new FlyingScore();
            LayoutRoot.Children.Add(flyingScore);
            Grid.SetColumnSpan(flyingScore, 0xFF);
            Grid.SetRowSpan(flyingScore, 0xFF);
            Canvas.SetZIndex(flyingScore, Int16.MaxValue);
            flyingScore.Message = scores.Total.ToString();
            flyingScore.Center = p;
            flyingScore.Width = scoreDiameter;
            flyingScore.Height = scoreDiameter;
            flyingScore.Angle = angle;
            flyingScore.HorizontalAlignment = HorizontalAlignment.Center;
            flyingScore.VerticalAlignment = VerticalAlignment.Center;

            FlyoutScoreAnimationHelper helper = new FlyoutScoreAnimationHelper(list, flyingScore, ShowInstructionsUi);
            _scoreQueue.Enqueue(helper);
            _timerForFlyingScore.Start();
        }

        private void OnFireScoreAnimation(object sender, object e)
        {
            _timerForFlyingScore.Interval = TimeSpan.FromMilliseconds(ANIMATION_SPEED_FLYING_SCORE / 4); //subsequent ones delay
            FlyoutScoreAnimationHelper helper = null;
            if (_scoreQueue.Count == 0)
            {
                _timerForFlyingScore.Stop();
                return;
            }

            try
            {
                helper = _scoreQueue.Dequeue();
                helper.Animate(ANIMATION_SPEED_FLYING_SCORE);
            }
            catch (Exception)
            {
                _timerForFlyingScore.Stop();
            }
        }

        private Point GetCenterPointForScoreAnimation(Point To, bool right)
        {
            double centerScoreX = Window.Current.Bounds.Width / 2.0;
            double centerScoreY = Window.Current.Bounds.Height / 2.0;
            double intersectX = To.X;
            double intersectY = To.Y;

            double radius = Math.Sqrt(Math.Pow(centerScoreX - intersectX, 2) + Math.Pow(centerScoreY - intersectY, 2));
            Point p = new Point(-centerScoreX, intersectY + radius - centerScoreY);
            if (right)
                p.X = -p.X;

            return p;
        }

        string IShowInstructionsAndHistoryController.Save()
        {
            string s = "[Score History]\n";

            ObservableCollection<UserControl> history = ShowInstructionsUi.HistoryList;

            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].GetType() == typeof(ScoreHistoryView))
                {
                    s += String.Format("Score {0}={1}*{2}\n", i, "ScoreInstance", ((ScoreHistoryView)history[i]).Save());
                }
            }

            return s;
        }
        async Task<bool> IShowInstructionsAndHistoryController.Load(string s)
        {
            Dictionary<string, string> history = StaticHelpers.DeserializeDictionary(s);
            if (history == null) return false;
            string value = "";
            Deck deck = MainPage.Current.Deck;
            for (int i = 0; i < history.Count; i++)
            {
                string key = String.Format("Score {0}", i);
                if (history.TryGetValue(key, out value))
                {
                    var tokens = value.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens[0] == "ScoreInstance")
                    {
                        ScoreHistoryView shv = new ScoreHistoryView();
                        await shv.PopulateGrid(tokens[1]);
                        ShowInstructionsUi.HistoryList.Add(shv);
                    }

                }
            }

            return true;
        }

    }
}
