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
       

        private async Task AnimateAllCardsBackToDeck(double duration = Double.MaxValue)
        {
            CountControl.Hide();
            if (duration == Double.MaxValue)
                duration = MainPage.AnimationSpeeds.Medium;

            // flip the cards and then move them for a nice affect

            List<Task<object>> list = new List<Task<object>>();
            GridPlayer.FlipAllCards(CardOrientation.FaceDown, list);
            GridPlayer.MoveAllCardsToTarget(GridDeck, list, duration);

            GridCrib.FlipAllCards(CardOrientation.FaceDown, list);
            GridCrib.MoveAllCardsToTarget(GridDeck, list, duration);

            GridPlayedCards.FlipAllCards(CardOrientation.FaceDown, list);
            GridPlayedCards.MoveAllCardsToTarget(GridDeck, list, duration);

            GridComputer.FlipAllCards(CardOrientation.FaceDown, list);
            GridComputer.MoveAllCardsToTarget(GridDeck, list, duration);

            foreach (CardView card in GridDeck.Items)
            {
                card.Reset();
            }

            GridDeck.UpdateCardLayout(list, duration, false);

            await Task.WhenAll(list);


        }
        private void ScatterCards(List<CardView> cards, double animationTime, List<Task<object>> taskList)
        {

            if (cards.Count == 0)
            {
                Debug.Assert(false, "Called ScatterCards before AddCardsToGrid");
                return;
            }

            Rect rect = Window.Current.Bounds;
            Rect scatterBounds = ViewCallback.ScatterBounds();
            double width = rect.Width - cards[0].ActualWidth;
            double height = rect.Height - cards[0].ActualHeight;
            Random rand = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            Point ptRand;
            foreach (CardView card in cards)
            {
                do
                {
                    ptRand = new Point(rand.NextDouble() * width, rand.NextDouble() * height);

                } while (!scatterBounds.Contains(ptRand));

                card.AnimateToTaskList(ptRand, true, animationTime, taskList);

            }
        }
        private async Task DealAnimation(HandsFromServer hfs, double duration = Double.MaxValue)
        {
            if (duration == Double.MaxValue)
                duration = MainPage.AnimationSpeeds.Medium;
            CardView card;
            List<Task<Object>> tasks = new List<Task<object>>();
            for (int i = 0; i < 6; i++)
            {

                card = hfs.PlayerCards[i];
                card.BoostZindex();
                card.Owner = Owner.Player;
                GridDeck.MoveCardToTarget(card, GridPlayer, tasks, duration, true);
                card = hfs.ComputerCards[i];
                card.Owner = Owner.Computer;
                card.BoostZindex();
                GridDeck.MoveCardToTarget(card, GridComputer, tasks, duration, true);

            }

            await Task.WhenAll(tasks);
            await GridPlayer.FlipAllCards(CardOrientation.FaceUp, duration);

            for (int i = 0; i < 6; i++)
            {
                GridPlayer.Items[i].ResetZIndex();
                GridComputer.Items[i].ResetZIndex();
            }

            hfs.SharedCard.BoostZindex(ZIndexBoost.SmallBoost);

        }
        private async Task AnimateSelectComputerCribCards(List<CardView> cards)
        {
            if (GridComputer.Items.Count == 0) return;
            List<Task<Object>> tasks = new List<Task<object>>();
            GridComputer.MoveCardToTarget(cards[1], GridPlayedCards, tasks);
            GridComputer.MoveCardToTarget(cards[0], GridPlayedCards, tasks);
            await Task.WhenAll(tasks);
        }
        public async Task OnAnimateMoveCardsToCrib()
        {
            await GridPlayedCards.FlipAllCards(CardOrientation.FaceDown);
            await GridPlayedCards.MoveAllCardsToTarget(GridCrib, MoveCardOptions.MoveAllAtSameTime);
            ShowCountControl();
        }

        public void ShowCountControl()
        {
            CountControl.Show();
            CountControl.Locate();
        }
        private async Task AnimateCardsBackToOwner()
        {
            List<Task<object>> taskList = new List<Task<object>>();
            CardView card = null;
            for (int i = GridPlayedCards.Items.Count - 1; i >= 0; i--)
            {
                card = GridPlayedCards.Items[i];
                if (card.Owner == Owner.Player)
                {
                    GridPlayedCards.MoveCardToTarget(card, GridPlayer, taskList);
                }
                else if (card.Owner == Owner.Computer)
                {
                    GridPlayedCards.MoveCardToTarget(card, GridComputer, taskList);
                }
                if (card.Orientation == CardOrientation.FaceDown)
                {
                    card.SetOrientation(CardOrientation.FaceUp, taskList);
                }
                if (card.AnimatedOpacity != 1.0)
                {
                    card.AnimateFade(1.0, taskList);
                }

            }

            await Task.WhenAll(taskList);

            CountControl.Hide();
        }
        private async Task AnimateCribCardsToOwner(PlayerType owner)
        {

            CardContainer cribOwner = GridPlayer;
            if (owner == PlayerType.Computer) cribOwner = GridComputer;

            List<Task<Object>> tasks = new List<Task<object>>();
            //
            //  return player and computer cards to the deck

            GridPlayer.FlipAllCards(CardOrientation.FaceDown, tasks);
            GridPlayer.MoveAllCardsToTarget(GridDeck, tasks, MainPage.AnimationSpeeds.Medium, true);
            GridComputer.FlipAllCards(CardOrientation.FaceDown, tasks);
            GridComputer.MoveAllCardsToTarget(GridDeck, tasks, MainPage.AnimationSpeeds.Medium, true);


            //
            //  move crib cards back to player
            GridCrib.MoveAllCardsToTarget(cribOwner, tasks, MainPage.AnimationSpeeds.Medium, true);
            cribOwner.FlipAllCards(CardOrientation.FaceUp, tasks);
            await Task.WhenAll(tasks);

        }   
       
    }
}
