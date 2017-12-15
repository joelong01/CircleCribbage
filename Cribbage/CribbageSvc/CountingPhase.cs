using Facet.Combinatorics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Cribbage;

namespace CribbageService
{

    public class CountingState
    {
        Player _player = null;
        Player _computer = null;
        Player _turnPlayer = null;
        bool _startOver = false;
        int _nCardsCounted = 0;

        public ScoreCollection ScoreStory { get; set; }

        public bool ComputersCount
        {
            get
            {
                return (_turnPlayer.ID == _computer.ID);

            }
        }




        public string Serialize()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("CardsCounted", _nCardsCounted.ToString());
            dict.Add("StartOver", _startOver.ToString());
            dict.Add("Count", Count.ToString());
            bool playerTurn = (_turnPlayer.ID == _player.ID);
            dict.Add("PlayerTurn", playerTurn.ToString());
            return StaticHelpers.SerializeDictionary(dict, "\n");
        }

        public bool Deserialize(string s)
        {

            Dictionary<string, string> dict = StaticHelpers.DeserializeDictionary(s);
            if (dict == null)
                return false;
            _nCardsCounted = Convert.ToInt32(dict["CardsCounted"]);
            _startOver = Convert.ToBoolean(dict["StartOver"]);
            Count = Convert.ToInt32(dict["Count"]);
            bool playerTurn = Convert.ToBoolean(dict["PlayerTurn"]);
            if (playerTurn)
                _turnPlayer = _player;
            else
                _turnPlayer = _computer;
            return true;
        }

        public int CardsCounted
        {
            get
            {
                return _nCardsCounted;
            }
            set
            {
                _nCardsCounted = value;
            }
        }

        /// <summary>
        /// the count of the current counting phase (0...31)
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// if Count is 0 and IsGo == true, then this will hold the count that the state had prior to setting it to 0
        /// </summary>
        public int CountBeforeReset { get; set; }


        /// <summary>
        /// a PlayerID has a turn and can't go
        /// </summary>
        public bool isGo { get; set; }
        public bool ThisPlayerCanGo { get; set; }
        public bool NextPlayerCanGo { get; set; }
        /// <summary>
        /// this flag is set when neither player can play a card
        /// </summary>
        public bool ResetCount
        {
            get
            {
                return _startOver;
            }
            set
            {
                _startOver = value;
            }
        }
        /// <summary>
        /// Keeps track of whose turn it is
        /// </summary>
        public Player TurnPlayer
        {
            get
            {
                return _turnPlayer;
            }
            set
            {
                _turnPlayer = value;
                //  MainPage.LogTrace.WriteLogEntry(String.Format("TurnPlayer updated.  New Value is: {0}", _turnPlayer.Name));
            }
        }


        public Player NextTurnPlayer
        {
            get
            {
                if (_turnPlayer == null)
                    return null;

                if (_turnPlayer.ID == _player.ID)
                    return _computer;

                return _player;
            }

        }



        public void Start()
        {

            this.isGo = false;
            this.NextPlayerCanGo = true;
            this.ThisPlayerCanGo = true;

        }

        /// <summary>
        /// the nScore of the last card played
        /// </summary>
        /// <param name="playerCards"></param>
        /// <param name="computerCards"></param>
        public int LastScore { get; set; }



        public CountingState(Player player, Player computer)
        {
            Count = 0;
            ThisPlayerCanGo = true;
            NextPlayerCanGo = true;
            isGo = false;
            _player = player;
            _computer = computer;
            if (player.HasCrib)
                TurnPlayer = computer;
            else
                TurnPlayer = player;

            this.ScoreStory = new ScoreCollection();
        }


        internal void CanNotGo(Player turnPlayer)
        {
            if (turnPlayer == _player)
                this.ThisPlayerCanGo = false;
            else
                this.NextPlayerCanGo = false;
        }
    }



    public class CountingPhase
    {

        CountingState _state = null;
        List<CardView> _playerHand = new List<CardView>();
        List<CardView> _computerHand = new List<CardView>();
        List<CardView> _countedCards = new List<CardView>();
        List<CardView> _runCards = new List<CardView>();
        const int MUGGINS_CHANGE_ON_REGULAR = 5;
        const int MUGGINS_CHANGE_ON_HARD = 3;

        //
        //  TODO: why isn't the current count part of this?
        public string Serialize()
        {
            //
            //  don't serialize the playerHand and computerHand objects because they are 
            //  serialized by the LocalGame and passed in the ctor.  we pull the cards out
            //  based on the CountedCard list
            //
            Dictionary<string, string> dict = new Dictionary<string, string>();
             dict.Add("RunCards", StaticHelpers.SerializeFromList(_runCards));
            dict.Add("CountedCards", StaticHelpers.SerializeFromList(_countedCards));

            return StaticHelpers.SerializeDictionary(dict);

        }
        public bool Deserialize(string s, Deck deck)
        {
            Dictionary<string, string> dict = StaticHelpers.DeserializeDictionary(s);
            if (dict == null)
                return false;
           
            _runCards = StaticHelpers.DeserializeToList(dict["RunCards"], deck);
            _countedCards = StaticHelpers.DeserializeToList(dict["CountedCards"], deck);

            foreach (CardView card in _countedCards)
            {
                if (StaticHelpers.RemoveCardByValueFromList(_playerHand, card) == false)
                    StaticHelpers.RemoveCardByValueFromList(_computerHand, card);

            }


            return true;
        }

        public bool Deserialize(Dictionary<string, string> dict)
        {
            if (dict == null)
                return false;
            //  _playerHand = StaticHelpers.DeserializeToList(dict["PlayerHand"], false);
            //  _computerHand = StaticHelpers.DeserializeToList(dict["ComputerHand"], true);
            _runCards = StaticHelpers.DeserializeToList(dict["RunCards"]);
            _countedCards = StaticHelpers.DeserializeToList(dict["CountedCards"]);

            foreach (CardView card in _countedCards)
            {
                if (StaticHelpers.RemoveCardByValueFromList(_playerHand, card) == false)
                    StaticHelpers.RemoveCardByValueFromList(_computerHand, card);

            }


            return true;
        }
        public CountingState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }

        }

        public List<CardView> CountedCards
        {
            get
            {
                return _countedCards;
            }

        }



        private List<CardView> CopyListByValue(List<CardView> copyFrom)
        {
            List<CardView> copyTo = new List<CardView>();
            foreach (CardView c in copyFrom)
            {
                CardView copy = new CardView(c);
                copyTo.Add(c);
            }

            return copyTo;

        }
        /// <summary>
        /// This should last for the length of one turn.  after each deal, the Match should get a new counting phase
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerCards"></param>
        /// <param name="computer"></param>
        /// <param name="computerCards"></param>
        public CountingPhase(Player player, List<CardView> playerCards, Player computer, List<CardView> computerCards)
        {

            if (playerCards.Count != 4 || computerCards.Count != 4)
                throw new WebException("Invalid number of cards in counting phase");


            _playerHand.AddRange(playerCards);
            _playerHand.Sort(CardView.CompareCardsByRank);

            _computerHand.AddRange(computerCards);
            _computerHand.Sort(CardView.CompareCardsByRank);

            _state = new CountingState(player, computer);



        }
        /// <summary>
        /// a web request comes into the service to play a card during counting phase.
        /// 
        /// we
        /// 1. Verify it is the right turn
        /// 2. Verify the card is in the hand
        /// 3. Count the card
        /// 4. remove the card
        /// 5. set the turn state
        /// 6. return the state of the count
        /// 
        /// Whoever should go next should be definitively set
        /// </summary>
        /// <param name="PlayerId"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public CountingState PlayCard(Player player, CardView card, GameDifficulty diffuculty)
        {


            if (_state.TurnPlayer != player)
            {
                throw new Exception("Not your turn!");
            }

            List<CardView> cards = _playerHand;
            if (player.Type == PlayerType.Computer) cards = _computerHand;


            if (cards.Contains(card))
            {

                ScoreCollection score = null;
                if (CountCard(card, out score) != -1)
                {
                    _state.ScoreStory = score;
                    _state.LastScore = score.Total;
                    cards.Remove(card);
                    if (!HasCardsToPlay)
                    {
                        _state.LastScore++; // point for last card  
                        score.Scores.Add(new ScoreInstance(StatName.CountingLastCard, 1, 1, card.Index));
                        score.Total++;
                    }

                    score.ActualScore = score.Total;

                    

                }
            }

            UpdateTurnState();

            MainPage.LogTrace.TraceMessageAsync(String.Format("In PlayCard.  Player:{0} Score:{1} Adding:{2} CardPlayed:{3}", player.Type, player.Score, _state.LastScore, card.Data.Name));
            player.Score += _state.LastScore;



            return _state;

        }

       

      
        /// <summary>
        ///  set the count to 0 and pick the person who should go next
        /// </summary>
        public void Reset()
        {
            _state.Count = 0;
            if (HasCardsToPlay)
            {
                UpdateTurnState();

            }


        }

        /// <summary>
        /// peek ahead to see which player can play a card.  if neither player can play a card, it is up to the Match object to check the Reset bit, record
        /// information about this play, and then reset the count
        /// </summary>
        private void UpdateTurnState()
        {

            if (!HasCardsToPlay)
                return; // done with counting

            Player currentPlayer = _state.TurnPlayer;
            Player nextPlayer = _state.NextTurnPlayer;

            CardView card = PickCard(nextPlayer);
            if (card == null)
                _state.NextPlayerCanGo = false;
            else
                _state.NextPlayerCanGo = true;

            card = PickCard(currentPlayer);
            if (card == null)
                _state.ThisPlayerCanGo = false;
            else
                _state.ThisPlayerCanGo = true;


            if (_state.NextPlayerCanGo)
            {
                _state.TurnPlayer = nextPlayer;
            }
            else if (_state.ThisPlayerCanGo)
            {
                _state.TurnPlayer = currentPlayer;

            }
            else
            {

                if (_state.Count < 31)
                {
                    //  Debug.WriteLine("Setting isGo to true");
                    _state.isGo = true;
                    _state.LastScore++;
                    List<int> indexList = new List<int>();
                    indexList.Add(_countedCards[0].Index); // we isert into the list at the head
                    ScoreInstance score = new ScoreInstance(StatName.CountingGo, 1, 1, indexList);
                    _state.ScoreStory.Scores.Add(score);
                    _state.ScoreStory.Total++;

                }

                _state.ResetCount = true;
                _state.CountBeforeReset = _state.Count;
                _state.Count = 0;

                _countedCards.Clear();

                UpdateTurnState();  //recurse
            }


        }

        /// <summary>
        /// by the time we've gotten here, we've verified that Card is valid
        /// </summary>
        /// <param name="card"></param>
        private int CountCard(CardView card, out  ScoreCollection score)
        {
            int nScore = 0;

            score = new ScoreCollection();
            score.ScoreType = ScoreType.Count;

            if (card.Value + _state.Count > 31)
            {
                throw new Exception("The total count must be less than 31. Play a different card");

            }
            List<int> cardIndeces = new List<int>();
            if (card.Value + _state.Count == 15)
            {
                cardIndeces = Hand.CardListToIntList(_countedCards);
                cardIndeces.Add(card.Index);
                nScore += 2;
                score.Scores.Add(new ScoreInstance(StatName.CountingHit15, 1, 2, cardIndeces));
                cardIndeces.Clear();
            }

            if (card.Value + _state.Count == 31)
            {                
                nScore += 2;
                score.Scores.Add(new ScoreInstance(StatName.CountingHit31, 1, 2, card.Index));
                cardIndeces.Clear();
            }

            _countedCards.Insert(0, card);
            int run = FindARun(_countedCards);
            StatName statName = StatName.Ignored;
            if (run > 0)
            {

                switch (run)
                {
                    case 3:
                        statName = StatName.Counting3CardRun;                        
                        break;
                    case 4:
                        statName = StatName.Counting4CardRun;
                        break;
                    case 5:
                        statName = StatName.Counting5CardRun;
                        break;
                    case 6:
                        statName = StatName.Counting6CardRun;
                        break;
                    case 7:
                        statName = StatName.Counting7CardRun;
                        break;
                    default:
                       MainPage.LogTrace.TraceMessageAsync(String.Format("ERROR: Bug in CountCard looking for run length! run: {0}", run));
                        break;

                }
                score.Scores.Add(new ScoreInstance(statName, run, run, Hand.CardListToIntList(_countedCards, run)));                
            }
            nScore += run;
            int pairs = FindPairs(_countedCards);
            if (pairs > 0)
            {
                switch (pairs) // this is points from pairs, not pairs count
                {
                    case 2:
                        cardIndeces = Hand.CardListToIntList(_countedCards, 2);
                        statName = StatName.CountingPair;
                        break;
                    case 6:
                        cardIndeces = Hand.CardListToIntList(_countedCards, 3);
                        statName = StatName.Counting3OfAKind;
                        break;
                    case 12:
                        cardIndeces = Hand.CardListToIntList(_countedCards, 4);
                        statName = StatName.Counting4OfAKind;
                        break;
                    default:
                        MainPage.LogTrace.TraceMessageAsync(String.Format("ERROR: Bug in CountCard looking for pairs! pairs:{0}", pairs));
                        break;

                }

                score.Scores.Add(new ScoreInstance(statName, 1, pairs, cardIndeces));

            }

            nScore += pairs;
            _state.Count += card.Value;
            _state.LastScore = nScore;
            _state.CardsCounted++;

            score.Total = nScore;
            return nScore;
        }
        public int Count
        {
            get
            {
                return _state.Count;
            }
            set
            {

                _state.Count = value;
            }

        }
        private int FindPairs(List<CardView> cards)
        {

            int run = 1;
            for (int i = 0; i < cards.Count - 1; i++)
            {
                if (cards[i].Rank == cards[i + 1].Rank)
                {
                    run++;
                }
                else
                    break;
            }

            int score = 0;
            switch (run)
            {
                case 2:
                    score = 2;
                    break;
                case 3:
                    score = 6;
                    break;
                case 4:
                    score = 12;
                    break;
                default:
                    score = 0;
                    break;
            }

            return score;
        }

        private int IsRun(List<CardView> cards)
        {

            if (cards.Count < 3)
                return -1;


            int run = 1;
            for (int i = 0; i < cards.Count - 1; i++)
            {
                if (cards[i].Rank == cards[i + 1].Rank - 1) // up by one
                {
                    run++;
                }
            }
            return run;
        }
        /// <summary>
        ///  some rules to remember
        ///  1. it isn't a run unless the card you added counts in the run
        ///  2. if first n cards have to be part of an n card run for it to count
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        private int FindARun(List<CardView> cards)
        {

            if (cards.Count < 3)
                return 0;


            List<CardView> copy = new List<CardView>();

            int run = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                copy.Add(cards[i]);
                copy.Sort(CardView.CompareCardsByRank);
                if (IsRun(copy) == i + 1)
                {
                    run = i + 1;
                }
            }

            if (run < 3) run = 0;
            return run;



        }

        /// <summary>
        ///  the algo for picking a card for the comptuer to play
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public CardView PickCard(Player player)
        {


            if (player == null) return null;


            List<CardView> myCards = _playerHand;
            if (player.Type == PlayerType.Computer) myCards = _computerHand;

            if (myCards.Count == 0)
            {
                return (CardView)null;

            }

            if (myCards.Count == 1)
            {
                if (myCards[0].Value + _state.Count > 31)
                    return (CardView)null;

                return myCards[0];
            }

            //
            //
            //  see if there is no valid card to play so we don't get screwed below
            bool atLeastOneValidCard = false;



            //
            //  first see if we can nScore points                
            foreach (CardView c in myCards)
            {


                if (c.Value + _state.Count > 31)
                    continue;

                atLeastOneValidCard = true;

                if (c.Value + _state.Count == 15)
                    return c;


                if (_countedCards.Count > 0)
                    if (c.Rank == _countedCards[0].Rank)
                        return c;

                if (c.Value + _state.Count == 31)
                    return c;

                _countedCards.Add(c);
                if (FindARun(_countedCards) > 0)
                {
                    _countedCards.Remove(c);
                    return c;
                }
                _countedCards.Remove(c);

            }

            if (!atLeastOneValidCard)
                return null;

            //
            //  play a card that we have a pair so we can get 3 of a kind - as long as it isn't a 5 and the 3 of a kind makes > 31
            //

            for (int i = 0; i < myCards.Count - 1; i++)
            {

                //  dont' do it if it will force us over 31
                if (myCards[i].Rank * 3 + _state.Count > 31)
                    continue;

                if (myCards[i].Rank == myCards[i + 1].Rank)
                {
                    if (myCards[i].Rank != 5)
                        return myCards[i];

                }
            }

            //
            //  play a card next to it in Rank, with a hope that we get a run going -- as long as it will help us
            //

            //  TODO...


            //
            //  make the right choice if assuming they'll play a 10
            //
            Combinations<CardView> combinations = new Combinations<CardView>(myCards, 2); // at most 6 of these: 4 choose 2
            foreach (List<CardView> cards in combinations)
            {
                int sum = cards[0].Value + cards[1].Value;
                if (sum + _state.Count == 5) // i'll 15 them if they play a 10
                    return cards[1];

                if (sum + _state.Count == 21) // i'll 31 them if they play a 10
                    return cards[1];

            }


            //
            //  if the count is less than 5, try not to play a 10...but don't play a 5
            //
            CardView lastCard = null;
            int index = myCards.Count;
            if (_state.Count < 5)
            {
                do
                {
                    index--;
                    lastCard = myCards[index];

                } while (index > 0 && lastCard.Value != 5 && lastCard.Value == 10);

                if (lastCard.Value > 5)
                    return lastCard;
            }


            //
            //  can't nScore points, and nothing special if they play a 10 - play the biggest card we have
            //  unless it is a 5.  note: sorted smallest to largest, so go
            //  backwards through the collection
            index = myCards.Count;
            for (index = myCards.Count - 1; index >= 0; index--)
            {

                lastCard = myCards[index];
                if (lastCard.Value + _state.Count > 31)
                    continue;



                if (lastCard.Value != 5)
                    break;

            }

            if (lastCard.Value + _state.Count > 31) return null;

            return lastCard;



        }




        public bool HasCardsToPlay
        {
            get
            {
                return (_state.CardsCounted < 8);

            }
        }
    }


}