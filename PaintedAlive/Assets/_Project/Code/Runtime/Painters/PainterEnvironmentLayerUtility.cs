using UnityEngine;

namespace PaintedAlive.Painters
{
    /// <summary>
    /// Keeps Painter world raycasts compatible with the authored continuation
    /// maps without replacing existing scene masks. Existing bits are preserved
    /// and shared environment collision layers are added when they exist.
    /// </summary>
    public static class PainterEnvironmentLayerUtility
    {
        private static readonly string[] SharedEnvironmentLayers =
        {
            "PrismaticCollision"
        };

        public static LayerMask ExpandSurfaceMask(LayerMask source)
        {
            int value = source.value;

            for (int index = 0; index < SharedEnvironmentLayers.Length; index++)
            {
                int layer = LayerMask.NameToLayer(SharedEnvironmentLayers[index]);
                if (layer >= 0)
                    value |= 1 << layer;
            }

            return value;
        }

        public static bool ContainsLayer(LayerMask mask, int layer)
        {
            return layer >= 0 &&
                   (mask.value & (1 << layer)) != 0;
        }
    }
}
