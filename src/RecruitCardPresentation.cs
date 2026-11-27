using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeeklyHermit
{
    internal sealed class RecruitCardPresentation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly AccessTools.FieldRef<UICardUnit, TextMeshProUGUI> Description =
            AccessTools.FieldRefAccess<UICardUnit, TextMeshProUGUI>("description");
        private static readonly AccessTools.FieldRef<UICardUnit, Image> Frame =
            AccessTools.FieldRefAccess<UICardUnit, Image>("card");
        private static readonly AccessTools.FieldRef<UICardUnit, Sprite> SelectedFrame =
            AccessTools.FieldRefAccess<UICardUnit, Sprite>("spriteCardSelected");
        [SerializeField] private Outline outline;
        [SerializeField] private Button action;
        [SerializeField] private Image frame;
        [SerializeField] private Sprite selectedFrame;
        private Sprite previousFrame;
        private bool hovered;

        internal static void Apply(UICardUnit card, UnitBase unit)
        {
            if (!card || !unit) return;
            var description = Description(card);
            if (description && description.richText)
                FormatDescription(description, unit);

            // A priced unit has a separate Buy button. Keep the focus cue on its card
            // only while confirming the selected control would recruit that unit.
            var purchase = card.GetComponent<RecruitPurchase>();
            var action = purchase ? purchase.Button : card.GetCardButton();
            var frame = Frame(card);
            if (!action || !frame) return;
            BindOutline(action, frame, SelectedFrame(card));
        }

        private static void BindOutline(Button action, Image frame, Sprite selectedFrame)
        {
            var feedback = action.gameObject.AddComponent<RecruitCardPresentation>();
            feedback.action = action;
            feedback.frame = frame;
            feedback.selectedFrame = selectedFrame;
            feedback.outline = frame.gameObject.AddComponent<Outline>();
            feedback.outline.effectColor = new Color32(255, 246, 196, 255);
            feedback.outline.effectDistance = new Vector2(2f, -2f);
            feedback.RefreshOutline();
        }

        public void OnPointerEnter(PointerEventData data) => hovered = true;
        public void OnPointerExit(PointerEventData data) => hovered = false;
        private void LateUpdate() => RefreshOutline();

        private void RefreshOutline()
        {
            if (!outline || !action) return;
            bool selected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
            bool gamepad = GamepadManager.Instance && GamepadManager.Instance.IsGamepadInUse();
            bool showCue = action.isActiveAndEnabled && action.IsInteractable() && (gamepad ? selected : hovered);
            outline.enabled = showCue;
            // The same outline looks different on the native hover and selected sprites.
            // Use the game's selected frame for both actionable states.
            if (showCue && frame && selectedFrame && frame.sprite != selectedFrame)
            {
                previousFrame = frame.sprite;
                frame.sprite = selectedFrame;
            }
            if (!showCue) RestoreFrame();
        }

        private void OnDisable()
        {
            hovered = false;
            if (outline) outline.enabled = false;
            RestoreFrame();
        }

        private void RestoreFrame()
        {
            // Native focus or pointer events may already have supplied a new sprite.
            if (previousFrame && frame && frame.sprite == selectedFrame)
                frame.sprite = previousFrame;
            previousFrame = null;
        }

        private static void FormatDescription(TextMeshProUGUI description, UnitBase unit)
        {
            string role = Helper.Instance.GetRoleTitle(unit.GetRole());
            string attack = Helper.Instance.GetAttackTypeTitle(unit.GetAttackType());
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(attack)) return;

            // Preserve the quote and font size. Fall back to the text comparison if
            // the native sprites do not fit or are missing from this font's atlas.
            string quote = description.text;
            string roleIcon = KeywordIcon(unit.GetRole() switch
            {
                UnitRole.Damage => EffectKeywordType.Damage,
                UnitRole.Support => EffectKeywordType.Support,
                UnitRole.Tank => EffectKeywordType.Tank,
                _ => (EffectKeywordType?)null
            });
            string attackIcon = KeywordIcon(unit.GetAttackType() switch
            {
                UnitAttackType.Melee => EffectKeywordType.Melee,
                UnitAttackType.Ranged => EffectKeywordType.Ranged,
                UnitAttackType.Magic => EffectKeywordType.Mage,
                UnitAttackType.Summoner => EffectKeywordType.Summoner,
                _ => (EffectKeywordType?)null
            });
            if (roleIcon != "" && attackIcon != "")
            {
                string heading = $"<size=85%><color=#40223B><nobr>{roleIcon} {role}</nobr>  <nobr>{attackIcon} {attack}</nobr></color></size>";
                description.text = $"{heading}\n{quote}";
                description.ForceMeshUpdate(ignoreActiveState: true);
                // The prefab's textInfo can be uninitialized until the card is rendered.
                if (!description.isTextOverflowing)
                    return;
            }

            description.text = $"<color=#40223B>{role} / {attack}</color>\n{quote}";
            description.ForceMeshUpdate(ignoreActiveState: true);
            if (description.isTextOverflowing)
                description.text = quote;
        }

        private static string KeywordIcon(EffectKeywordType? keyword)
        {
            var dictionary = EffectKeywordDictionaryManager.Instance;
            var entry = dictionary && keyword.HasValue ? dictionary.Get(keyword.Value) : null;
            return entry && entry.icon?.StartsWith("<sprite", System.StringComparison.Ordinal) == true ? entry.icon : "";
        }
    }
}
