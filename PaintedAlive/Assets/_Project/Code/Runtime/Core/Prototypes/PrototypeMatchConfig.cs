using UnityEngine;

namespace PaintedAlive.Core.Prototypes
{
    [CreateAssetMenu(
        fileName = "PrototypeMatchConfig",
        menuName = "Painted Alive/Prototypes/Match Config")]
    public sealed class PrototypeMatchConfig : ScriptableObject
    {
        [SerializeField, Min(10f)]
        private float matchDuration = 300f;

        [SerializeField, Min(0f)]
        private float countdownDuration = 3f;

        public float MatchDuration => matchDuration;
        public float CountdownDuration => countdownDuration;

        private void OnValidate()
        {
            matchDuration = Mathf.Max(10f, matchDuration);
            countdownDuration = Mathf.Max(0f, countdownDuration);
        }
    }
}