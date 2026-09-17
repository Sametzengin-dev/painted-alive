using PaintedAlive.Figures;
using UnityEngine;
using UnityEngine.Events;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum RotatingFrameGateState
    {
        Open,
        Closed,
        Transitioning
    }

    [DisallowMultipleComponent]
    public sealed class RotatingFrameGate : MonoBehaviour, ILivingGalleryResettable
    {
        private const int OverlapBufferSize = 32;

        [SerializeField] private string bindingId;
        [SerializeField] private Transform visualGateRoot;
        [SerializeField] private Transform collisionGateRoot;
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform clearanceReference;
        [SerializeField] private Collider movingCollider;

        [Header("Authored Contract")]
        [SerializeField] private float openAngleDegrees = 0f;
        [SerializeField] private float closedAngleDegrees = 90f;
        [SerializeField, Min(0f)] private float telegraphSeconds = 1.5f;
        [SerializeField, Min(0f)] private float minimumEscapeSeconds = 1.25f;
        [SerializeField, Min(0.1f)] private float clearanceRadius = 2.54f;
        [SerializeField, Min(0.1f)] private float clearanceHeight = 6f;
        [SerializeField, Min(1f)] private float rotationSpeedDegreesPerSecond = 70f;

        [Header("Unity Runtime Policy")]
        [SerializeField] private RotatingFrameGateState initialState =
            RotatingFrameGateState.Open;

        [Header("Runtime")]
        [SerializeField] private RotatingFrameGateState state =
            RotatingFrameGateState.Open;
        [SerializeField] private float currentAngle;
        [SerializeField] private float targetAngle;
        [SerializeField] private bool transitioning;
        [SerializeField] private bool blockedByFigure;
        [SerializeField] private FigureMotor lastBlockingFigure;
        [SerializeField] private UnityEvent onTelegraph = new UnityEvent();
        [SerializeField] private UnityEvent onTransitionCompleted = new UnityEvent();

        private Quaternion importedVisualRotation;
        private Quaternion importedCollisionRotation;
        private Vector3 importedVisualPosition;
        private Vector3 importedCollisionPosition;
        private float importedAngle;
        private float telegraphRemaining;
        private bool initialized;
        private RotatingFrameGateState pendingFinalState;
        private readonly Collider[] overlapBuffer = new Collider[OverlapBufferSize];

        public string BindingId => bindingId;
        public bool IsTransitioning => transitioning;
        public bool BlockedByFigure => blockedByFigure;
        public FigureMotor LastBlockingFigure => lastBlockingFigure;
        public float CurrentAngle => currentAngle;
        public float TelegraphSeconds => telegraphSeconds;
        public RotatingFrameGateState State => state;
        public string StateLabel => state.ToString().ToUpperInvariant();
        public bool IsConfigured =>
            visualGateRoot != null &&
            collisionGateRoot != null &&
            pivot != null &&
            clearanceReference != null &&
            movingCollider != null;
        public bool UsesPivotPreservingRotation => true;

        public void Configure(
            string id,
            Transform visualRoot,
            Transform collisionRoot,
            Transform pivotReference,
            Transform clearance,
            Collider moving,
            float openAngle,
            float closedAngle,
            float telegraph,
            float minEscape,
            float radius,
            float height)
        {
            bindingId = id ?? string.Empty;
            visualGateRoot = visualRoot;
            collisionGateRoot = collisionRoot;
            pivot = pivotReference;
            clearanceReference = clearance;
            movingCollider = moving;
            openAngleDegrees = openAngle;
            closedAngleDegrees = closedAngle;
            telegraphSeconds = Mathf.Max(0f, telegraph);
            minimumEscapeSeconds = Mathf.Max(0f, minEscape);
            clearanceRadius = Mathf.Max(0.1f, radius);
            clearanceHeight = Mathf.Max(0.1f, height);
            initialized = false;
            CaptureImportedBasis();
        }

        public void ConfigurePainterInitialState(RotatingFrameGateState configuredState)
        {
            initialState = configuredState == RotatingFrameGateState.Closed
                ? RotatingFrameGateState.Closed
                : RotatingFrameGateState.Open;
        }

        private void Awake()
        {
            CaptureImportedBasis();
            ApplyInitialLogicalState();
        }

        private void CaptureImportedBasis()
        {
            if (initialized)
                return;

            if (visualGateRoot != null)
            {
                importedVisualRotation = visualGateRoot.rotation;
                importedVisualPosition = visualGateRoot.position;
            }

            if (collisionGateRoot != null)
            {
                importedCollisionRotation = collisionGateRoot.rotation;
                importedCollisionPosition = collisionGateRoot.position;
            }

            importedAngle = CalculateCurrentAngle();
            currentAngle = importedAngle;
            targetAngle = importedAngle;
            initialized = true;
        }

        private void ApplyInitialLogicalState()
        {
            float angle = initialState == RotatingFrameGateState.Closed
                ? closedAngleDegrees
                : openAngleDegrees;

            transitioning = false;
            blockedByFigure = false;
            lastBlockingFigure = null;
            telegraphRemaining = 0f;
            currentAngle = angle;
            targetAngle = angle;
            state = initialState == RotatingFrameGateState.Closed
                ? RotatingFrameGateState.Closed
                : RotatingFrameGateState.Open;
            pendingFinalState = state;
            ApplyAngle(angle);
        }

        private float CalculateCurrentAngle()
        {
            if (pivot == null || visualGateRoot == null)
                return 0f;

            Vector3 pivotForward = Vector3.ProjectOnPlane(
                pivot.forward,
                pivot.up);
            Vector3 gateForward = Vector3.ProjectOnPlane(
                visualGateRoot.forward,
                pivot.up);

            if (pivotForward.sqrMagnitude < 0.0001f ||
                gateForward.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            return Vector3.SignedAngle(
                pivotForward,
                gateForward,
                pivot.up);
        }

        public bool RequestOpen() =>
            BeginTransition(
                openAngleDegrees,
                RotatingFrameGateState.Open,
                Mathf.Max(telegraphSeconds, minimumEscapeSeconds));

        public bool RequestClosed() =>
            BeginTransition(
                closedAngleDegrees,
                RotatingFrameGateState.Closed,
                Mathf.Max(telegraphSeconds, minimumEscapeSeconds));

        public bool RequestAngle(float requestedAngle)
        {
            requestedAngle = Mathf.Clamp(
                requestedAngle,
                Mathf.Min(openAngleDegrees, closedAngleDegrees),
                Mathf.Max(openAngleDegrees, closedAngleDegrees));

            RotatingFrameGateState final =
                Mathf.Abs(requestedAngle - closedAngleDegrees) <
                Mathf.Abs(requestedAngle - openAngleDegrees)
                    ? RotatingFrameGateState.Closed
                    : RotatingFrameGateState.Open;

            return BeginTransition(
                requestedAngle,
                final,
                Mathf.Max(telegraphSeconds, minimumEscapeSeconds));
        }

        public bool CanPainterToggle(out string reason)
        {
            reason = string.Empty;

            if (!IsConfigured)
            {
                reason = "Frame Gate binding incomplete";
                return false;
            }

            if (transitioning)
            {
                reason = "Frame Gate is already transitioning";
                return false;
            }

            if (HasFigureInClearance())
            {
                reason = lastBlockingFigure != null
                    ? $"Authored rotation sweep occupied by Figure '{lastBlockingFigure.name}'"
                    : "Authored rotation sweep occupied";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called only after the shared Painter interaction has already shown
        /// the authored 1.5 s telegraph. The safety gate is checked again here.
        /// </summary>
        public bool TryPainterToggleAfterExternalTelegraph()
        {
            if (!CanPainterToggle(out _))
                return false;

            bool currentlyClosed =
                state == RotatingFrameGateState.Closed ||
                Mathf.Abs(currentAngle - closedAngleDegrees) <
                Mathf.Abs(currentAngle - openAngleDegrees);

            return currentlyClosed
                ? BeginTransition(
                    openAngleDegrees,
                    RotatingFrameGateState.Open,
                    0f)
                : BeginTransition(
                    closedAngleDegrees,
                    RotatingFrameGateState.Closed,
                    0f);
        }

        private bool BeginTransition(
            float requestedAngle,
            RotatingFrameGateState finalState,
            float warningSeconds)
        {
            if (pivot == null || transitioning)
                return false;

            if (HasFigureInClearance())
            {
                blockedByFigure = true;
                return false;
            }

            targetAngle = requestedAngle;
            pendingFinalState = finalState;
            telegraphRemaining = Mathf.Max(0f, warningSeconds);
            transitioning = true;
            state = RotatingFrameGateState.Transitioning;
            blockedByFigure = false;
            onTelegraph?.Invoke();
            return true;
        }

        private void Update()
        {
            if (!transitioning)
                return;

            if (telegraphRemaining > 0f)
            {
                telegraphRemaining -= Time.deltaTime;
                return;
            }

            if (HasFigureInClearance())
            {
                blockedByFigure = true;
                transitioning = false;
                state = Mathf.Abs(currentAngle - closedAngleDegrees) <
                        Mathf.Abs(currentAngle - openAngleDegrees)
                    ? RotatingFrameGateState.Closed
                    : RotatingFrameGateState.Open;
                return;
            }

            blockedByFigure = false;
            currentAngle = Mathf.MoveTowards(
                currentAngle,
                targetAngle,
                rotationSpeedDegreesPerSecond * Time.deltaTime);
            ApplyAngle(currentAngle);

            if (Mathf.Abs(currentAngle - targetAngle) <= 0.01f)
            {
                currentAngle = targetAngle;
                ApplyAngle(currentAngle);
                transitioning = false;
                state = pendingFinalState;
                onTransitionCompleted?.Invoke();
            }
        }

        private void ApplyAngle(float angle)
        {
            if (pivot == null)
                return;

            // The imported root is an authored art pose (30/-25/15 degrees),
            // while 0/90 are defined in the neutral pivot basis. Rotate by the
            // DELTA from the imported angle so the root keeps its authored
            // offset from the pivot instead of snapping to pivot.position.
            float deltaAngle = Mathf.DeltaAngle(importedAngle, angle);
            Quaternion delta = Quaternion.AngleAxis(deltaAngle, pivot.up);

            if (visualGateRoot != null)
            {
                visualGateRoot.position =
                    pivot.position +
                    delta * (importedVisualPosition - pivot.position);
                visualGateRoot.rotation =
                    delta * importedVisualRotation;
            }

            if (collisionGateRoot != null)
            {
                collisionGateRoot.position =
                    pivot.position +
                    delta * (importedCollisionPosition - pivot.position);
                collisionGateRoot.rotation =
                    delta * importedCollisionRotation;
            }
        }

        private bool HasFigureInClearance()
        {
            lastBlockingFigure = null;

            if (clearanceReference == null)
                return false;

            Vector3 up = clearanceReference.up;
            float cylinder = Mathf.Max(
                0f,
                clearanceHeight - 2f * clearanceRadius);
            Vector3 center = clearanceReference.position;
            Vector3 p0 = center - up * (cylinder * 0.5f);
            Vector3 p1 = center + up * (cylinder * 0.5f);

            int count = Physics.OverlapCapsuleNonAlloc(
                p0,
                p1,
                clearanceRadius,
                overlapBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int index = 0;
                 index < count && index < overlapBuffer.Length;
                 index++)
            {
                Collider hit = overlapBuffer[index];
                FigureMotor figure = hit != null
                    ? hit.GetComponentInParent<FigureMotor>()
                    : null;

                if (figure == null ||
                    !figure.gameObject.activeInHierarchy)
                {
                    continue;
                }

                lastBlockingFigure = figure;
                return true;
            }

            return false;
        }

        public void ResetLivingGalleryState()
        {
            CaptureImportedBasis();
            ApplyInitialLogicalState();
        }
    }
}
