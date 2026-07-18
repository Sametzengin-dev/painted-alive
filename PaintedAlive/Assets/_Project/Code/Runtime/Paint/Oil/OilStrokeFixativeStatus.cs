using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilStrokeFixativeStatus : MonoBehaviour
    {
        [Header("Runtime - Read Only")]
        [SerializeField, Range(0f, 1f)]
        private float saturation;

        [SerializeField, Min(1f)]
        private float maximumCutDamageMultiplier = 1f;

        public float Saturation => saturation;
        public bool IsFullySaturated => saturation >= 0.999f;

        public float CutDamageMultiplier =>
            Mathf.Lerp(
                1f,
                maximumCutDamageMultiplier,
                Mathf.SmoothStep(0f, 1f, saturation));

        public float ApplyDose(
            float normalizedDose,
            float requestedMaximumCutDamageMultiplier)
        {
            float previousSaturation = saturation;

            saturation =
                Mathf.Clamp01(
                    saturation +
                    Mathf.Max(0f, normalizedDose));

            maximumCutDamageMultiplier =
                Mathf.Max(
                    1f,
                    Mathf.Max(
                        maximumCutDamageMultiplier,
                        requestedMaximumCutDamageMultiplier));

            return saturation - previousSaturation;
        }

        private void OnValidate()
        {
            saturation = Mathf.Clamp01(saturation);

            maximumCutDamageMultiplier =
                Mathf.Max(
                    1f,
                    maximumCutDamageMultiplier);
        }
    }
}
