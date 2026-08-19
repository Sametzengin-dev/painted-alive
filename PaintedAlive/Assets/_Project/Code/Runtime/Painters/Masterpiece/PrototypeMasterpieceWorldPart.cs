using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceWorldPart :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeMasterpiecePartKind kind;

        [SerializeField]
        private PrototypeMasterpieceCapability capability;

        [SerializeField]
        private PrototypeMasterpieceProxyKind proxyKind;

        [SerializeField] private Collider proxyCollider;
        [SerializeField] private bool capabilityActive = true;
        [SerializeField] private int capabilityChangeCount;
        [SerializeField] private string lastCapabilityChangeReason =
            "Initial deployment";

        public PrototypeMasterpiecePartKind Kind => kind;
        public PrototypeMasterpieceCapability Capability => capability;
        public PrototypeMasterpieceProxyKind ProxyKind => proxyKind;
        public Collider ProxyCollider => proxyCollider;
        public bool CapabilityActive => capabilityActive;
        public int CapabilityChangeCount =>
            capabilityChangeCount;
        public string LastCapabilityChangeReason =>
            lastCapabilityChangeReason;
        public bool ColliderIsTrigger =>
            proxyCollider != null &&
            proxyCollider.isTrigger;

        public void Configure(
            PrototypeMasterpiecePartSnapshot snapshot,
            Collider configuredCollider)
        {
            if (snapshot == null)
            {
                return;
            }

            kind = snapshot.Kind;
            capability = snapshot.Capability;
            proxyKind = snapshot.ProxyKind;
            proxyCollider = configuredCollider;
            capabilityActive = true;
            capabilityChangeCount = 0;
            lastCapabilityChangeReason =
                "Initial deployment";
        }

        public bool SetCapabilityActive(
            bool active,
            string reason)
        {
            if (capabilityActive == active)
            {
                return false;
            }

            capabilityActive = active;
            capabilityChangeCount++;
            lastCapabilityChangeReason =
                string.IsNullOrWhiteSpace(reason)
                    ? "Unspecified"
                    : reason;

            return true;
        }
    }
}
