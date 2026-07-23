using System.Collections.Generic;

namespace PaintedAlive.Paint.Ink.GlyphLoadouts
{
    public static class InkGlyphComplexityUtility
    {
        public static int GetDefinitionCost(
            InkCreatureDefinition definition,
            int fallback = 2)
        {
            if (definition == null || definition.Glyphs == null)
            {
                return fallback;
            }

            IReadOnlyList<InkGlyphDefinition> glyphs = definition.Glyphs;
            int total = 0;

            for (int i = 0; i < glyphs.Count; i++)
            {
                InkGlyphDefinition glyph = glyphs[i];

                if (glyph != null)
                {
                    total += glyph.ComplexityCost;
                }
            }

            return total > 0 ? total : fallback;
        }

        public static int GetCreatureCost(
            InkCreatureRuntime creature,
            int fallback = 2)
        {
            return creature != null
                ? GetDefinitionCost(creature.Definition, fallback)
                : 0;
        }
    }
}
