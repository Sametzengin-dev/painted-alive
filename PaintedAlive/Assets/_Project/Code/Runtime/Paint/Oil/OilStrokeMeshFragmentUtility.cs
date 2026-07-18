using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Paint
{
    public static class OilStrokeMeshFragmentUtility
    {
        public static List<Mesh> SplitConnectedComponents(
            Mesh source,
            int minimumTriangleCount)
        {
            var result = new List<Mesh>();

            if (source == null || !source.isReadable)
                return result;

            int[] sourceTriangles = source.triangles;
            Vector3[] sourceVertices = source.vertices;

            int triangleCount = sourceTriangles.Length / 3;

            if (triangleCount <= 0 || sourceVertices.Length == 0)
                return result;

            int[] parents = new int[triangleCount];
            byte[] ranks = new byte[triangleCount];

            for (int i = 0; i < triangleCount; i++)
                parents[i] = i;

            var firstTriangleByVertex =
                new Dictionary<int, int>(sourceVertices.Length);

            for (int triangle = 0;
                 triangle < triangleCount;
                 triangle++)
            {
                int firstIndex = triangle * 3;

                for (int corner = 0; corner < 3; corner++)
                {
                    int vertex = sourceTriangles[firstIndex + corner];

                    if (firstTriangleByVertex.TryGetValue(
                            vertex,
                            out int connectedTriangle))
                    {
                        Union(
                            parents,
                            ranks,
                            triangle,
                            connectedTriangle);
                    }
                    else
                    {
                        firstTriangleByVertex.Add(vertex, triangle);
                    }
                }
            }

            var groups = new Dictionary<int, List<int>>();

            for (int triangle = 0;
                 triangle < triangleCount;
                 triangle++)
            {
                int root = Find(parents, triangle);

                if (!groups.TryGetValue(root, out List<int> group))
                {
                    group = new List<int>();
                    groups.Add(root, group);
                }

                group.Add(triangle);
            }

            Vector3[] sourceNormals = source.normals;
            Vector4[] sourceTangents = source.tangents;
            Vector2[] sourceUv = source.uv;
            Color[] sourceColors = source.colors;

            IEnumerable<List<int>> orderedGroups =
                groups.Values
                    .Where(group =>
                        group.Count >= minimumTriangleCount)
                    .OrderBy(group => group[0]);

            int fragmentIndex = 0;

            foreach (List<int> group in orderedGroups)
            {
                Mesh fragment = BuildFragmentMesh(
                    source,
                    sourceVertices,
                    sourceTriangles,
                    sourceNormals,
                    sourceTangents,
                    sourceUv,
                    sourceColors,
                    group,
                    fragmentIndex);

                if (fragment != null)
                {
                    result.Add(fragment);
                    fragmentIndex++;
                }
            }

            return result;
        }

        private static Mesh BuildFragmentMesh(
            Mesh source,
            Vector3[] sourceVertices,
            int[] sourceTriangles,
            Vector3[] sourceNormals,
            Vector4[] sourceTangents,
            Vector2[] sourceUv,
            Color[] sourceColors,
            List<int> group,
            int fragmentIndex)
        {
            var vertexMap = new Dictionary<int, int>();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>(group.Count * 3);

            bool copyNormals =
                sourceNormals.Length == sourceVertices.Length;
            bool copyTangents =
                sourceTangents.Length == sourceVertices.Length;
            bool copyUv = sourceUv.Length == sourceVertices.Length;
            bool copyColors =
                sourceColors.Length == sourceVertices.Length;

            foreach (int sourceTriangle in group)
            {
                int firstIndex = sourceTriangle * 3;

                for (int corner = 0; corner < 3; corner++)
                {
                    int sourceVertex =
                        sourceTriangles[firstIndex + corner];

                    if (!vertexMap.TryGetValue(
                            sourceVertex,
                            out int targetVertex))
                    {
                        targetVertex = vertices.Count;
                        vertexMap.Add(sourceVertex, targetVertex);

                        vertices.Add(sourceVertices[sourceVertex]);

                        if (copyNormals)
                            normals.Add(sourceNormals[sourceVertex]);

                        if (copyTangents)
                            tangents.Add(sourceTangents[sourceVertex]);

                        if (copyUv)
                            uv.Add(sourceUv[sourceVertex]);

                        if (copyColors)
                            colors.Add(sourceColors[sourceVertex]);
                    }

                    triangles.Add(targetVertex);
                }
            }

            if (vertices.Count < 4 || triangles.Count < 6)
                return null;

            var mesh = new Mesh
            {
                name = $"{source.name}_Fragment_{fragmentIndex:00}",
                indexFormat =
                    vertices.Count > ushort.MaxValue
                        ? IndexFormat.UInt32
                        : IndexFormat.UInt16
            };

            mesh.SetVertices(vertices);

            if (copyNormals)
                mesh.SetNormals(normals);

            if (copyTangents)
                mesh.SetTangents(tangents);

            if (copyUv)
                mesh.SetUVs(0, uv);

            if (copyColors)
                mesh.SetColors(colors);

            mesh.SetTriangles(triangles, 0, true);

            if (!copyNormals)
                mesh.RecalculateNormals();

            if (!copyTangents && copyUv)
                mesh.RecalculateTangents();

            mesh.RecalculateBounds();

            return mesh;
        }

        private static int Find(int[] parents, int item)
        {
            int root = item;

            while (parents[root] != root)
                root = parents[root];

            while (parents[item] != item)
            {
                int parent = parents[item];
                parents[item] = root;
                item = parent;
            }

            return root;
        }

        private static void Union(
            int[] parents,
            byte[] ranks,
            int first,
            int second)
        {
            int firstRoot = Find(parents, first);
            int secondRoot = Find(parents, second);

            if (firstRoot == secondRoot)
                return;

            if (ranks[firstRoot] < ranks[secondRoot])
            {
                parents[firstRoot] = secondRoot;
            }
            else if (ranks[firstRoot] > ranks[secondRoot])
            {
                parents[secondRoot] = firstRoot;
            }
            else
            {
                parents[secondRoot] = firstRoot;
                ranks[firstRoot]++;
            }
        }
    }
}
