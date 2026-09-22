using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Painters.Masterpiece
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceWorldInstance :
        MonoBehaviour
    {
        private sealed class Mapping
        {
            public Vector2 Center;
            public float MinimumY;
            public float Scale;
            public float FloorOffset;

            public Vector3 ToLocal(Vector2 point)
            {
                return new Vector3(
                    (point.x - Center.x) * Scale,
                    (point.y - MinimumY) * Scale +
                        FloorOffset,
                    0f);
            }

            public float RadiusToWorld(float normalizedRadius)
            {
                return Mathf.Max(
                    0.01f,
                    normalizedRadius *
                    Scale);
            }
        }

        [Header("Runtime Read Only")]
        [SerializeField] private int snapshotHash;
        [SerializeField] private int sourceAssemblyRevision;
        [SerializeField] private int sourceStrokeCount;
        [SerializeField] private int sourcePointCount;
        [SerializeField] private int partCount;
        [SerializeField] private int colliderCount;
        [SerializeField] private int visualSegmentCount;
        [SerializeField] private int renderedPointCount;
        [SerializeField] private bool proxyDebugVisible = true;
        [SerializeField] private bool originalStrokeVisualsVisible = true;
        [SerializeField] private bool allCollidersAreTriggers;
        [SerializeField] private bool kinematicBody;
        [SerializeField] private bool buildSucceeded;
        [SerializeField] private bool runtimeMaterialsReady;
        [SerializeField] private bool semanticHeadPartitionEnabled = true;
        [SerializeField] private int semanticHeadVisualSegmentCount;
        [SerializeField] private int semanticHeadRenderedPointCount;
        [SerializeField] private string lastBuildResult = "Not built";

        private readonly Dictionary<
            PrototypeMasterpiecePartKind,
            Transform> partRoots =
            new Dictionary<
                PrototypeMasterpiecePartKind,
                Transform>();

        private readonly List<GameObject> proxyDebugObjects =
            new List<GameObject>();

        private readonly List<LineRenderer> originalStrokeRenderers =
            new List<LineRenderer>();

        private Material strokeMaterial;
        private Material proxyMaterial;
        private Rigidbody body;

        public int SnapshotHash => snapshotHash;
        public int SourceAssemblyRevision => sourceAssemblyRevision;
        public int SourceStrokeCount => sourceStrokeCount;
        public int SourcePointCount => sourcePointCount;
        public int PartCount => partCount;
        public int ColliderCount => colliderCount;
        public int VisualSegmentCount => visualSegmentCount;
        public int RenderedPointCount => renderedPointCount;
        public bool ProxyDebugVisible => proxyDebugVisible;
        public bool OriginalStrokeVisualsVisible =>
            originalStrokeVisualsVisible;
        public int OriginalStrokeRendererCount =>
            originalStrokeRenderers.Count;
        public bool AllCollidersAreTriggers => allCollidersAreTriggers;
        public bool KinematicBody => kinematicBody;
        public bool BuildSucceeded => buildSucceeded;
        public bool RuntimeMaterialsReady => runtimeMaterialsReady;
        public bool SemanticHeadPartitionEnabled =>
            semanticHeadPartitionEnabled;
        public int SemanticHeadVisualSegmentCount =>
            semanticHeadVisualSegmentCount;
        public int SemanticHeadRenderedPointCount =>
            semanticHeadRenderedPointCount;
        public string LastBuildResult => lastBuildResult;
        public Rigidbody KinematicRigidbody => body;

        public bool SolidCollisionEnabled => false;
        public bool BossAIEnabled => false;
        public bool DamageAuthorityEnabled => false;

        public bool Build(
            PrototypeMasterpieceDeploymentSnapshot snapshot,
            PrototypeMasterpieceRouteAnchor anchor,
            Material strokeMaterialTemplate,
            Material proxyMaterialTemplate,
            out string reason)
        {
            buildSucceeded = false;
            semanticHeadVisualSegmentCount = 0;
            semanticHeadRenderedPointCount = 0;
            reason = string.Empty;
            originalStrokeRenderers.Clear();
            originalStrokeVisualsVisible = true;

            if (snapshot == null)
            {
                reason = "Deployment snapshot is null.";
                lastBuildResult = reason;
                return false;
            }

            if (anchor == null)
            {
                reason = "Route anchor is null.";
                lastBuildResult = reason;
                return false;
            }

            if (snapshot.Parts.Count != 6)
            {
                reason = "Deployment requires exactly six parts.";
                lastBuildResult = reason;
                return false;
            }

            transform.SetPositionAndRotation(
                anchor.DeploymentPosition,
                anchor.DeploymentRotation);

            transform.localScale = snapshot.DeploymentScale;

            snapshotHash = snapshot.ContentHash;
            sourceAssemblyRevision =
                snapshot.SourceAssemblyRevision;
            sourceStrokeCount =
                snapshot.Strokes.Count;
            sourcePointCount =
                snapshot.StrokePointCount;

            Mapping mapping =
                BuildMapping(
                    snapshot,
                    anchor);

            CreateRuntimeMaterials(
                strokeMaterialTemplate,
                proxyMaterialTemplate);

            if (!runtimeMaterialsReady)
            {
                reason =
                    "Kukla görünür dünya materyali oluşturulamadı; deploy iptal edildi.";
                lastBuildResult = reason;
                return false;
            }

            CreateKinematicBody();

            for (int index = 0;
                 index < snapshot.Parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartSnapshot part =
                    snapshot.Parts[index];

                if (part == null)
                {
                    continue;
                }

                CreatePart(
                    part,
                    mapping);
            }

            CreateStrokeVisuals(
                snapshot,
                mapping);

            partCount =
                partRoots.Count;

            Collider[] colliders =
                GetComponentsInChildren<
                    Collider>(
                        true);

            colliderCount =
                colliders.Length;

            allCollidersAreTriggers =
                colliderCount == 6;

            for (int index = 0;
                 index < colliders.Length;
                 index++)
            {
                if (colliders[index] == null ||
                    !colliders[index].isTrigger)
                {
                    allCollidersAreTriggers = false;
                    break;
                }
            }

            kinematicBody =
                body != null &&
                body.isKinematic &&
                !body.useGravity;

            buildSucceeded =
                partCount == 6 &&
                colliderCount == 6 &&
                allCollidersAreTriggers &&
                kinematicBody &&
                runtimeMaterialsReady &&
                visualSegmentCount > 0;

            reason =
                buildSucceeded
                    ? "Static world deployment built."
                    : "World deployment contract incomplete.";

            lastBuildResult = reason;
            SetProxyDebugVisible(
                proxyDebugVisible);

            return buildSucceeded;
        }

        public bool TryGetPartRoot(
            PrototypeMasterpiecePartKind kind,
            out Transform partRoot)
        {
            if (partRoots.TryGetValue(
                    kind,
                    out Transform found) &&
                found != null)
            {
                partRoot = found;
                return true;
            }

            partRoot = null;
            return false;
        }

        public bool TryGetWorldPart(
            PrototypeMasterpiecePartKind kind,
            out PrototypeMasterpieceWorldPart part)
        {
            part = null;

            if (!TryGetPartRoot(
                    kind,
                    out Transform partRoot))
            {
                return false;
            }

            part =
                partRoot.GetComponent<
                    PrototypeMasterpieceWorldPart>();

            return part != null;
        }

        public bool HasActiveCapability(
            PrototypeMasterpieceCapability capability)
        {
            foreach (KeyValuePair<
                         PrototypeMasterpiecePartKind,
                         Transform> pair in partRoots)
            {
                Transform partRoot =
                    pair.Value;

                if (partRoot == null)
                {
                    continue;
                }

                PrototypeMasterpieceWorldPart part =
                    partRoot.GetComponent<
                        PrototypeMasterpieceWorldPart>();

                if (part != null &&
                    part.Capability ==
                        capability &&
                    part.CapabilityActive)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetOriginalStrokeVisualsVisible(
            bool visible)
        {
            originalStrokeVisualsVisible = visible;

            for (int index = 0;
                 index < originalStrokeRenderers.Count;
                 index++)
            {
                LineRenderer renderer =
                    originalStrokeRenderers[index];

                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        public void SetProxyDebugVisible(bool visible)
        {
            proxyDebugVisible = visible;

            for (int index = 0;
                 index < proxyDebugObjects.Count;
                 index++)
            {
                GameObject debugObject =
                    proxyDebugObjects[index];

                if (debugObject != null)
                {
                    debugObject.SetActive(visible);
                }
            }
        }

        private Mapping BuildMapping(
            PrototypeMasterpieceDeploymentSnapshot snapshot,
            PrototypeMasterpieceRouteAnchor anchor)
        {
            Vector2 minimum =
                new Vector2(
                    float.PositiveInfinity,
                    float.PositiveInfinity);

            Vector2 maximum =
                new Vector2(
                    float.NegativeInfinity,
                    float.NegativeInfinity);

            for (int strokeIndex = 0;
                 strokeIndex < snapshot.Strokes.Count;
                 strokeIndex++)
            {
                PrototypeMasterpieceStrokeSnapshot stroke =
                    snapshot.Strokes[strokeIndex];

                if (stroke == null)
                {
                    continue;
                }

                for (int pointIndex = 0;
                     pointIndex < stroke.Points.Count;
                     pointIndex++)
                {
                    Vector2 point =
                        stroke.Points[pointIndex];

                    minimum =
                        Vector2.Min(
                            minimum,
                            point);

                    maximum =
                        Vector2.Max(
                            maximum,
                            point);
                }
            }

            for (int partIndex = 0;
                 partIndex < snapshot.Parts.Count;
                 partIndex++)
            {
                PrototypeMasterpiecePartSnapshot part =
                    snapshot.Parts[partIndex];

                if (part == null)
                {
                    continue;
                }

                minimum =
                    Vector2.Min(
                        minimum,
                        part.NormalizedStart);

                minimum =
                    Vector2.Min(
                        minimum,
                        part.NormalizedEnd);

                maximum =
                    Vector2.Max(
                        maximum,
                        part.NormalizedStart);

                maximum =
                    Vector2.Max(
                        maximum,
                        part.NormalizedEnd);
            }

            if (float.IsInfinity(minimum.x) ||
                float.IsInfinity(minimum.y))
            {
                minimum = Vector2.zero;
                maximum = Vector2.one;
            }

            float width =
                Mathf.Max(
                    0.18f,
                    maximum.x -
                    minimum.x);

            float height =
                Mathf.Max(
                    0.24f,
                    maximum.y -
                    minimum.y);

            float scale =
                Mathf.Min(
                    anchor.WorldWidth /
                        width,
                    anchor.WorldHeight /
                        height);

            return new Mapping
            {
                Center =
                    new Vector2(
                        (minimum.x + maximum.x) *
                        0.5f,
                        0f),
                MinimumY = minimum.y,
                Scale = scale,
                FloorOffset = anchor.FloorOffset
            };
        }

        private void CreateRuntimeMaterials(
            Material strokeTemplate,
            Material proxyTemplate)
        {
            runtimeMaterialsReady = false;

            if (strokeTemplate != null)
            {
                strokeMaterial = new Material(strokeTemplate)
                {
                    name = "M60_MasterpieceStroke_Runtime",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (proxyTemplate != null)
            {
                proxyMaterial = new Material(proxyTemplate)
                {
                    name = "M60_MasterpieceProxy_Runtime",
                    hideFlags = HideFlags.DontSave
                };
            }

            Shader strokeShader = Shader.Find(
                "PaintedAlive/M58 Final/Stroke Telegraph");

            if (strokeShader == null)
            {
                strokeShader = Shader.Find(
                    "Universal Render Pipeline/Unlit");
            }

            if (strokeShader == null)
            {
                strokeShader = Shader.Find("Sprites/Default");
            }

            if (strokeMaterial == null && strokeShader != null)
            {
                strokeMaterial = new Material(strokeShader)
                {
                    name = "M60_MasterpieceStroke_Runtime",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (proxyMaterial == null && strokeShader != null)
            {
                proxyMaterial = new Material(strokeShader)
                {
                    name = "M60_MasterpieceProxy_Runtime",
                    hideFlags = HideFlags.DontSave
                };
            }

            ConfigureVisibleLineMaterial(strokeMaterial, false);
            ConfigureVisibleLineMaterial(proxyMaterial, true);
            runtimeMaterialsReady =
                strokeMaterial != null &&
                strokeMaterial.shader != null &&
                proxyMaterial != null &&
                proxyMaterial.shader != null;
        }

        private static void ConfigureVisibleLineMaterial(
            Material material,
            bool proxy)
        {
            if (material == null)
            {
                return;
            }

            Color white = Color.white;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", white);
            if (material.HasProperty("_EdgeColor"))
                material.SetColor("_EdgeColor", proxy
                    ? new Color(0.12f, 0.82f, 0.90f, 1f)
                    : white);
            if (material.HasProperty("_DashFill"))
                material.SetFloat("_DashFill", proxy ? 0.52f : 0.92f);
            if (material.HasProperty("_PulseStrength"))
                material.SetFloat("_PulseStrength", proxy ? 0.14f : 0.04f);

            material.renderQueue = 3100;
        }

        private void CreateKinematicBody()
        {
            body =
                gameObject.AddComponent<
                    Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            body.interpolation =
                RigidbodyInterpolation.None;

            body.collisionDetectionMode =
                CollisionDetectionMode.Discrete;
        }

        private void CreatePart(
            PrototypeMasterpiecePartSnapshot snapshot,
            Mapping mapping)
        {
            GameObject partObject =
                new GameObject(
                    $"Part_{snapshot.Kind}");

            partObject.transform.SetParent(
                transform,
                false);

            partRoots[snapshot.Kind] =
                partObject.transform;

            GameObject proxyObject =
                new GameObject(
                    $"Proxy_{snapshot.Kind}");

            proxyObject.transform.SetParent(
                partObject.transform,
                false);

            Collider proxyCollider;

            if (snapshot.ProxyKind ==
                PrototypeMasterpieceProxyKind.Circle)
            {
                SphereCollider sphere =
                    proxyObject.AddComponent<
                        SphereCollider>();

                sphere.center =
                    Vector3.zero;

                sphere.radius =
                    mapping.RadiusToWorld(
                        snapshot.NormalizedRadius);

                proxyObject.transform.localPosition =
                    mapping.ToLocal(
                        snapshot.NormalizedStart);

                proxyCollider = sphere;

                CreateProxyRing(
                    partObject.transform,
                    snapshot.NormalizedStart,
                    snapshot.NormalizedRadius,
                    mapping,
                    $"ProxyRing_{snapshot.Kind}");
            }
            else
            {
                Vector3 start =
                    mapping.ToLocal(
                        snapshot.NormalizedStart);

                Vector3 end =
                    mapping.ToLocal(
                        snapshot.NormalizedEnd);

                Vector3 direction =
                    end - start;

                float length =
                    direction.magnitude;

                float radius =
                    mapping.RadiusToWorld(
                        snapshot.NormalizedRadius);

                proxyObject.transform.localPosition =
                    (start + end) *
                    0.5f;

                float angle =
                    Mathf.Atan2(
                        direction.y,
                        direction.x) *
                    Mathf.Rad2Deg -
                    90f;

                proxyObject.transform.localRotation =
                    Quaternion.AngleAxis(
                        angle,
                        Vector3.forward);

                CapsuleCollider capsule =
                    proxyObject.AddComponent<
                        CapsuleCollider>();

                capsule.direction = 1;
                capsule.center = Vector3.zero;
                capsule.radius = radius;
                capsule.height =
                    Mathf.Max(
                        radius * 2f,
                        length +
                        radius * 2f);

                proxyCollider = capsule;

                CreateProxyAxis(
                    partObject.transform,
                    start,
                    end,
                    $"ProxyAxis_{snapshot.Kind}");

                CreateProxyRingLocal(
                    partObject.transform,
                    start,
                    radius,
                    $"ProxyStart_{snapshot.Kind}");

                CreateProxyRingLocal(
                    partObject.transform,
                    end,
                    radius,
                    $"ProxyEnd_{snapshot.Kind}");
            }

            proxyCollider.isTrigger = true;

            PrototypeMasterpieceWorldPart part =
                partObject.AddComponent<
                    PrototypeMasterpieceWorldPart>();

            part.Configure(
                snapshot,
                proxyCollider);
        }

        private void CreateStrokeVisuals(
            PrototypeMasterpieceDeploymentSnapshot snapshot,
            Mapping mapping)
        {
            const int maximumVisualSegments = 160;
            const int maximumRenderedPoints = 4096;

            for (int strokeIndex = 0;
                 strokeIndex < snapshot.Strokes.Count;
                 strokeIndex++)
            {
                if (visualSegmentCount >=
                        maximumVisualSegments ||
                    renderedPointCount >=
                        maximumRenderedPoints)
                {
                    break;
                }

                PrototypeMasterpieceStrokeSnapshot stroke =
                    snapshot.Strokes[strokeIndex];

                if (stroke == null ||
                    stroke.Points.Count < 2)
                {
                    continue;
                }

                PrototypeMasterpiecePartSnapshot currentPart =
                    FindVisualPart(
                        snapshot.Parts,
                        stroke.Points[0]);

                var run =
                    new List<Vector2>
                    {
                        stroke.Points[0]
                    };

                for (int pointIndex = 1;
                     pointIndex < stroke.Points.Count;
                     pointIndex++)
                {
                    Vector2 previous =
                        stroke.Points[
                            pointIndex - 1];

                    Vector2 current =
                        stroke.Points[
                            pointIndex];

                    Vector2 midpoint =
                        (previous + current) *
                        0.5f;

                    PrototypeMasterpiecePartSnapshot segmentPart =
                        FindVisualPart(
                            snapshot.Parts,
                            midpoint);

                    if (segmentPart != null &&
                        currentPart != null &&
                        segmentPart.Kind !=
                            currentPart.Kind)
                    {
                        run.Add(previous);
                        CreateStrokeRun(
                            currentPart,
                            run,
                            stroke,
                            mapping);

                        run.Clear();
                        run.Add(previous);
                        currentPart =
                            segmentPart;
                    }

                    run.Add(current);
                }

                CreateStrokeRun(
                    currentPart,
                    run,
                    stroke,
                    mapping);
            }
        }

        private void CreateStrokeRun(
            PrototypeMasterpiecePartSnapshot part,
            List<Vector2> points,
            PrototypeMasterpieceStrokeSnapshot stroke,
            Mapping mapping)
        {
            if (part == null ||
                points == null ||
                points.Count < 2 ||
                !partRoots.TryGetValue(
                    part.Kind,
                    out Transform partRoot))
            {
                return;
            }

            GameObject lineObject =
                new GameObject(
                    $"Stroke_{visualSegmentCount:000}");

            lineObject.transform.SetParent(
                partRoot,
                false);

            LineRenderer line =
                lineObject.AddComponent<
                    LineRenderer>();

            line.useWorldSpace = false;
            line.loop = false;
            line.alignment =
                LineAlignment.View;

            line.textureMode =
                LineTextureMode.Stretch;

            line.numCornerVertices = 5;
            line.numCapVertices = 5;
            line.shadowCastingMode =
                ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.sharedMaterial =
                strokeMaterial;

            Color partColor =
                ResolvePartColor(
                    part.Kind);

            // Preserve some of the original ink while still making the six
            // capability regions readable in the world spike.
            Color finalColor =
                Color.Lerp(
                    stroke.Color,
                    partColor,
                    0.28f);

            finalColor.a = 1f;

            line.startColor =
                finalColor;

            line.endColor =
                finalColor;

            line.sortingOrder = 120;
            line.enabled = true;

            originalStrokeRenderers.Add(
                line);

            line.widthMultiplier =
                Mathf.Clamp(
                    stroke.NormalizedWidth *
                    mapping.Scale,
                    0.025f,
                    0.28f);

            int allowedPointCount =
                Mathf.Min(
                    points.Count,
                    4096 -
                    renderedPointCount);

            line.positionCount =
                allowedPointCount;

            for (int index = 0;
                 index < allowedPointCount;
                 index++)
            {
                line.SetPosition(
                    index,
                    mapping.ToLocal(
                        points[index]));
            }

            visualSegmentCount++;
            renderedPointCount +=
                allowedPointCount;

            if (
                part.Kind ==
                PrototypeMasterpiecePartKind.Head
            )
            {
                semanticHeadVisualSegmentCount++;
                semanticHeadRenderedPointCount +=
                    allowedPointCount;
            }
        }

        private PrototypeMasterpiecePartSnapshot FindVisualPart(
            IReadOnlyList<
                PrototypeMasterpiecePartSnapshot> parts,
            Vector2 point)
        {
            if (
                TryResolveHeadVisualPartition(
                    parts,
                    out PrototypeMasterpiecePartSnapshot head,
                    out Vector2 origin,
                    out Vector2 axis,
                    out float boundaryProjection)
            )
            {
                float projection =
                    Vector2.Dot(
                        point - origin,
                        axis);

                if (
                    projection >=
                    boundaryProjection
                )
                {
                    return head;
                }

                return FindNearestPart(
                    parts,
                    point,
                    excludeHead: true);
            }

            return FindNearestPart(
                parts,
                point,
                excludeHead: false);
        }

        private bool TryResolveHeadVisualPartition(
            IReadOnlyList<
                PrototypeMasterpiecePartSnapshot> parts,
            out PrototypeMasterpiecePartSnapshot head,
            out Vector2 origin,
            out Vector2 axis,
            out float boundaryProjection)
        {
            head = null;
            PrototypeMasterpiecePartSnapshot core = null;
            origin = Vector2.zero;
            axis = Vector2.up;
            boundaryProjection = 0f;

            if (
                parts == null ||
                parts.Count == 0
            )
            {
                return false;
            }

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartSnapshot part =
                    parts[index];

                if (part == null)
                {
                    continue;
                }

                if (
                    part.Kind ==
                    PrototypeMasterpiecePartKind.Head
                )
                {
                    head = part;
                }
                else if (
                    part.Kind ==
                    PrototypeMasterpiecePartKind.Core
                )
                {
                    core = part;
                }
            }

            if (
                head == null ||
                core == null
            )
            {
                return false;
            }

            Vector2 coreCenter =
                ResolvePartCenter(
                    core);

            Vector2 headCenter =
                ResolvePartCenter(
                    head);

            Vector2 coreToHead =
                headCenter -
                coreCenter;

            float coreToHeadDistance =
                coreToHead.magnitude;

            if (
                coreToHeadDistance <=
                0.0001f
            )
            {
                return false;
            }

            origin = coreCenter;
            axis =
                coreToHead /
                coreToHeadDistance;

            float headProjection =
                coreToHeadDistance;

            float nearestLowerCompetitorProjection =
                float.NegativeInfinity;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartSnapshot part =
                    parts[index];

                if (
                    part == null ||
                    part == head
                )
                {
                    continue;
                }

                Vector2 center =
                    ResolvePartCenter(
                        part);

                float projection =
                    Vector2.Dot(
                        center -
                        coreCenter,
                        axis);

                if (
                    projection >=
                        headProjection -
                        0.0001f ||
                    projection <
                        -0.0001f
                )
                {
                    continue;
                }

                nearestLowerCompetitorProjection =
                    Mathf.Max(
                        nearestLowerCompetitorProjection,
                        projection);
            }

            if (
                float.IsNegativeInfinity(
                    nearestLowerCompetitorProjection)
            )
            {
                nearestLowerCompetitorProjection =
                    0f;
            }

            boundaryProjection =
                Mathf.Lerp(
                    nearestLowerCompetitorProjection,
                    headProjection,
                    0.50f);

            boundaryProjection =
                Mathf.Clamp(
                    boundaryProjection,
                    headProjection *
                        0.30f,
                    headProjection *
                        0.82f);

            return true;
        }

        private static Vector2 ResolvePartCenter(
            PrototypeMasterpiecePartSnapshot part)
        {
            if (part == null)
            {
                return Vector2.zero;
            }

            if (
                part.ProxyKind ==
                PrototypeMasterpieceProxyKind.Circle
            )
            {
                return part.NormalizedStart;
            }

            return
                (
                    part.NormalizedStart +
                    part.NormalizedEnd
                ) *
                0.5f;
        }

        private PrototypeMasterpiecePartSnapshot FindNearestPart(
            IReadOnlyList<
                PrototypeMasterpiecePartSnapshot> parts,
            Vector2 point,
            bool excludeHead)
        {
            PrototypeMasterpiecePartSnapshot best = null;

            float bestDistance =
                float.PositiveInfinity;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartSnapshot part =
                    parts[index];

                if (
                    part == null ||
                    (
                        excludeHead &&
                        part.Kind ==
                            PrototypeMasterpiecePartKind.Head
                    )
                )
                {
                    continue;
                }

                float distance =
                    part.ProxyKind ==
                        PrototypeMasterpieceProxyKind.Circle
                        ? Vector2.Distance(
                            point,
                            part.NormalizedStart)
                        : DistanceToSegment(
                            point,
                            part.NormalizedStart,
                            part.NormalizedEnd);

                distance -=
                    part.NormalizedRadius *
                    0.45f;

                if (distance <
                    bestDistance)
                {
                    bestDistance = distance;
                    best = part;
                }
            }

            return best;
        }

        private void CreateProxyRing(
            Transform parent,
            Vector2 center,
            float normalizedRadius,
            Mapping mapping,
            string objectName)
        {
            CreateProxyRingLocal(
                parent,
                mapping.ToLocal(center),
                mapping.RadiusToWorld(
                    normalizedRadius),
                objectName);
        }

        private void CreateProxyRingLocal(
            Transform parent,
            Vector3 center,
            float radius,
            string objectName)
        {
            GameObject ringObject =
                new GameObject(
                    objectName);

            ringObject.transform.SetParent(
                parent,
                false);

            LineRenderer ring =
                ringObject.AddComponent<
                    LineRenderer>();

            ring.useWorldSpace = false;
            ring.loop = true;
            ring.alignment =
                LineAlignment.View;

            ring.positionCount = 32;
            ring.widthMultiplier = 0.025f;
            ring.startColor =
                new Color(
                    1f,
                    0.76f,
                    0.10f,
                    0.88f);

            ring.endColor =
                ring.startColor;

            ring.sharedMaterial =
                proxyMaterial;

            for (int index = 0;
                 index < 32;
                 index++)
            {
                float angle =
                    index /
                    32f *
                    Mathf.PI *
                    2f;

                ring.SetPosition(
                    index,
                    center +
                    new Vector3(
                        Mathf.Cos(angle) *
                            radius,
                        Mathf.Sin(angle) *
                            radius,
                        -0.02f));
            }

            proxyDebugObjects.Add(
                ringObject);
        }

        private void CreateProxyAxis(
            Transform parent,
            Vector3 start,
            Vector3 end,
            string objectName)
        {
            GameObject axisObject =
                new GameObject(
                    objectName);

            axisObject.transform.SetParent(
                parent,
                false);

            LineRenderer axis =
                axisObject.AddComponent<
                    LineRenderer>();

            axis.useWorldSpace = false;
            axis.loop = false;
            axis.alignment =
                LineAlignment.View;

            axis.positionCount = 2;
            axis.widthMultiplier = 0.032f;
            axis.startColor =
                new Color(
                    1f,
                    0.76f,
                    0.10f,
                    0.90f);

            axis.endColor =
                axis.startColor;

            axis.sharedMaterial =
                proxyMaterial;

            axis.SetPosition(
                0,
                start +
                Vector3.back *
                0.02f);

            axis.SetPosition(
                1,
                end +
                Vector3.back *
                0.02f);

            proxyDebugObjects.Add(
                axisObject);
        }

        private static float DistanceToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment =
                end - start;

            float squaredLength =
                segment.sqrMagnitude;

            if (squaredLength <=
                0.000001f)
            {
                return Vector2.Distance(
                    point,
                    start);
            }

            float t =
                Mathf.Clamp01(
                    Vector2.Dot(
                        point - start,
                        segment) /
                    squaredLength);

            return Vector2.Distance(
                point,
                start +
                segment *
                t);
        }

        private static Color ResolvePartColor(
            PrototypeMasterpiecePartKind kind)
        {
            switch (kind)
            {
                case PrototypeMasterpiecePartKind.Head:
                    return new Color(
                        0.05f,
                        0.72f,
                        0.78f,
                        1f);

                case PrototypeMasterpiecePartKind.LeftAttack:
                    return new Color(
                        1f,
                        0.43f,
                        0.06f,
                        1f);

                case PrototypeMasterpiecePartKind.RightAttack:
                    return new Color(
                        0.88f,
                        0.17f,
                        0.08f,
                        1f);

                case PrototypeMasterpiecePartKind.LeftContact:
                    return new Color(
                        0.14f,
                        0.48f,
                        0.88f,
                        1f);

                case PrototypeMasterpiecePartKind.RightContact:
                    return new Color(
                        0.34f,
                        0.25f,
                        0.78f,
                        1f);

                default:
                    return new Color(
                        0.08f,
                        0.07f,
                        0.06f,
                        1f);
            }
        }

        private void OnDestroy()
        {
            if (strokeMaterial != null)
            {
                Destroy(strokeMaterial);
            }

            if (proxyMaterial != null)
            {
                Destroy(proxyMaterial);
            }
        }
    }
}
