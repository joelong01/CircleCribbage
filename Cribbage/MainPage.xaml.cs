using CribbageService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.Extensions;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Cribbage
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //
        //  Settings support
        Popup _settingsPopup = null;
        double _settingsWidth = 346;
        Rect _windowBounds;
        SettingsCtrl _ctrlSettings = null;
        Deck _deck = null;
        ClientStateMachine _stateMachine = null;
        DispatcherTimer _initViewTimer = new DispatcherTimer();
        //
        //  logging
        public static LogTrace LogTrace = null;
        public static AnimationSpeedsClass AnimationSpeeds = null;

      public Deck Deck
        {
            get
            {
                return _deck;
            }
        }

        internal AppBar AppBar
        {
            get
            {
                return _appBarBottom;
            }
        }

        public Settings Settings
        {
            get
            {
                if (_ctrlSettings == null)
                    return null;

                return _ctrlSettings.Settings;
            }
        }

        public StatsViewControl StatsView
        {
            get
            {
                NewRoundBoardPage board = (NewRoundBoardPage)_frame.Content;
                return board.StatsView;
                
            }
        }

        public static MainPage Current;

        public MainPage()
        {
            this.InitializeComponent();
           

        }

        private async Task Init()
        {
            LogTrace = new LogTrace();
            await LogTrace.Init(true);
            Current = this;

            AnimationSpeeds = new AnimationSpeedsClass(AnimationSpeedSetting.Regular);
            _deck = new Deck();

            InitializeSettings();
            _windowBounds = Window.Current.Bounds;
            Window.Current.SizeChanged += OnWindowSizeChanged;
            _settingsPopup = new Popup();
            CurrentBoardType = BoardType.Uninitialized;
            
            _frame.Navigate(typeof(NewRoundBoardPage));
            CurrentBoardType = BoardType.Round;

            CribbageView view = GetCribbageView();
            view.InitializeAsync(_deck, _stateMachine);



            if (_stateMachine != null)
                _stateMachine.TransferState();

          
            _appBarBottom.IsOpen = true;
        }
        
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await Init();
          
        }

        private async void OnShowStats(object sender, RoutedEventArgs e)
        {
            //_appBarBottom.IsOpen = false;
            //_statsPopup.IsLightDismissEnabled = true;

            //CribbageView view = GetCribbageView();

            //_statsPopup.Width = view.CenterGrid.ActualWidth;
            //_statsPopup.Height = view.CenterGrid.ActualHeight;
            //_statsPopup.VerticalOffset = 225;
            //_statsPopup.HorizontalOffset = 100;
            //_statsPopup.Child = _ctrlStatsView;
            //_statsPopup.SetValue(Canvas.LeftProperty, 0);
            //_statsPopup.SetValue(Canvas.TopProperty, 0);
            //_statsPopup.IsOpen = true;

            _appBarBottom.IsOpen = false;
            _appBarBottom.IsEnabled = false;
            Debug.WriteLine("Showing stats");
            NewRoundBoardPage board = (NewRoundBoardPage)_frame.Content;
            await board.ShowStats();
            _appBarBottom.IsEnabled = true;
            Debug.Write("Stats hidden");

        }

        

       

      
        public Panel DialogParent
        {
            get
            {
                return _frame.GetFirstDescendantOfType<Panel>();
            }
        }

       

        private void OnWindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {

        }


        CribbageView GetCribbageView()
        {
            if (_frame.Content == null)
                return null;

            IBaseView baseView = _frame.Content as IBaseView;
            return baseView.GetCommonView();
        }

        private async void OnNewGame(object sender, RoutedEventArgs e)
        {
            _btnNewGame.IsEnabled = false;
            _appBarBottom.IsOpen = false;
            CribbageView view = GetCribbageView();
            _stateMachine = new ClientStateMachine();
            _stateMachine.Init(false, view);
            await view.OnNewGame(_stateMachine);
            _btnNewGame.IsEnabled = true;

        }

        private async void OnSaveGame(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            b.IsEnabled = false;           
            try
            {
                if (_stateMachine == null)
                    return;

                string saveString = _stateMachine.Save();
                if (saveString == "")
                    return;


                var filePicker = new FileSavePicker();
                filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                filePicker.FileTypeChoices.Add("Cribbage Files", new[] { ".crib" });
                StorageFile file = await filePicker.PickSaveFileAsync();
                if (file == null)
                    return;

                await FileIO.WriteTextAsync(file, saveString);
            }
            catch (Exception exception)
            {
                MainPage.LogTrace.TraceMessageAsync((exception.ToString()));
                
            }

            
             finally
            {
                b.IsEnabled = true; ;
            }
        }

        private async void OnOpenGame(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            b.IsEnabled = false;
            _btnNewGame.IsEnabled = false;
            _appBarBottom.IsOpen = false;
            try
            {

                var filePicker = new FileOpenPicker();
                filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                filePicker.ViewMode = PickerViewMode.List;
                filePicker.FileTypeFilter.Add(".crib");
                StorageFile file = await filePicker.PickSingleFileAsync();
                if (file == null)
                    return;



                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        string savedGame = streamReader.ReadToEnd();

                        CribbageView view = GetCribbageView();
                        _stateMachine = new ClientStateMachine();
                        _stateMachine.Init(false, view);
                        await view.OnLoadGame(_stateMachine);
                        await _stateMachine.Load(savedGame);
                    }
                }
            }

            finally
            {
                b.IsEnabled = true;
                _btnNewGame.IsEnabled = true;
            }
        }

        private void OnGetSuggestedCard(object sender, RoutedEventArgs e)
        {
            GetCribbageView().OnGetSuggestedCard();
        }

        private void OnShowLogFile(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LogViewer), this);
        }

        private void OnOptions(object sender, RoutedEventArgs e)
        {
            _appBarBottom.IsOpen = false;

            _settingsPopup.Closed += SettingsPopup_Closed;
            Window.Current.Activated += OnWindowActivated;
            _settingsPopup.IsLightDismissEnabled = true;
            _settingsWidth = Math.Max(Window.Current.Bounds.Width * .25, _settingsWidth);
            _settingsPopup.Width = _settingsWidth;
            _settingsPopup.Height = _windowBounds.Height;



            _ctrlSettings.OnOptionChanged += OnOptionChanged;
            _ctrlSettings.Width = _settingsWidth;
            _ctrlSettings.Height = _windowBounds.Height;

            _settingsPopup.Child = _ctrlSettings;
            _settingsPopup.SetValue(Canvas.LeftProperty, _windowBounds.Width - _settingsWidth);

#if false
            if (SettingsPane.Edge == SettingsEdgeLocation.Right)
            {
                _settingsPopup.SetValue(Canvas.LeftProperty, _windowBounds.Width - _settingsWidth);
            }
            else
            {
                _settingsPopup.SetValue(Canvas.LeftProperty, 0);

            }
#endif
            _settingsPopup.SetValue(Canvas.TopProperty, 0);
            _settingsPopup.IsOpen = true;
        }

     

     


    #region Settings
    void InitializeSettings()
        {
            if (_ctrlSettings != null) return;

            _ctrlSettings = new SettingsCtrl();
            _ctrlSettings.OnOptionChanged += OnOptionChanged;
            _settingsPopup = new Popup();

        }

        //void MainPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        //{
        //    if (_settingsPopup == null)
        //        return;
        //    _windowBounds = Window.Current.Bounds;
        //    SettingsCommand cmd = new SettingsCommand(Guid.NewGuid(), "eCribbage", (x) =>
        //    {

        //        _settingsPopup.Closed += SettingsPopup_Closed;
        //        Window.Current.Activated += OnWindowActivated;
        //        _settingsPopup.IsLightDismissEnabled = true;
        //        _settingsPopup.Width = _settingsWidth;
        //        _settingsPopup.Height = _windowBounds.Height;



        //        _ctrlSettings.OnOptionChanged += OnOptionChanged;
        //        _ctrlSettings.Width = _settingsWidth;
        //        _ctrlSettings.Height = _windowBounds.Height;

        //        _settingsPopup.Child = _ctrlSettings;
        //        if (SettingsPane.Edge == SettingsEdgeLocation.Right)
        //        {
        //            _settingsPopup.SetValue(Canvas.LeftProperty, _windowBounds.Width - _settingsWidth);
        //        }
        //        else
        //        {
        //            _settingsPopup.SetValue(Canvas.LeftProperty, 0);

        //        }
        //        _settingsPopup.SetValue(Canvas.TopProperty, 0);
        //        _settingsPopup.IsOpen = true;



        //    });

        //    args.Request.ApplicationCommands.Add(cmd);
        //}


        public async Task OnOptionChanged(object sender, CribbageSettingsEventArgs e)
        {

            var tcs = new TaskCompletionSource<object>();
            Settings settings = _ctrlSettings.Settings;

            MainPage.LogTrace.TraceMessageAsync("Updated Settings " + settings.ToString());

            //
            //  note:  you pass in a reference to your settings object -- so the data is already up to date
            //         here you should just interpret the settings and do the right thing
            switch (e.ChangedSetting)
            {
                case CribbageSettings.AnimationSpeed:
                    MainPage.AnimationSpeeds.AnimationSpeed = settings.AnimationSpeed;
                    break;
                case CribbageSettings.HideSettings:
                    if (settings.HideSettings == false)
                    {
                        _btnOptions.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        _btnOptions.Visibility = Visibility.Collapsed;
                    }
                    break;
                case CribbageSettings.LockRotation:
                    break;
                case CribbageSettings.ShowInstructions:
                    break;
                case CribbageSettings.HitContinueOnGo:
                //foreach (CardView card in _gridPlayedCards.Items)
                //{
                //    if (settings.HitContinueOnGo)
                //    {
                //        if (card.AnimatedOpacity == 0.5)
                //        {
                //            await card.SetOrientation(CardOrientaton.FaceDown);
                //            await card.AnimateFade(1.0);
                //        }
                //    }
                //    else
                //    {
                //        if (card.Orientation == CardOrientaton.FaceDown)
                //        {
                //            await card.SetOrientation(CardOrientaton.FaceUp);
                //            await card.AnimateFade(0.5);
                //        }
                //    }

                //}
                    break;
                case CribbageSettings.BoardType:

                

                    break;
                case CribbageSettings.EnableLogging:
                    MainPage.LogTrace.EnableLogging = settings.EnableLogging;
                    break;

            }

            tcs.SetResult(null);

            await tcs.Task;

        }

    

        void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                _settingsPopup.IsOpen = false;

            }
        }

        void SettingsPopup_Closed(object sender, object e)
        {
            Window.Current.Activated -= OnWindowActivated;
        }
#endregion

        internal void EnableAppBarButtons(bool enabled)
        {
            _btnNewGame.IsEnabled = enabled;
          
        }


        internal void EnableSaveGame(bool enable)
        {
            _btnOnSave.IsEnabled = enable;
        }

        public BoardType CurrentBoardType { get; set; }


    }
}
