using UnityEngine;

namespace PaintedAlive.UI
{
    public static class PrototypeRuntimeHudVisibilityUtility
    {
        public static void ApplyCanvasGroup(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        public static bool Hold(
            bool relevantNow,
            ref float visibleUntil,
            float holdSeconds)
        {
            if (relevantNow)
            {
                visibleUntil = Mathf.Max(
                    visibleUntil,
                    Time.unscaledTime + Mathf.Max(0f, holdSeconds));

                return true;
            }

            return Time.unscaledTime <= visibleUntil;
        }
    }
}
