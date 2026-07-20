using System.Collections.Generic;
using PaintedAlive.Paint.Sponge;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Paint.Watercolor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class WatercolorFlowSurface :
        MonoBehaviour,
        ISpongeAbsorbableSource
    {
        private const int MaximumProbeHits = 16;

        private static readonly List<WatercolorFlowSurface>
            ActiveSurfaceList = new();

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private readonly List<Vector3> worldPoints = new();
        private readonly List<Vector3> worldNormals = new();
        private readonly RaycastHit[] probeHits =
            new RaycastHit[MaximumProbeHits];

        [Header("Dependencies")]
        [SerializeField]
        private WatercolorFlowConfig config;

        [SerializeField]
        private MeshFilter meshFilter;

        [SerializeField]
        private MeshRenderer meshRenderer;

        [SerializeField]
        private MeshCollider meshCollider;

        [Header("Ground Detection")]
        [SerializeField]
        private LayerMask surfaceMask =
            Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float availableAmount;

        [SerializeField]
        private Color paintColor =
            new Color(0.12f, 0.62f, 0.95f, 0.68f);

        [SerializeField]
        private Vector3 currentFlowDirection = Vector3.forward;

        [SerializeField]
        private float currentLength;

        [SerializeField]
        private bool initialized;

        private Mesh generatedMesh;
        private MaterialPropertyBlock propertyBlock;
        private float growthDistance;

        public static IReadOnlyList<WatercolorFlowSurface>
            ActiveSurfaces => ActiveSurfaceList;

        public bool CanAbsorb =>
            isActiveAndEnabled && availableAmount > 0.001f;

        public float AvailableAmount => availableAmount;
        public Color PaintColor => paintColor;

        public float Instability =>
            config != null
                ? config.AbsorptionInstability
                : 0f;

        public float NormalizedAmount =>
            config != null && config.InitialAmount > 0f
                ? Mathf.Clamp01(
                    availableAmount / config.InitialAmount)
                : 0f;

        public float FigureFlowAcceleration =>
            config != null ? config.FigureFlowAcceleration : 0f;

        public float RigidbodyFlowAcceleration =>
            config != null ? config.RigidbodyFlowAcceleration : 0f;

        public float ClarityExposurePerSecond =>
            config != null ? config.ClarityExposurePerSecond : 0f;

        public int NodeCount => worldPoints.Count;
        public float CurrentLength => currentLength;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveSurfaces()
        {
            ActiveSurfaceList.Clear();
        }

        private void Awake()
        {
            CacheComponents();
            CreateRuntimeMesh();

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(WatercolorFlowSurface)} on {name} " +
                    "requires a WatercolorFlowConfig.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!ActiveSurfaceList.Contains(this))
            {
                ActiveSurfaceList.Add(this);
            }
        }

        private void Start()
        {
            if (!initialized && config != null)
            {
                Initialize(
                    transform.forward,
                    paintColor,
                    config.InitialAmount);
            }
        }

        private void OnDisable()
        {
            ActiveSurfaceList.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveSurfaceList.Remove(this);

            if (generatedMesh != null)
            {
                Destroy(generatedMesh);
            }
        }

        private void Update()
        {
            if (!initialized || config == null || !CanAbsorb)
            {
                return;
            }

            float amountLengthMultiplier =
                Mathf.Lerp(0.35f, 1f, Mathf.Sqrt(NormalizedAmount));
            float allowedLength =
                config.MaximumFlowLength * amountLengthMultiplier;

            if (currentLength >= allowedLength ||
                worldPoints.Count >= config.MaximumNodeCount)
            {
                return;
            }

            growthDistance += config.GrowthSpeed * Time.deltaTime;

            while (growthDistance >= config.NodeSpacing &&
                   currentLength < allowedLength &&
                   worldPoints.Count < config.MaximumNodeCount)
            {
                growthDistance -= config.NodeSpacing;

                if (!TryAppendFlowNode())
                {
                    growthDistance = 0f;
                    break;
                }
            }
        }

        public void Configure(WatercolorFlowConfig flowConfig)
        {
            config = flowConfig;
        }

        public void Initialize(
            Vector3 initialWorldDirection,
            Color color,
            float amount)
        {
            if (config == null)
            {
                return;
            }

            CacheComponents();
            CreateRuntimeMesh();

            worldPoints.Clear();
            worldNormals.Clear();
            currentLength = 0f;
            growthDistance = 0f;

            paintColor = color;
            paintColor.a = Mathf.Clamp(
                color.a > 0f ? color.a : 0.68f,
                0.18f,
                0.82f);
            availableAmount = Mathf.Clamp(
                amount,
                0f,
                config.InitialAmount);

            if (!TrySampleGround(
                    transform.position +
                    Vector3.up * config.ProbeHeight,
                    out RaycastHit startHit))
            {
                Debug.LogWarning(
                    "Watercolor flow could not find a start surface.",
                    this);
                initialized = false;
                return;
            }

            transform.position =
                startHit.point +
                startHit.normal * config.SurfaceOffset;

            Vector3 planarDirection =
                Vector3.ProjectOnPlane(
                    initialWorldDirection,
                    startHit.normal);

            if (planarDirection.sqrMagnitude < 0.001f)
            {
                planarDirection =
                    Vector3.ProjectOnPlane(
                        Vector3.forward,
                        startHit.normal);
            }

            currentFlowDirection = planarDirection.normalized;
            worldPoints.Add(transform.position);
            worldNormals.Add(startHit.normal.normalized);
            initialized = true;
            RebuildMesh();
            RefreshVisual();
        }

        public float Absorb(float requestedAmount)
        {
            float absorbed = Mathf.Min(
                Mathf.Max(0f, requestedAmount),
                availableAmount);

            if (absorbed <= 0f)
            {
                return 0f;
            }

            availableAmount -= absorbed;

            if (availableAmount <= 0.001f)
            {
                availableAmount = 0f;
                Destroy(gameObject);
                return absorbed;
            }

            RebuildMesh();
            RefreshVisual();
            return absorbed;
        }

        public bool TrySampleFlow(
            Vector3 worldPosition,
            out Vector3 flowDirection,
            out float influence,
            out Vector3 nearestPoint)
        {
            flowDirection = Vector3.zero;
            influence = 0f;
            nearestPoint = worldPosition;

            if (!CanAbsorb || config == null ||
                worldPoints.Count < 2)
            {
                return false;
            }

            float halfWidth = GetCurrentHalfWidth();
            float bestDistanceSquared = float.PositiveInfinity;
            Vector3 bestDirection = Vector3.zero;
            Vector3 bestPoint = worldPosition;

            for (int i = 0; i < worldPoints.Count - 1; i++)
            {
                Vector3 start = worldPoints[i];
                Vector3 end = worldPoints[i + 1];
                Vector3 segment = end - start;
                float segmentLengthSquared = segment.sqrMagnitude;

                if (segmentLengthSquared <= 0.0001f)
                {
                    continue;
                }

                float t = Mathf.Clamp01(
                    Vector3.Dot(worldPosition - start, segment) /
                    segmentLengthSquared);
                Vector3 candidate = start + segment * t;
                float verticalDifference =
                    Mathf.Abs(worldPosition.y - candidate.y);

                if (verticalDifference >
                    config.ContactHeightTolerance)
                {
                    continue;
                }

                float distanceSquared =
                    (worldPosition - candidate).sqrMagnitude;

                if (distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                bestPoint = candidate;
                bestDirection = segment.normalized;
            }

            if (bestDistanceSquared > halfWidth * halfWidth)
            {
                return false;
            }

            float distance = Mathf.Sqrt(bestDistanceSquared);
            float edgeInfluence =
                1f - Mathf.Clamp01(distance / halfWidth);

            flowDirection = bestDirection;
            influence =
                edgeInfluence *
                Mathf.Lerp(0.3f, 1f, NormalizedAmount);
            nearestPoint = bestPoint;
            return influence > 0.001f;
        }

        private bool TryAppendFlowNode()
        {
            int lastIndex = worldPoints.Count - 1;
            Vector3 lastPoint = worldPoints[lastIndex];
            Vector3 lastNormal = worldNormals[lastIndex];
            Vector3 projectedDirection =
                Vector3.ProjectOnPlane(
                    currentFlowDirection,
                    lastNormal).normalized;
            Vector3 downhill =
                Vector3.ProjectOnPlane(
                    Vector3.down,
                    lastNormal);

            Vector3 desiredDirection = projectedDirection;

            if (downhill.sqrMagnitude > 0.0004f)
            {
                desiredDirection = Vector3.Slerp(
                    projectedDirection,
                    downhill.normalized,
                    config.SlopeSteering).normalized;
            }

            Vector3 candidate =
                lastPoint +
                desiredDirection * config.NodeSpacing;

            if (!TrySampleGround(
                    candidate +
                    Vector3.up * config.ProbeHeight,
                    out RaycastHit hit))
            {
                return false;
            }

            Vector3 nextPoint =
                hit.point +
                hit.normal * config.SurfaceOffset;

            float stepLength =
                Vector3.Distance(lastPoint, nextPoint);

            if (stepLength <= 0.05f ||
                stepLength > config.NodeSpacing * 2.25f)
            {
                return false;
            }

            worldPoints.Add(nextPoint);
            worldNormals.Add(hit.normal.normalized);
            currentLength += stepLength;
            currentFlowDirection =
                (nextPoint - lastPoint).normalized;
            RebuildMesh();
            return true;
        }

        private bool TrySampleGround(
            Vector3 origin,
            out RaycastHit bestHit)
        {
            int hitCount = Physics.RaycastNonAlloc(
                new Ray(origin, Vector3.down),
                probeHits,
                config.ProbeDistance,
                surfaceMask,
                QueryTriggerInteraction.Ignore);

            bestHit = default;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = probeHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                WatercolorFlowSurface hitWatercolor =
                    hit.collider.GetComponentInParent<
                        WatercolorFlowSurface>();

                if (hitWatercolor == this)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            return bestHit.collider != null;
        }

        private void RebuildMesh()
        {
            if (generatedMesh == null || meshFilter == null)
            {
                return;
            }

            generatedMesh.Clear();

            if (worldPoints.Count < 2)
            {
                meshCollider.sharedMesh = null;
                return;
            }

            int pointCount = worldPoints.Count;
            var vertices = new Vector3[pointCount * 2];
            var normals = new Vector3[pointCount * 2];
            var uv = new Vector2[pointCount * 2];
            var triangles = new int[(pointCount - 1) * 6];
            float halfWidth = GetCurrentHalfWidth();

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 tangent;

                if (i == 0)
                {
                    tangent = worldPoints[1] - worldPoints[0];
                }
                else if (i == pointCount - 1)
                {
                    tangent =
                        worldPoints[i] - worldPoints[i - 1];
                }
                else
                {
                    tangent =
                        worldPoints[i + 1] - worldPoints[i - 1];
                }

                Vector3 normal = worldNormals[i];
                Vector3 right =
                    Vector3.Cross(normal, tangent).normalized;
                float normalizedIndex =
                    pointCount > 1
                        ? i / (pointCount - 1f)
                        : 0f;
                float taper = Mathf.Lerp(0.88f, 0.42f,
                    normalizedIndex * normalizedIndex);
                Vector3 centerLocal =
                    transform.InverseTransformPoint(worldPoints[i]);
                Vector3 widthLocal =
                    transform.InverseTransformVector(
                        right * halfWidth * taper);

                vertices[i * 2] = centerLocal - widthLocal;
                vertices[i * 2 + 1] = centerLocal + widthLocal;
                normals[i * 2] =
                    transform.InverseTransformDirection(normal);
                normals[i * 2 + 1] = normals[i * 2];
                uv[i * 2] = new Vector2(0f, normalizedIndex);
                uv[i * 2 + 1] = new Vector2(1f, normalizedIndex);
            }

            for (int i = 0; i < pointCount - 1; i++)
            {
                int vertex = i * 2;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            generatedMesh.vertices = vertices;
            generatedMesh.normals = normals;
            generatedMesh.uv = uv;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateBounds();

            meshFilter.sharedMesh = generatedMesh;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = generatedMesh;
        }

        private float GetCurrentHalfWidth()
        {
            float amountScale =
                Mathf.Lerp(0.22f, 1f, Mathf.Sqrt(NormalizedAmount));
            return config.SurfaceWidth * 0.5f * amountScale;
        }

        private void RefreshVisual()
        {
            if (meshRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);

            Color displayColor = paintColor;
            displayColor.a *=
                Mathf.Lerp(0.28f, 1f, NormalizedAmount);
            propertyBlock.SetColor(BaseColorId, displayColor);
            propertyBlock.SetColor(ColorId, displayColor);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void CacheComponents()
        {
            meshFilter ??= GetComponent<MeshFilter>();
            meshRenderer ??= GetComponent<MeshRenderer>();
            meshCollider ??= GetComponent<MeshCollider>();
        }

        private void CreateRuntimeMesh()
        {
            if (generatedMesh != null)
            {
                return;
            }

            generatedMesh = new Mesh
            {
                name = $"{name}_WatercolorFlowMesh",
                indexFormat = IndexFormat.UInt32
            };
            generatedMesh.MarkDynamic();
            meshFilter.sharedMesh = generatedMesh;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.6f);

            for (int i = 0; i < worldPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(worldPoints[i], worldPoints[i + 1]);
            }
        }
    }
}
