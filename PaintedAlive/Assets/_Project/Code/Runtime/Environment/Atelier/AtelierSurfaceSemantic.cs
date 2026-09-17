using UnityEngine;

namespace PaintedAlive.Environment.Atelier
{
    public enum AtelierSurfaceKind
    {
        Unknown = 0,
        PaintableTraversal = 1,
        SolidObstacle = 2,
        WatercolorVisual = 3,
        Decorative = 4,
        NoPaint = 5
    }

    /// <summary>
    /// Lightweight semantic metadata for the Atelier test map.
    /// It intentionally does not drive Figure tools or Painter raycasts; those
    /// continue to use their existing contracts. The component exists so the
    /// environment can be audited and later ported to network/map authoring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AtelierSurfaceSemantic : MonoBehaviour
    {
        [SerializeField] private AtelierSurfaceKind kind = AtelierSurfaceKind.Unknown;
        [SerializeField] private string sourceObjectName;
        [SerializeField] private bool traversalProxyReplacedCollider;
        [SerializeField] private bool sourceColliderWasEnabled;

        public AtelierSurfaceKind Kind => kind;
        public string SourceObjectName => sourceObjectName;
        public bool TraversalProxyReplacedCollider => traversalProxyReplacedCollider;
        public bool SourceColliderWasEnabled => sourceColliderWasEnabled;

        public void Configure(
            AtelierSurfaceKind configuredKind,
            string configuredSourceObjectName,
            bool proxyReplacedCollider = false,
            bool colliderWasEnabled = false)
        {
            kind = configuredKind;
            sourceObjectName = configuredSourceObjectName;
            traversalProxyReplacedCollider = proxyReplacedCollider;
            sourceColliderWasEnabled = colliderWasEnabled;
        }

        public void RestoreSourceColliderIfNeeded()
        {
            if (!traversalProxyReplacedCollider)
            {
                return;
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = sourceColliderWasEnabled;
            }

            traversalProxyReplacedCollider = false;
        }
    }
}
