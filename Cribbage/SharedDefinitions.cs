using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Windows.UI.Xaml.Shapes;
using System.Threading;
using Windows.UI.Popups;
using System.Threading.Tasks;
using CribbageService;
using Windows.UI.Core;

namespace Cribbage
{
    
    /// <summary>
    /// implemented by CribbageView, called by MainPage.   
    /// </summary>
    public interface ICommonView
    {
       
        Task OnNewGame(ClientStateMachine statemachine);
        void OnGetSuggestedCard();
        Task Initialize(Deck deck, ClientStateMachine statemachine);
        Task ReleaseCards();

        void InitializeAsync(Deck _deck, ClientStateMachine _stateMachine);
        ICribbageBoardUi GetUiStateCallback();

       
    }

    //public interface ILoadSave
    //{
    //    string SaveCardGridState();
    //    Task  PutCardsInCorrectGrid(string state, HandsFromServer hfs, GameState gameState);
    //}


    /// <summary>
    ///     Implemented by CircleLayoutPage and SquarePage, called by CribbageView
    /// </summary>
    public interface IViewCallback
    {
        void Reset();
        Rect ScatterBounds();

        void SetUIState(UIState uiState);

        Task ShowSomethingFlashy();

        void RemindUserToHitContinue(bool start);

    }

    /// <summary>
    ///  Implemented by the Board, called by UiStates
    /// </summary>
    public interface ICribbageBoardUi
    {
        Task Reset();
        void AnimateScoreAsync(PlayerType player, int scoreToAdd);

        Task AnimateScore(PlayerType player, int scoreToAdd);
        
       
    }

    
    /// <summary>
    ///  Implemented by CircleLayoutPage and SquarePage, called by MainPage 
    /// </summary>
    public interface IBaseView
    {
        CribbageView GetCommonView();

        Task ShowStats();
    }

   public interface IPlayerSetScore
   {

       Task<int> ShowAndWaitForContinue(int actualScore);

       Task Hide();

       void HideAsync();
   }

    //
    //  these are the (usually) XAML defined UI elements that the CribbageView needs in order to drive the game experience    
    public class UIClasses
    {
        public ICribbageBoardUi Board = null;               
        public CardContainer GridDeck = null;
        public Grid LayoutRoot = null;
        public Grid CenterGrid = null;
        public Deck Deck = null;
        public CoreDispatcher Dispatcher = null;
        public CardContainer GridPlayer = null;
        public CardContainer GridComputer = null;
        public CardContainer GridPlayedCards = null;
        public CardContainer GridCrib = null;
        public CountCtrl CountControl = null;
        public IShowInstructionsUi ShowInstructionsUi = null;
        public IPlayerSetScore PlayerSetScoreControl = null;
        public PegScore PlayerScore = null;
        public PegScore ComputerScore = null;
        public IViewCallback ViewCallback = null;
        public string FriendlyName = "";
    }

    /// <summary>
    ///     implemented by CribbageView, called by ClientStateMachine
    ///     passed to ClientStateMachine in ::Init(0
    /// </summary>
    public interface ICribbageUX
    {
        PlayerType OnPickCard();
        Task OnDeal(HandsFromServer hfs, double duration=Double.MaxValue);
        Task OnComputerGiveToCrib(List<CardView> cards);
        Task OnPlayerGiveToCrib(int maxCards, PlayerType cribOwner);
        Task OnSendCardsToCrib();

        void OnPlayerCountCard();

        Task OnCountCard(CardView card, PlayerType currentPlayer, CountingData countingData);

        Task OnCountingEnded(int playerScore, int computerScore);

        Task<int> OnGetPlayerHandScore(int actualScore = int.MaxValue);

        Task OnComputerScoreHand(int scoreToAdd, bool promptUser, ScoreCollection scores);
     
        Task OnComputerScoreCrib(int scoreToAdd, ScoreCollection scoreStory);
        Task OnUpdateScoreUi(PlayerType player, int scoreDelta, ScoreCollection scores);
        Task OnShowCrib(PlayerType cribOwner);
        Task<int> OnGetPlayerCribScore(int actualScore = int.MaxValue);
        Task OnEndOfHand(PlayerType dealer, int cribpionts, int nComputerCountingPoint, int nPlayerCountingPoint, int ComputerPointsThisTurn, int PlayerPointsThisTurn);
        Task OnGameOver(PlayerType winner, int winBy);
        Task AsyncInit();
        
        Task ShowUserMessage(string message);

        Task TransferCards(HandsFromServer hfs);
        void Reset();
        Rect ScatterBounds();

    }
    public enum GameState
    {
        Uninitialized = -1,
        Start = 10,
        PickCard = 20,
        Deal = 30,
        GiveToCrib = 40,
        PlayerGiveToCrib = 50,
        ComputerGiveToCrib = 60,
        Count = 70,
        ComputerCountCard = 80,
        PlayerCountCard = 90,
        CountingEnded = 100,
        ScoreHands = 110,
        PlayerScoreHand = 120,
        PlayerScoreCrib = 130,
        ComputerScoreHand = 140,
        ComputerScoreCrib = 150,
        ShowCrib = 160,
        EndOfHand = 170,
        GameOver = 180,
        None
    }

    public enum Turn
    {
        Computer,
        Player,
        Nobody
    };
    public class PegScore
    {
        Owner _owner;

        public PegScore()
        {
           
            FirstPegControl = new PegControl();
            FirstPegControl.Width = 15.0;
            FirstPegControl.Height = 15.0;
            SecondPegControl = new PegControl();
            SecondPegControl.Width = 15.0;
            SecondPegControl.Height = 15.0;
            Canvas.SetZIndex(SecondPegControl, 75);
            Canvas.SetZIndex(FirstPegControl, 75);
            Score1 = 0;
            Score2 = 0;

        }

        public int Score1 
        { 
            get
            {
                return FirstPegControl.Score;
            } 
            set
            {
                FirstPegControl.Score = value;
            }
        }
        public int Score2
        {
            get
            {
                return SecondPegControl.Score;
            }
            set
            {
                SecondPegControl.Score = value;
            }
        }
        public Owner Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;
                FirstPegControl.Owner = _owner;
                SecondPegControl.Owner = _owner;
            }

        }

        public void Reset()
        {

            Score1 = 0;
            Score2 = 0;

        }

        public PegControl FirstPegControl { get; set; }
        public PegControl SecondPegControl { get; set; }
        public double Diameter
        {
            get
            {
                return FirstPegControl.Width;
            }
            set
            {
                FirstPegControl.Width = value;
                FirstPegControl.Height = value;
                SecondPegControl.Width = value;
                SecondPegControl.Height = value;

            }

        }


    }

    public class ResolutionDependentVariables
    {
        public double PegSize { get; set; }
        public double CountFontSize { get; set; }
        public bool RotateBug { get; set; }
        public double YourCribFontSize { get; set; }
        public double SharedCardFontSize { get; set; }

    }

    public class DealerChosenEventArgs : EventArgs
    {
        public DealerChosenEventArgs(bool playerDeal)
        {

            if (playerDeal)
                Dealer = PlayerType.Player;
            else
                Dealer = PlayerType.Computer;
        }
        public PlayerType Dealer { get; set; }
    }

    public delegate Task DealerChosenHandler2(object sender, DealerChosenEventArgs e);

    
}