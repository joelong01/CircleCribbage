using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed partial class TutorialControl : UserControl
    {

        
        public TutorialControl()
        {
            this.InitializeComponent();
            _tbRightArrow.Visibility = Visibility.Collapsed;
            _tbDownArrow.Visibility = Visibility.Collapsed;
        }

        private async void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Point pt = await DragAsync(this, e);
           // Debug.WriteLine("Position: {0}", pt);
        }

        public Task<Point> DragAsync(TutorialControl control, PointerRoutedEventArgs origE, IDragAndDropProgress progress = null)
        {
            TaskCompletionSource<Point> taskCompletionSource = new TaskCompletionSource<Point>();
            UIElement mousePositionWindow = Window.Current.Content;
            Point pointMouseDown = origE.GetCurrentPoint(mousePositionWindow).Position;
      
            PointerEventHandler pointerMovedHandler = null;
            PointerEventHandler pointerReleasedHandler = null;

            pointerMovedHandler = (Object s, PointerRoutedEventArgs e) =>
            {

                Point pt = e.GetCurrentPoint(mousePositionWindow).Position;
                Point delta = new Point();
                delta.X = pt.X - pointMouseDown.X;
                delta.Y = pt.Y - pointMouseDown.Y;

            

                if (progress != null)
                {
                    progress.Report(pt);
                }

                
                 this.TranlateReletive(delta);                
                pointMouseDown = pt;

            };

            pointerReleasedHandler = (Object s, PointerRoutedEventArgs e) =>
            {
                TutorialControl localControl = (TutorialControl)s;
                localControl.PointerMoved -= pointerMovedHandler;
                localControl.PointerReleased -= pointerReleasedHandler;
                localControl.ReleasePointerCapture(origE.Pointer);                
                Point exitPoint = e.GetCurrentPoint(mousePositionWindow).Position;

                if (progress != null)
                {
                    progress.PointerUp(exitPoint);
                }
               
                taskCompletionSource.SetResult(exitPoint);
            };

            control.CapturePointer(origE.Pointer);
            control.PointerMoved += pointerMovedHandler;
            control.PointerReleased += pointerReleasedHandler;
            return taskCompletionSource.Task;
        }

        private void TranlateReletive(Point delta)
        {

            _gridTransform.TranslateX += delta.X;
            _gridTransform.TranslateY += delta.Y;

            LayoutRoot.RenderTransform = _gridTransform;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Grid)this.Parent).Children.Remove(this);
        }

        internal async Task Start()
        {
            _tbText.Text = "This is the app bar. Right click on the app or swipe up to open it.  The functions should be self explanatory.";
            _daMoveX.To = 435;
            _daMoveY.To = 600;
            _sbMove.Duration = TimeSpan.FromSeconds(2);
            _tbDownArrow.Visibility = Visibility.Visible;
            await _sbMove.ToTask();
           
        }
    }
}
