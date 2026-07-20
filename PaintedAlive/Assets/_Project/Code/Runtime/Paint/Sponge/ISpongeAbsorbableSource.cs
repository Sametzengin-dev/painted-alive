using UnityEngine;

namespace PaintedAlive.Paint.Sponge
{
    public interface ISpongeAbsorbableSource
    {
        bool CanAbsorb { get; }
        float AvailableAmount { get; }
        Color PaintColor { get; }
        float Instability { get; }
        float Absorb(float requestedAmount);
    }
}
