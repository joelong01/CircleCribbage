using CribbageService;
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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class RoundBoardControl : UserControl, INotifyPropertyChanged, ICribbageBoardUi, IPlayerSetScore
    {
        public readonly double PLAYER_PEG_OFFSET = 10.0;
        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(RoundBoardControl), null);
        List<Ellipse> _playerHoles = new List<Ellipse>();
        List<Ellipse> _computerHoles = new List<Ellipse>();
        List<RoundRectDivider> _dividers = new List<RoundRectDivider>();
        private double _holeDiameter = 12.0;
        private double _pegDiameter = 14.0;
        public double HoleDiameter
        {
            get { return _holeDiameter; }
            set { _holeDiameter = value; NotifyPropertyChanged(); }
        }

        public RoundBoardControl()
        {
            this.InitializeComponent();
            this.DataContext = this;
            BuildPegLists();
            _playerPegScore2.Score = -1;
            _playerPegScore2.MovePegStoryBoard = _sbMovePegScore2;
            _playerPegScore1.Score = 0;
            _playerPegScore1.MovePegStoryBoard = _sbMovePegScore1;


            _computerPegScore2.Score = -1;
            _computerPegScore2.MovePegStoryBoard = _sbMoveComputerPegScore2;
            _computerPegScore1.Score = 0;
            _computerPegScore1.MovePegStoryBoard = _sbMoveComputerPegScore1;

        }

        private void BuildPegLists()
        {
            Ellipse e = null;
            string name = "";

            _playerHoles.Add(_pStartBack);
            _playerHoles.Add(_pStartFront);
            _computerHoles.Add(_cStartBack);
            _computerHoles.Add(_cStartFront);


            for (int i = 1; i < 121; i++)
            {
                name = "_p" + i.ToString();
                e = (Ellipse)LayoutRoot.FindName(name);
                _playerHoles.Add(e);
                name = "_c" + i.ToString();
                e = (Ellipse)LayoutRoot.FindName(name);
                _computerHoles.Add(e);
            }
            _playerHoles.Add(_winningHole);
            _computerHoles.Add(_winningHole);

            foreach (UIElement element in LayoutRoot.Children)
            {
                if (element.GetType() == typeof(RoundRectDivider))
                {
                    _dividers.Add((RoundRectDivider)element);
                }
            }


        }


        public double Radius
        {
            get
            {
                return _ring.Radius;
            }
            set
            {
                _ring.Radius = value;
                NotifyPropertyChanged();
            }
        }
        public double InnerRadius
        {
            get
            {
                return _ring.InnerRadius;

            }
            set
            {
                _ring.InnerRadius = value;
                NotifyPropertyChanged();
            }
        }

        public double XOffset
        {
            get
            {
                return _ring.StrokeThickness;
            }
            set
            {

            }
        }


        private double _insideHoleTranslateX = 316;
        double CalculateInsideHole_TranslateX()
        {
            return -(Radius - 100 + _holeDiameter / 2.0 + PLAYER_PEG_OFFSET);
        }
        public double InsideHole_TranslateX
        {
            get
            {
                return _insideHoleTranslateX;
            }
            set
            {
                _insideHoleTranslateX = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("InsideHole_CenterX");
            }
        }


        public double InsideHole_CenterX
        {
            get
            {

                return -_insideHoleTranslateX;
            }
            set
            {
                _insideHoleTranslateX = -value;
                NotifyPropertyChanged();
            }
        }

        private double _outsideHole_TranslateX = 384;

        double CalculateOutsideHole_TranslateX()
        {
            return -(Radius - PLAYER_PEG_OFFSET - _holeDiameter / 2.0);
        }

        public double OutsideHole_TranslateX
        {
            get
            {
                return _outsideHole_TranslateX;
            }
            set
            {
                _outsideHole_TranslateX = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("OutsideHole_CenterX");
            }
        }
        public double OutsideHole_CenterX
        {
            get
            {
                return -_outsideHole_TranslateX;
            }
            set
            {
                _outsideHole_TranslateX = -value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("OutsideHole_TranslateX");
            }
        }

        double _winningHole_TranslateX = 344;
        double CalculateWinningHole_TranslateX()
        {
            _winningHole_TranslateX = -(Radius - 50 - _holeDiameter / 2.0);
            return _winningHole_TranslateX;
        }

        public double WinningHole_TranslateX
        {
            get
            {
                return _winningHole_TranslateX;
            }
            set
            {
                _winningHole_TranslateX = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("WinningHole_CenterX");
            }
        }
        public double WinningHole_CenterX
        {
            get
            {
                return -_winningHole_TranslateX;
            }
            set
            {
                _winningHole_TranslateX = -value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("WinningHole_TranslateX");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        double GetEllipseDiameter(double bigRadius)
        {

            if (bigRadius < 401)
                return 10;

            if (bigRadius < 500)
                return 12.0;

            if (bigRadius < 600)
                return 13;

            if (bigRadius < 700)
                return 14;

            return 16;

        }

        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            Radius = Math.Min(e.NewSize.Height, e.NewSize.Width) / 2.0;
            double diameter = Radius * 2;
            LayoutRoot.Width = diameter;
            LayoutRoot.Height = diameter;
            InnerRadius = Radius - 100;

            _holeDiameter = GetEllipseDiameter(Radius);
            _pegDiameter = _holeDiameter + 2;

            //
            //  drives the layout of all the peg holes
            OutsideHole_TranslateX = CalculateOutsideHole_TranslateX();
            InsideHole_TranslateX = CalculateInsideHole_TranslateX();
            WinningHole_TranslateX = CalculateWinningHole_TranslateX();

            for (int i = 0; i < 122; i++)
            {

                Ellipse ellipse = _playerHoles[i];
                ellipse.Width = _holeDiameter;
                ellipse.Height = _holeDiameter;
                CompositeTransform ct = ellipse.RenderTransform as CompositeTransform;
                ct.CenterX = -OutsideHole_TranslateX;
                ct.TranslateX = OutsideHole_TranslateX;
                ellipse = _computerHoles[i];
                ellipse.Width = _holeDiameter;
                ellipse.Height = _holeDiameter;
                ct = ellipse.RenderTransform as CompositeTransform;
                ct.CenterX = -InsideHole_TranslateX;
                ct.TranslateX = InsideHole_TranslateX;
            }


            CompositeTransform _winningTransform = ((Ellipse)_winningHole).RenderTransform as CompositeTransform;
            _winningTransform.CenterX = -WinningHole_TranslateX;
            _winningTransform.TranslateX = WinningHole_TranslateX;

            //
            //  there is a XAML bug where I can't animate and databind the same object
            foreach (RoundRectDivider divider in _dividers)
            {
                divider.Radius = Radius;
            }
            UpdatePegTransform(_playerPegScore1, true);
            UpdatePegTransform(_playerPegScore2, true);
            UpdatePegTransform(_computerPegScore1, false);
            UpdatePegTransform(_computerPegScore2, false);

            //
            //  support for getting the user score

            _ringGetPlayerScore.Radius = Radius;
            _ringGetPlayerScore.InnerRadius = Radius - 100;
            ((CompositeTransform)_vbUp.RenderTransform).CenterX = Radius - 50;
            ((CompositeTransform)_vbUp.RenderTransform).TranslateX = -(Radius - 50);
            ((CompositeTransform)_vbDown.RenderTransform).CenterX = Radius - 50;
            ((CompositeTransform)_vbDown.RenderTransform).TranslateX = -(Radius - 50);
            ((CompositeTransform)_vbOk.RenderTransform).CenterX = Radius - 33;
            ((CompositeTransform)_vbOk.RenderTransform).TranslateX = -(Radius - 33);
            ((CompositeTransform)_vbScore.RenderTransform).CenterX = Radius - 80;
            ((CompositeTransform)_vbScore.RenderTransform).TranslateX = -(Radius - 80);
            _rectPendulum1.Height = Radius - 100;
            _rectPendulum2.Height = Radius - 100;
            ((CompositeTransform)_rectPendulum1.RenderTransform).TranslateY = (Radius - 100) / 2.0;
            ((CompositeTransform)_rectPendulum2.RenderTransform).TranslateY = (Radius - 100) / 2.0;
            _ctEllipseRivet1.TranslateY = Radius - 100;
            _ctEllipseRivet1.CenterY = 100 - Radius;
            _ctEllipseRivet2.TranslateY = Radius - 100;
            _ctEllipseRivet2.CenterY = 100 - Radius;
            _ctPendulumCrossbar.TranslateY = (Radius - 100) * Math.Cos(20 * Math.PI / 180); ;
            _rectPendulumCrossbar.Width = 2 * (Radius - 100) * Math.Sin(20 * Math.PI / 180);


        }

        private void UpdatePegTransform(PegControl pegControl, bool outsidePeg)
        {
            pegControl.Width = _pegDiameter;
            pegControl.Height = _pegDiameter;
            CompositeTransform transform = pegControl.RenderTransform as CompositeTransform;
            double translateX = -(Radius - 100 + _pegDiameter / 2.0 + PLAYER_PEG_OFFSET);
            if (outsidePeg)
            {
                translateX = -(Radius - PLAYER_PEG_OFFSET - _pegDiameter / 2.0);
            }

            transform.TranslateX = translateX;
            transform.CenterX = -translateX;
        }

        Ellipse GetEllipseForScore(List<Ellipse> holeList, int score)
        {
            return holeList[score + 1];
        }

        public List<Task<object>> AnimateScore(PlayerType type, int scoreDelta)
        {

            List<Task<object>> taskList = new List<Task<object>>();
            PegControl backPeg = null;

            List<Ellipse> holeList = null;
            int newScore = -1;
            if (type == PlayerType.Player)
            {

                holeList = _playerHoles;
                if (_playerPegScore1.Score > _playerPegScore2.Score)
                {
                    backPeg = _playerPegScore2;
                    newScore = _playerPegScore1.Score + scoreDelta;

                }
                else
                {
                    backPeg = _playerPegScore1;
                    newScore = _playerPegScore2.Score + scoreDelta;
                }
            }
            else
            {
                holeList = _computerHoles;
                if (_computerPegScore1.Score > _computerPegScore2.Score)
                {
                    backPeg = _computerPegScore2;
                    newScore = _computerPegScore1.Score + scoreDelta;
                }
                else
                {
                    backPeg = _computerPegScore1;
                    newScore = _computerPegScore2.Score + scoreDelta;
                }
            }

            int animateScore = newScore;
            if (newScore > 121) animateScore = 121;


            double ms = 50;
            if (MainPage.AnimationSpeeds != null)
                ms = MainPage.AnimationSpeeds.Medium / 10;

            Duration rotateDuration = TimeSpan.FromMilliseconds(ms * (animateScore - backPeg.Score));
            Duration durationTo120 = TimeSpan.FromMilliseconds(ms * (120 - backPeg.Score));
            TimeSpan durationZero = TimeSpan.FromMilliseconds(0);

            backPeg.Score = newScore;

            DoubleAnimation animateRotate = backPeg.MovePegStoryBoard.Children[0] as DoubleAnimation;
            DoubleAnimation animateTranslateX = backPeg.MovePegStoryBoard.Children[1] as DoubleAnimation;
            DoubleAnimation animateCenterX = backPeg.MovePegStoryBoard.Children[2] as DoubleAnimation;


            Ellipse ellipse = GetEllipseForScore(holeList, animateScore);
            CompositeTransform compositeTransform = ellipse.RenderTransform as CompositeTransform;
            animateRotate.To = compositeTransform.Rotation;
            animateTranslateX.To = compositeTransform.TranslateX;
            animateCenterX.To = compositeTransform.CenterX;

            animateCenterX.Duration = TimeSpan.FromMilliseconds(0);
            animateTranslateX.Duration = TimeSpan.FromMilliseconds(0);
            if (newScore <= 120)
            {
                animateRotate.Duration = rotateDuration;
                animateCenterX.BeginTime = durationZero;
                animateTranslateX.BeginTime = durationZero;
                animateCenterX.Duration = durationZero;
                animateTranslateX.Duration = durationZero;
            }
            else
            {

                animateCenterX.BeginTime = durationTo120.TimeSpan;
                animateTranslateX.BeginTime = durationTo120.TimeSpan;
                Duration durationToWin = TimeSpan.FromMilliseconds(ms);
                animateCenterX.Duration = durationToWin;
                animateTranslateX.Duration = durationToWin;

            }

            taskList.Add(backPeg.MovePegStoryBoard.ToTask());
            return taskList;
        }

        public async Task Reset()
        {
            _playerPegScore1.Score = 0;
            _playerPegScore2.Score = -1;
            List<Task<object>> taskList = new List<Task<object>>();

            Task<object> t = ResetAnimation(_playerPegScore1.MovePegStoryBoard, _pStartFront);
            taskList.Add(t);
            t = ResetAnimation(_playerPegScore2.MovePegStoryBoard, _pStartBack);
            taskList.Add(t);
            _computerPegScore1.Score = 0;
            _computerPegScore2.Score = -1;
            t = ResetAnimation(_computerPegScore1.MovePegStoryBoard, _cStartFront);
            taskList.Add(t);
            t = ResetAnimation(_computerPegScore2.MovePegStoryBoard, _cStartBack);
            taskList.Add(t);
            await Task.WhenAll(taskList);

        }

        private Task<object> ResetAnimation(Storyboard sb, Ellipse target)
        {
            DoubleAnimation animateRotate = sb.Children[0] as DoubleAnimation;
            DoubleAnimation animateTranslateX = sb.Children[1] as DoubleAnimation;
            DoubleAnimation animateCenterX = sb.Children[2] as DoubleAnimation;

            animateRotate.To = ((CompositeTransform)((Ellipse)target).RenderTransform).Rotation;
            animateTranslateX.To = ((CompositeTransform)((Ellipse)target).RenderTransform).TranslateX;
            animateCenterX.To = ((CompositeTransform)((Ellipse)target).RenderTransform).CenterX;

            Duration resetDuration = TimeSpan.FromMilliseconds(500);
            TimeSpan beginDuration = TimeSpan.FromMilliseconds(0);
            foreach (DoubleAnimation animation in sb.Children)
            {
                animation.Duration = resetDuration;
                animation.BeginTime = beginDuration;
            }

            return sb.ToTask();
        }

        private async Task HighlightScore(int score, int count, bool highlight)
        {
            int start = score + 1;
           // TimeSpan delay = TimeSpan.FromMilliseconds(MainPage.AnimationSpeeds.VeryFast);
            TimeSpan delay = TimeSpan.FromMilliseconds(10);
            
            if (highlight)
            {
                for (int i = start; i < start + count; i++)
                {
                    HighlightPegHole(i, highlight);
                    await Task.Delay(delay);
                }
            }
            else
            {
                for (int i = start + count - 1; i >=start ; i--)
                {
                    HighlightPegHole(i, highlight);
                    await Task.Delay(delay);
                }
            }
        }


        public void HighlightPegHole(int score, bool highlight)
        {
            if (score > 121) score = 121;
            if (score < 1) score = 1;

            Brush br = _pStartFront.Fill;
            Brush brStroke = _pStartFront.Stroke;
            if (highlight)
            {
                br = _playerPegScore1.PlayerBrush;
                brStroke = _playerPegScore1.PegStroke;
            }

            Ellipse e = GetEllipseForScore(_playerHoles, score);
            e.Fill = br;
            e.Stroke = brStroke;

        }

        public int PlayerFrontScore
        {
            get
            {
                return Math.Max(_playerPegScore1.Score, _playerPegScore2.Score);
            }
        }

        public async Task<int> ShowAndWaitForContinue(int actualScore)
        {

            await ShowGetUserScoreUi(true);

            bool autoSetScore = true;
            if (MainPage.Current != null && !MainPage.Current.Settings.AutoSetScore)
                autoSetScore = false;

            if (autoSetScore)
            {
                _tbScoreToAdd.Text = actualScore.ToString();
                _tbScoreToAdd.UpdateLayout();

                await HighlightScore(PlayerFrontScore + actualScore, Convert.ToInt32(_tbScoreToAdd.Text), false);  //if the player guessed too high, need to reset those back to normal                
                await HighlightScore(PlayerFrontScore, actualScore, true);
            }
            else
            {
                await HighlightScore(PlayerFrontScore, Convert.ToInt32(_tbScoreToAdd.Text), true);
            }

            var tcs = new TaskCompletionSource<object>();

            RoutedEventHandler OnCompletion = (_, args) =>
            {
                int scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
                tcs.SetResult(null);
            };

            try
            {
                _btnAccept.Click += OnCompletion;
                await tcs.Task;
                int score = Convert.ToInt32(_tbScoreToAdd.Text);
                await HighlightScore(PlayerFrontScore, score, false);
                await ShowGetUserScoreUi(false);
                return score;
            }
            finally
            {
                _btnAccept.Click -= OnCompletion;


            }
        }

        private void SetupSetScoreAnimation(bool show)
        {
            DoubleAnimation animateOpacity = _sbGetPlayerScore.Children[0] as DoubleAnimation;
            DoubleAnimation animateRotation = _sbGetPlayerScore.Children[1] as DoubleAnimation;
            animateOpacity.Duration = TimeSpan.FromMilliseconds(1000);
            animateRotation.Duration = TimeSpan.FromMilliseconds(1000);

            BounceEase bounce = ((DoubleAnimation)_sbGetPlayerScore.Children[1]).EasingFunction as BounceEase;

            if (show)
            {
                ((CompositeTransform)_gridGetScore.RenderTransform).Rotation = 90;
                animateOpacity.To = 1;
                animateRotation.To = 0;
                animateOpacity.BeginTime = TimeSpan.FromMilliseconds(0);
                animateRotation.BeginTime = TimeSpan.FromMilliseconds(500);
                bounce.Bounces = 1;

            }
            else
            {
                animateOpacity.To = 0;
                animateRotation.To = -270;
                animateRotation.BeginTime = TimeSpan.FromMilliseconds(0);
                animateOpacity.BeginTime = TimeSpan.FromMilliseconds(0);
                bounce.Bounces = 0;
            }
        }

        public async Task ShowGetUserScoreUi(bool show)
        {
            SetupSetScoreAnimation(show);
            await _sbGetPlayerScore.ToTask();
        }

        private void ButtonDownScore_Click(object sender, RoutedEventArgs e)
        {


            int scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            if (scoreDelta < 0) scoreDelta = 0;
            if (scoreDelta > 0)
            {
                HighlightPegHole(PlayerFrontScore + scoreDelta, false);
                scoreDelta -= 1;
                _tbScoreToAdd.Text = scoreDelta.ToString();
            }


        }
        private Brush _scoreBrush = (Brush)App.Current.Resources["SelectColor"];
        private void ButtonUpScore_Click(object sender, RoutedEventArgs e)
        {
            int scoreDelta = Convert.ToInt32(_tbScoreToAdd.Text);
            scoreDelta += 1;
            if (scoreDelta > 29) scoreDelta = 29;
            _tbScoreToAdd.Text = scoreDelta.ToString();
            HighlightPegHole(PlayerFrontScore + scoreDelta, true);

        }



        public async void AnimateScoreAsync(PlayerType player, int scoreToAdd)
        {
            List<Task<object>> taskList = AnimateScore(player, scoreToAdd);
            await Task.WhenAll(taskList);

        }

        async Task ICribbageBoardUi.AnimateScore(PlayerType player, int scoreToAdd)
        {
            List<Task<object>> taskList = AnimateScore(player, scoreToAdd);
            await Task.WhenAll(taskList);
        }


        public async Task Hide()
        {
            SetupSetScoreAnimation(false);
            await _sbGetPlayerScore.ToTask();
        }

        public void HideAsync()
        {
            SetupSetScoreAnimation(false);
            _sbGetPlayerScore.Begin();
        }


    }
}
