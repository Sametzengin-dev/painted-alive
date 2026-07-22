using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [CreateAssetMenu(
        fileName = "InkCounterplayConfig",
        menuName = "Painted Alive/Paint/Ink/Counterplay Config")]
    public sealed class InkCounterplayConfig : ScriptableObject
    {
        [Header("Palette Knife")]
        [SerializeField, Min(0.1f)]
        private float paletteKnifeGlyphDamage = 1f;

        [SerializeField, Min(1f)]
        private float paletteKnifeMaximumAimDistance = 50f;

        [SerializeField, Min(0f)]
        private float paletteKnifeCastRadius = 0.12f;

        [SerializeField, Min(0.1f)]
        private float paletteKnifeReach = 2.5f;

        [Header("Fixative")]
        [SerializeField, Min(0.1f)]
        private float fixativeDuration = 3f;

        [SerializeField, Min(0.1f)]
        private float fixativeReach = 4.25f;

        [SerializeField, Min(0f)]
        private float fixativeCastRadius = 0.2f;

        [SerializeField, Min(1f)]
        private float fixativeMaximumAimDistance = 50f;

        [SerializeField]
        private Color fixedInkColor =
            new Color(0.36f, 0.46f, 0.54f, 1f);

        public float PaletteKnifeGlyphDamage =>
            paletteKnifeGlyphDamage;
        public float PaletteKnifeMaximumAimDistance =>
            paletteKnifeMaximumAimDistance;
        public float PaletteKnifeCastRadius =>
            paletteKnifeCastRadius;
        public float PaletteKnifeReach => paletteKnifeReach;
        public float FixativeDuration => fixativeDuration;
        public float FixativeReach => fixativeReach;
        public float FixativeCastRadius => fixativeCastRadius;
        public float FixativeMaximumAimDistance =>
            fixativeMaximumAimDistance;
        public Color FixedInkColor => fixedInkColor;

        private void OnValidate()
        {
            paletteKnifeGlyphDamage =
                Mathf.Max(0.1f, paletteKnifeGlyphDamage);
            paletteKnifeMaximumAimDistance =
                Mathf.Max(1f, paletteKnifeMaximumAimDistance);
            paletteKnifeCastRadius =
                Mathf.Max(0f, paletteKnifeCastRadius);
            paletteKnifeReach = Mathf.Max(0.1f, paletteKnifeReach);
            fixativeDuration = Mathf.Max(0.1f, fixativeDuration);
            fixativeReach = Mathf.Max(0.1f, fixativeReach);
            fixativeCastRadius = Mathf.Max(0f, fixativeCastRadius);
            fixativeMaximumAimDistance =
                Mathf.Max(1f, fixativeMaximumAimDistance);
        }
    }
}
