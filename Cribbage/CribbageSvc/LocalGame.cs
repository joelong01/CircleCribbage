using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Xml;
using Cribbage;
using System.Collections.ObjectModel;

namespace CribbageService
{


    class LocalGame
    {
        private const string SERIALIZATION_VERSION = "1.1";

        private Player _computer = new Player("Computer");
        private Player _player = new Player("Your");
        private Deck _deck = null;
        private PlayerType _dealer = PlayerType.Player;
        private CountingPhase _countingPhase = null;

        public int CurrentCount
        {
            get
            {
                if (_countingPhase != null)
                    return _countingPhase.State.Count;

                return 0;
            }
        }

        public string Serialize()
        {
            string s = "[Game]\n";
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("Version", SERIALIZATION_VERSION);
            dict.Add("Dealer", _dealer.ToString());
            dict.Add("SharedCard", _deck.SharedCard.Serialize());
            s += StaticHelpers.SerializeDictionary(dict);
            s += "[Player]\n";
            s += _player.Serialize();
            s += "[Computer]\n";
            s += _computer.Serialize();
            s += SerializeCountingState();
            return s;

        }

        public string SerializeCountingState()
        {
            string s = "";

            if (_countingPhase != null)
            {
                s += "[Counting]\n";
                s += _countingPhase.Serialize();

                s += "[CountingState]\n";
                s += _countingPhase.State.Serialize();
            }
            return s;
        }

        public Deck Deck
        {
            get
            {
                return _deck;
            }
        }

        public bool Deserialize(Dictionary<string, string> sections, Deck deck)
        {
            if (sections.Count == 0)
                return false;
            Dictionary<string, string> game = StaticHelpers.DeserializeDictionary(sections["Game"]);
            if (sections == null)
                return false;

            if (game["Version"] != SERIALIZATION_VERSION)
                return false;

            _dealer = (PlayerType)Enum.Parse(typeof(PlayerType), game["Dealer"]);


            NewGame(_dealer, deck);

            CardView shared = StaticHelpers.CardFromString(game["SharedCard"], deck);            
            _deck.SharedCard = shared;

            _player.Deserialize(sections["Player"], deck);
            _computer.Deserialize(sections["Computer"], deck);
            string countingPhase;
            if (sections.TryGetValue("Counting",  out countingPhase))
            {

                _countingPhase = new CountingPhase(_player, _player.Hand.Cards, _computer, _computer.Hand.Cards);

                _countingPhase.Deserialize(countingPhase, deck);
                CountingState state = new CountingState(_player, _computer);
                state.Deserialize(sections["CountingState"]);
                _countingPhase.State = state;

            }

            return true;
        }

        public bool Deserialize(string s, Deck deck)
        {


            Dictionary<string, string> sections = StaticHelpers.GetSections(s);
            return Deserialize(sections, deck);

        }

       
        internal void SetSavedData(Deck deck, HandsFromServer hfs, List<CardView> cribCards, List<CardView> playedCards, PlayerType dealer, Dictionary<string, string> countingState)
        {

            _dealer = dealer;
            NewGame(_dealer, deck);
            _deck.SharedCard = hfs.SharedCard;
            _player.Hand = new Hand(hfs.PlayerCards);
            _computer.Hand = new Hand(hfs.ComputerCards);

            if (dealer == PlayerType.Player)
            {
                _player.Crib = new Crib(cribCards);
                _player.HasCrib = true;
                _computer.HasCrib = false;
            }
            else
            {
                _computer.Crib = new Crib(cribCards);
                _player.HasCrib = false;
                _computer.HasCrib = true;
            }

            if (countingState != null && countingState.Count != 0)
            {
                _countingPhase = new CountingPhase(_player, _player.Hand.Cards, _computer, _computer.Hand.Cards);

                _countingPhase.Deserialize(countingState);
                CountingState state = new CountingState(_player, _computer);

                _countingPhase.State = state;

            }

        }

        public bool ComputersCount()
        {
            if (_countingPhase == null)
                return false;

            return _countingPhase.State.ComputersCount;


        }

        /// <summary>
        ///  Starts a new game.  Returns True if it is player's turn, False if it is computer's turn
        /// </summary>
        /// <param name="playerDeal"></param>
        /// <returns></returns>
        public void NewGame(PlayerType dealer, Deck deck)
        {
            _deck = deck;
            _computer = new Player("Computer");
            _computer.Type = PlayerType.Computer;
            _player = new Player("Your");

            SetCribBoolean(dealer);
            _dealer = dealer;
            

        }

        private void SetCribBoolean(PlayerType dealer)
        {
            if (dealer == PlayerType.Player)
            {
                _computer.HasCrib = false;
                _player.HasCrib = true;
            }
            else
            {
                _computer.HasCrib = true;
                _player.HasCrib = false;
            }
        }

        private void ToggleDeal()
        {
            if (_dealer == PlayerType.Player)
            {
                _dealer = PlayerType.Computer;

            }
            else
            {
                _dealer = PlayerType.Player;
            }

            SetCribBoolean(_dealer);

        }

        /// <summary>
        ///     Gives a new random set of 13 cards
        ///     also changes who the dealer is
        ///     
        ///     you can pass in a set of cards for the game to deal (for testing only! :))
        /// </summary>        
        public HandsFromServer ShuffleAndReturnAllCards(HandsFromServer hfs = null)
        {
            if (hfs == null)
            {
                _deck.Shuffle();
                _player.Hand = new Hand(_deck.FirstHand);
                _computer.Hand = new Hand(_deck.SecondHand);

                hfs = new HandsFromServer();
                hfs.PlayerCards = _player.Hand.Cards;
                hfs.ComputerCards = _computer.Hand.Cards;
                hfs.SharedCard = _deck.SharedCard;
            }
            else
            {
                _player.Hand = new Hand(hfs.PlayerCards);
                _computer.Hand = new Hand(hfs.ComputerCards);
                _deck.SharedCard = hfs.SharedCard;
            }




            //
            //  if the shared card is a Jack, the dealer gets 2 points
            if (_deck.SharedCard.Rank == 11) // jack
            {
                if (_dealer == PlayerType.Player)
                    _player.Score += 2;
                else
                    _computer.Score += 2;
            }

            return hfs;


        }

        public List<CardView> GetCountedCards()
        {

            if (_countingPhase == null)
                return null;

            return _countingPhase.CountedCards;
        }


        public HandsFromServer GetHfs()
        {

            HandsFromServer hfs = new HandsFromServer();
            hfs.ComputerCards = _computer.Hand.Cards;
            hfs.PlayerCards = _player.Hand.Cards;
            hfs.SharedCard = GetSharedCard();
            return hfs;
        }


        /// <summary>
        ///  Sends two cards from the players to the crib
        /// </summary>

        public void SendToCrib(PlayerType type, List<CardView> cards)
        {

            if (_dealer == PlayerType.Player && type == PlayerType.Computer)
                throw new Exception("It is not the computer's crib");

            if (_dealer == PlayerType.Computer && type == PlayerType.Player)
                throw new Exception("It is not the player's crib");

            Player p = GetPlayer(type);
            p.Hand.RemoveCribCards(cards);
            p.AddToCrib(cards);

        }

        private void PullCardsFromDeck(List<CardView> deck, List<CardView> pull)
        {
            foreach (CardView card in pull)
            {

                deck.Remove(card);
            }


        }
        /// <summary>
        ///  Called after the user has dropped two cards for their crib.
        ///  the list shoudl have count == 4
        /// </summary>
        /// <param name="cards"></param>
        public void SendAllCardsToCrib(List<CardView> cards)
        {

            if (cards.Count != 4)
            {
                string s = String.Format("Invalid number of cards in crib.  Instead of 4, has {0}\n", cards.Count);
                foreach (CardView card in cards)
                {
                    s += card.Name + "\n";

                }
                throw new Exception(s);
            }

            PullCardsFromDeck(_player.Hand.Cards, cards);
            PullCardsFromDeck(_computer.Hand.Cards, cards);

            if (_dealer == PlayerType.Player)
                _player.AddToCrib(cards);
            else
                _computer.AddToCrib(cards);
        }

        public CardView GetSharedCard()
        {

            return _deck.SharedCard;

        }
        public CardView GetSuggestedCard(PlayerType type)
        {

            Player player = GetPlayer(type);

            CardView card = PickCard(player);
            return card;
        }

        private Player GetPlayer(PlayerType type)
        {
            if (type == PlayerType.Computer)
                return _computer;
            return _player;

        }

        public CardView PickCard(Player player)
        {

            if (_countingPhase == null)
            {
                //
                //  get a state machine to for the counting phase
                _countingPhase = new CountingPhase(_player, _player.Hand.Cards, _computer, _computer.Hand.Cards);
            }
            return _countingPhase.PickCard(player);
        }
        public List<CardView> GetSuggestedCrib(PlayerType type, GameDifficulty difficulty)
        {
            Player player = GetPlayer(type);
            List<CardView> list = player.Hand.PickCribCards(player.Hand.Cards, null, player.HasCrib, difficulty);
            return list;
        }
        public CountingData CountCard(PlayerType type, CardView card, GameDifficulty diffuculty)
        {
            Player player = GetPlayer(type);


            if (_countingPhase == null)
            {
                //
                //  get a state machine to for the counting phase
                _countingPhase = new CountingPhase(_player, _player.Hand.Cards, _computer, _computer.Hand.Cards);
            }

            // MainPage.LogTrace.WriteLogEntry("in countcard.  Turn is: {0}", _countingPhase.State.TurnPlayer.Name);

            CountingData data = new CountingData();

            Player playThisTurn = _countingPhase.State.TurnPlayer;
            Player playNextTurn = _countingPhase.State.NextTurnPlayer;

            if (playThisTurn.Type != type)
            {
                Debug.Assert(false, "Wrong turn encountered");
                //throw new Exception("Not your turn!");
            }

            CountingState state = _countingPhase.PlayCard(playThisTurn, card, diffuculty);


            data.Score = state.LastScore;
            data.CurrentCount = state.Count;
            data.ResetCount = state.ResetCount;
            data.CardId = card.Index;
            data.CardName = card.CardName;
            data.isGo = state.isGo;
            data.NextPlayer = state.TurnPlayer.Type;
            data.NextPlayerId = state.TurnPlayer.ID;
            data.NextPlayerCanGo = state.NextPlayerCanGo;
            data.ThisPlayerCanGo = state.ThisPlayerCanGo;
            data.CardsCounted = state.CardsCounted;
            data.ScoreStory = state.ScoreStory;
            data.NextPlayerIsComputer = (state.TurnPlayer.Type == PlayerType.Computer);
            data.CountBeforeReset = state.CountBeforeReset;

            if (data.CardsCounted == 8)
                _countingPhase = null;
            else
            {
                _countingPhase.State.ResetCount = false;
                _countingPhase.State.isGo = false;
            }

            return data;

        }
        //
        //  called after a muggins..
        public void UpdateScoreDirect(PlayerType playerType, ScoreType scoreType, int scoreDelta)
        {
            Player p = GetPlayer(playerType);
            p.Score += scoreDelta;
            if (scoreType == ScoreType.Crib) // this means that somebody called muggins on a crib, but the crib score has been entered...toggle the deal
                ToggleDeal();
        }
        /// <summary>
        ///     the score is set to what is in the Total field, not the ActualScore field
        /// </summary>
        /// <param name="type"></param>
        /// <param name="handType"></param>
        /// <param name="scoredelta"></param>
        /// <param name="difficulty"></param>
        /// <param name="muggins"></param>
        /// <returns></returns>
        public ScoreCollection UpdateScore(PlayerType type, HandType handType, long scoredelta, GameDifficulty difficulty)
        {

            ScoreCollection score = new ScoreCollection();

            if (handType == HandType.Crib)
            {
                score.ScoreType = ScoreType.Crib; ;
                score = UpdateScoreForCrib(type, scoredelta);
            }
            else
            {
                Player p = GetPlayer(type);
                score.ScoreType = ScoreType.Hand;
                if (p != null)
                {
                    int s = p.Hand.ScoreHand(_deck.SharedCard, score, handType);


                    if (p.Type == PlayerType.Computer)
                        scoredelta = s;

                    if (s == scoredelta)
                    {
                        score.Accepted = true;
                        p.Score += score.Total;


                    }
                }
               
            }
            return score;
        }

        
        public int GetScore(PlayerType type, HandType handType)
        {
            Player p = GetPlayer(type);
            ScoreCollection score = new ScoreCollection();

            if (handType == HandType.Regular)
            {
                score.ScoreType = ScoreType.Hand;
                return p.Hand.ScoreHand(_deck.SharedCard, score, handType);
            }



            score.ScoreType = ScoreType.Crib;

            return p.Crib.ScoreHand(_deck.SharedCard, score, handType);



        }

        public ScoreCollection UpdateScoreForCrib(PlayerType type, long scoredelta)
        {
            Player p = GetPlayer(type);

            ScoreCollection ss = new ScoreCollection();
            ss.ScoreType = ScoreType.Crib;
            ss.Accepted = false;
            if (p != null && p.HasCrib)
            {
                int score = p.Crib.ScoreHand(_deck.SharedCard, ss, HandType.Crib);


                if (p.Type == PlayerType.Computer)
                    scoredelta = score;

                if (score == scoredelta)
                {
                    ss.Accepted = true;
                    p.Score += score;

                }

            }
            if (ss.Accepted)
            {
                ToggleDeal();
            }
            else
            {
                MainPage.LogTrace.TraceMessageAsync(String.Format("Warning: Crib backScore not accepted! Deal not toggled."));
            }

            return ss;

        }
        public List<CardView> GetCrib(PlayerType type)
        {

            if ((_dealer == PlayerType.Player && type == PlayerType.Computer) || (_dealer == PlayerType.Computer && type == PlayerType.Player))
                throw new Exception("This player doesn't have the crib");

            Player p = GetPlayer(type);
            List<CardView> cards = new List<CardView>();
            foreach (CardView card in p.Crib.Cards)
            {
                card.SetOrientationAsync(CardOrientation.FaceDown, MainPage.AnimationSpeeds.Medium);
                card.Owner = Owner.Crib;
                cards.Add(card);
            }

            return cards;
        }


        public int GetCurrentScore(PlayerType type)
        {
            Player p = GetPlayer(type);
            return p.Score;


        }
        public PlayerType Dealer
        {
            get
            {
                return _dealer;
            }

        }


    }
}
