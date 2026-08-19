using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaintedAlive.Painters.SideCanvas
{
    /// <summary>
    /// Geometry helper for M45's virtual pointer.
    ///
    /// It intentionally does not implement EventSystem pointer interfaces.
    /// Physical mouse position is ignored because the prototype camera stack
    /// may recenter it every frame.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class PrototypeSideCanvasPointerInput :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLivingSideCanvasController controller;

        [SerializeField] private RectTransform inputRect;
        [SerializeField] private bool pointerInside;
        [SerializeField] private int receivedEventCount;

        public bool IsConfigured =>
            controller != null &&
            inputRect != null;

        public bool PointerInside =>
            pointerInside;

        public int ReceivedEventCount =>
            receivedEventCount;

        public RectTransform InputRect =>
            inputRect;

        public void Configure(
            PrototypeLivingSideCanvasController configuredController,
            RectTransform configuredInputRect)
        {
            controller = configuredController;
            inputRect = configuredInputRect;

            RawImage image =
                GetComponent<RawImage>();

            if (image != null)
            {
                image.raycastTarget = false;
            }
        }

        public bool ContainsScreenPoint(
            Vector2 screenPoint)
        {
            if (!IsConfigured)
            {
                pointerInside = false;
                return false;
            }

            pointerInside =
                RectTransformUtility
                    .RectangleContainsScreenPoint(
                        inputRect,
                        screenPoint,
                        null);

            return pointerInside;
        }

        public bool TryNormalizeScreenPoint(
            Vector2 screenPoint,
            bool clampOutside,
            out Vector2 normalized)
        {
            normalized = default;

            if (!IsConfigured)
            {
                return false;
            }

            if (!RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        inputRect,
                        screenPoint,
                        null,
                        out Vector2 local))
            {
                return false;
            }

            Rect rect = inputRect.rect;

            if (rect.width <= 0f ||
                rect.height <= 0f)
            {
                return false;
            }

            if (!clampOutside &&
                !rect.Contains(local))
            {
                return false;
            }

            normalized =
                new Vector2(
                    Mathf.InverseLerp(
                        rect.xMin,
                        rect.xMax,
                        local.x),
                    Mathf.InverseLerp(
                        rect.yMin,
                        rect.yMax,
                        local.y));

            normalized.x =
                Mathf.Clamp01(normalized.x);

            normalized.y =
                Mathf.Clamp01(normalized.y);

            return true;
        }

        public void RecordVirtualEvent()
        {
            receivedEventCount++;
        }
    }
}
