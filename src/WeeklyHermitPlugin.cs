using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.Localization;

namespace WeeklyHermit
{
    [BepInPlugin(PluginId, "Weekly Hermit", "0.1.0")]
    public sealed class WeeklyHermitPlugin : BaseUnityPlugin
    {
        private const string PluginId = "gkj.superfantasykingdom.weeklyhermit";
        private Harmony harmony;
        private static ConfigEntry<int> interval;
        private static GameManager queuedScene;

        private void Awake()
        {
            interval = Config.Bind("Schedule", "IntervalDays", 7,
                new ConfigDescription("Days between repeat visits after Day 2 in a standard run.",
                    new AcceptableValueRange<int>(1, 365)));
            Checkpoint.Log = Logger;
            harmony = new Harmony(PluginId);
            harmony.PatchAll(typeof(WeeklyHermitPlugin).Assembly);
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            queuedScene = null;
        }

        [HarmonyPatch(typeof(GuestHermit), "OnLoad")]
        private static class HermitLoad
        {
            private static void Prefix(GuestHermit __instance, int day, LocalizedString ___stringTitle,
                LocalizedString ___stringDesc, LocalizedString ___stringCancel)
            {
                if (!Schedule.IsRepeat(day, interval.Value, MainManager.Instance.IsChallenge()) || Checkpoint.Resolved)
                    return;

                // All guest OnLoad handlers run before the first frame. Let the
                // scheduled guest claim the world slot regardless of handler order.
                var scene = GameManager.Instance;
                if (scene != queuedScene)
                {
                    queuedScene = scene;
                    scene.StartCoroutine(WaitForMap(scene, day, ___stringTitle, ___stringDesc,
                        ___stringCancel, __instance.GetDialogueCollection().GetActor()));
                }
            }
        }

        private static IEnumerator WaitForMap(GameManager scene, int day, LocalizedString title,
            LocalizedString description, LocalizedString decline, Actor actor)
        {
            int readyFrames = 0;
            do
            {
                yield return null;
                if (scene.IsUnloading() || scene.IsGameOver() || DaytimeManager.Instance.CurrentDay() != day)
                    yield break;

                bool ready = scene.IsGameRunning() && !scene.IsForcedPause()
                    && WorldManager.Instance.GetGuest() == null
                    && !GameCamera.Instance.IsCutscene() && !DialogueManager.Instance.IsInDialogue()
                    && UIManager.Instance.IsWorldInteractable() && !OverlayManager.Instance.HasMinimizedOverlay();
                // Give the previous screen's confirm/back input a frame to clear.
                readyFrames = ready ? readyFrames + 1 : 0;
            } while (readyFrames < 2);

            var eligible = EligibleUnits();
            if (eligible.Count == 0)
                yield break;

            var deck = new OfferDeck<Unit>(scene.GetSeed(), day, eligible);
            var screen = OverlayManager.Instance.GetMerchantScreen();
            screen.Init(title.GetLocalizedString());
            screen.SetActor(actor);
            screen.SetText(description.GetLocalizedString());
            screen.SetUseCardSelectCallback(true);
            screen.SetCallback((id, costs) => Checkpoint.Resolved = true);
            screen.CanBan();
            screen.Units(deck.Draw, unit => null);
            screen.AllowReroll(cbBefore: merchant => deck.Shuffle(EligibleUnits()));
            screen.SetPossibleItemAmount(eligible.Count);
            screen.SetCancel(decline.GetLocalizedString(),
                merchant =>
                {
                    RaceDataManager.Instance.AddRaceEvent(RaceEvent.HermitRejected);
                    Checkpoint.Resolved = true;
                });
            screen.Display();
        }

        private static List<Unit> EligibleUnits()
        {
            var unlocks = UnlockedUnitsManager.Instance;
            var excluded = new HashSet<string>(unlocks.GetBannedUnits());
            var result = new List<Unit>();
            var race = RaceManager.Instance.GetRace();
            foreach (string id in unlocks.GetUnlocked())
            {
                if (!excluded.Add(id) || !UnitManager.Instance.CanAddUnitTypeExclusive(id))
                    continue;
                var unit = AddressablesManager.Instance.GetUnit(id);
                if (unit != null && unit.GetRace() != race && unit.GetRace() != Races.Elemental)
                    result.Add(unit);
            }
            return result;
        }
    }
}
