using PaintedAlive.Figures.StainMovement;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.StainSupport
{
    [DefaultExecutionOrder(14600)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class StainSpongeCarryController :
        MonoBehaviour
    {
        private const int MaximumGroundHits = 24;

        private readonly RaycastHit[] groundHits =
            new RaycastHit[MaximumGroundHits];

        [Header("References")]
        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private CharacterController characterController;

        [SerializeField]
        private Camera figureCamera;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkStainCreatureHijackController hijackController;

        [SerializeField]
        private StainSurfaceCrawlController crawlController;

        [SerializeField]
        private StainSpongeCarryConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private StainSpongeCarrier currentCarrier;

        [SerializeField]
        private StainSpongeCarrier nearbyCarrier;

        [SerializeField]
        private string lastResult = "Leke formu bekleniyor";

        private float nextInputTime;
        private Vector3 lastSafeFigurePosition;
        private bool characterControllerWasEnabled;

        public FigureClarityState ClarityState => clarityState;
        public bool IsCarried => currentCarrier != null;
        public StainSpongeCarrier CurrentCarrier =>
            currentCarrier;
        public StainSpongeCarrier NearbyCarrier =>
            nearbyCarrier;
        public string LastResult => lastResult;

        private bool IsStainRoleActive =>
            clarityState != null &&
            clarityState.CurrentLevel ==
                FigureClarityLevel.Stain &&
            roleAuthority != null &&
            !roleAuthority.IsInkPainter;

        private void Awake()
        {
            ResolveReferences();

            if (clarityState == null ||
                figureMotor == null ||
                characterController == null ||
                figureCamera == null ||
                roleAuthority == null ||
                hijackController == null ||
                crawlController == null ||
                config == null)
            {
                Debug.LogError(
                    "M29 Stain sponge carry references are " +
                    "incomplete. Run M29 Setup again.",
                    this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (currentCarrier != null)
            {
                ForceReleaseToLastSafePosition(
                    "M29 denetleyicisi kapandı");
            }
        }

        private void Update()
        {
            if (currentCarrier != null)
            {
                UpdateCarriedState();
                return;
            }

            if (!IsStainRoleActive ||
                hijackController.IsHijacking ||
                IsEditingText())
            {
                nearbyCarrier = null;
                return;
            }

            nearbyCarrier = FindNearestCarrier();

            if (nearbyCarrier == null)
            {
                lastResult = "Yakında boş kurtarma süngeri yok";
                return;
            }

            lastResult = nearbyCarrier.IsPrototypeCarrier
                ? "E: test kurtarma süngerine gir"
                : "E: takım arkadaşının süngerine gir";

            if (WasUsePressed() &&
                Time.unscaledTime >= nextInputTime)
            {
                TryEnterCarrier(nearbyCarrier);
            }
        }

        private void LateUpdate()
        {
            if (currentCarrier == null ||
                currentCarrier.CarrySocket == null)
            {
                return;
            }

            transform.position =
                currentCarrier.CarrySocket.position;
        }

        public void Configure(
            FigureClarityState targetClarity,
            FigureMotor targetFigureMotor,
            CharacterController targetCharacterController,
            Camera targetCamera,
            InkPainterRoleAuthority targetRoleAuthority,
            InkStainCreatureHijackController targetHijack,
            StainSurfaceCrawlController targetCrawl,
            StainSpongeCarryConfig targetConfig)
        {
            clarityState = targetClarity;
            figureMotor = targetFigureMotor;
            characterController = targetCharacterController;
            figureCamera = targetCamera;
            roleAuthority = targetRoleAuthority;
            hijackController = targetHijack;
            crawlController = targetCrawl;
            config = targetConfig;
        }

        public bool TryCompleteRestoration(
            float restoredNormalizedClarity,
            out string result)
        {
            result = "Restorasyon başlatılamadı";

            if (currentCarrier == null ||
                clarityState == null ||
                clarityState.CurrentLevel !=
                    FigureClarityLevel.Stain)
            {
                lastResult =
                    "Restorasyon için süngerde Leke yok";
                result = lastResult;
                return false;
            }

            if (!TryFindSafeExitPosition(
                    currentCarrier,
                    out Vector3 exitPosition))
            {
                lastResult =
                    "Restorasyon yanında güvenli çıkış zemini yok";
                result = lastResult;
                return false;
            }

            StainSpongeCarrier restoredCarrier =
                currentCarrier;
            currentCarrier = null;
            restoredCarrier.Release(this);

            // Restoration can notify other systems through
            // FigureClarityState.LevelChanged. Keep the normal motor
            // disabled until the CharacterController is ready so no
            // listener can leave FigureMotor moving an inactive
            // controller for a frame.
            StopNormalMotor();
            MoveToFigurePosition(exitPosition);
            characterControllerWasEnabled = true;
            EnsureCharacterControllerReady();

            clarityState.RestorePartial(
                Mathf.Clamp(
                    restoredNormalizedClarity,
                    0.05f,
                    0.95f));

            EnsureCharacterControllerReady();
            lastSafeFigurePosition = exitPosition;
            RestoreMovementAfterCarry();
            nextInputTime =
                Time.unscaledTime + config.InputCooldown;

            bool restored =
                clarityState.CurrentLevel !=
                    FigureClarityLevel.Stain;
            lastResult = restored
                ? "Leke kısmi Netlikle Figür olarak restore edildi"
                : "Restorasyon Netlik seviyesini yükseltemedi";
            result = lastResult;
            return restored;
        }

        public void HandleCarrierUnavailable(
            StainSpongeCarrier unavailableCarrier)
        {
            if (currentCarrier != unavailableCarrier)
            {
                return;
            }

            currentCarrier = null;
            MoveToFigurePosition(lastSafeFigurePosition);
            RestoreMovementAfterCarry();
            lastResult =
                "Taşıyıcı kayboldu; son güvenli konuma dönüldü";
        }

        private void UpdateCarriedState()
        {
            nearbyCarrier = null;

            if (currentCarrier == null ||
                currentCarrier.CarrySocket == null)
            {
                ForceReleaseToLastSafePosition(
                    "Taşıyıcı bağlantısı kayboldu");
                return;
            }

            if (!IsStainRoleActive)
            {
                ForceReleaseToLastSafePosition(
                    "Rol veya Netlik değişti");
                return;
            }

            lastResult = currentCarrier.IsPrototypeCarrier
                ? "Süngerde taşınıyor • Ok tuşları: taşı • E: çık"
                : "Takım arkadaşının süngerinde • E: çık";

            if (!IsEditingText() &&
                WasUsePressed() &&
                Time.unscaledTime >= nextInputTime)
            {
                TryExitCarrier();
            }
        }

        private bool TryEnterCarrier(
            StainSpongeCarrier carrier)
        {
            if (carrier == null ||
                !carrier.TryBoard(this) ||
                !IsStainRoleActive ||
                hijackController.IsHijacking)
            {
                lastResult = "Süngere girilemedi";
                return false;
            }

            lastSafeFigurePosition = transform.position;
            characterControllerWasEnabled =
                characterController.enabled;
            crawlController.SetExternalSuspended(
                true,
                "M29: kurtarma süngerinde taşınıyor");
            StopNormalMotor();

            if (characterController.enabled)
            {
                characterController.enabled = false;
            }

            currentCarrier = carrier;
            transform.position =
                carrier.CarrySocket.position;
            nextInputTime =
                Time.unscaledTime + config.InputCooldown;
            lastResult = "Kurtarma süngerine girildi";
            return true;
        }

        private bool TryExitCarrier()
        {
            if (currentCarrier == null)
            {
                return false;
            }

            if (!TryFindSafeExitPosition(
                    currentCarrier,
                    out Vector3 exitPosition))
            {
                lastResult =
                    "Çıkış için güvenli yüzey bulunamadı";
                nextInputTime =
                    Time.unscaledTime + config.InputCooldown;
                return false;
            }

            StainSpongeCarrier releasedCarrier =
                currentCarrier;
            currentCarrier = null;
            releasedCarrier.Release(this);
            MoveToFigurePosition(exitPosition);
            RestoreMovementAfterCarry();
            lastSafeFigurePosition = exitPosition;
            nextInputTime =
                Time.unscaledTime + config.InputCooldown;
            lastResult = "Güvenli yüzeye bırakıldı";
            return true;
        }

        private void ForceReleaseToLastSafePosition(
            string reason)
        {
            StainSpongeCarrier releasedCarrier =
                currentCarrier;
            currentCarrier = null;

            if (releasedCarrier != null)
            {
                releasedCarrier.Release(this);
            }

            MoveToFigurePosition(lastSafeFigurePosition);
            RestoreMovementAfterCarry();
            nextInputTime =
                Time.unscaledTime + config.InputCooldown;
            lastResult = reason;
        }

        private void RestoreMovementAfterCarry()
        {
            bool shouldUseNormalMotor =
                clarityState != null &&
                clarityState.CurrentLevel !=
                    FigureClarityLevel.Stain &&
                roleAuthority != null &&
                !roleAuthority.IsInkPainter &&
                !hijackController.IsHijacking;

            if (shouldUseNormalMotor)
            {
                // A restored Figure always needs its controller, even
                // when it was intentionally disabled while carried.
                characterControllerWasEnabled = true;
                EnsureCharacterControllerReady();
            }
            else if (characterController != null &&
                     characterControllerWasEnabled &&
                     !characterController.enabled &&
                     characterController.gameObject.activeInHierarchy)
            {
                characterController.enabled = true;
            }

            if (crawlController != null)
            {
                crawlController.SetExternalSuspended(
                    false,
                    "M29: sünger taşıması sona erdi");
            }

            if (figureMotor != null && shouldUseNormalMotor)
            {
                if (EnsureCharacterControllerReady())
                {
                    figureMotor.enabled = true;
                }
                else
                {
                    figureMotor.enabled = false;
                    Debug.LogError(
                        "M30 restoration could not reactivate " +
                        "CharacterController. FigureMotor was kept " +
                        "disabled to prevent inactive-controller Move calls.",
                        this);
                }
            }
        }

        private void StopNormalMotor()
        {
            if (figureMotor == null)
            {
                return;
            }

            figureMotor.ResetMotion();
            figureMotor.enabled = false;
        }

        private bool EnsureCharacterControllerReady()
        {
            if (characterController == null ||
                !characterController.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!characterController.enabled)
            {
                characterController.enabled = true;
            }

            return characterController.enabled &&
                characterController.gameObject.activeInHierarchy;
        }

        private void MoveToFigurePosition(Vector3 position)
        {
            bool wasEnabled =
                characterController != null &&
                characterController.enabled;

            if (wasEnabled)
            {
                characterController.enabled = false;
            }

            transform.position = position;

            if (wasEnabled)
            {
                characterController.enabled = true;
            }
        }

        private StainSpongeCarrier FindNearestCarrier()
        {
            StainSpongeCarrier best = null;
            float bestDistanceSquared =
                config.InteractionRange *
                config.InteractionRange;
            var carriers =
                StainSpongeCarrier.ActiveCarriers;

            for (int i = 0; i < carriers.Count; i++)
            {
                StainSpongeCarrier candidate = carriers[i];

                if (candidate == null ||
                    !candidate.CanBoard(this))
                {
                    continue;
                }

                Vector3 targetPosition =
                    candidate.CarrySocket.position;
                float distanceSquared =
                    (targetPosition - transform.position)
                    .sqrMagnitude;

                if (distanceSquared > bestDistanceSquared ||
                    !HasClearPath(
                        targetPosition,
                        candidate))
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
            }

            return best;
        }

        private bool HasClearPath(
            Vector3 targetPosition,
            StainSpongeCarrier candidateCarrier)
        {
            Vector3 origin =
                transform.position + Vector3.up * 0.15f;
            Vector3 direction = targetPosition - origin;
            float distance = direction.magnitude;

            if (distance <= 0.05f)
            {
                return true;
            }

            RaycastHit[] hits =
                Physics.RaycastAll(
                    origin,
                    direction / distance,
                    distance,
                    config.SurfaceMask,
                    QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;

                if (hitCollider == null ||
                    hitCollider.transform.IsChildOf(transform) ||
                    (candidateCarrier != null &&
                     hitCollider.transform.IsChildOf(
                         candidateCarrier.transform)))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private bool TryFindSafeExitPosition(
            StainSpongeCarrier carrier,
            out Vector3 safePosition)
        {
            Vector3[] directions =
            {
                carrier.transform.right,
                -carrier.transform.right,
                -carrier.transform.forward,
                carrier.transform.forward
            };

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 candidate =
                    carrier.transform.position +
                    Vector3.ProjectOnPlane(
                        directions[i],
                        Vector3.up).normalized *
                    config.ExitHorizontalOffset;

                if (TryResolveGroundedFigurePosition(
                        candidate,
                        carrier,
                        out safePosition))
                {
                    return true;
                }
            }

            safePosition = Vector3.zero;
            return false;
        }

        private bool TryResolveGroundedFigurePosition(
            Vector3 candidate,
            StainSpongeCarrier carrier,
            out Vector3 figurePosition)
        {
            Vector3 origin =
                candidate +
                Vector3.up * config.GroundProbeHeight;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                config.GroundProbeHeight +
                    config.GroundProbeDistance,
                config.SurfaceMask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearest ||
                    hit.normal.y <
                        config.MinimumExitUpDot ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.transform.IsChildOf(
                        carrier.transform))
                {
                    continue;
                }

                nearest = hit.distance;
                best = hit;
            }

            if (best.collider == null)
            {
                figurePosition = Vector3.zero;
                return false;
            }

            float localBottom =
                characterController.center.y -
                characterController.height * 0.5f;
            float rootOffset =
                characterController.skinWidth +
                0.035f -
                localBottom;
            figurePosition =
                best.point + Vector3.up * rootOffset;
            return true;
        }

        private static bool WasUsePressed()
        {
            return Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame;
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

        private void ResolveReferences()
        {
            clarityState ??=
                GetComponent<FigureClarityState>();
            figureMotor ??= GetComponent<FigureMotor>();
            characterController ??=
                GetComponent<CharacterController>();
            roleAuthority ??=
                InkPainterRoleAuthority.ActiveInstance;
            hijackController ??=
                GetComponent<
                    InkStainCreatureHijackController>();
            crawlController ??=
                GetComponent<StainSurfaceCrawlController>();

            if (figureCamera == null &&
                roleAuthority != null)
            {
                figureCamera =
                    roleAuthority.ActiveRoleCamera;
            }
        }
    }
}
