using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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
    public sealed partial class LogViewer : UserControl
    {

        ObservableCollection<string> _logFiles = new ObservableCollection<string>();  

        public LogViewer()
        {
            this.InitializeComponent();
           _popup.IsLightDismissEnabled = true;
            _list.ItemsSource = _logFiles;

        }

       
        private void OnClose(object sender, RoutedEventArgs e)
        {
            
        }

        public string Text
        {
            get
            {
                return _txtLog.Text;
            }
            set
            {
                _txtLog.Text = value;
            }

        }

        public async Task Show()
        {
            double ScreenW = Window.Current.Bounds.Width;
            double ScreenH = Window.Current.Bounds.Height;
            _border.Width = ScreenW;
            _border.Height = ScreenH;
            _grid.Width = ScreenW - 1;
            _grid.Height = ScreenH - 1;
            _popup.IsOpen = true;
            _txtLog.Focus(Windows.UI.Xaml.FocusState.Programmatic);
            await  MainPage.LogTrace.GetLogFiles(_logFiles);
            if (_logFiles.Count > 0)
                _list.SelectedIndex = 0;
            
        }

        private void OnBackButton_Click(object sender, RoutedEventArgs e)
        {
            _popup.IsOpen = false;
        }

        private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                
                string file = (string)e.AddedItems[0];            
                this.Text = await MainPage.LogTrace.ReadLogFile(file);
                

            }
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            DataPackage data = new DataPackage();
            data.SetText(this.Text);
            Clipboard.SetContent(data);
        }

        private async void OnDelete(object sender, RoutedEventArgs e)
        {
            if (_list.SelectedItems.Count == 1)
            {
                int index = _list.SelectedIndex;
                bool ret = await MainPage.LogTrace.DeleteFile((string)_list.SelectedItem);
                if (ret)
                {
                    _logFiles.Remove((string)_list.SelectedItem);                    
                    
                    if (index == _logFiles.Count)
                        index = _logFiles.Count - 1;

                    if (index < 0) index = 0;

                    if (_logFiles.Count > 0)
                        _list.SelectedIndex = index;
                    else
                        this.Text = "";
                }
            }

        }
    }
}
