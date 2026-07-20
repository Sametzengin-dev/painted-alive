using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureMotor))]
    public sealed class WatercolorFlowInteractor : MonoBehaviour
    {
        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField, Min(0.05f)]
        private float exposureInterval = 0.3f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private WatercolorFlowSurface currentSurface;

        [SerializeField, Range(0f, 1f)]
        private float currentInfluence;

        [SerializeField]
        private Vector3 currentFlowDirection;

        private float exposureTimer;

        public bool IsInWatercolor => currentSurface != null;
        public float CurrentInfluence => currentInfluence;
        public Vector3 CurrentFlowDirection => currentFlowDirection;

        private void Awake()
        {
            figureMotor ??= GetComponent<FigureMotor>();
            clarityState ??= GetComponent<FigureClarityState>();
        }

        private void Update()
        {
            currentSurface = null;
            currentInfluence = 0f;
            currentFlowDirection = Vector3.zero;

            if (figureMotor == null || !figureMotor.IsGrounded)
            {
                exposureTimer = 0f;
                return;
            }

            IReadOnlyList<WatercolorFlowSurface> surfaces =
                WatercolorFlowSurface.ActiveSurfaces;

            for (int i = surfaces.Count - 1; i >= 0; i--)
            {
                WatercolorFlowSurface surface = surfaces[i];

                if (surface == null ||
                    !surface.TrySampleFlow(
                        transform.position,
                        out Vector3 direction,
                        out float influence,
                        out _))
                {
                    continue;
                }

                if (influence <= currentInfluence)
                {
                    continue;
                }

                currentSurface = surface;
                currentInfluence = influence;
                currentFlowDirection = direction;
            }

            if (currentSurface == null)
            {
                exposureTimer = 0f;
                return;
            }

            Vector3 right = Vector3.Cross(
                Vector3.up,
                currentFlowDirection).normalized;
            float wobble =
                Mathf.Sin(
                    Time.time * 2.1f +
                    GetInstanceID() * 0.017f) * 0.08f;
            Vector3 slipperyDirection =
                (currentFlowDirection + right * wobble).normalized;

            figureMotor.AddExternalImpulse(
                slipperyDirection *
                currentSurface.FigureFlowAcceleration *
                currentInfluence *
                Time.deltaTime);

            exposureTimer += Time.deltaTime;

            if (clarityState != null &&
                exposureTimer >= exposureInterval)
            {
                float elapsed = exposureTimer;
                exposureTimer = 0f;
                clarityState.ApplyPaintExposure(
                    currentSurface.ClarityExposurePerSecond *
                    currentInfluence * elapsed,
                    FigurePaintRegion.Legs);
            }
        }

        private void OnValidate()
        {
            exposureInterval = Mathf.Max(0.05f, exposureInterval);
        }
    }
}
