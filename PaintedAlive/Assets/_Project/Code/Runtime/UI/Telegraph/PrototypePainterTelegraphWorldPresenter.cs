using System;
using System.Collections.Generic;
using PaintedAlive.UI.UnifiedHUD;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.UI.Telegraph
{
    [DisallowMultipleComponent]
    public sealed class PrototypePainterTelegraphWorldPresenter :
        MonoBehaviour
    {
        [Serializable]
        private sealed class TelegraphSlot
        {
            public int SourceId;
            public LineRenderer Line;
            public PrototypePainterTelegraphSignal LastSignal;
            public PrototypePainterTelegraphResolution Resolution;
            public float ResolvedAt;
            public bool Active;
        }

        [Header("Role Visibility")]
        [SerializeField]
        private PrototypeUnifiedHudController unifiedHudController;

        [SerializeField]
        private bool showWhilePainterForPrototype;

        [Header("World Visual")]
        [SerializeField, Min(0.001f)] private float worldLift = 0.035f;
        [SerializeField, Min(0.01f)] private float draftingWidth = 0.14f;
        [SerializeField, Min(0.01f)] private float armedWidth = 0.23f;
        [SerializeField, Min(0.05f)] private float resolutionFadeDuration = 0.55f;

        [SerializeField] private Color draftingColor =
            new Color(1f, 0.58f, 0.10f, 0.76f);

        [SerializeField] private Color armedColor =
            new Color(1f, 0.18f, 0.06f, 0.96f);

        [SerializeField] private Color blockedColor =
            new Color(0.58f, 0.58f, 0.60f, 0.75f);

        [SerializeField] private Color committedColor =
            new Color(1f, 0.94f, 0.68f, 1f);

        [Header("Runtime Read Only")]
        [SerializeField] private int activeTelegraphCount;
        [SerializeField] private int committedFlashCount;
        [SerializeField] private int cancelledFadeCount;

        private readonly Dictionary<int, TelegraphSlot> slots =
            new Dictionary<int, TelegraphSlot>();

        private Material runtimeMaterial;

        public int ActiveTelegraphCount => activeTelegraphCount;
        public int CommittedFlashCount => committedFlashCount;
        public int CancelledFadeCount => cancelledFadeCount;
        public bool RoleVisibilityConfigured =>
            unifiedHudController != null;

        public void Configure(
            PrototypeUnifiedHudController configuredHudController)
        {
            unifiedHudController = configuredHudController;
        }

        private void OnEnable()
        {
            PrototypePainterTelegraphHub.TelegraphUpdated +=
                HandleTelegraphUpdated;

            PrototypePainterTelegraphHub.TelegraphResolved +=
                HandleTelegraphResolved;
        }

        private void OnDisable()
        {
            PrototypePainterTelegraphHub.TelegraphUpdated -=
                HandleTelegraphUpdated;

            PrototypePainterTelegraphHub.TelegraphResolved -=
                HandleTelegraphResolved;

            HideAll();
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private void Update()
        {
            bool shouldShow = ShouldShowForCurrentRole();
            float now = Time.time;
            activeTelegraphCount = 0;

            List<int> expired =
                new List<int>();

            foreach (KeyValuePair<int, TelegraphSlot> pair in slots)
            {
                TelegraphSlot slot = pair.Value;
                if (slot == null || slot.Line == null)
                {
                    expired.Add(pair.Key);
                    continue;
                }

                if (slot.Active)
                {
                    ApplyActiveAppearance(
                        slot,
                        shouldShow);

                    if (slot.Line.enabled)
                    {
                        activeTelegraphCount++;
                    }

                    continue;
                }

                float elapsed =
                    now - slot.ResolvedAt;

                float normalized =
                    resolutionFadeDuration > 0f
                        ? Mathf.Clamp01(
                            elapsed /
                            resolutionFadeDuration)
                        : 1f;

                if (normalized >= 1f)
                {
                    slot.Line.enabled = false;
                    expired.Add(pair.Key);
                    continue;
                }

                Color baseColor =
                    slot.Resolution ==
                    PrototypePainterTelegraphResolution.Committed
                        ? committedColor
                        : blockedColor;

                float alpha =
                    1f - normalized;

                baseColor.a *= alpha;
                slot.Line.startColor = baseColor;
                slot.Line.endColor = baseColor;
                slot.Line.widthMultiplier =
                    Mathf.Lerp(
                        armedWidth * 1.35f,
                        draftingWidth * 0.65f,
                        normalized);

                slot.Line.enabled =
                    shouldShow &&
                    slot.Line.positionCount >= 2;
            }

            for (int index = 0;
                 index < expired.Count;
                 index++)
            {
                int sourceId = expired[index];
                if (!slots.TryGetValue(
                        sourceId,
                        out TelegraphSlot slot))
                {
                    continue;
                }

                if (slot.Line != null)
                {
                    Destroy(slot.Line.gameObject);
                }

                slots.Remove(sourceId);
            }
        }

        private void HandleTelegraphUpdated(
            PrototypePainterTelegraphSignal signal)
        {
            if (!signal.HasRenderableGeometry)
            {
                return;
            }

            TelegraphSlot slot = GetOrCreateSlot(signal.SourceId);
            slot.Active = true;
            slot.Resolution =
                PrototypePainterTelegraphResolution.None;
            slot.LastSignal = signal;
            UpdateGeometry(slot.Line, signal.WorldPoints);
        }

        private void HandleTelegraphResolved(
            PrototypePainterTelegraphOutcome outcome)
        {
            TelegraphSlot slot = GetOrCreateSlot(outcome.SourceId);
            slot.Active = false;
            slot.Resolution = outcome.Resolution;
            slot.ResolvedAt = outcome.ResolvedAt;

            if (outcome.WorldPoints != null &&
                outcome.WorldPoints.Length >= 2)
            {
                UpdateGeometry(
                    slot.Line,
                    outcome.WorldPoints);
            }

            if (outcome.Resolution ==
                PrototypePainterTelegraphResolution.Committed)
            {
                committedFlashCount++;
            }
            else
            {
                cancelledFadeCount++;
            }
        }

        private TelegraphSlot GetOrCreateSlot(int sourceId)
        {
            if (slots.TryGetValue(
                    sourceId,
                    out TelegraphSlot existing))
            {
                return existing;
            }

            GameObject lineObject =
                new GameObject(
                    $"M44_PainterTelegraph_{sourceId}");

            lineObject.transform.SetParent(
                transform,
                false);

            LineRenderer line =
                lineObject.AddComponent<LineRenderer>();

            line.useWorldSpace = true;
            line.loop = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCornerVertices = 6;
            line.numCapVertices = 6;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 0;
            line.enabled = false;
            line.sharedMaterial = ResolveMaterial();

            TelegraphSlot created =
                new TelegraphSlot
                {
                    SourceId = sourceId,
                    Line = line,
                    Active = false
                };

            slots.Add(sourceId, created);
            return created;
        }

        private void ApplyActiveAppearance(
            TelegraphSlot slot,
            bool shouldShow)
        {
            PrototypePainterTelegraphSignal signal =
                slot.LastSignal;

            Color color;
            float width;

            switch (signal.Phase)
            {
                case PrototypePainterTelegraphPhase.Armed:
                    color = armedColor;
                    width = armedWidth;
                    break;

                case PrototypePainterTelegraphPhase.Blocked:
                    color = blockedColor;
                    width = draftingWidth;
                    break;

                default:
                    float pulse =
                        0.82f +
                        Mathf.Sin(
                            Time.unscaledTime * 10f) *
                        0.12f;

                    color = draftingColor;
                    color.a *= pulse;
                    width = Mathf.Lerp(
                        draftingWidth * 0.88f,
                        draftingWidth * 1.12f,
                        pulse);
                    break;
            }

            slot.Line.startColor = color;
            slot.Line.endColor = color;
            slot.Line.widthMultiplier = width;
            slot.Line.enabled =
                shouldShow &&
                slot.Line.positionCount >= 2;
        }

        private void UpdateGeometry(
            LineRenderer line,
            Vector3[] points)
        {
            if (line == null || points == null)
            {
                return;
            }

            line.positionCount = points.Length;

            for (int index = 0;
                 index < points.Length;
                 index++)
            {
                line.SetPosition(
                    index,
                    points[index] +
                    Vector3.up * worldLift);
            }
        }

        private bool ShouldShowForCurrentRole()
        {
            if (showWhilePainterForPrototype ||
                unifiedHudController == null)
            {
                return true;
            }

            try
            {
                return !unifiedHudController
                    .BuildSnapshot()
                    .IsPainter;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private Material ResolveMaterial()
        {
            if (runtimeMaterial != null)
            {
                return runtimeMaterial;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find(
                    "Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            runtimeMaterial =
                new Material(shader)
                {
                    name =
                        "M44_PainterTelegraph_RuntimeMaterial",
                    hideFlags = HideFlags.DontSave
                };

            return runtimeMaterial;
        }

        private void HideAll()
        {
            foreach (TelegraphSlot slot in slots.Values)
            {
                if (slot != null &&
                    slot.Line != null)
                {
                    slot.Line.enabled = false;
                }
            }
        }
    }
}
