using UnityEngine;

namespace PaintedAlive.Figures.StainRestoration
{
    [CreateAssetMenu(
        fileName = "StainSpongeRestorationConfig",
        menuName = "Painted Alive/Figures/" +
            "Stain Sponge Restoration Config")]
    public sealed class StainSpongeRestorationConfig :
        ScriptableObject
    {
        [Header("Clean Pigment")]
        [SerializeField, Min(0.25f)]
        private float pigmentLoadRadius = 1.35f;

        [Header("Restoration Surface")]
        [SerializeField, Min(0.25f)]
        private float restorationRadius = 1.55f;

        [SerializeField, Min(0.1f)]
        private float restorationDuration = 2.25f;

        [SerializeField, Range(0.05f, 0.95f)]
        private float restoredNormalizedClarity = 0.55f;

        [SerializeField, Min(0f)]
        private float interruptedProgressRetention = 0f;

        public float PigmentLoadRadius => pigmentLoadRadius;
        public float RestorationRadius => restorationRadius;
        public float RestorationDuration =>
            restorationDuration;
        public float RestoredNormalizedClarity =>
            restoredNormalizedClarity;
        public float InterruptedProgressRetention =>
            interruptedProgressRetention;
    }
}
