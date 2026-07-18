using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class StainWallCrawlController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FigureMotor figureMotor;
        [SerializeField] private Transform stainVisualPivot;
        [SerializeField] private Transform wallProbeOrigin;

        [Header("Input")]
        [SerializeField] private InputActionProperty moveAction;
        [SerializeField] private InputActionProperty clingAction;

        [Header("Wall Detection")]
        [SerializeField] private LayerMask climbableSurfaceMask;
        [SerializeField, Min(0.01f)] private float probeRadius = 0.10f;
        [SerializeField, Min(0.1f)] private float probeDistance = 0.75f;
        [SerializeField, Range(0f, 1f)] private float maximumWallUpDot = 0.35f;

        [Header("Crawl")]
        [SerializeField, Min(0.1f)] private float crawlSpeed = 1.8f;
        [SerializeField, Min(0f)] private float stickSpeed = 1.5f;
        [SerializeField, Min(0.5f)] private float maximumCrawlDuration = 4f;
        [SerializeField, Min(0f)] private float crawlCooldown = 2f;

        [Header("Ledge Pull")]
        [SerializeField] private bool allowLedgePull = true;
        [SerializeField, Min(0f)] private float ledgeUpDistance = 0.32f;
        [SerializeField, Min(0f)] private float ledgeForwardDistance = 0.42f;

        private CharacterController characterController;
        private FigureClarityState clarityState;

        private Vector3 currentWallNormal;
        private Quaternion originalStainVisualRotation;

        private float crawlStartedTime;
        private float nextAllowedCrawlTime;

        private bool isCrawling;
        private bool waitingForRelease;
        private bool enabledMoveInputHere;
        private bool enabledClingInputHere;

        public bool IsInStainForm =>
            clarityState != null &&
            clarityState.CurrentLevel == FigureClarityLevel.Stain;

        public bool IsCrawling => isCrawling;

        public float CrawlCooldownRemaining =>
            Mathf.Max(0f, nextAllowedCrawlTime - Time.time);

        public float CrawlTimeRemaining
        {
            get
            {
                if (!isCrawling)
                    return 0f;

                float elapsed = Time.time - crawlStartedTime;

                return Mathf.Max(
                    0f,
                    maximumCrawlDuration - elapsed);
            }
        }

        public float AbilityNormalized
        {
            get
            {
                if (isCrawling)
                {
                    return maximumCrawlDuration > 0f
                        ? Mathf.Clamp01(
                            CrawlTimeRemaining /
                            maximumCrawlDuration)
                        : 0f;
                }

                if (CrawlCooldownRemaining > 0f)
                {
                    return crawlCooldown > 0f
                        ? 1f - Mathf.Clamp01(
                            CrawlCooldownRemaining /
                            crawlCooldown)
                        : 1f;
                }

                return 1f;
            }
        }

        public bool CanStartCrawl =>
            IsInStainForm &&
            !isCrawling &&
            !waitingForRelease &&
            CrawlCooldownRemaining <= 0f;

        private void Awake()
        {
            characterController =
                GetComponent<CharacterController>();

            clarityState =
                GetComponent<FigureClarityState>();

            if (figureMotor == null)
                figureMotor = GetComponent<FigureMotor>();

            if (wallProbeOrigin == null)
                wallProbeOrigin = transform;

            if (stainVisualPivot != null)
            {
                originalStainVisualRotation =
                    stainVisualPivot.localRotation;
            }
        }

        private void OnEnable()
        {
            enabledMoveInputHere =
                EnableActionIfRequired(moveAction.action);

            enabledClingInputHere =
                EnableActionIfRequired(clingAction.action);
        }

        private void OnDisable()
        {
            if (enabledMoveInputHere &&
                moveAction.action != null)
            {
                moveAction.action.Disable();
            }

            if (enabledClingInputHere &&
                clingAction.action != null)
            {
                clingAction.action.Disable();
            }

            enabledMoveInputHere = false;
            enabledClingInputHere = false;

            // RoleSwitcher motor durumunu yöneteceği için burada
            // FigureMotor tekrar açılmaz.
            CancelCrawlWithoutRestoringMotor();
        }

        private void Update()
        {
            bool clingHeld =
                clingAction.action != null &&
                clingAction.action.IsPressed();

            if (!clingHeld)
            {
                waitingForRelease = false;

                if (isCrawling)
                    StopCrawl(true);

                return;
            }

            if (!IsInStainForm)
            {
                if (isCrawling)
                    StopCrawl(true);

                return;
            }

            if (isCrawling)
            {
                UpdateCrawl();
                return;
            }

            if (!CanStartCrawl)
                return;

            if (TryFindWallInFront(out RaycastHit hit))
                BeginCrawl(hit);
        }

        private void BeginCrawl(RaycastHit wallHit)
        {
            isCrawling = true;
            crawlStartedTime = Time.time;
            currentWallNormal = wallHit.normal.normalized;

            if (figureMotor != null)
            {
                figureMotor.ResetMotion();
                figureMotor.enabled = false;
            }

            UpdateStainVisualOrientation();
        }

        private void UpdateCrawl()
        {
            if (Time.time - crawlStartedTime >=
                maximumCrawlDuration)
            {
                StopCrawl(true);
                return;
            }

            Vector2 moveInput = moveAction.action != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            if (!TryMaintainWallContact(out RaycastHit wallHit))
            {
                if (allowLedgePull && moveInput.y > 0.35f)
                    TryPullOverLedge();

                StopCrawl(true);
                return;
            }

            currentWallNormal =
                wallHit.normal.normalized;

            Vector3 wallUp = Vector3.ProjectOnPlane(
                Vector3.up,
                currentWallNormal);

            if (wallUp.sqrMagnitude < 0.001f)
            {
                StopCrawl(true);
                return;
            }

            wallUp.Normalize();

            Vector3 wallRight = Vector3.Cross(
                currentWallNormal,
                wallUp).normalized;

            Vector3 desiredDirection =
                wallRight * moveInput.x +
                wallUp * moveInput.y;

            if (desiredDirection.sqrMagnitude > 1f)
                desiredDirection.Normalize();

            Vector3 crawlMovement =
                desiredDirection *
                crawlSpeed *
                Time.deltaTime;

            Vector3 adhesionMovement =
                -currentWallNormal *
                stickSpeed *
                Time.deltaTime;

            characterController.Move(
                crawlMovement + adhesionMovement);

            UpdateStainVisualOrientation();
        }

        private bool TryFindWallInFront(
            out RaycastHit hit)
        {
            Vector3 direction = transform.forward;

            bool found = Physics.SphereCast(
                wallProbeOrigin.position,
                probeRadius,
                direction,
                out hit,
                probeDistance,
                climbableSurfaceMask,
                QueryTriggerInteraction.Ignore);

            return found && IsValidWall(hit.normal);
        }

        private bool TryMaintainWallContact(
            out RaycastHit hit)
        {
            Vector3 probeStart =
                characterController.bounds.center +
                currentWallNormal * 0.05f;

            bool found = Physics.SphereCast(
                probeStart,
                probeRadius,
                -currentWallNormal,
                out hit,
                probeDistance,
                climbableSurfaceMask,
                QueryTriggerInteraction.Ignore);

            return found && IsValidWall(hit.normal);
        }

        private bool IsValidWall(Vector3 normal)
        {
            float upDot = Mathf.Abs(
                Vector3.Dot(normal.normalized, Vector3.up));

            return upDot <= maximumWallUpDot;
        }

        private void TryPullOverLedge()
        {
            Vector3 upwardMovement =
                Vector3.up * ledgeUpDistance;

            Vector3 forwardMovement =
                -currentWallNormal * ledgeForwardDistance;

            characterController.Move(upwardMovement);
            characterController.Move(forwardMovement);
        }

        private void StopCrawl(bool restoreMotor)
        {
            if (!isCrawling)
                return;

            isCrawling = false;
            waitingForRelease = true;
            nextAllowedCrawlTime =
                Time.time + crawlCooldown;

            RestoreStainVisualOrientation();

            if (restoreMotor && figureMotor != null)
            {
                figureMotor.enabled = true;
                figureMotor.ResetMotion();
            }
        }

        private void CancelCrawlWithoutRestoringMotor()
        {
            if (!isCrawling)
                return;

            isCrawling = false;
            waitingForRelease = false;

            nextAllowedCrawlTime =
                Time.time + crawlCooldown;

            RestoreStainVisualOrientation();
        }

        private void UpdateStainVisualOrientation()
        {
            if (stainVisualPivot == null)
                return;

            Vector3 wallUp = Vector3.ProjectOnPlane(
                Vector3.up,
                currentWallNormal);

            if (wallUp.sqrMagnitude < 0.001f)
                return;

            wallUp.Normalize();

            stainVisualPivot.rotation =
                Quaternion.LookRotation(
                    wallUp,
                    currentWallNormal);
        }

        private void RestoreStainVisualOrientation()
        {
            if (stainVisualPivot == null)
                return;

            stainVisualPivot.localRotation =
                originalStainVisualRotation;
        }

        private static bool EnableActionIfRequired(
            InputAction action)
        {
            if (action == null || action.enabled)
                return false;

            action.Enable();
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin =
                wallProbeOrigin != null
                    ? wallProbeOrigin
                    : transform;

            Gizmos.color = new Color(
                0.25f,
                0.8f,
                0.65f,
                0.8f);

            Gizmos.DrawWireSphere(
                origin.position,
                probeRadius);

            Gizmos.DrawLine(
                origin.position,
                origin.position +
                transform.forward * probeDistance);
        }
    }
}
