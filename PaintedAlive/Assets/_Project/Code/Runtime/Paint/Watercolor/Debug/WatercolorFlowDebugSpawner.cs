using PaintedAlive.Figures;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Watercolor
{
    [DisallowMultipleComponent]
    public sealed class WatercolorFlowDebugSpawner : MonoBehaviour
    {
        private const int MaximumSurfaceHits = 24;

        private readonly RaycastHit[] surfaceHits =
            new RaycastHit[MaximumSurfaceHits];

        [SerializeField]
        private FigureMotor targetFigure;

        [SerializeField]
        private WatercolorFlowSurface flowSurfacePrefab;

        [SerializeField]
        private LayerMask surfaceMask =
            Physics.DefaultRaycastLayers;

        [SerializeField]
        private bool enableF8Shortcut = true;

        [SerializeField, Min(0.5f)]
        private float forwardDistance = 2.6f;

        [SerializeField, Min(0.5f)]
        private float probeHeight = 2.5f;

        [SerializeField, Min(1f)]
        private float probeDistance = 6f;

        [SerializeField]
        private Color testColor =
            new Color(0.08f, 0.58f, 0.95f, 0.68f);

        private void Awake()
        {
            targetFigure ??= GetComponentInParent<FigureMotor>();
        }

        private void Update()
        {
            if (!enableF8Shortcut ||
                Keyboard.current == null ||
                !Keyboard.current.f8Key.wasPressedThisFrame)
            {
                return;
            }

            SpawnTestFlow();
        }

        [ContextMenu("Debug/Spawn Watercolor Flow")]
        public void SpawnTestFlow()
        {
            if (!Application.isPlaying ||
                targetFigure == null ||
                flowSurfacePrefab == null)
            {
                Debug.LogWarning(
                    "Watercolor test requires Play Mode, " +
                    "Target Figure and Flow Surface Prefab.",
                    this);
                return;
            }

            Transform figureTransform = targetFigure.transform;
            Vector3 planarForward = Vector3.ProjectOnPlane(
                figureTransform.forward,
                Vector3.up).normalized;
            Vector3 probeOrigin =
                figureTransform.position +
                planarForward * forwardDistance +
                Vector3.up * probeHeight;

            int hitCount = Physics.RaycastNonAlloc(
                new Ray(probeOrigin, Vector3.down),
                surfaceHits,
                probeDistance,
                surfaceMask,
                QueryTriggerInteraction.Ignore);

            RaycastHit bestHit = default;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = surfaceHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearestDistance)
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
                Debug.LogWarning(
                    "Watercolor test could not find ground " +
                    "in front of the Figure.",
                    this);
                return;
            }

            WatercolorFlowSurface created = Instantiate(
                flowSurfacePrefab,
                bestHit.point + bestHit.normal * 0.03f,
                Quaternion.identity);
            created.name = "WatercolorFlow_Runtime";
            created.Initialize(
                Vector3.ProjectOnPlane(
                    planarForward,
                    bestHit.normal).normalized,
                testColor,
                100f);
        }

        private void OnValidate()
        {
            forwardDistance = Mathf.Max(0.5f, forwardDistance);
            probeHeight = Mathf.Max(0.5f, probeHeight);
            probeDistance = Mathf.Max(1f, probeDistance);
        }
    }
}
