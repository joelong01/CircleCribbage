using CribbageService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Cribbage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewRoundBoardPage : Page, IViewCallback, IBaseView
    {
        UIClasses _uiClasses = new UIClasses();
        CribbageView _view = null;
        DispatcherTimer _initTimer = new DispatcherTimer();
       

      
        public NewRoundBoardPage()
        {
            this.InitializeComponent();
            _uiClasses.Board = _board;
            _uiClasses.GridDeck = _gridDeck;
            _uiClasses.LayoutRoot = LayoutRoot;
            _uiClasses.CenterGrid = PlayGrid;
            _uiClasses.Dispatcher = Dispatcher;
            _uiClasses.GridPlayer = _gridPlayer;
            _uiClasses.GridComputer = _gridComputer;
            _uiClasses.GridPlayedCards = _gridPlayedCards;
            _uiClasses.GridCrib = _gridCrib;
            _uiClasses.CountControl = _ctrlCount;
            _uiClasses.ShowInstructionsUi = _hintWindow;
            _uiClasses.PlayerSetScoreControl = _board;
            _uiClasses.ViewCallback = this;
            _uiClasses.FriendlyName = "RoundPage";

            _view = new CribbageView(_uiClasses);
            _view.Initialize();

            _initTimer.Interval = TimeSpan.FromMilliseconds(100);
            _initTimer.Tick += InitOnceAsyc;
            _initTimer.Start();



            _board.HideAsync();
        }

        private async void InitOnceAsyc(object sender, object e)
        {
            MainPage.LogTrace.TraceMessageAsync("Callback occurred");
            _initTimer.Stop();
           await _board.Reset();
        }
       
        private async void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double diameter = Math.Min(e.NewSize.Height, e.NewSize.Width);
            LayoutRoot.ColumnDefinitions[2].Width = new GridLength(diameter-200);
            LayoutRoot.ColumnDefinitions[5].Width = new GridLength(_gridDeck.ActualWidth * 5 + 35);

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,  () =>
            {
                _ctrlCount.Locate();

                //
                //  hide all but the grab bars of the stats window
                _ctrlStatsViewTransform.TranslateX = -(_ctrlStatsView.ActualWidth - 20);
                _daHideStats.To = _ctrlStatsViewTransform.TranslateX; 

            });


            
        }
        

        public async void Reset()
        {
            await _board.Reset(); 
        }

        public Rect ScatterBounds()
        {
            Rect rect = Window.Current.Bounds;
            
            double width = _gridDeck.Items[0].ActualWidth * 2;
            double height = _gridDeck.Items[0].ActualHeight * 2;
            double hintWidth = _hintWindow.ActualWidth;

            if (width * height == 0)
            {
                width = 175.0;
                height = 250.0;
            }

            if (hintWidth == 0)
            {
                hintWidth = rect.Width - 51 - rect.Height;
            }

            rect.X = width - hintWidth;
            rect.Y = height;
            rect.Width -= width * 2;
            rect.Height -= height * 2;
            return rect;
        }

        public void SetUIState(UIState uiState)
        {
            this.DataContext = uiState;
        }

        public async Task ShowSomethingFlashy()
        {
            await AnimateAllCardsBackToDeck();

            double angle = 90;
            double delta = 180.0 / 52.0;


            List<Task<object>> taskList = new List<Task<object>>();
            foreach (CardView card in _gridDeck.Items)
            {
                Point to = new Point();
                to.X = Window.Current.Bounds.Width / 2.0 - card.ActualWidth * 0.5;
                to.Y = Window.Current.Bounds.Height / 2.0 - card.ActualHeight * 0.5;
                card.AnimateToTaskList(to, true, 0, taskList);
                angle -= delta;
            }

            double animationTime = 1000;

            foreach (CardView card in _gridDeck.Items)
            {
                Point to = new Point();
                to.X = (_board.ActualWidth * .5 + card.ActualWidth * .5) * Math.Cos(angle) + card.AnimationPosition.X;
                to.Y = (_board.ActualHeight * .5 + card.ActualHeight * .5) * Math.Sin(angle) + card.AnimationPosition.Y;
                card.AnimateToTaskList(to, true, animationTime, taskList);
                card.SetOrientation(CardOrientation.FaceUp, taskList, animationTime);
                card.Rotate(90 + angle, taskList, animationTime);
                angle -= delta;
            }



            await Task.WhenAll(taskList.ToArray());
           
            taskList.Clear();
            foreach (CardView card in _gridDeck.Items)
            {
                card.SetOrientation(CardOrientation.FaceDown, taskList, animationTime);
            }
            await Task.WhenAll(taskList.ToArray());

            _gridDeck.UpdateCardLayout(taskList, MainPage.AnimationSpeeds.VerySlow, true);

            await Task.WhenAll(taskList.ToArray());
        
        }

        public CribbageView GetCommonView()
        {
            return _view;
        }

        private async void OnShowScore(object sender, RoutedEventArgs e)
        {
            await _board.ShowAndWaitForContinue(12);
        
        }
        private  void OnLocateCount(object sender, RoutedEventArgs e)
        {
            _ctrlCount.Show();
            _ctrlCount.Locate();
        
        }

        private async void OnAddScore(object sender, RoutedEventArgs e)
        {
            await AddScoreCollectionToHistory();
        }
        public async Task ShowStats()
        {

            double animationDuration = MainPage.AnimationSpeeds.DefaultFlipSpeed;
            await _sbShowStats.ToTask();
            await _ctrlStatsView.WaitForOk();
            await _sbHideStats.ToTask();


        }

        public void RemindUserToHitContinue(bool start)
        {
            GeneralTransform gt = _hintWindow.TransformToVisual(this);
            Point screenPoint = gt.TransformPoint(new Point(0, 0));
            _daReminder.To = screenPoint.X + 50;

            if (start)
            {
                _sbReminder.Begin();
            }
            else
            {
                _sbReminder.Stop();
            }
        }
    }
}
