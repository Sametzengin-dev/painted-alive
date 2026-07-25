using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.StainSupport
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StainSpongeCarrier))]
    public sealed class PrototypeSpongeCarrierMover :
        MonoBehaviour
    {
        private const float MoveAcceleration = 20f;
        private const float StopAcceleration = 28f;

        [SerializeField]
        private StainSpongeCarrier carrier;

        [SerializeField]
        private StainSpongeCarryConfig config;

        [SerializeField]
        private Camera movementCamera;

        [Header("Physics")]
        [SerializeField]
        private Rigidbody physicsBody;

        [SerializeField]
        private BoxCollider physicsCollider;

        private Vector3 desiredPlanarVelocity;

        public void Configure(
            StainSpongeCarrier targetCarrier,
            StainSpongeCarryConfig targetConfig,
            Camera targetCamera)
        {
            carrier = targetCarrier;
            config = targetConfig;
            movementCamera = targetCamera;
            EnsurePhysics();
        }

        private void Awake()
        {
            carrier ??= GetComponent<StainSpongeCarrier>();
            EnsurePhysics();
        }

        private void OnEnable()
        {
            EnsurePhysics();
        }

        private void OnDisable()
        {
            desiredPlanarVelocity = Vector3.zero;
        }

        private void Update()
        {
            desiredPlanarVelocity = Vector3.zero;

            if (carrier == null ||
                config == null ||
                !carrier.HasPassenger ||
                Keyboard.current == null ||
                IsEditingText())
            {
                return;
            }

            Vector2 input = Vector2.zero;
            input.y +=
                Keyboard.current.upArrowKey.isPressed
                    ? 1f
                    : 0f;
            input.y -=
                Keyboard.current.downArrowKey.isPressed
                    ? 1f
                    : 0f;
            input.x +=
                Keyboard.current.rightArrowKey.isPressed
                    ? 1f
                    : 0f;
            input.x -=
                Keyboard.current.leftArrowKey.isPressed
                    ? 1f
                    : 0f;
            input = Vector2.ClampMagnitude(input, 1f);

            if (input.sqrMagnitude < 0.001f)
            {
                return;
            }

            Camera referenceCamera =
                movementCamera != null
                    ? movementCamera
                    : InkPainterRoleAuthority.ActiveInstance !=
                        null
                        ? InkPainterRoleAuthority.ActiveInstance
                            .ActiveRoleCamera
                        : null;
            Vector3 forward = referenceCamera != null
                ? Vector3.ProjectOnPlane(
                    referenceCamera.transform.forward,
                    Vector3.up).normalized
                : Vector3.forward;
            Vector3 right = referenceCamera != null
                ? Vector3.ProjectOnPlane(
                    referenceCamera.transform.right,
                    Vector3.up).normalized
                : Vector3.right;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.right;
            }

            desiredPlanarVelocity =
                (forward * input.y + right * input.x) *
                config.PrototypeMoveSpeed;
        }

        private void FixedUpdate()
        {
            EnsurePhysics();

            if (physicsBody == null ||
                physicsBody.isKinematic)
            {
                return;
            }

            Vector3 currentVelocity =
                physicsBody.linearVelocity;
            Vector3 currentPlanarVelocity =
                Vector3.ProjectOnPlane(
                    currentVelocity,
                    Vector3.up);
            float acceleration =
                desiredPlanarVelocity.sqrMagnitude > 0.001f
                    ? MoveAcceleration
                    : StopAcceleration;
            Vector3 nextPlanarVelocity =
                Vector3.MoveTowards(
                    currentPlanarVelocity,
                    desiredPlanarVelocity,
                    acceleration * Time.fixedDeltaTime);

            physicsBody.linearVelocity =
                nextPlanarVelocity +
                Vector3.up * currentVelocity.y;
        }

        private void EnsurePhysics()
        {
            if (physicsCollider == null)
            {
                physicsCollider =
                    GetComponent<BoxCollider>();
            }

            if (physicsCollider == null)
            {
                physicsCollider =
                    gameObject.AddComponent<BoxCollider>();
                physicsCollider.center =
                    new Vector3(0f, 0.38f, 0f);
                physicsCollider.size =
                    new Vector3(1.1f, 0.55f, 0.72f);
            }

            physicsCollider.isTrigger = false;

            if (physicsBody == null)
            {
                physicsBody = GetComponent<Rigidbody>();
            }

            if (physicsBody == null)
            {
                physicsBody =
                    gameObject.AddComponent<Rigidbody>();
            }

            physicsBody.mass = 0.9f;
            physicsBody.useGravity = true;
            physicsBody.isKinematic = false;
            physicsBody.linearDamping = 0.35f;
            physicsBody.angularDamping = 5f;
            physicsBody.interpolation =
                RigidbodyInterpolation.Interpolate;
            physicsBody.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
            physicsBody.constraints =
                RigidbodyConstraints.FreezeRotation;
        }

        private static bool IsEditingText()
        {
            if (EventSystem.current == null ||
                EventSystem.current.currentSelectedGameObject ==
                    null)
            {
                return false;
            }

            GameObject selected =
                EventSystem.current.currentSelectedGameObject;
            return selected.GetComponent<
                    UnityEngine.UI.InputField>() != null ||
                selected.GetComponent("TMP_InputField") != null;
        }
    }
}
