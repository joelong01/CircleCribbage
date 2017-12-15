using Cribbage.Common;
using CribbageService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
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


namespace Cribbage
{



    public partial class CribbageView 
    {

        public string Save()
        {
            return HintWindow.Save();
        }

        public Task<bool> Load(string s)
        {
            return HintWindow.Load(s);
        }

        public void MoveCards(List<Task<object>> taskList, List<CardView> destinationList, CardContainer destinationContainer)
        {
            double duration = MainPage.AnimationSpeeds.Fast;
            foreach (CardView card in destinationList)
            {

                GridDeck.MoveCardToTarget(taskList, card, destinationContainer, duration, false, true);                
            }

        }


       
    }
}