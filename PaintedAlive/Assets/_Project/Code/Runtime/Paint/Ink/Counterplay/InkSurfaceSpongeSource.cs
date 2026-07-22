using PaintedAlive.Paint.Sponge;
using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkSurface))]
    public sealed class InkSurfaceSpongeSource :
        MonoBehaviour,
        ISpongeAbsorbableSource
    {
        [SerializeField]
        private InkSurface inkSurface;

        [SerializeField]
        private Color inkColor =
            new Color(0.015f, 0.01f, 0.025f, 1f);

        public bool CanAbsorb =>
            isActiveAndEnabled &&
            inkSurface != null &&
            inkSurface.IsInitialized &&
            inkSurface.InkAmount > 0.001f;

        public float AvailableAmount =>
            inkSurface != null ? inkSurface.InkAmount : 0f;

        public Color PaintColor => inkColor;

        public float Instability =>
            inkSurface != null ? inkSurface.Wetness : 0f;

        private void Awake()
        {
            inkSurface ??= GetComponent<InkSurface>();
        }

        public void Configure(InkSurface surface)
        {
            inkSurface = surface;
        }

        public float Absorb(float requestedAmount)
        {
            return inkSurface != null
                ? inkSurface.AbsorbInk(requestedAmount)
                : 0f;
        }
    }
}
