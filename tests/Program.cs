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

var modUnit = new Recruit("ModKnight", 80);
var rebalanced = new Recruit("Mage", 51);
var catalog = new Dictionary<string, Recruit>
{
    ["ModKnight"] = modUnit, ["Mage"] = rebalanced,
    ["OldMage"] = new Recruit("Mage", 1), ["Jailed"] = new Recruit("Jailed", 10),
    ["BrokenAlias"] = new Recruit("MissingCanonical", 12)
};
var unlocked = new List<string> { null, "", "Removed", "BrokenAlias", "OldMage", "Mage", "ModKnight", "Jailed" };
var banned = new HashSet<string> { "Jailed" };
var owned = new HashSet<string> { "Mage" };
bool hermitAllowsOwned = false, merchantAllowsRank = false;
List<Recruit> Pool() => RecruitmentPool.Read(unlocked, banned, key => catalog.GetValueOrDefault(key),
    unit => unit.Id, key => hermitAllowsOwned || !owned.Contains(key),
    unit => !owned.Contains(unit.Id) || merchantAllowsRank);
Check(Pool().SequenceEqual(new[] { modUnit }), "New registered foreign unit is eligible; native exclusivity filters owned unit even through an alias");
hermitAllowsOwned = true;
Check(Pool().SequenceEqual(new[] { modUnit }), "Merchant can veto an owned rank-up even when the Hermit allows it");
merchantAllowsRank = true;
Check(Pool().SequenceEqual(new[] { rebalanced, modUnit }), "Both policies allow rank-up; the canonical rebalanced prefab is offered once");
banned.Add("OldMage");
Check(Pool().SequenceEqual(new[] { modUnit }), "A ban saved under the legacy ID excludes the canonical ID too");
banned.Remove("OldMage");
var forward = new OfferDeck<Recruit>(1234, 16, Pool());
unlocked.Remove("ModKnight");
unlocked.Insert(0, "ModKnight");
var backward = new OfferDeck<Recruit>(1234, 16, Pool());
Check(Enumerable.Range(0, 2).All(i => forward.Draw() == backward.Draw()),
    "Reload reproduces the draw after changing registration order");
banned.Add("Mage");
Check(Pool().SequenceEqual(new[] { modUnit }), "Banish applies to canonical ID and its legacy unlock key");
var pending = new OfferDeck<Recruit>(23, 16, Pool());
Check(pending.Draw() == modUnit, "Pre-reload card exists");
catalog.Remove("ModKnight");
Check(new OfferDeck<Recruit>(23, 16, Pool()).Draw() == null,
    "Reload after content removal drops the offered unit and safely skips an empty pool");
catalog["ModKnight"] = new Recruit("ModKnight", 91);
Check(Pool().Single().Power == 91, "Reload uses current stats after same-ID replacement");

var handoff = new MapHandoff();
Check(!handoff.CanOpen(true, true, false, false) && !handoff.CanOpen(true, false, true, false)
    && !handoff.CanOpen(true, false, false, true), "Caravan guest, open screen and minimized screen each hold the Hermit");
Check(!handoff.CanOpen(true, false, false, false) && !handoff.CanOpen(false, false, false, false)
    && !handoff.CanOpen(true, false, false, false) && handoff.CanOpen(true, false, false, false),
    "A dialogue interrupts the handoff; opening requires two consecutive available frames");

record Recruit(string Id, int Power);
