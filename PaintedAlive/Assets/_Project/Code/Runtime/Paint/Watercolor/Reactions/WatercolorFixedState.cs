using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    [DisallowMultipleComponent]
    public sealed class WatercolorFixedState : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [SerializeField]
        private WatercolorFlowSurface flowSurface;

        [SerializeField]
        private Renderer targetRenderer;

        [SerializeField]
        private bool isFrozen;

        [SerializeField]
        private float frozenAtTime = -1f;

        private MaterialPropertyBlock propertyBlock;

        public bool IsFrozen => isFrozen;
        public float FrozenAtTime => frozenAtTime;

        private void Awake()
        {
            flowSurface ??= GetComponent<WatercolorFlowSurface>();
            targetRenderer ??= GetComponentInChildren<Renderer>();
        }

        public bool Freeze(Color fixedColor)
        {
            if (isFrozen)
            {
                return false;
            }

            flowSurface ??= GetComponent<WatercolorFlowSurface>();
            targetRenderer ??= GetComponentInChildren<Renderer>();
            isFrozen = true;
            frozenAtTime = Time.time;

            if (flowSurface != null)
            {
                flowSurface.enabled = false;
            }

            if (targetRenderer != null)
            {
                propertyBlock ??= new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, fixedColor);
                propertyBlock.SetColor(ColorId, fixedColor);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            return true;
        }
    }
}
