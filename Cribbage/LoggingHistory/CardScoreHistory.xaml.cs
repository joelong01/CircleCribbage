using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CribbageService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{


    public sealed partial class CardScoreHistory : UserControl
    {

        private bool _mouseCaptured = false;
        private Point _pointMouseDown;
        ObservableCollection<OneHandHistoryCtrl> _list = new ObservableCollection<OneHandHistoryCtrl>();


        public CardScoreHistory()
        {
            this.InitializeComponent();
            _listHands.ItemsSource = _list;


        }

        public OneHandHistoryCtrl Current
        {
            get
            {
                try
                {
                    OneHandHistoryCtrl ctrl = null;
                    if (_list.Count == 0)
                    {
                        ctrl = new OneHandHistoryCtrl();
                        _list.Add(ctrl);
                    }
                    else
                    {
                        ctrl = _list[_list.Count - 1];
                    }
                    return ctrl;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Excption Caught! {0}", e.Message);

                }
                return null;
            }

        }

        public OneHandHistoryCtrl AddCreateHandHistory()
        {
            OneHandHistoryCtrl ctrl = new OneHandHistoryCtrl();
            _list.Add(ctrl);
            ctrl.UpdateLayout();
            return ctrl;

        }

        internal void Reset()
        {
            _list.Clear();
        }

        private void LayoutRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

            _pointMouseDown = e.GetCurrentPoint(this).Position;
            _mouseCaptured = ((UIElement)sender).CapturePointer(e.Pointer);
            e.Handled = true;

        }

        private void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_mouseCaptured)
                return;


        }

        private async void LayoutRoot_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;



            if (_mouseCaptured)
            {
                _mouseCaptured = false;
                this.ReleasePointerCapture(e.Pointer);
                await ToggleOpen();

            }
        }

        int _open = 0;
        public async Task ToggleOpen()
        {
            if (_open == 0)
            {

                _xAnimation.Value = this.ActualWidth - 20 - LayoutRoot.ColumnDefinitions[0].ActualWidth;
            }
            else
            {
                _xAnimation.Value = 0;
            }

            await StaticHelpers.RunStoryBoard(ScoreHistoryAnimatePosition, false, 1000, false);

            _open = 1 - _open;
        }


    }
}
