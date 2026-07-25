using UnityEngine;

namespace PaintedAlive.Figures.StainTraversal
{
    [CreateAssetMenu(
        fileName = "StainCrackTraversalConfig",
        menuName = "Painted Alive/Figures/Stain Crack Traversal Config")]
    public sealed class StainCrackTraversalConfig : ScriptableObject
    {
        [SerializeField, Min(0.25f)]
        private float interactionRange = 1.45f;

        [SerializeField, Min(0.1f)]
        private float traversalDuration = 0.72f;

        [SerializeField, Min(0f)]
        private float inputCooldown = 0.35f;

        [SerializeField, Range(0.05f, 1f)]
        private float minimumTransitScale = 0.16f;

        public float InteractionRange => interactionRange;
        public float TraversalDuration => traversalDuration;
        public float InputCooldown => inputCooldown;
        public float MinimumTransitScale => minimumTransitScale;
    }
}
