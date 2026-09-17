using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    public enum UnderpaintingRuntimeState
    {
        Current = 0,
        Transitioning = 1,
        Revealed = 2
    }

    [DisallowMultipleComponent]
    public sealed class UnderpaintingRevealSystem : MonoBehaviour,
        IPalimpsestPainterActivatable,
        IPalimpsestPersistentState,
        IPalimpsestResettable
    {
        private sealed class TransformPose
        {
            public Transform target;
            public Vector3 position;
            public Quaternion rotation;
        }

        [SerializeField] private string systemId;
        [SerializeField] private Transform currentHelper;
        [SerializeField] private Transform revealedHelper;
        [SerializeField] private Transform revealReference;
        [SerializeField] private Transform[] currentVisuals;
        [SerializeField] private Transform[] revealedVisuals;
        [SerializeField] private Collider currentCollider;
        [SerializeField] private Collider revealedCollider;
        [SerializeField, Min(0.1f)] private float transitionDuration = 1.1f;
        [SerializeField] private UnderpaintingRuntimeState state;
        [SerializeField, Range(0f, 1f)] private float progress01;
        [SerializeField] private PalimpsestNetworkState networkState;

        private readonly List<TransformPose> currentBasePoses =
            new List<TransformPose>();
        private Coroutine revealRoutine;

        public string PalimpsestSystemId => systemId;
        public string PersistentStateName =>
            state == UnderpaintingRuntimeState.Revealed
                ? "REVEALED"
                : state == UnderpaintingRuntimeState.Transitioning
                    ? "TRANSITIONING"
                    : "CURRENT";
        public float PersistentProgress01 => progress01;
        public UnderpaintingRuntimeState State => state;
        public bool IsConfigured =>
            currentHelper != null &&
            revealedHelper != null &&
            currentCollider != null &&
            revealedCollider != null;

        public void Configure(
            string id,
            Transform currentStateHelper,
            Transform revealedStateHelper,
            Transform revealHelper,
            Transform[] currentStateVisuals,
            Transform[] revealedStateVisuals,
            Collider currentStateCollider,
            Collider revealedStateCollider,
            float duration,
            PalimpsestNetworkState stateHub)
        {
            systemId = id ?? string.Empty;
            currentHelper = currentStateHelper;
            revealedHelper = revealedStateHelper;
            revealReference = revealHelper;
            currentVisuals = currentStateVisuals ?? Array.Empty<Transform>();
            revealedVisuals = revealedStateVisuals ?? Array.Empty<Transform>();
            currentCollider = currentStateCollider;
            revealedCollider = revealedStateCollider;
            transitionDuration = Mathf.Max(0.1f, duration);
            networkState = stateHub;
            CaptureBasePoses();
            ApplyImmediate(UnderpaintingRuntimeState.Current);
        }

        private void Awake()
        {
            CaptureBasePoses();
        }

        public bool CanPainterActivate(out string reason)
        {
            if (!IsConfigured)
            {
                reason = "Underpainting binding incomplete";
                return false;
            }
            if (state != UnderpaintingRuntimeState.Current || revealRoutine != null)
            {
                reason = "Underpainting already revealed or transitioning";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public bool TryPainterActivate()
        {
            if (!CanPainterActivate(out _))
                return false;
            revealRoutine = StartCoroutine(Reveal());
            return true;
        }

        private IEnumerator Reveal()
        {
            state = UnderpaintingRuntimeState.Transitioning;
            Vector3 delta = revealedHelper.position - currentHelper.position;
            float elapsed = 0f;
            bool revealedVisualsEnabled = false;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = t * t * (3f - 2f * t);
                progress01 = t;

                foreach (TransformPose pose in currentBasePoses)
                {
                    if (pose.target == null)
                        continue;
                    pose.target.position = pose.position + delta * eased;
                    pose.target.rotation = pose.rotation;
                }

                if (!revealedVisualsEnabled && t >= 0.55f)
                {
                    SetActive(revealedVisuals, true);
                    revealedVisualsEnabled = true;
                }

                yield return null;
            }

            ApplyImmediate(UnderpaintingRuntimeState.Revealed);
            revealRoutine = null;
            networkState?.PublishState(this);
        }

        private void ApplyImmediate(UnderpaintingRuntimeState targetState)
        {
            bool revealed = targetState == UnderpaintingRuntimeState.Revealed;
            state = targetState;
            progress01 = revealed ? 1f : 0f;

            RestoreCurrentVisualPoses();
            SetActive(currentVisuals, !revealed);
            SetActive(revealedVisuals, revealed);
            if (currentCollider != null) currentCollider.enabled = !revealed;
            if (revealedCollider != null) revealedCollider.enabled = revealed;
        }

        private void CaptureBasePoses()
        {
            currentBasePoses.Clear();
            if (currentVisuals == null)
                return;

            var seen = new HashSet<Transform>();
            foreach (Transform target in currentVisuals)
            {
                if (target == null || !seen.Add(target))
                    continue;
                currentBasePoses.Add(new TransformPose
                {
                    target = target,
                    position = target.position,
                    rotation = target.rotation
                });
            }
        }

        private void RestoreCurrentVisualPoses()
        {
            foreach (TransformPose pose in currentBasePoses)
            {
                if (pose.target == null)
                    continue;
                pose.target.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        private static void SetActive(Transform[] targets, bool value)
        {
            if (targets == null)
                return;
            foreach (Transform target in targets)
            {
                if (target != null && target.gameObject.activeSelf != value)
                    target.gameObject.SetActive(value);
            }
        }

        public void ApplyPersistentState(string stateName, float snapshotProgress01)
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }

            bool revealed = string.Equals(
                stateName,
                "REVEALED",
                StringComparison.OrdinalIgnoreCase) || snapshotProgress01 >= 0.5f;
            ApplyImmediate(
                revealed
                    ? UnderpaintingRuntimeState.Revealed
                    : UnderpaintingRuntimeState.Current);
        }

        public void ResetPalimpsestState()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
            ApplyImmediate(UnderpaintingRuntimeState.Current);
        }
    }
}
