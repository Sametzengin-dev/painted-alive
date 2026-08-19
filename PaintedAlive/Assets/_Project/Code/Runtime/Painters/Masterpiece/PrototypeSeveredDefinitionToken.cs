using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSeveredDefinitionToken :
        MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField]
        private PrototypeMasterpiecePartKind partKind;

        [SerializeField]
        private PrototypeMasterpieceCapability capability;

        [SerializeField] private int snapshotHash;

        [Header("Motion")]
        [SerializeField] private Vector3 velocity;
        [SerializeField] private Vector3 angularVelocity;
        [SerializeField, Min(0f)]
        private float gravity = 12.5f;

        [SerializeField, Min(0f)]
        private float groundLift = 0.055f;

        [SerializeField]
        private LayerMask groundMask = ~0;

        [Header("Carry Lifecycle")]
        [SerializeField] private bool carried;
        [SerializeField] private bool consumed;
        [SerializeField] private Transform carrySocket;
        [SerializeField] private int pickupCount;
        [SerializeField] private int dropCount;
        [SerializeField] private int consumeCount;

        [Header("Runtime Read Only")]
        [SerializeField] private bool settled;
        [SerializeField] private int groundQueryCount;
        [SerializeField] private int ignoredHitCount;
        [SerializeField] private float travelledDistance;
        [SerializeField] private string lastGroundResult =
            "Falling";

        private Transform ignoredMasterpieceRoot;
        private Transform ignoredFigureRoot;

        private readonly RaycastHit[] groundHits =
            new RaycastHit[24];

        public PrototypeMasterpiecePartKind PartKind =>
            partKind;
        public PrototypeMasterpieceCapability Capability =>
            capability;
        public int SnapshotHash => snapshotHash;
        public bool Settled => settled;
        public bool IsCarried => carried;
        public bool Consumed => consumed;
        public Transform CarrySocket => carrySocket;
        public int PickupCount => pickupCount;
        public int DropCount => dropCount;
        public int ConsumeCount => consumeCount;
        public int GroundQueryCount => groundQueryCount;
        public int IgnoredHitCount => ignoredHitCount;
        public float TravelledDistance => travelledDistance;
        public string LastGroundResult => lastGroundResult;

        public bool GameplayPickupEnabled => true;
        public bool GameplayColliderAdded => false;
        public bool DefinitionUseEnabled => true;
        public bool SingleCarrySlotCompatible => true;
        public bool HeadPerceptionDefinitionCompatible =>
            partKind ==
                PrototypeMasterpiecePartKind.Head &&
            capability ==
                PrototypeMasterpieceCapability.Perception;
        public bool NetworkAuthorityEnabled => false;

        public void Configure(
            PrototypeMasterpiecePartKind configuredPartKind,
            PrototypeMasterpieceCapability configuredCapability,
            int configuredSnapshotHash,
            Vector3 initialVelocity,
            Vector3 initialAngularVelocity,
            Transform configuredIgnoredMasterpieceRoot,
            Transform configuredIgnoredFigureRoot)
        {
            partKind = configuredPartKind;
            capability = configuredCapability;
            snapshotHash = configuredSnapshotHash;
            velocity = initialVelocity;
            angularVelocity = initialAngularVelocity;
            ignoredMasterpieceRoot =
                configuredIgnoredMasterpieceRoot;

            ignoredFigureRoot =
                configuredIgnoredFigureRoot;

            settled = false;
            carried = false;
            consumed = false;
            carrySocket = null;
            travelledDistance = 0f;
            lastGroundResult = "Falling";
        }

        public bool BeginCarry(
            Transform socket,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            if (
                socket == null ||
                consumed ||
                carried
            )
            {
                return false;
            }

            carrySocket = socket;
            carried = true;
            settled = true;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;

            transform.SetParent(
                socket,
                false);

            transform.localPosition =
                localPosition;

            transform.localRotation =
                localRotation;

            pickupCount++;
            lastGroundResult = "Carried";
            return true;
        }

        public void SetCarryPose(
            Vector3 localPosition,
            Quaternion localRotation)
        {
            if (
                !carried ||
                consumed ||
                carrySocket == null
            )
            {
                return;
            }

            transform.localPosition =
                localPosition;

            transform.localRotation =
                localRotation;
        }

        public bool Drop(
            Vector3 worldPosition,
            Vector3 initialVelocity,
            Vector3 initialAngularVelocity)
        {
            if (
                consumed ||
                !carried
            )
            {
                return false;
            }

            transform.SetParent(
                null,
                true);

            transform.position =
                worldPosition;

            velocity =
                initialVelocity;

            angularVelocity =
                initialAngularVelocity;

            carrySocket = null;
            carried = false;
            settled = false;
            dropCount++;
            lastGroundResult = "Dropped";
            return true;
        }

        public bool MarkConsumed()
        {
            if (consumed)
            {
                return false;
            }

            consumed = true;
            carried = false;
            settled = true;
            carrySocket = null;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            consumeCount++;
            lastGroundResult = "Consumed";
            return true;
        }

        private void Update()
        {
            if (
                settled ||
                carried ||
                consumed
            )
            {
                return;
            }

            float deltaTime =
                Mathf.Max(
                    0f,
                    Time.deltaTime);

            velocity +=
                Vector3.down *
                gravity *
                deltaTime;

            Vector3 before =
                transform.position;

            Vector3 proposed =
                before +
                velocity *
                deltaTime;

            float downwardTravel =
                Mathf.Max(
                    0f,
                    before.y -
                    proposed.y);

            if (
                velocity.y <= 0f &&
                TryFindGround(
                    before,
                    downwardTravel +
                    0.36f,
                    out RaycastHit ground)
            )
            {
                float desiredY =
                    ground.point.y +
                    groundLift;

                if (proposed.y <= desiredY)
                {
                    proposed.y = desiredY;
                    velocity = Vector3.zero;
                    settled = true;
                    lastGroundResult =
                        $"Settled on {ground.collider.name}";
                }
            }

            transform.position = proposed;

            transform.Rotate(
                angularVelocity *
                deltaTime,
                Space.World);

            travelledDistance +=
                Vector3.Distance(
                    before,
                    proposed);
        }

        private bool TryFindGround(
            Vector3 origin,
            float distance,
            out RaycastHit selected)
        {
            selected = default;
            groundQueryCount++;

            Vector3 rayOrigin =
                origin +
                Vector3.up *
                0.18f;

            int hitCount =
                Physics.RaycastNonAlloc(
                    rayOrigin,
                    Vector3.down,
                    groundHits,
                    Mathf.Max(
                        0.2f,
                        distance),
                    groundMask,
                    QueryTriggerInteraction.Ignore);

            float nearest =
                float.PositiveInfinity;

            bool found = false;

            for (int index = 0;
                 index < hitCount;
                 index++)
            {
                RaycastHit candidate =
                    groundHits[index];

                Collider collider =
                    candidate.collider;

                if (collider == null)
                {
                    continue;
                }

                Transform hitTransform =
                    collider.transform;

                if (
                    IsInside(
                        hitTransform,
                        ignoredMasterpieceRoot) ||
                    IsInside(
                        hitTransform,
                        ignoredFigureRoot)
                )
                {
                    ignoredHitCount++;
                    continue;
                }

                if (candidate.distance < nearest)
                {
                    nearest =
                        candidate.distance;

                    selected = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                lastGroundResult =
                    "No valid ground";
            }

            return found;
        }

        private static bool IsInside(
            Transform candidate,
            Transform root)
        {
            return
                candidate != null &&
                root != null &&
                (
                    candidate == root ||
                    candidate.IsChildOf(root)
                );
        }
    }
}
