using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    public enum CanvasFoldRuntimeState
    {
        Open = 0,
        Transitioning = 1,
        Folded = 2
    }

    [DisallowMultipleComponent]
    public sealed class CanvasFoldSystem : MonoBehaviour,
        IPalimpsestPainterActivatable,
        IPalimpsestPersistentState,
        IPalimpsestResettable
    {
        private sealed class PivotPose
        {
            public Transform target;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        [SerializeField] private string systemId = "FOLD_A";
        [SerializeField] private string openStateLabel = "OPEN";
        [SerializeField] private string foldedStateLabel = "FOLDED";
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform openStateHelper;
        [SerializeField] private Transform foldedStateHelper;
        [SerializeField] private PalimpsestClearanceVolume clearanceVolume;
        [SerializeField] private Transform[] movingVisuals;
        [SerializeField] private Collider openMovingCollider;
        [SerializeField] private Collider foldedMovingCollider;
        [SerializeField] private Collider openRouteCollider;
        [SerializeField] private Collider foldedRouteCollider;
        [SerializeField, Range(1f, 120f)] private float authoredAngleDegrees = 65f;
        [SerializeField, Min(0.1f)] private float transitionDuration = 1.15f;
        [SerializeField] private CanvasFoldRuntimeState state = CanvasFoldRuntimeState.Open;
        [SerializeField, Range(0f, 1f)] private float transitionProgress;
        [SerializeField] private PalimpsestNetworkState networkState;

        private readonly List<PivotPose> visualPoses = new List<PivotPose>();
        private PivotPose openColliderPose;
        private PivotPose foldedColliderPose;
        private Coroutine transitionRoutine;

        public string PalimpsestSystemId => systemId;
        public string PersistentStateName =>
            state == CanvasFoldRuntimeState.Folded
                ? foldedStateLabel
                : state == CanvasFoldRuntimeState.Transitioning
                    ? "TRANSITIONING"
                    : openStateLabel;
        public float PersistentProgress01 => transitionProgress;
        public CanvasFoldRuntimeState State => state;
        public bool IsConfigured =>
            pivot != null &&
            openStateHelper != null &&
            foldedStateHelper != null &&
            openMovingCollider != null &&
            foldedMovingCollider != null;

        public void Configure(
            string id,
            string openLabel,
            string foldedLabel,
            Transform pivotTransform,
            Transform openHelper,
            Transform foldedHelper,
            PalimpsestClearanceVolume clearance,
            Transform[] visuals,
            Collider openCollider,
            Collider foldedCollider,
            Collider openRoute,
            Collider foldedRoute,
            float authoredAngle,
            float duration,
            PalimpsestNetworkState stateHub)
        {
            systemId = id ?? string.Empty;
            openStateLabel = string.IsNullOrWhiteSpace(openLabel) ? "OPEN" : openLabel;
            foldedStateLabel = string.IsNullOrWhiteSpace(foldedLabel) ? "FOLDED" : foldedLabel;
            pivot = pivotTransform;
            openStateHelper = openHelper;
            foldedStateHelper = foldedHelper;
            clearanceVolume = clearance;
            movingVisuals = visuals ?? Array.Empty<Transform>();
            openMovingCollider = openCollider;
            foldedMovingCollider = foldedCollider;
            openRouteCollider = openRoute;
            foldedRouteCollider = foldedRoute;
            authoredAngleDegrees = Mathf.Abs(authoredAngle);
            transitionDuration = Mathf.Max(0.1f, duration);
            networkState = stateHub;

            CapturePoses();
            ApplyImmediate(CanvasFoldRuntimeState.Open);
        }

        private void Awake()
        {
            CapturePoses();
        }

        public bool CanPainterActivate(out string reason)
        {
            if (!IsConfigured)
            {
                reason = "Fold binding is incomplete";
                return false;
            }

            if (state == CanvasFoldRuntimeState.Transitioning || transitionRoutine != null)
            {
                reason = "Fold is already transitioning";
                return false;
            }

            if (clearanceVolume != null &&
                clearanceVolume.IsOccupiedByFigure())
            {
                reason =
                    clearanceVolume.DescribeLastOccupation(
                        "Authored swept clearance");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryPainterActivate()
        {
            if (!CanPainterActivate(out _))
                return false;

            bool toFolded = state != CanvasFoldRuntimeState.Folded;
            transitionRoutine = StartCoroutine(Transition(toFolded));
            return true;
        }

        private IEnumerator Transition(bool toFolded)
        {
            state = CanvasFoldRuntimeState.Transitioning;
            float fromAngle = toFolded ? 0f : authoredAngleDegrees;
            float toAngle = toFolded ? authoredAngleDegrees : 0f;
            float elapsed = 0f;

            Collider movingSource = toFolded
                ? openMovingCollider
                : foldedMovingCollider;
            Collider movingDestination = toFolded
                ? foldedMovingCollider
                : openMovingCollider;

            SetColliderEnabled(movingSource, true);
            SetColliderEnabled(movingDestination, false);

            while (elapsed < transitionDuration)
            {
                if (clearanceVolume != null && clearanceVolume.IsOccupiedByFigure())
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = t * t * (3f - 2f * t);
                float angle = Mathf.Lerp(fromAngle, toAngle, eased);
                transitionProgress = toFolded ? t : 1f - t;

                ApplyVisualAngle(angle);
                ApplyMovingColliderPose(toFolded, angle);
                yield return null;
            }

            ApplyImmediate(
                toFolded
                    ? CanvasFoldRuntimeState.Folded
                    : CanvasFoldRuntimeState.Open);
            transitionRoutine = null;
            networkState?.PublishState(this);
        }

        private void ApplyImmediate(CanvasFoldRuntimeState targetState)
        {
            bool folded = targetState == CanvasFoldRuntimeState.Folded;
            state = targetState;
            transitionProgress = folded ? 1f : 0f;

            ApplyVisualAngle(folded ? authoredAngleDegrees : 0f);
            RestorePose(openColliderPose);
            RestorePose(foldedColliderPose);

            SetColliderEnabled(openMovingCollider, !folded);
            SetColliderEnabled(foldedMovingCollider, folded);
            SetColliderEnabled(openRouteCollider, !folded);
            SetColliderEnabled(foldedRouteCollider, folded);
        }

        private void ApplyVisualAngle(float angleDegrees)
        {
            if (pivot == null)
                return;

            Quaternion delta = Quaternion.AngleAxis(angleDegrees, Vector3.right);
            foreach (PivotPose pose in visualPoses)
                ApplyPivotPose(pose, delta);
        }

        private void ApplyMovingColliderPose(bool folding, float visualAngle)
        {
            if (pivot == null)
                return;

            if (folding)
            {
                Quaternion delta = Quaternion.AngleAxis(visualAngle, Vector3.right);
                ApplyPivotPose(openColliderPose, delta);
            }
            else
            {
                float relativeFromFolded = visualAngle - authoredAngleDegrees;
                Quaternion delta = Quaternion.AngleAxis(relativeFromFolded, Vector3.right);
                ApplyPivotPose(foldedColliderPose, delta);
            }
        }

        private void CapturePoses()
        {
            if (pivot == null)
                return;

            visualPoses.Clear();
            var seen = new HashSet<Transform>();
            if (movingVisuals != null)
            {
                foreach (Transform target in movingVisuals)
                {
                    if (target == null || target == pivot || !seen.Add(target))
                        continue;
                    visualPoses.Add(CapturePose(target));
                }
            }

            openColliderPose = openMovingCollider != null
                ? CapturePose(openMovingCollider.transform)
                : null;
            foldedColliderPose = foldedMovingCollider != null
                ? CapturePose(foldedMovingCollider.transform)
                : null;
        }

        private PivotPose CapturePose(Transform target)
        {
            return new PivotPose
            {
                target = target,
                localPosition = pivot.InverseTransformPoint(target.position),
                localRotation = Quaternion.Inverse(pivot.rotation) * target.rotation
            };
        }

        private void ApplyPivotPose(PivotPose pose, Quaternion delta)
        {
            if (pose == null || pose.target == null || pivot == null)
                return;

            pose.target.position = pivot.TransformPoint(delta * pose.localPosition);
            pose.target.rotation = pivot.rotation * delta * pose.localRotation;
        }

        private void RestorePose(PivotPose pose)
        {
            if (pose == null || pose.target == null || pivot == null)
                return;

            pose.target.position = pivot.TransformPoint(pose.localPosition);
            pose.target.rotation = pivot.rotation * pose.localRotation;
        }

        private static void SetColliderEnabled(Collider collider, bool enabledState)
        {
            if (collider != null)
                collider.enabled = enabledState;
        }

        public void ApplyPersistentState(string stateName, float progress01)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            bool folded = string.Equals(
                stateName,
                foldedStateLabel,
                StringComparison.OrdinalIgnoreCase);
            ApplyImmediate(
                folded
                    ? CanvasFoldRuntimeState.Folded
                    : CanvasFoldRuntimeState.Open);
        }

        public void ResetPalimpsestState()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
            ApplyImmediate(CanvasFoldRuntimeState.Open);
        }
    }
}
