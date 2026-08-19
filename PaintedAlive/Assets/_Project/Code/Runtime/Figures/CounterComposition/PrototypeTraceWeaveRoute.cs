using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.CounterComposition
{
    [DisallowMultipleComponent]
    public sealed class PrototypeTraceWeaveRoute :
        MonoBehaviour
    {
        [Header("Route Contract")]
        [SerializeField] private float lifetimeSeconds = 8f;
        [SerializeField] private float routeRadius = 0.12f;
        [SerializeField] private float bornAt;
        [SerializeField] private int sourceSampleCount;
        [SerializeField] private float routeLength;

        [Header("Counterplay")]
        [SerializeField] private int watercolorBendCount;
        [SerializeField] private int eraserRemoveCount;
        [SerializeField] private int paletteKnifeConnectionCount;
        [SerializeField] private int paletteKnifeBreakCount;
        [SerializeField] private string lastReaction = "None";

        [Header("Runtime Read Only")]
        [SerializeField] private bool activeRoute;
        [SerializeField] private bool expired;
        [SerializeField] private int colliderSegmentCount;
        [SerializeField] private int visualPointCount;
        [SerializeField] private float remainingSeconds;
        [SerializeField] private string lastState = "Not initialized";

        private LineRenderer routeLine;
        private Material routeMaterial;

        private readonly List<CapsuleCollider> colliders =
            new List<CapsuleCollider>();

        private readonly List<Vector3> routePoints =
            new List<Vector3>();

        private readonly List<Vector3> editBuffer =
            new List<Vector3>();

        public bool ActiveRoute => activeRoute;
        public bool Expired => expired;
        public int SourceSampleCount => sourceSampleCount;
        public int ColliderSegmentCount => colliderSegmentCount;
        public int VisualPointCount => visualPointCount;
        public int RoutePointCount => routePoints.Count;
        public int WatercolorBendCount => watercolorBendCount;
        public int EraserRemoveCount => eraserRemoveCount;
        public int PaletteKnifeConnectionCount =>
            paletteKnifeConnectionCount;
        public int PaletteKnifeBreakCount =>
            paletteKnifeBreakCount;
        public float RouteLength => routeLength;
        public float RemainingSeconds => remainingSeconds;
        public string LastState => lastState;
        public string LastReaction => lastReaction;

        public bool WatercolorBendEnabled => true;
        public bool OilLoadHandledByTeamRoute => true;
        public bool EraserRemovalEnabled => true;
        public bool PaletteKnifeConnectionEditingEnabled => true;
        public bool PreservesValidatedAnchorsOnWatercolor => true;
        public bool PaletteKnifeCreatesFreeAirGeometry => false;
        public bool UsesMeshCollider => false;
        public bool UsesCapsuleSegmentProxies => true;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Build(
            IReadOnlyList<Vector3> worldPoints,
            float configuredLifetimeSeconds,
            float configuredRouteRadius)
        {
            ClearRoute();

            if (
                worldPoints == null ||
                worldPoints.Count < 2
            )
            {
                lastState = "Insufficient route points";
                return;
            }

            lifetimeSeconds =
                Mathf.Max(
                    0.5f,
                    configuredLifetimeSeconds);

            routeRadius =
                Mathf.Clamp(
                    configuredRouteRadius,
                    0.06f,
                    0.24f);

            sourceSampleCount =
                worldPoints.Count;

            routePoints.Clear();

            for (int index = 0;
                 index < worldPoints.Count;
                 index++)
            {
                routePoints.Add(
                    worldPoints[index]);
            }

            bornAt =
                Time.unscaledTime;

            expired = false;
            remainingSeconds =
                lifetimeSeconds;

            RebuildGeometry();

            lastState =
                activeRoute
                    ? "Active"
                    : "No valid collider segments";
        }

        private void Update()
        {
            if (!activeRoute)
            {
                return;
            }

            float elapsed =
                Time.unscaledTime -
                bornAt;

            remainingSeconds =
                Mathf.Max(
                    0f,
                    lifetimeSeconds -
                    elapsed);

            if (
                elapsed >=
                lifetimeSeconds
            )
            {
                Expire();
            }
        }

        public bool TryGetRoutePoint(
            int index,
            out Vector3 point)
        {
            if (
                index < 0 ||
                index >= routePoints.Count
            )
            {
                point = Vector3.zero;
                return false;
            }

            point =
                routePoints[index];

            return true;
        }

        public bool TryGetNearestRouteSample(
            Vector3 worldPosition,
            out Vector3 nearestPoint,
            out Vector3 tangent,
            out float distance,
            out float normalizedDistance)
        {
            return TryGetNearestRouteSampleInternal(
                worldPosition,
                out nearestPoint,
                out tangent,
                out distance,
                out normalizedDistance,
                out _,
                out _);
        }

        public bool TryApplyWatercolorBend(
            Vector3 worldPoint,
            Vector3 bendDirection,
            float radius,
            float strength,
            out string reason)
        {
            reason = string.Empty;

            if (
                !activeRoute ||
                routePoints.Count < 3
            )
            {
                reason =
                    "Aktif ve en az üç noktalı rota gerekli.";

                return false;
            }

            Vector3 lateral =
                Vector3.ProjectOnPlane(
                    bendDirection,
                    Vector3.up);

            if (
                lateral.sqrMagnitude <=
                0.0001f
            )
            {
                lateral =
                    Vector3.right;
            }
            else
            {
                lateral.Normalize();
            }

            float effectiveRadius =
                Mathf.Max(
                    0.25f,
                    radius);

            float effectiveStrength =
                Mathf.Clamp(
                    strength,
                    0f,
                    1.25f);

            int affectedPointCount = 0;

            // Endpoints are deliberately excluded:
            // M51.0 validated anchors remain fixed.
            for (int index = 1;
                 index < routePoints.Count - 1;
                 index++)
            {
                Vector3 point =
                    routePoints[index];

                float distance =
                    Vector3.Distance(
                        point,
                        worldPoint);

                if (
                    distance >
                    effectiveRadius
                )
                {
                    continue;
                }

                float normalized =
                    1f -
                    Mathf.Clamp01(
                        distance /
                        effectiveRadius);

                float falloff =
                    normalized *
                    normalized;

                routePoints[index] +=
                    lateral *
                    effectiveStrength *
                    falloff;

                affectedPointCount++;
            }

            if (affectedPointCount <= 0)
            {
                reason =
                    "Suluboya etki yarıçapında iç rota noktası yok.";

                return false;
            }

            RebuildGeometry();

            if (!activeRoute)
            {
                reason =
                    "Suluboya sonrası rota collision üretilemedi.";

                return false;
            }

            watercolorBendCount++;

            lastReaction =
                $"Watercolor bend: {affectedPointCount} point";

            lastState =
                "Active / Watercolor bent";

            reason =
                $"{affectedPointCount} iç rota noktası büküldü.";

            return true;
        }

        public bool TryErase(
            string source,
            out string reason)
        {
            if (!activeRoute)
            {
                reason =
                    "Silinecek aktif rota yok.";

                return false;
            }

            eraserRemoveCount++;

            lastReaction =
                $"Eraser: {source}";

            lastState =
                "Erased";

            Expire();

            reason =
                "İz Dokuma Silgiyle kaldırıldı.";

            return true;
        }

        public bool TryOpenPaletteKnifeConnection(
            Vector3 worldPoint,
            float maximumDistance,
            float safeEndpointFraction,
            out bool routeBroken,
            out string reason)
        {
            routeBroken = false;
            reason = string.Empty;

            if (
                !activeRoute ||
                routePoints.Count < 3
            )
            {
                reason =
                    "Palet Bıçağı için aktif rota yok.";

                return false;
            }

            if (
                !TryGetNearestRouteSampleInternal(
                    worldPoint,
                    out Vector3 nearestPoint,
                    out _,
                    out float distance,
                    out float normalizedDistance,
                    out int segmentIndex,
                    out _)
            )
            {
                reason =
                    "En yakın rota segmenti çözülemedi.";

                return false;
            }

            if (
                distance >
                Mathf.Max(
                    0.1f,
                    maximumDistance)
            )
            {
                reason =
                    $"Rota Palet Bıçağı menzilinde değil: {distance:F2} m.";

                return false;
            }

            float endpointFraction =
                Mathf.Clamp(
                    safeEndpointFraction,
                    0.08f,
                    0.35f);

            if (
                normalizedDistance >
                    endpointFraction &&
                normalizedDistance <
                    1f -
                    endpointFraction
            )
            {
                paletteKnifeBreakCount++;

                lastReaction =
                    $"Palette Knife mis-cut at {normalizedDistance:F2}";

                lastState =
                    "Broken by Palette Knife";

                routeBroken = true;

                Expire();

                reason =
                    "Orta bölüm kesildi; rota tamamen koptu.";

                return true;
            }

            editBuffer.Clear();

            if (
                normalizedDistance <=
                endpointFraction
            )
            {
                editBuffer.Add(
                    nearestPoint);

                for (int index = segmentIndex;
                     index < routePoints.Count;
                     index++)
                {
                    editBuffer.Add(
                        routePoints[index]);
                }

                lastReaction =
                    "Palette Knife opened new start connection";
            }
            else
            {
                for (int index = 0;
                     index < segmentIndex;
                     index++)
                {
                    editBuffer.Add(
                        routePoints[index]);
                }

                editBuffer.Add(
                    nearestPoint);

                lastReaction =
                    "Palette Knife opened new end connection";
            }

            RemoveNearDuplicatePoints(
                editBuffer,
                0.04f);

            if (editBuffer.Count < 2)
            {
                reason =
                    "Yeni bağlantı sonrası rota çok kısa kaldı.";

                return false;
            }

            routePoints.Clear();

            for (int index = 0;
                 index < editBuffer.Count;
                 index++)
            {
                routePoints.Add(
                    editBuffer[index]);
            }

            RebuildGeometry();

            if (!activeRoute)
            {
                reason =
                    "Yeni bağlantı collision üretemedi.";

                return false;
            }

            paletteKnifeConnectionCount++;

            lastState =
                "Active / New Palette Knife connection";

            reason =
                normalizedDistance <=
                    endpointFraction
                    ? "Yeni başlangıç bağlantısı açıldı."
                    : "Yeni bitiş bağlantısı açıldı.";

            return true;
        }

        public void Expire()
        {
            if (expired)
            {
                return;
            }

            expired = true;
            activeRoute = false;
            remainingSeconds = 0f;

            if (
                string.IsNullOrEmpty(
                    lastState) ||
                lastState ==
                    "Active"
            )
            {
                lastState =
                    "Expired";
            }

            if (routeLine != null)
            {
                routeLine.enabled = false;
            }

            for (int index = 0;
                 index < colliders.Count;
                 index++)
            {
                CapsuleCollider collider =
                    colliders[index];

                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        private bool TryGetNearestRouteSampleInternal(
            Vector3 worldPosition,
            out Vector3 nearestPoint,
            out Vector3 tangent,
            out float distance,
            out float normalizedDistance,
            out int segmentIndex,
            out float segmentNormalized)
        {
            nearestPoint = Vector3.zero;
            tangent = Vector3.forward;
            distance = float.PositiveInfinity;
            normalizedDistance = 0f;
            segmentIndex = -1;
            segmentNormalized = 0f;

            if (
                routePoints.Count < 2 ||
                !activeRoute
            )
            {
                return false;
            }

            float bestSquaredDistance =
                float.PositiveInfinity;

            float accumulatedLength = 0f;
            float bestAccumulatedLength = 0f;

            for (int index = 1;
                 index < routePoints.Count;
                 index++)
            {
                Vector3 start =
                    routePoints[index - 1];

                Vector3 end =
                    routePoints[index];

                Vector3 segment =
                    end -
                    start;

                float segmentLength =
                    segment.magnitude;

                if (
                    segmentLength <=
                    0.0001f
                )
                {
                    continue;
                }

                Vector3 direction =
                    segment /
                    segmentLength;

                float projected =
                    Vector3.Dot(
                        worldPosition -
                        start,
                        direction);

                float clamped =
                    Mathf.Clamp(
                        projected,
                        0f,
                        segmentLength);

                Vector3 candidate =
                    start +
                    direction *
                    clamped;

                float squaredDistance =
                    (
                        worldPosition -
                        candidate
                    ).sqrMagnitude;

                if (
                    squaredDistance <
                    bestSquaredDistance
                )
                {
                    bestSquaredDistance =
                        squaredDistance;

                    nearestPoint =
                        candidate;

                    tangent =
                        direction;

                    segmentIndex =
                        index;

                    segmentNormalized =
                        segmentLength >
                            0.0001f
                            ? clamped /
                              segmentLength
                            : 0f;

                    bestAccumulatedLength =
                        accumulatedLength +
                        clamped;
                }

                accumulatedLength +=
                    segmentLength;
            }

            if (
                float.IsPositiveInfinity(
                    bestSquaredDistance)
            )
            {
                return false;
            }

            distance =
                Mathf.Sqrt(
                    bestSquaredDistance);

            normalizedDistance =
                routeLength >
                    0.0001f
                    ? Mathf.Clamp01(
                        bestAccumulatedLength /
                        routeLength)
                    : 0f;

            return true;
        }

        private void RebuildGeometry()
        {
            DestroyColliderSegments();

            if (
                routePoints.Count < 2
            )
            {
                activeRoute = false;
                colliderSegmentCount = 0;
                visualPointCount = 0;
                routeLength = 0f;
                return;
            }

            EnsureLine();

            routeLength = 0f;

            routeLine.positionCount =
                routePoints.Count;

            for (int index = 0;
                 index < routePoints.Count;
                 index++)
            {
                routeLine.SetPosition(
                    index,
                    routePoints[index]);

                if (index > 0)
                {
                    routeLength +=
                        Vector3.Distance(
                            routePoints[index - 1],
                            routePoints[index]);
                }
            }

            routeLine.widthMultiplier =
                routeRadius *
                1.15f;

            routeLine.enabled = true;

            visualPointCount =
                routePoints.Count;

            for (int index = 1;
                 index < routePoints.Count;
                 index++)
            {
                Vector3 start =
                    routePoints[index - 1];

                Vector3 end =
                    routePoints[index];

                float length =
                    Vector3.Distance(
                        start,
                        end);

                if (
                    length <=
                    0.035f
                )
                {
                    continue;
                }

                GameObject segment =
                    new GameObject(
                        $"TraceWeaveCollider_{index:00}");

                segment.transform.SetParent(
                    transform,
                    false);

                segment.transform.position =
                    (
                        start +
                        end
                    ) *
                    0.5f;

                Vector3 direction =
                    (
                        end -
                        start
                    ).normalized;

                segment.transform.rotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        direction);

                CapsuleCollider collider =
                    segment.AddComponent<
                        CapsuleCollider>();

                collider.direction = 1;

                collider.radius =
                    routeRadius;

                collider.height =
                    Mathf.Max(
                        routeRadius *
                            2f,
                        length +
                        routeRadius *
                            2f);

                collider.center =
                    Vector3.zero;

                collider.isTrigger =
                    false;

                colliders.Add(
                    collider);
            }

            colliderSegmentCount =
                colliders.Count;

            activeRoute =
                colliderSegmentCount > 0 &&
                !expired;
        }

        private void DestroyColliderSegments()
        {
            for (int index = 0;
                 index < colliders.Count;
                 index++)
            {
                CapsuleCollider collider =
                    colliders[index];

                if (collider == null)
                {
                    continue;
                }

                collider.enabled = false;

                GameObject segment =
                    collider.gameObject;

                if (Application.isPlaying)
                {
                    Destroy(
                        segment);
                }
                else
                {
                    DestroyImmediate(
                        segment);
                }
            }

            colliders.Clear();
            colliderSegmentCount = 0;
        }

        private static void RemoveNearDuplicatePoints(
            List<Vector3> points,
            float minimumSpacing)
        {
            if (
                points == null ||
                points.Count < 2
            )
            {
                return;
            }

            float spacing =
                Mathf.Max(
                    0.001f,
                    minimumSpacing);

            for (int index =
                     points.Count - 1;
                 index > 0;
                 index--)
            {
                if (
                    Vector3.Distance(
                        points[index],
                        points[index - 1]) <
                    spacing
                )
                {
                    points.RemoveAt(
                        index);
                }
            }
        }

        private void EnsureLine()
        {
            if (routeLine != null)
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                routeMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M51_0_TraceWeave_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject lineObject =
                new GameObject(
                    "TraceWeaveVisual");

            lineObject.transform.SetParent(
                transform,
                false);

            routeLine =
                lineObject.AddComponent<
                    LineRenderer>();

            routeLine.useWorldSpace = true;
            routeLine.loop = false;
            routeLine.alignment =
                LineAlignment.View;

            routeLine.textureMode =
                LineTextureMode.Stretch;

            routeLine.numCapVertices = 4;
            routeLine.numCornerVertices = 4;

            routeLine.shadowCastingMode =
                ShadowCastingMode.Off;

            routeLine.receiveShadows = false;

            routeLine.sharedMaterial =
                routeMaterial;

            Color color =
                new Color(
                    0.88f,
                    0.90f,
                    0.78f,
                    0.92f);

            routeLine.startColor =
                color;

            routeLine.endColor =
                color;

            routeLine.enabled = false;
        }

        private void ClearRoute()
        {
            DestroyColliderSegments();

            if (routeLine != null)
            {
                routeLine.enabled = false;
                routeLine.positionCount = 0;
            }

            routePoints.Clear();
            editBuffer.Clear();

            sourceSampleCount = 0;
            visualPointCount = 0;
            routeLength = 0f;
            remainingSeconds = 0f;
            activeRoute = false;
            expired = false;

            watercolorBendCount = 0;
            eraserRemoveCount = 0;
            paletteKnifeConnectionCount = 0;
            paletteKnifeBreakCount = 0;
            lastReaction = "None";
        }

        private void OnDestroy()
        {
            DestroyColliderSegments();

            if (routeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    routeMaterial);
            }
            else
            {
                DestroyImmediate(
                    routeMaterial);
            }

            routeMaterial = null;
        }
    }
}
