using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [CreateAssetMenu(
        fileName = "InkCreatureDefinition",
        menuName = "Painted Alive/Paint/Ink/Creature Definition")]
    public sealed class InkCreatureDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Lekebacak";

        [SerializeField]
        private InkGlyphDefinition[] glyphs;

        [SerializeField, Min(1f)]
        private float baseDurability = 18f;

        [SerializeField, Min(0.25f)]
        private float baseScale = 1f;

        public string DisplayName => displayName;
        public IReadOnlyList<InkGlyphDefinition> Glyphs => glyphs;
        public float BaseDurability => baseDurability;
        public float BaseScale => baseScale;

        public bool ContainsGlyph(InkGlyphType glyphType)
        {
            if (glyphs == null)
            {
                return false;
            }

            for (int i = 0; i < glyphs.Length; i++)
            {
                InkGlyphDefinition glyph = glyphs[i];

                if (glyph != null && glyph.GlyphType == glyphType)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            baseDurability = Mathf.Max(1f, baseDurability);
            baseScale = Mathf.Max(0.25f, baseScale);
        }
    }
}
