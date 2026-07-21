using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    public readonly struct WatercolorReactionContext
    {
        public WatercolorReactionContext(
            WatercolorFlowSurface source,
            Vector3 direction,
            Vector3 contactPoint,
            float influence,
            float deltaTime)
        {
            Source = source;
            Direction = direction;
            ContactPoint = contactPoint;
            Influence = influence;
            DeltaTime = deltaTime;
        }

        public WatercolorFlowSurface Source { get; }
        public Vector3 Direction { get; }
        public Vector3 ContactPoint { get; }
        public float Influence { get; }
        public float DeltaTime { get; }
    }

    public interface IWatercolorReactive
    {
        bool CanReactToWatercolor { get; }
        Bounds ReactionBounds { get; }
        void ApplyWatercolorReaction(
            WatercolorReactionContext context);
    }

    public static class WatercolorReactionRegistry
    {
        private static readonly List<IWatercolorReactive>
            ActiveReactionList = new();

        public static IReadOnlyList<IWatercolorReactive>
            ActiveReactions => ActiveReactionList;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ActiveReactionList.Clear();
        }

        public static void Register(IWatercolorReactive reactive)
        {
            if (reactive != null &&
                !ActiveReactionList.Contains(reactive))
            {
                ActiveReactionList.Add(reactive);
            }
        }

        public static void Unregister(IWatercolorReactive reactive)
        {
            if (reactive != null)
            {
                ActiveReactionList.Remove(reactive);
            }
        }
    }
}
