using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Painters.SideCanvas
{
    /// <summary>
    /// Reliable standard-UI raster presenter for the side canvas.
    /// Stroke data stays normalized in the controller. The Texture2D exists
    /// only as a local visual preview and is never saved or networked.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class PrototypeSideCanvasSurfaceGraphic :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLivingSideCanvasController source;

        [SerializeField] private RawImage targetImage;

        [Header("Raster Preview")]
        [SerializeField, Range(256, 1024)]
        private int maximumTextureDimension = 768;

        [SerializeField, Range(0.15f, 1f)]
        private float resolutionScale = 0.72f;

        [SerializeField] private Color emptyGuideColor =
            new Color(0.14f, 0.13f, 0.12f, 0.13f);

        [SerializeField] private Color coreColor =
            new Color(1f, 0.56f, 0.08f, 1f);

        [SerializeField] private Color headColor =
            new Color(0.12f, 0.78f, 0.82f, 1f);

        [SerializeField] private Color handColor =
            new Color(0.86f, 0.27f, 0.18f, 1f);

        [SerializeField] private Color footColor =
            new Color(0.36f, 0.52f, 0.92f, 1f);

        [Header("Runtime Read Only")]
        [SerializeField] private int textureWidth;
        [SerializeField] private int textureHeight;
        [SerializeField] private int rebuildCount;
        [SerializeField] private bool textureReady;

        private Texture2D canvasTexture;
        private Color32[] pixels;

        public int TextureWidth => textureWidth;
        public int TextureHeight => textureHeight;
        public int RebuildCount => rebuildCount;
        public bool TextureReady => textureReady;
        public bool UsesStandardRawImage =>
            targetImage != null;

        public void Configure(
            PrototypeLivingSideCanvasController configuredSource)
        {
            source = configuredSource;
            ResolveTargetImage();
            EnsureTexture();
            RebuildTexture();
        }

        public void Refresh()
        {
            ResolveTargetImage();
            EnsureTexture();
            RebuildTexture();
        }

        private void Awake()
        {
            ResolveTargetImage();
        }

        private void OnEnable()
        {
            ResolveTargetImage();
            EnsureTexture();
            RebuildTexture();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            EnsureTexture();
            RebuildTexture();
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        private void ResolveTargetImage()
        {
            if (targetImage == null)
            {
                targetImage =
                    GetComponent<RawImage>();
            }

            if (targetImage == null)
            {
                return;
            }

            targetImage.color = Color.white;
            targetImage.uvRect =
                new Rect(0f, 0f, 1f, 1f);
        }

        private void EnsureTexture()
        {
            if (targetImage == null)
            {
                textureReady = false;
                return;
            }

            Rect rect =
                targetImage.rectTransform.rect;

            if (rect.width < 2f ||
                rect.height < 2f)
            {
                textureReady = false;
                return;
            }

            int desiredWidth =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        rect.width *
                        resolutionScale),
                    256,
                    maximumTextureDimension);

            int desiredHeight =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        rect.height *
                        resolutionScale),
                    256,
                    maximumTextureDimension);

            if (canvasTexture != null &&
                desiredWidth == textureWidth &&
                desiredHeight == textureHeight)
            {
                textureReady = true;
                return;
            }

            ReleaseTexture();

            textureWidth = desiredWidth;
            textureHeight = desiredHeight;

            canvasTexture =
                new Texture2D(
                    textureWidth,
                    textureHeight,
                    TextureFormat.RGBA32,
                    false,
                    false)
                {
                    name =
                        "M45_SideCanvas_RuntimeRaster",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };

            pixels =
                new Color32[
                    textureWidth *
                    textureHeight];

            targetImage.texture = canvasTexture;
            textureReady = true;
        }

        private void RebuildTexture()
        {
            if (!textureReady ||
                canvasTexture == null ||
                pixels == null)
            {
                return;
            }

            Array.Clear(
                pixels,
                0,
                pixels.Length);

            DrawGuide();

            if (source != null)
            {
                DrawStrokes(source.Strokes);
                DrawMarkers(source.Markers);
            }

            canvasTexture.SetPixels32(pixels);
            canvasTexture.Apply(false, false);
            targetImage.texture = canvasTexture;
            targetImage.SetMaterialDirty();
            targetImage.SetVerticesDirty();
            rebuildCount++;
        }

        private void DrawGuide()
        {
            int spacing =
                Mathf.Max(
                    30,
                    Mathf.Min(
                        textureWidth,
                        textureHeight) /
                    10);

            Color32 guide =
                (Color32)emptyGuideColor;

            for (int x = spacing;
                 x < textureWidth;
                 x += spacing)
            {
                DrawLine(
                    new Vector2Int(x, 0),
                    new Vector2Int(
                        x,
                        textureHeight - 1),
                    1,
                    guide);
            }

            for (int y = spacing;
                 y < textureHeight;
                 y += spacing)
            {
                DrawLine(
                    new Vector2Int(0, y),
                    new Vector2Int(
                        textureWidth - 1,
                        y),
                    1,
                    guide);
            }
        }

        private void DrawStrokes(
            IReadOnlyList<PrototypeSideCanvasStroke> strokes)
        {
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
                        2,
                        Mathf.RoundToInt(
                            stroke.Width *
                            Mathf.Min(
                                textureWidth,
                                textureHeight) *
                            0.5f));

                Color32 strokeColor =
                    (Color32)stroke.Color;

                Vector2Int previous =
                    ToPixel(stroke.Points[0]);

                DrawDisk(
                    previous.x,
                    previous.y,
                    radius,
                    strokeColor);

                for (int pointIndex = 1;
                     pointIndex < stroke.Points.Count;
                     pointIndex++)
                {
                    Vector2Int current =
                        ToPixel(
                            stroke.Points[pointIndex]);

                    DrawLine(
                        previous,
                        current,
                        radius,
                        strokeColor);

                    previous = current;
                }

                DrawDisk(
                    previous.x,
                    previous.y,
                    radius,
                    strokeColor);
            }
        }

        private void DrawMarkers(
            IReadOnlyList<PrototypeRigMarkerPlacement> markers)
        {
            int radius =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        Mathf.Min(
                            textureWidth,
                            textureHeight) *
                        0.024f),
                    7,
                    18);

            for (int index = 0;
                 index < markers.Count;
                 index++)
            {
                PrototypeRigMarkerPlacement marker =
                    markers[index];

                if (marker == null)
                {
                    continue;
                }

                Vector2Int center =
                    ToPixel(
                        marker.NormalizedPosition);

                Color32 markerColor =
                    (Color32)ResolveMarkerColor(
                        marker.Kind);

                DrawDisk(
                    center.x,
                    center.y,
                    radius + 3,
                    new Color32(0, 0, 0, 190));

                if (marker.Kind ==
                    PrototypeRigMarkerKind.Core)
                {
                    DrawDiamond(
                        center.x,
                        center.y,
                        radius,
                        markerColor);
                }
                else
                {
                    DrawDisk(
                        center.x,
                        center.y,
                        radius,
                        markerColor);
                }

                DrawDisk(
                    center.x,
                    center.y,
                    Mathf.Max(2, radius / 3),
                    new Color32(
                        255,
                        255,
                        255,
                        255));
            }
        }

        private Vector2Int ToPixel(Vector2 normalized)
        {
            return new Vector2Int(
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        normalized.x *
                        (textureWidth - 1)),
                    0,
                    textureWidth - 1),
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        normalized.y *
                        (textureHeight - 1)),
                    0,
                    textureHeight - 1));
        }

        private void DrawLine(
            Vector2Int from,
            Vector2Int to,
            int radius,
            Color32 colorValue)
        {
            int steps =
                Mathf.Max(
                    1,
                    Mathf.Max(
                        Mathf.Abs(to.x - from.x),
                        Mathf.Abs(to.y - from.y)));

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
                            from.x,
                            to.x,
                            t)),
                    Mathf.RoundToInt(
                        Mathf.Lerp(
                            from.y,
                            to.y,
                            t)),
                    radius,
                    colorValue);
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
                     Mathf.Max(0, centerY - radius);
                 y <=
                     Mathf.Min(
                         textureHeight - 1,
                         centerY + radius);
                 y++)
            {
                int deltaY =
                    y - centerY;

                for (int x =
                         Mathf.Max(0, centerX - radius);
                     x <=
                         Mathf.Min(
                             textureWidth - 1,
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

        private void DrawDiamond(
            int centerX,
            int centerY,
            int radius,
            Color32 colorValue)
        {
            for (int y = -radius;
                 y <= radius;
                 y++)
            {
                int width =
                    radius -
                    Mathf.Abs(y);

                for (int x = -width;
                     x <= width;
                     x++)
                {
                    BlendPixel(
                        centerX + x,
                        centerY + y,
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
                x >= textureWidth ||
                y < 0 ||
                y >= textureHeight)
            {
                return;
            }

            int index =
                y * textureWidth + x;

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

            if (outputAlpha <= 0.0001f)
            {
                pixels[index] =
                    new Color32(0, 0, 0, 0);
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
                            outputAlpha * 255f),
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
                    (source *
                     sourceAlpha +
                     destination *
                     destinationAlpha *
                     (1f - sourceAlpha)) /
                    outputAlpha),
                0,
                255);
        }

        private Color ResolveMarkerColor(
            PrototypeRigMarkerKind kind)
        {
            switch (kind)
            {
                case PrototypeRigMarkerKind.Core:
                    return coreColor;

                case PrototypeRigMarkerKind.Head:
                    return headColor;

                case PrototypeRigMarkerKind.LeftHand:
                case PrototypeRigMarkerKind.RightHand:
                    return handColor;

                default:
                    return footColor;
            }
        }

        private void ReleaseTexture()
        {
            textureReady = false;
            pixels = null;
            textureWidth = 0;
            textureHeight = 0;

            if (targetImage != null)
            {
                targetImage.texture = null;
            }

            if (canvasTexture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(canvasTexture);
            }
            else
            {
                DestroyImmediate(canvasTexture);
            }

            canvasTexture = null;
        }
    }
}
