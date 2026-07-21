using PaintedAlive.Paint.Watercolor;
using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [DisallowMultipleComponent]
    public sealed class WatercolorInkReaction : MonoBehaviour, IWatercolorReactive
    {
        [SerializeField]
        private InkSystemConfig config;

        [SerializeField]
        private InkSurface inkSurface;

        [SerializeField]
        private InkCreatureRuntime inkCreature;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int reactionCount;

        [SerializeField, Range(0f, 1f)]
        private float lastInfluence;

        public int ReactionCount => reactionCount;
        public float LastInfluence => lastInfluence;
        public bool IsSurfaceReaction => inkSurface != null;
        public bool IsCreatureReaction => inkCreature != null;

        public bool CanReactToWatercolor =>
            isActiveAndEnabled &&
            config != null &&
            ((inkSurface != null && inkSurface.IsInitialized) ||
             (inkCreature != null && inkCreature.IsInitialized));

        public Bounds ReactionBounds
        {
            get
            {
                if (inkSurface != null)
                {
                    return inkSurface.WorldBounds;
                }

                return inkCreature != null
                    ? inkCreature.WorldBounds
                    : new Bounds(transform.position, Vector3.one * 0.25f);
            }
        }

        private void Awake()
        {
            inkSurface ??= GetComponent<InkSurface>();
            inkCreature ??= GetComponent<InkCreatureRuntime>();
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

        public void Configure(InkSystemConfig systemConfig)
        {
            config = systemConfig;
            inkSurface ??= GetComponent<InkSurface>();
            inkCreature ??= GetComponent<InkCreatureRuntime>();
        }

        public void ApplyWatercolorReaction(WatercolorReactionContext context)
        {
            if (!CanReactToWatercolor)
            {
                return;
            }

            float influence = Mathf.Clamp01(context.Influence);
            float deltaTime = Mathf.Max(0f, context.DeltaTime);

            if (inkSurface != null)
            {
                float expansion =
                    config.SurfaceExpansionPerSecond *
                    influence * deltaTime;
                inkSurface.ApplyWatercolorExpansion(
                    expansion,
                    config.MaximumSurfaceRadius,
                    expansion * 8f);
            }

            if (inkCreature != null)
            {
                inkCreature.ApplyWatercolorExposure(
                    influence *
                    config.CreatureWaterGainPerSecond *
                    deltaTime,
                    context.Direction);
            }

            lastInfluence = influence;
            reactionCount++;
        }
    }
}
