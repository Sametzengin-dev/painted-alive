using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OilStrokeRuntime))]
    public sealed class WatercolorOilReaction :
        MonoBehaviour,
        IWatercolorReactive
    {
        [SerializeField]
        private OilStrokeRuntime stroke;

        [SerializeField]
        private Renderer strokeRenderer;

        [SerializeField]
        private WatercolorReactionConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float cumulativeDisplacement;

        [SerializeField]
        private int reactionCount;

        [SerializeField, Range(0f, 1f)]
        private float currentHeightRatio = 1f;

        private Vector3 originalLocalScale;

        public bool CanReactToWatercolor
        {
            get
            {
                if (!isActiveAndEnabled ||
                    stroke == null ||
                    !stroke.IsFinalized ||
                    stroke.State != OilStrokeState.Wet ||
                    config == null)
                {
                    return false;
                }

                bool canMove =
                    cumulativeDisplacement <
                    config.MaximumOilDisplacement - 0.001f;
                float minimumHeight =
                    originalLocalScale.y *
                    config.MinimumOilHeightScale;
                bool canThin =
                    transform.localScale.y >
                    minimumHeight + 0.001f;
                return canMove || canThin;
            }
        }

        public Bounds ReactionBounds
        {
            get
            {
                if (strokeRenderer != null)
                {
                    return strokeRenderer.bounds;
                }

                return new Bounds(
                    transform.position,
                    Vector3.one * 0.5f);
            }
        }

        public float CumulativeDisplacement =>
            cumulativeDisplacement;

        public float CurrentHeightRatio => currentHeightRatio;
        public int ReactionCount => reactionCount;

        private void Awake()
        {
            stroke ??= GetComponent<OilStrokeRuntime>();
            strokeRenderer ??= GetComponentInChildren<Renderer>();
            originalLocalScale = transform.localScale;
        }

        private void OnEnable()
        {
            WatercolorReactionRegistry.Register(this);
        }

        private void OnDisable()
        {
            WatercolorReactionRegistry.Unregister(this);
        }

        private void OnDestroy()
        {
            WatercolorReactionRegistry.Unregister(this);
        }

        public void Configure(WatercolorReactionConfig reactionConfig)
        {
            config = reactionConfig;

            if (originalLocalScale.sqrMagnitude <= 0.0001f)
            {
                originalLocalScale = transform.localScale;
            }
        }

        public void ApplyWatercolorReaction(
            WatercolorReactionContext context)
        {
            if (!CanReactToWatercolor ||
                context.Influence <= 0f ||
                context.DeltaTime <= 0f)
            {
                return;
            }

            Vector3 flowDirection = context.Direction.normalized;
            float requestedDistance =
                config.OilDriftSpeed *
                context.Influence *
                context.DeltaTime;
            float remainingDistance =
                Mathf.Max(
                    0f,
                    config.MaximumOilDisplacement -
                    cumulativeDisplacement);
            float appliedDistance =
                Mathf.Min(requestedDistance, remainingDistance);

            transform.position += flowDirection * appliedDistance;
            cumulativeDisplacement += appliedDistance;

            float minimumHeight =
                originalLocalScale.y *
                config.MinimumOilHeightScale;
            float nextHeight = Mathf.MoveTowards(
                transform.localScale.y,
                minimumHeight,
                originalLocalScale.y *
                config.OilThinningPerSecond *
                context.Influence *
                context.DeltaTime);
            Vector3 nextScale = transform.localScale;
            nextScale.y = nextHeight;
            transform.localScale = nextScale;
            currentHeightRatio =
                originalLocalScale.y > 0.0001f
                    ? Mathf.Clamp01(
                        nextHeight / originalLocalScale.y)
                    : 1f;
            reactionCount++;
        }
    }
}
