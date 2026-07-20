using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class FrameGunRopeVisual : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer lineRenderer;

        private FrameGunConfig config;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            SetVisible(false);
        }

        public void Configure(FrameGunConfig frameGunConfig)
        {
            config = frameGunConfig;

            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (config == null || lineRenderer == null)
            {
                return;
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = config.RopeSegments;
            lineRenderer.startWidth = config.RopeWidth;
            lineRenderer.endWidth = config.RopeWidth * 0.82f;
            ApplyRopeColor(0f, false, 1f);
        }

        public void SetVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }

        public void RenderRope(
            Vector3 start,
            Vector3 end,
            float ropeLength,
            float normalizedTension)
        {
            RenderRope(
                start,
                end,
                ropeLength,
                normalizedTension,
                false,
                1f);
        }

        public void RenderRope(
            Vector3 start,
            Vector3 end,
            float ropeLength,
            float normalizedTension,
            bool isSliding,
            float surfaceGrip)
        {
            if (config == null || lineRenderer == null)
            {
                return;
            }

            int segmentCount = config.RopeSegments;

            if (lineRenderer.positionCount != segmentCount)
            {
                lineRenderer.positionCount = segmentCount;
            }

            float directDistance = Vector3.Distance(start, end);
            float slack = Mathf.Max(0f, ropeLength - directDistance);

            float sag =
                Mathf.Min(
                    config.MaximumVisualSag,
                    slack * 0.65f +
                    (1f - Mathf.Clamp01(normalizedTension)) *
                    0.035f);

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                float sagCurve = 4f * t * (1f - t);
                point += Vector3.down * (sag * sagCurve);
                lineRenderer.SetPosition(i, point);
            }

            ApplyRopeColor(
                normalizedTension,
                isSliding,
                surfaceGrip);
        }

        private void ApplyRopeColor(
            float normalizedTension,
            bool isSliding,
            float surfaceGrip)
        {
            if (config == null || lineRenderer == null)
            {
                return;
            }

            float tension = Mathf.Clamp01(normalizedTension);
            Color color;

            if (isSliding)
            {
                color = Color.Lerp(
                    config.SlidingRopeColor,
                    config.CriticalRopeColor,
                    tension * (1f - Mathf.Clamp01(surfaceGrip)));
            }
            else if (tension < 0.78f)
            {
                color = Color.Lerp(
                    config.SlackRopeColor,
                    config.TensionRopeColor,
                    tension / 0.78f);
            }
            else
            {
                color = Color.Lerp(
                    config.TensionRopeColor,
                    config.CriticalRopeColor,
                    Mathf.InverseLerp(0.78f, 1f, tension));
            }

            lineRenderer.startColor = color;
            lineRenderer.endColor =
                Color.Lerp(color, Color.white, 0.08f);
        }
    }
}
