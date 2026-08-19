namespace PaintedAlive.MatchFlow
{
    public enum PrototypeCoreMatchPhase
    {
        Boot,
        Preparation,
        Active,
        Complete
    }

    public enum PrototypeCoreMatchResult
    {
        None,
        FigureEscaped,
        TimeExpired
    }

    public static class PrototypeProductionScopeContract
    {
        public const bool FeatureFreezeActive = true;
        public const bool NewMajorGameplayMechanicsAllowed = false;
        public const bool ExistingSystemsIntegrationPriority = true;
        public const bool CoreMatchProductionPriority = true;
        public const bool AnimationProductionPassQueued = true;
        public const bool MultiplayerFoundationQueued = true;
        public const bool NewLiveCompositionTemplatesFrozen = true;
        public const bool ControllerSupportInCurrentScope = false;
    }
}
