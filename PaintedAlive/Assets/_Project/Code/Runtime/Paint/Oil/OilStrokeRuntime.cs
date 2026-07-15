using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class OilStrokeRuntime : MonoBehaviour
    {
        private readonly List<Vector3> controlPoints = new();

        private OilStrokeConfig config;
        private SplineContainer splineContainer;
        private Spline spline;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh generatedMesh;

        public bool HasRenderableGeometry => controlPoints.Count >= 2;

        public void Initialize(
            OilStrokeConfig strokeConfig,
            Material strokeMaterial)
        {
            config = strokeConfig;

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
            meshRenderer.sharedMaterial = strokeMaterial;
            meshCollider.sharedMesh = null;
        }

        public bool TryAppendWorldPoint(Vector3 worldPoint)
        {
            if (config == null ||
                controlPoints.Count >= config.MaximumControlPoints)
            {
                return false;
            }

            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

            if (controlPoints.Count > 0)
            {
                Vector3 previousPoint =
                    controlPoints[controlPoints.Count - 1];

                float minimumDistanceSquared =
                    config.ControlPointSpacing *
                    config.ControlPointSpacing;

                if ((localPoint - previousPoint).sqrMagnitude <
                    minimumDistanceSquared)
                {
                    return false;
                }
            }

            controlPoints.Add(localPoint);

            spline.Add(
                new float3(localPoint.x, localPoint.y, localPoint.z),
                TangentMode.AutoSmooth);

            if (controlPoints.Count >= 2)
            {
                RebuildMesh();
            }

            return true;
        }

        public void FinalizeStroke()
        {
            if (HasRenderableGeometry)
            {
                RebuildMesh();
            }
        }

        private void RebuildMesh()
        {
            int segmentCount = Mathf.Max(1, spline.Count - 1);

            int sampleCount =
                segmentCount * config.SamplesPerSegment + 1;

            var vertices = new List<Vector3>(sampleCount * 4);
            var triangles = new List<int>((sampleCount - 1) * 24 + 12);
            var uv = new List<Vector2>(sampleCount * 4);

            float halfWidth = config.Width * 0.5f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount <= 1
                    ? 0f
                    : i / (float)(sampleCount - 1);

                if (!splineContainer.Evaluate(
                        t,
                        out float3 worldPosition,
                        out float3 worldTangent,
                        out _))
                {
                    continue;
                }

                Vector3 localPosition =
                    transform.InverseTransformPoint(
                        new Vector3(
                            worldPosition.x,
                            worldPosition.y,
                            worldPosition.z));

                Vector3 localTangent =
                    transform.InverseTransformDirection(
                        new Vector3(
                            worldTangent.x,
                            worldTangent.y,
                            worldTangent.z));

                localTangent = Vector3.ProjectOnPlane(
                    localTangent,
                    Vector3.up);

                if (localTangent.sqrMagnitude < 0.0001f)
                {
                    localTangent = Vector3.forward;
                }

                localTangent.Normalize();

                Vector3 side =
                    Vector3.Cross(Vector3.up, localTangent).normalized;

                Vector3 bottomLeft =
                    localPosition - side * halfWidth;

                Vector3 bottomRight =
                    localPosition + side * halfWidth;

                Vector3 topLeft =
                    bottomLeft + Vector3.up * config.Height;

                Vector3 topRight =
                    bottomRight + Vector3.up * config.Height;

                vertices.Add(bottomLeft);
                vertices.Add(bottomRight);
                vertices.Add(topLeft);
                vertices.Add(topRight);

                uv.Add(new Vector2(0f, t));
                uv.Add(new Vector2(1f, t));
                uv.Add(new Vector2(0f, t));
                uv.Add(new Vector2(1f, t));
            }

            for (int i = 0; i < sampleCount - 1; i++)
            {
                int current = i * 4;
                int next = (i + 1) * 4;

                int bottomLeftCurrent = current;
                int bottomRightCurrent = current + 1;
                int topLeftCurrent = current + 2;
                int topRightCurrent = current + 3;

                int bottomLeftNext = next;
                int bottomRightNext = next + 1;
                int topLeftNext = next + 2;
                int topRightNext = next + 3;

                // Top
                AddQuad(
                    triangles,
                    topLeftCurrent,
                    topLeftNext,
                    topRightNext,
                    topRightCurrent);

                // Right side
                AddQuad(
                    triangles,
                    bottomRightCurrent,
                    topRightCurrent,
                    topRightNext,
                    bottomRightNext);

                // Left side
                AddQuad(
                    triangles,
                    bottomLeftCurrent,
                    bottomLeftNext,
                    topLeftNext,
                    topLeftCurrent);

                // Bottom
                AddQuad(
                    triangles,
                    bottomLeftCurrent,
                    bottomRightCurrent,
                    bottomRightNext,
                    bottomLeftNext);
            }

            int finalSample = (sampleCount - 1) * 4;

            // Start cap
            AddQuad(triangles, 0, 2, 3, 1);

            // End cap
            AddQuad(
                triangles,
                finalSample,
                finalSample + 1,
                finalSample + 3,
                finalSample + 2);

            generatedMesh.Clear();
            generatedMesh.SetVertices(vertices);
            generatedMesh.SetUVs(0, uv);
            generatedMesh.SetTriangles(triangles, 0, true);
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = generatedMesh;
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
