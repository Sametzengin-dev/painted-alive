using System;
using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    public enum PrototypeMasterpiecePartKind
    {
        Core,
        Head,
        LeftAttack,
        RightAttack,
        LeftContact,
        RightContact
    }

    public enum PrototypeMasterpieceCapability
    {
        CoreIntegrity,
        Perception,
        LeftAttack,
        RightAttack,
        LeftMovement,
        RightMovement
    }

    public enum PrototypeMasterpieceProxyKind
    {
        Circle,
        Capsule
    }

    [Serializable]
    public sealed class PrototypeMasterpiecePartState
    {
        [SerializeField]
        private PrototypeMasterpiecePartKind kind;

        [SerializeField]
        private PrototypeMasterpieceCapability capability;

        [SerializeField]
        private PrototypeMasterpieceProxyKind proxyKind;

        [SerializeField]
        private Vector2 normalizedStart;

        [SerializeField]
        private Vector2 normalizedEnd;

        [SerializeField, Min(0.001f)]
        private float normalizedRadius;

        [SerializeField]
        private bool detached;

        [SerializeField]
        private float detachedAt = -100f;

        [SerializeField]
        private int stateRevision;

        public PrototypeMasterpiecePartKind Kind => kind;
        public PrototypeMasterpieceCapability Capability => capability;
        public PrototypeMasterpieceProxyKind ProxyKind => proxyKind;
        public Vector2 NormalizedStart => normalizedStart;
        public Vector2 NormalizedEnd => normalizedEnd;
        public float NormalizedRadius => normalizedRadius;
        public bool Detached => detached;
        public float DetachedAt => detachedAt;
        public int StateRevision => stateRevision;

        public PrototypeMasterpiecePartState(
            PrototypeMasterpiecePartKind configuredKind,
            PrototypeMasterpieceCapability configuredCapability,
            PrototypeMasterpieceProxyKind configuredProxyKind,
            Vector2 configuredStart,
            Vector2 configuredEnd,
            float configuredRadius)
        {
            kind = configuredKind;
            capability = configuredCapability;
            proxyKind = configuredProxyKind;
            normalizedStart = configuredStart;
            normalizedEnd = configuredEnd;
            normalizedRadius =
                Mathf.Max(
                    0.001f,
                    configuredRadius);
        }

        public void SetDetached(
            bool value,
            float changedAt)
        {
            if (detached == value)
            {
                return;
            }

            detached = value;
            detachedAt =
                value
                    ? changedAt
                    : -100f;

            stateRevision++;
        }
    }
}
