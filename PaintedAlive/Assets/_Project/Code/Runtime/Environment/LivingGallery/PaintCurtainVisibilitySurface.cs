using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class PaintCurtainVisibilitySurface : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private PainterPartialVisibilitySurface partialSurface;
        [SerializeField] private Transform pivotReference;

        public void Configure(string newBindingId, PainterPartialVisibilitySurface partial, Transform pivot)
        {
            bindingId = newBindingId ?? string.Empty;
            partialSurface = partial;
            pivotReference = pivot;
        }
    }
}
