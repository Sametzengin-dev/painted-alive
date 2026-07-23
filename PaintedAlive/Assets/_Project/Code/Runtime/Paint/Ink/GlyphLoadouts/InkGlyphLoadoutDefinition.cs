using UnityEngine;

namespace PaintedAlive.Paint.Ink.GlyphLoadouts
{
    public enum InkGlyphLoadoutId
    {
        Lekebacak = 0,
        Kabuklu = 1,
        KesikAvci = 2
    }

    [CreateAssetMenu(
        fileName = "InkGlyphLoadout",
        menuName = "Painted Alive/Paint/Ink/Glyph Loadout")]
    public sealed class InkGlyphLoadoutDefinition : ScriptableObject
    {
        [SerializeField]
        private InkGlyphLoadoutId loadoutId;

        [SerializeField]
        private string displayName = "Lekebacak";

        [SerializeField, TextArea(1, 3)]
        private string shortDescription = "Göz + Ayak";

        [SerializeField]
        private InkCreatureDefinition creatureDefinition;

        [SerializeField, Min(0f)]
        private float pigmentCost = 35f;

        [SerializeField, Range(1, 16)]
        private int complexityCost = 2;

        [SerializeField]
        private Color accentColor = new Color(0.55f, 0.12f, 0.78f, 1f);

        public InkGlyphLoadoutId LoadoutId => loadoutId;
        public string DisplayName => displayName;
        public string ShortDescription => shortDescription;
        public InkCreatureDefinition CreatureDefinition =>
            creatureDefinition;
        public float PigmentCost => pigmentCost;
        public int ComplexityCost => complexityCost;
        public Color AccentColor => accentColor;

        private void OnValidate()
        {
            pigmentCost = Mathf.Max(0f, pigmentCost);
            complexityCost = Mathf.Clamp(complexityCost, 1, 16);
        }
    }
}
