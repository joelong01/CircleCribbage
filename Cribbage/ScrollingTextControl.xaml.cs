using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed partial class ScrollingTextControl : UserControl
    {
        
        int _count = 0;
        const int TIMES_TO_SCROLL = 2;
        public ScrollingTextControl()
        {
            this.InitializeComponent();
            _sbScrollText.Completed += AnimationCompleted;
        }

        private void AnimationCompleted(object sender, object e)
        {
            if (_count < TIMES_TO_SCROLL)
            {                
                Start();
            }
            else
            {
                Stop();
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

        public void Start()
        {
            _tb.UpdateLayout();

            _count++;
            _sbScrollText.Stop();
            _daScrollText.To = -1* _tb.ActualWidth;
            Debug.WriteLine("Text: {1} Text Width: {0}", _daScrollText.To, _tb.Text);
            _daScrollText.Duration = TimeSpan.FromMilliseconds(50 * _tb.Text.Length);
            _sbScrollText.Begin();
        }
        public void Stop()
        {
            _count = 0;
            _sbScrollText.Stop();
            
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ctText.TranslateX = e.NewSize.Width;
            _rectClip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
            
            _canvas.Height = _tb.ActualHeight;
            LayoutRoot.RowDefinitions[0].Height = new GridLength(_canvas.Height);
            Canvas.SetTop(_tb, (_canvas.Height - _tb.ActualHeight) / 2.0);
            MainPage.LogTrace.TraceMessageAsync(String.Format("Width={0}", e.NewSize.Width));
        }

       
    }
}
