using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Facet.Combinatorics;
using Cribbage;
using System.Collections.ObjectModel;

namespace CribbageService
{
    [DataContract(Name = "Hand")]
    public class Hand
    {
        protected List<CardView> _cards = new List<CardView>();
        public Hand() { }

        override public string ToString()
        {
            string s = "";
            int count = 0;
            foreach (CardView c in _cards)
            {
                s += String.Format("Count:{0}\t {1} \n", count++, c.ToString());
            }


            return s;

        }
        public Hand(List<CardView> cards)
        {
            _cards = cards;

        }

        [DataMember]
        public List<CardView> Cards
        {
            get
            {
                return _cards;
            }
            set
            {
                _cards = value;
            }
        }



        public bool RemoveCribCards(List<CardView> crib)
        {
            if (crib.Count != 2)
                return false;


            _cards.Remove(crib[0]);
            _cards.Remove(crib[1]);
            return true;

        }

        public List<CardView> RemoveCribCards(CardView card1, CardView card2)
        {

            List<CardView> crib = new List<CardView>();
            crib.Add(card1);
            crib.Add(card2);
            RemoveCribCards(crib);
            return crib;

        }
        public int ScoreCards(List<CardView> list, ScoreCollection scoreStory, HandType handType)
        {
            int score = 0;

            score += ScoreNibs(list, scoreStory, handType);                    // this is the only one where it matters which particular card is shared

            //
            //   DON't SORT BEFORE NIBS!!!
            list.Sort(CardView.CompareCardsByRank);

            score += ScoreFifteens(list, scoreStory, handType);
            score += ScorePairs(list, scoreStory, handType);
            score += ScoreRuns(list, scoreStory, handType);
            score += ScoreFlush(list, scoreStory, handType);


            scoreStory.Total = score;
            scoreStory.ActualScore = score;


            return score;


        }

        public int ScoreHand(CardView sharedCard, ScoreCollection scoreStory, HandType handType)
        {
            int score = 0;
            List<CardView> list = new List<CardView>(_cards);
            list.Add(sharedCard);
            score = ScoreCards(list, scoreStory, handType);

            string trace = "Cards: ";
            foreach (CardView c in _cards)
            {
                trace += c.Data.Name.ToString() + " ";
            }

            trace += "Shared: " + sharedCard.Data.Name.ToString();
            trace += " Score: " + score;

            MainPage.LogTrace.TraceMessageAsync(trace);

            return score;

        }

        private int ScoreNibs(List<CardView> list, ScoreCollection scoreStory, HandType handType)
        {

            if (list.Count == 4) // shared card, not passed in, we can't tell if we have nibs
                return 0;

            for (int i = 0; i < 4; i++)
            {
                if (list[i].Rank == 11) //Jack -- 1 indexed
                {
                    if (list[i].Suit == list[4].Suit)
                    {
                        StatName name = (handType == HandType.Regular) ? StatName.HandJackOfTheSameSuit : StatName.CribJackOfTheSameSuit;
                        scoreStory.Scores.Add(new ScoreInstance(name, 1, 1, list[i].Index));
                        return 1;
                    }
                }

            }

            return 0;

        }

        private int Nibs(List<CardView> list)
        {

            if (list.Count == 4) // shared card, not passed in, we can't tell if we have nibs
                return 0;

            for (int i = 0; i < 4; i++)
            {
                if (list[i].Rank == 11) //Jack -- 1 indexed
                {
                    if (list[i].Suit == list[4].Suit)
                    {
                        return 1;
                    }
                }

            }

            return 0;
        }

        private int ScoreFlush(List<CardView> list, ScoreCollection scoreStory, HandType handType)
        {

            List<int>[] cards = new List<int>[4];
            for (int i = 0; i < 4; i++)
            {
                cards[i] = new List<int>();
            }
            foreach (CardView c in list)
            {

                switch (c.Suit)
                {
                    case Suit.Clubs:
                        cards[0].Add(c.Index);
                        break;
                    case Suit.Hearts:
                        cards[1].Add(c.Index);
                        break;
                    case Suit.Diamonds:
                        cards[2].Add(c.Index);
                        break;
                    case Suit.Spades:
                        cards[3].Add(c.Index);
                        break;

                }



            }

            int minCardsForFlush = 4;

            if (handType == HandType.Crib) minCardsForFlush++;

            int score = 0;

            for (int i = 0; i < 4; i++)
            {

                if (cards[i].Count >= minCardsForFlush)
                {
                    StatName name = StatName.Ignored;
                    switch (cards[i].Count)
                    {
                        case 4:
                            name = StatName.Hand4CardFlush; // no 4 card flush in crib
                            break;
                        case 5:
                            name = (handType == HandType.Regular) ? StatName.Hand5CardFlush : StatName.Crib5CardFlush;
                            break;
                        default:
                            MainPage.LogTrace.TraceMessageAsync(String.Format("ERROR: flushcount wrong. count:{0}", cards[i].Count));
                            break;

                    }
                    score = cards[i].Count;
                    scoreStory.Scores.Add(new ScoreInstance(name, 1, cards[i].Count, cards[i]));
                    break;

                }

            }

            return score;
        }


        //
        //  optimize -- if you get 2 different suits and you are a crib, break
        //  optimize -- if you get 2 different cards in 2 different suits, break
        //  optimize -- if you get 3 different suits, break
        private int TSGetFlush(List<CardView> list, HandType handType)
        {
            int score = 0;
            int[] count = new int[4];
            foreach (CardView c in list)
            {

                switch (c.Suit)
                {
                    case Suit.Clubs:
                        count[0]++;
                        break;
                    case Suit.Hearts:
                        count[1]++;
                        break;
                    case Suit.Diamonds:
                        count[2]++;
                        break;
                    case Suit.Spades:
                        count[3]++;
                        break;

                }



            }

            int minCardsForFlush = 4;

            if (handType == HandType.Crib) minCardsForFlush++;


            for (int i = 0; i < 4; i++)
            {

                if (count[i] >= minCardsForFlush)
                {
                    score = count[i];
                    break;
                }

            }

            return score;
        }


        //
        //  just call ScoreRuns to get ths list..
        private int ScoreRuns(List<CardView> list, ScoreCollection scoreStory, HandType handType)
        {
            List<List<CardView>> cardLists = DemuxPairs(list);
            List<List<CardView>> runs = new List<List<CardView>>();
            List<ScoreInstance> scores = new List<ScoreInstance>();
            foreach (List<CardView> cards in cardLists)
            {
                List<CardView> l = GetRuns(cards);
                if (l != null)
                {
                    runs.Add(l);
                }
            }
            //
            //  eliminate duplicate lists - this happens if you have a hand that looks like 5, 5, 7, 8, 9 where the pair is not in the run
            if (runs.Count == 2)
            {
                if (runs[0].Count == runs[1].Count) // same length
                {
                    bool same = false;
                    for (int i = 0; i < runs[0].Count; i++)
                    {
                        if (runs[0][i] != runs[1][i])
                        {
                            same = false;
                            break;
                        }
                        
                        same = true;

                    }

                    if (same)
                    {
                        runs.RemoveAt(1);
                    }
                }
            }
                

            //
            //  runs now how the list of cards that have runs in them
            int score = 0;
            {

                foreach (List<CardView> cards in runs)
                {
                    if (cards.Count > 2)
                    {
                        StatName name = StatName.Ignored;
                        switch (cards.Count)
                        {
                            case 3:
                                name = (handType == HandType.Regular) ? StatName.Hand3CardRun : StatName.Crib3CardRun;
                                break;
                            case 4:
                                name = (handType == HandType.Regular) ? StatName.Hand4CardRun : StatName.Crib4CardRun;
                                break;
                            case 5:
                                name = (handType == HandType.Regular) ? StatName.Hand5CardRun : StatName.Crib5CardRun;
                                break;
                            default:
                                MainPage.LogTrace.TraceMessageAsync(String.Format("ERRROR: invalid Maxrun.  id:2109347809. maxrun: {0}", cards.Count));
                                break;

                        }
                        score += cards.Count;
                        scoreStory.Scores.Add(new ScoreInstance(name, 1, cards.Count, CardListToIntList(cards)));
                    }
                }
            }

            return score;

        }

        bool Is3CardRun(CardView card1, CardView card2, CardView card3)
        {
            if (card1.Rank == card2.Rank - 1 &&
                card2.Rank == card3.Rank - 1)
            {
                return true;
            }

            return false;

        }
        private List<List<CardView>> DemuxPairs(List<CardView> list)
        {
            List<List<CardView>> cardList = new List<List<CardView>>();

            CardView previousCard = null;
            int consecutive = 0;
            int pairs = 0;
            foreach (CardView thisCard in list)
            {
                if (previousCard == null)
                {
                    cardList.Add(new List<CardView>());
                    cardList[0].Add(thisCard);
                }
                else if (previousCard.Rank != thisCard.Rank)
                {
                    consecutive = 0;
                    foreach (List<CardView> cards in cardList)
                    {
                        cards.Add(thisCard);
                    }
                }
                else if (previousCard.Rank == thisCard.Rank) // pair
                {
                    consecutive++;
                    pairs++;

                    if ((consecutive == 1 && pairs == 1) || (consecutive == 2 && pairs == 2))
                    {
                        int count = cardList.Count;
                        List<CardView> newList = new List<CardView>(cardList[count - 1]);
                        cardList.Add(newList);
                        newList.Remove(previousCard);
                        newList.Add(thisCard);
                    }
                    else if (consecutive == 1 && pairs == 2)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            List<CardView> newList = new List<CardView>(cardList[k]);
                            newList.Remove(previousCard);
                            newList.Add(thisCard);
                            cardList.Add(newList);
                        }

                    }

                }

                previousCard = thisCard;

            }

            return cardList;

        }
        //
        //   3, four of 5 cards can be passed in 
        private List<CardView> GetRuns(List<CardView> list)
        {
            int count = list.Count;
            if (count < 3)
                return null;


            if (Is3CardRun(list[0], list[1], list[2]))
            {
                if (count > 3 && list[2].Rank == list[3].Rank - 1)
                {
                    if (count > 4 && list[3].Rank == list[4].Rank - 1)
                    {

                        return new List<CardView>(list); // 5 card run
                    }
                    else
                    {
                        if (count > 4)
                            list.RemoveAt(4);
                        return new List<CardView>(list); // 4 card run
                    }

                }
                else
                {

                    if (count > 4)
                        list.RemoveAt(4);
                    if (count > 3)
                        list.RemoveAt(3);

                    return new List<CardView>(list); // 3 card run
                }

            }
            else if (count > 3 && Is3CardRun(list[1], list[2], list[3]))
            {
                if (count > 4 && list[3].Rank == list[4].Rank - 1)
                {
                    list.RemoveAt(0);
                    return new List<CardView>(list); // 4 card run
                }
                else
                {
                    if (count > 4)
                        list.RemoveAt(4);

                    if (count > 3)
                        list.RemoveAt(0);

                    return new List<CardView>(list); // 3 card run
                }
            }
            else if (count > 4 && Is3CardRun(list[2], list[3], list[4]))
            {
                list.RemoveAt(1);
                list.RemoveAt(0);
                return new List<CardView>(list); // 3 card run
            }

            return null;
        }



        private int ScorePairs(List<CardView> list, ScoreCollection scoreStory, HandType handType)
        {
            List<List<CardView>> listlist = GetPairCards(list);
            int score = 0;
            foreach (List<CardView> cards in listlist)
            {
                int cardCount = cards.Count;

                StatName name = StatName.Ignored;
                if (cardCount == 2)
                {
                    name = (handType == HandType.Regular) ? StatName.HandPairs : StatName.CribPairs;
                    score += 2;
                }
                else if (cardCount == 3)
                {
                    name = (handType == HandType.Regular) ? StatName.Hand3OfAKind : StatName.Crib3OfAKind;
                    score += 6;
                }
                else if (cardCount == 4)
                {
                    score += 12;
                    name = (handType == HandType.Regular) ? StatName.Hand4OfAKind : StatName.Crib4OfAKind;
                }

                scoreStory.Scores.Add(new ScoreInstance(name, 1, score, CardListToIntList(cards)));

            }

            return score;

        }

        public static List<int> CardListToIntList(List<CardView> cards)
        {
            List<int> list = new List<int>();
            foreach (CardView card in cards)
            {
                list.Add(card.Index);
            }
            return list;
        }

        public static List<int> CardListToIntList(List<CardView> cards, int firstN)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < firstN; i++ )
            {
                list.Add(cards[i].Index);
            }
            return list;
        }

        List<List<CardView>> GetPairCards(List<CardView> cardList)
        {
            List<List<CardView>> listlist = new List<List<CardView>>();

            CardView c1 = cardList[0];
            CardView c2 = cardList[1];
            CardView c3 = cardList[2];
            CardView c4 = cardList[3];
            CardView c5 = null;
            if (cardList.Count == 5)
                c5 = cardList[4];

            bool ThreeOfAKind = false;
            bool FourOfAKind = false;

            if (c1.Rank == c2.Rank)
            {
                List<CardView> list = new List<CardView>();
                list.Add(c1);
                list.Add(c2);
                if (c2.Rank == c3.Rank)
                {
                    // 3 of a kind
                    list.Add(c3);
                    ThreeOfAKind = true;
                    if (c3.Rank == c4.Rank)
                    {
                        list.Add(c4);

                    }
                }

                listlist.Add(list);
                if (FourOfAKind)
                    return listlist;
            }

            if (c2.Rank == c3.Rank && !ThreeOfAKind)
            {
                List<CardView> list = new List<CardView>();
                list.Add(c2);
                list.Add(c3);
                if (c3.Rank == c4.Rank)
                {
                    list.Add(c4);
                    ThreeOfAKind = true;
                    if (c5 != null && c4.Rank == c5.Rank)
                    {
                        FourOfAKind = true;
                        list.Add(c5);
                    }
                    else
                    {
                        FourOfAKind = false;
                    }
                }
                else
                {
                    ThreeOfAKind = false;
                }

                listlist.Add(list);
                if (FourOfAKind)
                    return listlist;

            }

            if (c3.Rank == c4.Rank && !(ThreeOfAKind))
            {

                List<CardView> list = new List<CardView>();
                list.Add(c3);
                list.Add(c4);

                if (c5 != null && c4.Rank == c5.Rank)
                {
                    ThreeOfAKind = true;
                    list.Add(c5);
                }

                listlist.Add(list);
            }
            else
            {
                ThreeOfAKind = false;
            }

            if (c5 != null && c4.Rank == c5.Rank && !(ThreeOfAKind))
            {
                List<CardView> list = new List<CardView>();
                list.Add(c4);
                list.Add(c5);
                listlist.Add(list);

            }

            return listlist;

        }

     
  

        int ScoreFifteens(List<CardView> list, ScoreCollection scoreStory, HandType handType)
        {
            int score = 0;
            List<List<CardView>> listlist = ScoreFifteensInternal(list);
            foreach (List<CardView> cards in listlist)
            {

                StatName name = (handType == HandType.Regular) ? StatName.Hand15s : StatName.Crib15s;
                scoreStory.Scores.Add(new ScoreInstance(name, 1, 2, CardListToIntList(cards)));
                score += 2;
            }
            return score;
        }

        List<List<CardView>> ScoreFifteensInternal(List<CardView> CardList)
        {

            int score = 0;
            int iVal = 0;
            int ijVal = 0;
            int ijkVal = 0;
            int ijkxVal = 0;

            List<List<CardView>> listlist = new List<List<CardView>>();

            for (int i = 0; i < CardList.Count; i++)
            {
                iVal = CardList[i].Value;
                for (int j = i + 1; j < CardList.Count; j++)
                {


                    ijVal = CardList[j].Value + iVal;
                    if (ijVal > 15)
                        break; //because we are ordered;
                    if (ijVal == 15)
                    {

                        score += 2;
                        List<CardView> list = new List<CardView>();
                        list.Add(CardList[i]);
                        list.Add(CardList[j]);
                        listlist.Add(list);
                    }
                    else
                    {
                        //
                        // here because ijVal < 15
                        for (int k = j + 1; k < CardList.Count; k++)
                        {
                            ijkVal = CardList[k].Value + ijVal;
                            if (ijkVal > 15)
                                break;

                            if (ijkVal == 15)
                            {
                                score += 2;

                                List<CardView> list = new List<CardView>();
                                list.Add(CardList[i]);
                                list.Add(CardList[j]);
                                list.Add(CardList[k]);
                                listlist.Add(list);

                                // don't break here -- it might be a pair like 5555

                            }
                            else
                            {
                                //
                                // here because ijkVal < 15
                                for (int x = k + 1; x < CardList.Count; x++)
                                {

                                    ijkxVal = CardList[x].Value + ijkVal;
                                    if (ijkxVal > 15)
                                        break;
                                    if (ijkxVal == 15)
                                    {
                                        List<CardView> list = new List<CardView>();
                                        list.Add(CardList[i]);
                                        list.Add(CardList[j]);
                                        list.Add(CardList[k]);
                                        list.Add(CardList[x]);
                                        listlist.Add(list);
                                        score += 2;
                                    }

                                    if (CardList.Count == 5) // if the shared card is passed in...
                                    {
                                        int sumAll = ijkVal + CardList[3].Value + CardList[4].Value;
                                        if (sumAll == 15) // takes all 5...
                                        {
                                            List<CardView> list = new List<CardView>();
                                            list.Add(CardList[i]);
                                            list.Add(CardList[j]);
                                            list.Add(CardList[k]);
                                            list.Add(CardList[3]);
                                            list.Add(CardList[4]);
                                            listlist.Add(list);
                                            score += 2;
                                            return listlist;
                                        }

                                        if (sumAll < 15) // not enough points to get to 15 with all 5 cards
                                        {
                                            return listlist;
                                        }


                                    }
                                }
                            }
                        }
                    }
                }
            }

            return listlist;
        }

        private double ExpectedMaxCribScore(List<CardView> cards)
        {
            double score = 0;
            double max = 0;
            int iMax = 0;
            int jMax = 1;



            int i = 0;
            int j = 0;

            for (i = 0; i < 5; i++)
            {
                for (j = i + 1; j < 5; j++)
                {

                    score = CribbageStats.dropTable[_cards[i].Rank - 1, _cards[j].Rank - 1];
                    if (score > max)
                    {
                        max = score;
                        iMax = i;
                        jMax = j;
                    }
                }
            }

            return score;
        }

        public List<CardView> PickCribCards(List<CardView> hand, CardView sharedCard, bool myCrib, GameDifficulty diffuculty)
        {

            Combinations<CardView> combinations = new Combinations<CardView>(hand, 4);
            List<CardView> maxCrib = null;
            double maxScore = -1000.0;

            foreach (List<CardView> cards in combinations)
            {
                if (sharedCard != null)
                {
                    cards.Add(sharedCard);
                }
                ScoreCollection scoreStory = new ScoreCollection();
                double score = (double)ScoreCards(cards, scoreStory, HandType.Regular);
                List<CardView> crib = GetCrib(hand, cards);
                double expectedValue = 0.0;
                if (diffuculty == GameDifficulty.Hard)
                {
                    if (myCrib)
                    {
                        expectedValue = CribbageStats.dropTableToMyCrib[crib[0].Rank - 1, crib[1].Rank - 1];
                        score += expectedValue;
                    }
                    else
                    {
                        expectedValue = CribbageStats.dropTableToYouCrib[crib[0].Rank - 1, crib[1].Rank - 1];
                        score -= expectedValue;
                    }
                }

                if (score > maxScore)
                {
                    maxScore = score;
                    maxCrib = crib;
                }
            }

            return maxCrib;

        }

        private List<CardView> GetCrib(List<CardView> hand, List<CardView> holdCards)
        {
            List<CardView> crib = new List<CardView>(hand);

            foreach (CardView card in holdCards)
            {
                crib.Remove(card);
            }
            return crib;
        }



        public static void DumpCards(List<CardView> cards, string description)
        {
            MainPage.LogTrace.TraceMessageAsync(description);
            foreach (CardView card in cards)
            {
                MainPage.LogTrace.TraceMessageAsync(String.Format("Index: {0}\tName:{1}", card.Index, card.Name));
            }
        }

    }



    public static class CribbageStats
    {
        public static int[,] TwoCard = new int[,] { { 0, 1 }, { 0, 2 }, { 0, 3 }, { 0, 4 }, { 1, 2 }, { 1, 3 }, { 1, 4 }, { 2, 3 }, { 3, 4 }, { 3, 4 } };
        public static int[, ,] ThreeCard = new int[,,] { { { 0, 1, 2 }, { 0, 1, 3 }, { 0, 1, 4 }, { 0, 2, 3 }, { 0, 2, 4 }, { 0, 3, 4 }, { 1, 2, 3 }, { 1, 2, 4 }, { 1, 3, 4 }, { 2, 3, 4 } } };
        public static int[, , ,] FourCard = new int[,,,] { { { { 0, 1, 2, 3 }, { 0, 1, 2, 4 }, { 1, 2, 3, 4 } } } };
        public static int[] FiveCard = new int[] { 0, 1, 2, 3, 4 };
        public static int[] TwoCardList = new int[] { 0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 1, 3, 1, 4, 2, 3, 3, 4 };

        public static double[,] dropTable = new double[,]{
               {5.26, 4.18, 4.47, 5.45, 5.48, 3.80, 3.73, 3.70, 3.33, 3.37, 3.65, 3.39, 3.42},
               {4.18, 5.67, 6.97, 4.51, 5.44, 3.87, 3.81, 3.58, 3.63, 3.51, 3.79, 3.52, 3.55} ,
               {4.47, 6.97, 5.90, 4.88, 6.01, 3.72, 3.67, 3.84, 3.66, 3.61, 3.88, 3.62, 3.66} ,
               {5.45, 4.51, 4.88, 5.65, 6.54, 3.87, 3.74, 3.84, 3.69, 3.62, 3.89, 3.63, 3.67} ,
               {5.48, 5.44, 6.01, 6.54, 8.95, 6.65, 6.04, 5.49, 5.47, 6.68, 7.04, 6.71, 6.70} ,
               {3.80, 3.87, 3.72, 3.87, 6.65, 5.74, 4.94, 4.70, 5.11, 3.15, 3.40, 3.08, 3.13} ,
               {3.73, 3.81, 3.67, 3.74, 6.04, 4.94, 5.98, 6.58, 4.06, 3.10, 3.43, 3.17, 3.21} ,
               {3.70, 3.58, 3.84, 3.84, 5.49, 4.70, 6.58, 5.42, 4.74, 3.86, 3.39, 3.16, 3.20} ,
               {3.33, 3.63, 3.66, 3.69, 5.47, 5.11, 4.06, 4.74, 5.09, 4.27, 3.98, 2.97, 3.05} ,
               {3.37, 3.51, 3.61, 3.62, 6.68, 3.15, 3.10, 3.86, 4.27, 4.73, 4.64, 3.36, 2.86} ,
               {3.65, 3.79, 3.88, 3.89, 7.04, 3.40, 3.43, 3.39, 3.98, 4.64, 5.37, 4.90, 4.07} ,
               {3.39, 3.52, 3.62, 3.63, 6.71, 3.08, 3.17, 3.16, 2.97, 3.36, 4.90, 4.66, 3.50} ,
               {3.42, 3.55, 3.66, 3.67, 6.70, 3.13, 3.21, 3.20, 3.05, 2.86, 4.07, 3.50, 4.62} };


        public static double[,] dropTableToMyCrib = new double[,]{
            	{5.38,	4.23,	4.52,	5.43,	5.45,	3.85,	3.85,	3.80,	3.40,	3.42,	3.65,	3.42,	3.41},
            	{4.23,	5.72,	7.00,	4.52,	5.45,	3.93,	3.81,	3.66,	3.71,	3.55,	3.84,	3.58,	3.52},
            	{4.52,	7.00,	5.94,	4.91,	5.97,	3.81,	3.58,	3.92,	3.78,	3.57,	3.90,	3.59,	3.67},
            	{5.43,	4.52,	4.91,	5.63,	6.48,	3.85,	3.72,	3.83,	3.72,	3.59,	3.88,	3.59,	3.60},
            	{5.45,	5.45,	5.97,	6.48,	8.79,	6.63,	6.01,	5.48,	5.43,	6.66,	7.00,	6.63,	6.66},
            	{3.85,	3.93,	3.81,	3.85,	6.63,	5.76,	4.98,	4.63,	5.13,	3.17,	3.41,	3.23,	3.13},
            	{3.85,	3.81,	3.58,	3.72,	6.01,	4.98,	5.92,	6.53,	4.04,	3.23,	3.53,	3.23,	3.26},
            	{3.80,	3.66,	3.92,	3.83,	5.48,	4.63,	6.53,	5.45,	4.72,	3.80,	3.52,	3.19,	3.16},
            	{3.40,	3.71,	3.78,	3.72,	5.43,	5.13,	4.04,	4.72,	5.16,	4.29,	3.97,	2.99,	3.06},
            	{3.42,	3.55,	3.57,	3.59,	6.66,	3.17,	3.23,	3.80,	4.29,	4.76,	4.61,	3.31,	2.84},
            	{3.65,	3.84,	3.90,	3.88,	7.00,	3.41,	3.53,	3.52,	3.97,	4.61,	5.33,	4.81,	3.96},
            	{3.42,	3.58,	3.59,	3.59,	6.63,	3.23,	3.23,	3.19,	2.99,	3.31,	4.81,	4.79,	3.46},
                {3.41,	3.52,	3.67,	3.60,	6.66,	3.13,	3.26,	3.16,	3.06,	2.84,	3.96,	3.46,	4.58}};

        public static double[,] dropTableToYouCrib = new double[,]{            
                {6.02,	5.07,	5.07,	5.72,	6.01,	4.91,	4.89,	4.85,	4.55,	4.48,	4.68,	4.33,	4.30},
                {5.07,	6.38,	7.33,	5.33,	6.11,	4.97,	4.97,	4.94,	4.70,	4.59,	4.81,	4.56,	4.45},
                {5.07,	7.33,	6.68,	5.96,	6.78,	4.87,	5.01,	5.05,	4.87,	4.63,	4.86,	4.59,	4.48},
                {5.72,	5.33,	5.96,	6.53,	7.26,	5.34,	4.88,	4.94,	4.68,	4.53,	4.85,	4.46,	4.36},
                {6.01,	6.11,	6.78,	7.26,	9.37,	7.47,	7.00,	6.30,	6.15,	7.41,	7.76,	7.34,	7.25},
                {4.91,	4.97,	4.87,	5.34,	7.47,	7.08,	6.42,	5.86,	6.26,	4.31,	4.57,	4.22,	4.14},
                {4.89,	4.97,	5.01,	4.88,	7.00,	6.42,	7.14,	7.63,	5.26,	4.31,	4.68,	4.32,	4.27},
                {4.85,	4.94,	5.05,	4.94,	6.30,	5.86,	7.63,	6.82,	5.83,	5.10,	4.59,	4.31,	4.20},
                {4.55,	4.70,	4.87,	4.68,	6.15,	6.26,	5.26,	5.83,	6.39,	5.43,	4.96,	4.11,	4.03},
                {4.48,	4.59,	4.63,	4.53,	7.41,	4.31,	4.31,	5.10,	5.43,	6.08,	5.63,	4.61,	3.88},
                {4.68,	4.81,	4.86,	4.85,	7.76,	4.57,	4.68,	4.59,	4.96,	5.63,	6.42,	5.46,	4.77},
                {4.33,	4.56,	4.59,	4.46,	7.34,	4.22,	4.32,	4.31,	4.11,	4.61,	5.46,	5.79,	4.49},
                {4.30,	4.45,	4.48,	4.36,	7.25,	4.14,	4.27,	4.20,	4.03,	3.88,	4.77,	4.49,	5.65}};



        //                                 A      2      3    4      5     6     7    8     9     10    J    Q     K
        //double[] ace = new double[]     {5.26, 4.18, 4.47, 5.45, 5.48, 3.80, 3.73, 3.70, 3.33, 3.37, 3.65, 3.39, 3.42};
        //double[] two = new double[]     {4.18, 5.67, 6.97, 4.51, 5.44, 3.87, 3.81, 3.58, 3.63, 3.51, 3.79, 3.52, 3.55}; 
        //double[] three = new double[]   {4.47, 6.97, 5.90, 4.88, 6.01, 3.72, 3.67, 3.84, 3.66, 3.61, 3.88, 3.62, 3.66}; 
        //double[] four = new double[]    {5.45, 4.51, 4.88, 5.65, 6.54, 3.87, 3.74, 3.84, 3.69, 3.62, 3.89, 3.63, 3.67};
        //double[] five = new double[]    {5.48, 5.44, 6.01, 6.54, 8.95, 6.65, 6.04, 5.49, 5.47, 6.68, 7.04, 6.71, 6.70};
        //double[] six = new double[]     {3.80, 3.87, 3.72, 3.87, 6.65, 5.74, 4.94, 4.70, 5.11, 3.15, 3.40, 3.08, 3.13};
        //double[] seven = new double[]   {3.73, 3.81, 3.67, 3.74, 6.04, 4.94, 5.98, 6.58, 4.06, 3.10, 3.43, 3.17, 3.21};
        //double[] eight = new double[]   {3.70, 3.58, 3.84, 3.84, 5.49, 4.70, 6.58, 5.42, 4.74, 3.86, 3.39, 3.16, 3.20};
        //double[] nine = new double[]    {3.33, 3.63, 3.66, 3.69, 5.47, 5.11, 4.06, 4.74, 5.09, 4.27, 3.98, 2.97, 3.05};
        //double[] ten = new double[]     {3.37, 3.51, 3.61, 3.62, 6.68, 3.15, 3.10, 3.86, 4.27, 4.73, 4.64, 3.36, 2.86};
        //double[] jack = new double[]    {3.65, 3.79, 3.88, 3.89, 7.04, 3.40, 3.43, 3.39, 3.98, 4.64, 5.37, 4.90, 4.07};
        //double[] queen = new double[]   {3.39, 3.52, 3.62, 3.63, 6.71, 3.08, 3.17, 3.16, 2.97, 3.36, 4.90, 4.66, 3.50}; 
        //double[] king = new double[]    {3.42, 3.55, 3.66, 3.67, 6.70, 3.13, 3.21, 3.20, 3.05, 2.86, 4.07, 3.50, 4.62};



    }


}