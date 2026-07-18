using UnityEngine;

namespace PaintedAlive.Figures
{
    public sealed class StainDirectionMark : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] markRenderers;

        [SerializeField] private Color markColor =
            new(0.32f, 0.04f, 0.10f, 0.85f);

        [SerializeField, Min(0.1f)]
        private float fadeDuration = 1.5f;

        private MaterialPropertyBlock propertyBlock;
        private float remainingLifetime;
        private float totalLifetime;

        public bool IsActive =>
            gameObject.activeSelf &&
            remainingLifetime > 0f;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (remainingLifetime <= 0f)
                return;

            remainingLifetime -= Time.deltaTime;

            if (remainingLifetime <= 0f)
            {
                Deactivate();
                return;
            }

            float fade = remainingLifetime < fadeDuration
                ? Mathf.Clamp01(remainingLifetime / fadeDuration)
                : 1f;

            ApplyColor(fade);
        }

        public void Activate(
            Vector3 position,
            Quaternion rotation,
            float lifetime)
        {
            Initialize();

            transform.SetPositionAndRotation(position, rotation);

            totalLifetime = Mathf.Max(0.1f, lifetime);
            remainingLifetime = totalLifetime;

            gameObject.SetActive(true);
            ApplyColor(1f);
        }

        public void Deactivate()
        {
            remainingLifetime = 0f;
            gameObject.SetActive(false);
        }

        private void Initialize()
        {
            propertyBlock ??= new MaterialPropertyBlock();

            if (markRenderers == null ||
                markRenderers.Length == 0)
            {
                markRenderers =
                    GetComponentsInChildren<Renderer>(true);
            }
        }

        private void ApplyColor(float alphaMultiplier)
        {
            Color color = markColor;
            color.a *= alphaMultiplier;

            foreach (Renderer markRenderer in markRenderers)
            {
                if (markRenderer == null)
                    continue;

                markRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                markRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
