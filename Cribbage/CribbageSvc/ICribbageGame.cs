using Cribbage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;


namespace CribbageService
{

    public interface ICribbageGame
    {

        /// <summary>
        /// get a match against the computer
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        MatchIds PlayComputer(string name);

        /// <summary>
        /// shuffle the deck and return the cards
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <returns></returns>
        List<CardData> Shuffle(string matchid, string playerid);


        /// <summary>
        /// shuffle the deck and return both hands and the shared card
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <returns></returns>
        List<CardData> ShuffleAndReturnAllCards(string matchid);

        /// <summary>
        /// used because you want to get the same hand (to show player's two cards at the right time)
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <returns></returns>
        List<CardData> GetHand(string matchid, string playerid);

        /// <summary>
        /// send 2 cards to the crib -- computer does this after Shuffle
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <param name="card1"></param>
        /// <param name="card2"></param>
        /// <returns></returns>
        long SendToCrib(string matchid, string playerid, long card1, long card2);


        /// <summary>
        ///  sends 4 cards from the client to the servicer
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="cards"></param>
        /// <returns></returns>
        void SendAllCardsToCrib(string matchid, List<CardData> cards);

        /// <summary>
        /// after sending cards to crib, look at the shared card
        /// </summary>
        /// <param name="matchid"></param>
        /// <returns></returns>
        CardData GetSharedCard(string matchid);

        /// <summary>
        ///  called during the counting phase
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <param name="cardIndex"></param>
        /// <returns></returns>
        List<CountingData> CountCard(string matchid, string playerid, long cardIndex);


        /// <summary>
        /// after counting, user counts their cards and sends the answer 
        /// sequence number is so the client can retry
        /// 
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <param name="sequencenumber"></param>
        /// <param name="scoredelta"></param>
        /// <returns></returns>
        ScoreCollection UpdateScoreForHand(string matchid, string playerid, long sequencenumber, long scoredelta);


        /// <summary>
        /// scores the crib for the particular player.  returns the story so that it can be displayed.
        /// if the nScore sent in doesn't match what the computer thinks it is, it won't accept the nScore
        /// 
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <param name="sequencenumber"></param>
        /// <param name="scoredelta"></param>
        /// <returns></returns>
        ScoreCollection UpdateScoreForCrib(string matchid, string playerid, long sequencenumber, long scoredelta);

        /// <summary>
        ///  Get a suggested card for the player.  Used during counting.  Computer always plays the suggested CardData
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <returns></returns>
        List<CardData> GetSuggestedCard(string matchid, string playerid);

        /// <summary>
        ///  Get a suggested crib for the player. Computer always plays the suggested crib
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <returns></returns>
        List<CardData> GetSuggestedCrib(string matchid, string playerid);


        /// <summary>
        ///  Called after the hand is counted via UpdateScore to see what the crib is
        /// </summary>
        /// <param name="matchid"></param>
        /// <param name="playerid"></param>
        /// <returns></returns>
        List<CardData> GetCrib(string matchid, string playerid);


        /// <summary>
        /// new game between the same players.  resets nScore, etc.
        /// </summary>
        void NewGame();


        int GetScore(string matchid, string playerid);


        string WhosTurn(string matchid);


        MatchIds GetNewMatchIds();


        string RegisterUser(string user);


    }

    public enum PlayerType { Player = 0, Computer = 1 };
    public enum HandType { Crib = 0, Regular = 1 };

    public enum Open { Up, Left, Right, Down };


    [DataContract]
    public class MatchIds
    {
        [DataMember]
        public string PlayerId = "";

        [DataMember]
        public string MatchId = "";

        [DataMember]
        public string ComputerId = "";

    }

    public class HandsFromServer
    {
        public List<CardView> PlayerCards { get; set; }
        public List<CardView> ComputerCards { get; set; }

        public CardView SharedCard { get; set; }
    }

    [DataContract]
    public class HandWithScore
    {
        [DataMember]
        public int Score { get; set; }
        [DataMember]
        public List<CardData> Cards { get; set; }
        [DataMember]
        public CardData SharedCard { get; set; }
        [DataMember]
        public List<CardData> Crib { get; set; }

    }

    public enum MuggensType { None, ThreeCardRunTo0, FourCardTo3Card, Forgot15, ForgotJackSameSuit, Add15, ForgotFlush };

    public enum ScoreType { Count, Hand, Crib, Cut, Saved, Unspecified }
    public class ScoreInstance
    {

        public string Description
        {
            get
            {
                string resourceKey = "Score" + ScoreType.ToString();
                return (string)Application.Current.Resources[resourceKey];

            }

        }
        public int Count { get; set; }
        public int Score { get; set; }
        public StatName ScoreType { get; set; }

        //
        // for Muggins support
        public bool Muggins { get; set; }
        public int ActualScore { get; set; }
        public StatName ActualScoreType { get; set; }
        public MuggensType MuggensType { get; set; }

        public List<int> Cards { get; set; }

        public ScoreInstance(string s)
        {
            Cards = new List<int>();
            Load(s);
        }

        public ScoreInstance(StatName name, int count, int score, List<int> cards)
        {
            ScoreType = name;
            Count = count;
            Score = score;
            Cards = new List<int>(cards);
            Muggins = false;
        }

        public ScoreInstance(StatName name, int count, int score, int cardIndex)
        {
            ScoreType = name;
            Count = count;
            Score = score;
            Cards = new List<int>();
            Cards.Add(cardIndex);
            Muggins = false;
        }

        //  put all state on one line
        public string Save()
        {
            string s = String.Format("{0},{1},{2},{3},{4},{5},{6},", Count, Score, ScoreType, Muggins, ActualScore, ActualScoreType, MuggensType);
            foreach (int i in Cards)
            {
                s += String.Format("{0},", i);
            }

            return s;
        }

        public bool Load(string s)
        {
            char[] sep1 = new char[] { ',' };

            string[] tokens = s.Split(sep1, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Count() < 7) return false;

            Count = Convert.ToInt32(tokens[0]);
            Score = Convert.ToInt32(tokens[1]);
            ScoreType = (StatName)Enum.Parse(typeof(StatName), tokens[2]);
            Muggins = Convert.ToBoolean(tokens[3]);
            ActualScore = Convert.ToInt32(tokens[4]);
            ActualScoreType = (StatName)Enum.Parse(typeof(StatName), tokens[5]);
            MuggensType = (MuggensType)Enum.Parse(typeof(MuggensType), tokens[6]);

            for (int i = 7; i < tokens.Count(); i++)
            {
                Cards.Add(Convert.ToInt32(tokens[i]));
            }

            return true;
        }

    }

    //
    //  should this have inherited from ObservableCollection?
    public class ScoreCollection
    {

        public ObservableCollection<ScoreInstance> Scores { get; set; }

        public ScoreType ScoreType { get; set; }

        public int Total { get; set; }
        public bool Accepted { get; set; }

        public int ActualScore { get; set; } // Total might be wrong because of Muggins

        public ScoreCollection()
        {
            Accepted = false;
            Scores = new ObservableCollection<ScoreInstance>();
            Total = 0;
            ScoreType = ScoreType.Unspecified;
            ActualScore = 0;
        }

        public ScoreCollection(string s)
        {
            Scores = new ObservableCollection<ScoreInstance>();
            Load(s);
        }
        //
        //  one line. everything afer the "=" sign.
        public string Save()
        {
            string s = String.Format("{0}-{1}-{2}-{3}|", ScoreType, Total, Accepted, ActualScore);
            foreach (ScoreInstance scoreInstance in Scores)
            {
                s += scoreInstance.Save() + "|";
            }
            return s;
        }

        private bool Load(string s)
        {
            char[] sep1 = new char[] { '|' };
            char[] sep2 = new char[] { '-' };

            string[] tokens = s.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
            string[] tokens2 = tokens[0].Split(sep2, StringSplitOptions.RemoveEmptyEntries);

            ScoreType = (ScoreType)Enum.Parse(typeof(ScoreType), tokens2[0]);
            Total = Convert.ToInt32(tokens2[1]);
            Accepted = Convert.ToBoolean(tokens2[2]);
            ActualScore = Convert.ToInt32(tokens2[3]);

            for (int i = 1; i < tokens.Count(); i++)
            {
                ScoreInstance scoreInstance = new ScoreInstance(tokens[i]);
                Scores.Add(scoreInstance);
            }
            return true;
        }

        public string LogString()
        {
            string s = "";

            foreach (ScoreInstance p in this.Scores)
            {
                s += String.Format("|{0}, {1}, {2}|-", p.Description, p.Count, p.Score);
            }

            s += String.Format("Total: {0}", Total);

            return s;

        }

        public string Format(bool includeHeader = true, bool formatForMessagebox = true, bool smallFormat = false)
        {
            string story = "";
            string tabs = "\t\t";
            string tab = "\t";

            string line = "";
            int len;

            if (smallFormat)
            {
                foreach (ScoreInstance p in this.Scores)
                {

                    line = String.Format("{0}{1}{2}\n", p.Description, tabs, p.Score);
                    story += line;
                }
                story += String.Format("\nTotal:\t{0}", Total);
                return story;
            }

            if (includeHeader)
            {
                if (formatForMessagebox)
                    story = String.Format("{0}\t\t{1}\t\t{2}\n", "Type", "Count", "Score");
                else
                    story = String.Format("{0}\t\t{1}\t{2}\n", "Type", "     Count", "      Score");
            }


            foreach (ScoreInstance p in this.Scores)
            {
                len = p.Description.Length;
                if ((len > 5 && !formatForMessagebox) || p.Description == "Saved")
                {
                    line = String.Format("{0}{1}{2}{3}{4}\n", p.Description, tabs, p.Count, tabs, p.Score);
                }
                else
                {
                    if (formatForMessagebox)
                    {
                        line = String.Format("{0}{1}{2}{3}{4}\n", p.Description, tabs, p.Count, tabs, p.Score);

                    }
                    else
                    {
                        line = String.Format("{0}{1}{2}{3}{4}\n", p.Description, tabs + tab, p.Count, tabs, p.Score);

                    }

                }
                story += line;
            }
            story += String.Format("\n\t\t\tTotal:\t{0}", Total);
            return story;

        }


        internal ScoreInstance GetScoreType(StatName statName)
        {
            foreach (ScoreInstance s in this.Scores)
            {
                if (s.ScoreType == statName)
                {
                    return s;
                }
            }

            return null;

        }
    }



    [DataContract]
    public class CountingData
    {
        [DataMember(Order = 0)]
        public int CardId { get; set; }

        [DataMember(Order = 1)]
        public string CardName { get; set; }

        [DataMember(Order = 2)]
        public int CurrentCount { get; set; }

        [DataMember(Order = 3)]
        public int Score { get; set; }

        [DataMember(Order = 4)]
        public bool isGo { get; set; }

        [DataMember(Order = 5)]
        public bool ThisPlayerCanGo { get; set; }

        [DataMember(Order = 6)]
        public bool NextPlayerCanGo { get; set; }

        [DataMember(Order = 7)]
        public string NextPlayerId { get; set; }

        [DataMember(Order = 8)]
        public bool ResetCount { get; set; }

        [DataMember(Order = 9)]
        public PlayerType NextPlayer { get; set; }

        [DataMember(Order = 10)]
        public int CardsCounted { get; set; }

        [DataMember(Order = 12)]
        public bool NextPlayerIsComputer { get; set; }

        [DataMember(Order = 12)]
        public ScoreCollection ScoreStory { get; set; }

        [DataMember(Order = 13)]
        public int CountBeforeReset { get; set; }

        public int ActualScore { get; set; }

    }

    public class CribbageGameDifficulty
    {
        public GameDifficulty GameDifficulty { get; set; }
        
    }

}
