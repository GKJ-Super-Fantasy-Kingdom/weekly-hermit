using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.TitleScreen;
using SuperFantasyKingdom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace WeeklyHermit.Tests
{
    [BepInPlugin("gkj.test.weeklyhermit.offers", "Weekly Hermit offer checks", "1.0.0")]
    internal sealed class OfferChecks : BaseUnityPlugin
    {
        private string trace;
        private Gamepad pad;
        private static readonly Dictionary<string, int> prices = new Dictionary<string, int>();
        private static bool priced;
        private static bool missed;

        private void Awake()
        {
            var output = Environment.GetEnvironmentVariable("HERMIT_AUDIT_OUTPUT");
            if (string.IsNullOrEmpty(output) || Application.productName != "Hermit03 Disposable 30956" ||
                Application.companyName != "SFK Mod Compatibility Tests") return;
            trace = Path.Combine(output, "live.txt");
            var hooks = new Harmony("gkj.test.weeklyhermit.fixture");
            hooks.Patch(AccessTools.Method(typeof(AchievementManager), "CheckAchievements"),
                prefix: new HarmonyMethod(typeof(OfferChecks), nameof(SkipAchievements)));
            hooks.Patch(AccessTools.Method(typeof(GuestHermit), "GetPrice"),
                postfix: new HarmonyMethod(typeof(OfferChecks), nameof(Price)));
            hooks.Patch(AccessTools.Method(typeof(UIManager), nameof(UIManager.IsWorldInteractable)),
                postfix: new HarmonyMethod(typeof(OfferChecks), nameof(HoldMap)));
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            StartCoroutine(Run());
        }

        private static bool SkipAchievements() => false;
        private static void HoldMap(ref bool __result) { if (missed) __result = false; }

        private static void Price(Unit unit, ref ResourceAmount[] __result)
        {
            if (!priced) return;
            string id = unit.GetEntityIdentifier();
            if (!prices.TryGetValue(id, out int cost))
                prices[id] = cost = new[] { 0, 1, 99999 }[prices.Count % 3];
            __result = cost == 0 ? null : new[] { new ResourceAmount(ResourceType.Coins, cost) };
        }

        private void Log(string s) => File.AppendAllText(trace, s + Environment.NewLine);

        private void Check(bool ok, string description)
        {
            if (!ok) throw new InvalidOperationException(description);
            Log("PASS " + description);
        }

        private IEnumerator Run()
        {
            // Unity starts nested iterators separately. Capture their failures in the same report.
            var stack = new Stack<IEnumerator>();
            stack.Push(Test());
            while (stack.Count > 0)
            {
                bool next;
                object value = null;
                try
                {
                    next = stack.Peek().MoveNext();
                    if (next) value = stack.Peek().Current;
                }
                catch (Exception e) { Log("FAIL " + e); break; }
                if (!next) stack.Pop();
                else if (value is IEnumerator nested) stack.Push(nested);
                else yield return value;
            }
            Log("END");
            Application.Quit();
        }

        private static IEnumerator Wait(Func<bool> ready, string name)
        {
            float until = Time.realtimeSinceStartup + 55;
            while (!ready())
            {
                if (Time.realtimeSinceStartup > until) throw new TimeoutException(name);
                yield return null;
            }
        }

        private static WaitForSecondsRealtime Delay(float seconds) => new WaitForSecondsRealtime(seconds);
        private static string Id(UICardUnit card) =>
            Traverse.Create(card).Field("m_Unit").GetValue<UnitBase>().GetEntityIdentifier();

        private IEnumerator Dawn(int nextDay)
        {
            var old = GameDataManager.Instance;
            DaytimeManager.Instance.SetDay(nextDay);
            GameManager.Instance.Save();
            GameManager.Instance.ResetScene();
            yield return Wait(() => GameDataManager.Instance != old && GameManager.Instance &&
                GameManager.Instance.IsGameRunning() && DaytimeManager.Instance.CurrentDay() == nextDay,
                "loading day " + nextDay);
            Log("Loaded dawn " + nextDay);
        }

        private IEnumerator WaitForOffer()
        {
            var until = Time.realtimeSinceStartup + 55;
            float nextStatus = Time.realtimeSinceStartup + 8;
            while (true)
            {
                var overlays = OverlayManager.Instance;
                var merchant = overlays.GetMerchant();
                if (overlays.IsOpen() && merchant.GetComponentsInChildren<UICardUnit>().Length > 0) break;
                if (Time.realtimeSinceStartup > until) throw new TimeoutException("Hermit recruitment");
                var dialogueBox = Traverse.Create(DialogueManager.Instance).Field("dialogueBox").GetValue<DialogueBox>();
                if (Time.realtimeSinceStartup > nextStatus)
                {
                    Log("Waiting for offer, guest=" + WorldManager.Instance.GetGuest()?.name +
                        ", forced pause=" + GameManager.Instance.IsForcedPause() + ", cutscene=" + GameCamera.Instance.IsCutscene() +
                        ", minimized=" + overlays.HasMinimizedOverlay() + ", overlay=" + overlays.IsOpen() +
                        ", dialogue box=" + dialogueBox.gameObject.activeInHierarchy + ", controls=" +
                        string.Join("|", FindObjectsOfType<Button>().Where(b => b.isActiveAndEnabled && b.IsInteractable())
                            .Select(b => b.GetComponentInChildren<TMP_Text>()?.text).Where(s => !string.IsNullOrEmpty(s)).Take(24)));
                    nextStatus += 8;
                }
                var dismiss = FindObjectsOfType<TMP_Text>().FirstOrDefault(t => t.text == "Later" ||
                    string.Equals(t.text, "Leave Caravan", StringComparison.OrdinalIgnoreCase))?.GetComponentInParent<Button>();
                if (dismiss && dismiss.IsInteractable())
                {
                    Log("Selected " + dismiss.GetComponentInChildren<TMP_Text>()?.text);
                    dismiss.onClick.Invoke();
                }
                else if (DialogueManager.Instance.IsInDialogue()) DialogueManager.Instance.Skip();
                else if (dialogueBox.gameObject.activeInHierarchy && pad != null && !overlays.IsOpen())
                    yield return Press(GamepadButton.South);
                else if (overlays.IsOpen() && merchant.isActiveAndEnabled)
                {
                    var decline = merchant.transform.Find("Panel/Buttons/Cancel/Button")?.GetComponent<Button>();
                    if (decline && decline.isActiveAndEnabled && decline.IsInteractable())
                    {
                        Log("Resolved earlier merchant: " + string.Join("|",
                            merchant.GetComponentsInChildren<TMP_Text>().Select(t => t.text).Take(6)));
                        decline.onClick.Invoke();
                    }
                }
                yield return Delay(.15f);
            }
            yield return Delay(1.4f);
        }

        private IEnumerator Press(GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return Delay(.12f);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return Delay(.5f);
        }

        private IEnumerator Test()
        {
            yield return Wait(() => TitleScreenHeroSelectionManager.Instance && SaveManager.Instance, "title");
            yield return Delay(1);
            Log(string.Join(", ", Chainloader.PluginInfos.Values.Select(p => p.Metadata.Name + " " + p.Metadata.Version)));
            var charter = Chainloader.PluginInfos["gkj.sfk.kingdomcharters"].Instance;
            var pending = AccessTools.Field(charter.GetType(), "Pending");
            pending.SetValue(charter, Enum.Parse(pending.FieldType, "Forester"));
            SaveManager.Instance.Delete("human_game", true);
            MainManager.Instance.SetHero("Velka1b");
            MainManager.Instance.SetHeroItems(new List<string>());
            MainManager.Instance.SetCurse(0);
            RaceDataManager.Instance.StartNewRun();
            MainManager.Instance.ContinueGame();
            yield return Wait(() => GameManager.Instance && GameManager.Instance.IsGameRunning(), "day 1");

            yield return Dawn(2);
            var hermit = WorldManager.Instance.GetGuest() as GuestHermit;
            Check(hermit, "native Day 2 arrival");
            var unlocked = UnlockedUnitsManager.Instance.GetUnlocked();
            var original = unlocked.ToArray();
            var complete = AccessTools.DeclaredMethod(typeof(GuestHermit), "OnDialogOver");
            try
            {
                unlocked.Clear();
                complete.Invoke(hermit, new object[] { false });
                Check(!OverlayManager.Instance.IsOpen(), "empty Day 2 roster returns without opening recruitment");
                unlocked.Add("WeeklyHermitFixtureRemovedUnit");
                complete.Invoke(hermit, new object[] { false });
                Check(!OverlayManager.Instance.IsOpen(), "all-stale Day 2 roster returns without opening recruitment");
            }
            finally { unlocked.Clear(); unlocked.AddRange(original); }
            yield return WaitForOffer();
            var day2 = OverlayManager.Instance.GetMerchant().GetComponentsInChildren<UICardUnit>();
            Check(day2.Length > 0, "ordinary Day 2 conversation opens native offer");
            string recruit = Id(day2[0]);
            day2[0].GetCardButton().onClick.Invoke();
            yield return Wait(() => GridManager.Instance.IsInNewUnitMode(), "Day 2 placement");
            GridManager.Instance.CancelNewUnitMode();
            Check(UnitManager.Instance.GetUnit(recruit), "ordinary Day 2 recruit placed");

            priced = true;
            yield return Dawn(9);
            ResourceManager.Instance.AddResource(ResourceType.Coins, 10);
            ResourceManager.Instance.AddResource(ResourceType.Faith, 30);
            yield return WaitForOffer();
            var screen = OverlayManager.Instance.GetMerchant();
            var cards = screen.GetComponentsInChildren<UICardUnit>();
            Check(cards.Length == 3, "three mixed-price recruits offered after earlier visitor");
            var input = GamepadManager.Instance.GetPlayerInput();
            input.neverAutoSwitchControlSchemes = true;
            pad = InputSystem.AddDevice<Gamepad>();
            input.SwitchCurrentControlScheme("Gamepad", pad);
            yield return Delay(.3f);
            var cancel = screen.transform.Find("Panel/Buttons/Cancel/Button").gameObject;
            var buys = screen.transform.Find("Panel/BuyButtons").GetComponentsInChildren<Button>();
            var availableBuy = buys.Single(b => b.IsInteractable());
            foreach (var card in cards)
            {
                int cost = prices[Id(card)];
                var inspect = card.transform.Find("Options/Details").GetComponentInChildren<Button>();
                GamepadManager.Instance.SetGameObjectToSelect(inspect.gameObject, true);
                yield return Press(GamepadButton.South);
                Check(!screen.gameObject.activeInHierarchy && OverlayManager.Instance.IsOpen(), "Inspect opened, price " + cost);
                yield return Press(GamepadButton.East);
                var expected = cost == 0 ? card.GetCardButton().gameObject : cost == 1 ? availableBuy.gameObject : cancel;
                Check(screen.gameObject.activeInHierarchy && EventSystem.current.currentSelectedGameObject == expected,
                    "Back restored actionable focus, price " + cost);
            }

            var reroll = screen.transform.Find("Panel/Buttons/Reroll/Button").GetComponent<Button>();
            Check(reroll.IsInteractable(), "full pool allows affordable reroll");
            var keep = new HashSet<string>(cards.Select(Id));
            var unlocks = UnlockedUnitsManager.Instance;
            var previousBans = new List<string>(unlocks.GetBannedUnits());
            foreach (var key in unlocks.GetUnlocked())
                if (!keep.Contains(key)) unlocks.AddBannedUnit(key);
            reroll.onClick.Invoke();
            yield return Delay(1.5f);
            Check(!reroll.IsInteractable() && !reroll.GetComponent<CanvasGroup>().blocksRaycasts,
                "depleted reroll stays disabled for navigation and pointer");
            var refreshed = screen.GetComponentsInChildren<UICardUnit>();
            Check(keep.SetEquals(refreshed.Select(Id)), "reroll honors the current filtered pool");
            availableBuy = screen.transform.Find("Panel/BuyButtons").GetComponentsInChildren<Button>().Single(b => b.IsInteractable());
            int coins = ResourceManager.Instance.GetCoins();
            string paidId = prices.Single(pair => pair.Value == 1 && keep.Contains(pair.Key)).Key;
            GamepadManager.Instance.SetGameObjectToSelect(availableBuy.gameObject, true);
            yield return Press(GamepadButton.South);
            yield return Wait(() => GridManager.Instance.IsInNewUnitMode(), "paid recruitment placement");
            GridManager.Instance.CancelNewUnitMode();
            Check(UnitManager.Instance.GetUnit(paidId) && ResourceManager.Instance.GetCoins() == coins - 1,
                "focused Buy recruited the priced unit and charged exactly once");

            priced = false;
            unlocks.SetBannedUnits(previousBans);
            RaceDataManager.Instance.AddRaceEvent(RaceEvent.GateOpened);
            RaceDataManager.Instance.AddRaceEvent(RaceEvent.HermitInQuestion);
            yield return Dawn(16);
            yield return WaitForOffer();
            var day16 = OverlayManager.Instance.GetMerchant();
            day16.transform.Find("Panel/Buttons/Cancel/Button").GetComponent<Button>().onClick.Invoke();
            yield return Delay(.5f);
            var calendarState = Chainloader.PluginInfos["sfk.visitorcalendar"].Instance.GetType().Assembly
                .GetType("VisitorCalendar.VisitState");
            var arrivals = (HashSet<Actors>)AccessTools.Field(calendarState, "Arrived").GetValue(null);
            Check(arrivals.Contains(Actors.Hermit) &&
                !(bool)AccessTools.Method(calendarState, "IsClaimed").Invoke(null, new object[] { Actors.Hermit }),
                "Visitor Calendar recorded the declined Day 16 Hermit as arrived and unclaimed");

            missed = true;
            yield return Dawn(23);
            yield return Delay(1);
            var plugin = Chainloader.PluginInfos["gkj.superfantasykingdom.weeklyhermit"].Instance.GetType();
            var retained = AccessTools.Field(plugin, "retainedGuest").GetValue(null) as GameObject;
            var queueScene = AccessTools.Field(plugin, "queuedScene").GetValue(null) as GameManager;
            Log("Day 23 retained=" + (retained ? retained.name : "none") + ", current scene=" +
                (queueScene == GameManager.Instance) + ", world interactable=" + UIManager.Instance.IsWorldInteractable() +
                ", resolved day=" + AccessTools.Field(plugin.Assembly.GetType("WeeklyHermit.Checkpoint"), "resolvedDay").GetValue(null));
            Check(retained && !retained.activeSelf && queueScene == GameManager.Instance &&
                !UIManager.Instance.IsWorldInteractable() && !WeeklyOfferOpen(),
                "busy map holds the Day 23 offer");
            yield return Dawn(24);
            missed = false;
            yield return Delay(1);
            Check(!WeeklyOfferOpen(),
                "missed Day 23 offer did not carry to Day 24");
        }

        private static bool WeeklyOfferOpen()
        {
            var overlays = OverlayManager.Instance;
            var merchant = overlays.GetMerchant();
            if (!overlays.IsOpen() || !merchant.isActiveAndEnabled) return false;
            var callback = Traverse.Create(merchant).Field("m_Callback").GetValue<Delegate>();
            return callback?.Method.DeclaringType?.Assembly ==
                Chainloader.PluginInfos["gkj.superfantasykingdom.weeklyhermit"].Instance.GetType().Assembly;
        }
    }
}
