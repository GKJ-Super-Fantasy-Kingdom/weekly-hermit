using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace WeeklyHermit
{
    [BepInPlugin(PluginId, "Weekly Hermit", "0.1.2")]
    public sealed class WeeklyHermitPlugin : BaseUnityPlugin
    {
        private const string PluginId = "gkj.superfantasykingdom.weeklyhermit";
        private Harmony harmony;
        private static ConfigEntry<int> interval;
        private static GameManager queuedScene;
        private static GameObject retainedGuest;

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
            retainedGuest = null;
            inspectedCard = null;
        }

        [HarmonyPatch(typeof(GuestHermit), "OnLoad")]
        private static class HermitLoad
        {
            // Only intercept the native branch that discards an unscheduled Hermit.
            // A modded native visit still follows its own OnLoad and takes precedence.
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Action<UnityEngine.Object> destroy = UnityEngine.Object.Destroy;
                Action<UnityEngine.Object> release = ReleaseOrQueue;
                return Transpilers.MethodReplacer(instructions, destroy.Method, release.Method);
            }

            private static void ReleaseOrQueue(UnityEngine.Object target)
            {
                int day = DaytimeManager.Instance.CurrentDay();
                var scene = GameManager.Instance;
                if (scene == queuedScene && target == retainedGuest)
                    return;
                if (target is GameObject obj && Schedule.IsRepeat(day, interval.Value, MainManager.Instance.IsChallenge())
                    && !Checkpoint.Resolved && scene != queuedScene && obj.TryGetComponent<GuestHermit>(out var hermit))
                {
                    queuedScene = scene;
                    retainedGuest = obj;
                    // Keep the original component alive for patched eligibility and price
                    // methods, without registering a second arrival in the world guest slot.
                    obj.SetActive(false);
                    scene.StartCoroutine(WaitForMap(scene, day, hermit));
                    return;
                }
                UnityEngine.Object.Destroy(target);
            }
        }

        // Keep the native arrival, dialogue completion and encounter screen. Only skip
        // the offer when the native picker would have nothing it could recruit.
        [HarmonyPatch(typeof(GuestHermit), "OnDialogOver")]
        private static class NativeEncounter
        {
            private static bool Prefix(GuestHermit __instance, bool b)
            {
                if (EligibleUnits(__instance).Count != 0) return true;
                NativeDialogue.Complete(__instance, b);
                return false;
            }
        }

        [HarmonyPatch]
        private static class NativeDialogue
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(Guest), "OnDialogOver")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void Complete(Guest instance, bool b) =>
                throw new NotSupportedException("Reverse patch was not installed.");
        }

        // Validate each native draw batch, including rerolls, before entering
        // GetUnit's rejection loop. Keep the game's original picker and RNG.
        [HarmonyPatch(typeof(UIOverlayMerchant), "GenerateUnits")]
        private static class NativeDraw
        {
            internal static HashSet<string> Allowed;
            private static bool Prefix(Func<Unit> ___m_GetUnitCallback, bool reroll, out HashSet<string> __state)
            {
                __state = Allowed;
                Allowed = null;
                if (!reroll || !(___m_GetUnitCallback?.Target is GuestHermit hermit)) return true;
                var eligible = EligibleUnits(hermit);
                if (eligible.Count == 0)
                {
                    OverlayManager.Instance.CloseOverlay();
                    return false;
                }
                Allowed = new HashSet<string>(eligible.ConvertAll(unit => unit.GetEntityIdentifier()),
                    StringComparer.Ordinal);
                return true;
            }

            private static void Finalizer(HashSet<string> __state) => Allowed = __state;
        }

        [HarmonyPatch(typeof(GuestHermit), "CheckUnit")]
        private static class NativeEligibility
        {
            private static bool Prefix(string __0)
            {
                if (string.IsNullOrWhiteSpace(__0)) return false;
                if (NativeDraw.Allowed == null) return true;
                var unit = AddressablesManager.Instance.GetUnit(__0);
                return unit && NativeDraw.Allowed.Contains(unit.GetEntityIdentifier());
            }
        }

        private static readonly AccessTools.FieldRef<GuestHermit, LocalizedString> Title =
            AccessTools.FieldRefAccess<GuestHermit, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<GuestHermit, LocalizedString> Description =
            AccessTools.FieldRefAccess<GuestHermit, LocalizedString>("stringDesc");
        private static readonly AccessTools.FieldRef<GuestHermit, LocalizedString> Decline =
            AccessTools.FieldRefAccess<GuestHermit, LocalizedString>("stringCancel");

        private static readonly AccessTools.FieldRef<DialogueManager, DialogueBox> ActiveBox =
            AccessTools.FieldRefAccess<DialogueManager, DialogueBox>("dialogueBox");

        private static IEnumerator WaitForMap(GameManager scene, int day, GuestHermit hermit)
        {
            var handoff = new MapHandoff();
            var afterUpdates = new WaitForEndOfFrame();
            while (true)
            {
                // A timed visitor notification may become due this same frame. Let
                // Update and ordinary coroutines present it before claiming the screen.
                yield return afterUpdates;
                if (!hermit || scene.IsUnloading() || scene.IsGameOver() || DaytimeManager.Instance.CurrentDay() != day)
                    yield break;

                // Some visitors show the native dialogue box directly without starting
                // a managed Dialogue. Its parent stays active until the player dismisses it.
                var dialogue = DialogueManager.Instance;
                var box = ActiveBox(dialogue);
                bool mapAvailable = scene.IsGameRunning() && !scene.IsForcedPause()
                    && !GameCamera.Instance.IsCutscene() && !dialogue.IsInDialogue() && !(box && box.gameObject.activeInHierarchy)
                    && UIManager.Instance.IsWorldInteractable();
                var overlays = OverlayManager.Instance;
                if (handoff.CanOpen(mapAvailable, WorldManager.Instance.GetGuest() != null,
                        overlays.IsOpen(), overlays.HasMinimizedOverlay()))
                    break;
            }

            var getPrice = AccessTools.MethodDelegate<Func<Unit, ResourceAmount[]>>(
                AccessTools.DeclaredMethod(typeof(GuestHermit), "GetPrice"), hermit);
            var eligible = EligibleUnits(hermit);
            if (eligible.Count == 0)
                yield break;

            var deck = new OfferDeck<Unit>(scene.GetSeed(), day, eligible);
            var screen = OverlayManager.Instance.GetMerchantScreen();
            screen.Init(Title(hermit).GetLocalizedString());
            screen.SetActor(hermit.GetDialogueCollection().GetActor());
            screen.SetText(Description(hermit).GetLocalizedString());
            screen.SetUseCardSelectCallback(true);
            screen.SetCallback(ResolveRecruitment);
            screen.CanBan();
            screen.Units(deck.Draw, getPrice);
            screen.AllowReroll(cbBefore: merchant =>
            {
                var current = EligibleUnits(hermit);
                deck.Shuffle(current);
                merchant.SetPossibleItemAmount(current.Count);
            });
            screen.SetPossibleItemAmount(eligible.Count);
            screen.SetCancel(Decline(hermit).GetLocalizedString(),
                merchant =>
                {
                    RaceDataManager.Instance.AddRaceEvent(RaceEvent.HermitRejected);
                    Checkpoint.Resolve();
                });
            screen.Display();
            var spacing = screen.GetComponent<RecruitCardSpacing>() ?? screen.gameObject.AddComponent<RecruitCardSpacing>();
            spacing.Bind(Keywords(screen));
        }

        private static readonly AccessTools.FieldRef<UIOverlayMerchant, UIEffectKeywordDictionary> Keywords =
            AccessTools.FieldRefAccess<UIOverlayMerchant, UIEffectKeywordDictionary>("keywordDictionary");

        [HarmonyPatch(typeof(UIOverlayMerchant), nameof(UIOverlayMerchant.Init))]
        private static class ResetRecruitSpacing
        {
            private static void Prefix(UIOverlayMerchant __instance)
            {
                var spacing = __instance.GetComponent<RecruitCardSpacing>();
                if (spacing) spacing.enabled = false;
            }
        }

        private static void ResolveRecruitment(int id, ResourceAmount[] costs) => Checkpoint.Resolve();

        [HarmonyPatch(typeof(UIOverlayMerchant), nameof(UIOverlayMerchant.AddUnit))]
        private static class CompareRecruits
        {
            private static void Postfix(UIOverlayMerchant __instance, UnitBase unit, GameObject __result)
            {
                if (__result && MerchantCallback(__instance) == ResolveRecruitment)
                    RecruitCardPresentation.Apply(__result.GetComponent<UICardUnit>(), unit);
            }
        }

        private static readonly AccessTools.FieldRef<UIOverlayMerchant, Action<int, ResourceAmount[]>> MerchantCallback =
            AccessTools.FieldRefAccess<UIOverlayMerchant, Action<int, ResourceAmount[]>>("m_Callback");
        private static UICardUnit inspectedCard;

        [HarmonyPatch(typeof(UIOverlayMerchant), "Reroll", typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
        private static class FinishReroll
        {
            // The native final affordability refresh can re-enable a depleted deck.
            private static void Postfix(UIOverlayMerchant __instance, int ___m_PossibleItemAmount)
            {
                if (MerchantCallback(__instance) == ResolveRecruitment)
                    __instance.SetPossibleItemAmount(___m_PossibleItemAmount);
            }
        }

        [HarmonyPatch(typeof(UIOverlayMerchant), "AddBuyButton")]
        private static class BindPurchase
        {
            private static void Postfix(UIOverlayMerchant __instance, UICard spawned, Transform __result)
            {
                if (spawned is UICardUnit && MerchantCallback(__instance) == ResolveRecruitment)
                    spawned.gameObject.AddComponent<RecruitPurchase>().Button = __result.GetComponent<Button>();
            }
        }

        [HarmonyPatch(typeof(UICardUnit), nameof(UICardUnit.Details))]
        private static class InspectRecruit
        {
            private static void Postfix(UICardUnit __instance)
            {
                var screen = __instance.GetComponentInParent<UIOverlayMerchant>(true);
                if (screen && !__instance.gameObject.activeInHierarchy && MerchantCallback(screen) == ResolveRecruitment)
                {
                    inspectedCard = __instance;
                    var overlays = OverlayManager.Instance;
                    var details = overlays.GetOverlaySelection().GetComponentInChildren<UIUnitDetails>();
                    if (details)
                        details.gameObject.AddComponent<RecruitDetailsFooter>().Bind(overlays);
                }
            }
        }

        [HarmonyPatch(typeof(OverlayManager), "ShowBackup")]
        private static class ReturnToRecruit
        {
            private static void Postfix()
            {
                // Native details retain the original merchant and cards in the backup.
                if (inspectedCard && inspectedCard.gameObject.activeInHierarchy)
                {
                    var purchase = inspectedCard.GetComponent<RecruitPurchase>();
                    var target = purchase ? purchase.Button : inspectedCard.GetCardButton();
                    // ShowBackup already selected Decline if this unit is unaffordable.
                    if (target && target.isActiveAndEnabled && target.IsInteractable())
                        GamepadManager.Instance.SetGameObjectToSelect(target.gameObject, force: true);
                }
                inspectedCard = null;
            }
        }

        private static List<Unit> EligibleUnits(GuestHermit hermit)
        {
            var checkUnit = AccessTools.MethodDelegate<Func<string, bool>>(
                AccessTools.DeclaredMethod(typeof(GuestHermit), "CheckUnit"), hermit);
            var unlocks = UnlockedUnitsManager.Instance;
            var race = RaceManager.Instance.GetRace();
            return RecruitmentPool.Read(unlocks.GetUnlocked(), unlocks.GetBannedUnits(),
                key =>
                {
                    var unit = AddressablesManager.Instance.GetUnit(key);
                    return unit ? unit : null; // Normalize destroyed Unity objects for the pool.
                },
                unit => unit.GetEntityIdentifier(), checkUnit,
                unit => unit.GetRace() != race && unit.GetRace() != Races.Elemental
                    && UnitManager.Instance.CanAddUnit(unit));
        }
    }

    internal sealed class RecruitPurchase : MonoBehaviour
    {
        internal Button Button;
    }
}
