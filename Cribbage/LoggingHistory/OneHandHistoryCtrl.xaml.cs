using CribbageService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public sealed partial class OneHandHistoryCtrl : UserControl
    {

       
        public OneHandHistoryCtrl()
        {
            this.InitializeComponent();
            
       
        }

      

      

        public async Task SetPlayerCards(List<CardView> cards)
        {

            try
            {

                await SetCardToBitmap(cards[0], _playerC1);
                await SetCardToBitmap(cards[1], _playerC2);
                await SetCardToBitmap(cards[2], _playerC3);
                await SetCardToBitmap(cards[3], _playerC4); 

            }
            catch (Exception e)
            {
                Debug.WriteLine("Excption Caught! {0}", e.Message);

            }
        }

        private async Task SetCardToBitmap(CardView card, Rectangle rect)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(card.Face.Canvas, (int)rect.Width, (int)rect.Height);
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = renderTargetBitmap;
            rect.Fill = imageBrush;
        }

        public async Task SetComputerHand(List<CardView> cards)
        {

            try
            {
                await SetCardToBitmap(cards[0], _computerC1);
                await SetCardToBitmap(cards[1], _computerC2);
                await SetCardToBitmap(cards[2], _computerC3);
                await SetCardToBitmap(cards[3], _computerC4); 
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception Caught! {0}", e.Message);

            }
            
        }

        public async Task SetCribHand(List<CardView> cards, PlayerType dealer)
        {
            try
            {
                await SetCardToBitmap(cards[0], _cribC1);
                await SetCardToBitmap(cards[1], _cribC2);
                await SetCardToBitmap(cards[2], _cribC3);
                await SetCardToBitmap(cards[3], _cribC4); 

                if (dealer == PlayerType.Player)
                    _txtCribCaption.Text = "Crib (Player):";
                else
                    _txtCribCaption.Text = "Crib (Computer):";

             //   _ccCrib.UpdateCardLayoutAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Excption Caught! {0}", e.Message);

            }
        }


        public async Task SetSharedCard(CardView card)
        {
            try
            {
                await SetCardToBitmap(card, _sharedC1); 
               
            }
            catch (Exception e)
            {
                Debug.WriteLine("Excption Caught! {0}", e.Message);

            }
        }

        public void SetCountScores(int playerScore, int computerScore)
        {
            try
            {
                _txtPlayerLable.Text += String.Format("Count Points:{0}", playerScore);
                _txtComputerLable.Text += String.Format("Count Points:{0}", computerScore);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Excption Caught! {0}", e.Message);

            }
        }


        public void SetPlayerHandScore(int playerScore)
        {
            try
            {
                _txtPlayerLable.Text += String.Format(" Hand Points:{0}", playerScore);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception Caught! {0}", e.Message);

            }

        }

        public void SetComputerHandScore(int computerScore)
        {
            try
            {
                _txtComputerLable.Text += String.Format(" Hand Points:{0}", computerScore);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Excption Caught! {0}", e.Message);

            }
        }

        public void SetCribScore(int score)
        {
            try
            {
                _txtCribCaption.Text += String.Format(" Score:{0}", score);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Excption Caught! {0}", e.Message);

            }
        }


    }
}
