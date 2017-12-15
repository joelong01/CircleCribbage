using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;


namespace CribbageService
{

    public enum Suit { Clubs = 0, Diamonds = 1, Hearts = 2, Spades = 3 };
    public enum CardNames
    {
        AceOfClubs = 0, TwoOfClubs = 1, ThreeOfClubs = 2, FourOfClubs = 3, FiveOfClubs = 4, SixOfClubs = 5, SevenOfClubs = 6, EightOfClubs = 7, NineOfClubs = 8, TenOfClubs = 9, JackOfClubs = 10, QueenOfClubs = 11, KingOfClubs = 12,
        AceOfDiamonds = 13, TwoOfDiamonds = 14, ThreeOfDiamonds = 15, FourOfDiamonds = 16, FiveOfDiamonds = 17, SixOfDiamonds = 18, SevenOfDiamonds = 19, EightOfDiamonds = 20, NineOfDiamonds = 21, TenOfDiamonds = 22, JackOfDiamonds = 23, QueenOfDiamonds = 24, KingOfDiamonds = 25,
        AceOfHearts = 26, TwoOfHearts = 27, ThreeOfHearts = 28, FourOfHearts = 29, FiveOfHearts = 30, SixOfHearts = 31, SevenOfHearts = 32, EightOfHearts = 33, NineOfHearts = 34, TenOfHearts = 35, JackOfHearts = 36, QueenOfHearts = 37, KingOfHearts = 38,
        AceOfSpades = 39, TwoOfSpades = 40, ThreeOfSpades = 41, FourOfSpades = 42, FiveOfSpades = 43, SixOfSpades = 44, SevenOfSpades = 45, EightOfSpades = 46, NineOfSpades = 47, TenOfSpades = 48, JackOfSpades = 49, QueenOfSpades = 50, KingOfSpades = 51,
        BlackJoker = 52, RedJoker = 53, BackOfCard = 54
    };

    public enum Owner
    {
        Player = 1,
        Computer = 2,
        Shared = 3,
        Crib = 4
    };


    public class CardData
    {
        #region PRIVATE DATA
        private int _value = 0;         // the value for counting 
        private int _index = 0;         // the index into the array of 52 cars (0...51)
        private Suit _suit;             // enum of the suit name 
        private int _rank = 0;       // the number of the card in the suit -- e.g. 1...13 for A...K - used for counting runs and sorting
        private Owner _owner = Owner.Shared; // who owns the card -- drives the faceup/facedown decision
        private CardNames _cardName;



        #endregion

        #region CONSTRUCTORS

        public CardData() { }

        public override string ToString()
        {

            return String.Format("Name:{0}\t\t Index:{1}\t Value:{2}\t Rank:{3}\t Suit:{4}\t Owner:{5}", _cardName, _index, _value, _rank, _suit, _owner);
        }


        public CardData(CardNames name, Owner owner, int rank, int value, int index, Suit suit)
        {
            _cardName = name;
            _value = value;
            _index = index;
            _suit = suit;
            _rank = rank;
            _owner = owner;

        }

        public CardData(CardData c)
        {
            _value = c.Value;
            _index = c.Index;
            _suit = c.Suit;
            _rank = c.Rank;
            _owner = c.Owner;

        }
        #endregion

        #region PROPERTIES
        /// <summary>
        /// value for countinge (1..10)
        /// </summary>

        public int Value
        {
            get
            {
                return _value;
            }
            set
            {

                _value = value;
            }
        }
        /// <summary>
        /// used where runs and order matters (1...13)
        /// </summary>

        public int Rank
        {
            get
            {
                return _rank;
            }
            set
            {

                _rank = value;
            }
        }
        /// <summary>
        /// the index into the unordered card deck
        /// </summary>

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {

                _index = value;
            }
        }
        /// <summary>
        /// enum of the suits
        /// </summary>

        public Suit Suit
        {
            get
            {
                return _suit;
            }
            set
            {

                _suit = value;
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
            }

        }


        public CardNames Name
        {
            get { return _cardName; }
            set { _cardName = value; }

        }
        #endregion


        #region METHODS

        public static int CompareCardsByRank(CardData x, CardData y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're 
                    // equal.  
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y 
                    // is greater.  
                    return -1;
                }
            }
            else
            {
                // If x is not null... 
                // 
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the  
                    // lengths of the two strings. 
                    // 
                    return x.Rank - y.Rank;


                }
            }


        }

        #endregion

   

    }
}