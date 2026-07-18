using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace PaintedAlive.Paint
{
    public enum OilStrokeState
    {
        Wet,
        Drying,
        Dry
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class OilStrokeRuntime : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int SmoothnessId =
            Shader.PropertyToID("_Smoothness");

        private readonly List<Vector3> controlPoints = new();
        private readonly List<Vector2> cutIntervals = new();

        private OilStrokeConfig config;
        private Material wetMaterial;
        private Material dryMaterial;

        private SplineContainer splineContainer;
        private Spline spline;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh generatedMesh;
        private MaterialPropertyBlock materialPropertyBlock;

        private bool finalized;
        private float lifecycleElapsed;


        private OilStrokePressureProfile pressureProfile =
            OilStrokePressureProfile.Balanced;
        public bool HasRenderableGeometry => controlPoints.Count >= 2;
        public OilStrokeState State { get; private set; }
        public bool IsFinalized => finalized;
        public OilStrokeShape Shape { get; private set; }

        public OilStrokePressureProfile PressureProfile =>
            pressureProfile;

        public float OriginalLength =>
            splineContainer != null && HasRenderableGeometry
                ? splineContainer.CalculateLength()
                : 0f;

        public int CutCount { get; private set; }
        public int WetCutCount { get; private set; }
        public int DryingCutCount { get; private set; }
        public int DryCutCount { get; private set; }

        public void Initialize(
            OilStrokeConfig strokeConfig,
            Material initialWetMaterial,
            Material finalDryMaterial,
            OilStrokeShape strokeShape)
        {
            Initialize(
                strokeConfig,
                initialWetMaterial,
                finalDryMaterial,
                strokeShape,
                OilStrokePressureProfile.Balanced);
        }

        public void Initialize(
            OilStrokeConfig strokeConfig,
            Material initialWetMaterial,
            Material finalDryMaterial,
            OilStrokeShape strokeShape,
            OilStrokePressureProfile strokePressureProfile)
        {
            config = strokeConfig;
            wetMaterial = initialWetMaterial;
            dryMaterial = finalDryMaterial;
            Shape = strokeShape;


            pressureProfile =
                strokePressureProfile.IsValid
                    ? strokePressureProfile
                    : OilStrokePressureProfile.Balanced;
            splineContainer = GetComponent<SplineContainer>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            spline = new Spline();
            splineContainer.Spline = spline;

            generatedMesh = new Mesh
            {
                name = $"{name}_RuntimeMesh",
                indexFormat = IndexFormat.UInt32
            };

            generatedMesh.MarkDynamic();

            meshFilter.sharedMesh = generatedMesh;

            meshRenderer.sharedMaterial =
                wetMaterial != null
                    ? wetMaterial
                    : dryMaterial;

            meshCollider.sharedMesh = null;

            materialPropertyBlock =
                new MaterialPropertyBlock();

            State = OilStrokeState.Wet;
            ApplyLifecycleVisual(0f);
        }

        private void Update()
        {
            if (!finalized || config == null)
            {
                return;
            }

            lifecycleElapsed += Time.deltaTime;

            float lifecycleMultiplier =
                Mathf.Max(
                    0.1f,
                    pressureProfile.LifecycleDurationMultiplier);

            float wetEnd =
                config.WetDuration *
                lifecycleMultiplier;

            float dryingDuration =
                config.DryingDuration *
                lifecycleMultiplier;

            float dryEnd =
                wetEnd + dryingDuration;

            if (lifecycleElapsed < wetEnd)
            {
                State = OilStrokeState.Wet;
                ApplyLifecycleVisual(0f);
                return;
            }

            if (dryingDuration > 0f &&
                lifecycleElapsed < dryEnd)
            {
                State = OilStrokeState.Drying;

                float dryingProgress =
                    Mathf.InverseLerp(
                        wetEnd,
                        dryEnd,
                        lifecycleElapsed);

                ApplyLifecycleVisual(dryingProgress);
                return;
            }

            State = OilStrokeState.Dry;
            ApplyLifecycleVisual(1f);
        }

        public bool TryAppendWorldPoint(
            Vector3 worldPoint)
        {
            if (config == null ||
                controlPoints.Count >=
                config.MaximumControlPoints)
            {
                return false;
            }

            Vector3 localPoint =
                transform.InverseTransformPoint(
                    worldPoint);

            if (controlPoints.Count > 0)
            {
                Vector3 previousPoint =
                    controlPoints[
                        controlPoints.Count - 1];

                float minimumDistanceSquared =
                    config.ControlPointSpacing *
                    config.ControlPointSpacing;

                if ((localPoint - previousPoint)
                    .sqrMagnitude <
                    minimumDistanceSquared)
                {
                    return false;
                }
            }

            controlPoints.Add(localPoint);

            spline.Add(
                new float3(
                    localPoint.x,
                    localPoint.y,
                    localPoint.z),
                TangentMode.AutoSmooth);

            if (controlPoints.Count >= 2)
            {
                RebuildMesh();
            }

            return true;
        }

        public void FinalizeStroke()
        {
            if (!HasRenderableGeometry)
            {
                return;
            }

            finalized = true;
            lifecycleElapsed = 0f;
            State = OilStrokeState.Wet;

            RebuildMesh();
            ApplyLifecycleVisual(0f);
        }

        public bool TryCutWorldPoint(
            Vector3 worldPoint,
            float requestedGapWidth)
        {
            if (!HasRenderableGeometry ||
                requestedGapWidth <= 0f)
            {
                return false;
            }

            float splineLength =
                splineContainer.CalculateLength();

            if (splineLength <= 0.001f)
            {
                return false;
            }

            Vector3 localPoint =
                transform.InverseTransformPoint(
                    worldPoint);

            var localFloatPoint = new float3(
                localPoint.x,
                localPoint.y,
                localPoint.z);

            SplineUtility.GetNearestPoint(
                spline,
                localFloatPoint,
                out _,
                out float normalizedT,
                8,
                3);

            float stateMultiplier =
                GetCutMultiplier();

            float normalizedHalfWidth =
                requestedGapWidth *
                stateMultiplier *
                0.5f /
                splineLength;

            normalizedHalfWidth =
                Mathf.Clamp(
                    normalizedHalfWidth,
                    0.002f,
                    0.5f);

            Vector2 newInterval = new(
                Mathf.Clamp01(
                    normalizedT -
                    normalizedHalfWidth),
                Mathf.Clamp01(
                    normalizedT +
                    normalizedHalfWidth));

            AddAndMergeCutInterval(newInterval);
            RebuildMesh();

            CutCount++;

            switch (State)
            {
                case OilStrokeState.Wet:
                    WetCutCount++;
                    break;

                case OilStrokeState.Drying:
                    DryingCutCount++;
                    break;

                case OilStrokeState.Dry:
                    DryCutCount++;
                    break;
            }

            return true;
        }

        private float GetCutMultiplier()
        {
            float stateMultiplier = State switch
            {
                OilStrokeState.Wet =>
                    config.WetCutMultiplier,

                OilStrokeState.Drying =>
                    config.DryingCutMultiplier,

                OilStrokeState.Dry =>
                    config.DryCutMultiplier,

                _ => 1f
            };

            float resistance =
                Mathf.Max(
                    0.1f,
                    pressureProfile.CutResistanceMultiplier);

            return stateMultiplier / resistance;
        }

        private void AddAndMergeCutInterval(
            Vector2 newInterval)
        {
            cutIntervals.Add(newInterval);

            cutIntervals.Sort(
                (first, second) =>
                    first.x.CompareTo(second.x));

            for (int i = cutIntervals.Count - 1;
                 i > 0;
                 i--)
            {
                Vector2 previous =
                    cutIntervals[i - 1];

                Vector2 current =
                    cutIntervals[i];

                if (current.x >
                    previous.y + 0.0001f)
                {
                    continue;
                }

                cutIntervals[i - 1] =
                    new Vector2(
                        Mathf.Min(
                            previous.x,
                            current.x),
                        Mathf.Max(
                            previous.y,
                            current.y));

                cutIntervals.RemoveAt(i);
            }
        }

        private bool IsSegmentCut(
            float startT,
            float endT)
        {
            foreach (Vector2 interval
                     in cutIntervals)
            {
                bool overlaps =
                    interval.x < endT &&
                    interval.y > startT;

                if (overlaps)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildMesh()
        {
            int segmentCount =
                Mathf.Max(
                    1,
                    spline.Count - 1);

            int sampleCount =
                segmentCount *
                config.SamplesPerSegment +
                1;

            var vertices =
                new List<Vector3>(
                    sampleCount * 4);

            var triangles =
                new List<int>(
                    (sampleCount - 1) *
                    24 +
                    24);

            var uv =
                new List<Vector2>(
                    sampleCount * 4);

            float halfWidth =
                config.GetWidth(Shape) *
                pressureProfile.WidthMultiplier *
                0.5f;

            for (int i = 0;
                 i < sampleCount;
                 i++)
            {
                float t =
                    i /
                    (float)(sampleCount - 1);

                bool evaluated =
                    splineContainer.Evaluate(
                        t,
                        out float3 worldPosition,
                        out float3 worldTangent,
                        out _);

                if (!evaluated)
                {
                    return;
                }

                Vector3 localPosition =
                    transform.InverseTransformPoint(
                        new Vector3(
                            worldPosition.x,
                            worldPosition.y,
                            worldPosition.z));

                Vector3 localTangent =
                    transform
                        .InverseTransformDirection(
                            new Vector3(
                                worldTangent.x,
                                worldTangent.y,
                                worldTangent.z));

                localTangent =
                    Vector3.ProjectOnPlane(
                        localTangent,
                        Vector3.up);

                if (localTangent.sqrMagnitude <
                    0.0001f)
                {
                    localTangent =
                        Vector3.forward;
                }

                localTangent.Normalize();

                Vector3 side =
                    Vector3.Cross(
                            Vector3.up,
                            localTangent)
                        .normalized;

                Vector3 bottomLeft =
                    localPosition -
                    side * halfWidth;

                Vector3 bottomRight =
                    localPosition +
                    side * halfWidth;

                float sampleHeight =
                    config.GetHeight(
                        Shape,
                        t) *
                    pressureProfile.HeightMultiplier;

                Vector3 topLeft =
                    bottomLeft +
                    Vector3.up *
                    sampleHeight;

                Vector3 topRight =
                    bottomRight +
                    Vector3.up *
                    sampleHeight;

                vertices.Add(bottomLeft);
                vertices.Add(bottomRight);
                vertices.Add(topLeft);
                vertices.Add(topRight);

                uv.Add(new Vector2(0f, t));
                uv.Add(new Vector2(1f, t));
                uv.Add(new Vector2(0f, t));
                uv.Add(new Vector2(1f, t));
            }

            int meshSegmentCount =
                sampleCount - 1;

            var cutSegments =
                new bool[meshSegmentCount];

            for (int i = 0;
                 i < meshSegmentCount;
                 i++)
            {
                float startT =
                    i /
                    (float)meshSegmentCount;

                float endT =
                    (i + 1) /
                    (float)meshSegmentCount;

                cutSegments[i] =
                    IsSegmentCut(
                        startT,
                        endT);
            }

            for (int i = 0;
                 i < meshSegmentCount;
                 i++)
            {
                if (cutSegments[i])
                {
                    continue;
                }

                int current = i * 4;
                int next = (i + 1) * 4;

                int bottomLeftCurrent =
                    current;

                int bottomRightCurrent =
                    current + 1;

                int topLeftCurrent =
                    current + 2;

                int topRightCurrent =
                    current + 3;

                int bottomLeftNext =
                    next;

                int bottomRightNext =
                    next + 1;

                int topLeftNext =
                    next + 2;

                int topRightNext =
                    next + 3;

                AddQuad(
                    triangles,
                    topLeftCurrent,
                    topLeftNext,
                    topRightNext,
                    topRightCurrent);

                AddQuad(
                    triangles,
                    bottomRightCurrent,
                    topRightCurrent,
                    topRightNext,
                    bottomRightNext);

                AddQuad(
                    triangles,
                    bottomLeftCurrent,
                    bottomLeftNext,
                    topLeftNext,
                    topLeftCurrent);

                AddQuad(
                    triangles,
                    bottomLeftCurrent,
                    bottomRightCurrent,
                    bottomRightNext,
                    bottomLeftNext);

                bool needsStartCap =
                    i == 0 ||
                    cutSegments[i - 1];

                bool needsEndCap =
                    i ==
                    meshSegmentCount - 1 ||
                    cutSegments[i + 1];

                if (needsStartCap)
                {
                    AddQuad(
                        triangles,
                        bottomLeftCurrent,
                        topLeftCurrent,
                        topRightCurrent,
                        bottomRightCurrent);
                }

                if (needsEndCap)
                {
                    AddQuad(
                        triangles,
                        bottomLeftNext,
                        bottomRightNext,
                        topRightNext,
                        topLeftNext);
                }
            }

            generatedMesh.Clear();

            generatedMesh.SetVertices(
                vertices);

            generatedMesh.SetUVs(
                0,
                uv);

            generatedMesh.SetTriangles(
                triangles,
                0,
                true);

            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();

            meshCollider.sharedMesh = null;

            if (triangles.Count > 0)
            {
                meshCollider.sharedMesh =
                    generatedMesh;
            }
        }

        private void ApplyLifecycleVisual(
            float dryingProgress)
        {
            Material fallbackWet =
                wetMaterial != null
                    ? wetMaterial
                    : dryMaterial;

            Material fallbackDry =
                dryMaterial != null
                    ? dryMaterial
                    : wetMaterial;

            if (fallbackWet == null ||
                fallbackDry == null)
            {
                return;
            }

            if (State == OilStrokeState.Dry)
            {
                meshRenderer.SetPropertyBlock(null);

                meshRenderer.sharedMaterial =
                    fallbackDry;

                return;
            }

            meshRenderer.sharedMaterial =
                fallbackWet;

            if (State == OilStrokeState.Wet)
            {
                meshRenderer.SetPropertyBlock(null);
                return;
            }

            Color wetColor =
                GetMaterialColor(
                    fallbackWet);

            Color dryColor =
                GetMaterialColor(
                    fallbackDry);

            float wetSmoothness =
                GetMaterialSmoothness(
                    fallbackWet);

            float drySmoothness =
                GetMaterialSmoothness(
                    fallbackDry);

            materialPropertyBlock.Clear();

            materialPropertyBlock.SetColor(
                BaseColorId,
                Color.Lerp(
                    wetColor,
                    dryColor,
                    dryingProgress));

            materialPropertyBlock.SetFloat(
                SmoothnessId,
                Mathf.Lerp(
                    wetSmoothness,
                    drySmoothness,
                    dryingProgress));

            meshRenderer.SetPropertyBlock(
                materialPropertyBlock);
        }

        private static Color GetMaterialColor(
            Material material)
        {
            return material.HasProperty(
                    BaseColorId)
                ? material.GetColor(
                    BaseColorId)
                : Color.white;
        }

        private static float GetMaterialSmoothness(
            Material material)
        {
            return material.HasProperty(
                    SmoothnessId)
                ? material.GetFloat(
                    SmoothnessId)
                : 0.5f;
        }

        private static void AddQuad(
            List<int> triangles,
            int a,
            int b,
            int c,
            int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);

            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        private void OnDestroy()
        {
            if (generatedMesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedMesh);
            }
            else
            {
                DestroyImmediate(generatedMesh);
            }
        }
    }
}