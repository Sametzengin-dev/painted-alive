using System.Collections.Generic;
using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    [DisallowMultipleComponent]
    public sealed class WatercolorReactionCoordinator : MonoBehaviour
    {
        private const float MinimumDiscoveryInterval = 0.75f;
        private const float ReactionTickInterval = 0.05f;

        [SerializeField]
        private WatercolorReactionConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int discoveredOilStrokeCount;

        [SerializeField]
        private int activeReactionCount;

        [SerializeField]
        private int appliedReactionCount;

        private float nextDiscoveryTime;
        private float nextReactionTime;
        private float accumulatedReactionTime;

        public int DiscoveredOilStrokeCount =>
            discoveredOilStrokeCount;

        public int ActiveReactionCount => activeReactionCount;
        public int AppliedReactionCount => appliedReactionCount;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(WatercolorReactionCoordinator)} " +
                    "requires a WatercolorReactionConfig.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }

            IReadOnlyList<WatercolorFlowSurface> surfaces =
                WatercolorFlowSurface.ActiveSurfaces;

            if (surfaces.Count == 0)
            {
                activeReactionCount =
                    WatercolorReactionRegistry.ActiveReactions.Count;
                accumulatedReactionTime = 0f;
                return;
            }

            if (Time.time >= nextDiscoveryTime)
            {
                DiscoverOilStrokes();
                nextDiscoveryTime =
                    Time.time + Mathf.Max(
                        MinimumDiscoveryInterval,
                        config.DiscoveryInterval);
            }

            accumulatedReactionTime += Time.deltaTime;

            if (Time.time < nextReactionTime)
            {
                return;
            }

            float reactionDeltaTime = accumulatedReactionTime;
            accumulatedReactionTime = 0f;
            nextReactionTime = Time.time + ReactionTickInterval;
            ProcessReactions(reactionDeltaTime, surfaces);
        }

        private void DiscoverOilStrokes()
        {
            OilStrokeRuntime[] strokes =
                UnityEngine.Object.FindObjectsByType<
                    OilStrokeRuntime>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);

            discoveredOilStrokeCount = strokes.Length;

            foreach (OilStrokeRuntime stroke in strokes)
            {
                WatercolorOilReaction reaction =
                    stroke.GetComponent<WatercolorOilReaction>();

                if (reaction == null)
                {
                    reaction =
                        stroke.gameObject.AddComponent<
                            WatercolorOilReaction>();
                }

                reaction.Configure(config);
            }
        }

        private void ProcessReactions(
            float deltaTime,
            IReadOnlyList<WatercolorFlowSurface> surfaces)
        {
            IReadOnlyList<IWatercolorReactive> reactions =
                WatercolorReactionRegistry.ActiveReactions;
            activeReactionCount = reactions.Count;

            for (int i = reactions.Count - 1; i >= 0; i--)
            {
                IWatercolorReactive reactive = reactions[i];
                UnityEngine.Object reactiveObject =
                    reactive as UnityEngine.Object;

                if (reactiveObject == null ||
                    !reactive.CanReactToWatercolor)
                {
                    continue;
                }

                Bounds bounds = reactive.ReactionBounds;
                WatercolorFlowSurface bestSurface = null;
                Vector3 bestDirection = Vector3.zero;
                Vector3 bestPoint = bounds.center;
                float bestInfluence = 0f;

                for (int surfaceIndex = surfaces.Count - 1;
                     surfaceIndex >= 0;
                     surfaceIndex--)
                {
                    WatercolorFlowSurface surface =
                        surfaces[surfaceIndex];

                    if (surface == null)
                    {
                        continue;
                    }

                    Bounds surfaceBounds = surface.WorldBounds;
                    surfaceBounds.Expand(
                        new Vector3(0.5f, 2f, 0.5f));

                    if (!surfaceBounds.Intersects(bounds) ||
                        !TrySampleBounds(
                            surface,
                            bounds,
                            out Vector3 direction,
                            out float influence,
                            out Vector3 contactPoint) ||
                        influence <= bestInfluence)
                    {
                        continue;
                    }

                    bestSurface = surface;
                    bestDirection = direction;
                    bestInfluence = influence;
                    bestPoint = contactPoint;
                }

                if (bestSurface == null)
                {
                    continue;
                }

                reactive.ApplyWatercolorReaction(
                    new WatercolorReactionContext(
                        bestSurface,
                        bestDirection,
                        bestPoint,
                        bestInfluence,
                        deltaTime));
                appliedReactionCount++;
            }
        }

        private static bool TrySampleBounds(
            WatercolorFlowSurface surface,
            Bounds bounds,
            out Vector3 direction,
            out float influence,
            out Vector3 contactPoint)
        {
            direction = Vector3.zero;
            influence = 0f;
            contactPoint = bounds.center;

            Vector3 center = bounds.center;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float sampleY = Mathf.Lerp(min.y, center.y, 0.35f);
            TryUseSample(
                surface,
                new Vector3(center.x, sampleY, center.z),
                ref direction,
                ref influence,
                ref contactPoint);
            TryUseSample(
                surface,
                new Vector3(min.x, sampleY, min.z),
                ref direction,
                ref influence,
                ref contactPoint);
            TryUseSample(
                surface,
                new Vector3(min.x, sampleY, max.z),
                ref direction,
                ref influence,
                ref contactPoint);
            TryUseSample(
                surface,
                new Vector3(max.x, sampleY, min.z),
                ref direction,
                ref influence,
                ref contactPoint);
            TryUseSample(
                surface,
                new Vector3(max.x, sampleY, max.z),
                ref direction,
                ref influence,
                ref contactPoint);

            return influence > 0.001f;
        }

        private static void TryUseSample(
            WatercolorFlowSurface surface,
            Vector3 samplePosition,
            ref Vector3 bestDirection,
            ref float bestInfluence,
            ref Vector3 bestContactPoint)
        {
            if (!surface.TrySampleFlow(
                    samplePosition,
                    out Vector3 direction,
                    out float influence,
                    out Vector3 contactPoint) ||
                influence <= bestInfluence)
            {
                return;
            }

            bestDirection = direction;
            bestInfluence = influence;
            bestContactPoint = contactPoint;
        }
    }
}
