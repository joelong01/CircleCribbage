using CribbageService;
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
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{

    interface ITestButtonsCallback
    {
      void  OnDeal(bool Increment);      
      void  OnGetComputerCrib(bool Increment);
      void  OnMoveToCrib(bool Increment);
      void  OnBackToDeck(bool Increment);
      void  OnFlipAllCards(bool Increment);
      void  OnBackToOwner(bool Increment);
      void  OnSetPlayerScore(bool Increment);
      void  OnChangeRTO(bool Increment);      
      void  OnRunHandAnimations(bool Increment);

      void OnTestHand();
      
    }

   
    public sealed partial class TestGridCtrl : UserControl
    {

        private Open _open = Open.Right;
        private ITestButtonsCallback _callback = null;

        internal ITestButtonsCallback Callback
        {
            get { return _callback; }
            set { _callback = value; }
        }

     
        public Open Open
        {
            get { return _open; }
            set
            {
                if (value == Open.Up || value == Open.Down || value == Open.Left)
                    throw new NotSupportedException();

                _open = value;
            }
        }

        public TestGridCtrl()
        {
            this.InitializeComponent();
        }


        private void Deal(object sender, RoutedEventArgs e)
        {
            _callback.OnDeal((bool)_checkboxTest.IsChecked);            
        }

        private void GetComputerCrib(object sender, RoutedEventArgs e)
        {
            _callback.OnGetComputerCrib((bool)_checkboxTest.IsChecked);
               
            
        }

        private void MoveToCrib(object sender, RoutedEventArgs e)
        {
            _callback.OnMoveToCrib((bool)_checkboxTest.IsChecked);
        }

        private void BackToDeck(object sender, RoutedEventArgs e)
        {
            _callback.OnBackToDeck((bool)_checkboxTest.IsChecked);
        }

        private void FlipAllCards(object sender, RoutedEventArgs e)
        {
            _callback.OnFlipAllCards((bool)_checkboxTest.IsChecked);

        }

        private void BackToOwner(object sender, RoutedEventArgs e)
        {
            _callback.OnBackToOwner((bool)_checkboxTest.IsChecked);

        }

        private void TestGridTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_animationTestOpen.To > 25)
                _animationTestOpen.To = 0;
            else
                _animationTestOpen.To = (this.ActualWidth - 23);

            _sbOpenTest.Begin();
        }

        public void SetOpenPosition(double top, double left)
        {
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
            _animationTestOpen.To = -(this.ActualWidth - 23);
            _sbOpenTest.Begin();

        }

        public async Task Hide()
        {

            _animationTestOpen.To = 0;

            await _sbOpenTest.ToTask();
        }

        private void SetPlayerScore(object sender, RoutedEventArgs e)
        {
            _callback.OnSetPlayerScore((bool)_checkboxTest.IsChecked);
        }

        private void ChangRTO(object sender, RoutedEventArgs e)
        {
            _callback.OnChangeRTO((bool)_checkboxTest.IsChecked);
        }
        
        private void RunHandAnimations(object sender, RoutedEventArgs e)
        {
            _callback.OnRunHandAnimations((bool)_checkboxTest.IsChecked);
        }
        private void TestHand(object sender, RoutedEventArgs e)
        {
            _callback.OnTestHand();
        }
    }
}
