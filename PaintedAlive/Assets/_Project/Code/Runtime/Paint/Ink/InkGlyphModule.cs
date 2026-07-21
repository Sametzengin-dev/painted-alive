namespace PaintedAlive.Paint.Ink
{
    public sealed class InkGlyphModule
    {
        public InkGlyphModule(InkGlyphDefinition definition)
        {
            Definition = definition;
        }

        public InkGlyphDefinition Definition { get; }
        public InkGlyphType Type => Definition.GlyphType;
        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }
    }
}
