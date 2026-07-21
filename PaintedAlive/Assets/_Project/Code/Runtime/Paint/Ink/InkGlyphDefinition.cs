using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [CreateAssetMenu(
        fileName = "InkGlyphDefinition",
        menuName = "Painted Alive/Paint/Ink/Glyph Definition")]
    public sealed class InkGlyphDefinition : ScriptableObject
    {
        [SerializeField]
        private InkGlyphType glyphType;

        [SerializeField, Min(0)]
        private int complexityCost = 1;

        [Header("Eye")]
        [SerializeField, Min(0f)]
        private float detectionRange;

        [SerializeField, Range(0.05f, 2f)]
        private float targetRefreshInterval = 0.25f;

        [SerializeField]
        private bool requiresLineOfSight = true;

        [Header("Foot")]
        [SerializeField, Min(0f)]
        private float movementSpeed;

        [SerializeField, Min(0f)]
        private float turnSpeedDegrees;

        [Header("Shared")]
        [SerializeField]
        private float durabilityModifier;

        public InkGlyphType GlyphType => glyphType;
        public int ComplexityCost => complexityCost;
        public float DetectionRange => detectionRange;
        public float TargetRefreshInterval => targetRefreshInterval;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public float MovementSpeed => movementSpeed;
        public float TurnSpeedDegrees => turnSpeedDegrees;
        public float DurabilityModifier => durabilityModifier;

        private void OnValidate()
        {
            complexityCost = Mathf.Max(0, complexityCost);
            detectionRange = Mathf.Max(0f, detectionRange);
            targetRefreshInterval = Mathf.Clamp(
                targetRefreshInterval,
                0.05f,
                2f);
            movementSpeed = Mathf.Max(0f, movementSpeed);
            turnSpeedDegrees = Mathf.Max(0f, turnSpeedDegrees);
        }
    }
}
