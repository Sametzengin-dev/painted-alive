using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [DisallowMultipleComponent]
    public sealed class InkGlyphHitZone : MonoBehaviour
    {
        [SerializeField]
        private InkGlyphType glyphType;

        [SerializeField]
        private InkCreatureRuntime creature;

        [SerializeField]
        private Collider hitCollider;

        [SerializeField]
        private Renderer[] glyphRenderers;

        public InkGlyphType GlyphType => glyphType;
        public InkCreatureRuntime Creature => creature;
        public bool IsActive =>
            creature != null && creature.HasGlyph(glyphType);

        private void Awake()
        {
            creature ??= GetComponentInParent<InkCreatureRuntime>();
            hitCollider ??= GetComponent<Collider>();

            if (glyphRenderers == null || glyphRenderers.Length == 0)
            {
                glyphRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        public void Configure(
            InkCreatureRuntime owner,
            InkGlyphType type,
            Collider targetCollider,
            Renderer[] renderers)
        {
            creature = owner;
            glyphType = type;
            hitCollider = targetCollider;
            glyphRenderers = renderers;
        }

        public bool TryApplyDamage(
            float damage,
            out bool glyphDisabled,
            out float remainingDurability)
        {
            glyphDisabled = false;
            remainingDurability = 0f;

            if (creature == null || damage <= 0f)
            {
                return false;
            }

            InkGlyphType resolvedType =
                glyphType != InkGlyphType.Shell &&
                creature.HasGlyph(InkGlyphType.Shell)
                    ? InkGlyphType.Shell
                    : glyphType;
            bool applied = creature.TryDamageGlyph(
                resolvedType,
                damage,
                out glyphDisabled,
                out remainingDurability);

            if (applied)
            {
                creature.RefreshGlyphHitZones();
            }

            return applied;
        }

        public void RefreshFromCreature()
        {
            bool active = creature != null && creature.HasGlyph(glyphType);

            if (hitCollider != null)
            {
                hitCollider.enabled = active;
            }

            if (glyphRenderers == null)
            {
                return;
            }

            for (int i = 0; i < glyphRenderers.Length; i++)
            {
                Renderer targetRenderer = glyphRenderers[i];

                if (targetRenderer != null)
                {
                    targetRenderer.enabled = active;
                }
            }
        }
    }
}
