using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.StainSupport
{
    [DisallowMultipleComponent]
    public sealed class StainDirectionSignal : MonoBehaviour
    {
        private static readonly List<StainDirectionSignal>
            Signals = new List<StainDirectionSignal>();

        [SerializeField]
        private float expiresAt;

        [SerializeField]
        private float createdAt;

        [SerializeField]
        private Vector3 surfaceNormal = Vector3.up;

        [SerializeField]
        private Vector3 signalDirection = Vector3.forward;

        private Transform visualRoot;
        private Vector3 visualBaseScale = Vector3.one;
        private bool initialized;

        public static IReadOnlyList<StainDirectionSignal>
            ActiveSignals => Signals;
        public static event Action<StainDirectionSignal>
            SignalCreated;
        public float RemainingLifetime =>
            Mathf.Max(0f, expiresAt - Time.time);
        public Vector3 SurfaceNormal => surfaceNormal;
        public Vector3 SignalDirection => signalDirection;

        private void OnEnable()
        {
            if (!Signals.Contains(this))
            {
                Signals.Add(this);
            }
        }

        private void OnDisable()
        {
            Signals.Remove(this);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            float remaining = RemainingLifetime;

            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (visualRoot == null)
            {
                return;
            }

            float appear =
                Mathf.Clamp01(
                    (Time.time - createdAt) / 0.18f);
            float expiry =
                Mathf.Clamp01(remaining / 0.8f);
            float pulse =
                remaining <= 1.4f
                    ? 0.94f +
                      Mathf.Sin(Time.time * 14f) * 0.06f
                    : 1f;
            visualRoot.localScale =
                visualBaseScale *
                Mathf.Min(appear, expiry) *
                pulse;
        }

        public void Initialize(
            Vector3 point,
            Vector3 normal,
            Vector3 direction,
            StainDirectionSignalConfig config)
        {
            if (config == null)
            {
                Destroy(gameObject);
                return;
            }

            surfaceNormal =
                normal.sqrMagnitude > 0.001f
                    ? normal.normalized
                    : Vector3.up;
            signalDirection =
                Vector3.ProjectOnPlane(
                    direction,
                    surfaceNormal).normalized;

            if (signalDirection.sqrMagnitude < 0.001f)
            {
                signalDirection =
                    Vector3.Cross(
                        surfaceNormal,
                        Vector3.right).normalized;
            }

            if (signalDirection.sqrMagnitude < 0.001f)
            {
                signalDirection = Vector3.forward;
            }

            transform.position =
                point +
                surfaceNormal * config.SurfaceOffset;
            transform.rotation =
                Quaternion.LookRotation(
                    signalDirection,
                    surfaceNormal);
            createdAt = Time.time;
            expiresAt =
                Time.time + config.SignalLifetime;
            gameObject.layer = 2;

            CreateVisual(config);
            initialized = true;
            SignalCreated?.Invoke(this);
        }

        public void ExpireNow()
        {
            expiresAt = Time.time;

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            Destroy(gameObject);
        }

        private void CreateVisual(
            StainDirectionSignalConfig config)
        {
            GameObject root =
                new GameObject("DirectionSignalVisual");
            root.layer = 2;
            root.transform.SetParent(transform, false);
            visualRoot = root.transform;
            visualBaseScale = Vector3.one;

            float shaftLength =
                Mathf.Max(
                    0.2f,
                    config.ArrowLength -
                    config.ArrowHeadLength * 0.7f);
            CreatePart(
                "ArrowShaft",
                new Vector3(
                    0f,
                    0f,
                    -config.ArrowHeadLength * 0.22f),
                Quaternion.identity,
                new Vector3(
                    config.ArrowWidth,
                    0.035f,
                    shaftLength),
                config.SignalMaterial);

            float headForward =
                config.ArrowLength * 0.36f;
            float headSide =
                config.ArrowHeadLength * 0.24f;
            CreatePart(
                "ArrowHeadLeft",
                new Vector3(
                    -headSide,
                    0f,
                    headForward),
                Quaternion.Euler(0f, -43f, 0f),
                new Vector3(
                    config.ArrowWidth * 1.08f,
                    0.04f,
                    config.ArrowHeadLength),
                config.SignalMaterial);
            CreatePart(
                "ArrowHeadRight",
                new Vector3(
                    headSide,
                    0f,
                    headForward),
                Quaternion.Euler(0f, 43f, 0f),
                new Vector3(
                    config.ArrowWidth * 1.08f,
                    0.04f,
                    config.ArrowHeadLength),
                config.SignalMaterial);

            visualRoot.localScale = Vector3.zero;
        }

        private void CreatePart(
            string partName,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            GameObject part =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
            part.name = partName;
            part.layer = 2;
            part.transform.SetParent(visualRoot, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            Collider collider =
                part.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode =
                    ShadowCastingMode.On;
                renderer.receiveShadows = false;
            }
        }
    }
}
