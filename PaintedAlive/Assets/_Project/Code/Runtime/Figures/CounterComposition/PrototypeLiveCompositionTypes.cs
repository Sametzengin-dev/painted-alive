namespace PaintedAlive.Figures.CounterComposition
{
    public enum PrototypeLiveCompositionState
    {
        WaitingForFigure,
        PainterRole,
        FrameGunRequired,
        SeekingFirstAnchor,
        SeekingSecondAnchor,
        ReadyForCommit,
        Composing,
        SafetyRelease
    }

    public enum PrototypeLiveCompositionAnchorId
    {
        A,
        B
    }
}
