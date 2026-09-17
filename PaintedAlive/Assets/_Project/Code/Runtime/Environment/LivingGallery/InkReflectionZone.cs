using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class InkReflectionZone : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField, Min(0.1f)] private float radius = 3.3f;

        public string BindingId => bindingId;
        public float Radius => radius;

        public void Configure(string newBindingId, float newRadius)
        {
            bindingId = newBindingId ?? string.Empty;
            radius = Mathf.Max(0.1f, newRadius);
        }

        public Vector3 GetDeterministicPoint(int seed)
        {
            System.Random random = new System.Random(seed ^ bindingId.GetHashCode());
            float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
            float r = Mathf.Sqrt((float)random.NextDouble()) * radius;
            return transform.position + transform.right * (Mathf.Cos(angle) * r) +
                   transform.forward * (Mathf.Sin(angle) * r);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
