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
            if (config == null || lineRenderer == null)
            {
                return;
            }

            int segmentCount = config.RopeSegments;

            if (lineRenderer.positionCount != segmentCount)
            {
                lineRenderer.positionCount = segmentCount;
            }

            float directDistance =
                Vector3.Distance(start, end);

            float slack =
                Mathf.Max(0f, ropeLength - directDistance);

            float sag =
                Mathf.Min(
                    config.MaximumVisualSag,
                    slack * 0.65f +
                    (1f - Mathf.Clamp01(normalizedTension)) *
                    0.035f);

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);

                Vector3 point =
                    Vector3.Lerp(start, end, t);

                float sagCurve =
                    4f * t * (1f - t);

                point += Vector3.down * (sag * sagCurve);

                lineRenderer.SetPosition(i, point);
            }
        }
    }
}
