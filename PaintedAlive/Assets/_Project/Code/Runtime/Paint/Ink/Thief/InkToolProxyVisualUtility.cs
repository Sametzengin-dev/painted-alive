using PaintedAlive.Figures.Tools;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Thief
{
    internal static class InkToolProxyVisualUtility
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        public static GameObject CreateProxy(
            Transform parent,
            FigureToolId tool,
            string objectName,
            Vector3 localPosition,
            float scale = 1f)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * scale;

            switch (tool)
            {
                case FigureToolId.PaletteKnife:
                    CreatePart(
                        root.transform,
                        PrimitiveType.Cube,
                        "Handle",
                        new Vector3(0f, 0f, -0.05f),
                        new Vector3(0.07f, 0.07f, 0.28f),
                        new Color(0.12f, 0.07f, 0.03f, 1f));
                    CreatePart(
                        root.transform,
                        PrimitiveType.Cube,
                        "Blade",
                        new Vector3(0f, 0f, 0.14f),
                        new Vector3(0.13f, 0.025f, 0.16f),
                        new Color(0.68f, 0.72f, 0.74f, 1f));
                    break;

                case FigureToolId.FixativeSpray:
                    CreatePart(
                        root.transform,
                        PrimitiveType.Cylinder,
                        "Can",
                        Vector3.zero,
                        new Vector3(0.09f, 0.16f, 0.09f),
                        new Color(0.2f, 0.7f, 0.78f, 1f));
                    break;

                case FigureToolId.FrameGun:
                    CreatePart(
                        root.transform,
                        PrimitiveType.Cube,
                        "Body",
                        Vector3.zero,
                        new Vector3(0.16f, 0.1f, 0.22f),
                        new Color(0.48f, 0.31f, 0.12f, 1f));
                    CreatePart(
                        root.transform,
                        PrimitiveType.Cube,
                        "Barrel",
                        new Vector3(0f, 0f, 0.15f),
                        new Vector3(0.06f, 0.06f, 0.18f),
                        new Color(0.75f, 0.58f, 0.22f, 1f));
                    break;

                case FigureToolId.Sponge:
                    CreatePart(
                        root.transform,
                        PrimitiveType.Cube,
                        "Sponge",
                        Vector3.zero,
                        new Vector3(0.18f, 0.11f, 0.22f),
                        new Color(0.76f, 0.62f, 0.18f, 1f));
                    break;
            }

            return root;
        }

        public static void ApplyColor(
            Renderer renderer,
            Color color)
        {
            if (renderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }

        private static void CreatePart(
            Transform parent,
            PrimitiveType primitiveType,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();

            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            ApplyColor(renderer, color);
        }
    }
}
