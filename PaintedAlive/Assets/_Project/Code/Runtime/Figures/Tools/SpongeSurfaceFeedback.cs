using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    /// <summary>
    /// Presentation-only bridge from the authoritative sponge reservoir to
    /// the tactile sponge shader. It never mutates capacity or paint state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpongeSurfaceFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int AbsorbedColorId =
            Shader.PropertyToID("_AbsorbedColor");

        private static readonly int FillId =
            Shader.PropertyToID("_Fill");

        private static readonly int InstabilityId =
            Shader.PropertyToID("_Instability");

        [SerializeField]
        private SpongeReservoir reservoir;

        [SerializeField]
        private Renderer spongeRenderer;

        [SerializeField]
        private Color drySpongeColor =
            new(0.72f, 0.46f, 0.12f, 1f);

        private MaterialPropertyBlock propertyBlock;

        public SpongeReservoir Reservoir => reservoir;
        public Renderer SpongeRenderer => spongeRenderer;
        public bool IsConfigured =>
            reservoir != null && spongeRenderer != null;

        public void Configure(
            SpongeReservoir targetReservoir,
            Renderer targetRenderer)
        {
            bool wasActive = isActiveAndEnabled;

            if (wasActive && reservoir != null)
            {
                reservoir.ReservoirChanged -= Refresh;
            }

            reservoir = targetReservoir;
            spongeRenderer = targetRenderer;

            if (wasActive && reservoir != null)
            {
                reservoir.ReservoirChanged += Refresh;
            }

            Refresh();
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (reservoir == null)
            {
                reservoir = GetComponent<SpongeReservoir>();
            }

            if (reservoir != null)
            {
                reservoir.ReservoirChanged -= Refresh;
                reservoir.ReservoirChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (reservoir != null)
            {
                reservoir.ReservoirChanged -= Refresh;
            }

            ApplyValues(0f, drySpongeColor, 0f);
        }

        private void Refresh()
        {
            if (reservoir == null || spongeRenderer == null)
            {
                return;
            }

            Color absorbed = reservoir.IsEmpty
                ? drySpongeColor
                : reservoir.StoredColor;
            absorbed.a = 1f;

            ApplyValues(
                reservoir.NormalizedFill,
                absorbed,
                reservoir.MixtureInstability);
        }

        private void ApplyValues(
            float fill,
            Color absorbedColor,
            float instability)
        {
            if (spongeRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            spongeRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, drySpongeColor);
            propertyBlock.SetColor(
                AbsorbedColorId,
                absorbedColor);
            propertyBlock.SetFloat(FillId, Mathf.Clamp01(fill));
            propertyBlock.SetFloat(
                InstabilityId,
                Mathf.Clamp01(instability));
            spongeRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
