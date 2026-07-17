using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures
{
    public sealed class RestorationPoint : MonoBehaviour
    {
        [Header("Restoration")]
        [SerializeField, Range(0.05f, 1f)]
        private float restoredClarityLevel = 0.65f;

        [SerializeField] private Transform interactionPosition;

        private readonly HashSet<int> restoredFigureIds = new();

        public Transform InteractionPosition =>
            interactionPosition != null
                ? interactionPosition
                : transform;

        public float RestoredClarityLevel => restoredClarityLevel;

        public bool HasBeenUsedBy(FigureClarityState clarityState)
        {
            if (clarityState == null)
                return false;

            return restoredFigureIds.Contains(
                clarityState.GetInstanceID());
        }

        public bool NeedsRestoration(FigureClarityState clarityState)
        {
            if (clarityState == null)
                return false;

            float targetClarity =
                clarityState.MaximumClarity * restoredClarityLevel;

            return clarityState.CurrentClarity <
                   targetClarity - 0.01f;
        }

        public bool CanRestore(FigureClarityState clarityState)
        {
            if (clarityState == null)
                return false;

            if (HasBeenUsedBy(clarityState))
                return false;

            return NeedsRestoration(clarityState);
        }

        public bool TryRestore(FigureClarityState clarityState)
        {
            if (!CanRestore(clarityState))
                return false;

            float previousClarity =
                clarityState.CurrentClarity;

            clarityState.RestorePartial(restoredClarityLevel);

            if (clarityState.CurrentClarity <= previousClarity)
                return false;

            restoredFigureIds.Add(
                clarityState.GetInstanceID());

            return true;
        }

        public void ResetPoint()
        {
            restoredFigureIds.Clear();
        }
    }
}
