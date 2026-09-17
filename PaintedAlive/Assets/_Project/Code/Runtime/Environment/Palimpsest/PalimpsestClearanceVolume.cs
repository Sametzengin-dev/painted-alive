using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    /// <summary>
    /// Exact authored Figure-safety query for Fold / Smear swept volumes.
    ///
    /// Important: BoxCollider.bounds is a WORLD AXIS-ALIGNED bounding box.
    /// Using bounds + Quaternion.identity for a rotated authored clearance
    /// inflates the safe volume a second time and produces false positives.
    ///
    /// This class queries the actual oriented BoxCollider instead.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PalimpsestClearanceVolume : MonoBehaviour,
        IPalimpsestResettable
    {
        private const int OverlapBufferSize = 48;

        [SerializeField] private BoxCollider triggerVolume;
        [SerializeField] private string bindingId;

        [Header("Blocked-volume runtime feedback")]
        [SerializeField] private bool visualizeWhenBlocked = true;
        [SerializeField, Min(0.5f)] private float blockedOutlineSeconds = 2.75f;
        [SerializeField, Min(0.01f)] private float blockedOutlineWidth = 0.055f;

        [Header("Runtime - Read Only")]
        [SerializeField] private FigureMotor lastBlockingFigure;
        [SerializeField] private int lastOverlapCount;
        [SerializeField] private Vector3 lastQueryCenter;
        [SerializeField] private Vector3 lastQueryHalfExtents;
        [SerializeField] private Quaternion lastQueryRotation = Quaternion.identity;

        private readonly Collider[] overlapBuffer =
            new Collider[OverlapBufferSize];

        private LineRenderer outline;
        private Material outlineMaterial;
        private float outlineVisibleUntil;

        public string BindingId => bindingId;
        public BoxCollider TriggerVolume => triggerVolume;
        public bool IsOccupied => IsOccupiedByFigure();
        public FigureMotor LastBlockingFigure => lastBlockingFigure;
        public int LastOverlapCount => lastOverlapCount;
        public Vector3 LastQueryCenter => lastQueryCenter;
        public Vector3 LastQueryHalfExtents => lastQueryHalfExtents;
        public Quaternion LastQueryRotation => lastQueryRotation;
        public bool UsesExactOrientedBoxQuery => true;

        public void Configure(string id, BoxCollider volume)
        {
            bindingId = id ?? string.Empty;
            triggerVolume = volume;

            if (triggerVolume != null)
                triggerVolume.isTrigger = true;
        }

        private void Awake()
        {
            if (triggerVolume == null)
                triggerVolume = GetComponent<BoxCollider>();

            if (triggerVolume != null)
                triggerVolume.isTrigger = true;
        }

        private void Update()
        {
            if (outline != null &&
                outline.enabled &&
                Time.unscaledTime > outlineVisibleUntil)
            {
                outline.enabled = false;
            }
        }

        public bool IsOccupiedByFigure()
        {
            return TryGetBlockingFigure(out _);
        }

        public bool TryGetBlockingFigure(out FigureMotor figure)
        {
            figure = null;
            lastBlockingFigure = null;
            lastOverlapCount = 0;

            if (triggerVolume == null ||
                !triggerVolume.enabled ||
                !triggerVolume.gameObject.activeInHierarchy)
            {
                return false;
            }

            GetExactWorldBox(
                out Vector3 center,
                out Vector3 halfExtents,
                out Quaternion rotation);

            lastQueryCenter = center;
            lastQueryHalfExtents = halfExtents;
            lastQueryRotation = rotation;

            int count = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                overlapBuffer,
                rotation,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            lastOverlapCount = count;

            for (int index = 0;
                 index < count && index < overlapBuffer.Length;
                 index++)
            {
                Collider overlap = overlapBuffer[index];

                if (overlap == null ||
                    overlap == triggerVolume)
                {
                    continue;
                }

                FigureMotor candidate =
                    overlap.GetComponentInParent<FigureMotor>();

                if (candidate == null ||
                    !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                figure = candidate;
                lastBlockingFigure = candidate;

                if (visualizeWhenBlocked)
                    ShowBlockedOutline();

                return true;
            }

            return false;
        }

        public string DescribeLastOccupation(string prefix)
        {
            string label = string.IsNullOrWhiteSpace(prefix)
                ? "Clearance"
                : prefix;

            if (lastBlockingFigure == null)
            {
                return label + " is occupied";
            }

            float distance = Vector3.Distance(
                lastBlockingFigure.transform.position,
                lastQueryCenter);

            return
                $"{label} is occupied by Figure " +
                $"'{lastBlockingFigure.name}' " +
                $"({bindingId}, center distance {distance:0.0}m)";
        }

        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (triggerVolume == null)
                return false;

            Transform target = triggerVolume.transform;

            Vector3 local =
                target.InverseTransformPoint(worldPoint) -
                triggerVolume.center;

            Vector3 half =
                triggerVolume.size * 0.5f;

            const float epsilon = 0.0001f;

            return
                Mathf.Abs(local.x) <= half.x + epsilon &&
                Mathf.Abs(local.y) <= half.y + epsilon &&
                Mathf.Abs(local.z) <= half.z + epsilon;
        }

        [ContextMenu("Debug Print Exact Clearance")]
        private void DebugPrintExactClearance()
        {
            bool occupied =
                TryGetBlockingFigure(out FigureMotor blocker);

            GetExactWorldBox(
                out Vector3 center,
                out Vector3 half,
                out Quaternion rotation);

            Debug.Log(
                "[M55.9.8 Exact Palimpsest Clearance]\n" +
                $"Binding={bindingId}\n" +
                $"Occupied={occupied}\n" +
                $"BlockingFigure={(blocker != null ? blocker.name : "None")}\n" +
                $"Center={center:F3}\n" +
                $"HalfExtents={half:F3}\n" +
                $"RotationEuler={rotation.eulerAngles:F2}\n" +
                $"OverlapCount={lastOverlapCount}\n" +
                "Query=Physics.OverlapBoxNonAlloc exact OBB\n" +
                "Legacy BoxCollider.bounds AABB=False",
                this);
        }

        [ContextMenu("Debug Show Exact Clearance")]
        private void DebugShowExactClearance()
        {
            ShowBlockedOutline();
        }

        private void GetExactWorldBox(
            out Vector3 center,
            out Vector3 halfExtents,
            out Quaternion rotation)
        {
            Transform target = triggerVolume.transform;

            center = target.TransformPoint(
                triggerVolume.center);

            Vector3 scale = target.lossyScale;

            halfExtents = Vector3.Scale(
                triggerVolume.size * 0.5f,
                new Vector3(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y),
                    Mathf.Abs(scale.z)));

            // Avoid a zero-axis box that Physics treats unexpectedly.
            halfExtents.x = Mathf.Max(0.001f, halfExtents.x);
            halfExtents.y = Mathf.Max(0.001f, halfExtents.y);
            halfExtents.z = Mathf.Max(0.001f, halfExtents.z);

            rotation = target.rotation;
        }

        private void ShowBlockedOutline()
        {
            if (!Application.isPlaying ||
                triggerVolume == null)
            {
                return;
            }

            EnsureOutline();

            if (outline == null)
                return;

            Vector3[] points = BuildOutlinePoints();
            outline.positionCount = points.Length;
            outline.SetPositions(points);
            outline.widthMultiplier = blockedOutlineWidth;
            outline.enabled = true;
            outlineVisibleUntil =
                Time.unscaledTime + blockedOutlineSeconds;
        }

        private Vector3[] BuildOutlinePoints()
        {
            Transform target = triggerVolume.transform;
            Vector3 center = triggerVolume.center;
            Vector3 half = triggerVolume.size * 0.5f;

            Vector3 C(float x, float y, float z)
            {
                return target.TransformPoint(
                    center + new Vector3(
                        x * half.x,
                        y * half.y,
                        z * half.z));
            }

            Vector3 a = C(-1, -1, -1);
            Vector3 b = C( 1, -1, -1);
            Vector3 c = C( 1, -1,  1);
            Vector3 d = C(-1, -1,  1);

            Vector3 e = C(-1,  1, -1);
            Vector3 f = C( 1,  1, -1);
            Vector3 g = C( 1,  1,  1);
            Vector3 h = C(-1,  1,  1);

            // A single continuous line that traverses all 12 box edges.
            return new[]
            {
                a, b, c, d, a,
                e, f, b,
                f, g, c,
                g, h, d,
                h, e
            };
        }

        private void EnsureOutline()
        {
            if (outline != null)
                return;

            GameObject child =
                new GameObject("__ExactClearanceBlockedOutline");

            child.transform.SetParent(transform, false);

            outline = child.AddComponent<LineRenderer>();
            outline.useWorldSpace = true;
            outline.loop = false;
            outline.numCapVertices = 2;
            outline.numCornerVertices = 2;
            outline.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                outlineMaterial =
                    new Material(shader);

                outlineMaterial.name =
                    "Runtime_Palimpsest_ClearanceBlocked";

                outline.material =
                    outlineMaterial;
            }

            Color blockedColor =
                new Color(
                    1f,
                    0.18f,
                    0.06f,
                    0.96f);

            outline.startColor = blockedColor;
            outline.endColor = blockedColor;
            outline.enabled = false;
        }

        public void ResetPalimpsestState()
        {
            lastBlockingFigure = null;
            lastOverlapCount = 0;

            if (outline != null)
                outline.enabled = false;
        }

        private void OnDestroy()
        {
            if (outlineMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(outlineMaterial);
            else
                DestroyImmediate(outlineMaterial);
        }
    }
}
