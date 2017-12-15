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
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class PromptUserDlg : UserControl
    {
        private const  long WINDOW_HEIGHT = 150;

        UserChoice _choice = UserChoice.Continue;
        TaskCompletionSource<object> _tcs = null;
        Popup _dialogPopup = null;
        public PromptUserDlg()
        {
            this.InitializeComponent();
            _dialogPopup = new Popup { Child = this };
            _dialogPopup.Transitions = new TransitionCollection();
            var trans = new PopupThemeTransition();
            trans.FromHorizontalOffset = 400;
            _dialogPopup.Transitions.Add(trans);
            
        }

        private void OnClickMuggins(object sender, RoutedEventArgs e)
        {
            _choice = UserChoice.Muggins;
            _tcs.SetResult(null);
        }

        private void OnClickContinue(object sender, RoutedEventArgs e)
        {
            _choice = UserChoice.Continue;
            _tcs.SetResult(null);
        }

        public async Task<UserChoice> ShowAndWait(string message)
        {

            _tcs = new TaskCompletionSource<object>();
           
            
            _txtMessage.Text = message;
            this.UpdateLayout();
            Panel parent = MainPage.Current.DialogParent;
            parent.SizeChanged += Parent_SizeChanged;
            this.Width = parent.ActualWidth;
            this.Height = WINDOW_HEIGHT;
            parent.Children.Add(_dialogPopup);           
            _dialogPopup.IsOpen = true;
            await _tcs.Task;
            _dialogPopup.IsOpen = false;
            parent.Children.Remove(_dialogPopup);
            parent.SizeChanged -= Parent_SizeChanged;
            return _choice;
        }

        void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _dialogPopup.IsOpen = false;
            _dialogPopup.Width = e.NewSize.Width;
            _dialogPopup.IsOpen = true;
        }
    }
}
