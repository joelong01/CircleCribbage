using CribbageService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class NewPegControl : UserControl
    {
        CompositeTransform _textTransform = new CompositeTransform();

        public NewPegControl()
        {
            this.InitializeComponent();
        }

        int _score = 0;
        double _textAngle = 0.0;
        
        PlayerType _playerType = PlayerType.Player;

        public int Score
        {
            get { return _score; }
            set
            {
                _score = value;

               
               _tbInsideScore.Text = value.ToString();
               _tbOutsideScore.Text = _tbInsideScore.Text;

            }
        }

        public PlayerType Owner
        {
            get
            {
                return _playerType;
            }
            set
            {
                _playerType = value;    
                 if (_playerType == PlayerType.Computer)
                 {

                     _tbInsideScore.Visibility = Visibility.Visible;
                     _tbOutsideScore.Visibility = Visibility.Collapsed;
                 }
                 else
                 {
                     _tbInsideScore.Visibility = Visibility.Collapsed;
                     _tbOutsideScore.Visibility = Visibility.Visible;
                 }
            }
        }

        public double TextAngle
        {
            get
            {
                return _textAngle;
            }
            set
            {

                _textAngle = value;
                _textTransform.Rotation = _textAngle;
                _tbInsideScore.RenderTransform = _textTransform;
                _tbOutsideScore.RenderTransform = _textTransform;
            }
        }
       
    }
}
