using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class PainterPartialVisibilitySurface : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField, Range(0f, 1f)] private float informationRetention = 0.35f;

        public string BindingId => bindingId;
        public Renderer TargetRenderer => targetRenderer;
        public float InformationRetention => informationRetention;

        public void Configure(string newBindingId, Renderer renderer, float retention = 0.35f)
        {
            bindingId = newBindingId ?? string.Empty;
            targetRenderer = renderer;
            informationRetention = Mathf.Clamp01(retention);
        }

        // Conservative renderer-bounds test for systems that need a cheap
        // partial-information query before a dedicated Painter render pass exists.
        public bool LiesBetween(Vector3 observer, Vector3 target)
        {
            if (targetRenderer == null || !targetRenderer.enabled)
                return false;
            Vector3 delta = target - observer;
            float distance = delta.magnitude;
            if (distance <= 0.001f)
                return false;
            Ray ray = new Ray(observer, delta / distance);
            return targetRenderer.bounds.IntersectRay(ray, out float hitDistance) &&
                   hitDistance >= 0f && hitDistance < distance;
        }
    }
}
