using CribbageService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;


namespace Cribbage
{
    public sealed partial class ScoreHistoryView : UserControl
    {
        List<Rectangle> _rectangles = new List<Rectangle>();
        List<TextBlock> _textBlocks = new List<TextBlock>();
        List<CardView> _cards = new List<CardView>();
        
        //
        //  state
        PlayerType _player;
        ScoreType _scoreType;
        ScoreInstance _score;
        int _index;
        int _total;
        int _actualScore;
        string _gameScore;


        public string Save()
        {
            
            //
            //  Score Instance and Cards use "," as a seperator.  This is level 2, use "|"

            string s = String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|", 
                                    _player, _scoreType, _score.Save(), _index, _total, _actualScore, _gameScore, StaticHelpers.SerializeFromList(_cards));

            return s;
        }


        public async Task PopulateGrid(string savedState)
        {
            char[] sep = new char[] { '|' };
            string[] tokens = savedState.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Count() < 8) throw new InvalidDataException();
            _player = (PlayerType)Enum.Parse(typeof(PlayerType), tokens[0]);
            _scoreType = (ScoreType)Enum.Parse(typeof(ScoreType), tokens[1]);
            _score = new ScoreInstance(tokens[2]);
            _index = Convert.ToInt32(tokens[3]);
            _total = Convert.ToInt32(tokens[4]);
            _actualScore = Convert.ToInt32(tokens[5]);
            _gameScore = tokens[6];
            _cards = StaticHelpers.DeserializeToList(tokens[7], MainPage.Current.Deck);
            await PopulateGrid();
        }

        public ScoreHistoryView()
        {
            this.InitializeComponent();

            _rectangles.Add(_card0);
            _rectangles.Add(_card1);
            _rectangles.Add(_card2);
            _rectangles.Add(_card3);
            _rectangles.Add(_card4);
            _rectangles.Add(_card5);
            _rectangles.Add(_card6);
            _rectangles.Add(_card7);

            foreach (FrameworkElement el in LayoutRoot.Children)
            {
                try
                {
                    Type t = el.GetType();
                    if (t == typeof(Viewbox))
                    {
                        TextBlock tb = ((Viewbox)el).Child as TextBlock;                    
                        _textBlocks.Add((TextBlock)tb);


                    }
                }
                catch (Exception)
                {
                    
                }
            }

            this.DataContext = this;

        }

        // top text to look like: Computer  Counting Run of 3 
        // bottom text like 3 of 9 points

        public async Task PopulateGrid(List<CardView> cards, ScoreInstance score, PlayerType player, ScoreType scoreType, int index, int total, int actualScore, string gameScore)
        {
            _cards = cards;
            _score = score;
            _player = player;
            _scoreType = scoreType;
            _index = index;
            _total = total;
            _actualScore = actualScore;
            _gameScore = gameScore;
            await PopulateGrid();

            MainPage.Current.StatsView.Stats.Stat(score.ScoreType).UpdateStatistic(player, actualScore);

        }

   

        private async Task PopulateGrid()
        {
            string gameScore = _gameScore.Replace("&", "\n");
            _txtScore.Text = String.Format("{0} Points of {1}", _score.Score, _total);
            _txtScoreType.Text = _scoreType.ToString();
            _txtPlayer.Text = _player.ToString();
            _txtScoreDescription.Text = _score.Description;
            _txtGameScore.Text = gameScore;
            if (_player == PlayerType.Player)
            {
                _rectBackground.Fill = (ImageBrush)Application.Current.Resources["bmBurledMaple"];
                SetTextColor(Colors.Black);

            }
            else
            {
                _rectBackground.Fill = (ImageBrush)Application.Current.Resources["bmWalnut"];
                SetTextColor(Colors.White);
            }


            await AddCardImages(_cards, _scoreType);
            ShowScore(_cards, _score.Cards);
        }

        private void SetTextColor(Color color)
        {
            SolidColorBrush br = new SolidColorBrush(color);
            foreach (TextBlock tb in _textBlocks)
            {
                tb.Foreground = br;
            }
        }

        private void ShowScore(List<CardView> cards, List<int> scoreCards)
        {

            foreach (Rectangle rect in _rectangles)
            {
                if (rect.Tag != null)
                {
                    if (scoreCards.Contains((int)rect.Tag))
                    {
                        Grid.SetRow(rect, 2);
                    }
                }
            }           
        }

        private async Task AddCardImages(List<CardView> cards, ScoreType scoreType)
        {

            if (scoreType == ScoreType.Count)
            {
                for (int i = 0; i < _rectangles.Count; i++)
                {
                    if (i < cards.Count)
                    {
                        await SetCardToBitmap(cards[i], _rectangles[i]);
                    }
                    else
                    {
                        LayoutRoot.Children.Remove(_rectangles[i]);
                    }
                }

                return;
            }
            else if (scoreType == ScoreType.Hand || scoreType == ScoreType.Crib || scoreType == ScoreType.Cut)
            {
                for (int i = 0; i < _rectangles.Count; i++)
                {
                    if (i < 4)
                    {
                        await SetCardToBitmap(cards[i], _rectangles[i]);
                    }
                    else if (i == 6)
                    {
                        await SetCardToBitmap(cards[4], _rectangles[i]);
                    }
                    else
                    {
                        LayoutRoot.Children.Remove(_rectangles[i]);
                    }
                }

                return;
            }

            throw new NotSupportedException();

        }

        private async Task SetCardToBitmap(CardView card, Rectangle rect)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(card.Face.Canvas, (int)rect.Width, (int)rect.Height);
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = renderTargetBitmap;
            rect.Fill = imageBrush;
            rect.Tag = card.Index;
        }

         public async Task Animate(Point point)
         {
             _daX.To = point.X;
             _daY.To = point.Y;
             _daX.Duration = TimeSpan.FromMilliseconds(MainPage.AnimationSpeeds.Medium);
             _daY.Duration = TimeSpan.FromMilliseconds(MainPage.AnimationSpeeds.Medium * point.Y / point.X);
             _daY.BeginTime = _daX.Duration.TimeSpan;

             await _sbAnimateAcrossScreen.ToTask();
         }

        public CompositeTransform Transform
        { 
            get
            {
                return _transform;
            }
        }

        public void EndAnimation()
        {
            _sbAnimateAcrossScreen.Stop();
            Transform.TranslateX = 0;
        }
    }
}
