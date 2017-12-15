using CribbageService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


namespace Cribbage
{



    public enum CardOrientation { FaceDown, FaceUp };
    public enum ZIndexBoost { SmallBoost, LargeBoost};

    public delegate void CardSelectionChangedDelegate(CardView card, bool selected);

    public sealed partial class CardView : UserControl
    {
        private bool _selected = false;
        CardData _cardData = new CardData();
        VectorCard _cardFace;
        double _cardMargin = 2.0;
        CardOrientation _orientation = CardOrientation.FaceDown;
        string _displayName = "";

        public event CardSelectionChangedDelegate CardSelectionChanged;

        public string Serialize()
        {
            string s = String.Format("{0}\\{1}\\{2}",  _cardData.Name, _cardData.Owner, _orientation);
            return s;
        }



        public string DisplayName
        {
            get
            {
                if (_displayName == "")
                {
                    int n = this.CardName.IndexOf("Of");
                    string s = CardName.Substring(0, n);
                    _displayName = String.Format("{0} of {1}", s, Suit);

                }
                return _displayName;
            }

        }

        public CardOrientation Orientation
        {
            get { return _orientation; }
        }




        #region CONSTANTS
        public const double CARD_HEIGHT = 175.013;
        public const double CARD_WIDTH = 250.55;

        #endregion


        #region CONSTRUCTORS

        public CardView(VectorCard card, CardData cardData, CardOrientation orientation)
        {
            Init();

            _cardFace = card;
            _cardData = cardData;
            _canvasFrontOfCard.Children.Clear(); // pull the AceOfClubs that is there for design purposes
            _canvasFrontOfCard.Children.Add(_cardFace.Canvas);
            _cardFace.Canvas.Width = CARD_WIDTH;
            _cardFace.Canvas.Height = CARD_HEIGHT;
            _orientation = orientation;

            Reset();

        }
        public CardView()
        {
            _orientation = CardOrientation.FaceDown;
            Init();
        }

        public Canvas CardCanvas
        {
            get
            {

                return _canvasFrontOfCard;
            }

        }

        public VectorCard Face { get { return _cardFace; } }

        public CardView(CardView card)
        {
            _cardData = card.Data;
            _cardFace = card.Face;
            Init();
            this.SetOrientationAsync(card.Orientation, 2);
            this.Selected = card.Selected;

        }

        public CardView(string serializedCard)
        {

            throw new NotImplementedException();

            //CardView c = StaticHelpers.CardFromString(serializedCard);
            //_cardData = new CardData(c.Data);
            //Init();


        }

        public void Init()
        {

            this.InitializeComponent();
            Selected = false;
            Canvas.SetZIndex(this, 100);



        }
        #endregion

        #region METHODS





        public Point RTO
        {
            get
            {
                return LayoutRoot.RenderTransformOrigin;
            }

            set
            {
                LayoutRoot.RenderTransformOrigin = value;
            }
        }

        public async Task Rotate(double angle, bool callStop)
        {

            _daRotate.To = angle;
            await RunStoryBoard(_sbRotateCard, false);

        }

        public void Rotate(double angle, List<Task<object>> taskList, double duration)
        {
            _daRotate.To = angle;
            _daRotate.Duration = TimeSpan.FromMilliseconds(duration);
            taskList.Add(_sbRotateCard.ToTask());
        }

        public override string ToString()
        {
            return String.Format("CardName:{0}\t\t Index:{1}\t Value:{2}\t Rank:{3}", _cardData.Name, _cardData.Index, _cardData.Value, _cardData.Rank);
        }


        public void SetOrientation(CardOrientation orientation, List<Task<object>> taskList, double animationDuration = Double.MaxValue)
        {
            if (orientation == _orientation)
                return;

            SetupFlipAnimationData(orientation, animationDuration);
            taskList.Add(_sbFlip.ToTask());
        }

        public async Task SetOrientation(CardOrientation orientation, double animationDuration = Double.MaxValue)
        {
            if (_orientation == orientation)
                return;

            _orientation = orientation;

             if (animationDuration == Double.MaxValue)
                animationDuration = MainPage.AnimationSpeeds.DefaultFlipSpeed;


            _daFlipBack.Duration = new Duration(TimeSpan.FromMilliseconds(animationDuration * .5));
            _daFlipFront.Duration = new Duration(TimeSpan.FromMilliseconds(animationDuration * .5));


            if (_orientation == CardOrientation.FaceDown)
            {

                _daFlipFront.To = 90;
                _daFlipBack.To = -90;
                await _sbFlip.ToTask();


                _daFlipBack.To = 0;
                await _sbFlip.ToTask();


            }
            else
            {
                _daFlipBack.To = -90;
                _daFlipFront.To = 90;
                await _sbFlip.ToTask();

                _daFlipFront.To = 0;
                await _sbFlip.ToTask();

            }

        }

        public void SetOrientationAsync(CardOrientation orientation, double animationDuration = Double.MaxValue)
        {

            if (orientation == _orientation)
                return;



            animationDuration = SetupFlipAnimationData(orientation, animationDuration);

            if (animationDuration != MainPage.AnimationSpeeds.NoAnimation)
                _sbFlip.Begin();

        }

        private double SetupFlipAnimationData(CardOrientation orientation, double animationDuration)
        {
            _orientation = orientation;

            if (animationDuration == Double.MaxValue)
                animationDuration = MainPage.AnimationSpeeds.DefaultFlipSpeed;


            if (_orientation == CardOrientation.FaceDown)
            {
                _daFlipBack.To = 0;
                _daFlipFront.To = 90;

                _daFlipBack.BeginTime = new TimeSpan(0, 0, 0, 0, (int)(animationDuration * .5));
                _daFlipFront.BeginTime = new TimeSpan(0);

            }
            else
            {
                _daFlipBack.To = -90;
                _daFlipFront.To = 0;

                _daFlipFront.BeginTime = new TimeSpan(0, 0, 0, 0, (int)(animationDuration * .5));
                _daFlipBack.BeginTime = new TimeSpan(0);
            }


            _daFlipBack.Duration = new Duration(TimeSpan.FromMilliseconds(animationDuration * .5));
            _daFlipFront.Duration = new Duration(TimeSpan.FromMilliseconds(animationDuration * .5));
            return animationDuration;
        }

        public void AnimateToReletiveAsync(Point to, double milliseconds = 0)
        {
            MoveCardDoubleAnimationX.To += to.X;
            MoveCardDoubleAnimationY.To += to.Y;
            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardStoryboard.Begin();
        }

        public async Task AnimateToReletiveTask(Point to, double milliseconds = 0)
        {
            MoveCardDoubleAnimationX.To += to.X;
            MoveCardDoubleAnimationY.To += to.Y;

            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(milliseconds);

            await RunStoryBoard(MoveCardStoryboard, false, milliseconds);
        }

        public Point AnimationPosition
        {
            get
            {
                double x = (double)MoveCardDoubleAnimationX.To;
                double y = (double)MoveCardDoubleAnimationY.To;
                return new Point(x, y);
            }
            set
            {
                MoveCardDoubleAnimationX.To = value.X;
                MoveCardDoubleAnimationY.To = value.Y;
            }
        }

        public double AnimateRotation
        {
            get
            {
                return (double)MoveCardDoubleAnimationAngle.To;
            }
            set
            {
                MoveCardDoubleAnimationAngle.To = value;
            }
        }

        public double AnimateOpacity
        {
            get
            {
                return (double)_daAnimateOpacity.To;
            }
            set
            {
                _daAnimateOpacity.To = value;

            }
        }

        public Task<object> GetAnimationTask(double milliseconds)
        {
            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationAngle.Duration = TimeSpan.FromMilliseconds(milliseconds);
            return MoveCardStoryboard.ToTask();

        }

        public async Task AnimateTo(Point to, bool rotate, bool callStop, double milliseconds)
        {
            this.AnimationPosition = to;

            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationAngle.Duration = TimeSpan.FromMilliseconds(milliseconds);



            if (rotate)
                MoveCardDoubleAnimationAngle.To += 360;

            await RunStoryBoard(MoveCardStoryboard, callStop, milliseconds);
        }

        public void AnimateToAsync(Point to, bool rotate, double milliseconds)
        {
            MoveCardDoubleAnimationX.To = to.X;
            MoveCardDoubleAnimationY.To = to.Y;

            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationAngle.Duration = TimeSpan.FromMilliseconds(milliseconds);

            if (rotate)
                MoveCardDoubleAnimationAngle.To += 360;

            MoveCardStoryboard.Begin();
        }

        public void AnimateToTaskList(Point to, bool rotate, double milliseconds, List<Task<object>> tasks)
        {
            MoveCardDoubleAnimationX.To = to.X;
            MoveCardDoubleAnimationY.To = to.Y;

            MoveCardDoubleAnimationX.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationY.Duration = TimeSpan.FromMilliseconds(milliseconds);
            MoveCardDoubleAnimationAngle.Duration = TimeSpan.FromMilliseconds(milliseconds);

            if (rotate)
                MoveCardDoubleAnimationAngle.To += 360;

            tasks.Add(MoveCardStoryboard.ToTask());
        }

        static public Task RunStoryBoard(Storyboard sb, bool callStop = true, double ms = 500, bool setDuration = true)
        {
            return StaticHelpers.RunStoryBoard(sb, callStop, ms, setDuration);
        }
        public void PushCard(bool shrink)
        {



            if (shrink)
            {
                _daScaleCardX.To = .98;
                _daScaleCardY.To = .98;
            }
            else
            {
                _daScaleCardX.To = 1.0;
                _daScaleCardY.To = 1.0;
            }

            _sbScaleCard.Begin();



        }
        public static int CompareCardsByRank(CardView x, CardView y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're 
                    // equal.  
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y 
                    // is greater.  
                    return -1;
                }
            }
            else
            {
                // If x is not null... 
                // 
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the  
                    // lengths of the two strings. 
                    // 
                    return x.Rank - y.Rank;


                }
            }


        }

        public static int CompareCardsByIndex(CardView x, CardView y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're 
                    // equal.  
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y 
                    // is greater.  
                    return -1;
                }
            }
            else
            {
                // If x is not null... 
                // 
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {

                    //
                    //  sorted largest to smallest for easier destructive iterations
                    return (y.Index - x.Index);


                }
            }


        }
        public async Task AnimateFade(double opacity)
        {
            _daAnimateOpacity.To = opacity;
            _daAnimateOpacity.Duration = new Duration(TimeSpan.FromMilliseconds(MainPage.AnimationSpeeds.Medium));
            await RunStoryBoard(_sbAnimateOpacity, false);

        }
        public void AnimateFadeAsync(double opacity)
        {
            _daAnimateOpacity.To = opacity;
            _daAnimateOpacity.Duration = new Duration(TimeSpan.FromMilliseconds(MainPage.AnimationSpeeds.Medium));
            _sbAnimateOpacity.Begin();

        }

        public void AnimateFade(double opacity, List<Task<object>> tasks)
        {
            _daAnimateOpacity.To = opacity;
            _daAnimateOpacity.Duration = new Duration(TimeSpan.FromMilliseconds(MainPage.AnimationSpeeds.Medium));
            tasks.Add(_sbAnimateOpacity.ToTask());

        }
        #endregion

        #region PROPERTIES
        public double CardMargin
        {
            get { return _cardMargin; }
            set
            {
                _cardMargin = value;
                LayoutRoot.ColumnDefinitions[0].Width = new GridLength(_cardMargin, GridUnitType.Pixel);
                LayoutRoot.ColumnDefinitions[LayoutRoot.ColumnDefinitions.Count - 1].Width = new GridLength(_cardMargin, GridUnitType.Pixel);
                LayoutRoot.RowDefinitions[0].Height = new GridLength(_cardMargin, GridUnitType.Pixel);
                LayoutRoot.RowDefinitions[LayoutRoot.RowDefinitions.Count - 1].Height = new GridLength(_cardMargin, GridUnitType.Pixel);
            }
        }

        public double CardWidth
        {
            get
            {
                return this.ActualWidth;
            }
            set
            {
                this.Width = value;
                LayoutRoot.Width = value;
                UpdateLayout();
            }

        }

        public double CardHeight
        {
            get
            {
                return this.ActualHeight;
            }
            set
            {
                this.Height = value;
                LayoutRoot.Height = value;
                UpdateLayout();
            }

        }

        public double AnimatedOpacity
        {
            get
            {
                return (double)_daAnimateOpacity.To;

            }

        }
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    if (_border == null)
                        return;
                    Visibility visibility = _selected ? Visibility.Visible : Visibility.Collapsed;
                    _border.Visibility = visibility;
                    _gridCheck.Visibility = visibility;

                    if (CardSelectionChanged != null)
                    {

                        CardSelectionChanged(this, value);

                    }
                }

            }

        }

        public Owner Owner
        {
            get
            {
                return _cardData.Owner;
            }
            set
            {
                _cardData.Owner = value;
            }

        }
        public int Value
        {
            get
            {
                return _cardData.Value;
            }
            set
            {

                _cardData.Value = value;
            }
        }
        public int Rank
        {
            get
            {
                return _cardData.Rank;
            }
            set
            {

                _cardData.Rank = value;
            }
        }
        public int Index
        {
            get
            {
                return _cardData.Index;
            }
            set
            {

                _cardData.Index = value;
            }
        }
        public CardData Data
        {
            get
            {
                return _cardData;
            }
        }
        public string CardName
        {
            get
            {
                return _cardData.Name.ToString();
            }

        }
        public Suit Suit
        {
            get
            {
                return _cardData.Suit;
            }
            set
            {
                _cardData.Suit = value;
            }

        }
        bool _isEnabled = true;
        new public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;

                base.IsEnabled = _isEnabled;
                if (_isEnabled)
                {
                    AnimateOpacity = 1.0;
                }
                else
                {
                    AnimateOpacity = 0.5;
                }
                _daAnimateOpacity.Duration = new Duration(TimeSpan.FromMilliseconds(250));
                _sbAnimateOpacity.Begin();
            }
        }


        public ScaleTransform ZoomTranform
        {
            get
            {
                try
                {
                    return _cardFace.Canvas.RenderTransform as ScaleTransform;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            set
            {
                _cardFace.Canvas.RenderTransform = value;
                _canvasBackOfCard.RenderTransform = value;

            }
        }



        internal void Reset()
        {
            Owner = Owner.Shared;
            this.SetOrientationAsync(CardOrientation.FaceDown, 2);
            Canvas.SetZIndex(this, 100);
            _daScaleCardX.To = 1.0; ;
            _daScaleCardY.To = 1.0;
            _sbScaleCard.Duration = new Duration(TimeSpan.FromMilliseconds(0));
            _sbScaleCard.Begin();

            if (Rank == 13)
                _rectAceKing.Fill = new SolidColorBrush(Colors.Red);
            if (Rank == 1)
                _rectAceKing.Fill = new SolidColorBrush(Colors.Green);
        }

        #endregion




        internal async Task AnimateScale(double scale, double duration = double.MaxValue)
        {
            if (duration == double.MaxValue)
                duration = 500;

            _sbScaleCard.Duration = new Duration(TimeSpan.FromMilliseconds(duration));
            _daScaleCardX.To = scale;
            _daScaleCardY.To = scale;
            await _sbScaleCard.ToTask();
        }

        internal void AnimateScale(List<Task<object>> taskList, double scale, double duration = double.MaxValue)
        {

            if (duration == double.MaxValue)
                duration = 500;

            _sbScaleCard.Duration = new Duration(TimeSpan.FromMilliseconds(duration));
            _daScaleCardX.To = scale;
            _daScaleCardY.To = scale;
            taskList.Add(_sbScaleCard.ToTask());
        }

        internal void BoostZindex(ZIndexBoost boost = ZIndexBoost.LargeBoost)
        {
            int zIndex = Canvas.GetZIndex(this);
            zIndex += 1000;
            if (boost == ZIndexBoost.LargeBoost) zIndex += 1000;

            //Debug.WriteLine("Boosting ZIndex.  Card: {0} ZIndex:{1}", this.CardName, zIndex + 500);
            Canvas.SetZIndex(this, zIndex);

            if (zIndex > 2000)
                _rectZIndex.Fill = new SolidColorBrush(Colors.Red);
            else
                _rectZIndex.Fill = new SolidColorBrush(Colors.Yellow);
        }

        internal void ResetZIndex()
        {
            int zIndex = Canvas.GetZIndex(this);


            while (zIndex > 1000)
            {
                zIndex -= 1000;
                //Debug.WriteLine("Resetting ZIndex.  Card: {0} ZIndex:{1}", this.CardName, zIndex - 500);
                Canvas.SetZIndex(this, zIndex);

            }

            if (zIndex < 200)
                _rectZIndex.Fill = new SolidColorBrush(Colors.Green);
            else
                _rectZIndex.Fill = new SolidColorBrush(Colors.Teal);

        }



        public UIElement FullFace
        {
            get
            {
                return this.LayoutRoot;
            }
        }
    }


}
