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

    public class ChooseDealerEventArgs : EventArgs
    {
        public ChooseDealerEventArgs(bool playerDeal)
        {

            if (playerDeal)
                Dealer = PlayerType.Player;
            else
                Dealer = PlayerType.Computer;
        }
        public PlayerType Dealer { get; set; }
    }

    public delegate Task DealerChosenHandler(object sender, DealerChosenEventArgs e);

    public sealed partial class FlipToPickCardControl : UserControl
    {
        
        private Deck _deck = new Deck();

        public event DealerChosenHandler2 OnDealerChosen;

        public FlipToPickCardControl()
        {
            this.InitializeComponent();

        }

        private async Task Init()
        {

            await AddCardsToGrid();
        }

        private async Task AddCardsToGrid()
        {
            int row = 0;
            int col = 0;
            double width = _gridDeck.ActualWidth;
            double height = _gridDeck.ActualHeight;
            int i = 0;
            List<Task<object>> taskList = new List<Task<object>>();
            foreach (CardView card in _deck.Cards)
            {
                card.Reset();
                card.PointerPressed -= Card_PointerReleased;
                card.PointerPressed += Card_PointerPressed;
                card.CardWidth = width;
                card.CardHeight = height;
                card.Width = width;
                card.Height = height;
                card.HorizontalAlignment = HorizontalAlignment.Left;
                card.VerticalAlignment = VerticalAlignment.Top;
                LayoutRoot.Children.Add(card);
                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                Canvas.SetZIndex(card, 100 + i);
                _gridDeck.Items.Add(card);
            }

            await Task.WhenAll(taskList);

            _gridDeck.UpdateCardLayoutAsync(2000, true);

        }

        private void Card_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

         async void Card_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
           

            TurnOffMouseClick();

            CardView card = sender as CardView;

            //
            // TODO: Move cards into the right spot

            Random r = new Random();
            CardView compCard = null;
            while (true)
            {
                int index = r.Next(50);
                if (_deck.Cards[index].GetType() == typeof(CardView))
                {
                    compCard = _deck.Cards[index] as CardView;
                    if (compCard.Rank != card.Rank)
                        break;
                }
            }



            

            
            DealerChosenEventArgs args = new DealerChosenEventArgs(compCard.Rank < card.Rank);
            await OnDealerChosen(this, args);


            //if (pPlayerDeal)
            //    _txtNotification.Text = "You drew low card and get to deal!";

        }

        private void TurnOffMouseClick()
        {

            for (int i = 0; i < _deck.Cards.Count; i++)
            {
                _deck.Cards[i].PointerPressed -= Card_PointerPressed;

            }

        }
    }
}
