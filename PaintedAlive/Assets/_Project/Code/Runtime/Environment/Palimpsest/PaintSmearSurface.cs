using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    public enum PaintSmearRuntimeState
    {
        Retracted = 0,
        Moving = 1,
        Extended = 2
    }

    [DisallowMultipleComponent]
    public sealed class PaintSmearSurface : MonoBehaviour,
        IPalimpsestPainterActivatable,
        IPalimpsestPersistentState,
        IPalimpsestResettable
    {
        private sealed class WorldPose
        {
            public Transform target;
            public Vector3 position;
            public Quaternion rotation;
        }

        [SerializeField] private string systemId;
        [SerializeField] private SkinnedMeshRenderer wetLayer;
        [SerializeField] private string blendShapeName = "Retracted";
        [SerializeField] private Transform extendedHelper;
        [SerializeField] private Transform retractedHelper;
        [SerializeField] private PalimpsestClearanceVolume clearanceVolume;
        [SerializeField] private Transform[] movingVisuals;
        [SerializeField] private Collider extendedCollider;
        [SerializeField] private Collider retractedCollider;
        [SerializeField] private Collider extendedRouteCollider;
        [SerializeField] private Collider retractedRouteCollider;
        [SerializeField, Min(0.1f)] private float transitionDuration = 1.35f;
        [SerializeField, Min(0f)] private float expectedTravelMetres = 7f;
        [SerializeField] private PaintSmearRuntimeState state = PaintSmearRuntimeState.Retracted;
        [SerializeField, Range(0f, 1f)] private float extendedProgress01;
        [SerializeField] private PalimpsestNetworkState networkState;
        [SerializeField] private int blendShapeIndex = -1;

        private readonly List<WorldPose> movingBasePoses = new List<WorldPose>();
        private WorldPose extendedColliderBasePose;
        private WorldPose retractedColliderBasePose;
        private Coroutine movementRoutine;

        public string PalimpsestSystemId => systemId;
        public string PersistentStateName =>
            state == PaintSmearRuntimeState.Extended
                ? "EXTENDED"
                : state == PaintSmearRuntimeState.Moving
                    ? "MOVING"
                    : "RETRACTED";
        public float PersistentProgress01 => extendedProgress01;
        public PaintSmearRuntimeState State => state;
        public bool ShapeKeyResolved => blendShapeIndex >= 0;
        public float AuthoredHelperTravel =>
            extendedHelper != null && retractedHelper != null
                ? Vector3.Distance(extendedHelper.position, retractedHelper.position)
                : -1f;
        public bool IsConfigured =>
            extendedHelper != null &&
            retractedHelper != null &&
            extendedCollider != null &&
            retractedCollider != null;

        public void Configure(
            string id,
            SkinnedMeshRenderer layer,
            Transform extendedState,
            Transform retractedState,
            PalimpsestClearanceVolume clearance,
            Transform[] movingTargets,
            Collider extendedStateCollider,
            Collider retractedStateCollider,
            Collider extendedRoute,
            Collider retractedRoute,
            float travelMetres,
            float duration,
            PalimpsestNetworkState stateHub)
        {
            systemId = id ?? string.Empty;
            wetLayer = layer;
            extendedHelper = extendedState;
            retractedHelper = retractedState;
            clearanceVolume = clearance;
            movingVisuals = movingTargets ?? Array.Empty<Transform>();
            extendedCollider = extendedStateCollider;
            retractedCollider = retractedStateCollider;
            extendedRouteCollider = extendedRoute;
            retractedRouteCollider = retractedRoute;
            expectedTravelMetres = Mathf.Max(0f, travelMetres);
            transitionDuration = Mathf.Max(0.1f, duration);
            networkState = stateHub;

            // The imported authored pose is EXTENDED (blendshape 0). Capture that
            // exact source pose before applying the runtime initial RETRACTED state.
            CaptureBasePoses(inferExtendedBaseFromRuntimeState: false);
            ResolveBlendShape();
            ApplyImmediate(PaintSmearRuntimeState.Retracted);
        }

        private void Awake()
        {
            // After the Editor setup has been saved, the scene/prefab starts in
            // RETRACTED state. Reconstruct the authored EXTENDED base pose instead
            // of recapturing the already-offset visuals (which would double the
            // 7 m helper delta on the first runtime activation).
            CaptureBasePoses(inferExtendedBaseFromRuntimeState: true);
            ResolveBlendShape();
            ApplyImmediate(state == PaintSmearRuntimeState.Extended
                ? PaintSmearRuntimeState.Extended
                : PaintSmearRuntimeState.Retracted);
        }

        public bool CanPainterActivate(out string reason)
        {
            if (!IsConfigured)
            {
                reason = "Smear binding incomplete";
                return false;
            }

            if (state == PaintSmearRuntimeState.Moving || movementRoutine != null)
            {
                reason = "Smear is already moving";
                return false;
            }

            if (clearanceVolume != null &&
                clearanceVolume.IsOccupiedByFigure())
            {
                reason =
                    clearanceVolume.DescribeLastOccupation(
                        "Authored Smear clearance");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryPainterActivate()
        {
            if (!CanPainterActivate(out _))
                return false;

            bool toExtended = state != PaintSmearRuntimeState.Extended;
            movementRoutine = StartCoroutine(MoveSmear(toExtended));
            return true;
        }

        private IEnumerator MoveSmear(bool toExtended)
        {
            state = PaintSmearRuntimeState.Moving;
            float from = toExtended ? 0f : 1f;
            float to = toExtended ? 1f : 0f;
            float elapsed = 0f;

            Collider movingSource = toExtended ? retractedCollider : extendedCollider;
            Collider movingDestination = toExtended ? extendedCollider : retractedCollider;
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
                extendedProgress01 = Mathf.Lerp(from, to, eased);
                ApplyVisualProgress(extendedProgress01);
                ApplyMovingColliderProgress(toExtended, extendedProgress01);
                yield return null;
            }

            ApplyImmediate(
                toExtended
                    ? PaintSmearRuntimeState.Extended
                    : PaintSmearRuntimeState.Retracted);
            movementRoutine = null;
            networkState?.PublishState(this);
        }

        private void ApplyImmediate(PaintSmearRuntimeState targetState)
        {
            bool extended = targetState == PaintSmearRuntimeState.Extended;
            state = targetState;
            extendedProgress01 = extended ? 1f : 0f;

            RestorePose(extendedColliderBasePose);
            RestorePose(retractedColliderBasePose);
            ApplyVisualProgress(extendedProgress01);

            SetColliderEnabled(extendedCollider, extended);
            SetColliderEnabled(retractedCollider, !extended);
            SetColliderEnabled(extendedRouteCollider, extended);
            SetColliderEnabled(retractedRouteCollider, !extended);
        }

        private void ApplyVisualProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            Vector3 retractedOffset = GetRetractedOffset();

            foreach (WorldPose pose in movingBasePoses)
            {
                if (pose.target == null)
                    continue;
                pose.target.position = pose.position + retractedOffset * (1f - progress);
                pose.target.rotation = pose.rotation;
            }

            if (wetLayer != null && blendShapeIndex >= 0)
            {
                wetLayer.SetBlendShapeWeight(
                    blendShapeIndex,
                    (1f - progress) * 100f);
            }
        }

        private void ApplyMovingColliderProgress(bool extending, float progress)
        {
            Vector3 retractedOffset = GetRetractedOffset();
            if (extending)
            {
                // Retracted endpoint proxy follows the moving paint until final swap.
                ApplyPoseOffset(
                    retractedColliderBasePose,
                    -retractedOffset * progress);
            }
            else
            {
                // Extended endpoint proxy follows the paint back toward retracted.
                ApplyPoseOffset(
                    extendedColliderBasePose,
                    retractedOffset * (1f - progress));
            }
        }

        private Vector3 GetRetractedOffset()
        {
            if (extendedHelper == null || retractedHelper == null)
                return Vector3.zero;
            return retractedHelper.position - extendedHelper.position;
        }

        private void CaptureBasePoses(bool inferExtendedBaseFromRuntimeState)
        {
            movingBasePoses.Clear();
            var seen = new HashSet<Transform>();
            Vector3 retractedOffset = GetRetractedOffset();
            bool currentVisualPoseIsRetracted =
                inferExtendedBaseFromRuntimeState &&
                state != PaintSmearRuntimeState.Extended;

            if (movingVisuals != null)
            {
                foreach (Transform target in movingVisuals)
                {
                    if (target == null || !seen.Add(target))
                        continue;

                    Vector3 authoredExtendedPosition = target.position;
                    if (currentVisualPoseIsRetracted)
                        authoredExtendedPosition -= retractedOffset;

                    movingBasePoses.Add(new WorldPose
                    {
                        target = target,
                        position = authoredExtendedPosition,
                        rotation = target.rotation
                    });
                }
            }

            // Endpoint collision proxies are already authored at their own fixed
            // endpoint poses, so unlike moving visuals they are never offset here.
            extendedColliderBasePose = extendedCollider != null
                ? CapturePose(extendedCollider.transform)
                : null;
            retractedColliderBasePose = retractedCollider != null
                ? CapturePose(retractedCollider.transform)
                : null;
        }

        private static WorldPose CapturePose(Transform target)
        {
            return target == null
                ? null
                : new WorldPose
                {
                    target = target,
                    position = target.position,
                    rotation = target.rotation
                };
        }

        private static void ApplyPoseOffset(WorldPose pose, Vector3 offset)
        {
            if (pose == null || pose.target == null)
                return;
            pose.target.position = pose.position + offset;
            pose.target.rotation = pose.rotation;
        }

        private static void RestorePose(WorldPose pose)
        {
            if (pose == null || pose.target == null)
                return;
            pose.target.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void ResolveBlendShape()
        {
            blendShapeIndex = -1;
            if (wetLayer == null || wetLayer.sharedMesh == null)
                return;
            blendShapeIndex = wetLayer.sharedMesh.GetBlendShapeIndex(blendShapeName);
        }

        private static void SetColliderEnabled(Collider collider, bool value)
        {
            if (collider != null)
                collider.enabled = value;
        }

        public void ApplyPersistentState(string stateName, float progress01)
        {
            if (movementRoutine != null)
            {
                StopCoroutine(movementRoutine);
                movementRoutine = null;
            }

            bool extended = string.Equals(
                stateName,
                "EXTENDED",
                StringComparison.OrdinalIgnoreCase) || progress01 >= 0.5f;
            ApplyImmediate(
                extended
                    ? PaintSmearRuntimeState.Extended
                    : PaintSmearRuntimeState.Retracted);
        }

        public void ResetPalimpsestState()
        {
            if (movementRoutine != null)
            {
                StopCoroutine(movementRoutine);
                movementRoutine = null;
            }
            ApplyImmediate(PaintSmearRuntimeState.Retracted);
        }
    }
}
