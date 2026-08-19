using System;
using UnityEngine;

namespace PaintedAlive.UI.UnifiedHUD
{
    [Serializable]
    public struct PrototypeUnifiedHudSnapshot
    {
        public string Role;
        public string MatchState;
        public float RemainingSeconds;

        public float Clarity;
        public float MaximumClarity;
        public float NormalizedClarity;
        public string ClarityLevel;

        public float Pigment;
        public float MaximumPigment;
        public float NormalizedPigment;

        public float NormalizedProgress;
        public float FurthestDistance;
        public int JourneyScore;

        public string EncounterPhase;
        public int EncounterIndex;

        public string FigureTool;
        public string PainterMaterial;
        public string ContextStatus;

        public bool IsPainter =>
            string.Equals(Role, "Painter", StringComparison.OrdinalIgnoreCase);

        public bool IsFullStain =>
            !string.IsNullOrWhiteSpace(ClarityLevel) &&
            ClarityLevel.IndexOf("Stain", StringComparison.OrdinalIgnoreCase) >= 0;

        public bool IsRunning =>
            string.Equals(MatchState, "Running", StringComparison.OrdinalIgnoreCase);
    }
}
