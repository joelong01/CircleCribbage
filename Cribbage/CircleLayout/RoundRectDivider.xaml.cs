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
    public sealed partial class RoundRectDivider : UserControl
    {
        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(RoundRectDivider), null);

       // double _radius = 400;

        //public double Radius
        //{
        //    get { return _radius; }
        //    set
        //    {
        //        _radius = value;
        //        UpdateRadius(value);
        //    }
        //}

        public RoundRectDivider()
        {
            this.InitializeComponent();
            this.DataContext = this;
            Radius = 400;
        }

        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set
            {
                SetValue(RadiusProperty, value);
                UpdateRadius(value);
            }
        }

        public double Angle
        {
            get
            {
                return _rotateControl.Angle;
            }
            set
            {
                _rotateTextBlock.Angle = -value;
                _rotateControl.Angle = value;

            }
        }

        public string Text
        {
            get
            {
                return _tb.Text;
            }
            set
            {
                _tb.Text = value;
            }
        }

        public double ControlWidth
        {
            get
            {
                double width = this.Width;
                if (width == 0) width = 96;
                if (System.Double.NaN.CompareTo(width) == 0) width = 96;
                return width;
            }
            set
            {
                this.Width = value;
                UpdateRadius(this.Radius);
            }
        }

        public double ControlHeight
        {
            get
            {
                double height = this.Height;
                if (height == 0) height = 24;
                if (System.Double.NaN.CompareTo(height) == 0) height = 24;

                return height;
            }
            set
            {
                this.Height = value;
                UpdateRadius(this.Radius);
            }
        }

        //
        //  this should be the boarder width of the RingSlice
        public double XOffset
        {
            get
            {
                return _ttControl.X;
            }
            set
            {
                _ttControl.X = value;
            }
        }



        private void UpdateRadius(double radius)
        {


            double height = this.ControlHeight; ;
            double width = this.ControlWidth;

            if (width * height * radius == 0) return;

            _ttControl.Y = radius - height / 2.0;
            _rotateControl.CenterY = radius - height / 2.0;
            _rotateControl.CenterX = radius - width / 2.0;
        }




    }
}
