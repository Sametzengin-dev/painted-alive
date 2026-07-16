using UnityEngine;

namespace PaintedAlive.Painters
{
    [CreateAssetMenu(
        fileName = "PainterPigmentConfig",
        menuName = "Painted Alive/Painters/Pigment Config")]
    public sealed class PainterPigmentConfig : ScriptableObject
    {
        [Header("Capacity")]
        [SerializeField, Min(1f)] private float capacity = 100f;
        [SerializeField, Min(0f)] private float regenerationPerSecond = 8f;

        [Header("Stroke Costs")]
        [SerializeField, Min(0f)] private float strokeBeginCost = 3f;
        [SerializeField, Min(0f)] private float costPerMeter = 3.5f;

        public float Capacity => capacity;
        public float RegenerationPerSecond => regenerationPerSecond;
        public float StrokeBeginCost => strokeBeginCost;
        public float CostPerMeter => costPerMeter;

        private void OnValidate()
        {
            capacity = Mathf.Max(1f, capacity);
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            strokeBeginCost = Mathf.Max(0f, strokeBeginCost);
            costPerMeter = Mathf.Max(0f, costPerMeter);
        }
    }
}
