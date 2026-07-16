using System;
using UnityEngine;

namespace PaintedAlive.Core.Prototypes
{
    public sealed class FigureProgressTracker : MonoBehaviour
    {
        [SerializeField] private Transform figure;
        [SerializeField] private RoutePath routePath;

        public event Action<float, float, float> ProgressChanged;

        public float FurthestDistance { get; private set; }
        public float NormalizedProgress { get; private set; }
        public float RemainingDistance { get; private set; }

        private void Update()
        {
            if (figure == null || routePath == null)
            {
                return;
            }

            bool evaluated = routePath.EvaluateProgress(
                figure.position,
                out float currentDistance,
                out _,
                out _);

            if (!evaluated)
            {
                return;
            }

            if (currentDistance <= FurthestDistance)
            {
                return;
            }

            FurthestDistance = currentDistance;

            NormalizedProgress =
                routePath.TotalLength > 0f
                    ? Mathf.Clamp01(
                        FurthestDistance /
                        routePath.TotalLength)
                    : 0f;

            RemainingDistance = Mathf.Max(
                0f,
                routePath.TotalLength -
                FurthestDistance);

            ProgressChanged?.Invoke(
                NormalizedProgress,
                FurthestDistance,
                RemainingDistance);
        }

        public void ResetProgress()
        {
            FurthestDistance = 0f;
            NormalizedProgress = 0f;

            RemainingDistance =
                routePath != null
                    ? routePath.TotalLength
                    : 0f;

            ProgressChanged?.Invoke(
                NormalizedProgress,
                FurthestDistance,
                RemainingDistance);
        }
    }
}
