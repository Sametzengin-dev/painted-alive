using System;
using System.Collections.Generic;
using PaintedAlive.Painters.SideCanvas;
using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    [Serializable]
    public sealed class PrototypeMasterpieceStrokeSnapshot
    {
        [SerializeField] private List<Vector2> points =
            new List<Vector2>();

        [SerializeField] private Color color = Color.black;

        [SerializeField, Min(0.001f)] private float normalizedWidth;

        public List<Vector2> Points => points;
        public Color Color => color;
        public float NormalizedWidth => normalizedWidth;

        public PrototypeMasterpieceStrokeSnapshot(
            PrototypeSideCanvasStroke source)
        {
            if (source == null)
            {
                return;
            }

            color = source.Color;
            normalizedWidth =
                Mathf.Max(
                    0.001f,
                    source.Width);

            points.AddRange(source.Points);
        }
    }

    [Serializable]
    public sealed class PrototypeMasterpiecePartSnapshot
    {
        [SerializeField]
        private PrototypeMasterpiecePartKind kind;

        [SerializeField]
        private PrototypeMasterpieceCapability capability;

        [SerializeField]
        private PrototypeMasterpieceProxyKind proxyKind;

        [SerializeField] private Vector2 normalizedStart;
        [SerializeField] private Vector2 normalizedEnd;

        [SerializeField, Min(0.001f)]
        private float normalizedRadius;

        public PrototypeMasterpiecePartKind Kind => kind;
        public PrototypeMasterpieceCapability Capability => capability;
        public PrototypeMasterpieceProxyKind ProxyKind => proxyKind;
        public Vector2 NormalizedStart => normalizedStart;
        public Vector2 NormalizedEnd => normalizedEnd;
        public float NormalizedRadius => normalizedRadius;

        public PrototypeMasterpiecePartSnapshot(
            PrototypeMasterpiecePartState source)
        {
            if (source == null)
            {
                return;
            }

            kind = source.Kind;
            capability = source.Capability;
            proxyKind = source.ProxyKind;
            normalizedStart = source.NormalizedStart;
            normalizedEnd = source.NormalizedEnd;
            normalizedRadius =
                Mathf.Max(
                    0.001f,
                    source.NormalizedRadius);
        }
    }

    [Serializable]
    public sealed class PrototypeMasterpieceDeploymentSnapshot
    {
        [SerializeField] private int sourceAssemblyRevision;

        [SerializeField]
        private List<PrototypeMasterpieceStrokeSnapshot> strokes =
            new List<PrototypeMasterpieceStrokeSnapshot>();

        [SerializeField]
        private List<PrototypeMasterpiecePartSnapshot> parts =
            new List<PrototypeMasterpiecePartSnapshot>();

        [SerializeField] private int strokePointCount;
        [SerializeField] private int contentHash;

        public int SourceAssemblyRevision => sourceAssemblyRevision;
        public IReadOnlyList<PrototypeMasterpieceStrokeSnapshot> Strokes =>
            strokes;
        public IReadOnlyList<PrototypeMasterpiecePartSnapshot> Parts =>
            parts;
        public int StrokePointCount => strokePointCount;
        public int ContentHash => contentHash;

        public static bool TryCreate(
            PrototypeLivingSideCanvasController sideCanvas,
            PrototypeMasterpieceAssemblyController assembly,
            out PrototypeMasterpieceDeploymentSnapshot snapshot,
            out string reason)
        {
            snapshot = null;
            reason = string.Empty;

            if (sideCanvas == null)
            {
                reason = "M45 Yan Tuval kaynağı bulunamadı.";
                return false;
            }

            if (assembly == null ||
                !assembly.AssemblyReady)
            {
                reason = "M46 assembly hazır değil.";
                return false;
            }

            if (!sideCanvas.IsOpen ||
                sideCanvas.CurrentMode !=
                    PrototypeSideCanvasMode.Preview)
            {
                reason =
                    "Deploy için Yan Tuval Kukla Testi modunda açık olmalı.";
                return false;
            }

            if (!sideCanvas.RigValid ||
                !sideCanvas.HasCompleteRig)
            {
                reason = "Altı marker'lı geçerli rig gerekli.";
                return false;
            }

            if (assembly.PartCount != 6 ||
                assembly.ProxyCount != 6)
            {
                reason =
                    "M46 sabit altı parça/proxy sözleşmesi eksik.";
                return false;
            }

            if (assembly.DetachedPartCount != 0)
            {
                reason =
                    "Deploy öncesi N ile bütün organları geri tak.";
                return false;
            }

            if (sideCanvas.StrokeCount <= 0)
            {
                reason = "Deploy edilecek stroke bulunamadı.";
                return false;
            }

            var created =
                new PrototypeMasterpieceDeploymentSnapshot
                {
                    sourceAssemblyRevision =
                        assembly.AssemblyRevision
                };

            IReadOnlyList<PrototypeSideCanvasStroke> sourceStrokes =
                sideCanvas.Strokes;

            for (int index = 0;
                 index < sourceStrokes.Count;
                 index++)
            {
                PrototypeSideCanvasStroke source =
                    sourceStrokes[index];

                if (source == null ||
                    source.Points.Count == 0)
                {
                    continue;
                }

                var stroke =
                    new PrototypeMasterpieceStrokeSnapshot(
                        source);

                created.strokes.Add(stroke);
                created.strokePointCount +=
                    stroke.Points.Count;
            }

            IReadOnlyList<PrototypeMasterpiecePartState> sourceParts =
                assembly.Parts;

            for (int index = 0;
                 index < sourceParts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState source =
                    sourceParts[index];

                if (source == null)
                {
                    continue;
                }

                created.parts.Add(
                    new PrototypeMasterpiecePartSnapshot(
                        source));
            }

            if (created.strokes.Count == 0 ||
                created.strokePointCount < 2)
            {
                reason =
                    "En az iki noktalı bir stroke gerekli.";
                return false;
            }

            if (created.parts.Count != 6)
            {
                reason =
                    "Snapshot altı parçayı kopyalayamadı.";
                return false;
            }

            created.contentHash =
                created.CalculateContentHash();

            snapshot = created;
            reason = "Snapshot hazır.";
            return true;
        }

        private int CalculateContentHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + sourceAssemblyRevision;
                hash = hash * 31 + strokePointCount;

                for (int strokeIndex = 0;
                     strokeIndex < strokes.Count;
                     strokeIndex++)
                {
                    PrototypeMasterpieceStrokeSnapshot stroke =
                        strokes[strokeIndex];

                    if (stroke == null)
                    {
                        continue;
                    }

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            stroke.NormalizedWidth *
                            10000f);

                    for (int pointIndex = 0;
                         pointIndex < stroke.Points.Count;
                         pointIndex++)
                    {
                        Vector2 point =
                            stroke.Points[pointIndex];

                        hash = hash * 31 +
                            Mathf.RoundToInt(
                                point.x *
                                10000f);

                        hash = hash * 31 +
                            Mathf.RoundToInt(
                                point.y *
                                10000f);
                    }
                }

                for (int partIndex = 0;
                     partIndex < parts.Count;
                     partIndex++)
                {
                    PrototypeMasterpiecePartSnapshot part =
                        parts[partIndex];

                    if (part == null)
                    {
                        continue;
                    }

                    hash = hash * 31 +
                        (int)part.Kind;

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            part.NormalizedStart.x *
                            10000f);

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            part.NormalizedStart.y *
                            10000f);

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            part.NormalizedEnd.x *
                            10000f);

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            part.NormalizedEnd.y *
                            10000f);

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            part.NormalizedRadius *
                            10000f);
                }

                return hash;
            }
        }
    }
}
