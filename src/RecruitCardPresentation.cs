using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeeklyHermit
{
    internal sealed class RecruitCardPresentation : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private static readonly AccessTools.FieldRef<UICardUnit, TextMeshProUGUI> Description =
            AccessTools.FieldRefAccess<UICardUnit, TextMeshProUGUI>("description");
        private static readonly AccessTools.FieldRef<UICardUnit, Image> Frame =
            AccessTools.FieldRefAccess<UICardUnit, Image>("card");
        private Outline focus;

        internal static void Apply(UICardUnit card, UnitBase unit)
        {
            if (!card || !unit) return;
            var description = Description(card);
            string role = Helper.Instance.GetRoleTitle(unit.GetRole());
            string attack = Helper.Instance.GetAttackTypeTitle(unit.GetAttackType());
            if (description && description.richText && !string.IsNullOrWhiteSpace(role) && !string.IsNullOrWhiteSpace(attack))
            {
                // Keep the original quote and font size, including for translated or modded cards.
                string quote = description.text;
                description.text = $"<color=#40223B>{role} / {attack}</color>\n{quote}";
                description.ForceMeshUpdate(ignoreActiveState: true);
                if (description.isTextOverflowing)
                    description.text = quote;
            }

            // A priced unit has a separate Buy button. Keep the focus cue on its card
            // only while confirming the selected control would recruit that unit.
            var purchase = card.GetComponent<RecruitPurchase>();
            var action = purchase ? purchase.Button : card.GetCardButton();
            var frame = Frame(card);
            if (!action || !frame) return;
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 246, 196, 255);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = EventSystem.current && EventSystem.current.currentSelectedGameObject == action.gameObject;
            action.gameObject.AddComponent<RecruitCardPresentation>().focus = outline;
        }

        public void OnSelect(BaseEventData data) { if (focus) focus.enabled = true; }
        public void OnDeselect(BaseEventData data) { if (focus) focus.enabled = false; }
        private void OnDisable() { if (focus) focus.enabled = false; }
    }
}
