using System.Collections;
using PaintedAlive.Figures;
using PaintedAlive.Painters.DraftVision;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.StainSupport.DraftVision
{
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class StainEarlyDraftVisionDebugEmitter : MonoBehaviour
    {
        [SerializeField] private StainEarlyDraftVisionConfig config;
        [SerializeField] private FigureClarityState clarityState;
        [SerializeField] private Camera figureCamera;
        [SerializeField] private LayerMask surfaceMask = ~0;

        [Header("Runtime - Read Only")]
[SerializeField] private bool isEmitting;

public bool IsEmitting => isEmitting;

private Coroutine emitRoutine;
private int nextSourceId = -35000;

        public void Configure(
            StainEarlyDraftVisionConfig visionConfig,
            FigureClarityState figureClarity,
            Camera roleCamera)
        {
            config = visionConfig;
            clarityState = figureClarity;
            figureCamera = roleCamera;
        }

        private void Awake()
        {
            clarityState ??= GetComponent<FigureClarityState>();
            if (figureCamera == null)
            {
                figureCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null ||
                !Keyboard.current.f10Key.wasPressedThisFrame)
            {
                return;
            }

            if (config == null || clarityState == null)
            {
                Debug.LogWarning(
                    "[M35.1 Debug] F10 alındı fakat M35 config veya FigureClarityState bağlantısı eksik.",
                    this);
                return;
            }

            if (clarityState.CurrentLevel != FigureClarityLevel.Stain)
            {
                Debug.Log(
                    "[M35 Debug] F10 taslağı yalnız tam Leke formunda görülebilir. " +
                    "Temiz/ara Netlik seviyesinde hiçbir özel çizgi gösterilmemesi beklenir.",
                    this);
                return;
            }

            if (emitRoutine != null)
            {
                StopCoroutine(emitRoutine);
            }

            StainEarlyDraftVisionController vision =
                GetComponent<StainEarlyDraftVisionController>();

            if (vision == null || !vision.isActiveAndEnabled)
            {
                Debug.LogWarning(
                    "[M35.1 Debug] F10 alındı fakat StainEarlyDraftVisionController aktif değil.",
                    this);
                return;
            }

            Debug.Log(
                "[M35.1 Debug] F10 Leke taslağı yayınlanıyor.",
                this);

            emitRoutine = StartCoroutine(EmitDraftRoutine());
        }

        private IEnumerator EmitDraftRoutine()
        {
            isEmitting = true;
            int sourceId = nextSourceId--;
            float startedAt = Time.time;
            float normalRevealAt = startedAt + config.EarlyLeadDuration;
            int pointCount = Mathf.Max(3, config.DebugPointCount);
            Vector3[] fullPath = BuildDebugPath(pointCount);

            float elapsed = 0f;
            while (elapsed < config.DebugDrawDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / config.DebugDrawDuration);
                int visiblePoints = Mathf.Clamp(
                    Mathf.CeilToInt(Mathf.Lerp(2, pointCount, normalized)),
                    2,
                    pointCount);

                var currentPoints = new Vector3[visiblePoints];
                for (int i = 0; i < visiblePoints; i++)
                {
                    currentPoints[i] = fullPath[i];
                }

                PainterDraftSignalHub.Publish(
                    new PainterDraftSignal(
                        sourceId,
                        currentPoints,
                        startedAt,
                        normalRevealAt));

                yield return null;
            }

            PainterDraftSignalHub.End(sourceId);
            isEmitting = false;
            emitRoutine = null;
        }

        private Vector3[] BuildDebugPath(int pointCount)
        {
            Camera cameraToUse = ResolveActiveCamera();
            Vector3 origin = transform.position + Vector3.up * 0.25f;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            if (cameraToUse != null)
            {
                forward = Vector3.ProjectOnPlane(
                    cameraToUse.transform.forward,
                    Vector3.up);
                right = Vector3.ProjectOnPlane(
                    cameraToUse.transform.right,
                    Vector3.up);
            }

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
            }

            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(Vector3.up, forward);
            }

            forward.Normalize();
            right.Normalize();

            var points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0f : i / (float)(pointCount - 1);
                float lateral = Mathf.Sin(t * Mathf.PI * 1.25f) * 1.15f;
                Vector3 candidate =
                    origin +
                    forward * Mathf.Lerp(1.5f, config.DebugDraftLength, t) +
                    right * lateral;

                Vector3 rayOrigin = candidate + Vector3.up * 5f;
                if (Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out RaycastHit hit,
                        12f,
                        surfaceMask,
                        QueryTriggerInteraction.Ignore))
                {
                    candidate = hit.point + hit.normal * 0.035f;
                }

                points[i] = candidate;
            }

            return points;
        }

        private Camera ResolveActiveCamera()
        {
            if (figureCamera != null && figureCamera.isActiveAndEnabled)
            {
                return figureCamera;
            }

            Camera main = Camera.main;
            if (main != null && main.isActiveAndEnabled)
            {
                figureCamera = main;
                return main;
            }

            return figureCamera;
        }

        private void OnDisable()
        {
            if (emitRoutine != null)
            {
                StopCoroutine(emitRoutine);
                emitRoutine = null;
            }

            isEmitting = false;
        }
    }
}
