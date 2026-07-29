using UnityEngine;
using PaintedAlive.Figures;

namespace PaintedAlive.Figures.StainSupport.FrameExit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class StainFrameExitDebugProbe : MonoBehaviour
    {
        [SerializeField] private FigureClarityState clarityState;

        [Header("Runtime - Read Only")]
        [SerializeField] private FigureFrameExitOutcome lastOutcome;
        [SerializeField] private int lastAwardedScore;
        [SerializeField] private bool lastCountsAsNormalExit;
        [SerializeField] private float lastEvaluatedAt;

        public FigureFrameExitOutcome LastOutcome => lastOutcome;
        public int LastAwardedScore => lastAwardedScore;
        public bool LastCountsAsNormalExit => lastCountsAsNormalExit;
        public float LastEvaluatedAt => lastEvaluatedAt;

        public void Configure(FigureClarityState figureClarity)
        {
            clarityState = figureClarity;
        }

        private void Awake()
        {
            clarityState ??= GetComponent<FigureClarityState>();
        }

        private void OnEnable()
        {
            FigureFrameExitRuleService.ExitEvaluated += HandleExitEvaluated;
        }

        private void OnDisable()
        {
            FigureFrameExitRuleService.ExitEvaluated -= HandleExitEvaluated;
        }

        private void HandleExitEvaluated(FigureFrameExitDecision decision)
        {
            if (clarityState == null || decision.Figure != clarityState)
            {
                return;
            }

            lastOutcome = decision.Outcome;
            lastAwardedScore = decision.AwardedScore;
            lastCountsAsNormalExit = decision.CountsAsNormalExit;
            lastEvaluatedAt = decision.EvaluatedAt;
        }
    }
}
