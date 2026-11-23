using System;
using System.Collections.Generic;
using System.Linq;
using WeeklyHermit;

static void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    Console.WriteLine("PASS " + description);
}

var days = Enumerable.Range(1, 29);
Check(days.Where(day => Schedule.IsRepeat(day, 7, false)).SequenceEqual(new[] { 9, 16, 23 }),
    "Standard run leaves Day 2 native and adds Days 9, 16 and 23");
Check(days.All(day => !Schedule.IsRepeat(day, 7, true)), "Challenge uses its authored schedule");
Check(!Schedule.IsRepeat(2, 1, false) && Schedule.IsRepeat(3, 1, false)
    && Schedule.IsRepeat(12, 5, false), "Configured interval is anchored to Day 2");

static OfferDeck<string> Deck() => new(1234, 16, new List<string> { "A", "B", "C", "D", "E" });
var first = Deck();
var reload = Deck();
var offer = Enumerable.Range(0, 3).Select(_ => first.Draw()).ToArray();
Check(offer.Distinct().Count() == 3, "Offer has three distinct candidates");
Check(offer.SequenceEqual(Enumerable.Range(0, 3).Select(_ => reload.Draw())), "Same checkpoint reproduces the offer");
first.Shuffle(new List<string> { "B", "C" });
Check(new HashSet<string> { first.Draw(), first.Draw() }.SetEquals(new[] { "B", "C" })
    && first.Draw() == null, "Reduced pool cannot return an excluded candidate");
first.Shuffle(new List<string>());
Check(first.Draw() == null, "Empty pool terminates");
