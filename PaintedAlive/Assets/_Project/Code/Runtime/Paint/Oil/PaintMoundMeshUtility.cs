using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Paint
{
    public static class PaintMoundMeshUtility
    {
        public static Mesh CreateDomeMesh(
            float radius,
            float height,
            int radialSegments,
            int verticalRings,
            string meshName)
        {
            radius = Mathf.Max(0.01f, radius);
            height = Mathf.Max(0.01f, height);
            radialSegments = Mathf.Clamp(radialSegments, 8, 24);
            verticalRings = Mathf.Clamp(verticalRings, 3, 10);

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            vertices.Add(new Vector3(0f, height, 0f));
            uv.Add(new Vector2(0.5f, 1f));

            for (int ring = 1; ring <= verticalRings; ring++)
            {
                float ringNormalized =
                    ring / (float)verticalRings;

                float angle = ringNormalized * Mathf.PI * 0.5f;
                float ringRadius = Mathf.Sin(angle) * radius;
                float ringHeight = Mathf.Cos(angle) * height;

                for (int segment = 0;
                     segment < radialSegments;
                     segment++)
                {
                    float segmentNormalized =
                        segment / (float)radialSegments;

                    float rotation =
                        segmentNormalized * Mathf.PI * 2f;

                    vertices.Add(
                        new Vector3(
                            Mathf.Cos(rotation) * ringRadius,
                            ringHeight,
                            Mathf.Sin(rotation) * ringRadius));

                    uv.Add(
                        new Vector2(
                            segmentNormalized,
                            1f - ringNormalized));
                }
            }

            for (int segment = 0;
                 segment < radialSegments;
                 segment++)
            {
                int current = 1 + segment;
                int next = 1 + (segment + 1) % radialSegments;

                triangles.Add(0);
                triangles.Add(next);
                triangles.Add(current);
            }

            for (int ring = 0;
                 ring < verticalRings - 1;
                 ring++)
            {
                int upperStart = 1 + ring * radialSegments;
                int lowerStart = upperStart + radialSegments;

                for (int segment = 0;
                     segment < radialSegments;
                     segment++)
                {
                    int nextSegment =
                        (segment + 1) % radialSegments;

                    int upperCurrent = upperStart + segment;
                    int upperNext = upperStart + nextSegment;
                    int lowerCurrent = lowerStart + segment;
                    int lowerNext = lowerStart + nextSegment;

                    triangles.Add(upperCurrent);
                    triangles.Add(upperNext);
                    triangles.Add(lowerCurrent);

                    triangles.Add(upperNext);
                    triangles.Add(lowerNext);
                    triangles.Add(lowerCurrent);
                }
            }

            int bottomCenter = vertices.Count;
            vertices.Add(Vector3.zero);
            uv.Add(new Vector2(0.5f, 0.5f));

            int bottomRingStart =
                1 + (verticalRings - 1) * radialSegments;

            for (int segment = 0;
                 segment < radialSegments;
                 segment++)
            {
                int current = bottomRingStart + segment;
                int next =
                    bottomRingStart +
                    (segment + 1) % radialSegments;

                triangles.Add(bottomCenter);
                triangles.Add(current);
                triangles.Add(next);
            }

            var mesh = new Mesh
            {
                name = meshName,
                indexFormat = IndexFormat.UInt32
            };

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
