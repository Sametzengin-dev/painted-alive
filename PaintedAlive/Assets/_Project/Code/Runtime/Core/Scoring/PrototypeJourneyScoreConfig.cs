using UnityEngine;

namespace PaintedAlive.Core.Scoring
{
    [CreateAssetMenu(
        fileName = "PrototypeJourneyScoreConfig",
        menuName = "Painted Alive/Core/Prototype Journey Score Config")]
    public sealed class PrototypeJourneyScoreConfig : ScriptableObject
    {
        [Header("Journey Score")]
        [SerializeField, Min(1)] private int maximumDistanceScore = 1000;
        [SerializeField, Min(0.02f)] private float sampleInterval = 0.08f;
        [SerializeField, Min(1)] private int distanceEventStep = 50;

        [Header("HUD")]
        [SerializeField] private bool showPrototypeHud = true;
        [SerializeField] private Vector2 hudPosition = new Vector2(18f, 18f);
        [SerializeField, Min(260f)] private float hudWidth = 360f;

        public int MaximumDistanceScore => maximumDistanceScore;
        public float SampleInterval => sampleInterval;
        public int DistanceEventStep => distanceEventStep;
        public bool ShowPrototypeHud => showPrototypeHud;
        public Vector2 HudPosition => hudPosition;
        public float HudWidth => hudWidth;

        private void OnValidate()
        {
            maximumDistanceScore = Mathf.Max(1, maximumDistanceScore);
            sampleInterval = Mathf.Max(0.02f, sampleInterval);
            distanceEventStep = Mathf.Clamp(distanceEventStep, 1, maximumDistanceScore);
            hudWidth = Mathf.Max(260f, hudWidth);
        }
    }
}
