using UnityEngine;

namespace PaintedAlive.Figures.StainSupport.FrameExit
{
    [CreateAssetMenu(
        fileName = "DA_StainFrameExitRules",
        menuName = "Painted Alive/Figures/Stain Frame Exit Rules")]
    public sealed class StainFrameExitConfig : ScriptableObject
    {
        [Header("Prototype Scoring Contract")]
        [SerializeField, Min(0)] private int normalFigureExitScore = 250;
        [SerializeField, Min(0)] private int stainSupportArrivalScore = 0;

        [Header("Gate Debounce")]
        [SerializeField, Min(0.05f)] private float sameFigureCooldown = 1.25f;

        [Header("Feedback")]
        [SerializeField, Min(0.1f)] private float feedbackDuration = 2.4f;
        [SerializeField] private Color normalExitColor = new Color(0.24f, 0.95f, 0.66f, 1f);
        [SerializeField] private Color stainArrivalColor = new Color(0.20f, 0.86f, 0.92f, 1f);
        [SerializeField] private Color idleColor = new Color(0.35f, 0.72f, 0.92f, 1f);

        public int NormalFigureExitScore => normalFigureExitScore;
        public int StainSupportArrivalScore => stainSupportArrivalScore;
        public float SameFigureCooldown => sameFigureCooldown;
        public float FeedbackDuration => feedbackDuration;
        public Color NormalExitColor => normalExitColor;
        public Color StainArrivalColor => stainArrivalColor;
        public Color IdleColor => idleColor;

        private void OnValidate()
        {
            normalFigureExitScore = Mathf.Max(0, normalFigureExitScore);
            stainSupportArrivalScore = Mathf.Max(0, stainSupportArrivalScore);
            sameFigureCooldown = Mathf.Max(0.05f, sameFigureCooldown);
            feedbackDuration = Mathf.Max(0.1f, feedbackDuration);
        }
    }
}
