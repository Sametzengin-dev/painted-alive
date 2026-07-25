using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.StainMovement
{
    [DefaultExecutionOrder(14500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class StainSurfaceCrawlController : MonoBehaviour
    {
        private const int MaximumSurfaceHits = 24;

        private readonly RaycastHit[] surfaceHits =
            new RaycastHit[MaximumSurfaceHits];

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
        private StainSurfaceCrawlConfig config;

        [SerializeField]
        private Transform stainVisual;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool isCrawling;

        [SerializeField]
        private bool hasSurface;

        [SerializeField]
        private Vector3 surfaceNormal = Vector3.up;

        [SerializeField]
        private string surfaceType = "Yok";

        [SerializeField]
        private string lastResult = "Leke formu bekleniyor";

        [SerializeField]
        private bool externallySuspended;

        private bool figureMotorWasEnabled;
        private float surfaceLostSeconds;
        private float nextAttachTime;
        private Vector3 lastMoveDirection;
        private Vector3 surfacePoint;
        private Renderer stainRenderer;
        private Renderer[] figureRenderers;
        private bool[] figureRendererStates;

        public bool IsCrawling => isCrawling;
        public bool HasSurface => hasSurface;
        public bool HasStainVisual =>
            stainVisual != null && stainRenderer != null;
        public bool IsExternallySuspended => externallySuspended;
        public Vector3 SurfaceNormal => surfaceNormal;
        public string SurfaceType => surfaceType;
        public string LastResult => lastResult;

        private bool WantsCrawl =>
            roleAuthority != null &&
            !roleAuthority.IsInkPainter &&
            clarityState != null &&
            clarityState.CurrentLevel == FigureClarityLevel.Stain &&
            !externallySuspended &&
            (hijackController == null ||
             !hijackController.IsHijacking);

        private void Awake()
        {
            ResolveReferences();

            if (clarityState == null ||
                figureMotor == null ||
                characterController == null ||
                figureCamera == null ||
                roleAuthority == null ||
                hijackController == null ||
                config == null ||
                stainVisual == null ||
                stainRenderer == null)
            {
                Debug.LogError(
                    "M28 Stain surface crawl references are incomplete. " +
                    "Run M28 Setup again.",
                    this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            DeactivateCrawl();
        }

        private void Update()
        {
            if (!WantsCrawl || IsEditingText())
            {
                DeactivateCrawl();
                return;
            }

            if (!isCrawling)
            {
                ActivateCrawl();
            }

            UpdateCrawl(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (isCrawling)
            {
                UpdateStainVisual(Time.deltaTime);
            }
        }

        public void Configure(
            FigureClarityState targetClarity,
            FigureMotor targetMotor,
            CharacterController targetCharacterController,
            Camera targetCamera,
            InkPainterRoleAuthority targetRoleAuthority,
            InkStainCreatureHijackController targetHijackController,
            StainSurfaceCrawlConfig crawlConfig,
            Transform targetStainVisual)
        {
            clarityState = targetClarity;
            figureMotor = targetMotor;
            characterController = targetCharacterController;
            figureCamera = targetCamera;
            roleAuthority = targetRoleAuthority;
            hijackController = targetHijackController;
            config = crawlConfig;
            stainVisual = targetStainVisual;
            stainRenderer = stainVisual != null
                ? stainVisual.GetComponent<Renderer>()
                : null;
        }

        public void SetExternalSuspended(
            bool suspended,
            string reason)
        {
            if (externallySuspended == suspended)
            {
                return;
            }

            externallySuspended = suspended;

            if (suspended)
            {
                DeactivateCrawl();
                lastResult = string.IsNullOrWhiteSpace(reason)
                    ? "Harici sistem tarafından askıya alındı"
                    : reason;
                return;
            }

            if (clarityState != null &&
                clarityState.CurrentLevel !=
                FigureClarityLevel.Stain)
            {
                RestoreFigureRenderers();

                bool mayRestoreNormalMotor =
                    figureMotor != null &&
                    figureMotorWasEnabled &&
                    roleAuthority != null &&
                    !roleAuthority.IsInkPainter &&
                    (hijackController == null ||
                     !hijackController.IsHijacking);

                if (mayRestoreNormalMotor)
                {
                    figureMotor.enabled = true;
                }
            }

            lastResult = string.IsNullOrWhiteSpace(reason)
                ? "Harici askı kaldırıldı"
                : reason;
        }

        private void ActivateCrawl()
        {
            if (isCrawling)
            {
                return;
            }

            figureMotorWasEnabled =
                figureMotor != null && figureMotor.enabled;

            if (figureMotor != null)
            {
                figureMotor.ResetMotion();
                figureMotor.enabled = false;
            }

            CaptureAndHideFigureRenderers();
            SetStainVisualVisible(true);
            isCrawling = true;
            surfaceLostSeconds = 0f;
            nextAttachTime = 0f;
            lastMoveDirection = Vector3.zero;
            hasSurface = TryAcquireNearestSurface(
                out RaycastHit initialSurface);

            if (hasSurface)
            {
                SetSurface(initialSurface);
                SnapToSurface(initialSurface, 1f);
                UpdateStainVisual(1f);
                lastResult =
                    $"{surfaceType} yüzeyine tutunuldu";
            }
            else
            {
                surfaceNormal = Vector3.up;
                surfaceType = "Yok";
                lastResult = "Yakında tutunulabilir yüzey yok";
            }

            Debug.Log(
                "[M28] Leke yüzey sürünmesi aktif. " +
                "WASD: sürün, Space: yüzeyden bırak.",
                this);
        }

        private void DeactivateCrawl()
        {
            if (!isCrawling)
            {
                return;
            }

            isCrawling = false;
            SetStainVisualVisible(false);
            hasSurface = false;
            surfaceLostSeconds = 0f;
            surfaceNormal = Vector3.up;
            surfaceType = "Yok";
            lastMoveDirection = Vector3.zero;

            bool mayRestoreNormalMotor =
                figureMotor != null &&
                figureMotorWasEnabled &&
                roleAuthority != null &&
                !roleAuthority.IsInkPainter &&
                clarityState != null &&
                clarityState.CurrentLevel != FigureClarityLevel.Stain &&
                (hijackController == null ||
                 !hijackController.IsHijacking);

            if (mayRestoreNormalMotor)
            {
                figureMotor.enabled = true;
            }

            if (clarityState != null &&
                clarityState.CurrentLevel !=
                FigureClarityLevel.Stain)
            {
                RestoreFigureRenderers();
            }

            lastResult = roleAuthority != null &&
                roleAuthority.IsInkPainter
                    ? "Painter rolünde kapalı"
                    : "Leke yüzey sürünmesi kapalı";
        }

        private void UpdateCrawl(float deltaTime)
        {
            if (deltaTime <= 0f ||
                characterController == null ||
                !characterController.enabled)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame &&
                hasSurface)
            {
                hasSurface = false;
                surfaceLostSeconds = 0f;
                nextAttachTime =
                    Time.unscaledTime +
                    config.DetachReattachDelay;
                lastResult = "Yüzey bırakıldı";
            }

            if (!hasSurface)
            {
                UpdateDetached(deltaTime);
                return;
            }

            Vector2 input = ReadMovementInput(keyboard);
            Vector3 moveDirection =
                BuildSurfaceMoveDirection(input);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                TryTransitionToAdjacentSurface(
                    moveDirection,
                    out bool transitioned);

                if (transitioned)
                {
                    moveDirection =
                        BuildSurfaceMoveDirection(input);
                }

                lastMoveDirection = moveDirection;
            }

            Vector3 motion =
                moveDirection *
                config.CrawlSpeed *
                deltaTime;
            Vector3 predictedCenter =
                GetControllerWorldCenter() + motion;

            if (TryProbeSurface(
                    predictedCenter,
                    -surfaceNormal,
                    GetSurfaceProbeLength(surfaceNormal),
                    out RaycastHit support))
            {
                surfaceLostSeconds = 0f;
                SetSurface(support);
                Vector3 snap =
                    CalculateSurfaceCorrection(support);
                characterController.Move(
                    motion +
                    GetStableSurfaceCorrection(
                        snap,
                        deltaTime));
                lastResult =
                    $"{surfaceType} yüzeyinde sürünüyor";
                return;
            }

            surfaceLostSeconds += deltaTime;

            if (surfaceLostSeconds <= config.LostSurfaceGrace &&
                lastMoveDirection.sqrMagnitude > 0.001f &&
                TryAcquireNearestSurface(out RaycastHit recovery))
            {
                SetSurface(recovery);
                characterController.Move(
                    GetStableSurfaceCorrection(
                        CalculateSurfaceCorrection(recovery),
                        deltaTime));
                surfaceLostSeconds = 0f;
                lastResult = "Yüzey köşesinde tutunma korundu";
                return;
            }

            hasSurface = false;
            nextAttachTime =
                Time.unscaledTime +
                config.DetachReattachDelay;
            lastResult = "Yüzey kaybedildi";
        }

        private void UpdateDetached(float deltaTime)
        {
            characterController.Move(
                Vector3.down *
                config.DetachedFallSpeed *
                deltaTime);

            if (Time.unscaledTime < nextAttachTime)
            {
                return;
            }

            if (!TryAcquireNearestSurface(
                    out RaycastHit acquired))
            {
                lastResult = "Yakın yüzey aranıyor";
                return;
            }

            hasSurface = true;
            surfaceLostSeconds = 0f;
            SetSurface(acquired);
            SnapToSurface(acquired, deltaTime);
            lastResult =
                $"{surfaceType} yüzeyine yeniden tutunuldu";
        }

        private Vector2 ReadMovementInput(Keyboard keyboard)
        {
            Vector2 input = Vector2.zero;
            input.y += keyboard.wKey.isPressed ? 1f : 0f;
            input.y -= keyboard.sKey.isPressed ? 1f : 0f;
            input.x += keyboard.dKey.isPressed ? 1f : 0f;
            input.x -= keyboard.aKey.isPressed ? 1f : 0f;
            return Vector2.ClampMagnitude(input, 1f);
        }

        private Vector3 BuildSurfaceMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 forward;
            Vector3 right;
            float verticalSurface =
                Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up));

            if (verticalSurface < 0.55f)
            {
                forward = Vector3.ProjectOnPlane(
                    Vector3.up,
                    surfaceNormal).normalized;
                right = Vector3.Cross(
                    forward,
                    surfaceNormal).normalized;

                if (figureCamera != null &&
                    Vector3.Dot(right, figureCamera.transform.right) < 0f)
                {
                    right = -right;
                }
            }
            else
            {
                Transform reference =
                    figureCamera != null
                        ? figureCamera.transform
                        : transform;
                forward = Vector3.ProjectOnPlane(
                    reference.forward,
                    surfaceNormal).normalized;
                right = Vector3.ProjectOnPlane(
                    reference.right,
                    surfaceNormal).normalized;

                if (forward.sqrMagnitude < 0.001f)
                {
                    forward = Vector3.ProjectOnPlane(
                        transform.forward,
                        surfaceNormal).normalized;
                }

                if (right.sqrMagnitude < 0.001f)
                {
                    right = Vector3.Cross(
                        surfaceNormal,
                        forward).normalized;
                }
            }

            Vector3 direction =
                forward * input.y +
                right * input.x;
            return direction.sqrMagnitude > 1f
                ? direction.normalized
                : direction;
        }

        private bool TryTransitionToAdjacentSurface(
            Vector3 direction,
            out bool transitioned)
        {
            transitioned = false;

            if (direction.sqrMagnitude < 0.001f)
            {
                return false;
            }

            Vector3 origin = GetControllerWorldCenter();

            if (!TryProbeSurface(
                    origin,
                    direction.normalized,
                    config.TransitionProbeDistance,
                    out RaycastHit hit))
            {
                return false;
            }

            float angle =
                Vector3.Angle(surfaceNormal, hit.normal);

            if (angle < config.MinimumTransitionAngle)
            {
                return false;
            }

            SetSurface(hit);
            characterController.Move(
                GetStableSurfaceCorrection(
                    CalculateSurfaceCorrection(hit),
                    Mathf.Max(
                        0.001f,
                        Time.deltaTime)));
            transitioned = true;
            lastResult =
                $"{surfaceType} yüzeyine geçildi";
            return true;
        }

        private bool TryAcquireNearestSurface(
            out RaycastHit bestHit)
        {
            Vector3 center = GetControllerWorldCenter();
            float bestDistance = float.PositiveInfinity;
            bestHit = default;

            TryCandidate(
                center,
                -surfaceNormal,
                ref bestHit,
                ref bestDistance);
            TryCandidate(
                center,
                Vector3.down,
                ref bestHit,
                ref bestDistance);
            TryCandidate(
                center,
                transform.forward,
                ref bestHit,
                ref bestDistance);
            TryCandidate(
                center,
                -transform.forward,
                ref bestHit,
                ref bestDistance);
            TryCandidate(
                center,
                transform.right,
                ref bestHit,
                ref bestDistance);
            TryCandidate(
                center,
                -transform.right,
                ref bestHit,
                ref bestDistance);
            TryCandidate(
                center,
                Vector3.up,
                ref bestHit,
                ref bestDistance);

            return bestHit.collider != null;
        }

        private void TryCandidate(
            Vector3 origin,
            Vector3 direction,
            ref RaycastHit bestHit,
            ref float bestDistance)
        {
            float length =
                GetSurfaceProbeLength(-direction.normalized);

            if (!TryProbeSurface(
                    origin,
                    direction,
                    length,
                    out RaycastHit candidate) ||
                candidate.distance >= bestDistance)
            {
                return;
            }

            bestHit = candidate;
            bestDistance = candidate.distance;
        }

        private bool TryProbeSurface(
            Vector3 origin,
            Vector3 direction,
            float distance,
            out RaycastHit bestHit)
        {
            bestHit = default;

            if (direction.sqrMagnitude < 0.001f)
            {
                return false;
            }

            int count = Physics.SphereCastNonAlloc(
                origin,
                config.ProbeRadius,
                direction.normalized,
                surfaceHits,
                Mathf.Max(0.05f, distance),
                config.SurfaceMask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = surfaceHits[i];

                if (hit.collider == null ||
                    IsOwnCollider(hit.collider) ||
                    hit.distance >= nearest)
                {
                    continue;
                }

                nearest = hit.distance;
                bestHit = hit;
            }

            return bestHit.collider != null;
        }

        private Vector3 CalculateSurfaceCorrection(
            RaycastHit surface)
        {
            Vector3 center = GetControllerWorldCenter();
            float extent =
                GetControllerExtentAlong(surface.normal);
            Vector3 desiredCenter =
                surface.point +
                surface.normal *
                (extent + config.SurfaceGap);
            return desiredCenter - center;
        }

        private void SnapToSurface(
            RaycastHit surface,
            float deltaTime)
        {
            Vector3 correction =
                CalculateSurfaceCorrection(surface);
            float maximum =
                deltaTime >= 0.99f
                    ? correction.magnitude
                    : config.SurfaceSnapSpeed *
                      Mathf.Max(0.001f, deltaTime);
            characterController.Move(
                correction.sqrMagnitude < 0.000225f
                    ? Vector3.zero
                    : Vector3.ClampMagnitude(
                        correction,
                        maximum));
        }

        private float GetSurfaceProbeLength(Vector3 normal)
        {
            return GetControllerExtentAlong(normal) +
                config.SurfaceProbeDistance +
                config.ProbeRadius;
        }

        private float GetControllerExtentAlong(Vector3 normal)
        {
            if (characterController == null)
            {
                return 0.5f;
            }

            float radius =
                Mathf.Max(0.01f, characterController.radius);
            float halfHeight =
                Mathf.Max(
                    radius,
                    characterController.height * 0.5f);
            float capsuleSegment =
                Mathf.Max(0f, halfHeight - radius);
            float verticalContribution =
                Mathf.Abs(
                    Vector3.Dot(
                        transform.up,
                        normal.normalized));
            return radius +
                capsuleSegment * verticalContribution;
        }

        private Vector3 GetControllerWorldCenter()
        {
            return characterController != null
                ? transform.TransformPoint(
                    characterController.center)
                : transform.position + Vector3.up;
        }

        private void SetSurface(RaycastHit surface)
        {
            Vector3 normal = surface.normal;

            if (normal.sqrMagnitude < 0.001f)
            {
                normal = Vector3.up;
            }

            surfaceNormal = normal.normalized;
            surfacePoint = surface.point;
            float upDot =
                Vector3.Dot(surfaceNormal, Vector3.up);
            surfaceType = upDot > 0.65f
                ? "Zemin"
                : upDot < -0.65f
                    ? "Tavan"
                    : "Duvar";
        }

        private Vector3 GetStableSurfaceCorrection(
            Vector3 correction,
            float deltaTime)
        {
            if (correction.sqrMagnitude < 0.000225f)
            {
                return Vector3.zero;
            }

            return Vector3.ClampMagnitude(
                correction,
                config.SurfaceSnapSpeed *
                Mathf.Max(0.001f, deltaTime));
        }

        private void SetStainVisualVisible(bool visible)
        {
            if (stainRenderer == null &&
                stainVisual != null)
            {
                stainRenderer =
                    stainVisual.GetComponent<Renderer>();
            }

            if (stainRenderer != null)
            {
                stainRenderer.enabled = visible;
            }
        }

        private void CaptureAndHideFigureRenderers()
        {
            if (figureRenderers == null ||
                figureRendererStates == null)
            {
                Renderer[] allRenderers =
                    GetComponentsInChildren<Renderer>(true);
                int count = 0;

                for (int i = 0; i < allRenderers.Length; i++)
                {
                    if (allRenderers[i] != null &&
                        allRenderers[i] != stainRenderer)
                    {
                        count++;
                    }
                }

                figureRenderers = new Renderer[count];
                figureRendererStates = new bool[count];
                int targetIndex = 0;

                for (int i = 0; i < allRenderers.Length; i++)
                {
                    Renderer target = allRenderers[i];

                    if (target == null ||
                        target == stainRenderer)
                    {
                        continue;
                    }

                    figureRenderers[targetIndex] = target;
                    figureRendererStates[targetIndex] =
                        target.enabled;
                    targetIndex++;
                }
            }

            for (int i = 0; i < figureRenderers.Length; i++)
            {
                if (figureRenderers[i] != null)
                {
                    figureRenderers[i].enabled = false;
                }
            }
        }

        private void RestoreFigureRenderers()
        {
            if (figureRenderers == null ||
                figureRendererStates == null)
            {
                return;
            }

            for (int i = 0; i < figureRenderers.Length; i++)
            {
                Renderer target = figureRenderers[i];

                if (target != null &&
                    i < figureRendererStates.Length)
                {
                    target.enabled =
                        figureRendererStates[i];
                }
            }

            figureRenderers = null;
            figureRendererStates = null;
        }

        private void UpdateStainVisual(float deltaTime)
        {
            if (stainVisual == null)
            {
                return;
            }

            Vector3 targetPosition;
            Vector3 targetUp;

            if (hasSurface)
            {
                targetUp = surfaceNormal;
                targetPosition =
                    surfacePoint +
                    targetUp * 0.075f;
            }
            else
            {
                targetUp = Vector3.up;
                targetPosition =
                    transform.position +
                    Vector3.up * 0.075f;
            }

            Vector3 forward =
                Vector3.ProjectOnPlane(
                    lastMoveDirection,
                    targetUp);

            if (forward.sqrMagnitude < 0.001f &&
                figureCamera != null)
            {
                forward =
                    Vector3.ProjectOnPlane(
                        figureCamera.transform.forward,
                        targetUp);
            }

            if (forward.sqrMagnitude < 0.001f)
            {
                forward =
                    Vector3.ProjectOnPlane(
                        Vector3.forward,
                        targetUp);
            }

            if (forward.sqrMagnitude < 0.001f)
            {
                forward =
                    Vector3.Cross(
                        targetUp,
                        Vector3.right);
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    forward.normalized,
                    targetUp);
            float blend =
                deltaTime >= 0.99f
                    ? 1f
                    : 1f -
                      Mathf.Exp(
                          -14f *
                          Mathf.Max(0.001f, deltaTime));

            stainVisual.position =
                Vector3.Lerp(
                    stainVisual.position,
                    targetPosition,
                    blend);
            stainVisual.rotation =
                Quaternion.Slerp(
                    stainVisual.rotation,
                    targetRotation,
                    blend);
        }

        private bool IsOwnCollider(Collider candidate)
        {
            return candidate != null &&
                (candidate.transform == transform ||
                 candidate.transform.IsChildOf(transform));
        }

        private void ResolveReferences()
        {
            if (clarityState == null)
            {
                clarityState =
                    GetComponent<FigureClarityState>();
            }

            if (figureMotor == null)
            {
                figureMotor = GetComponent<FigureMotor>();
            }

            if (characterController == null)
            {
                characterController =
                    GetComponent<CharacterController>();
            }

            if (roleAuthority == null)
            {
                roleAuthority =
                    UnityEngine.Object.FindFirstObjectByType<
                        InkPainterRoleAuthority>(
                        FindObjectsInactive.Include);
            }

            if (figureCamera == null)
            {
                figureCamera =
                    GetComponentInChildren<Camera>(true);

                if (figureCamera == null &&
                    roleAuthority != null)
                {
                    figureCamera =
                        roleAuthority.ActiveRoleCamera;
                }
            }

            if (hijackController == null)
            {
                hijackController =
                    GetComponent<
                        InkStainCreatureHijackController>();
            }

            if (stainVisual == null)
            {
                stainVisual =
                    transform.Find(
                        "M28_PlayerStainVisual");
            }

            if (stainVisual != null &&
                stainRenderer == null)
            {
                stainRenderer =
                    stainVisual.GetComponent<Renderer>();
            }
        }

        private static bool IsEditingText()
        {
            GameObject selected =
                EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;

            return selected != null &&
                (selected.GetComponent("TMP_InputField") != null ||
                 selected.GetComponent("InputField") != null);
        }
    }
}
