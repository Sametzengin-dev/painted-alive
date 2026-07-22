using UnityEngine;

namespace PaintedAlive.Paint.Ink.Lifecycle
{
    [CreateAssetMenu(
        fileName = "InkNestLifecycleConfig",
        menuName = "Painted Alive/Paint/Ink/Nest Lifecycle Config")]
    public sealed class InkNestLifecycleConfig : ScriptableObject
    {
        [Header("Nest Spawning")]
        [SerializeField, Min(0.5f)]
        private float firstSpawnDelay = 6f;

        [SerializeField, Min(1f)]
        private float spawnInterval = 12f;

        [SerializeField, Range(0.2f, 3f)]
        private float spawnTelegraphDuration = 1.25f;

        [SerializeField, Range(0.25f, 5f)]
        private float blockedRetryDelay = 1.5f;

        [SerializeField, Range(1, 6)]
        private int maximumActiveChildren = 2;

        [SerializeField, Range(0.15f, 1.5f)]
        private float spawnRadius = 0.55f;

        [Header("Critical Glyph Death")]
        [SerializeField, Range(0.15f, 1.5f)]
        private float tipDuration = 0.45f;

        [SerializeField, Range(0f, 5f)]
        private float tippedLingerDuration = 1.3f;

        [SerializeField, Range(0.2f, 2f)]
        private float dissolveDuration = 0.75f;

        [SerializeField, Range(70f, 145f)]
        private float tipAngle = 105f;

        [SerializeField, Range(0f, 0.5f)]
        private float dissolveSinkDistance = 0.12f;

        public float FirstSpawnDelay => firstSpawnDelay;
        public float SpawnInterval => spawnInterval;
        public float SpawnTelegraphDuration => spawnTelegraphDuration;
        public float BlockedRetryDelay => blockedRetryDelay;
        public int MaximumActiveChildren => maximumActiveChildren;
        public float SpawnRadius => spawnRadius;
        public float TipDuration => tipDuration;
        public float TippedLingerDuration => tippedLingerDuration;
        public float DissolveDuration => dissolveDuration;
        public float TipAngle => tipAngle;
        public float DissolveSinkDistance => dissolveSinkDistance;
        public float TotalDeathDuration =>
            tipDuration + tippedLingerDuration + dissolveDuration;

        private void OnValidate()
        {
            firstSpawnDelay = Mathf.Max(0.5f, firstSpawnDelay);
            spawnInterval = Mathf.Max(1f, spawnInterval);
            spawnTelegraphDuration = Mathf.Clamp(
                spawnTelegraphDuration,
                0.2f,
                Mathf.Min(3f, firstSpawnDelay));
            blockedRetryDelay = Mathf.Clamp(blockedRetryDelay, 0.25f, 5f);
            maximumActiveChildren = Mathf.Clamp(maximumActiveChildren, 1, 6);
            spawnRadius = Mathf.Clamp(spawnRadius, 0.15f, 1.5f);
            tipDuration = Mathf.Clamp(tipDuration, 0.15f, 1.5f);
            tippedLingerDuration = Mathf.Clamp(tippedLingerDuration, 0f, 5f);
            dissolveDuration = Mathf.Clamp(dissolveDuration, 0.2f, 2f);
            tipAngle = Mathf.Clamp(tipAngle, 70f, 145f);
            dissolveSinkDistance = Mathf.Clamp(
                dissolveSinkDistance,
                0f,
                0.5f);
        }
    }
}
