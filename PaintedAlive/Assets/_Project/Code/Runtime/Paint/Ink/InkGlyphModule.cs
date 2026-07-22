using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    public sealed class InkGlyphModule
    {
        public InkGlyphModule(InkGlyphDefinition definition)
        {
            Definition = definition;
            MaximumDurability = definition != null
                ? Mathf.Max(0.1f, definition.GlyphDurability)
                : 0.1f;
            CurrentDurability = MaximumDurability;
        }

        public InkGlyphDefinition Definition { get; }
        public InkGlyphType Type => Definition.GlyphType;
        public bool IsEnabled { get; private set; } = true;
        public float MaximumDurability { get; }
        public float CurrentDurability { get; private set; }

        public float ApplyDamage(float damage)
        {
            if (!IsEnabled || damage <= 0f)
            {
                return 0f;
            }

            float applied = Mathf.Min(damage, CurrentDurability);
            CurrentDurability -= applied;

            if (CurrentDurability <= 0.001f)
            {
                CurrentDurability = 0f;
                IsEnabled = false;
            }

            return applied;
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (enabled && CurrentDurability <= 0.001f)
            {
                CurrentDurability = MaximumDurability;
            }
        }
    }
}
