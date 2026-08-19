namespace PaintedAlive.Painters.Masterpiece
{
    public enum PrototypeMasterpieceHitValidationResult
    {
        None,
        Pending,
        HitConfirmed,
        MissDodged,
        MissOutOfReach,
        MissOccluded,
        TargetUnavailable,
        AbortedPossession,
        AbortedCapability,
        AbortedRecall,
        AbortedStateChange
    }
}
