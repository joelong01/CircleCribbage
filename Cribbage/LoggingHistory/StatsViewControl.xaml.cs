using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using CribbageService;
using Windows.UI.Popups;
using Windows.Storage;
using System.Threading.Tasks;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{



    public enum StatType { Count, Min, Max, Average, Total };
    public enum StatViewType { Game, Hand, Crib, Counting };

    public enum StatName
    {
        Ignored, Saved,
        /* Game stats -- these are set in the client state machine */
        WonDeal, GamesStarted, GamesWon, GamesLost, TotalHandsPlayed, TotalCribsPlayed, TotalCountingSessions, SmallestWinMargin, LargestWinMargin, SkunkWins, CutAJack,
        /* Hand stats -- these are set in the Hint Window*/
        HandMostPoints, HandTotalPoints, HandAveragePoints, Hand0Points, HandJackOfTheSameSuit, HandPairs, Hand3OfAKind, Hand4OfAKind, Hand4CardFlush, Hand5CardFlush, Hand3CardRun, Hand4CardRun, Hand5CardRun, Hand15s,
        /* Crib stats -- these are set in the Hint Window*/
        CribMostPoints, CribTotalPoints, CribAveragePoints, Crib0Points, CribJackOfTheSameSuit, CribPairs, Crib3OfAKind, Crib4OfAKind, Crib5CardFlush, Crib3CardRun, Crib4CardRun, Crib5CardRun, Crib15s,
        /* Counting stats -- these are set in the Hint Window*/
        CountingMostPoints, CountingTotalPoints, CountingAveragePoints, CountingPair, Counting3OfAKind, Counting4OfAKind, Counting3CardRun, Counting4CardRun, 
        Counting5CardRun, Counting6CardRun, Counting7CardRun, CountingLastCard, CountingHit31, CountingGo, CountingHit15, Counting0Points
    };

    public class Stat : INotifyPropertyChanged
    {

        int _playerAllCount = 0;
        int _computerAllCount = 0;
        int _playerGameCount = 0;
        int _computerGameCount = 0;
        StatName _averageDivisor;
        StatName _statTotal;



        public event PropertyChangedEventHandler PropertyChanged;

        public Stat(StatName name, int p, int c)
        {
            StatName = name;
            _playerGameCount = p;
            _computerGameCount = c;
            switch (name)
            {
                case StatName.HandMostPoints:
                case StatName.CribMostPoints:
                case StatName.LargestWinMargin:
                case StatName.CountingMostPoints:
                case StatName.Crib15s:
                case StatName.Hand15s:                
                    Type = StatType.Max;
                    break;
                case StatName.SmallestWinMargin:
                    Type = StatType.Min;
                    break;
                case StatName.HandAveragePoints:
                    _averageDivisor = StatName.TotalHandsPlayed;
                    _statTotal = StatName.HandTotalPoints;
                    Type = StatType.Average;
                    break;
                case StatName.CountingAveragePoints:
                    _averageDivisor = StatName.TotalCountingSessions;
                    _statTotal = StatName.CountingTotalPoints;
                    Type = StatType.Average;
                    break;
                case StatName.CribAveragePoints:
                    _averageDivisor = StatName.TotalCribsPlayed;
                    _statTotal = StatName.CribTotalPoints;
                    Type = StatType.Average;
                    break;                
                case StatName.CountingTotalPoints:
                case StatName.HandTotalPoints:
                case StatName.CribTotalPoints:
                    Type = StatType.Total;
                    break;
                default:
                    Type = StatType.Count;
                    break;


            }
        }

        public void Init(int player, int computer)
        {
            _playerAllCount = player;
            _computerAllCount = computer;

        }

        public StatType Type { get; set; }
        public StatName StatName { get; set; }
        public string Description
        {
            get
            {
                string s = StatName.ToString();
                s = (string)Application.Current.Resources[s];
                return s;
            }

        }

        //
        //  if type==Counting, newVal gets added
        //  if type== max||min, newVal gets set
        //
        public void UpdateStatistic(PlayerType playerType, int newVal)
        {
            if (playerType == PlayerType.Player)
            {


                switch (this.Type)
                {
                    case StatType.Count:
                        _playerGameCount++;
                        _playerAllCount++;
                        OnPropertyChanged("PlayerGameCount");
                        OnPropertyChanged("PlayerAllCount");
                        break;
                    case StatType.Total:
                        _playerGameCount += newVal;
                        _playerAllCount += newVal;
                        OnPropertyChanged("PlayerGameCount");
                        OnPropertyChanged("PlayerAllCount");
                        break;
                    case StatType.Max:
                        if (newVal > _playerGameCount)
                        {
                            _playerGameCount = newVal;
                            OnPropertyChanged("PlayerGameCount");
                        }
                        if (newVal > _playerAllCount)
                        {
                            _playerAllCount = newVal;
                            OnPropertyChanged("PlayerAllCount");
                        }
                        break;
                    case StatType.Min:
                        if (newVal < _playerGameCount || _playerGameCount == 0)
                        {
                            _playerGameCount = newVal;
                            OnPropertyChanged("PlayerGameCount");
                        }
                        if (newVal < _playerAllCount || _playerGameCount == 0)
                        {
                            _playerAllCount = newVal;
                            OnPropertyChanged("PlayerAllCount");
                        }
                        break;
                    case StatType.Average:
                        OnPropertyChanged("PlayerGameCount");
                        OnPropertyChanged("PlayerAllCount");
                        break;
                    default:
                        throw new Exception("bad StatType in StatViewControl.Xaml.cs.  error 9380093)");
                }
            }
            else
            {

                switch (this.Type)
                {
                    case StatType.Count:
                        _computerGameCount++;
                        _computerAllCount++;
                        OnPropertyChanged("ComputerGameCount");
                        OnPropertyChanged("ComputerAllCount");
                        break;
                    case StatType.Total:
                        _computerGameCount += newVal;
                        _computerAllCount += newVal;
                       OnPropertyChanged("ComputerGameCount");
                        OnPropertyChanged("ComputerAllCount");
                        break;
                    case StatType.Max:
                        if (newVal > _computerGameCount)
                        {
                            _computerGameCount = newVal;
                            OnPropertyChanged("ComputerGameCount");
                        }
                        if (newVal > _computerAllCount)
                        {
                            _computerAllCount = newVal;
                            OnPropertyChanged("ComputerAllCount");
                        }
                        break;
                    case StatType.Min:
                        if (newVal < _computerGameCount || _computerGameCount == 0)
                        {
                            _computerGameCount = newVal;
                            OnPropertyChanged("ComputerGameCount");
                        }
                        if (newVal < _computerAllCount || _computerAllCount == 0)
                        {
                            _computerAllCount = newVal;
                            OnPropertyChanged("ComputerAllCount");
                        }
                        break;
                    case StatType.Average:
                        OnPropertyChanged("ComputerGameCount");
                        OnPropertyChanged("ComputerAllCount");
                        break;
                    default:
                        throw new Exception("bad StatType in StatViewControl.Xaml.cs.  error 17)");
                }

            }

        }


        public string PlayerGameCount
        {
            get
            {
                if (Type == StatType.Average)
                {
                    double total = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_statTotal).PlayerGameCount);
                    double count = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_averageDivisor).PlayerGameCount);
                    if (count == 0)
                        return "0";

                    double ave = total / count;
                    return String.Format("{0:0.00}", ave);
                }


                return _playerGameCount.ToString();
            }
        }
        public string PlayerAllCount
        {
            get
            {
                if (Type == StatType.Average)
                {
                    double total = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_statTotal).PlayerAllCount);
                    double count = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_averageDivisor).PlayerAllCount);
                    if (count == 0)
                        return "0";

                    double ave = total / count;
                    return String.Format("{0:0.00}", ave);
                }
                return _playerAllCount.ToString();
            }
        }

        public string ComputerGameCount
        {
            get
            {
                if (Type == StatType.Average)
                {
                    double total = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_statTotal).ComputerGameCount);
                    double count = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_averageDivisor).ComputerGameCount);
                    if (count == 0)
                        return "0";

                    double ave = total / count;
                    return String.Format("{0:0.00}", ave);
                }

                return _computerGameCount.ToString();
            }
        }
        public string ComputerAllCount
        {
            get
            {
                if (Type == StatType.Average)
                {
                    double total = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_statTotal).ComputerAllCount);
                    double count = Convert.ToDouble(CribbageStats.g_CribbageStats.Stat(_averageDivisor).ComputerAllCount);
                    if (count == 0)
                        return "0";

                    double ave = total / count;
                    return String.Format("{0:0.00}", ave);
                }
                return _computerAllCount.ToString();
            }
        }


        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        internal void Reset()
        {
            _computerAllCount = 0;
            _playerAllCount = 0;
            OnPropertyChanged("ComputerGameCount");
            OnPropertyChanged("ComputerAllCount");
        }
    }

    public class CribbageStats
    {
        private ObservableCollection<Stat> _statsAboutGames = null;
        private ObservableCollection<Stat> _statsAboutHands = null;
        private ObservableCollection<Stat> _statsAboutCounting = null;
        private ObservableCollection<Stat> _statsAboutCrib = null;
        private SortedDictionary<StatName, Stat> _dictionary = new SortedDictionary<StatName, Stat>();
        ApplicationDataContainer _storage = ApplicationData.Current.LocalSettings;
        public static CribbageStats g_CribbageStats = null;

       

        public CribbageStats()
        {


            _storage.CreateContainer("eCribbage", ApplicationDataCreateDisposition.Always);
            g_CribbageStats = this;
            _statsAboutGames = new ObservableCollection<Stat>()
#region STAT_INITIALIZE
            {                
                new Stat(StatName.GamesStarted, 0,0),
                new Stat(StatName.WonDeal, 0,0),                
                new Stat(StatName.GamesWon, 0,0),
                new Stat(StatName.GamesLost, 0,0),                
                new Stat(StatName.TotalHandsPlayed, 0,0),                       
                new Stat(StatName.TotalCribsPlayed, 0,0),  
                new Stat(StatName.TotalCountingSessions, 0,0),  
                new Stat(StatName.SmallestWinMargin, 0,0),
                new Stat(StatName.LargestWinMargin, 0,0),
                new Stat(StatName.SkunkWins, 0,0),
            };

            _statsAboutHands = new ObservableCollection<Stat>()
            {
                
                new Stat(StatName.HandTotalPoints, 0,0),  
                new Stat(StatName.HandMostPoints, 0,0),      
                new Stat(StatName.HandAveragePoints, 0,0),      
                new Stat(StatName.Hand15s, 0,0),
                new Stat(StatName.CutAJack, 0,0),
                new Stat(StatName.Hand0Points, 0,0),                                     
                new Stat(StatName.HandJackOfTheSameSuit, 0,0),                     
                new Stat(StatName.HandPairs,   0 ,0),
                new Stat(StatName.Hand3OfAKind, 0,0),
                new Stat(StatName.Hand4OfAKind, 0,0),
                new Stat(StatName.Hand4CardFlush, 0,0),
                new Stat(StatName.Hand5CardFlush, 0,0),
                new Stat(StatName.Hand3CardRun, 0,0),
                new Stat(StatName.Hand4CardRun, 0,0),
                new Stat(StatName.Hand5CardRun, 0,0),
                
            };
            _statsAboutCrib = new ObservableCollection<Stat>()
            {
                new Stat(StatName.CribTotalPoints, 0,0),  
                new Stat(StatName.CribMostPoints, 0,0),              
                new Stat(StatName.CribAveragePoints, 0,0),
                new Stat(StatName.Crib15s, 0,0),
                new Stat(StatName.Crib0Points,  0,0),
                new Stat(StatName.CribJackOfTheSameSuit,  0,0),                     
                new Stat(StatName.CribPairs,  0,0),
                new Stat(StatName.Crib3OfAKind, 0,0),
                new Stat(StatName.Crib4OfAKind, 0,0),
                new Stat(StatName.Crib5CardFlush,  0,0),
                new Stat(StatName.Crib3CardRun, 0,0),
                new Stat(StatName.Crib4CardRun, 0,0),
                new Stat(StatName.Crib5CardRun, 0,0),      
                
            };
            _statsAboutCounting = new ObservableCollection<Stat>()
            {
                new Stat(StatName.CountingTotalPoints, 0,0),
                new Stat(StatName.CountingMostPoints, 0,0),
                new Stat(StatName.CountingAveragePoints, 0,0),
                new Stat(StatName.CountingPair, 0,0),
                new Stat(StatName.Counting3OfAKind, 0,0),                
                new Stat(StatName.Counting4OfAKind, 0,0),
                new Stat(StatName.Counting3CardRun, 0,0),
                new Stat(StatName.Counting4CardRun, 0,0),
                new Stat(StatName.Counting5CardRun, 0,0),
                new Stat(StatName.Counting6CardRun, 0,0),
                new Stat(StatName.Counting7CardRun, 0,0),
                new Stat(StatName.CountingLastCard,  0,0),
                new Stat(StatName.CountingGo, 0,0),
                new Stat(StatName.CountingHit31, 0,0),
                new Stat(StatName.CountingHit15, 0,0),
                new Stat(StatName.Counting0Points, 0,0),
            };
#endregion

            LoadAndAddToDictionary(_statsAboutGames);
            LoadAndAddToDictionary(_statsAboutHands);
            LoadAndAddToDictionary(_statsAboutCrib);
            LoadAndAddToDictionary(_statsAboutCounting);

        }

        private void LoadAndAddToDictionary(ObservableCollection<Stat> statList)
        {
            foreach (Stat stat in statList)
            {
                _dictionary[stat.StatName] = stat;
                LoadStat(stat);
                stat.PropertyChanged += OnStatChanged;

            }
        }

        private void OnStatChanged(object sender, PropertyChangedEventArgs e)
        {
            Stat stat = sender as Stat;
            string container = stat.StatName.ToString();
            ApplicationDataContainer storage = ApplicationData.Current.LocalSettings;
            storage.CreateContainer(container, ApplicationDataCreateDisposition.Always);
            storage.Containers[container].Values["ComputerAll"] = stat.ComputerAllCount.ToString();
            storage.Containers[container].Values["PlayerAll"] = stat.PlayerAllCount.ToString();

        }

        public void LoadStat(Stat stat)
        {
            
            //
            //  average stats aren't stored -- they are calculated
            if (stat.Type == StatType.Average)
                return;

            ApplicationDataContainer storage = ApplicationData.Current.LocalSettings;
            string container = stat.StatName.ToString();
            int computerCount = 0;
            int playerCount = 0;
            try
            {
                computerCount = Convert.ToInt32(storage.Containers[container].Values["ComputerAll"]);
                playerCount = Convert.ToInt32(storage.Containers[container].Values["PlayerAll"]);
            }
            catch (KeyNotFoundException) { } // swollow the exception -- "keyNotFound" when we first start

            stat.Init(playerCount, computerCount);


        }

        public ObservableCollection<Stat> StatsList(StatViewType type)
        {
            switch (type)
            {
                case StatViewType.Counting:
                    return _statsAboutCounting;
                case StatViewType.Crib:
                    return _statsAboutCrib;
                case StatViewType.Game:
                    return _statsAboutGames;
                case StatViewType.Hand:
                    return _statsAboutHands;

            }

            return _statsAboutGames;

        }

        public Stat Stat(StatName name)
        {
            try
            {
                return _dictionary[name];
            }
            catch
            {
                MainPage.LogTrace.TraceMessageAsync("Bad StatName: " + name.ToString());
                return null;
            }
            

        }


        internal void Reset()
        {
            foreach (Stat stat in _dictionary.Values)
            {
                stat.Reset();
            }


        }

        public string SerializeGameStats()
        {
            string ret = "";
            ret += "[HandStats]\n";
            foreach (Stat stat in _statsAboutHands)
            {
                ret += String.Format("{0}={1}|{2}\n", stat.StatName, stat.PlayerGameCount, stat.ComputerGameCount);

            }
            ret += "[CribStats]\n";
            foreach (Stat stat in _statsAboutCrib)
            {
                ret += String.Format("{0}={1}|{2}\n", stat.StatName, stat.PlayerGameCount, stat.ComputerGameCount);

            }
            ret += "[CountingStats]\n";
            foreach (Stat stat in _statsAboutCounting)
            {
                ret += String.Format("{0}={1}|{2}\n", stat.StatName, stat.PlayerGameCount, stat.ComputerGameCount);

            }
            return ret;
        }
        /// <summary>
        ///   TODO: loop over the sections and create a collection that you set in the object
        /// </summary>
        /// <param name="fullGameStats"></param>
        public void DeserializeGameStats(string fullGameStats)
        {


            char[] sep1 = new char[] { '\n' };
            char[] sep2 = new char[] { '=' };
            char[] sep3 = new char[] { '[' };
            _statsAboutHands.Clear();
            _statsAboutCrib.Clear();
            _statsAboutCounting.Clear();
            ObservableCollection<Stat> collection = null;
            string[] sections = fullGameStats.Split(sep3, StringSplitOptions.RemoveEmptyEntries);

            foreach (string section in sections)
            {
                string[] lines = section.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
                foreach (string setting in lines)
                {
                    if (setting.Contains("]"))
                    {
                        if (setting == "HandStats]")
                        {
                            collection = _statsAboutHands;
                        }
                        else if (setting == "CribStats]")
                        {
                            collection = _statsAboutCrib;
                        }
                        else if (setting == "CountingStats]")
                        {
                            collection = _statsAboutCounting;
                        }

                        continue;
                    }
                    string[] kvp = setting.Split(sep2, StringSplitOptions.RemoveEmptyEntries); // kvp[0] = StatName and kvp[1] = PlayerGameCount|ComputerGameCount
                    string[] values = kvp[1].Split('|');
                    int playerGameCount = Convert.ToInt32(values[0]);
                    int computerGameCount = Convert.ToInt32(values[1]);
                    StatName statName = (StatName)Enum.Parse(typeof(StatName), kvp[0]);
                    Stat stat = new Stat(statName, playerGameCount, computerGameCount);
                    collection.Add(stat);
                }
            }
        }
    }



    public delegate void StatsViewEventHandler(object sender, EventArgs e);

    public sealed partial class StatsViewControl : UserControl
    {

        CribbageStats _stats = new CribbageStats();
        TaskCompletionSource<object> _tcs = null;
        private bool _opened = false;

        public event StatsViewEventHandler OnDoneViewingStats;

        public StatsViewControl()
        {
            this.InitializeComponent();
            _listView.ItemsSource = _stats.StatsList(StatViewType.Game);
            _listView.SelectedIndex = 0;
            _listView.Focus(Windows.UI.Xaml.FocusState.Programmatic);

        }


        public CribbageStats Stats
        {
            get
            {
                return _stats;
            }
            set
            {
                _stats = value;
                _listView.ItemsSource = _stats.StatsList(StatViewType.Game);
                _rdoCountsStats.IsChecked = false;
                _rdoCribStats.IsChecked = false;
                _rdoGameStats.IsChecked = true;
                _rdoHandStats.IsChecked = false;
            }
        }

        private void OnDone(object sender, RoutedEventArgs e)
        {
            OnDone();
        }

        private void OnDone()
        {
            if (OnDoneViewingStats != null)
            {
                OnDoneViewingStats(this, null);

            }
            if (_tcs != null)
            {
                _tcs.SetResult(null);
                _tcs = null;
                 _opened = false;
    }
        }

        private void OnChangeStatView(object sender, RoutedEventArgs e)
        {
            RadioButton radio = sender as RadioButton;
            if (radio.Tag != null)
            {
                string tag = radio.Tag as String;
                StatViewType type = (StatViewType)Enum.Parse(typeof(StatViewType), tag);
                _listView.ItemsSource = _stats.StatsList(type);
                _listView.SelectedIndex = 0;
                _listView.Focus(Windows.UI.Xaml.FocusState.Programmatic);
            }
        }

        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _outerGrid.Width = e.NewSize.Width;
            _outerGrid.Height = e.NewSize.Height;
            _listView.Width = e.NewSize.Width;
        }

        private async void OnReset(object sender, RoutedEventArgs e)
        {
            var messageDialog = new MessageDialog("Are you sure you want to reset your stats?");
            messageDialog.Commands.Add(new UICommand("Yes", delegate(IUICommand command)
            {
                ResetAllStats();

            }));
            messageDialog.Commands.Add(new UICommand("No"));

            // call the ShowAsync() method to display the message dialog
            await messageDialog.ShowAsync();
        }

        private void ResetAllStats()
        {
            _stats.Reset();
            ObservableCollection<Stat> col = (ObservableCollection<Stat>)_listView.ItemsSource;
            _listView.ItemsSource = null;
            _listView.ItemsSource = col;
        }

        public string SerializeStats()
        {
            string s = _stats.SerializeGameStats();
            return s;

        }


        internal void LoadSettings(string stats)
        {
            _stats.DeserializeGameStats(stats);
        }

        public async Task WaitForOk()
        {
            _tcs = new TaskCompletionSource<object>();
            await _tcs.Task;
           _tcs = null;
        }

        private void GrabGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            OnDone();
        }
    }
}
