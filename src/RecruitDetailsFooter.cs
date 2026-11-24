using SuperFantasyKingdom;
using UnityEngine;

namespace WeeklyHermit
{
    // Inspect keeps the merchant in the native minimized slot. Its Choose button and
    // Cancel prompt sit behind the details panel at large UI scales.
    internal sealed class RecruitDetailsFooter : MonoBehaviour
    {
        private const float Padding = 6f;
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform panel;
        private RectTransform canvas;
        private FooterControl choose;
        private FooterControl cancel;

        internal void Bind(OverlayManager overlays)
        {
            canvas = overlays.transform as RectTransform;
            panel = overlays.GetOverlaySelection().parent as RectTransform;
            choose = new FooterControl(overlays.transform.Find("Minimized/Button") as RectTransform);
            cancel = new FooterControl(overlays.transform.Find("Minimized/ButtonPrompt") as RectTransform);
        }

        private void LateUpdate()
        {
            if (!panel || !canvas) return;
            panel.GetWorldCorners(corners);
            Vector3 panelBottom = corners[0];
            canvas.GetWorldCorners(corners);
            Vector3 canvasBottom = corners[0];
            choose.Fit(panelBottom, canvasBottom, corners);
            cancel.Fit(panelBottom, canvasBottom, corners);
        }

        private void OnDisable()
        {
            choose.Restore();
            cancel.Restore();
        }

        private readonly struct FooterControl
        {
            private readonly RectTransform rect;
            private readonly float nativeY;

            internal FooterControl(RectTransform rect)
            {
                this.rect = rect;
                nativeY = rect ? rect.anchoredPosition.y : 0f;
            }

            internal void Fit(Vector3 panelBottom, Vector3 canvasBottom, Vector3[] corners)
            {
                if (!rect || !rect.gameObject.activeInHierarchy) return;
                rect.GetWorldCorners(corners);
                var parent = rect.parent;
                float toNative = nativeY - rect.anchoredPosition.y;
                float overlap = parent.InverseTransformPoint(corners[1]).y + toNative + Padding
                    - parent.InverseTransformPoint(panelBottom).y;
                float room = parent.InverseTransformPoint(corners[0]).y + toNative - Padding
                    - parent.InverseTransformPoint(canvasBottom).y;
                SetY(nativeY - Mathf.Clamp(overlap, 0f, Mathf.Max(0f, room)));
            }

            internal void Restore()
            {
                if (rect) SetY(nativeY);
            }

            private void SetY(float y)
            {
                var position = rect.anchoredPosition;
                if (Mathf.Approximately(position.y, y)) return;
                position.y = y;
                rect.anchoredPosition = position;
            }
        }
    }
}
