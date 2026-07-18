using UnityEngine;

namespace PaintedAlive.Paint
{
    [CreateAssetMenu(
        fileName = "OilStrokeStructuralIntegrityConfig",
        menuName = "Painted Alive/Paint/Oil Stroke Structural Integrity Config")]
    public sealed class OilStrokeStructuralIntegrityConfig : ScriptableObject
    {
        [Header("Integrity")]
        [SerializeField, Min(0.1f)]
        private float baseIntegrity = 1f;

        [SerializeField, Min(0.01f)]
        private float baseDamagePerCut = 0.42f;

        [SerializeField, Range(1, 10)]
        private int minimumCutsBeforeFracture = 2;

        [Header("Lifecycle Damage Multiplier")]
        [SerializeField, Min(0.1f)]
        private float wetDamageMultiplier = 0.85f;

        [SerializeField, Min(0.1f)]
        private float dryingDamageMultiplier = 1f;

        [SerializeField, Min(0.1f)]
        private float dryDamageMultiplier = 1.15f;

        [Header("Mesh Fragments")]
        [SerializeField, Range(2, 64)]
        private int minimumTrianglesPerFragment = 12;

        [SerializeField, Min(0.01f)]
        private float fragmentDensity = 2.4f;

        [SerializeField, Min(0.01f)]
        private float minimumFragmentMass = 0.18f;

        [SerializeField, Min(0.01f)]
        private float maximumFragmentMass = 4f;

        [Header("Collapse Motion")]
        [SerializeField, Min(0f)]
        private float separationImpulse = 0.45f;

        [SerializeField, Min(0f)]
        private float upwardImpulse = 0.12f;

        [SerializeField, Min(0f)]
        private float angularImpulse = 1.25f;

        [SerializeField, Min(0f)]
        private float fragmentDrag = 0.12f;

        [SerializeField, Min(0f)]
        private float fragmentAngularDrag = 0.25f;

        [Header("Cleanup")]
        [SerializeField, Min(0f)]
        private float settleToKinematicDelay = 2.5f;

        [SerializeField, Min(0f)]
        private float fragmentLifetime = 12f;

        public float BaseIntegrity => baseIntegrity;
        public float BaseDamagePerCut => baseDamagePerCut;
        public int MinimumCutsBeforeFracture => minimumCutsBeforeFracture;
        public int MinimumTrianglesPerFragment =>
            minimumTrianglesPerFragment;
        public float FragmentDensity => fragmentDensity;
        public float MinimumFragmentMass => minimumFragmentMass;
        public float MaximumFragmentMass => maximumFragmentMass;
        public float SeparationImpulse => separationImpulse;
        public float UpwardImpulse => upwardImpulse;
        public float AngularImpulse => angularImpulse;
        public float FragmentDrag => fragmentDrag;
        public float FragmentAngularDrag => fragmentAngularDrag;
        public float SettleToKinematicDelay => settleToKinematicDelay;
        public float FragmentLifetime => fragmentLifetime;

        public float GetDamageMultiplier(OilStrokeState state)
        {
            return state switch
            {
                OilStrokeState.Wet => wetDamageMultiplier,
                OilStrokeState.Drying => dryingDamageMultiplier,
                OilStrokeState.Dry => dryDamageMultiplier,
                _ => 1f
            };
        }

        public float GetInitialIntegrity(
            OilStrokePressureProfile pressureProfile)
        {
            float resistance =
                pressureProfile.IsValid
                    ? pressureProfile.CutResistanceMultiplier
                    : 1f;

            return baseIntegrity * Mathf.Max(0.1f, resistance);
        }

        public float GetCutDamage(OilStrokeState state)
        {
            return baseDamagePerCut *
                   Mathf.Max(0.1f, GetDamageMultiplier(state));
        }

        public float CalculateFragmentMass(
            Bounds localBounds,
            Vector3 worldScale)
        {
            Vector3 size = Vector3.Scale(
                localBounds.size,
                new Vector3(
                    Mathf.Abs(worldScale.x),
                    Mathf.Abs(worldScale.y),
                    Mathf.Abs(worldScale.z)));

            float volume =
                Mathf.Max(0.001f, size.x * size.y * size.z);

            return Mathf.Clamp(
                volume * fragmentDensity,
                minimumFragmentMass,
                maximumFragmentMass);
        }

        private void OnValidate()
        {
            baseIntegrity = Mathf.Max(0.1f, baseIntegrity);
            baseDamagePerCut = Mathf.Max(0.01f, baseDamagePerCut);
            minimumCutsBeforeFracture =
                Mathf.Max(1, minimumCutsBeforeFracture);

            wetDamageMultiplier = Mathf.Max(0.1f, wetDamageMultiplier);
            dryingDamageMultiplier =
                Mathf.Max(0.1f, dryingDamageMultiplier);
            dryDamageMultiplier = Mathf.Max(0.1f, dryDamageMultiplier);

            minimumTrianglesPerFragment =
                Mathf.Max(2, minimumTrianglesPerFragment);
            fragmentDensity = Mathf.Max(0.01f, fragmentDensity);
            minimumFragmentMass =
                Mathf.Max(0.01f, minimumFragmentMass);
            maximumFragmentMass =
                Mathf.Max(minimumFragmentMass, maximumFragmentMass);

            separationImpulse = Mathf.Max(0f, separationImpulse);
            upwardImpulse = Mathf.Max(0f, upwardImpulse);
            angularImpulse = Mathf.Max(0f, angularImpulse);
            fragmentDrag = Mathf.Max(0f, fragmentDrag);
            fragmentAngularDrag = Mathf.Max(0f, fragmentAngularDrag);
            settleToKinematicDelay =
                Mathf.Max(0f, settleToKinematicDelay);
            fragmentLifetime = Mathf.Max(0f, fragmentLifetime);
        }
    }
}
