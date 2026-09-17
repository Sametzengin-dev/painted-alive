using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [DisallowMultipleComponent]
    public sealed class PalimpsestRespawnPoint : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField, Min(0.1f)] private float safeRadius = 1.2f;

        public string BindingId => bindingId;
        public float SafeRadius => safeRadius;

        public void Configure(string id, float radius)
        {
            bindingId = id ?? string.Empty;
            safeRadius = Mathf.Max(0.1f, radius);
        }
    }
}
