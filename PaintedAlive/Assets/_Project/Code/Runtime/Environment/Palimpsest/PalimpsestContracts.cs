namespace PaintedAlive.Environment.Palimpsest
{
    public interface IPalimpsestResettable
    {
        void ResetPalimpsestState();
    }

    public interface IPalimpsestPainterActivatable
    {
        string PalimpsestSystemId { get; }
        bool CanPainterActivate(out string reason);
        bool TryPainterActivate();
    }

    public interface IPalimpsestPersistentState
    {
        string PalimpsestSystemId { get; }
        string PersistentStateName { get; }
        float PersistentProgress01 { get; }
        void ApplyPersistentState(string stateName, float progress01);
    }
}
