using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilStrokeSystem : MonoBehaviour
    {
        private const string StrokeLayerName =
            "OilPaint";

        [Header("Configuration")]
        [SerializeField]
        private OilStrokeConfig config;

        [SerializeField]
        private Material wetMaterial;

        [SerializeField]
        private Material dryMaterial;

        [Header("Runtime Hierarchy")]
        [SerializeField]
        private Transform strokesRoot;

        private readonly List<OilStrokeRuntime> strokes =
            new();

        private OilStrokeRuntime activeStroke;
        private int nextStrokeId = 1;

        /// <summary>
        /// Raised only after a stroke has produced renderable geometry and
        /// entered its real wet lifecycle. Presentation systems may observe
        /// this event, but must not mutate gameplay state from it.
        /// </summary>
        public event Action<OilStrokeRuntime> StrokeFinalized;

        public bool IsDrawing =>
            activeStroke != null;

        public float SurfaceOffset =>
            config != null
                ? config.SurfaceOffset
                : 0f;

        public IReadOnlyList<OilStrokeRuntime> Strokes =>
            strokes;
        public OilStrokeRuntime LastFinalizedStroke { get; private set; }

        public float GetPreviewWidth(
            OilStrokeShape shape)
        {
            return config != null
                ? config.GetWidth(shape)
                : 0.5f;
        }

        public float GetPreviewWidth(
            OilStrokeShape shape,
            OilStrokePressureProfile pressureProfile)
        {
            float baseWidth =
                GetPreviewWidth(shape);

            float multiplier =
                pressureProfile.IsValid
                    ? pressureProfile.WidthMultiplier
                    : 1f;

            return baseWidth * multiplier;
        }

        public float GetPigmentMultiplier(
            OilStrokeShape shape)
        {
            return config != null
                ? config.GetPigmentMultiplier(shape)
                : 1f;
        }

        public bool BeginStroke(
            Vector3 worldPoint,
            OilStrokeShape shape)
        {
            return BeginStroke(
                worldPoint,
                shape,
                OilStrokePressureProfile.Balanced);
        }

        public bool BeginStroke(
            Vector3 worldPoint,
            OilStrokeShape shape,
            OilStrokePressureProfile pressureProfile)
        {
            return BeginStrokeInternal(
                worldPoint,
                shape,
                pressureProfile,
                nextStrokeId);
        }

        public bool BeginReplicatedStroke(
            int networkStrokeId,
            Vector3 worldPoint,
            OilStrokeShape shape,
            OilStrokePressureProfile pressureProfile)
        {
            return BeginStrokeInternal(
                worldPoint,
                shape,
                pressureProfile,
                networkStrokeId);
        }

        private bool BeginStrokeInternal(
            Vector3 worldPoint,
            OilStrokeShape shape,
            OilStrokePressureProfile pressureProfile,
            int networkStrokeId)
        {
            EndStroke();
            LastFinalizedStroke = null;

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(OilStrokeSystem)} " +
                    "requires a config.",
                    this);

                return false;
            }

            Transform parent =
                strokesRoot != null
                    ? strokesRoot
                    : transform;

            var strokeObject =
                new GameObject(
                    $"OilStroke_" +
                    $"{Mathf.Max(1, networkStrokeId):0000}");

            strokeObject.transform.SetParent(
                parent,
                false);

            int strokeLayer =
                LayerMask.NameToLayer(
                    StrokeLayerName);

            if (strokeLayer >= 0)
            {
                strokeObject.layer =
                    strokeLayer;
            }
            else
            {
                Debug.LogWarning(
                    $"Layer '{StrokeLayerName}' " +
                    "does not exist.",
                    this);
            }

            activeStroke =
                strokeObject
                    .AddComponent<
                        OilStrokeRuntime>();

            activeStroke.Initialize(
                config,
                wetMaterial,
                dryMaterial,
                shape,
                pressureProfile);

            activeStroke.SetNetworkStrokeId(networkStrokeId);

            bool pointAccepted =
                activeStroke
                    .TryAppendWorldPoint(
                        worldPoint);

            if (!pointAccepted)
            {
                Destroy(strokeObject);
                activeStroke = null;

                return false;
            }

            strokes.Add(activeStroke);
            nextStrokeId = Mathf.Max(
                nextStrokeId,
                networkStrokeId + 1);

            return true;
        }

        public bool AppendStrokePoint(
            Vector3 worldPoint)
        {
            if (activeStroke == null)
            {
                return false;
            }

            return activeStroke
                .TryAppendWorldPoint(
                    worldPoint);
        }

        public void EndStroke()
        {
            if (activeStroke == null)
            {
                return;
            }

            OilStrokeRuntime completedStroke =
                activeStroke;

            activeStroke = null;

            if (!completedStroke
                .HasRenderableGeometry)
            {
                strokes.Remove(
                    completedStroke);

                Destroy(
                    completedStroke.gameObject);

                return;
            }

            completedStroke.FinalizeStroke();
            LastFinalizedStroke = completedStroke;
            StrokeFinalized?.Invoke(completedStroke);
        }

        public bool TryApplyReplicatedCut(
            int networkStrokeId,
            Vector3 worldPoint,
            float gapWidth)
        {
            for (int index = 0; index < strokes.Count; index++)
            {
                OilStrokeRuntime stroke = strokes[index];
                if (stroke != null &&
                    stroke.NetworkStrokeId == networkStrokeId)
                {
                    return stroke.TryCutWorldPoint(worldPoint, gapWidth);
                }
            }

            return false;
        }

        public void ClearAllStrokes()
        {
            activeStroke = null;

            for (int i = strokes.Count - 1;
                 i >= 0;
                 i--)
            {
                OilStrokeRuntime stroke =
                    strokes[i];

                if (stroke != null)
                {
                    Destroy(
                        stroke.gameObject);
                }
            }

            strokes.Clear();
            nextStrokeId = 1;
            LastFinalizedStroke = null;
        }

        private void OnDisable()
        {
            EndStroke();
        }
    }
}
