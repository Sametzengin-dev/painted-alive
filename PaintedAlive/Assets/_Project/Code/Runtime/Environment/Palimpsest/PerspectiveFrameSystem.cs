using System;
using System.Collections;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    public enum PerspectiveRuntimeState
    {
        Misaligned = 0,
        Aligning = 1,
        Aligned = 2
    }

    [DisallowMultipleComponent]
    public sealed class PerspectiveFrameSystem : MonoBehaviour,
        IPalimpsestPainterActivatable,
        IPalimpsestPersistentState,
        IPalimpsestResettable
    {
        [SerializeField] private string systemId = "PERSPECTIVE_EXIT";
        [SerializeField] private Transform frameReference;
        [SerializeField] private Transform framePivot;
        [SerializeField] private Transform misalignedState;
        [SerializeField] private Transform alignedState;
        [SerializeField] private Transform viewReference;
        [SerializeField] private Transform targetA;
        [SerializeField] private Transform targetB;
        [SerializeField] private PerspectiveTraversalBridge traversalBridge;
        [SerializeField, Min(0.1f)] private float alignmentDuration = 1.1f;
        [SerializeField, Min(0.1f)] private float alignmentToleranceDegrees = 1.5f;
        [SerializeField] private PerspectiveRuntimeState state = PerspectiveRuntimeState.Misaligned;
        [SerializeField, Range(0f, 1f)] private float alignmentProgress;
        [SerializeField] private bool lastProjectionValidation;
        [SerializeField] private PalimpsestNetworkState networkState;

        private Coroutine alignmentRoutine;

        public string PalimpsestSystemId => systemId;
        public string PersistentStateName =>
            state == PerspectiveRuntimeState.Aligned
                ? "ALIGNED"
                : state == PerspectiveRuntimeState.Aligning
                    ? "ALIGNING"
                    : "MISALIGNED";
        public float PersistentProgress01 => alignmentProgress;
        public PerspectiveRuntimeState State => state;
        public bool LastProjectionValidation => lastProjectionValidation;
        public bool IsConfigured =>
            frameReference != null &&
            framePivot != null &&
            misalignedState != null &&
            alignedState != null &&
            viewReference != null &&
            targetA != null &&
            targetB != null &&
            traversalBridge != null;

        public void Configure(
            string id,
            Transform frame,
            Transform pivot,
            Transform misaligned,
            Transform aligned,
            Transform view,
            Transform firstTarget,
            Transform secondTarget,
            PerspectiveTraversalBridge bridge,
            float toleranceDegrees,
            float duration,
            PalimpsestNetworkState stateHub)
        {
            systemId = id ?? "PERSPECTIVE_EXIT";
            frameReference = frame;
            framePivot = pivot;
            misalignedState = misaligned;
            alignedState = aligned;
            viewReference = view;
            targetA = firstTarget;
            targetB = secondTarget;
            traversalBridge = bridge;
            alignmentToleranceDegrees = Mathf.Max(0.1f, toleranceDegrees);
            alignmentDuration = Mathf.Max(0.1f, duration);
            networkState = stateHub;
            ApplyImmediate(PerspectiveRuntimeState.Misaligned);
        }

        public bool CanPainterActivate(out string reason)
        {
            if (!IsConfigured)
            {
                reason = "Perspective binding incomplete";
                return false;
            }
            if (state != PerspectiveRuntimeState.Misaligned || alignmentRoutine != null)
            {
                reason = "Perspective exit is already aligned or aligning";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public bool TryPainterActivate()
        {
            if (!CanPainterActivate(out _))
                return false;
            alignmentRoutine = StartCoroutine(Align());
            return true;
        }

        private IEnumerator Align()
        {
            state = PerspectiveRuntimeState.Aligning;
            float elapsed = 0f;

            Vector3 startPosition = misalignedState.position;
            Quaternion startRotation = misalignedState.rotation;
            Vector3 endPosition = alignedState.position;
            Quaternion endRotation = alignedState.rotation;

            while (elapsed < alignmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / alignmentDuration);
                float eased = t * t * (3f - 2f * t);
                alignmentProgress = t;

                frameReference.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, endPosition, eased),
                    Quaternion.Slerp(startRotation, endRotation, eased));
                traversalBridge.SetAlignmentProgress(eased);
                yield return null;
            }

            frameReference.SetPositionAndRotation(endPosition, endRotation);
            alignmentProgress = 1f;
            lastProjectionValidation = ValidateAuthoredProjection();

            if (lastProjectionValidation)
            {
                state = PerspectiveRuntimeState.Aligned;
                traversalBridge.SetAlignmentProgress(1f);
                traversalBridge.SetTraversable(true);
            }
            else
            {
                state = PerspectiveRuntimeState.Misaligned;
                alignmentProgress = 0f;
                frameReference.SetPositionAndRotation(
                    misalignedState.position,
                    misalignedState.rotation);
                traversalBridge.SetAlignmentProgress(0f);
                traversalBridge.SetTraversable(false);
                Debug.LogWarning(
                    "Palimpsest Perspective Exit did not pass authored world-space alignment validation; bridge remains disabled.",
                    this);
            }

            alignmentRoutine = null;
            networkState?.PublishState(this);
        }

        public bool ValidateAuthoredProjection()
        {
            if (!IsConfigured)
                return false;

            float frameAngle = Quaternion.Angle(
                frameReference.rotation,
                alignedState.rotation);
            if (frameAngle > alignmentToleranceDegrees)
                return false;

            Vector3 a = targetA.position - viewReference.position;
            Vector3 b = targetB.position - viewReference.position;
            if (a.sqrMagnitude < 0.01f || b.sqrMagnitude < 0.01f)
                return false;

            float separation = Vector3.Angle(a, b);
            if (separation <= 0.01f || separation >= 120f)
                return false;

            // The authoritative solution is the exported aligned-state transform.
            // View/target world positions are included here so validity cannot be
            // reduced to matching object origins alone.
            Vector3 midpoint = (targetA.position + targetB.position) * 0.5f;
            Vector3 viewToMidpoint = midpoint - viewReference.position;
            Vector3 frameToMidpoint = midpoint - framePivot.position;
            if (viewToMidpoint.sqrMagnitude < 0.01f || frameToMidpoint.sqrMagnitude < 0.01f)
                return false;

            return true;
        }

        private void ApplyImmediate(PerspectiveRuntimeState targetState)
        {
            bool aligned = targetState == PerspectiveRuntimeState.Aligned;
            state = targetState;
            alignmentProgress = aligned ? 1f : 0f;

            if (frameReference != null)
            {
                Transform source = aligned ? alignedState : misalignedState;
                if (source != null)
                    frameReference.SetPositionAndRotation(source.position, source.rotation);
            }

            if (traversalBridge != null)
            {
                traversalBridge.SetAlignmentProgress(aligned ? 1f : 0f);
                traversalBridge.SetTraversable(aligned && ValidateAuthoredProjection());
            }

            lastProjectionValidation = aligned && ValidateAuthoredProjection();
        }

        public void ApplyPersistentState(string stateName, float progress01)
        {
            if (alignmentRoutine != null)
            {
                StopCoroutine(alignmentRoutine);
                alignmentRoutine = null;
            }

            bool aligned = string.Equals(
                stateName,
                "ALIGNED",
                StringComparison.OrdinalIgnoreCase) || progress01 >= 0.999f;
            ApplyImmediate(
                aligned
                    ? PerspectiveRuntimeState.Aligned
                    : PerspectiveRuntimeState.Misaligned);
        }

        public void ResetPalimpsestState()
        {
            if (alignmentRoutine != null)
            {
                StopCoroutine(alignmentRoutine);
                alignmentRoutine = null;
            }
            ApplyImmediate(PerspectiveRuntimeState.Misaligned);
        }
    }
}
