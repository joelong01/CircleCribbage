using CribbageService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class NewRoundBoardPage : Page, ITestButtonsCallback
    {
        public StatsViewControl StatsView
        {
            get
            {
                return _ctrlStatsView;
            }
        }

        public async void OnSetPlayerScore(bool increment)
        {
            int score = await _board.ShowAndWaitForContinue(12);
            await _board.Hide();
        }


        public async void OnChangeRTO(bool increment)
        {

            await ShowSomethingFlashy();


        }

        public void OnTestHand()
        {
            //CardNames[] playerCards = new CardNames[] { CardNames.FiveOfClubs, CardNames.KingOfHearts, CardNames.TenOfClubs, CardNames.JackOfDiamonds, CardNames.AceOfHearts, CardNames.TwoOfClubs };
            //CardNames[] computerCards = new CardNames[] { CardNames.FiveOfHearts, CardNames.FiveOfSpades, CardNames.TenOfSpades, CardNames.JackOfHearts, CardNames.AceOfClubs, CardNames.TwoOfDiamonds };


            //HandsFromServer hfs = new HandsFromServer();
            //hfs.SharedCard = Deck.Card(CardNames.QueenOfDiamonds);

            //hfs.PlayerCards = new List<CardView>();
            //hfs.ComputerCards = new List<CardView>();

            //for (int i = 0; i < 6; i++)
            //{
            //    hfs.PlayerCards.Add(Deck.Card(playerCards[i]));
            //    hfs.ComputerCards.Add(Deck.Card(computerCards[i]));
            //}

            //await StartGame(hfs);
        }

        private async Task AddScoreCollectionToHistory()
        {
            
            LocalGame game = new LocalGame();
            game.NewGame(PlayerType.Computer, MainPage.Current.Deck);
            HandsFromServer hfs = game.ShuffleAndReturnAllCards();
            List<CardView> crib = game.GetSuggestedCrib(PlayerType.Player, GameDifficulty.Hard);
            game.SendToCrib(PlayerType.Computer, crib);
            hfs.PlayerCards.Remove(crib[0]);
            hfs.PlayerCards.Remove(crib[1]);
            int scoreGuess = game.GetScore(PlayerType.Player, HandType.Regular);
            ScoreCollection scores = game.UpdateScore(PlayerType.Player, HandType.Regular, scoreGuess, GameDifficulty.Hard);
            List<CardView> cards = new List<CardView>(hfs.PlayerCards);
            _gridDeck.Items.Remove(hfs.SharedCard);
            _gridDeck.Items.Insert(0, hfs.SharedCard);



            await _view.AddToScoreHistory(cards, scores, PlayerType.Player);
        }

        private  Task StartGame(HandsFromServer hfs)
        {
            throw new NotImplementedException();
            //LocalGame game = new LocalGame();
            //game.NewGame(PlayerType.Computer, Deck);
            //hfs = game.ShuffleAndReturnAllCards(hfs);
            //List<CardView> crib = game.GetSuggestedCrib(PlayerType.Player, GameDifficulty.Hard);
            //game.SendToCrib(PlayerType.Computer, crib);
            //hfs.PlayerCards.Remove(crib[0]);
            //hfs.PlayerCards.Remove(crib[1]);
            //int scoreGuess = game.GetScore(PlayerType.Player, HandType.Regular);
            //ScoreCollection scores = game.UpdateScore(PlayerType.Player, HandType.Regular, scoreGuess, GameDifficulty.Hard, MugginsSettings.None);
            //List<CardView> cards = new List<CardView>(hfs.PlayerCards);
            //_gridDeck.Items.Remove(hfs.SharedCard);
            //_gridDeck.Items.Insert(0, hfs.SharedCard);

            //await _view.AddToScoreHistory(cards, scores, PlayerType.Computer);

        }

        private  Task Test_AddCardHistory()
        {
            throw new NotImplementedException();
            //await _ctrlCardHistory.AddCreateHandHistory().SetSharedCard(Deck.Cards[45]);

            //List<CardView> player = new List<CardView>();
            //List<CardView> crib = new List<CardView>();
            //List<CardView> computer = new List<CardView>();
            //for (int i = 0; i < 4; i++)
            //{
            //    player.Add(Deck.Cards[i]);
            //    crib.Add(Deck.Cards[i + 13]);
            //    computer.Add(Deck.Cards[i + 26]);
            //}

            //await _ctrlCardHistory.Current.SetComputerHand(computer);
            //await _ctrlCardHistory.Current.SetPlayerCards(player);
            //await _ctrlCardHistory.Current.SetCribHand(crib, PlayerType.Player);
        }


        public async void OnRunHandAnimations(bool increment)
        {
            //
            // deal the cards
            await TestDealAnimation();

            //
            // select computer crib cards
            List<CardView> cards = new List<CardView>();
            cards.Add(_gridComputer.Items[2]);
            cards.Add(_gridComputer.Items[0]);
            await AnimateSelectComputerCribCards(cards);

            //
            //  move in player cards
            List<Task<Object>> tasks = new List<Task<object>>();
            _gridPlayer.MoveCardToTarget(_gridPlayer.Items[2], _gridPlayedCards, tasks);
            _gridPlayer.MoveCardToTarget(_gridPlayer.Items[4], _gridPlayedCards, tasks);
            await Task.WhenAll(tasks);

            //  
            // update the cards
            //await _gridComputer.UpdateCardLayout(MoveCardOptions.WhenAll);
            //await _gridPlayer.UpdateCardLayout(MoveCardOptions.WhenAll);
            //
            //  move them to the crib
            await _gridPlayedCards.FlipAllCards(CardOrientation.FaceDown);
            await OnAnimateMoveCardsToCrib();
            await Task.Delay(2000);

            //
            //  pretent to count them
            await OnTestCountCards();
            await Task.Delay(2000);

            //
            //  back to owner
            await AnimateCardsBackToOwner();
            await Task.Delay(2000);

            await AnimateCribCardsToOwner(PlayerType.Player);

            await Task.Delay(2000);

            //
            //  move cards back to deck
            _gridPlayer.FlipAllCards(CardOrientation.FaceDown, tasks);
            _gridPlayer.MoveAllCardsToTarget(_gridDeck, tasks, MainPage.AnimationSpeeds.Medium, true);
            await Task.WhenAll(tasks);
        }

        private async Task AnimateCribCardsToOwner(PlayerType owner)
        {

            CardContainer cribOwner = _gridPlayer;
            if (owner == PlayerType.Computer) cribOwner = _gridComputer;

            List<Task<Object>> tasks = new List<Task<object>>();
            //
            //  return player and computer cards to the deck

            _gridPlayer.FlipAllCards(CardOrientation.FaceDown, tasks);
            _gridPlayer.MoveAllCardsToTarget(_gridDeck, tasks, MainPage.AnimationSpeeds.Medium, true);
            _gridComputer.FlipAllCards(CardOrientation.FaceDown, tasks);
            _gridComputer.MoveAllCardsToTarget(_gridDeck, tasks, MainPage.AnimationSpeeds.Medium, true);


            //
            //  move crib cards back to player
            _gridCrib.MoveAllCardsToTarget(cribOwner, tasks, MainPage.AnimationSpeeds.Medium, true);
            cribOwner.FlipAllCards(CardOrientation.FaceUp, tasks);
            await Task.WhenAll(tasks);

        }
        public async void OnTestcount(bool increment)
        {
            await OnTestCountCards();
        }
        public async Task OnTestCountCards()
        {
            for (int i = _gridComputer.Items.Count - 1; i >= 0; i--)
            {
                CardView card = _gridPlayer.Items[i];
                card.Owner = Owner.Player;
                await _gridPlayer.MoveCardToTarget(card, _gridPlayedCards);
                await _gridPlayer.UpdateCardLayout(MoveCardOptions.MoveAllAtSameTime);

                card = _gridComputer.Items[i];
                card.Owner = Owner.Computer;
                await _gridComputer.MoveCardToTarget(card, _gridPlayedCards);
                await card.SetOrientation(CardOrientation.FaceUp);
                await _gridComputer.UpdateCardLayout(MoveCardOptions.MoveAllAtSameTime);
            }


        }

        public async void OnBackToDeck(bool increment)
        {
            await AnimateAllCardsBackToDeck();
        }

        private async Task AnimateAllCardsBackToDeck()
        {
            _ctrlCount.Hide();

            // flip the cards and then move them for a nice affect

            List<Task<object>> list = new List<Task<object>>();
            _gridPlayer.FlipAllCards(CardOrientation.FaceDown, list);
            _gridPlayer.MoveAllCardsToTarget(_gridDeck, list, MainPage.AnimationSpeeds.Medium);

            _gridCrib.FlipAllCards(CardOrientation.FaceDown, list);
            _gridCrib.MoveAllCardsToTarget(_gridDeck, list, MainPage.AnimationSpeeds.Medium);

            _gridPlayedCards.FlipAllCards(CardOrientation.FaceDown, list);
            _gridPlayedCards.MoveAllCardsToTarget(_gridDeck, list, MainPage.AnimationSpeeds.Medium);

            _gridComputer.FlipAllCards(CardOrientation.FaceDown, list);
            _gridComputer.MoveAllCardsToTarget(_gridDeck, list, MainPage.AnimationSpeeds.Medium);

            foreach (CardView card in _gridDeck.Items)
            {
                card.Reset();
            }

            _gridDeck.UpdateCardLayout(list, MainPage.AnimationSpeeds.Medium, false);

            await Task.WhenAll(list);


        }

        public async void OnBackToOwner(bool increment)
        {


            await AnimateCardsBackToOwner();

        }

        private async Task AnimateCardsBackToOwner()
        {
            List<Task<object>> taskList = new List<Task<object>>();
            CardView card = null;
            for (int i = _gridPlayedCards.Items.Count - 1; i >= 0; i--)
            {
                card = _gridPlayedCards.Items[i];
                if (card.Owner == Owner.Player)
                {
                    _gridPlayedCards.MoveCardToTarget(card, _gridPlayer, taskList);
                }
                else if (card.Owner == Owner.Computer)
                {
                    _gridPlayedCards.MoveCardToTarget(card, _gridComputer, taskList);
                }
                if (card.Orientation == CardOrientation.FaceDown)
                {
                    card.SetOrientation(CardOrientation.FaceUp, taskList);
                }
                if (card.AnimatedOpacity != 1.0)
                {
                    card.AnimateFade(1.0, taskList);
                }

            }

            await Task.WhenAll(taskList);

            _ctrlCount.Hide();
        }

        public async void OnDeal(bool increment)
        {

            //foreach (CardView card in _gridDeck.Items)
            //{
            //    card.PointerPressed += card_PointerPressed;
            //}

            await TestDealAnimation();



        }

        void card_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //DragList dragList = new DragList();
            //Rect bounds = new Rect();
            //GeneralTransform gt = _gridPlayedCards.TransformToVisual(this);
            //Point topLeft = gt.TransformPoint(new Point(0, 0));
            //bounds.X = topLeft.X;
            //bounds.Y = topLeft.Y;
            //bounds.Width = _gridPlayedCards.ActualWidth;
            //bounds.Height = _gridPlayedCards.ActualHeight;
            //Debug.WriteLine("HitTestBounds: {0}", bounds);

            //HitTestClass hitTestC = new HitTestClass(_gridPlayedCards, bounds);

            //Point pt = await DragAsync(_gridPlayer, (CardView)sender, e, dragList, hitTestC);
            //bool hitTest = DoesPointContainElement(pt, _gridPlayedCards);
            //Debug.WriteLine("Dropped at {0}. DoesPointContainElement returned {1}", pt, hitTest);
            //foreach (CardView card in dragList)
            //{
            //    Debug.WriteLine("dropped cards: {0} HitTest: {1}", card.CardName, hitTest);
            //    if (hitTest)
            //    {
            //        SelectCard(card, SelectState.NotSelected);
            //        //
            //        //  move it to _gridPlayedCards for real
            //        _gridPlayer.TransferCard(card, _gridPlayedCards);
            //        // call to the state machine to tell it the card(s) were moved
            //    }
            //}

            //_gridPlayedCards.UpdateCardLayoutAsync();
            //_gridPlayer.UpdateCardLayoutAsync();

            ////
            ////  when I'm done, there are no cards in the draglist (but may be cards in the selected list)

            //dragList.Clear();



        }

        //private bool DoesPointContainElement(Point hitTestPoint, FrameworkElement element)
        //{
        //    GeneralTransform gt = this.TransformToVisual(element);
        //    Point localPoint = gt.TransformPoint(hitTestPoint);
        //    if (localPoint.X < 0 || localPoint.Y < 0 || localPoint.X > element.ActualWidth || localPoint.Y > element.ActualHeight)
        //        return false;

        //    return true;
        //}


        private async Task TestDealAnimation()
        {
            HandsFromServer hfs = new HandsFromServer();

            hfs.ComputerCards = new List<CardView>();
            hfs.PlayerCards = new List<CardView>();

            for (int i = 0; i < 6; i++)
            {
                hfs.PlayerCards.Add(_gridDeck.Items[i]);
                hfs.ComputerCards.Add(_gridDeck.Items[i + 20]);
            }

            hfs.SharedCard = _gridDeck.Items[30];

            await DealAnimation(hfs);
        }

        private async Task DealAnimation(HandsFromServer hfs)
        {
            CardView card;
            List<Task<Object>> tasks = new List<Task<object>>();
            for (int i = 0; i < 6; i++)
            {

                card = hfs.PlayerCards[i];
                card.BoostZindex();
                card.Owner = Owner.Player;
                _gridDeck.MoveCardToTarget(card, _gridPlayer, tasks, MainPage.AnimationSpeeds.Medium, true);
                card = hfs.ComputerCards[i];
                card.Owner = Owner.Computer;
                card.BoostZindex();
                _gridDeck.MoveCardToTarget(card, _gridComputer, tasks, MainPage.AnimationSpeeds.Medium, true);

            }

            await Task.WhenAll(tasks);
            await _gridPlayer.FlipAllCards(CardOrientation.FaceUp);

            for (int i = 0; i < 6; i++)
            {
                _gridPlayer.Items[i].ResetZIndex();
                _gridComputer.Items[i].ResetZIndex();
            }

            hfs.SharedCard.BoostZindex(ZIndexBoost.SmallBoost);

        }

        public async void OnFlipAllCards(bool increment)
        {
            await OnTestFlipAllCards();
        }

        public async Task OnTestFlipAllCards()
        {
            CardOrientation orient = CardOrientation.FaceUp;

            if (_gridPlayer.Items.Count > 0)
            {
                if (_gridPlayer.Items[0].Orientation == CardOrientation.FaceUp)
                    orient = CardOrientation.FaceDown;
                else
                    orient = CardOrientation.FaceUp;


            }

            List<Task<Object>> tasks = new List<Task<object>>();

            _gridComputer.FlipAllCards(orient, tasks);
            _gridCrib.FlipAllCards(orient, tasks);
            _gridPlayedCards.FlipAllCards(orient, tasks);
            _gridPlayer.FlipAllCards(orient, tasks);

            await Task.WhenAll(tasks);
        }

        public async void OnGetComputerCrib(bool increment)
        {
            List<CardView> cards = new List<CardView>();
            cards.Add(_gridComputer.Items[2]);
            cards.Add(_gridComputer.Items[0]);
            await AnimateSelectComputerCribCards(cards);
        }

        private async Task AnimateSelectComputerCribCards(List<CardView> cards)
        {
            if (_gridComputer.Items.Count == 0) return;
            List<Task<Object>> tasks = new List<Task<object>>();
            _gridComputer.MoveCardToTarget(cards[1], _gridPlayedCards, tasks);
            _gridComputer.MoveCardToTarget(cards[0], _gridPlayedCards, tasks);
            await Task.WhenAll(tasks);
        }

        public async void OnMoveToCrib(bool increment)
        {

            await OnAnimateMoveCardsToCrib();


        }

        public async Task OnAnimateMoveCardsToCrib()
        {
            await _gridPlayedCards.FlipAllCards(CardOrientation.FaceDown);
            await _gridPlayedCards.MoveAllCardsToTarget(_gridCrib, MoveCardOptions.MoveAllAtSameTime);
            _ctrlCount.Show();
            _ctrlCount.Locate();
        }





    }

}
