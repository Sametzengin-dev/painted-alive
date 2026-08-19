using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Painters.SideCanvas
{
    public enum PrototypeSideCanvasMode
    {
        Draw,
        Rig,
        Preview
    }

    public enum PrototypeRigMarkerKind
    {
        Core,
        Head,
        LeftHand,
        RightHand,
        LeftFoot,
        RightFoot
    }

    [Serializable]
    public sealed class PrototypeSideCanvasStroke
    {
        [SerializeField] private List<Vector2> points =
            new List<Vector2>();

        [SerializeField] private Color color = Color.black;

        [SerializeField, Min(0.001f)] private float width = 0.018f;

        public List<Vector2> Points => points;
        public Color Color => color;
        public float Width => width;

        public PrototypeSideCanvasStroke(
            Color configuredColor,
            float configuredWidth)
        {
            color = configuredColor;
            width = Mathf.Max(0.001f, configuredWidth);
        }
    }

    [Serializable]
    public sealed class PrototypeRigMarkerPlacement
    {
        [SerializeField] private PrototypeRigMarkerKind kind;
        [SerializeField] private Vector2 normalizedPosition;

        public PrototypeRigMarkerKind Kind => kind;
        public Vector2 NormalizedPosition
        {
            get => normalizedPosition;
            set => normalizedPosition = value;
        }

        public PrototypeRigMarkerPlacement(
            PrototypeRigMarkerKind configuredKind,
            Vector2 configuredPosition)
        {
            kind = configuredKind;
            normalizedPosition = configuredPosition;
        }
    }
}
