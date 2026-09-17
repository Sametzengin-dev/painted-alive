using UnityEngine;

namespace PaintedAlive.Painters.World
{
    /// <summary>
    /// Shared player-facing Painter world-action contract.
    /// Implemented by map-specific interaction adapters; the input/controller
    /// remains map-agnostic.
    /// </summary>
    public interface IPainterWorldAction
    {
        string SystemReference { get; }
        string DisplayName { get; }
        float TelegraphSeconds { get; }
        bool ActivationInProgress { get; }
        string LastResult { get; }
        Transform ActionTransform { get; }
        bool RequestActivate();
    }

    /// <summary>
    /// M56 server-authority seam. The server validates which connection owns
    /// Painter before calling this method. Implementations bypass only the
    /// local-machine role check; authored mechanic safety remains intact.
    /// </summary>
    public interface INetworkReplicatedPainterWorldAction :
        IPainterWorldAction
    {
        bool RequestActivateFromNetworkAuthority();
    }
}
