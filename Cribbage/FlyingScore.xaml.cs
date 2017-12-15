using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class FlyingScore : UserControl
    {
        Point _center;
        double _angle = -90;
        bool _animated = false;

        public bool Animated
        {
            get { return _animated; }
            set { _animated = value; }
        }

        public FlyingScore()
        {
            this.InitializeComponent();
        }


        public double Angle
        {
            get { return _angle; }
            set { _angle = value; }
        }


        public Point Center
        {
            get { return _center; }
            set { _center = value; }
        }
        public string Message
        {
            get
            {
                return _txtMessage.Text;
            }
            set
            {
                _txtMessage.Text = value;
            }
        }


        public void Animate(double duration, double beginTime)
        {
            _ctScore.CenterX = _center.X;
            _ctScore.CenterY = _center.Y;

            _daRotateScore.To = _angle;
            _daRotateScore.Duration = TimeSpan.FromMilliseconds(duration);
            _daRotateScore.BeginTime = TimeSpan.FromMilliseconds(beginTime);

            _daRotateText.To = -1 * _angle;
            _daRotateText.Duration = _daRotateScore.Duration;
            _daRotateText.BeginTime = _daRotateScore.BeginTime;
            _sbMoveScore.Begin();
            _animated = true;
        }

        public Storyboard Storyboard
        {
            get
            {
                return _sbMoveScore;
            }
        }
    }
}
