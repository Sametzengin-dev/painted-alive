using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Painters.SideCanvas
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class PrototypeSideCanvasPuppetGraphic :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLivingSideCanvasController source;

        [SerializeField] private RawImage targetImage;

        [SerializeField, Range(192, 768)]
        private int textureDimension = 420;

        [SerializeField] private Color puppetInk =
            new Color(0.06f, 0.055f, 0.05f, 1f);

        [SerializeField] private Color jointColor =
            new Color(1f, 0.56f, 0.08f, 1f);

        [SerializeField] private Color ghostColor =
            new Color(0.10f, 0.72f, 0.77f, 0.20f);

        [Header("Runtime Read Only")]
        [SerializeField] private bool previewVisible;
        [SerializeField] private int rebuildCount;

        private Texture2D texture;
        private Color32[] pixels;
        private float nextAnimatedRefreshAt;

        public bool PreviewVisible => previewVisible;
        public int RebuildCount => rebuildCount;
        public bool UsesStandardRawImage =>
            targetImage != null;

        public void Configure(
            PrototypeLivingSideCanvasController configuredSource)
        {
            source = configuredSource;
            ResolveTarget();
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
        }

        private void OnEnable()
        {
            ResolveTarget();
            EnsureTexture();
            Refresh();
        }

        private void Update()
        {
            if (source == null ||
                !source.IsOpen ||
                !source.HasCompleteRig)
            {
                if (previewVisible)
                {
                    RebuildTexture();
                }

                return;
            }

            if (Time.unscaledTime <
                nextAnimatedRefreshAt)
            {
                return;
            }

            nextAnimatedRefreshAt =
                Time.unscaledTime +
                1f / 30f;

            RebuildTexture();
        }

        private void OnDestroy()
        {
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

        private void EnsureTexture()
        {
            if (targetImage == null)
            {
                return;
            }

            int dimension =
                Mathf.Clamp(
                    textureDimension,
                    192,
                    768);

            if (texture != null &&
                texture.width == dimension &&
                texture.height == dimension)
            {
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
                        "M45_PuppetPreview_RuntimeRaster",
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
        }

        private void RebuildTexture()
        {
            if (texture == null ||
                pixels == null)
            {
                return;
            }

            Array.Clear(
                pixels,
                0,
                pixels.Length);

            previewVisible =
                source != null &&
                source.HasCompleteRig &&
                source.RigValid;

            if (previewVisible)
            {
                DrawPuppet();
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            targetImage.texture = texture;
            targetImage.SetMaterialDirty();
            targetImage.SetVerticesDirty();
            rebuildCount++;
        }

        private void DrawPuppet()
        {
            puppetInk = source.ResolvePreviewInkColor();

            Dictionary<
                PrototypeRigMarkerKind,
                Vector2> markers =
                BuildMarkerMap(
                    source.Markers);

            if (!markers.TryGetValue(
                    PrototypeRigMarkerKind.Core,
                    out Vector2 coreNormalized) ||
                !markers.TryGetValue(
                    PrototypeRigMarkerKind.Head,
                    out Vector2 headNormalized))
            {
                return;
            }

            Vector2 core =
                ToPixelFloat(coreNormalized);

            Vector2 head =
                ToPixelFloat(headNormalized);

            Vector2 leftHand =
                ToPixelFloat(
                    markers[
                        PrototypeRigMarkerKind.LeftHand]);

            Vector2 rightHand =
                ToPixelFloat(
                    markers[
                        PrototypeRigMarkerKind.RightHand]);

            Vector2 leftFoot =
                ToPixelFloat(
                    markers[
                        PrototypeRigMarkerKind.LeftFoot]);

            Vector2 rightFoot =
                ToPixelFloat(
                    markers[
                        PrototypeRigMarkerKind.RightFoot]);

            float cycle =
                Time.unscaledTime *
                5.2f;

            float walkSwing =
                Mathf.Sin(cycle) *
                13f;

            float attack =
                source.PreviewAttackNormalized;

            head +=
                Vector2.up *
                Mathf.Sin(cycle * 2f) *
                2.2f;

            leftHand =
                RotateAround(
                    leftHand,
                    core,
                    walkSwing -
                    attack * 42f);

            rightHand =
                RotateAround(
                    rightHand,
                    core,
                    -walkSwing -
                    attack * 42f);

            leftFoot =
                RotateAround(
                    leftFoot,
                    core,
                    -walkSwing * 0.62f);

            rightFoot =
                RotateAround(
                    rightFoot,
                    core,
                    walkSwing * 0.62f);

            int limbRadius =
                Mathf.Max(
                    4,
                    texture.width / 42);

            float depthStrength = Mathf.Abs(
                source.DeploymentScale.z - 1f);

            Vector2 depthOffset = new Vector2(1f, -0.62f) *
                (source.DeploymentScale.z - 1f) *
                texture.width * 0.055f;

            if (depthOffset.sqrMagnitude > 0.25f)
            {
                Color depthColor = ghostColor;
                depthColor.a = 0.12f + depthStrength * 0.10f;

                DrawBone(core + depthOffset, head + depthOffset,
                    limbRadius + 2, (Color32)depthColor);
                DrawBone(core + depthOffset, leftHand + depthOffset,
                    limbRadius + 2, (Color32)depthColor);
                DrawBone(core + depthOffset, rightHand + depthOffset,
                    limbRadius + 2, (Color32)depthColor);
                DrawBone(core + depthOffset, leftFoot + depthOffset,
                    limbRadius + 3, (Color32)depthColor);
                DrawBone(core + depthOffset, rightFoot + depthOffset,
                    limbRadius + 3, (Color32)depthColor);
            }

            DrawBone(
                core,
                head,
                limbRadius,
                (Color32)ghostColor);

            DrawBone(
                core,
                leftHand,
                limbRadius,
                (Color32)ghostColor);

            DrawBone(
                core,
                rightHand,
                limbRadius,
                (Color32)ghostColor);

            DrawBone(
                core,
                leftFoot,
                limbRadius + 2,
                (Color32)ghostColor);

            DrawBone(
                core,
                rightFoot,
                limbRadius + 2,
                (Color32)ghostColor);

            DrawBone(
                core,
                head,
                Mathf.Max(2, limbRadius / 2),
                (Color32)puppetInk);

            DrawBone(
                core,
                leftHand,
                Mathf.Max(2, limbRadius / 2),
                (Color32)puppetInk);

            DrawBone(
                core,
                rightHand,
                Mathf.Max(2, limbRadius / 2),
                (Color32)puppetInk);

            DrawBone(
                core,
                leftFoot,
                Mathf.Max(3, limbRadius / 2 + 1),
                (Color32)puppetInk);

            DrawBone(
                core,
                rightFoot,
                Mathf.Max(3, limbRadius / 2 + 1),
                (Color32)puppetInk);

            int bodyRadius =
                Mathf.Max(
                    10,
                    texture.width / 18);

            DrawDisk(
                Mathf.RoundToInt(core.x),
                Mathf.RoundToInt(core.y),
                bodyRadius,
                (Color32)puppetInk);

            DrawDisk(
                Mathf.RoundToInt(head.x),
                Mathf.RoundToInt(head.y),
                Mathf.Max(9, bodyRadius - 2),
                (Color32)puppetInk);

            DrawJoint(core, limbRadius);
            DrawJoint(head, limbRadius);
            DrawJoint(leftHand, limbRadius);
            DrawJoint(rightHand, limbRadius);
            DrawJoint(leftFoot, limbRadius);
            DrawJoint(rightFoot, limbRadius);
        }

        private Vector2 ToPixelFloat(
            Vector2 normalized)
        {
            Vector2 centered =
                normalized -
                new Vector2(
                    0.5f,
                    0.47f);

            float scale =
                texture.width *
                0.76f;

            Vector2 previewScale = new Vector2(
                source != null ? source.DeploymentScale.x : 1f,
                source != null ? source.DeploymentScale.y : 1f);

            centered = Vector2.Scale(centered, previewScale);

            return new Vector2(
                texture.width * 0.5f,
                texture.height * 0.47f) +
                centered *
                scale;
        }

        private void DrawBone(
            Vector2 from,
            Vector2 to,
            int radius,
            Color32 colorValue)
        {
            int steps =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        Vector2.Distance(
                            from,
                            to)));

            for (int index = 0;
                 index <= steps;
                 index++)
            {
                float t =
                    index /
                    (float)steps;

                Vector2 point =
                    Vector2.Lerp(
                        from,
                        to,
                        t);

                DrawDisk(
                    Mathf.RoundToInt(point.x),
                    Mathf.RoundToInt(point.y),
                    radius,
                    colorValue);
            }
        }

        private void DrawJoint(
            Vector2 position,
            int baseRadius)
        {
            int radius =
                Mathf.Max(
                    5,
                    baseRadius);

            DrawDisk(
                Mathf.RoundToInt(position.x),
                Mathf.RoundToInt(position.y),
                radius + 2,
                new Color32(0, 0, 0, 220));

            DrawDisk(
                Mathf.RoundToInt(position.x),
                Mathf.RoundToInt(position.y),
                radius,
                (Color32)jointColor);
        }

        private void DrawDisk(
            int centerX,
            int centerY,
            int radius,
            Color32 colorValue)
        {
            int width = texture.width;
            int height = texture.height;
            int radiusSquared =
                radius * radius;

            for (int y =
                     Mathf.Max(0, centerY - radius);
                 y <=
                     Mathf.Min(
                         height - 1,
                         centerY + radius);
                 y++)
            {
                int deltaY =
                    y - centerY;

                for (int x =
                         Mathf.Max(0, centerX - radius);
                     x <=
                         Mathf.Min(
                             width - 1,
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

                    pixels[
                        y * width + x] =
                        colorValue;
                }
            }
        }

        private static Vector2 RotateAround(
            Vector2 point,
            Vector2 pivot,
            float degrees)
        {
            float radians =
                degrees *
                Mathf.Deg2Rad;

            float cosine =
                Mathf.Cos(radians);

            float sine =
                Mathf.Sin(radians);

            Vector2 offset =
                point - pivot;

            return pivot +
                new Vector2(
                    offset.x * cosine -
                    offset.y * sine,
                    offset.x * sine +
                    offset.y * cosine);
        }

        private static Dictionary<
            PrototypeRigMarkerKind,
            Vector2> BuildMarkerMap(
                IReadOnlyList<
                    PrototypeRigMarkerPlacement> sourceMarkers)
        {
            var result =
                new Dictionary<
                    PrototypeRigMarkerKind,
                    Vector2>();

            for (int index = 0;
                 index < sourceMarkers.Count;
                 index++)
            {
                PrototypeRigMarkerPlacement marker =
                    sourceMarkers[index];

                if (marker != null)
                {
                    result[marker.Kind] =
                        marker.NormalizedPosition;
                }
            }

            return result;
        }

        private void ReleaseTexture()
        {
            previewVisible = false;
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
