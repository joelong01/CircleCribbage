using CribbageService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cribbage
{
    public class VectorCard
    {
        private Canvas _canvas = null;
        private int _index = 0;
        private CardNames _cardName;

        public VectorCard(Canvas canvas, int index, CardNames name)
        {
            _canvas = canvas;
            _index = index;
            _cardName = name;
        }

        public Canvas Canvas { get { return _canvas; } }
        public int Index { get { return _index; } }
        public CardNames Name { get { return _cardName; } }

    }

 

    public sealed partial class VectorCards : UserControl
    {
        
        List<CardView> _cards = new List<CardView>();

        
        public VectorCards()
        {
            // Debug.WriteLine("Creating a set of VectorCards");
            this.InitializeComponent();
            

        }

        public  void Init()
        {
            for (int n = _grid.Children.Count - 4; n >= 0; n--)
            {
                VectorCard v = new VectorCard(((Canvas)_grid.Children[n]), n, (CardNames)n);
                _grid.Children.RemoveAt(n);
                Suit suit = ((Suit)(int)(n / 13));
                int rank = (int)(n % 13) + 1;
                int val = (rank < 10) ? rank : 10;
                CardData data = new CardData((CardNames)n, Owner.Shared, rank, val, n, suit);
                CardView view = new CardView(v, data, CardOrientation.FaceDown);
                _cards.Insert(0, view);

            }
        }

        public void ResetCards()
        {
            foreach (CardView card in _cards)
            {
                card.Reset();
                

            }

        }

        public CardView GetCardByName(CardNames card)
        {
            return GetCardByIndex((int)card);
                
        }

        public CardView GetCardByIndex(int index)
        {
            CardView card = _cards[index];           
            return card;

        }

        public List<CardView> Cards
        {
            get { return _cards; }

        }
        
    }
}
