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
using Windows.UI.Core;
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
    public class AsycUpdateCardLayout
    {
        DispatcherTimer _timer = new DispatcherTimer();
        CardContainer _container = null;
        bool _rotate = false;
        double _duration = Double.MaxValue;

        public AsycUpdateCardLayout(CardContainer container, double duration = Double.MaxValue, bool rotation = false)
        {
            _container = container;
            _rotate = rotation;
            _duration = duration;
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += AsycUpdate;

        }

        private async void AsycUpdate(object sender, object e)
        {
            _timer.Stop();            
            await _container.UpdateCardLayout(MoveCardOptions.MoveAllAtSameTime, _duration);
        }


        internal void AsyncUpdate()
        {
            _timer.Start();
        }
    }

    //
    //  a class to keep track of cards that are beeing dragged.  we do this so that we can always
    //  boost the drag index when the card is added to the list and reset it when it is pulled from
    //  the list
    public class DragList : List<CardView>
    {
        public DragList() { }
        new public void Add(CardView card)
        {
            //Debug.WriteLine("\tAdd {0}", card.CardName);
            base.Add(card);
            card.BoostZindex();

        }

        new public void Remove(CardView card)
        {
            base.Remove(card);
            card.ResetZIndex();
        }

        new public void Insert(int index, CardView card)
        {
            base.Insert(index, card);
            card.BoostZindex();
        }

        new public void Clear()
        {
            foreach (CardView card in this)
            {
                card.ResetZIndex();
            }
            base.Clear();
        }

        public DragList(DragList list)
        {
            foreach (CardView card in list)
            {
                base.Add(card);  // we don't want to boost the ZIndex when copying
            }
        }

    }

    //
    //  an interface called by the drag and drop code so we can simlulate the DragOver behavior
    public interface IDragAndDropProgress
    {

        void Report(Point value);
        void PointerUp(Point value);
    }

    //
    //  for DragOver
    public class HitTestClass : IDragAndDropProgress
    {
        CardContainer _container = null;

        public HitTestClass(CardContainer container)
        {
            _container = container;

        }

        public void Report(Point value)
        {
            bool hit = _container.HitTest(value);
            _container.Highlight(hit);
        }


        public void PointerUp(Point value)
        {
            _container.Highlight(false);
        }
    }

    public interface ICardDropTarget
    {

        /// <summary>
        ///  called by the drop source to see if it is ok to drop the card.
        ///  the target should check the state to see if it is counting or crib
        ///  if it is crib, it should return true when two cards have been dropped, othewise return false.
        /// </summary>
        /// <param name=e the data></param>
        /// <returns> true if done with the card dropping phase and SetState should be called</returns>
        bool AllowDrop(List<CardView> cards);
        Task<GameState> DropCards(List<CardView> cards);
        Task PostDropCards(GameState state);

        // dropped these cards at this point
        Task<bool> DroppedCards(Point point, List<CardView> cards);

    }


    public enum CardLayout { Stacked, PlayedOverlapped, Full, History };
    public enum MoveCardOptions { MoveAllAtSameTime, MoveOneAtATime };
    public enum SelectState { Selected, NotSelected };

    public class CarddDropEventArgs : EventArgs
    {
        private List<CardView> _cards = new List<CardView>();
        public List<CardView> Cards
        {
            get
            {
                return _cards;

            }
        }


        public bool AllowDrop { get; set; }
        public bool SetState { get; set; }
        public GameState PlayerState { get; set; }

        public CarddDropEventArgs(List<CardView> cards)
        {
            AllowDrop = false;
            foreach (CardView c in cards)
            {
                _cards.Add(c);
            }

            SetState = false;
            PlayerState = GameState.Uninitialized;

        }

        public CarddDropEventArgs(GameState state)
        {
            PlayerState = state;
        }

    }

    public delegate void PreDropCardsEventHandler(object sender, CarddDropEventArgs e);
    public delegate Task DropCardsEventHandler(object sender, CarddDropEventArgs e);
    public delegate Task PostDropCardsEventHandler(object sender, CarddDropEventArgs e);

    public sealed partial class CardContainer : UserControl
    {

        CardLayout _layout = CardLayout.Stacked;
        List<CardView> _items = new List<CardView>();
        int _maxSelected = 0;
        Rect _bounds;

        //
        //  drag and drop support

        private const int MOUSE_MOVE_SENSITIVITY = 5;


        public List<CardView> SelectedCards
        {
            get
            {
                List<CardView> selectedCards = new List<CardView>();
                foreach (CardView card in this.Items)
                {
                    if (card.Selected)
                    {
                        selectedCards.Add(card);
                    }
                }

                return selectedCards;
            }
        }
        CardContainer _dropTarget = null;

        //
        //  highlight support
        Brush _regularBrush;        
        Thickness _regularBorderThickness;
        Thickness _highlightThickness = new Thickness(6.0);


        ICardDropTarget _iDropTarget = null;

        public ICardDropTarget IDropTarget
        {
            get { return _iDropTarget; }
            set { _iDropTarget = value; }
        }

        public bool HitTest(Point point)
        {
            return _bounds.Contains(point);
        }

        public CardContainer()
        {
            this.InitializeComponent();
            _regularBrush = _border.BorderBrush;
            _regularBorderThickness = _border.BorderThickness;
            this.SizeChanged += CardContainer_SizeChanged;


            UIElement mousePositionWindow = Window.Current.Content;
            GeneralTransform gt = this.TransformToVisual(mousePositionWindow);
            Point topLeft = gt.TransformPoint(new Point(0, 0));
            _bounds.X = topLeft.X;
            _bounds.Y = topLeft.Y;
            _bounds.Width = this.ActualWidth;
            _bounds.Height = this.ActualHeight;


        }

        private async void CardContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIElement mousePositionWindow = Window.Current.Content;
            GeneralTransform gt = this.TransformToVisual(mousePositionWindow);
            Point topLeft = gt.TransformPoint(new Point(0, 0));
            _bounds.X = topLeft.X;
            _bounds.Y = topLeft.Y;
            _bounds.Width = this.ActualWidth;
            _bounds.Height = this.ActualHeight;

            if (Items.Count != 0)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateAllCardGrids();

                });
            }
        }

        public async void UpdateAllCardGrids(double duration = Double.MaxValue, bool rotate = false)
        {
            foreach (CardView card in  this.Items)
            {
                SetCardSize(card);
            }

            await this.UpdateCardLayout();
            
        }

        public void Highlight(bool bHighlight)
        {
            if (bHighlight)
            {

                _border.BorderThickness = _highlightThickness;
                var brush = Application.Current.Resources["SelectColor"] as SolidColorBrush;
                _border.BorderBrush = brush;
            }
            else
            {
                _border.BorderThickness = _regularBorderThickness;
                _border.BorderBrush = _regularBrush;
            }
        }


        #region Properties
        public CardContainer DropTarget
        {
            get { return _dropTarget; }
            set
            {
                _dropTarget = value;
            }

        }

        public void ResetSelectedCards()
        {
            foreach (var c in this.Items)
                c.Selected = false;
        }

        public string FriendlyName { get; set; }


        public int MaxSelected
        {
            get { return _maxSelected; }
            set { _maxSelected = value; }
        }

        public bool Selectable { get; set; }

        public List<CardView> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public CardLayout CardLayout
        {
            get { return _layout; }
            set { _layout = value; UpdateCardLayoutAsync(); }
        }
        #endregion

        #region Moving cards around
        public void UpdateCardLayoutAsync(double animationDuration = Double.MaxValue, bool rotate = false)
        {
            if (animationDuration == Double.MaxValue)
                animationDuration = 50;

            if (this.Items.Count == 0) return;

            int prevZIndex = Canvas.GetZIndex(this.Items[0]);

            for (int i = 0; i < this.Items.Count; i++)
            {
                CardView card = this.Items[i];
                GeneralTransform gt = this.TransformToVisual(card);
                Point ptTo = gt.TransformPoint(this.GetCardTopLeft(i));
                card.AnimationPosition = ptTo;
                card.AnimateToAsync(ptTo, rotate, animationDuration);
                if (_layout == CardLayout.PlayedOverlapped)
                {
                    Canvas.SetZIndex(card, prevZIndex++);
                }

            }

        }


        public async Task UpdateCardLayout(MoveCardOptions options = MoveCardOptions.MoveAllAtSameTime, double animationDuration = Double.MaxValue)
        {

            if (animationDuration == Double.MaxValue)
                animationDuration = 250;

            if (this.Items.Count == 0)
                return;


            List<Task<object>> taskList = new List<Task<object>>();
            int prevZIndex = Canvas.GetZIndex(this.Items[0]);
            for (int i = 0; i < this.Items.Count; i++)
            {
                CardView card = this.Items[i];
                GeneralTransform gt = this.TransformToVisual(card);
                Point ptTo = gt.TransformPoint(this.GetCardTopLeft(i));
                card.AnimationPosition = ptTo;
                card.AnimateToTaskList(ptTo, false, animationDuration, taskList);
                if (_layout == CardLayout.PlayedOverlapped)
                {
                    Canvas.SetZIndex(card, prevZIndex++);
                }



            }

            if (options == MoveCardOptions.MoveAllAtSameTime)
                await Task.WhenAll(taskList);
            else
                Task.WaitAll(taskList.ToArray());
        }


        public async Task UpdateCardLayout(MoveCardOptions options, List<Task<object>> tasks, double animationDuration, bool rotate)
        {
            if (animationDuration == Double.MaxValue)
                animationDuration = 250;

            if (this.Items.Count == 0)
                return;

            bool localWhenAll = false;
            List<Task<object>> taskList = tasks;
            if (tasks == null && options == MoveCardOptions.MoveAllAtSameTime)
            {
                localWhenAll = true;
                taskList = new List<Task<object>>();
            }

            int prevZIndex = Canvas.GetZIndex(this.Items[0]);
            for (int i = 0; i < this.Items.Count; i++)
            {
                CardView card = this.Items[i];
                GeneralTransform gt = this.TransformToVisual(card);
                Point ptTo = gt.TransformPoint(this.GetCardTopLeft(i));
                card.AnimationPosition = ptTo;
                card.AnimateToTaskList(ptTo, false, animationDuration, taskList);
                if (_layout == CardLayout.PlayedOverlapped)
                {
                    Canvas.SetZIndex(card, prevZIndex++);
                }



            }

            if (options == MoveCardOptions.MoveAllAtSameTime && localWhenAll)
                await Task.WhenAll(taskList);
            else if (options == MoveCardOptions.MoveOneAtATime)
                Task.WaitAll(taskList.ToArray());


        }

        public void UpdateCardLayout(List<Task<object>> taskList, double animationDuration = Double.MaxValue, bool rotate = false)
        {

            if (animationDuration == Double.MaxValue)
                animationDuration = 50;


            if (this.Items.Count == 0)
                return;

            int prevZIndex = Canvas.GetZIndex(this.Items[0]);

            for (int i = 0; i < this.Items.Count; i++)
            {
                CardView card = this.Items[i];
                GeneralTransform gt = this.TransformToVisual(card);
                Point ptTo = gt.TransformPoint(this.GetCardTopLeft(i));
                card.AnimationPosition = ptTo;
                card.AnimateToTaskList(ptTo, false, animationDuration, taskList);
                if (rotate)
                    card.AnimateRotation += 360;

                if (_layout == CardLayout.PlayedOverlapped)
                {
                    Canvas.SetZIndex(card, prevZIndex++);
                }
            }


            this.UpdateLayout();


        }


        const double RATIO_CARD_WIDTH_TO_HEIGHT = 0.7;

       
        public void SetCardSize(CardView card)
        {
            double height = 125.0; // pick a default for design view
            if (this.ActualHeight != Double.NaN)
            {
                height = Math.Round(this.ActualHeight * .90);
            }

            if (card.CardHeight != height)
            {
                card.CardHeight = height;
                card.CardWidth = Math.Round(height * RATIO_CARD_WIDTH_TO_HEIGHT); ;
                // Debug.WriteLine("CardHeight={0} CardWidth={1}", _cardHeight, _cardWidth);
            }
        }
        public void MoveAllCardsToTargetAsync(CardContainer target, double animationDuration = Double.MaxValue, bool rotate = false, bool transferCard = true)
        {
            for (int i = this.Items.Count - 1; i >= 0; i--)
            {
                CardView card = this.Items[i];
                MoveCardToTargetAsync(card, target, animationDuration, rotate, transferCard);
            }
        }

        public void MoveAllCardsToTarget(CardContainer target, List<Task<object>> taskList, double animationDuration = Double.MaxValue, bool rotate = false, bool transferCard = true)
        {
            for (int i = this.Items.Count - 1; i >= 0; i--)
            {
                CardView card = this.Items[i];
                MoveCardToTarget(card, target, taskList, animationDuration, rotate, transferCard);
            }
        }

        public async Task MoveAllCardsToTarget(CardContainer target, MoveCardOptions moveOptions = MoveCardOptions.MoveAllAtSameTime, double animationDuration = Double.MaxValue, bool rotate = false, bool transferCard = true)
        {
            if (moveOptions == MoveCardOptions.MoveAllAtSameTime)
            {
                List<Task<object>> localList = new List<Task<object>>();
                MoveAllCardsToTarget(target, localList, animationDuration, rotate, transferCard);
                await Task.WhenAll(localList);
                return;
            }

            for (int i = this.Items.Count - 1; i >= 0; i--)
            {
                CardView card = this.Items[i];
                await MoveCardToTarget(card, target, animationDuration, rotate, transferCard);
            }

        }


        public async Task MoveCardToTarget(CardView card, CardContainer target, double animationDuration = Double.MaxValue, bool rotate = false, bool transferCard = true)
        {
            if (this.Items.Contains(card) == false)
            {
                //Debug.WriteLine("Attempting to move Card {0} from {1} to {2} and it is not in {1}", card.CardName, this.FriendlyName, target.FriendlyName);
                return;
            }

            if (animationDuration == Double.MaxValue) animationDuration = MainPage.AnimationSpeeds.Medium;
            Point ptTo = SetupCardForMove(card, target, rotate, transferCard);
            await card.AnimateTo(ptTo, rotate, false, animationDuration);
            if (transferCard) // if we don't transfer the card, UpdateCardLayout() just moves it back... :)
                await this.UpdateCardLayout();

        }

        public void MoveCardToTarget(List<Task<object>> taskList, CardView card, CardContainer target, double animationDuration = Double.MaxValue, bool rotate = false, bool transferCard = true)
        {
            if (this.Items.Contains(card) == false)
            {
                //Debug.WriteLine("Attempting to move Card {0} from {1} to {2} and it is not in {1}", card.CardName, this.FriendlyName, target.FriendlyName);
                return;
            }

            if (animationDuration == Double.MaxValue) animationDuration = MainPage.AnimationSpeeds.Medium;
            Point ptTo = SetupCardForMove(card, target, rotate, transferCard);
            card.AnimateToTaskList(ptTo, rotate, animationDuration, taskList);            

        }

        public void MoveCardToTarget(CardView card, CardContainer target, List<Task<object>> taskList, double animationDuration = Double.MaxValue, bool rotate = false, bool transferCard = true)
        {
            if (animationDuration == Double.MaxValue) animationDuration = MainPage.AnimationSpeeds.Medium;
            Point ptTo = SetupCardForMove(card, target, rotate, transferCard);
            card.AnimateToTaskList(ptTo, rotate, animationDuration, taskList);
            if (transferCard)
                this.UpdateCardLayout(taskList);
        }

        public void MoveCardToTargetAsync(CardView card, CardContainer target, double animationDuration = Double.MaxValue, bool rotate = false, bool transferCard = true)
        {
            if (animationDuration == Double.MaxValue) animationDuration = MainPage.AnimationSpeeds.Medium;
            Point ptTo = SetupCardForMove(card, target, rotate, transferCard);
            card.AnimateToAsync(ptTo, rotate, animationDuration);
            if (transferCard)
                this.UpdateCardLayoutAsync();
        }

        private Point SetupCardForMove(CardView card, CardContainer target, bool rotate, bool transferCard)
        {
            int targetCount = target.Items.Count;

            //
            //  if they are overlapped, make the ZIndex such that they are on top of each other
            if (target.CardLayout == CardLayout.PlayedOverlapped)
            {
                if (targetCount > 0)
                {
                    int zIndex = Canvas.GetZIndex(target.Items.Last());
                    Canvas.SetZIndex(card, zIndex + 1);

                }
            }

            //SetCardSize(card);

            //
            //  set up the animation values so we can do the animation
            GeneralTransform gt = target.TransformToVisual(card);
            Point ptTo = gt.TransformPoint(target.GetCardTopLeft(targetCount));
            card.AnimationPosition = ptTo;
            if (rotate)
                card.AnimateRotation += 360;

            //
            // if we are going to transferownership...
            if (transferCard)
            {
                TransferCard(card, target);
            }
            return ptTo;
        }

        public void TransferCard(CardView card, CardContainer target)
        {
            card.PointerPressed -= this.Card_OnPointerPressed;


            if (target.Selectable)
            {
                card.PointerPressed += target.Card_OnPointerPressed;
                card.DoubleTapped += target.Card_DoubleTapped;
                card.CardSelectionChanged += target.OnCardSelectionChanged;
                
                

            }
            else
            {
                card.PointerPressed -= target.Card_OnPointerPressed;
                card.DoubleTapped -= target.Card_DoubleTapped;
                card.CardSelectionChanged -= OnCardSelectionChanged;


            }

            this.Items.Remove(card);
            target.Items.Add(card);
        }

        List<CardView> _selectedCards = new List<CardView>();
        private void OnCardSelectionChanged(CardView card, bool selected)
        {
            if (selected)
            {
                _selectedCards.Add(card);                
                if (_selectedCards.Count > _maxSelected)
                {
                    _selectedCards[0].Selected = false; // will recurse to use
                }
            }
            else
            {               
                _selectedCards.Remove(card);
            }

            
        }



        //
        //  returns the right point in the coordinate system of the control
        public Point GetCardTopLeft(int index)
        {

            
            double top = Math.Round((this.ActualHeight*0.05));    // .05 == 1/2 * .1 because I scale the card to be 0.9 of the height
            double width = (this.ActualHeight * .9) * RATIO_CARD_WIDTH_TO_HEIGHT;

            Point pt = new Point(0, top);
            if (_layout == CardLayout.Stacked)
            {
                pt.X =Math.Round((this.ActualWidth - width) * 0.5);
                return pt;
            }
            if (_layout == CardLayout.Full)
            {
                double padding = Math.Round((this.ActualWidth - 6.0 * width) / 12.0);
                pt.X = index * width + (index*2 + 1) *padding;
                pt.Y = top;
                return pt;
            }

            if (_layout == CardLayout.PlayedOverlapped)
            {
                double cardWidth = this.ActualWidth / (double)6.0;
                pt.X = index * cardWidth / 2.0;
                pt.Y = top;
                return pt;
            }

            if (_layout == CardLayout.History)
            {
                double padding = 1; // Math.Round((this.ActualWidth - 4.0 * width) / 4);
                pt.X = index * width + (index * 2 + 1) * padding;
                pt.Y = top;
                return pt;
            }

            throw new Exception("bad argument in GetCardLeft");
        }

        #endregion

        #region Pointer_Handlers

        private async void Card_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_maxSelected == 0) return;
            if (!Selectable) return;

            DragList dragList = new DragList();

            try
            {
                HitTestClass hitTestProgress = new HitTestClass(_dropTarget);
                Point pt = await DragAsync(this, (CardView)sender, e, dragList, hitTestProgress);
                if (dragList.Count == 0) return; // probably double tapped it
                if (_dropTarget.HitTest(pt))
                {
                    bool accepted = await _iDropTarget.DroppedCards(pt, dragList);
                    if (accepted)
                    {
                        this.ResetSelectedCards();
                    }
                }
                else
                {
                    // await this.UpdateCardLayout();
                    AsycUpdateCardLayout asyncUppdateCardLayout = new AsycUpdateCardLayout(this);
                    asyncUppdateCardLayout.AsyncUpdate();
                }
            }
            finally
            {
                dragList.Clear();
            }

        }

        private async void Card_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_maxSelected == 0) return;

            await this.MoveCardToTarget(((CardView)sender), _dropTarget, MainPage.AnimationSpeeds.Medium, false, false);
            List<CardView> cards = new List<CardView>();
            cards.Add((CardView)sender);
            bool accepted = await _iDropTarget.DroppedCards(new Point(int.MaxValue, int.MaxValue), cards);
            if (accepted)
            {
                _dropTarget.ResetSelectedCards();
            }

        }

        #endregion

        #region Card_Selection
        //public void SelectCard(CardView card, SelectState state)
        //{
        //    //
        //    //  TODO: make a selected event on the card and subscribe to it so that we can maitain this list.  or get rid
        //    //  of _selectedCards and just loop each time.

        //    if (state == SelectState.Selected)
        //    {

        //        card.Selected = true;
        //        _selectedCards.Add(card);
        //    }
        //    else
        //    {
        //        card.Selected = false;
        //        _selectedCards.Remove(card);

        //    }



        //    if (_selectedCards.Count > _maxSelected)
        //    {
        //        _selectedCards[0].Selected = false;
        //        _selectedCards.RemoveAt(0);

        //    }

        //}
        public void ToggleSelectCard(CardView card)
        {
            if (card.Selected)
            {
                card.Selected = false;
            }
            else
            {
                card.Selected = true;
            }

            if (SelectedCards.Count > _maxSelected)
            {
                this.FirstSelectedCard.Selected = false;
            }

        }

        public void DumpZIndex()
        {
            foreach (CardView c in this.Items)
            {
                Debug.WriteLine("Card: {0} Zindex:{1}", c.CardName, Canvas.GetZIndex(c));
            }
        }

        internal void FlipAllCardsAsync(CardOrientation orientation, double timeout = Double.MaxValue)
        {
            foreach (CardView card in _items)
            {
                card.SetOrientationAsync(orientation, timeout);
            }
        }
        #endregion

        #region Card_Flipping

        public void FlipAllCards(CardOrientation orientation, List<Task<object>> tasks, double animationDuration = Double.MaxValue)
        {
            foreach (CardView card in _items)
            {
                card.SetOrientation(orientation, tasks, animationDuration);
            }
        }

        public async Task FlipAllCards(CardOrientation orientation, double animationDuration = Double.MaxValue)
        {
            List<Task<object>> taskList = new List<Task<object>>();
            FlipAllCards(orientation, taskList, animationDuration);
            await Task.WhenAll(taskList);

        }
        #endregion


        public Task<Point> DragAsync(CardContainer container, CardView card, PointerRoutedEventArgs origE, DragList dragList, IDragAndDropProgress progress = null)
        {
            TaskCompletionSource<Point> taskCompletionSource = new TaskCompletionSource<Point>();
            UIElement mousePositionWindow = Window.Current.Content;
            Point pointMouseDown = origE.GetCurrentPoint(mousePositionWindow).Position;
            card.PushCard(true);
            bool dragging = false;
            if (dragList.Contains(card) == false)
                dragList.Insert(0, card); // card you clicked is always the first one


            PointerEventHandler pointerMovedHandler = null;
            PointerEventHandler pointerReleasedHandler = null;

            pointerMovedHandler = (Object s, PointerRoutedEventArgs e) =>
            {

                Point pt = e.GetCurrentPoint(mousePositionWindow).Position;
                Point delta = new Point();
                delta.X = pt.X - pointMouseDown.X;
                delta.Y = pt.Y - pointMouseDown.Y;

                CardView localCard = (CardView)s;
                bool reorderCards = false;
                //
                //  fixup our lists
                foreach (var c in this.SelectedCards)
                {
                    if (dragList.Contains(c) == false)
                    {
                        dragList.Add(c);
                    }
                }

                if (dragList.Contains(localCard) == false)
                    dragList.Add(localCard);

                if (dragList.Count > _maxSelected)
                {
                    CardView c = this.FirstSelectedCard;
                    c.Selected = false;
                    dragList.Remove(c);
                    c.UpdateLayout();
                }



                if (Math.Abs(delta.X - MOUSE_MOVE_SENSITIVITY) > 0 || Math.Abs(delta.Y - MOUSE_MOVE_SENSITIVITY) > 0)
                {
                    dragging = true;
                }

                //
                //  check to see if we have moved out of the container in the Y direction
                if (container.HitTest(pt))
                {
                    reorderCards = true;
                }

                if (dragList.Count > 1)
                {
                    reorderCards = false;
                    CardView otherCard = dragList[0];
                    double cardWidth = card.CardWidth;
                    if (card.Index == otherCard.Index)
                        otherCard = dragList[1];

                    //
                    //  this moves the card to make space for reordering
                    int left = (int)(card.AnimationPosition.X - otherCard.AnimationPosition.X);

                    if (left > cardWidth)
                    {
                        otherCard.AnimateToReletiveAsync(new Point(left - cardWidth, 0), 0);
                        return;
                    }
                    else if (left < -card.CardWidth)
                    {
                        otherCard.AnimateToReletiveAsync(new Point(left + cardWidth, 0), 0);
                        return;
                    }

                }

                if (progress != null)
                {
                    progress.Report(pt);
                }

                foreach (CardView c in dragList)
                {
                    c.AnimateToReletiveAsync(delta);
                }

                if (reorderCards)
                {
                    int indexOfDraggedCard = container.Items.IndexOf(card);

                    if (delta.X > 0)
                    {
                        if (indexOfDraggedCard < container.Items.Count - 1)
                        {
                            CardView cardToMove = container.Items[indexOfDraggedCard + 1];
                            if (card.AnimationPosition.X + card.CardWidth * 0.5 > cardToMove.AnimationPosition.X)
                            {
                                cardToMove.AnimateToReletiveAsync(new Point(-card.CardWidth, 0), MainPage.AnimationSpeeds.VeryFast);
                                container.Items.Remove(card);
                                container.Items.Insert(container.Items.IndexOf(cardToMove) + 1, card);
                            }
                        }
                    }
                    else //moving left
                    {

                        if (indexOfDraggedCard > 0)
                        {
                            CardView cardToMove = container.Items[indexOfDraggedCard - 1];
                            if (card.AnimationPosition.X - card.CardWidth * 0.5 < cardToMove.AnimationPosition.X)
                            {
                                cardToMove.AnimateToReletiveAsync(new Point(card.CardWidth, 0), MainPage.AnimationSpeeds.VeryFast);
                                container.Items.Remove(card);
                                container.Items.Insert(container.Items.IndexOf(cardToMove), card);
                            }
                        }


                    }
                }


                pointMouseDown = pt;

            };

            pointerReleasedHandler = (Object s, PointerRoutedEventArgs e) =>
            {
                CardView localCard = (CardView)s;
                localCard.PointerMoved -= pointerMovedHandler;
                localCard.PointerReleased -= pointerReleasedHandler;
                localCard.ReleasePointerCapture(origE.Pointer);
                localCard.PushCard(false);
                if (!dragging)
                {
                    ToggleSelectCard(card);
                    dragList.Clear();
                }



                Point exitPoint = e.GetCurrentPoint(mousePositionWindow).Position;

                if (progress != null)
                {
                    progress.PointerUp(exitPoint);
                }

                //if (container.HitTest(exitPoint) == true) // you landed where you started
                //    container.UpdateCardLayoutAsync(500, false);

                //
                //  returns the point that the mouse was released.  the _selectedCards list
                //  will have the cards that were selected.  if dragging occurred, the card(s)
                //  will be in the _draggingList
                taskCompletionSource.SetResult(exitPoint);
            };

            card.CapturePointer(origE.Pointer);
            card.PointerMoved += pointerMovedHandler;
            card.PointerReleased += pointerReleasedHandler;
            return taskCompletionSource.Task;
        }




        public CardView FirstSelectedCard
        {
            get
            {
                foreach (CardView card in this.Items)
                {
                    if (card.Selected)
                        return card;
                }

                return null;
            }
        }
    }

}
