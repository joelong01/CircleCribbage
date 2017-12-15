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

    public class ClientStateMachine
    {

        GameState _gameState = GameState.Uninitialized;
        private LocalGame _game = new LocalGame();
        CribbageView _view = null;
        PlayerType _dealer = PlayerType.Player;
        List<CardView> _crib = new List<CardView>(); // we need to remember what cards are added to the crib so we can tell the game service what cards we picked
        DispatcherTimer _asyncStateTimer = new DispatcherTimer();
        int _totalCardsDropped = 0;
        int _nPlayerCountingPoint = 0;
        int _nComputerCountingPoint = 0;
        int _nPlayerPointsThisTurn = 0;
        int _nComputerPointsThisTurn = 0;
        int _nCribPointsThisTurn = 0;
        GameState _asyncGameState = GameState.None;
        DispatcherTimer _initTimer = new DispatcherTimer();
        private const int WINNING_SCORE = 120; // I use >
        public const string SERIALIZATION_VERSION = "1.2";

        UIState _boardUi = null;
        HandsFromServer _hfs = null;

        public string Save()
        {



            string s = ""; //_game.Serialize();
            s += "[State]\n";
            s += StaticHelpers.SetValue("Version", SERIALIZATION_VERSION);
            s += StaticHelpers.SetValue("GameState", _gameState);
            s += StaticHelpers.SetValue("TotalCardsDropped", _totalCardsDropped);
            s += StaticHelpers.SetValue("Dealer", _dealer);
            s += StaticHelpers.SetValue("PlayerCountingPoints", _nPlayerCountingPoint);
            s += StaticHelpers.SetValue("PlayerPointsThisTurn", _nPlayerPointsThisTurn);
            s += StaticHelpers.SetValue("ComputerCountingPoints", _nComputerCountingPoint);
            s += StaticHelpers.SetValue("ComputerPointsThisTurn", _nComputerPointsThisTurn);
            s += StaticHelpers.SetValue("CribPointsThisTurn", _nCribPointsThisTurn);
            s += StaticHelpers.SetValue("PlayerScore", _game.GetCurrentScore(PlayerType.Player));
            s += StaticHelpers.SetValue("ComputerScore", _game.GetCurrentScore(PlayerType.Computer));

            s += "[Grids]\n";
            s += StaticHelpers.SetValue("Player", StaticHelpers.SerializeFromList(_view.GridPlayer.Items));
            s += StaticHelpers.SetValue("Computer", StaticHelpers.SerializeFromList(_view.GridComputer.Items));
            s += StaticHelpers.SetValue("Played", StaticHelpers.SerializeFromList(_view.GridPlayedCards.Items));
            s += StaticHelpers.SetValue("Crib", StaticHelpers.SerializeFromList(_view.GridCrib.Items));
            s += StaticHelpers.SetValue("SharedCard", _game.GetSharedCard().Serialize());
            s += _game.Serialize();

            s += _view.Save();

            return s;
        }



        public async Task<Dictionary<string, string>> Load(string s)
        {
            if (s == "") return null;

            Dictionary<string, string> sections = StaticHelpers.GetSections(s);

            if (_game.Deserialize(sections, _view.Deck) == false)
                return null;

            Dictionary<string, string> game = StaticHelpers.DeserializeDictionary(sections["State"]);
            if (sections == null)
                return null;

            if (game["Version"] != SERIALIZATION_VERSION)
                return null;

            _gameState = (GameState)Enum.Parse(typeof(GameState), game["GameState"]);
            _dealer = (PlayerType)Enum.Parse(typeof(PlayerType), game["Dealer"]);
            _totalCardsDropped = Convert.ToInt32(game["TotalCardsDropped"]);
            _nPlayerCountingPoint = Convert.ToInt32(game["PlayerCountingPoints"]);
            _nPlayerPointsThisTurn = Convert.ToInt32(game["PlayerPointsThisTurn"]);
            _nComputerCountingPoint = Convert.ToInt32(game["ComputerCountingPoints"]);
            _nComputerPointsThisTurn = Convert.ToInt32(game["ComputerPointsThisTurn"]);
            _nCribPointsThisTurn = Convert.ToInt32(game["CribPointsThisTurn"]);
            int playerScore = Convert.ToInt32(game["PlayerScore"]);
            int computerScore = Convert.ToInt32(game["ComputerScore"]);


            _hfs = _game.GetHfs();
            _view.Hfs = _hfs;

            Dictionary<string, string> grids = StaticHelpers.DeserializeDictionary(sections["Grids"]);

            List<CardView> PlayerCards = StaticHelpers.DeserializeToList(grids["Player"], _view.Deck);
            List<CardView> ComputerCards = StaticHelpers.DeserializeToList(grids["Computer"], _view.Deck);
            List<CardView> cribCards = StaticHelpers.DeserializeToList(grids["Crib"], _view.Deck);



            CardView SharedCard = StaticHelpers.CardFromString(grids["SharedCard"], _view.Deck);
            List<CardView> playedCards = StaticHelpers.DeserializeToList(grids["Played"], _view.Deck);

            List<Task<object>> taskList = new List<Task<object>>();
            _view.MoveCards(taskList, playedCards, _view.GridPlayedCards);
            _view.MoveCards(taskList, cribCards, _view.GridCrib);
            _view.MoveCards(taskList, PlayerCards, _view.GridPlayer);
            _view.MoveCards(taskList, ComputerCards, _view.GridComputer);

            if (_gameState == GameState.PlayerGiveToCrib)
            {
                _crib.AddRange(playedCards);
            }

            await Task.WhenAll(taskList);

            SharedCard.BoostZindex(ZIndexBoost.SmallBoost);

            _game.UpdateScoreDirect(PlayerType.Player, ScoreType.Saved, playerScore);    // give the points to the player            
            _game.UpdateScoreDirect(PlayerType.Computer, ScoreType.Saved, computerScore);    // give the points to the player            

            await UiState.AddScore(PlayerType.Player, playerScore);
            await UiState.AddScore(PlayerType.Computer, computerScore);
            _view.CountControl.Count = _game.CurrentCount;

            await FixUpUiState();

            string scoreHistory = "";
            if (sections.TryGetValue("Score History", out scoreHistory))
            {
                await _view.Load(scoreHistory);
            }

            if (_gameState == GameState.PlayerScoreHand)
            {
                //
                //  if we saved in the state where the player is supposed to score the hard, we need to drive the state machine
                //  through the completion of the GameState.ScoreHand states
                if (_dealer == PlayerType.Player)
                {

                    await SetState(GameState.PlayerScoreHand);
                    await SetState(GameState.ShowCrib);
                    await SetState(GameState.PlayerScoreCrib);
                    _gameState = GameState.EndOfHand;
                }
                else
                {

                    _gameState = GameState.ScoreHands;

                }


            }
            else if (_gameState == GameState.PlayerScoreCrib)
            {

                await SetState(GameState.PlayerScoreCrib);
                _gameState = GameState.EndOfHand;
            }




            SetStateAsync(_gameState);


            return sections;
        }

        private async Task FixUpUiState()
        {

            switch (_gameState)
            {
                case GameState.PlayerGiveToCrib:
                    break;
                case GameState.PlayerCountCard:
                    _view.ShowCountControl();
                    await _view.ShowSharedCard();
                    break;
                case GameState.PlayerScoreHand:
                    await _view.ShowSharedCard();
                    break;
                case GameState.PlayerScoreCrib:
                    await _view.ShowSharedCard();
                    break;
                default:
                    break;
            }
        }

        public HandsFromServer HandsFromService
        {
            get { return _hfs; }
            set { _hfs = value; }
        }

        public UIState UiState
        {
            get { return _boardUi; }
        }


        public Settings Settings
        {
            get { return MainPage.Current.Settings; }
        }
        public PlayerType Dealer
        {
            get { return _dealer; }
        }
        int _tabDepth = 0;

        public void TransferState()
        {
            HandsFromServer hfs = _game.GetHfs();
            //  _view.TransferCards(hfs);

            switch (_gameState)
            {

                case GameState.Start:
                case GameState.PickCard:
                    SetStateAsync(_gameState);
                    break;
                case GameState.Deal:
                    break;
                case GameState.GiveToCrib:
                    break;
                case GameState.PlayerGiveToCrib:
                    break;
                case GameState.ComputerGiveToCrib:
                    break;
                case GameState.Count:
                    break;
                case GameState.ComputerCountCard:
                    break;
                case GameState.PlayerCountCard:
                    break;
                case GameState.CountingEnded:
                    break;
                case GameState.ScoreHands:
                    break;
                case GameState.PlayerScoreHand:
                    break;
                case GameState.PlayerScoreCrib:
                    break;
                case GameState.ComputerScoreHand:
                    break;
                case GameState.ComputerScoreCrib:
                    break;
                case GameState.ShowCrib:
                    break;
                case GameState.EndOfHand:
                    break;
                case GameState.GameOver:
                    break;
                case GameState.None:
                    break;
                default:
                    break;
            }
            {

            }

        }

        public ClientStateMachine() { }

        private async void OnSetStateAsync(object sender, object e)
        {
            _asyncStateTimer.Stop();
            if (_asyncGameState != GameState.None)
            {
                await SetState(_asyncGameState);
                _asyncGameState = GameState.None;
            }
        }

        private void SetStateAsync(GameState state)
        {
            _asyncGameState = state;
            _asyncStateTimer.Start();
        }

        public GameState State
        {
            get
            {
                return _gameState;
            }
        }


        public void TransfertoNewUI(CribbageView view)
        {
            _view = view;
        }

        public void Init(bool wantCallback, CribbageView view)
        {
            _boardUi = new UIState(view.Board);
            _view = view;
            if (wantCallback)
            {
                _initTimer.Tick += AysnInit;
                _initTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
                _initTimer.Start();
            }
            _asyncStateTimer.Interval = TimeSpan.FromMilliseconds(500);
            _asyncStateTimer.Tick += OnSetStateAsync;


        }
        private async void AysnInit(object sender, object e)
        {
            _initTimer.Stop();
            await _view.AsyncInit();

        }


        public async Task StartGame(PlayerType dealer, Deck deck)
        {
            _dealer = dealer;
            _game.NewGame(_dealer, deck);

            //
            //  2 stats when you start a game.  GameStarted, and who won the deal.  if you play 1,000 games, how many times will you deal?
            MainPage.Current.StatsView.Stats.Stat(StatName.GamesStarted).UpdateStatistic(PlayerType.Player, 1);
            MainPage.Current.StatsView.Stats.Stat(StatName.WonDeal).UpdateStatistic(dealer, 1);
            await SetState(GameState.Deal);

        }



        private async Task SetState(GameState state)
        {


            if (_gameState == GameState.GameOver)
            {
                return; // need to create a new game...
            }

            _gameState = state;
            
            MainPage.Current.EnableSaveGame(false); // default is you can't save the amge
            string enterLogString = "";
            for (int i = 0; i < _tabDepth; i++) enterLogString += "\t";
            enterLogString += String.Format("Setting State to:{0}", state);
            MainPage.LogTrace.TraceMessageAsync(enterLogString);
            _tabDepth++;

            switch (_gameState)
            {

                case GameState.PickCard:
                  
                    _dealer = _view.OnPickCard();
                    
                    await SetState(GameState.Deal);
                    break;
                case GameState.Deal:
                    _totalCardsDropped = 0;
                    _nPlayerCountingPoint = 0;
                    _nComputerCountingPoint = 0;
                    _nPlayerPointsThisTurn = 0;
                    _nComputerPointsThisTurn = 0;
                    _nCribPointsThisTurn = 0;
                    HandsFromService = _game.ShuffleAndReturnAllCards();
                    Debug.Assert(_game.Dealer == _dealer);
                    await _view.OnDeal(HandsFromService);
                    await SetState(GameState.GiveToCrib);
                    break;
                case GameState.GiveToCrib:
                    await SetState(GameState.ComputerGiveToCrib);
                    await SetState(GameState.PlayerGiveToCrib);
                    break;
                case GameState.ComputerGiveToCrib:
                    Debug.Assert(_crib.Count == 0);
                    _crib = _game.GetSuggestedCrib(PlayerType.Computer, Settings.Difficulty);
                    await _view.OnComputerGiveToCrib(_crib);
                    break;
                case GameState.PlayerGiveToCrib:
                    MainPage.Current.EnableSaveGame(true);
                    PlayerType cribOwner = (_dealer == PlayerType.Player ? PlayerType.Player : PlayerType.Computer);
                    _boardUi.Crib = cribOwner;
                    _boardUi.Turn = PlayerType.Player;
                    await _view.OnPlayerGiveToCrib(2 - _totalCardsDropped, cribOwner);
                    break;
                case GameState.Count:

                    if (_dealer == PlayerType.Player)
                    {
                        await SetState(GameState.ComputerCountCard);
                    }
                    else
                    {
                        await SetState(GameState.PlayerCountCard);
                    }
                    break;
                case GameState.ComputerCountCard:
                    {
                        _totalCardsDropped = await CountComputerCards();
                        if (_totalCardsDropped == 8)
                            await SetState(GameState.CountingEnded);
                        else
                            await SetState(GameState.PlayerCountCard);
                        break;
                    }

                case GameState.PlayerCountCard:
                    {
                        _view.OnPlayerCountCard();
                       
                        MainPage.Current.EnableSaveGame(true);
                        break;
                    }
                case GameState.CountingEnded:
                    await _view.OnCountingEnded(_nPlayerCountingPoint, _nComputerCountingPoint);
                    MainPage.Current.StatsView.Stats.Stat(StatName.TotalCountingSessions).UpdateStatistic(PlayerType.Player, 1);
                    MainPage.Current.StatsView.Stats.Stat(StatName.TotalCountingSessions).UpdateStatistic(PlayerType.Computer, 1);
                    await SetState(GameState.ScoreHands);
                    break;
                case GameState.ScoreHands:
                    if (_dealer == PlayerType.Player)
                    {
                        await SetState(GameState.ComputerScoreHand);
                        await SetState(GameState.PlayerScoreHand);
                        await SetState(GameState.ShowCrib);
                        await SetState(GameState.PlayerScoreCrib);
                    }
                    else
                    {
                        await SetState(GameState.PlayerScoreHand);
                        await SetState(GameState.ComputerScoreHand);
                        await SetState(GameState.ShowCrib);
                        await SetState(GameState.ComputerScoreCrib);
                    }

                    await SetState(GameState.EndOfHand);
                    break;
                case GameState.ComputerScoreHand:
                    ScoreCollection story = _game.UpdateScore(PlayerType.Computer, HandType.Regular, 0, Settings.Difficulty); // service ignores what computer sends
                    _nComputerPointsThisTurn = story.Total;
                    await _view.OnComputerScoreHand(story.Total, Settings.OKAfterHand, story);
                    if (_game.GetCurrentScore(PlayerType.Computer) > WINNING_SCORE)
                    {
                        await SetState(GameState.GameOver);
                        return;
                    }

                    MainPage.Current.StatsView.Stats.Stat(StatName.TotalHandsPlayed).UpdateStatistic(PlayerType.Computer, 1);
                    break;
                case GameState.ComputerScoreCrib:
                    {
                        ScoreCollection cribStory = _game.UpdateScore(PlayerType.Computer, HandType.Crib, 0, Settings.Difficulty); // service ignores what computer sends                        
                        _nCribPointsThisTurn = cribStory.Total;
                        await _view.OnComputerScoreCrib(cribStory.Total, cribStory);
                        if (_game.GetCurrentScore(PlayerType.Computer) > WINNING_SCORE)
                        {
                            await SetState(GameState.GameOver);
                            return;
                        }
                    }
                    MainPage.Current.StatsView.Stats.Stat(StatName.TotalCribsPlayed).UpdateStatistic(PlayerType.Computer, 1);
                    break;
                case GameState.PlayerScoreHand:
                    
                    MainPage.Current.EnableSaveGame(true);
                    int scoreGuess = int.MaxValue;
                    ScoreCollection playerHandStory = null;
                    do
                    {
                        if (Settings.AutoSetScore)
                        {
                            playerHandStory = _game.UpdateScore(PlayerType.Player, HandType.Regular, 0, Settings.Difficulty);
                            scoreGuess = playerHandStory.ActualScore;
                        }

                        scoreGuess = await _view.OnGetPlayerHandScore(scoreGuess);
                        playerHandStory = _game.UpdateScore(PlayerType.Player, HandType.Regular, scoreGuess, Settings.Difficulty);
                        if (playerHandStory.Accepted == false)
                        {


                            // if you aren't doing muggins and got the score wrong, you just try again
                            await _view.ShowUserMessage("That guess was close, but not quite right.  Try again.");
                        }
                        else // you got the score right!
                        {

                            break;
                        }

                    } while (true);

                    await _view.OnUpdateScoreUi(PlayerType.Player, scoreGuess, playerHandStory); // update the UI for the score the user gets
                    MainPage.Current.StatsView.Stats.Stat(StatName.TotalHandsPlayed).UpdateStatistic(PlayerType.Player, 1);

                    _nPlayerPointsThisTurn = scoreGuess;

                    if (_game.GetCurrentScore(PlayerType.Player) > WINNING_SCORE)
                    {
                        await SetState(GameState.GameOver);
                        return;
                    }

                    break;
                case GameState.ShowCrib:
                    await _view.OnShowCrib(_dealer);
                    break;
                case GameState.PlayerScoreCrib:
                   
                    MainPage.Current.EnableSaveGame(true);
                    int scoreCribGuess = int.MaxValue;
                    if (Settings.AutoSetScore)
                        scoreCribGuess = _game.GetScore(PlayerType.Player, HandType.Crib);

                    ScoreCollection playerCribStory = null;
                    do
                    {
                        scoreCribGuess = await _view.OnGetPlayerCribScore(scoreCribGuess);
                        playerCribStory = _game.UpdateScore(PlayerType.Player, HandType.Crib, scoreCribGuess, Settings.Difficulty);
                        scoreCribGuess = playerCribStory.Total;
                        if (playerCribStory.Accepted == false)
                            await _view.ShowUserMessage("That guess was close, but not quite right.  Try again.");

                    } while (playerCribStory.Accepted == false);
                    await _view.OnUpdateScoreUi(PlayerType.Player, playerCribStory.Total, playerCribStory);
                    MainPage.Current.StatsView.Stats.Stat(StatName.TotalCribsPlayed).UpdateStatistic(PlayerType.Player, 1);
                    _nCribPointsThisTurn = playerCribStory.Total;
                    if (_game.GetCurrentScore(PlayerType.Player) > WINNING_SCORE)
                    {
                        await SetState(GameState.GameOver);
                        return;
                    }
                    break;
                case GameState.EndOfHand:
                    await _view.OnEndOfHand(_dealer, _nCribPointsThisTurn, _nComputerCountingPoint, _nPlayerCountingPoint, _nComputerPointsThisTurn, _nPlayerPointsThisTurn);                    
                    if (_dealer == PlayerType.Player)
                        _dealer = PlayerType.Computer;
                    else
                        _dealer = PlayerType.Player;

                    await SetState(GameState.Deal);
                    break;
                case GameState.GameOver:
                    {
                       
                        int playerScore = _game.GetCurrentScore(PlayerType.Player);
                        int computerScore = _game.GetCurrentScore(PlayerType.Computer);

                        if (computerScore > 121) computerScore = 121;
                        if (playerScore > 121) playerScore = 121;

                        PlayerType winner = PlayerType.Player;
                        if (playerScore < computerScore)
                        {
                            winner = PlayerType.Computer;
                            
                        }

                        int winMargin = Math.Abs(playerScore - computerScore);

                        MainPage.Current.StatsView.Stats.Stat(StatName.GamesWon).UpdateStatistic(winner, 1);
                        MainPage.Current.StatsView.Stats.Stat(StatName.GamesLost).UpdateStatistic(winner == PlayerType.Player ? PlayerType.Computer : PlayerType.Player, 1);
                        MainPage.Current.StatsView.Stats.Stat(StatName.SmallestWinMargin).UpdateStatistic(winner, winMargin);
                        MainPage.Current.StatsView.Stats.Stat(StatName.LargestWinMargin).UpdateStatistic(winner, winMargin);
                        if (winMargin >= 30)
                            MainPage.Current.StatsView.Stats.Stat(StatName.SkunkWins).UpdateStatistic(winner, 1);

                        await _view.OnGameOver(winner, Math.Abs(playerScore - computerScore));
                    }
                    break;
            }

            string leaveLogString = "";
            for (int i = 0; i < _tabDepth; i++) leaveLogString += "\t";
            leaveLogString += String.Format("Returning from SetState. Current State {0}  Depth:{1}", state, _tabDepth);
            MainPage.LogTrace.TraceMessageAsync(leaveLogString);
            _tabDepth--;

        }

        private async Task ScoreCutJack(int jackIndex)
        {
            //
            //  NOTE: score already added in LocalGame
            ScoreCollection scores = new ScoreCollection();
            scores.ActualScore = 2;
            scores.Total = 2;
            scores.ScoreType = ScoreType.Cut;
            List<int> cards = new List<int>();
            cards.Add(jackIndex);
            scores.Scores.Add(new ScoreInstance(StatName.CutAJack, 1, 2, cards));
            await _view.OnUpdateScoreUi(_dealer, 2, scores);
        }

        private async Task<int> CountComputerCards()
        {
            CountingData data;
            do
            {
                CardView card = _game.GetSuggestedCard(PlayerType.Computer);
                MainPage.LogTrace.TraceMessageAsync(String.Format("Computer played card: {0}", card.Data.Name));
                data = _game.CountCard(PlayerType.Computer, card, Settings.Difficulty);
                _nComputerCountingPoint += data.Score;
                await _view.OnCountCard(card, PlayerType.Computer, data); // count until it isn't the computer's turn 
                _boardUi.AddScoreAsync(PlayerType.Computer, data.Score);
                _boardUi.Turn = data.NextPlayer;
                if (_game.GetCurrentScore(PlayerType.Computer) > WINNING_SCORE)
                {
                    await SetState(GameState.GameOver);
                    return 8;
                }
                _totalCardsDropped = data.CardsCounted;
                if (data.CardsCounted == 8)
                {
                    return 8;

                }

            } while (data.NextPlayerIsComputer);

            return data.CardsCounted;

        }


        public async Task<int> DroppedCards(List<CardView> cards)
        {
            _totalCardsDropped += cards.Count;
            if (this.State == GameState.PlayerGiveToCrib)
            {
                _crib.AddRange(cards);

                if (_crib.Count == 4)
                {
                    await _view.OnSendCardsToCrib();
                    if (HandsFromService.SharedCard.Rank == 11)
                    {
                        await ScoreCutJack(HandsFromService.SharedCard.Index);
                    }
                    _game.SendAllCardsToCrib(_crib);
                    _totalCardsDropped = 0;
                    _crib.Clear();
                    SetStateAsync(GameState.Count);
                    return 0;
                }

                return 4 - _crib.Count;
            }

            if (this.State == GameState.PlayerCountCard)
            {
                CountingData data = _game.CountCard(PlayerType.Player, cards[0], Settings.Difficulty);
                _nPlayerCountingPoint += data.Score;
                _boardUi.AddScoreAsync(PlayerType.Player, data.Score);
                await _view.OnCountCard(cards[0], PlayerType.Player, data);
                _boardUi.Turn = data.NextPlayer;
                if (_game.GetCurrentScore(PlayerType.Player) > WINNING_SCORE)
                {
                    await SetState(GameState.GameOver);
                    return 8;
                }
                if (data.CardsCounted == 8)
                {
                    SetStateAsync(GameState.CountingEnded);
                    return 0;
                }
                else if (data.NextPlayerIsComputer)
                {
                    SetStateAsync(GameState.ComputerCountCard);
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            throw new Exception(String.Format("Unexpected state during user dropping card.  State is: {0}", _gameState));
        }





        public List<CardView> GetSuggestedCards()
        {
            List<CardView> cards = new List<CardView>();
            if (_gameState == GameState.PlayerCountCard)
            {
                CardView card = _game.GetSuggestedCard(PlayerType.Player);
                cards.Add(card);
            }
            else if (_gameState == GameState.PlayerGiveToCrib)
            {
                return _game.GetSuggestedCrib(PlayerType.Player, Settings.Difficulty);

            }

            return cards; // I did it this way so that it returns an empty collection

        }
    }

}
