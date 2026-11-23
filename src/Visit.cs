using System;
using System.Collections.Generic;

namespace WeeklyHermit
{
    internal static class Schedule
    {
        internal static bool IsRepeat(int day, int interval, bool challenge)
        {
            return !challenge && interval > 0 && day > 2 && (day - 2) % interval == 0;
        }
    }

    // The merchant asks for one candidate at a time. Deal without replacement
    // so even a small pool yields every available card in bounded time.
    internal sealed class OfferDeck<T> where T : class
    {
        private readonly Random random;
        private List<T> cards;
        private int next;

        internal OfferDeck(int runSeed, int day, List<T> candidates)
        {
            random = new Random(unchecked(runSeed * 397 ^ day));
            Shuffle(candidates);
        }

        internal void Shuffle(List<T> candidates)
        {
            cards = candidates;
            next = 0;
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T card = cards[i];
                cards[i] = cards[j];
                cards[j] = card;
            }
        }

        internal T Draw() => next < cards.Count ? cards[next++] : null;
    }
}
