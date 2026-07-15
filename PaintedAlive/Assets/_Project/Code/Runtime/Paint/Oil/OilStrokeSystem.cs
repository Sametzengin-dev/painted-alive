using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilStrokeSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private OilStrokeConfig config;
        [SerializeField] private Material strokeMaterial;

        [Header("Runtime Hierarchy")]
        [SerializeField] private Transform strokesRoot;

        private readonly List<OilStrokeRuntime> strokes = new();

        private OilStrokeRuntime activeStroke;
        private int nextStrokeId = 1;

        public bool IsDrawing => activeStroke != null;

        public float SurfaceOffset =>
            config != null ? config.SurfaceOffset : 0f;

        public void BeginStroke(Vector3 worldPoint)
        {
            EndStroke();

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(OilStrokeSystem)} requires a config.",
                    this);

                return;
            }

            Transform parent = strokesRoot != null
                ? strokesRoot
                : transform;

            var strokeObject = new GameObject(
                $"OilStroke_{nextStrokeId:0000}");

            nextStrokeId++;

            strokeObject.transform.SetParent(parent, false);

            activeStroke =
                strokeObject.AddComponent<OilStrokeRuntime>();

            activeStroke.Initialize(config, strokeMaterial);
            activeStroke.TryAppendWorldPoint(worldPoint);

            strokes.Add(activeStroke);
        }

        public void AppendStrokePoint(Vector3 worldPoint)
        {
            if (activeStroke == null)
            {
                return;
            }

            activeStroke.TryAppendWorldPoint(worldPoint);
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
                if (strokes[i] != null)
                {
                    Destroy(strokes[i].gameObject);
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
