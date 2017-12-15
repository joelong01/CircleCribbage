using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cribbage;
using NPack;


namespace CribbageService
{


    public class Deck
    {
        static public int DECK_SIZE = 52;
        static public int HAND_SIZE = 6;      
        private static Random _random = null;         
        private List<CardView> _hand1 = new List<CardView>();
        private List<CardView> _hand2 = new List<CardView>();
        private CardView _sharedCard = new CardView();
        int[] _randomIndeces = new int[52];
     

        int _seed = 0;

        VectorCards _vectorCards = null;

        public Deck()
        {
            _vectorCards = new VectorCards();
            _seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            _random = new Random(_seed);
            Randomize();                        
            _vectorCards.Init();
            

        }

      


        internal void Reset()
        {
            _hand1.Clear();
            _hand2.Clear();
            _sharedCard = null;

            _vectorCards.ResetCards();
        }

        public List<CardView> Cards
        {

            get
            {
                List<CardView> cards = new List<CardView>();
                for (int i = 0; i < 52; i++)
                {
                    cards.Add(_vectorCards.GetCardByIndex(i));
                }
                return cards;

            }


        }

        public List<CardView> RandomCards
        {
            get
            {
                List<CardView> cards = new List<CardView>();
                for (int i = 0; i < 52; i++)
                {
                    cards.Add(_vectorCards.GetCardByIndex(_randomIndeces[i]));
                }
                return cards;
            }

        }


        public void DumpCards(List<CardView> cards)
        {
           

                int count = 1;
                foreach (CardView c in cards)
                {
                    MainPage.LogTrace.TraceMessageAsync(String.Format("Count: " + (count++).ToString() + " Name: " + c.Name + " Value: " + c.Value + " index: " + c.Index));

                }
            
          
        }

        public void GetHands()
        {
 
            _hand1.Clear();
            _hand2.Clear();


            for (int i = 0; i < 12; i+=2)
            {                
                _hand1.Add(_vectorCards.GetCardByIndex(_randomIndeces[i]));
                _hand2.Add(_vectorCards.GetCardByIndex(_randomIndeces[i+1]));
                
            }


            _sharedCard = _vectorCards.GetCardByIndex(_randomIndeces[12]);

                

           

        }

        public List<CardView> FirstHand
        {
            get
            {
                return _hand1;
               
            }
            set
            {
                _hand1 = value;
            }

        }

        public List<CardView> SecondHand
        {
            get
            {
               return _hand2;
                
            }
            set
            {
                _hand2 = value;
            }

        }

        
        public void Shuffle()
        {
            
            MainPage.LogTrace.TraceMessageAsync(String.Format("Shuffling Deck.  random seed:{0}", _seed));

            _vectorCards.ResetCards();
            Randomize();         
            GetHands();

        }

        public void Randomize()
        {

            MersenneTwister twist = new MersenneTwister();


            for (int i = 0; i < 52; i++)
            {
                _randomIndeces[i] = i;
             
            }

            int temp = 0;
            for (int n = 0; n < 52; n++)
            {
              //  int k = _random.Next(n + 1);
                int k = twist.Next(n + 1);
                temp = _randomIndeces[n];
                _randomIndeces[n] = _randomIndeces[k];
                _randomIndeces[k] = temp;
            }
        }

       

        public CardView SharedCard
        {
            get
            {
                return _sharedCard;
            }
            set
            {
                _sharedCard = value;
            }

        }




        internal CardView Card(CardNames cardName)
        {
           foreach (CardView card in _vectorCards.Cards)
           {
               if (card.Data.Name == cardName)
                   return card;
           }

           throw new Exception("card not found");
        }
    }


}