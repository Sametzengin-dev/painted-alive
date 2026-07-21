using PaintedAlive.Figures;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink
{
    [DisallowMultipleComponent]
    public sealed class InkDebugSpawner : MonoBehaviour
    {
        private const int MaximumSurfaceHits = 24;

        private readonly RaycastHit[] surfaceHits = new RaycastHit[MaximumSurfaceHits];

        [SerializeField]
        private FigureMotor targetFigure;

        [SerializeField]
        private InkSystemManager inkManager;

        [SerializeField]
        private LayerMask surfaceMask = Physics.DefaultRaycastLayers;

        [SerializeField]
        private bool enableF9Shortcut = true;

        [SerializeField, Min(0.5f)]
        private float forwardDistance = 3f;

        [SerializeField, Min(0.5f)]
        private float probeHeight = 2.5f;

        [SerializeField, Min(1f)]
        private float probeDistance = 6f;

        private bool missingSurfaceWarningIssued;
        private bool creatureLimitWarningIssued;

        public FigureMotor TargetFigure => targetFigure;
        public InkSystemManager InkManager => inkManager;

        private void Awake()
        {
            targetFigure ??= GetComponentInParent<FigureMotor>();
            inkManager ??= InkSystemManager.ActiveInstance;
        }

        private void Update()
        {
            if (!enableF9Shortcut ||
                Keyboard.current == null ||
                !Keyboard.current.f9Key.wasPressedThisFrame ||
                IsEditingText())
            {
                return;
            }

            SpawnTestInk();
        }

        [ContextMenu("Debug/Spawn Ink and Lekebacak")]
        public void SpawnTestInk()
        {
            inkManager ??= InkSystemManager.ActiveInstance;

            if (!Application.isPlaying || targetFigure == null || inkManager == null)
            {
                Debug.LogWarning(
                    "Ink test requires Play Mode, a Target Figure and one Ink Manager.",
                    this);
                return;
            }

            Transform figureTransform = targetFigure.transform;
            Vector3 planarForward = Vector3.ProjectOnPlane(
                figureTransform.forward,
                Vector3.up).normalized;

            if (planarForward.sqrMagnitude < 0.001f)
            {
                planarForward = Vector3.forward;
            }

            Vector3 probeOrigin =
                figureTransform.position +
                planarForward * forwardDistance +
                Vector3.up * probeHeight;
            int hitCount = Physics.RaycastNonAlloc(
                probeOrigin,
                Vector3.down,
                surfaceHits,
                probeDistance,
                surfaceMask,
                QueryTriggerInteraction.Ignore);
            RaycastHit bestHit = default;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = surfaceHits[i];

                if (hit.collider == null || hit.distance >= nearestDistance)
                {
                    continue;
                }

                FigureMotor hitFigure =
                    hit.collider.GetComponentInParent<FigureMotor>();

                if (hitFigure == targetFigure)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            if (bestHit.collider == null)
            {
                if (!missingSurfaceWarningIssued)
                {
                    Debug.LogWarning(
                        "M15 F9 test could not find a valid surface " +
                        "approximately 3 metres in front of the Figure.",
                        this);
                    missingSurfaceWarningIssued = true;
                }

                return;
            }

            missingSurfaceWarningIssued = false;

            if (!inkManager.TrySpawnLekebacak(
                    bestHit.point,
                    bestHit.normal,
                    planarForward,
                    out _,
                    out _))
            {
                if (!creatureLimitWarningIssued)
                {
                    Debug.LogWarning(
                        "M15 F9 spawn was rejected. The Ink creature limit " +
                        "may be full or the manager may be disabled.",
                        this);
                    creatureLimitWarningIssued = true;
                }

                return;
            }

            creatureLimitWarningIssued = false;
        }

        private static bool IsEditingText()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null
                ? eventSystem.currentSelectedGameObject
                : null;

            if (selected == null)
            {
                return false;
            }

            return selected.GetComponent("TMP_InputField") != null ||
                   selected.GetComponent("InputField") != null;
        }

        private void OnValidate()
        {
            forwardDistance = Mathf.Max(0.5f, forwardDistance);
            probeHeight = Mathf.Max(0.5f, probeHeight);
            probeDistance = Mathf.Max(1f, probeDistance);
        }
    }
}
