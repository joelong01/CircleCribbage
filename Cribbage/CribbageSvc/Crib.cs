using Cribbage;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CribbageService
{
    public class Crib : Hand
    {

        public Crib() {  }
        public Crib (List<CardView> cards)
        {
            _cards = cards;
        }
       
    }
}