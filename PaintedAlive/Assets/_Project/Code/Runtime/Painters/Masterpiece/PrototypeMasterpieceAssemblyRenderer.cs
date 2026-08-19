using System;
using System.Collections.Generic;
using PaintedAlive.Painters.SideCanvas;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class PrototypeMasterpieceAssemblyRenderer :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLivingSideCanvasController sideCanvas;

        [SerializeField]
        private PrototypeMasterpieceAssemblyController assembly;

        [SerializeField] private RawImage targetImage;

        [Header("Exclusive Preview Ownership")]
        [SerializeField] private RawImage m45PuppetImage;
        [SerializeField] private Text previewTitle;
        [SerializeField] private string assemblyPreviewTitle =
            "PARÇALI BAŞ YAPIT • ÖN İZLEME";

        [Header("Raster")]
        [SerializeField, Range(256, 768)]
        private int textureDimension = 440;

        [SerializeField, Range(10f, 60f)]
        private float refreshRate = 30f;

        [SerializeField] private Color coreColor =
            new Color(0.10f, 0.09f, 0.08f, 1f);

        [SerializeField] private Color headColor =
            new Color(0.04f, 0.58f, 0.64f, 1f);

        [SerializeField] private Color leftAttackColor =
            new Color(0.95f, 0.37f, 0.08f, 1f);

        [SerializeField] private Color rightAttackColor =
            new Color(0.78f, 0.16f, 0.09f, 1f);

        [SerializeField] private Color leftContactColor =
            new Color(0.16f, 0.42f, 0.78f, 1f);

        [SerializeField] private Color rightContactColor =
            new Color(0.30f, 0.28f, 0.74f, 1f);

        [SerializeField] private Color proxyColor =
            new Color(1f, 0.75f, 0.12f, 0.70f);

        [SerializeField] private Color seamColor =
            new Color(0.96f, 0.92f, 0.80f, 0.95f);

        [Header("Runtime Read Only")]
        [SerializeField] private bool textureReady;
        [SerializeField] private bool assemblyVisible;
        [SerializeField] private int rebuildCount;
        [SerializeField] private int renderedPartCount;
        [SerializeField] private bool exclusivePreviewOwnershipActive;
        [SerializeField] private bool m45PuppetSuppressed;

        private Texture2D texture;
        private Color32[] pixels;
        private float nextRefreshAt;
        private bool ownershipStateCaptured;
        private bool originalM45PuppetEnabled;
        private string originalPreviewTitle = string.Empty;

        public bool TextureReady => textureReady;
        public bool AssemblyVisible => assemblyVisible;
        public int RebuildCount => rebuildCount;
        public int RenderedPartCount => renderedPartCount;
        public bool UsesStandardRawImage =>
            targetImage != null;
        public bool ExclusivePreviewOwnershipActive =>
            exclusivePreviewOwnershipActive;
        public bool M45PuppetSuppressed =>
            m45PuppetSuppressed;
        public bool PreviewTitleSwapped =>
            previewTitle != null &&
            previewTitle.text ==
                assemblyPreviewTitle;

        public void Configure(
            PrototypeLivingSideCanvasController configuredSideCanvas,
            PrototypeMasterpieceAssemblyController configuredAssembly)
        {
            sideCanvas = configuredSideCanvas;
            assembly = configuredAssembly;

            ResolveTarget();
            ResolveExclusivePreviewTargets();
            EnsureTexture();
            Refresh();
        }

        public void Refresh()
        {
            ResolveTarget();
            EnsureTexture();
            RebuildTexture();
        }

        private void Awake()
        {
            ResolveTarget();
            ResolveExclusivePreviewTargets();
        }

        private void OnEnable()
        {
            ResolveTarget();
            ResolveExclusivePreviewTargets();
            EnsureTexture();
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime <
                nextRefreshAt)
            {
                return;
            }

            nextRefreshAt =
                Time.unscaledTime +
                1f /
                Mathf.Max(
                    10f,
                    refreshRate);

            if (sideCanvas != null &&
                sideCanvas.IsOpen)
            {
                RebuildTexture();
            }
        }

        private void OnDisable()
        {
            RestorePreviewOwnership();
        }

        private void OnDestroy()
        {
            RestorePreviewOwnership();
            ReleaseTexture();
        }

        private void ResolveTarget()
        {
            if (targetImage == null)
            {
                targetImage =
                    GetComponent<RawImage>();
            }

            if (targetImage != null)
            {
                targetImage.color =
                    Color.white;

                targetImage.raycastTarget =
                    false;
            }
        }

        private void ResolveExclusivePreviewTargets()
        {
            Transform previewPanel =
                transform.parent;

            if (previewPanel == null)
            {
                return;
            }

            if (m45PuppetImage == null)
            {
                PrototypeSideCanvasPuppetGraphic puppetPresenter =
                    previewPanel.GetComponentInChildren<
                        PrototypeSideCanvasPuppetGraphic>(
                            true);

                if (puppetPresenter != null)
                {
                    m45PuppetImage =
                        puppetPresenter.GetComponent<
                            RawImage>();
                }
            }

            if (previewTitle == null)
            {
                Transform titleTransform =
                    previewPanel.Find(
                        "PreviewTitle");

                if (titleTransform != null)
                {
                    previewTitle =
                        titleTransform.GetComponent<
                            Text>();
                }
            }

            CaptureOriginalPreviewState();
        }

        private void CaptureOriginalPreviewState()
        {
            if (ownershipStateCaptured)
            {
                return;
            }

            originalM45PuppetEnabled =
                m45PuppetImage != null &&
                m45PuppetImage.enabled;

            originalPreviewTitle =
                previewTitle != null
                    ? previewTitle.text
                    : string.Empty;

            ownershipStateCaptured = true;
        }

        private void ApplyExclusivePreviewOwnership(
            bool assemblyOwnsPreview)
        {
            ResolveExclusivePreviewTargets();

            exclusivePreviewOwnershipActive =
                assemblyOwnsPreview;

            if (targetImage != null)
            {
                targetImage.enabled =
                    assemblyOwnsPreview;
            }

            if (m45PuppetImage != null)
            {
                bool shouldShowM45 =
                    !assemblyOwnsPreview &&
                    originalM45PuppetEnabled;

                m45PuppetImage.enabled =
                    shouldShowM45;

                m45PuppetSuppressed =
                    assemblyOwnsPreview &&
                    !m45PuppetImage.enabled;
            }
            else
            {
                m45PuppetSuppressed = false;
            }

            if (previewTitle != null)
            {
                previewTitle.text =
                    assemblyOwnsPreview
                        ? assemblyPreviewTitle
                        : originalPreviewTitle;
            }
        }

        private void RestorePreviewOwnership()
        {
            exclusivePreviewOwnershipActive = false;
            m45PuppetSuppressed = false;

            if (!ownershipStateCaptured)
            {
                return;
            }

            if (m45PuppetImage != null)
            {
                m45PuppetImage.enabled =
                    originalM45PuppetEnabled;
            }

            if (previewTitle != null)
            {
                previewTitle.text =
                    originalPreviewTitle;
            }

            if (targetImage != null)
            {
                targetImage.enabled = false;
            }
        }

        private void EnsureTexture()
        {
            if (targetImage == null)
            {
                textureReady = false;
                return;
            }

            int dimension =
                Mathf.Clamp(
                    textureDimension,
                    256,
                    768);

            if (texture != null &&
                texture.width == dimension &&
                texture.height == dimension)
            {
                textureReady = true;
                return;
            }

            ReleaseTexture();

            texture =
                new Texture2D(
                    dimension,
                    dimension,
                    TextureFormat.RGBA32,
                    false,
                    false)
                {
                    name =
                        "M46_MasterpieceAssembly_RuntimeRaster",
                    filterMode =
                        FilterMode.Bilinear,
                    wrapMode =
                        TextureWrapMode.Clamp,
                    hideFlags =
                        HideFlags.DontSave
                };

            pixels =
                new Color32[
                    dimension *
                    dimension];

            targetImage.texture = texture;
            textureReady = true;
        }

        private void RebuildTexture()
        {
            if (!textureReady ||
                texture == null ||
                pixels == null)
            {
                return;
            }

            Array.Clear(
                pixels,
                0,
                pixels.Length);

            assemblyVisible =
                assembly != null &&
                assembly.AssemblyReady &&
                sideCanvas != null &&
                sideCanvas.IsOpen &&
                sideCanvas.CurrentMode ==
                    PrototypeSideCanvasMode.Preview;

            ApplyExclusivePreviewOwnership(
                assemblyVisible);

            renderedPartCount = 0;

            if (assemblyVisible)
            {
                DrawSegmentedStrokeBody();

                if (assembly.ProxyOverlayVisible)
                {
                    DrawProxyOverlay();
                }

                DrawPartSeams();
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            targetImage.texture = texture;
            targetImage.SetMaterialDirty();
            targetImage.SetVerticesDirty();
            rebuildCount++;
        }

        private void DrawSegmentedStrokeBody()
        {
            IReadOnlyList<
                PrototypeSideCanvasStroke> strokes =
                sideCanvas.Strokes;

            for (int strokeIndex = 0;
                 strokeIndex < strokes.Count;
                 strokeIndex++)
            {
                PrototypeSideCanvasStroke stroke =
                    strokes[strokeIndex];

                if (stroke == null ||
                    stroke.Points.Count == 0)
                {
                    continue;
                }

                int radius =
                    Mathf.Max(
                        3,
                        Mathf.RoundToInt(
                            stroke.Width *
                            texture.width *
                            0.60f));

                if (stroke.Points.Count == 1)
                {
                    PrototypeMasterpiecePartState pointPart =
                        FindNearestPart(
                            stroke.Points[0]);

                    Vector2 point =
                        ApplyDetachedOffset(
                            stroke.Points[0],
                            pointPart);

                    DrawDisk(
                        ToPixelX(point.x),
                        ToPixelY(point.y),
                        radius,
                        ResolvePartColor(
                            pointPart));

                    continue;
                }

                for (int pointIndex = 1;
                     pointIndex < stroke.Points.Count;
                     pointIndex++)
                {
                    Vector2 from =
                        stroke.Points[
                            pointIndex - 1];

                    Vector2 to =
                        stroke.Points[
                            pointIndex];

                    Vector2 midpoint =
                        (from + to) * 0.5f;

                    PrototypeMasterpiecePartState part =
                        FindNearestPart(
                            midpoint);

                    Vector2 shiftedFrom =
                        ApplyDetachedOffset(
                            from,
                            part);

                    Vector2 shiftedTo =
                        ApplyDetachedOffset(
                            to,
                            part);

                    DrawLine(
                        shiftedFrom,
                        shiftedTo,
                        radius,
                        ResolvePartColor(part));
                }
            }

            IReadOnlyList<
                PrototypeMasterpiecePartState> parts =
                assembly.Parts;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                if (parts[index] != null)
                {
                    renderedPartCount++;
                }
            }
        }

        private void DrawProxyOverlay()
        {
            IReadOnlyList<
                PrototypeMasterpiecePartState> parts =
                assembly.Parts;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState part =
                    parts[index];

                if (part == null)
                {
                    continue;
                }

                Vector2 start =
                    ApplyDetachedOffset(
                        part.NormalizedStart,
                        part);

                Vector2 end =
                    ApplyDetachedOffset(
                        part.NormalizedEnd,
                        part);

                int radius =
                    Mathf.Max(
                        2,
                        Mathf.RoundToInt(
                            part.NormalizedRadius *
                            texture.width));

                if (part.ProxyKind ==
                    PrototypeMasterpieceProxyKind.Circle)
                {
                    DrawCircleOutline(
                        ToPixelX(start.x),
                        ToPixelY(start.y),
                        radius,
                        2,
                        (Color32)proxyColor);
                }
                else
                {
                    DrawLine(
                        start,
                        end,
                        Mathf.Max(
                            1,
                            radius / 6),
                        (Color32)proxyColor);

                    DrawCircleOutline(
                        ToPixelX(start.x),
                        ToPixelY(start.y),
                        radius,
                        2,
                        (Color32)proxyColor);

                    DrawCircleOutline(
                        ToPixelX(end.x),
                        ToPixelY(end.y),
                        radius,
                        2,
                        (Color32)proxyColor);
                }
            }
        }

        private void DrawPartSeams()
        {
            IReadOnlyList<
                PrototypeMasterpiecePartState> parts =
                assembly.Parts;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState part =
                    parts[index];

                if (part == null)
                {
                    continue;
                }

                Vector2 marker =
                    part.ProxyKind ==
                    PrototypeMasterpieceProxyKind.Circle
                        ? part.NormalizedStart
                        : part.NormalizedEnd;

                marker =
                    ApplyDetachedOffset(
                        marker,
                        part);

                DrawCircleOutline(
                    ToPixelX(marker.x),
                    ToPixelY(marker.y),
                    Mathf.Max(
                        4,
                        Mathf.RoundToInt(
                            part.NormalizedRadius *
                            texture.width *
                            0.44f)),
                    2,
                    (Color32)seamColor);
            }
        }

        private PrototypeMasterpiecePartState FindNearestPart(
            Vector2 point)
        {
            IReadOnlyList<
                PrototypeMasterpiecePartState> parts =
                assembly.Parts;

            PrototypeMasterpiecePartState best = null;
            float bestDistance =
                float.PositiveInfinity;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState part =
                    parts[index];

                if (part == null)
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
                    0.40f;

                if (distance <
                    bestDistance)
                {
                    bestDistance = distance;
                    best = part;
                }
            }

            return best;
        }

        private Vector2 ApplyDetachedOffset(
            Vector2 value,
            PrototypeMasterpiecePartState part)
        {
            if (part == null ||
                !part.Detached)
            {
                return value;
            }

            float elapsed =
                Mathf.Max(
                    0f,
                    Time.unscaledTime -
                    part.DetachedAt);

            Vector2 direction =
                ResolveDetachDirection(
                    part.Kind);

            float outward =
                Mathf.Min(
                    0.12f,
                    elapsed * 0.055f);

            float fall =
                Mathf.Min(
                    0.28f,
                    elapsed * elapsed * 0.035f);

            return value +
                direction * outward +
                Vector2.down * fall;
        }

        private Color32 ResolvePartColor(
            PrototypeMasterpiecePartState part)
        {
            if (part == null)
            {
                return (Color32)coreColor;
            }

            Color color;

            switch (part.Kind)
            {
                case PrototypeMasterpiecePartKind.Head:
                    color = headColor;
                    break;

                case PrototypeMasterpiecePartKind.LeftAttack:
                    color = leftAttackColor;
                    break;

                case PrototypeMasterpiecePartKind.RightAttack:
                    color = rightAttackColor;
                    break;

                case PrototypeMasterpiecePartKind.LeftContact:
                    color = leftContactColor;
                    break;

                case PrototypeMasterpiecePartKind.RightContact:
                    color = rightContactColor;
                    break;

                default:
                    color = coreColor;
                    break;
            }

            if (part.Detached)
            {
                color.a *= 0.72f;
            }

            return (Color32)color;
        }

        private static Vector2 ResolveDetachDirection(
            PrototypeMasterpiecePartKind kind)
        {
            switch (kind)
            {
                case PrototypeMasterpiecePartKind.Head:
                    return Vector2.up;

                case PrototypeMasterpiecePartKind.LeftAttack:
                    return new Vector2(-1f, 0.25f).normalized;

                case PrototypeMasterpiecePartKind.RightAttack:
                    return new Vector2(1f, 0.25f).normalized;

                case PrototypeMasterpiecePartKind.LeftContact:
                    return new Vector2(-0.65f, -0.25f).normalized;

                case PrototypeMasterpiecePartKind.RightContact:
                    return new Vector2(0.65f, -0.25f).normalized;

                default:
                    return Vector2.zero;
            }
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

            Vector2 projection =
                start +
                segment * t;

            return Vector2.Distance(
                point,
                projection);
        }

        private void DrawLine(
            Vector2 from,
            Vector2 to,
            int radius,
            Color32 colorValue)
        {
            int fromX = ToPixelX(from.x);
            int fromY = ToPixelY(from.y);
            int toX = ToPixelX(to.x);
            int toY = ToPixelY(to.y);

            int steps =
                Mathf.Max(
                    1,
                    Mathf.Max(
                        Mathf.Abs(toX - fromX),
                        Mathf.Abs(toY - fromY)));

            for (int step = 0;
                 step <= steps;
                 step++)
            {
                float t =
                    step /
                    (float)steps;

                DrawDisk(
                    Mathf.RoundToInt(
                        Mathf.Lerp(
                            fromX,
                            toX,
                            t)),
                    Mathf.RoundToInt(
                        Mathf.Lerp(
                            fromY,
                            toY,
                            t)),
                    radius,
                    colorValue);
            }
        }

        private void DrawCircleOutline(
            int centerX,
            int centerY,
            int radius,
            int thickness,
            Color32 colorValue)
        {
            int outerSquared =
                radius * radius;

            int innerRadius =
                Mathf.Max(
                    0,
                    radius - thickness);

            int innerSquared =
                innerRadius *
                innerRadius;

            for (int y =
                     Mathf.Max(
                         0,
                         centerY - radius);
                 y <=
                     Mathf.Min(
                         texture.height - 1,
                         centerY + radius);
                 y++)
            {
                int deltaY =
                    y - centerY;

                for (int x =
                         Mathf.Max(
                             0,
                             centerX - radius);
                     x <=
                         Mathf.Min(
                             texture.width - 1,
                             centerX + radius);
                     x++)
                {
                    int deltaX =
                        x - centerX;

                    int distanceSquared =
                        deltaX * deltaX +
                        deltaY * deltaY;

                    if (distanceSquared <=
                            outerSquared &&
                        distanceSquared >=
                            innerSquared)
                    {
                        BlendPixel(
                            x,
                            y,
                            colorValue);
                    }
                }
            }
        }

        private void DrawDisk(
            int centerX,
            int centerY,
            int radius,
            Color32 colorValue)
        {
            int radiusSquared =
                radius * radius;

            for (int y =
                     Mathf.Max(
                         0,
                         centerY - radius);
                 y <=
                     Mathf.Min(
                         texture.height - 1,
                         centerY + radius);
                 y++)
            {
                int deltaY =
                    y - centerY;

                for (int x =
                         Mathf.Max(
                             0,
                             centerX - radius);
                     x <=
                         Mathf.Min(
                             texture.width - 1,
                             centerX + radius);
                     x++)
                {
                    int deltaX =
                        x - centerX;

                    if (deltaX * deltaX +
                        deltaY * deltaY >
                        radiusSquared)
                    {
                        continue;
                    }

                    BlendPixel(
                        x,
                        y,
                        colorValue);
                }
            }
        }

        private void BlendPixel(
            int x,
            int y,
            Color32 sourceColor)
        {
            if (x < 0 ||
                x >= texture.width ||
                y < 0 ||
                y >= texture.height)
            {
                return;
            }

            int index =
                y * texture.width + x;

            Color32 destination =
                pixels[index];

            float sourceAlpha =
                sourceColor.a / 255f;

            float destinationAlpha =
                destination.a / 255f;

            float outputAlpha =
                sourceAlpha +
                destinationAlpha *
                (1f - sourceAlpha);

            if (outputAlpha <=
                0.0001f)
            {
                pixels[index] =
                    new Color32(
                        0,
                        0,
                        0,
                        0);

                return;
            }

            pixels[index] =
                new Color32(
                    BlendChannel(
                        sourceColor.r,
                        destination.r,
                        sourceAlpha,
                        destinationAlpha,
                        outputAlpha),
                    BlendChannel(
                        sourceColor.g,
                        destination.g,
                        sourceAlpha,
                        destinationAlpha,
                        outputAlpha),
                    BlendChannel(
                        sourceColor.b,
                        destination.b,
                        sourceAlpha,
                        destinationAlpha,
                        outputAlpha),
                    (byte)Mathf.Clamp(
                        Mathf.RoundToInt(
                            outputAlpha *
                            255f),
                        0,
                        255));
        }

        private static byte BlendChannel(
            byte source,
            byte destination,
            float sourceAlpha,
            float destinationAlpha,
            float outputAlpha)
        {
            return (byte)Mathf.Clamp(
                Mathf.RoundToInt(
                    (
                        source *
                        sourceAlpha +
                        destination *
                        destinationAlpha *
                        (1f - sourceAlpha)
                    ) /
                    outputAlpha),
                0,
                255);
        }

        private int ToPixelX(float normalized)
        {
            return Mathf.Clamp(
                Mathf.RoundToInt(
                    normalized *
                    (texture.width - 1)),
                0,
                texture.width - 1);
        }

        private int ToPixelY(float normalized)
        {
            return Mathf.Clamp(
                Mathf.RoundToInt(
                    normalized *
                    (texture.height - 1)),
                0,
                texture.height - 1);
        }

        private void ReleaseTexture()
        {
            RestorePreviewOwnership();
            textureReady = false;
            assemblyVisible = false;
            pixels = null;

            if (targetImage != null)
            {
                targetImage.texture = null;
            }

            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }

            texture = null;
        }
    }
}
