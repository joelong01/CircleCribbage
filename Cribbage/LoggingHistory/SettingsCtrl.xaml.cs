using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Runtime.Serialization.Json;
using Windows.Storage;
using System.Threading.Tasks;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{


    //
    //  so you don't forget...
    //  to create a setting that is saved and loaded automagically, add a property to the Settings class and add its name to the CribbageSettings enum.  
    //  spelling matters.  they have to match.
    //
    //  the only types that are supported are: AnimationSpeedSetting, bool, Int, and GameDifficulty.  if you add an enum, you have to read it correctly below
    //
    //  to add a new enum you
    //  1. declare the enum below
    //  2. add the name of the seeting to the CribbageSettings enum
    //  3. add a property to the Settings class
    //  4. add the default to the ctor of Settings
    //  5. update the Settings.ToString() method
    //  6. add to the UpdateUi() function
    //  7. if it is an enum, update the LoadSettings API

    public enum CribbageSettings { HideSettings, LockRotation, ShowInstructions, AnimationSpeed, HitContinueOnGo, EnableLogging, AutoSetScore, OKAfterCount, OKAfterHand, OKAfterCrib, Difficulty, BoardType };

    public enum GameDifficulty { Easy, Normal, Hard };
    
    public enum BoardType { Round, Traditional, Uninitialized };

    public class CribbageSettingsEventArgs : EventArgs
    {

        public CribbageSettings ChangedSetting { get; set; }

        public CribbageSettingsEventArgs(CribbageSettings setting)
        {
            ChangedSetting = setting;
        }

    }

    public class Settings
    {


        AnimationSpeedsClass _animationSpeeds = new AnimationSpeedsClass(AnimationSpeedSetting.Regular);
        AnimationSpeedSetting _animationSpeedBase = AnimationSpeedSetting.Regular;
        //
        //  UI Settings        
        public bool HideSettings { get; set; }
        public bool LockRotation { get; set; }
        public bool ShowInstructions { get; set; }
        public AnimationSpeedSetting AnimationSpeed
        {
            get
            {
                return _animationSpeedBase;
            }
            set
            {
                _animationSpeedBase = value;
                _animationSpeeds.AnimationSpeed = _animationSpeedBase;

            }
        }
        public bool EnableLogging { get; set; }
        public bool AutoSetScore { get; set; }
        public bool HitContinueOnGo { get; set; }

        public bool OKAfterCount { get; set; }
        public bool OKAfterHand { get; set; }
        public bool OKAfterCrib { get; set; }
        public GameDifficulty Difficulty { get; set; }

        
        public BoardType BoardType { get; set; }

     
        public Settings()
        {
            HideSettings = false;
            LockRotation = false;
            ShowInstructions = true;
            AnimationSpeed = AnimationSpeedSetting.Regular;
            HitContinueOnGo = true;
            EnableLogging = true;
            AutoSetScore = true;
            OKAfterCount = false;
            OKAfterCrib = true;
            OKAfterHand = true;
            this.Difficulty = GameDifficulty.Normal;            
            this.BoardType = BoardType.Round;


        }

        public override string ToString()
        {
            return String.Format("HideOptions={0} LockRotation={1} ShowInstructions={2} AnimationSpeed={3} HitContinueOnGo={4} EnableLogging={5} AutoSetScore={6} OKAfterCount={7} OKAfterHand={8} OKAfterCrib={9} GameDifficulty={10} BoardType={11}",
                                HideSettings, LockRotation, ShowInstructions, AnimationSpeed, HitContinueOnGo, EnableLogging, AutoSetScore, OKAfterCount, OKAfterHand, OKAfterCrib, Difficulty, BoardType);
        }

    }

    public delegate Task OptionsChangedEvent(object sender, CribbageSettingsEventArgs e);

    public sealed partial class SettingsCtrl : UserControl
    {
        private Settings _settings = null;

        public Settings Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }
        private bool _initializing = true;

        public event OptionsChangedEvent OnOptionChanged;

        public SettingsCtrl()
        {
            this.InitializeComponent();
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                _settings = LoadSettings();
                UpdateUi();
            }

        }

        private void UpdateUi()
        {
            switch (_settings.AnimationSpeed)
            {
                case AnimationSpeedSetting.Regular:
                    _radioNormal.IsChecked = true;
                    _radioFast.IsChecked = false;
                    _radioSlow.IsChecked = false;
                    break;
                case AnimationSpeedSetting.Fast:
                    _radioNormal.IsChecked = false;
                    _radioFast.IsChecked = true;
                    _radioSlow.IsChecked = false;
                    break;
                case AnimationSpeedSetting.Slow:
                    _radioNormal.IsChecked = false;
                    _radioFast.IsChecked = false;
                    _radioSlow.IsChecked = true;
                    break;

            }

            switch (_settings.Difficulty)
            {
                case GameDifficulty.Easy:
                    _radioEasyGame.IsChecked = true;
                    _radioNormalGame.IsChecked = false;
                    _radioHardGame.IsChecked = false;
                    break;
                case GameDifficulty.Normal:
                    _radioEasyGame.IsChecked = false;
                    _radioNormalGame.IsChecked = true;
                    _radioHardGame.IsChecked = false;
                    break;
                case GameDifficulty.Hard:
                    _radioEasyGame.IsChecked = false;
                    _radioNormalGame.IsChecked = false;
                    _radioHardGame.IsChecked = true;
                    break;

            }

            


            _chkHideOptions.IsChecked = _settings.HideSettings;
            _chkLockRotation.IsChecked = _settings.LockRotation;
            _chkShowInstructions.IsChecked = _settings.ShowInstructions;
            _chkHitContinueOnGo.IsChecked = _settings.HitContinueOnGo;
            _chkEnableLogging.IsChecked = _settings.EnableLogging;
            _chkAutoSetScore.IsChecked = _settings.AutoSetScore;
            _chkHitOKCounting.IsChecked = _settings.OKAfterCount;
            _chkHitOKHand.IsChecked = _settings.OKAfterHand;
            _chkHitOKCrib.IsChecked = _settings.OKAfterCrib;


            _toggleBoardType.IsOn = (_settings.BoardType == BoardType.Traditional);

            _initializing = false;

        }


        private void OnBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Parent.GetType() == typeof(Popup))
            {
                ((Popup)this.Parent).IsOpen = false;
            }
            
        }



        private void HideSettings_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            _settings.HideSettings = (((CheckBox)sender).IsChecked == true);
            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.HideSettings));
            }
        }

        private void LockRotation_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;

            _settings.LockRotation = (((CheckBox)sender).IsChecked == true);
            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.LockRotation));
            }
        }


        private void ShowInstructions_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;

            _settings.ShowInstructions = (((CheckBox)sender).IsChecked == true);
            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.ShowInstructions));
            }
        }

        private void OnHitContinueOnGo_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;

            _settings.HitContinueOnGo = (((CheckBox)sender).IsChecked == true);

            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.HitContinueOnGo));
            }
        }

        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;

            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                return;

            RadioButton rb = sender as RadioButton;

            if (rb.Name == "_radioFast")
                _settings.AnimationSpeed = AnimationSpeedSetting.Fast;
            else if (rb.Name == "_radioNormal")
                _settings.AnimationSpeed = AnimationSpeedSetting.Regular;
            else if (rb.Name == "_radioSlow")
                _settings.AnimationSpeed = AnimationSpeedSetting.Slow;
            else
                throw new Exception("Unexpected error.");

            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.AnimationSpeed));
            }
        }

        private void DifficultyRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;

            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                return;

            RadioButton rb = sender as RadioButton;

            if (rb.Name == "_radioEasyGame")
                _settings.Difficulty = GameDifficulty.Easy;
            else if (rb.Name == "_radioNormalGame")
                _settings.Difficulty = GameDifficulty.Normal;
            else if (rb.Name == "_radioHardGame")
                _settings.Difficulty = GameDifficulty.Hard;
            else
                throw new Exception("Unexpected error.");

            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.Difficulty));
            }
        }
        private void EnableLogging_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            _settings.EnableLogging = (((CheckBox)sender).IsChecked == true);

            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.EnableLogging));
            }
        }

        private void OnResetToDefaults(object sender, RoutedEventArgs e)
        {
            _settings.HideSettings = false;
            _settings.LockRotation = false;
            _settings.ShowInstructions = true;
            _settings.AnimationSpeed = AnimationSpeedSetting.Regular;
            _settings.HitContinueOnGo = true;
            _settings.EnableLogging = true;
            _settings.AutoSetScore = true;
            _settings.OKAfterCount = false;
            _settings.OKAfterCrib = true;
            _settings.OKAfterHand = true;
            _settings.Difficulty = GameDifficulty.Normal;
            


            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.AnimationSpeed));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.EnableLogging));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.HitContinueOnGo));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.HideSettings));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.LockRotation));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.ShowInstructions));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.AutoSetScore));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.OKAfterCount));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.OKAfterCrib));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.OKAfterHand));
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.Difficulty));
                
            }

            UpdateUi();
        }

        private void AutoSetScore_Checked(object sender, RoutedEventArgs e)
        {

            if (_initializing)
                return;
            if (((CheckBox)sender).IsChecked == true)
            {
                _settings.AutoSetScore = true;
                
            }
            else
            {
                _settings.AutoSetScore = false;
            }
            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.AutoSetScore));
                
            }



        }

        private void HitOKForCounting_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            _settings.OKAfterCount = (((CheckBox)sender).IsChecked == true);

            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.OKAfterCount));
            }

        }

        private void HitOKForHand_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            _settings.OKAfterHand = (((CheckBox)sender).IsChecked == true);

            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.OKAfterHand));
            }
        }

        private void HitOKForCrib_Checked(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            _settings.OKAfterCrib = (((CheckBox)sender).IsChecked == true);

            SaveSettings();
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.OKAfterCrib));
            }

        }


        private void SaveSettings()
        {
            SaveSettings(_settings);
        }

        public static void SaveSettings(Settings settings)
        {
            ApplicationDataContainer storage = ApplicationData.Current.LocalSettings;
            storage.CreateContainer("eCribbage", ApplicationDataCreateDisposition.Always);
            if (storage != null)
            {
                foreach (CribbageSettings setting in Enum.GetValues(typeof(CribbageSettings)))
                {
                    PropertyInfo propInfo = settings.GetType().GetTypeInfo().GetDeclaredProperty(setting.ToString());
                    string s = propInfo.GetValue(settings, null).ToString();
                    storage.Containers["eCribbage"].Values[setting.ToString()] = s;

                }
            }
        }

        private Settings LoadSettings()
        {
            Settings settings = new Settings();
            ApplicationDataContainer storage = ApplicationData.Current.LocalSettings;

            if (storage == null)
                return settings;

            bool hasContainer = storage.Containers.ContainsKey("eCribbage");
            if (!hasContainer)
                return settings;

            ApplicationDataContainer container = storage.Containers["eCribbage"];
            try
            {
                foreach (CribbageSettings setting in Enum.GetValues(typeof(CribbageSettings)))
                {
                    try
                    {
                        PropertyInfo propInfo = settings.GetType().GetTypeInfo().GetDeclaredProperty(setting.ToString());
                        Object value = storage.Containers["eCribbage"].Values[setting.ToString()];
                        if (propInfo.PropertyType.Name == "AnimationSpeedSetting")
                        {
                            settings.AnimationSpeed = (AnimationSpeedSetting)Enum.Parse(typeof(AnimationSpeedSetting), (string)value);
                        }
                        else if (propInfo.PropertyType.Name == "GameDifficulty")
                        {
                            settings.Difficulty = (GameDifficulty)Enum.Parse(typeof(GameDifficulty), (string)value);
                        }
                        else if (propInfo.PropertyType == typeof(Int32))
                        {
                            propInfo.SetValue(settings, Convert.ToInt32(value));
                        }
                        else if (propInfo.PropertyType == typeof(Boolean))
                        {
                            propInfo.SetValue(settings, Convert.ToBoolean(value));
                        }
                        else if (propInfo.PropertyType == typeof(BoardType))
                        {
                            settings.BoardType = (BoardType)Enum.Parse(typeof(BoardType), (string)value);
                        }
                        else if (propInfo.PropertyType == typeof(CribbageStats))
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception enumerating storage containers.  did you forget to parse an enum? \n{0} ", ex.ToString());
                    }
                }
            }
            catch (Exception e) // swallow it - should only happen the first time the app is run since we save on exit
            {
                Debug.WriteLine("Exception enumerating storage containers.  did you forget to parse an enum? \n{0} ", e.ToString());
            }

            return settings;
        }

        
        

        private void Board_Toggled(object sender, RoutedEventArgs e)
        {
            _settings.BoardType = _toggleBoardType.IsOn ? BoardType.Traditional : BoardType.Round;
            SaveSettings();
            Debug.WriteLine("BoardType={0}", _settings.BoardType);
            if (OnOptionChanged != null)
            {
                OnOptionChanged(this, new CribbageSettingsEventArgs(CribbageSettings.BoardType));
            }
        }


    }
}
