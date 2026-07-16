using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilStrokeSystem : MonoBehaviour
    {
        private const string StrokeLayerName = "OilPaint";

        [Header("Configuration")]
        [SerializeField] private OilStrokeConfig config;
        [SerializeField] private Material wetMaterial;
        [SerializeField] private Material dryMaterial;

        [Header("Runtime Hierarchy")]
        [SerializeField] private Transform strokesRoot;

        private readonly List<OilStrokeRuntime> strokes = new();

        private OilStrokeRuntime activeStroke;
        private int nextStrokeId = 1;

        public bool IsDrawing => activeStroke != null;

        public float SurfaceOffset =>
            config != null ? config.SurfaceOffset : 0f;

        public bool BeginStroke(Vector3 worldPoint)
        {
            EndStroke();

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(OilStrokeSystem)} requires a config.",
                    this);

                return false;
            }

            Transform parent = strokesRoot != null
                ? strokesRoot
                : transform;

            var strokeObject = new GameObject(
                $"OilStroke_{nextStrokeId:0000}");

            strokeObject.transform.SetParent(parent, false);

            int strokeLayer = LayerMask.NameToLayer(
                StrokeLayerName);

            if (strokeLayer >= 0)
            {
                strokeObject.layer = strokeLayer;
            }
            else
            {
                Debug.LogWarning(
                    $"Layer '{StrokeLayerName}' does not exist.",
                    this);
            }

            activeStroke =
                strokeObject.AddComponent<OilStrokeRuntime>();

            activeStroke.Initialize(
                config,
                wetMaterial,
                dryMaterial);

            bool pointAccepted =
                activeStroke.TryAppendWorldPoint(worldPoint);

            if (!pointAccepted)
            {
                Destroy(strokeObject);
                activeStroke = null;

                return false;
            }

            strokes.Add(activeStroke);
            nextStrokeId++;

            return true;
        }

        public bool AppendStrokePoint(Vector3 worldPoint)
        {
            if (activeStroke == null)
            {
                return false;
            }

            return activeStroke.TryAppendWorldPoint(worldPoint);
        }

        public void EndStroke()
        {
            if (activeStroke == null)
            {
                return;
            }

            OilStrokeRuntime completedStroke = activeStroke;
            activeStroke = null;

            if (!completedStroke.HasRenderableGeometry)
            {
                strokes.Remove(completedStroke);
                Destroy(completedStroke.gameObject);

                return;
            }

            completedStroke.FinalizeStroke();
        }

        public void ClearAllStrokes()
        {
            activeStroke = null;

            for (int i = strokes.Count - 1; i >= 0; i--)
            {
                OilStrokeRuntime stroke = strokes[i];

                if (stroke != null)
                {
                    Destroy(stroke.gameObject);
                }
            }

            strokes.Clear();
            nextStrokeId = 1;
        }

        private void OnDisable()
        {
            EndStroke();
        }
    }
}
