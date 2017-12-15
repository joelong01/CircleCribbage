using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cribbage;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace CribbageService
{

    public class Player
    {
        private string _id;
        int _score = 0;        
        bool _hasCrib = false; // this should make it so playThisTurn starts    
        PlayerType _type;
        Hand _hand;
        Crib _crib = new Crib();
        string _name = "";

        internal int Score
        {
            get
            {
                return _score;
            }
            set
            {
                _score = value;
                if (value != 0)
                    MainPage.LogTrace.TraceMessageAsync(String.Format("{0} Score:{1}", _type, _score));
            }
        }



        public string Serialize()
        {
            if (_hand == null)
                return "";
            Dictionary<string, string> dict = new Dictionary<string, string>();            
            dict.Add("Hand", StaticHelpers.SerializeFromList(_hand.Cards));
            if (this.HasCrib)
                dict.Add("Crib", StaticHelpers.SerializeFromList(_crib.Cards));
            else
                dict.Add("Crib", "");
            dict.Add("Type", _type.ToString());
            dict.Add("HasCrib", _hasCrib.ToString());
            dict.Add("Score", _score.ToString());
            return StaticHelpers.SerializeDictionary(dict) ;


        }

        public bool Deserialize(string s, Deck deck)
        {
            Dictionary<string, string> dict = StaticHelpers.DeserializeDictionary(s);
            if (dict == null)
                return false;
            string sHand = dict["Hand"];
            _type = (PlayerType)Enum.Parse(typeof(PlayerType), dict["Type"], true);

            List<CardView> cards = StaticHelpers.DeserializeToList(sHand, deck); ;
            _hand = new Hand(cards);
            _hasCrib = Convert.ToBoolean(dict["HasCrib"]);
            if (_hasCrib)
            {
                _crib = new Crib();
                _crib.Cards = StaticHelpers.DeserializeToList(dict["Crib"], deck);
            }

            
            
            
            _score = Convert.ToInt32(dict["Score"]);
            
            return true;
        }

        public Player(string name)
        {
            _name = name;
            _id = this.GetHashCode().ToString();
        }
        public string Name { get { return _name; } }

        public Player(string id, List<CardView> cards)
        {
            _id = id;
            _hand = new Hand(cards);
        }

        public Crib AddToCrib(List<CardView> list)
        {
            if (_crib.Cards.Count >= 4)
                _crib.Cards.Clear();

            _crib.Cards.AddRange(list);
            return _crib;

        }

        public Crib Crib
        {
            get
            {
                return _crib;
            }
            set
            {
                _crib = value;
            }

        }

        public PlayerType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }

        }

       

        public bool HasCrib
        {
            get
            {
                return _hasCrib;
            }
            set
            {
                _hasCrib = value;
            }
        }

        public string ID
        {
            get
            {
                return _id;
            }
        }

        

        public Hand Hand
        {
            get
            {
                return _hand;
            }
            set
            {
                _hand = value;
            }
        }


    }



}
