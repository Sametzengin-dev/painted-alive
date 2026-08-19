using System;

namespace PaintedAlive.UI.Telegraph
{
    public static class PrototypePainterTelegraphHub
    {
        public static event Action<PrototypePainterTelegraphSignal>
            TelegraphUpdated;

        public static event Action<PrototypePainterTelegraphOutcome>
            TelegraphResolved;

        public static void Publish(
            PrototypePainterTelegraphSignal signal)
        {
            if (!signal.HasRenderableGeometry)
            {
                return;
            }

            TelegraphUpdated?.Invoke(signal);
        }

        public static void Resolve(
            PrototypePainterTelegraphOutcome outcome)
        {
            TelegraphResolved?.Invoke(outcome);
        }
    }
}
