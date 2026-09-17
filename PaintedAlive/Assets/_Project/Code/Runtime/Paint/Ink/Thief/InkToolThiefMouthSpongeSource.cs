using PaintedAlive.Paint.Sponge;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Thief
{
    [DisallowMultipleComponent]
    public sealed class InkToolThiefMouthSpongeSource :
        MonoBehaviour,
        ISpongeAbsorbableSource
    {
        [SerializeField]
        private InkCreatureRuntime creature;

        [SerializeField]
        private InkToolThiefConfig config;

        [SerializeField]
        private Color inkColor =
            new Color(0.015f, 0.01f, 0.025f, 1f);

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float remainingInk;

        [SerializeField]
        private bool initialized;

        public bool CanAbsorb =>
            isActiveAndEnabled &&
            creature != null &&
            creature.IsInitialized &&
            creature.Definition != null &&
            creature.Definition.ContainsGlyph(InkGlyphType.Mouth) &&
            creature.HasGlyph(InkGlyphType.Mouth) &&
            remainingInk > 0.001f;

        public float AvailableAmount => remainingInk;
        public Color PaintColor => inkColor;
        public float Instability => 0.75f;

        private void Awake()
        {
            creature ??= GetComponentInParent<InkCreatureRuntime>();
        }

        private void Start()
        {
            ResetSourceIfNeeded();
        }

        public void Configure(
            InkCreatureRuntime targetCreature,
            InkToolThiefConfig targetConfig)
        {
            creature = targetCreature;
            config = targetConfig;
            ResetSourceIfNeeded(true);
        }

        public float Absorb(float requestedAmount)
        {
            ResetSourceIfNeeded();

            if (!CanAbsorb || requestedAmount <= 0f)
            {
                return 0f;
            }

            float absorbed = Mathf.Min(
                requestedAmount,
                remainingInk);
            remainingInk = Mathf.Max(
                0f,
                remainingInk - absorbed);

            if (remainingInk <= 0.001f)
            {
                creature.TryDamageGlyph(
                    InkGlyphType.Mouth,
                    999f,
                    out _,
                    out _);

                Debug.Log(
                    "[M55.3 Boya Hırsızı] Sünger Ağız sembolündeki " +
                    "Mürekkebi emdi; taşıma yeteneği bozuldu.",
                    creature);
            }

            return absorbed;
        }

        private void ResetSourceIfNeeded(bool force = false)
        {
            if (initialized && !force)
            {
                return;
            }

            float amount = config != null
                ? config.MouthInkAmount
                : 0.65f;
            remainingInk = Mathf.Max(0.05f, amount);
            initialized = true;
        }
    }
}
