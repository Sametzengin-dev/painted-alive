using System;
using UnityEngine;
using PaintedAlive.Figures;

namespace PaintedAlive.Figures.StainSupport.FrameExit
{
    public enum FigureFrameExitOutcome
    {
        None = 0,
        NormalFigureExit = 1,
        StainSupportArrival = 2
    }

    public readonly struct FigureFrameExitDecision
    {
        public FigureFrameExitDecision(
            FigureClarityState figure,
            FigureFrameExitOutcome outcome,
            int awardedScore,
            bool countsAsNormalExit,
            Vector3 gatePosition,
            float evaluatedAt)
        {
            Figure = figure;
            Outcome = outcome;
            AwardedScore = awardedScore;
            CountsAsNormalExit = countsAsNormalExit;
            GatePosition = gatePosition;
            EvaluatedAt = evaluatedAt;
        }

        public FigureClarityState Figure { get; }
        public FigureFrameExitOutcome Outcome { get; }
        public int AwardedScore { get; }
        public bool CountsAsNormalExit { get; }
        public Vector3 GatePosition { get; }
        public float EvaluatedAt { get; }
    }

    public static class FigureFrameExitRuleService
    {
        public static event Action<FigureFrameExitDecision> ExitEvaluated;

        public static FigureFrameExitDecision Evaluate(
            FigureClarityState figure,
            StainFrameExitConfig config,
            Vector3 gatePosition)
        {
            if (figure == null)
            {
                return default;
            }

            bool isFullStain =
                figure.CurrentLevel == FigureClarityLevel.Stain;

            FigureFrameExitDecision decision = isFullStain
                ? new FigureFrameExitDecision(
                    figure,
                    FigureFrameExitOutcome.StainSupportArrival,
                    config != null ? config.StainSupportArrivalScore : 0,
                    false,
                    gatePosition,
                    Time.time)
                : new FigureFrameExitDecision(
                    figure,
                    FigureFrameExitOutcome.NormalFigureExit,
                    config != null ? config.NormalFigureExitScore : 250,
                    true,
                    gatePosition,
                    Time.time);

            ExitEvaluated?.Invoke(decision);
            return decision;
        }
    }
}
