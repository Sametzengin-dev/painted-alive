using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    [DisallowMultipleComponent]
    public sealed class PrototypeFigureHitQueryVolume :
        MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private Transform sourceRoot;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private CapsuleCollider capsuleCollider;

        [Header("Fallback")]
        [SerializeField, Min(0.1f)]
        private float fallbackRadius = 0.42f;

        [SerializeField, Min(0.2f)]
        private float fallbackHeight = 1.72f;

        [SerializeField]
        private Vector3 fallbackLocalCenter =
            new Vector3(0f, 0.86f, 0f);

        [Header("Runtime Read Only")]
        [SerializeField] private bool authoritativeSourceFound;
        [SerializeField] private int queryCount;
        [SerializeField] private int colliderScanCount;
        [SerializeField] private string sourceKind = "Fallback";
        [SerializeField] private float lastRadius;
        [SerializeField] private float lastHeight;
        [SerializeField] private Vector3 lastCenter;

        public Transform SourceRoot =>
            sourceRoot != null
                ? sourceRoot
                : transform;

        public bool AuthoritativeSourceFound =>
            authoritativeSourceFound;
        public int QueryCount => queryCount;
        public int ColliderScanCount => colliderScanCount;
        public string SourceKind => sourceKind;
        public float LastRadius => lastRadius;
        public float LastHeight => lastHeight;
        public Vector3 LastCenter => lastCenter;

        public bool AddsGameplayCollider => false;
        public bool ModifiesFigureMotor => false;
        public bool ModifiesClarity => false;

        public void Configure(
            Transform configuredSourceRoot)
        {
            sourceRoot =
                configuredSourceRoot != null
                    ? configuredSourceRoot
                    : transform;

            ResolveAuthoritativeSource();
        }

        private void Awake()
        {
            if (sourceRoot == null)
            {
                sourceRoot = transform;
            }

            ResolveAuthoritativeSource();
        }

        public bool TryGetWorldCapsule(
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius,
            out Vector3 center,
            out float height)
        {
            queryCount++;

            if (
                characterController != null &&
                characterController.enabled &&
                characterController.gameObject.activeInHierarchy
            )
            {
                BuildCharacterControllerCapsule(
                    characterController,
                    out pointA,
                    out pointB,
                    out radius,
                    out center,
                    out height);

                authoritativeSourceFound = true;
                sourceKind = "CharacterController";
                SaveLast(
                    radius,
                    center,
                    height);

                return true;
            }

            if (
                capsuleCollider != null &&
                capsuleCollider.enabled &&
                capsuleCollider.gameObject.activeInHierarchy
            )
            {
                BuildCapsuleColliderCapsule(
                    capsuleCollider,
                    out pointA,
                    out pointB,
                    out radius,
                    out center,
                    out height);

                authoritativeSourceFound = true;
                sourceKind = "CapsuleCollider";
                SaveLast(
                    radius,
                    center,
                    height);

                return true;
            }

            if (TryBuildBoundsCapsule(
                    out pointA,
                    out pointB,
                    out radius,
                    out center,
                    out height))
            {
                authoritativeSourceFound = true;
                sourceKind = "ColliderBounds";
                SaveLast(
                    radius,
                    center,
                    height);

                return true;
            }

            Transform root =
                SourceRoot;

            Vector3 scale =
                Abs(
                    root.lossyScale);

            center =
                root.TransformPoint(
                    fallbackLocalCenter);

            radius =
                Mathf.Max(
                    0.1f,
                    fallbackRadius *
                    Mathf.Max(
                        scale.x,
                        scale.z));

            height =
                Mathf.Max(
                    radius * 2f,
                    fallbackHeight *
                    scale.y);

            Vector3 axis =
                root.up.sqrMagnitude > 0.001f
                    ? root.up.normalized
                    : Vector3.up;

            float halfSegment =
                Mathf.Max(
                    0f,
                    height * 0.5f -
                    radius);

            pointA =
                center -
                axis *
                halfSegment;

            pointB =
                center +
                axis *
                halfSegment;

            authoritativeSourceFound = false;
            sourceKind = "Fallback";
            SaveLast(
                radius,
                center,
                height);

            return true;
        }

        private void ResolveAuthoritativeSource()
        {
            Transform root =
                SourceRoot;

            characterController =
                root.GetComponentInChildren<
                    CharacterController>(
                        true);

            CapsuleCollider[] capsules =
                root.GetComponentsInChildren<
                    CapsuleCollider>(
                        true);

            capsuleCollider = null;

            for (int index = 0;
                 index < capsules.Length;
                 index++)
            {
                CapsuleCollider candidate =
                    capsules[index];

                if (
                    candidate != null &&
                    candidate.enabled &&
                    !candidate.isTrigger
                )
                {
                    capsuleCollider = candidate;
                    break;
                }
            }

            authoritativeSourceFound =
                characterController != null ||
                capsuleCollider != null;

            if (characterController != null)
            {
                sourceKind = "CharacterController";
            }
            else if (capsuleCollider != null)
            {
                sourceKind = "CapsuleCollider";
            }
            else
            {
                sourceKind = "ColliderBounds/Fallback";
            }
        }

        private bool TryBuildBoundsCapsule(
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius,
            out Vector3 center,
            out float height)
        {
            pointA = default;
            pointB = default;
            radius = 0f;
            center = default;
            height = 0f;

            Collider[] colliders =
                SourceRoot.GetComponentsInChildren<
                    Collider>(
                        true);

            colliderScanCount++;

            bool hasBounds = false;
            Bounds combined = default;

            for (int index = 0;
                 index < colliders.Length;
                 index++)
            {
                Collider candidate =
                    colliders[index];

                if (
                    candidate == null ||
                    !candidate.enabled ||
                    !candidate.gameObject.activeInHierarchy
                )
                {
                    continue;
                }

                Bounds bounds =
                    candidate.bounds;

                if (
                    bounds.size.sqrMagnitude <=
                    0.000001f
                )
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(
                        bounds);
                }
            }

            if (!hasBounds)
            {
                return false;
            }

            center =
                combined.center;

            radius =
                Mathf.Clamp(
                    Mathf.Max(
                        combined.extents.x,
                        combined.extents.z),
                    0.18f,
                    1.2f);

            height =
                Mathf.Max(
                    combined.size.y,
                    radius * 2f);

            float halfSegment =
                Mathf.Max(
                    0f,
                    height * 0.5f -
                    radius);

            pointA =
                center -
                Vector3.up *
                halfSegment;

            pointB =
                center +
                Vector3.up *
                halfSegment;

            return true;
        }

        private static void BuildCharacterControllerCapsule(
            CharacterController source,
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius,
            out Vector3 center,
            out float height)
        {
            Transform target =
                source.transform;

            Vector3 scale =
                Abs(
                    target.lossyScale);

            center =
                target.TransformPoint(
                    source.center);

            radius =
                Mathf.Max(
                    0.05f,
                    source.radius *
                    Mathf.Max(
                        scale.x,
                        scale.z));

            height =
                Mathf.Max(
                    radius * 2f,
                    source.height *
                    scale.y);

            Vector3 axis =
                target.up.sqrMagnitude > 0.001f
                    ? target.up.normalized
                    : Vector3.up;

            float halfSegment =
                Mathf.Max(
                    0f,
                    height * 0.5f -
                    radius);

            pointA =
                center -
                axis *
                halfSegment;

            pointB =
                center +
                axis *
                halfSegment;
        }

        private static void BuildCapsuleColliderCapsule(
            CapsuleCollider source,
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius,
            out Vector3 center,
            out float height)
        {
            Transform target =
                source.transform;

            Vector3 scale =
                Abs(
                    target.lossyScale);

            Vector3 localAxis;
            float axisScale;
            float perpendicularScale;

            switch (source.direction)
            {
                case 0:
                    localAxis = Vector3.right;
                    axisScale = scale.x;
                    perpendicularScale =
                        Mathf.Max(
                            scale.y,
                            scale.z);
                    break;

                case 2:
                    localAxis = Vector3.forward;
                    axisScale = scale.z;
                    perpendicularScale =
                        Mathf.Max(
                            scale.x,
                            scale.y);
                    break;

                default:
                    localAxis = Vector3.up;
                    axisScale = scale.y;
                    perpendicularScale =
                        Mathf.Max(
                            scale.x,
                            scale.z);
                    break;
            }

            center =
                target.TransformPoint(
                    source.center);

            radius =
                Mathf.Max(
                    0.05f,
                    source.radius *
                    perpendicularScale);

            height =
                Mathf.Max(
                    radius * 2f,
                    source.height *
                    axisScale);

            Vector3 axis =
                target.TransformDirection(
                    localAxis);

            if (axis.sqrMagnitude <= 0.001f)
            {
                axis = Vector3.up;
            }
            else
            {
                axis.Normalize();
            }

            float halfSegment =
                Mathf.Max(
                    0f,
                    height * 0.5f -
                    radius);

            pointA =
                center -
                axis *
                halfSegment;

            pointB =
                center +
                axis *
                halfSegment;
        }

        private void SaveLast(
            float radius,
            Vector3 center,
            float height)
        {
            lastRadius = radius;
            lastCenter = center;
            lastHeight = height;
        }

        private static Vector3 Abs(
            Vector3 value)
        {
            return new Vector3(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
        }
    }
}
