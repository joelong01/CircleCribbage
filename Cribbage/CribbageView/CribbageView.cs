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


    //
    //  this is the view that the "base views" (CircleLayoutPage and SquarePage) delegate their logic too for shared implementation
    public partial class CribbageView : ICribbageUX, ICardDropTarget, ICommonView
    {

        UIClasses _uiClasses = null;                // passed in from the base views at construction
        ClientStateMachine _stateMachine = null;    // passed in from MainPage during NewGame, switching view, or loading a game
        HandsFromServer _hfs = null;                // passed in from MainPage 

        // timer to remind the user that it is there turn
        // TODO: needed?
        DispatcherTimer _timerTurnReminder = new DispatcherTimer();

        //
        //  Stats
        int _pointsPlayerCountedThisTurn = 0;
        int _pointsComputerCountedThisTurn = 0;


        //
        //  passed in state accessor properited     
        #region UI State properties
        public ICribbageBoardUi Board
        {
            get { return _uiClasses.Board; }
        }
        public IViewCallback ViewCallback
        {
            get { return _uiClasses.ViewCallback; }
        }
        public CardContainer GridDeck
        {
            get { return _uiClasses.GridDeck; }
        }
        public Grid LayoutRoot
        {
            get { return _uiClasses.LayoutRoot; }
        }
        public Grid CenterGrid
        {
            get { return _uiClasses.CenterGrid; }
        }
        public Deck Deck
        {
            get { return _uiClasses.Deck; }
            set { _uiClasses.Deck = value; }
        }
        public CoreDispatcher Dispatcher
        {
            get { return _uiClasses.Dispatcher; }
        }
        public CardContainer GridPlayer
        {
            get { return _uiClasses.GridPlayer; }
        }
        public CardContainer GridComputer
        {
            get { return _uiClasses.GridComputer; }
        }
        public CardContainer GridPlayedCards
        {
            get { return _uiClasses.GridPlayedCards; }
        }
        public CardContainer GridCrib
        {
            get { return _uiClasses.GridCrib; }
        }
        public HandsFromServer Hfs
        {
            get { return _hfs; }
            set { _hfs = value; }
        }
        public CountCtrl CountControl
        {
            get { return _uiClasses.CountControl; }
        }
        public IShowInstructionsAndHistoryController HintWindow
        {
            get { return this as IShowInstructionsAndHistoryController; }
        }

        public IShowInstructionsUi ShowInstructionsUi
        {
            get { return _uiClasses.ShowInstructionsUi; }
        }

        public IPlayerSetScore PlayerSetScoreControl
        {
            get { return _uiClasses.PlayerSetScoreControl; }
        }
        public PegScore PlayerScore
        {
            get { return _uiClasses.PlayerScore; }
        }
        public PegScore ComputerScore
        {
            get { return _uiClasses.ComputerScore; }
        }
        public ClientStateMachine StateMachine
        {
            get { return _stateMachine; }
            set { _stateMachine = value; }
        }
        #endregion


        public CribbageView(UIClasses uiClases)
        {
            _uiClasses = uiClases;
            _timerForFlyingScore.Tick += OnFireScoreAnimation;
        }

        public ICribbageBoardUi GetUiStateCallback()
        {
            return _uiClasses.Board;
        }
        public Settings Settings
        {
            get
            {
                return MainPage.Current.Settings;
            }
        }


        public void Initialize()
        {
            CountControl.DataContext = CountControl;            
            MainPage.Current.AppBar.IsOpen = true;


            GridPlayer.DropTarget = GridPlayedCards; // UI element to drop the cards on            
            GridPlayer.IDropTarget = this;   // who to tell the cards have been droppped
        }


        public async Task AddCardsToGrid()
        {
            int row = 0;
            int col = 0;

            GridDeck.SetCardSize(Deck.Cards[0]);
            LayoutRoot.UpdateLayout();


            foreach (CardView card in Deck.Cards)
            {
                GridDeck.SetCardSize(card);
                card.Reset();
                card.HorizontalAlignment = HorizontalAlignment.Left;
                card.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                Grid.SetRowSpan(card, 8);         //this is required so the cards don't get clipped
                Grid.SetColumnSpan(card, 8);      //this is required so the cards don't get clipped
                Canvas.SetZIndex(card, 100 + card.Index);
                if (LayoutRoot.Children.Contains(card) == false)
                {
                    GridDeck.Items.Add(card);
                    LayoutRoot.Children.Add(card);
                }
            }
            List<Task<object>> taskList = new List<Task<object>>();
            ScatterCards(GridDeck.Items, 0, taskList);
            await Task.WhenAll(taskList);
            await PostUpdateAllCards(MainPage.AnimationSpeeds.Slow, true);
        }

        private async Task PostUpdateAllCards(double duration = Double.MaxValue, bool rotate = false)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateAllCardGrids(duration, rotate);

            });
        }

        public async void UpdateAllCardGrids(double duration = Double.MaxValue, bool rotate = false)
        {
            foreach (CardView card in Deck.Cards)
            {
                GridDeck.SetCardSize(card);
            }

            List<Task<object>> taskList = new List<Task<object>>();
            GridDeck.UpdateCardLayout(taskList, duration, rotate);
            GridPlayer.UpdateCardLayout(taskList, duration, rotate);
            GridComputer.UpdateCardLayout(taskList, duration, rotate);
            GridPlayedCards.UpdateCardLayout(taskList, duration, rotate);
            GridCrib.UpdateCardLayout(taskList, duration, rotate);
            await Task.WhenAll(taskList);
        }


        #region ICribUx


        public PlayerType OnPickCard()
        {
            MainPage.LogTrace.TraceMessageAsync("");
            throw new NotImplementedException();
        }

        public async Task OnComputerGiveToCrib(List<CardView> cards)
        {
            GridPlayer.MaxSelected = 0;
            await AnimateSelectComputerCribCards(cards);

            //
            //  reset the counting points
            _pointsPlayerCountedThisTurn = 0;
            _pointsComputerCountedThisTurn = 0;
        }
        public async Task OnDeal(HandsFromServer hfs, double duration = Double.MaxValue)
        {
            GridDeck.Items.Remove(hfs.SharedCard);
            GridDeck.Items.Insert(0, hfs.SharedCard);
            hfs.PlayerCards.Sort(CardView.CompareCardsByRank);
            Hfs = hfs;
            await DealAnimation(hfs, duration);
        }

        public async Task OnPlayerGiveToCrib(int maxCards, PlayerType cribOwner)
        {
            var tcs = new TaskCompletionSource<object>();
            GridPlayer.MaxSelected = maxCards;
            string instructions = string.Format("Drag and drop {0} card{1}into the middle window.\n\nYou can swipe up and click on the Suggestion to get a hint.\n", maxCards, maxCards > 1 ? "s " : " ");
            if (StateMachine.Dealer == PlayerType.Player)
                instructions += "\nIt is your crib.";
            else
                instructions += "\nIt is the computer's crib.";

            ShowHintWindowAsync(true, false, instructions);            
            tcs.SetResult(null);
            await tcs.Task;
        }

        private void ShowHintWindowAsync(bool show, bool closeWithTimer, string message)
        {
            HintWindow.ShowAsync(show,closeWithTimer, message);
        }

        private async Task ShowHintWindow(bool show, bool closeWithTimer, string message)
        {
            await HintWindow.Show(show, closeWithTimer, message);
        }

        public async Task OnSendCardsToCrib()
        {
            await OnAnimateMoveCardsToCrib();
            await ShowSharedCard();
        }

        public async Task ShowSharedCard()
        {
            GridDeck.Items.Remove(Hfs.SharedCard);
            GridDeck.Items.Insert(0, Hfs.SharedCard);
            await Hfs.SharedCard.SetOrientation(CardOrientation.FaceUp);
        }

        public void OnPlayerCountCard()
        {
            string instructions = "Drag and drop one card into the middle window.\n\nYou can swipe up and click on the Suggestion button to get a hint.\n\nIf your card is translucent, it is not eligible to be played.\n";
            ShowHintWindowAsync(true, false, instructions);            
            GridPlayer.MaxSelected = 1;
        }

        
        public async Task OnCountCard(CardView card, PlayerType currentPlayer, CountingData countingData)
        {
            string message = "";
            if (countingData.Score > 0)
            {
                message = String.Format("{0} Scored Points\n\n{1}", currentPlayer == PlayerType.Player ? "You" : "The Computer", countingData.ScoreStory.Format());
                if (currentPlayer == PlayerType.Player)
                    _pointsPlayerCountedThisTurn++;
                else
                    _pointsComputerCountedThisTurn++;

            }
            if (currentPlayer == PlayerType.Computer)
            {
                //
                //  if it is the player, the card gets moved there via drag and drop
                await GridComputer.MoveCardToTarget(card, GridPlayedCards, MainPage.AnimationSpeeds.Medium);
            }


            if (card.Orientation == CardOrientation.FaceDown)
                await card.SetOrientation(CardOrientation.FaceUp);


            if (countingData.Score > 0)
                await AddToScoreHistory(GridPlayedCards.Items, countingData.ScoreStory, currentPlayer);


            UpdateCount(countingData.CurrentCount);


            if (countingData.ResetCount) // the card we just dropped hit 31 or a Go
            {
                UpdateCount(countingData.CountBeforeReset);
                await OnCountResetUpdateUi();
            }


            if (countingData.NextPlayer == PlayerType.Player)
            {
                GridPlayer.MaxSelected = 1;
                message += "Your Turn.\nDrag and drop one card into the middle window";

            }
            else
            {
                GridPlayer.MaxSelected = 0;
            }

            if (message != "")
            {
                ShowHintWindowAsync(true, false, message);            
            }
        }

        public async Task OnCountingEnded(int playerScore, int computerScore)
        {
            UpdateCount(0);

            HintWindow.InsertScoreSummary(ScoreType.Count, playerScore, computerScore);
            await AnimateCardsBackToOwner();
          
        }

        public async Task<int> OnGetPlayerHandScore(int actualScore = int.MaxValue)
        {
            string message = "Use the up and down arrows to specify your score. Then click the check.  There are many options in Settings on how this works.";            
            await ShowHintWindow(true, false, message);
            int playerScore = await PlayerSetScoreControl.ShowAndWaitForContinue(actualScore);
            await PlayerSetScoreControl.Hide();
            return playerScore;
        }
        //
        //  Helper function where we can prompt the user with a message.
        //  we have this helper function so we can give a visual cue to the user
        //  to let them know they have to click on the Continue. button.

        DispatcherTimer _hintDispatchTimer = null;
        public async Task HintWindow_ShowAndWait(string message)
        {
            if (_hintDispatchTimer == null)
            {
                _hintDispatchTimer = new DispatcherTimer();
                _hintDispatchTimer.Tick += HintReminder_Tick;
                _hintDispatchTimer.Interval = TimeSpan.FromSeconds(5);
            }
            _hintDispatchTimer.Start();
            await HintWindow.ShowAndWait(message);
            ViewCallback.RemindUserToHitContinue(false);
            _hintDispatchTimer.Stop();
        }

        private void HintReminder_Tick(object sender, object e)
        {
            ViewCallback.RemindUserToHitContinue(true);
            _hintDispatchTimer.Stop();

        }

        public async Task OnComputerScoreHand(int scoreToAdd, bool promptUser, ScoreCollection scores)
        {
           if (promptUser)
            {
                string message = String.Format("Computer's Score\n{0}\nHit Continue.", scores.Format(false, false, true));
                await HintWindow_ShowAndWait(message);           
            }

            HintWindow.InsertScoreSummary(ScoreType.Hand, 0, scoreToAdd);
            await StateMachine.UiState.AddScore(PlayerType.Computer, scoreToAdd);
            await AddToScoreHistory(GridComputer.Items, scores, PlayerType.Computer);
        }


        public async Task OnComputerScoreCrib(int scoreToAdd, ScoreCollection scores)
        {
            string message = String.Format("Computer's Crib Score\n{0}\nHit Continue.", scores.Format(false, false, true));
            await HintWindow_ShowAndWait(message);

            await StateMachine.UiState.AddScore(PlayerType.Computer, scoreToAdd);            
            await AddToScoreHistory(GridComputer.Items, scores, PlayerType.Computer);            
            HintWindow.InsertScoreSummary(ScoreType.Crib, 0, scoreToAdd);
        }

        public async Task OnUpdateScoreUi(PlayerType player, int scoreDelta, ScoreCollection scores)
        {
            await StateMachine.UiState.AddScore(player, scoreDelta);
            await AddToScoreHistory(GridPlayer.Items, scores, PlayerType.Player);
            HintWindow.InsertScoreSummary(scores.ScoreType, scoreDelta, 0);
            string message = String.Format("Your Score\n{0}\nHit Continue.", scores.Format(false, false, true));
            //ShowHintWindowAsync(true, false, message);
            await HintWindow_ShowAndWait(message);
        }

        public async Task OnShowCrib(PlayerType cribOwner)
        {

            await AnimateCribCardsToOwner(cribOwner);
        }

        public async Task<int> OnGetPlayerCribScore(int actualScore = int.MaxValue)
        {
            string message = "Use the up and down arrows to specify your score. Then click the check.  There are many options in Settings on how this works.";
            await ShowHintWindow(true, false, message);

            int playerScore = await PlayerSetScoreControl.ShowAndWaitForContinue(actualScore);
            await PlayerSetScoreControl.Hide();
            return playerScore;
        }

        public async Task OnEndOfHand(PlayerType dealer, int cribScore, int nComputerCountingPoint, int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn)
        {
            CardContainer cribOwner = GridPlayer;
            if (dealer == PlayerType.Computer) cribOwner = GridComputer;
            await HintWindow.InsertEndOfHandSummary(dealer, cribScore, cribOwner.Items, nComputerCountingPoint, nPlayerCountingPoint, ComputerPointsThisTurn, PlayerPointsThisTurn, Hfs);


            List<Task<Object>> tasks = new List<Task<object>>();
            cribOwner.FlipAllCards(CardOrientation.FaceDown, tasks);
            cribOwner.MoveAllCardsToTarget(GridDeck, tasks, MainPage.AnimationSpeeds.Medium, true);
            await Task.WhenAll(tasks);
        }

        public async Task OnGameOver(PlayerType winner, int winBy)
        {
            string message = "";



            if (winner == PlayerType.Player)
            {
                message = "Congratulations!  You won the game.";
                if (winBy > 30)
                    message += "\n\nYou skunked the computer!";
            }
            else
            {
                winner = PlayerType.Computer;
                message = "Sorry, you have lost the game.\nBetter luck next time.";
                if (winBy > 30)
                    message += "\n\n(You were skunked)";

            }

            await AnimateAllCardsBackToDeck();            
            ShowHintWindowAsync(true, false, message);

            if (winner == PlayerType.Player)
                await ViewCallback.ShowSomethingFlashy();

            
            MainPage.Current.AppBar.IsOpen = true;
        }

        public async Task AsyncInit()
        {
            double now = DateTime.Now.Ticks;
            if (Deck == null)
                Deck = new Deck(); ;


            await AddCardsToGrid();

            if (Settings.EnableLogging)
              await  MainPage.LogTrace.TraceMessage("Starting");

            _timerTurnReminder.Interval = new TimeSpan(0, 0, 15);
            _timerTurnReminder.Tick += OnTurnReminder;
            _timerTurnReminder.Start();


            PlayerScore.Owner = Owner.Player;
            ComputerScore.Owner = Owner.Computer;
        }

        private void OnTurnReminder(object sender, object e)
        {
            MainPage.LogTrace.TraceMessageAsync("");
            throw new NotImplementedException();
        }

       

        public async Task ShowUserMessage(string message)
        {
            await HintWindow_ShowAndWait(message);
        }

        public async Task TransferCards(HandsFromServer hfs)
        {
            await AnimateAllCardsBackToDeck();
            await OnDeal(hfs);
        }

        public void Reset()
        {
            if (PlayerScore != null)
            {
                PlayerScore.Reset();
                ComputerScore.Reset();
            }

            Board.Reset();
            PlayerSetScoreControl.HideAsync();
            HintWindow.ShowAsync(false, false, "Swipe Up to and hit New Game!");
          
            if (GridDeck.Items.Count > 52)
            {
                Debug.Assert(false, "Too many cards in your deck after reset!");
            }
            HintWindow.ResetScoreHistory();
            ViewCallback.Reset();
        }

        public Rect ScatterBounds()
        {
            return ViewCallback.ScatterBounds();
        }

        #endregion
        public async Task AddToScoreHistory(List<CardView> cards, ScoreCollection scores, PlayerType player)
        {

            List<CardView> fullHand = new List<CardView>(cards);
            switch (scores.ScoreType)
            {
                case ScoreType.Hand:
                case ScoreType.Crib:
                case ScoreType.Cut:
                    fullHand.Sort(CardView.CompareCardsByRank);
                    fullHand.Add(GridDeck.Items[0]);
                    break;
                case ScoreType.Count:
                    break;
                case ScoreType.Saved:
                case ScoreType.Unspecified:
                    return;
                default:
                    break;
            }

            string score = GetGameScore();
            await HintWindow.AddToHistory(fullHand, scores, player, Deck, score);
        }

        private void UpdateCount(int count)
        {
            CountControl.Count = count;
            foreach (CardView card in GridPlayer.Items)
            {
                if (card.Value + CountControl.Count > 31)
                    card.IsEnabled = false;
                else
                    card.IsEnabled = true;
            }
        }
        public string GetGameScore()
        {
            return String.Format("Player {0}&Computer {1}", StateMachine.UiState.UIString_PlayerScore, StateMachine.UiState.UIString_ComputerScore);
        }

        private async Task OnCountResetUpdateUi()
        {
            CountControl.UpdateLayout();

            await GridPlayedCards.UpdateCardLayout();
            await GridComputer.UpdateCardLayout();
            await GridPlayer.UpdateCardLayout();

            if (Settings.HitContinueOnGo == true)
            {
                await HintWindow_ShowAndWait("Go!\n\nHit Continue.");
            }
            else
            {
                await Task.Delay(1000);
            }
            foreach (CardView c in GridPlayedCards.Items)
            {
                c.SetOrientationAsync(CardOrientation.FaceDown, MainPage.AnimationSpeeds.Medium);
            }
            UpdateCount(0);
            CountControl.UpdateLayout();
        }

        #region ICardDropTarget
        public bool AllowDrop(List<CardView> cards)
        {
            MainPage.LogTrace.TraceMessageAsync("");
            throw new NotImplementedException();
        }

        public Task<GameState> DropCards(List<CardView> cards)
        {
            MainPage.LogTrace.TraceMessageAsync("");
            throw new NotImplementedException();
        }

        public Task PostDropCards(GameState state)
        {
            MainPage.LogTrace.TraceMessageAsync("");
            throw new NotImplementedException();
        }

        public async Task<bool> DroppedCards(Point point, List<CardView> cards)
        {

            bool acceptedCards = GridPlayedCards.HitTest(point);
            if (point.X - int.MaxValue < 2) // happens on double tap -- 
                acceptedCards = true;
            if (acceptedCards)
            {

                foreach (CardView card in cards)
                {
                    card.Selected = false;
                    GridPlayer.TransferCard(card, GridPlayedCards);
                }
                try
                {
                    int maxSel = await StateMachine.DroppedCards(cards); // this throws if the count > 31
                    GridPlayer.MaxSelected = maxSel;
                }
                catch (Exception)
                {
                    return false;
                }

            }

            GridPlayedCards.UpdateCardLayoutAsync();
            GridPlayer.UpdateCardLayoutAsync();
            return acceptedCards;
        }
        #endregion

        public async Task Initialize(Deck deck, ClientStateMachine stateMachine)
        {

            this.Deck = deck;
            await AddCardsToGrid();
            StateMachine = stateMachine;
            if (StateMachine != null)
            {
                Hfs = stateMachine.HandsFromService;
                ViewCallback.SetUIState(stateMachine.UiState);
            }
        }

        public void InitializeAsync(Deck deck, ClientStateMachine stateMachine)
        {
            this.Deck = deck;
            StateMachine = stateMachine;
#pragma warning disable 1998, 4014
            AddCardsToGrid();
#pragma warning restore 1998, 4014
            if (StateMachine != null)
            {
                ViewCallback.SetUIState(stateMachine.UiState);
            }

            PlayerSetScoreControl.HideAsync();
        }

        public async Task OnNewGame(ClientStateMachine stateMachine)
        {
            StateMachine = stateMachine;
            ViewCallback.SetUIState(stateMachine.UiState);

            await AnimateAllCardsBackToDeck();
            this.Reset();   // this reletive order matters -- you want all cards belonging to the deck when this is run
            await PickCard("Pick a card by touching (clicking) it.  Low card deals!");
            MainPage.Current.AppBar.IsOpen = false;
        }

        public async Task OnLoadGame(ClientStateMachine stateMachine)
        {
            StateMachine = stateMachine;
            ViewCallback.SetUIState(stateMachine.UiState);

            await AnimateAllCardsBackToDeck();
            this.Reset();   // this reletive order matters -- you want all cards belonging to the deck when this is run            
            MainPage.Current.AppBar.IsOpen = false;
        }

        public void OnGetSuggestedCard()
        {
            MainPage.Current.EnableAppBarButtons(false);
            MainPage.Current.AppBar.IsOpen = false;
            List<CardView> suggestions = StateMachine.GetSuggestedCards();
            foreach (CardView card in suggestions)
            {
                card.Selected = true;
            }
            MainPage.Current.EnableAppBarButtons(true);
        }



        public async Task ReleaseCards()
        {
            await AnimateAllCardsBackToDeck(0);
            
            if (Deck != null)
            {
                foreach (CardView card in Deck.Cards)
                {
                    LayoutRoot.Children.Remove(card);                   
                }
            }

            GridDeck.Items.Clear();            
        }

        private const int SHUFFLE_SCATTER_COUNT = 2;

        private async Task PickCard(string message)
        {

            List<Task<object>> taskList = new List<Task<object>>();
            for (int i = 0; i < SHUFFLE_SCATTER_COUNT; i++)
            {
                ScatterCards(GridDeck.Items, MainPage.AnimationSpeeds.Medium, taskList);
                await Task.WhenAll(taskList);
                taskList.Clear();

            }
            foreach (CardView card in GridDeck.Items)
            {
                card.PointerReleased += this.Deck_Card_PointerReleased;
            }
            
            ShowHintWindowAsync(true, false, message);


        }

        private async void Deck_Card_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

            CardView userCard = (CardView)sender;

            foreach (CardView card in GridDeck.Items)
            {
                card.PointerReleased -= this.Deck_Card_PointerReleased;
            }

            await userCard.SetOrientation(CardOrientation.FaceUp);
            userCard.BoostZindex();
            await userCard.AnimateScale(2.0, MainPage.AnimationSpeeds.Medium);

            CardView compCard = null;
            Random r = new Random();
            do
            {
                int index = r.Next(GridDeck.Items.Count - 1);
                compCard = GridDeck.Items[index] as CardView;

            } while (compCard.Rank == userCard.Rank);

            await compCard.SetOrientation(CardOrientation.FaceUp);
            compCard.BoostZindex();
            await compCard.AnimateScale(2.0, MainPage.AnimationSpeeds.Medium);

            string message = String.Format("\nYou selected the {0}\nThe Computer Selected the {1}\n{2} won the deal.",
                                        userCard.DisplayName, compCard.DisplayName,
                                        compCard.Rank < userCard.Rank ? "The Computer" : "You");

            await HintWindow_ShowAndWait(message);

            List<Task<object>> taskList = new List<Task<object>>();

            userCard.SetOrientation(CardOrientation.FaceDown, taskList);
            userCard.AnimateScale(taskList, 1.0, MainPage.AnimationSpeeds.Medium);
            compCard.SetOrientation(CardOrientation.FaceDown, taskList);
            compCard.AnimateScale(taskList, 1.0, MainPage.AnimationSpeeds.Medium);

            userCard.ResetZIndex();
            compCard.ResetZIndex();


            GridDeck.UpdateCardLayout(taskList, MainPage.AnimationSpeeds.Medium, true);
            await Task.WhenAll(taskList);

            await _stateMachine.StartGame((compCard.Rank < userCard.Rank) ? PlayerType.Computer : PlayerType.Player, Deck);
            MainPage.Current.EnableAppBarButtons(true);

            /// _btnNewGame.IsEnabled = true;
        }



    }


}
