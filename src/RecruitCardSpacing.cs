using SuperFantasyKingdom.UI;
using UnityEngine;

namespace WeeklyHermit
{
    internal sealed class RecruitCardSpacing : MonoBehaviour
    {
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform scroll;
        private RectTransform purchases;
        private RectTransform keywords;
        private Transform selection;
        private float scrollX;
        private float purchasesX;

        internal void Bind(UIEffectKeywordDictionary dictionary)
        {
            scroll = transform.Find("Panel/ScrollArea/Viewport") as RectTransform;
            purchases = transform.Find("Panel/BuyButtons") as RectTransform;
            selection = scroll.Find("Selection");
            keywords = dictionary.transform as RectTransform;
            scrollX = scroll.anchoredPosition.x;
            purchasesX = purchases.anchoredPosition.x;
            enabled = true;
        }

        private void LateUpdate()
        {
            if (!scroll || !selection || !keywords || selection.childCount == 0) return;
            var last = selection.GetChild(selection.childCount - 1);
            var card = last.GetComponent<UICardUnit>();
            var edge = card ? card.GetCardButton().transform as RectTransform : last as RectTransform;
            if (!edge) return;
            var parent = scroll.parent;
            edge.GetWorldCorners(corners);
            float right = parent.InverseTransformPoint(corners[2]).x + scrollX - scroll.anchoredPosition.x;

            // The native keyword drawer slides to anchored X=0 when a card gains focus.
            var entry = keywords.childCount > 0 ? keywords.GetChild(keywords.childCount - 1) as RectTransform : null;
            (entry ? entry : keywords).GetWorldCorners(corners);
            var openEdge = corners[0] - keywords.parent.TransformVector(Vector3.right * keywords.anchoredPosition.x);
            float left = parent.InverseTransformPoint(openEdge).x;
            float shift = Mathf.Max(0f, right + 10f - left);
            SetX(scroll, scrollX - shift);
            SetX(purchases, purchasesX - shift);
        }

        private void OnDisable()
        {
            if (scroll) SetX(scroll, scrollX);
            if (purchases) SetX(purchases, purchasesX);
        }

        private static void SetX(RectTransform rect, float x)
        {
            var pos = rect.anchoredPosition;
            if (Mathf.Approximately(pos.x, x)) return;
            pos.x = x;
            rect.anchoredPosition = pos;
        }
    }
}
