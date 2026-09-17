using System;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(260)]
    [DisallowMultipleComponent]
    public sealed class PrototypeUnderlayerCounterplayController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeUnderlayerDiveController underlayerDive;

        [SerializeField]
        private MonoBehaviour traceWeaveCounterplay;

        [SerializeField]
        private MonoBehaviour clarityState;

        [SerializeField]
        private Transform figureRoot;

        [Header("Painter Seam Aim")]
        [SerializeField, Min(8f)]
        private float seamAimPixels = 92f;

        [SerializeField, Min(2f)]
        private float seamMaximumCameraDistance = 120f;

        [Header("Oil Paint Seal")]
        [SerializeField, Min(0.25f)]
        private float oilSealSeconds = 6f;

        [Header("Watercolor Seep")]
        [SerializeField, Min(0.25f)]
        private float seepSeconds = 5f;

        [SerializeField, Min(0.2f)]
        private float seepRadius = 1.45f;

        [SerializeField, Min(0f)]
        private float seepFlowSpeed = 1.15f;

        [Header("Stain Crack Access")]
        [SerializeField, Min(0.2f)]
        private float stainCrackRadius = 0.78f;

        [SerializeField, Min(0.05f)]
        private float stainCrackHoldSeconds = 0.38f;

        [Header("HUD")]
        [SerializeField]
        private CanvasGroup statusGroup;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Text controlsText;

        [Header("Focus")]
        [SerializeField, Min(0f)]
        private float contextualHudHoldSeconds = 2.5f;

        [Header("Runtime Read Only")]
        [SerializeField]
        private bool painterRoleActive;

        [SerializeField]
        private bool figureRoleActive;

        [SerializeField]
        private bool sharedMaterialInputResolved;

        [SerializeField]
        private bool painterCameraResolved;

        [SerializeField]
        private bool seamAimValid;

        [SerializeField]
        private bool fullStain;

        [SerializeField]
        private bool stainCrackReady;

        [SerializeField]
        private bool watercolorSeepActive;

        [SerializeField]
        private int watercolorAttemptCount;

        [SerializeField]
        private int watercolorSuccessCount;

        [SerializeField]
        private int oilAttemptCount;

        [SerializeField]
        private int oilSuccessCount;

        [SerializeField]
        private int rejectedCounterplayCount;

        [SerializeField]
        private int stainEntryCount;

        [SerializeField]
        private int stainExitCount;

        [SerializeField]
        private int seepContactFrameCount;

        [SerializeField]
        private float stainCrackProgress;
        private float hudVisibleUntilUnscaled;

        [SerializeField]
        private float seepRemainingSeconds;

        [SerializeField]
        private float seepAppliedDistance;

        [SerializeField]
        private string aimedSeam = "None";

        [SerializeField]
        private string clarityLevel = "Unknown";

        [SerializeField]
        private string resolvedPainterRole = "Unknown";

        [SerializeField]
        private string resolvedFigureRole = "Unknown";

        [SerializeField]
        private string sharedInputContract = "Unresolved";

        [SerializeField]
        private string lastAction =
            "Alt Katman karşı-oyunu bekleniyor.";

        private InputAction sharedWatercolorAction;
        private InputAction sharedOilAction;

        private Camera activeCamera;
        private CharacterController characterController;
        private PropertyInfo clarityLevelProperty;

        private PrototypeUnderlayerSeam aimedSeamReference;
        private PrototypeUnderlayerSeam stainCandidate;

        private PrototypeUnderlayerSeam activeSeepSeam;
        private float seepUntil;

        private LineRenderer seepRing;
        private LineRenderer seepFlow;
        private Material seepMaterial;

        public bool PainterRoleActive => painterRoleActive;
        public bool FigureRoleActive => figureRoleActive;
        public bool SharedMaterialInputResolved =>
            sharedMaterialInputResolved;
        public bool PainterCameraResolved =>
            painterCameraResolved;
        public bool SeamAimValid => seamAimValid;
        public bool FullStain => fullStain;
        public bool StainCrackReady => stainCrackReady;
        public bool WatercolorSeepActive =>
            watercolorSeepActive;
        public int WatercolorAttemptCount =>
            watercolorAttemptCount;
        public int WatercolorSuccessCount =>
            watercolorSuccessCount;
        public int OilAttemptCount => oilAttemptCount;
        public int OilSuccessCount => oilSuccessCount;
        public int RejectedCounterplayCount =>
            rejectedCounterplayCount;
        public int StainEntryCount => stainEntryCount;
        public int StainExitCount => stainExitCount;
        public int SeepContactFrameCount =>
            seepContactFrameCount;
        public float StainCrackProgress =>
            stainCrackProgress;
        public float SeepRemainingSeconds =>
            seepRemainingSeconds;
        public float SeepAppliedDistance =>
            seepAppliedDistance;
        public string AimedSeam => aimedSeam;
        public string ClarityLevel => clarityLevel;
        public string SharedInputContract =>
            sharedInputContract;
        public string LastAction => lastAction;

        public bool UsesExistingM51PainterMaterialActionsReadOnly => true;
        public bool AddsNewPainterGameplayInputAction => false;
        public bool OilPaintSeamSealEnabled => true;
        public bool PreventsLastSafeExitSeal => true;
        public bool WatercolorUnderlayerSeepEnabled => true;
        public bool WatercolorRawDamageEnabled => false;
        public bool StainCrackEntryEnabled => true;
        public bool StainCrackNeedsNewInput => false;
        public bool FrameBraceBlocksOilSealWhileActive => true;
        public bool FrameBraceAllowsStainTraversalThroughSealedSeam => true;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeUnderlayerDiveController configuredDive,
            MonoBehaviour configuredTraceWeaveCounterplay,
            MonoBehaviour configuredClarityState,
            Transform configuredFigureRoot,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            underlayerDive = configuredDive;
            traceWeaveCounterplay =
                configuredTraceWeaveCounterplay;
            clarityState = configuredClarityState;
            figureRoot = configuredFigureRoot;

            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveFigureContracts();
            ResolveSharedMaterialActions();
            ResolveClarityContract();
            EnsureSeepVisual();
            RefreshHud();
        }

        private void Awake()
        {
            ResolveFigureContracts();
            ResolveSharedMaterialActions();
            ResolveClarityContract();
            EnsureSeepVisual();
        }

        private void OnEnable()
        {
            ResolveFigureContracts();
            ResolveSharedMaterialActions();
            ResolveClarityContract();
            EnsureSeepVisual();
        }

        private void Update()
        {
            RefreshRoles();
            RefreshClarity();
            RefreshSharedMaterialActions();

            if (painterRoleActive)
            {
                ResolvePainterCamera();
                RefreshSeamAim();
                ProcessSharedPainterMaterialInputs();
            }
            else
            {
                seamAimValid = false;
                aimedSeam = "None";
                aimedSeamReference = null;
            }

            RefreshWatercolorSeep();
            RefreshStainCrackAccess();
            RefreshHud();
        }

        public bool ForceOilSealA()
        {
            PrototypeUnderlayerSeam seam =
                underlayerDive != null
                    ? underlayerDive.SeamA
                    : null;

            return TryApplyOilSeal(
                seam,
                "Editor force A");
        }

        public bool ForceWatercolorSeepA()
        {
            PrototypeUnderlayerSeam seam =
                underlayerDive != null
                    ? underlayerDive.SeamA
                    : null;

            return TryApplyWatercolorSeep(
                seam,
                "Editor force A");
        }

        private void ResolveFigureContracts()
        {
            characterController =
                figureRoot != null
                    ? figureRoot.GetComponent<
                        CharacterController>()
                    : null;

            if (
                characterController == null &&
                figureRoot != null
            )
            {
                characterController =
                    figureRoot.GetComponentInChildren<
                        CharacterController>(
                            true);
            }
        }

        private void ResolveSharedMaterialActions()
        {
            sharedWatercolorAction = null;
            sharedOilAction = null;
            sharedMaterialInputResolved = false;

            if (traceWeaveCounterplay == null)
            {
                sharedInputContract =
                    "M51.2 counterplay missing";

                return;
            }

            Type runtimeType =
                traceWeaveCounterplay.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            FieldInfo watercolorField =
                runtimeType.GetField(
                    "watercolorAction",
                    flags);

            FieldInfo oilField =
                runtimeType.GetField(
                    "oilAction",
                    flags);

            if (
                watercolorField == null ||
                oilField == null
            )
            {
                sharedInputContract =
                    "M51.2 action fields unresolved";

                return;
            }

            try
            {
                sharedWatercolorAction =
                    watercolorField.GetValue(
                        traceWeaveCounterplay)
                    as InputAction;

                sharedOilAction =
                    oilField.GetValue(
                        traceWeaveCounterplay)
                    as InputAction;
            }
            catch (Exception)
            {
                sharedWatercolorAction = null;
                sharedOilAction = null;
            }

            sharedMaterialInputResolved =
                sharedWatercolorAction != null &&
                sharedOilAction != null;

            sharedInputContract =
                sharedMaterialInputResolved
                    ? "M51.2 F4/F10 actions read-only"
                    : "M51.2 shared actions unavailable";
        }

        private void RefreshSharedMaterialActions()
        {
            if (
                sharedWatercolorAction == null ||
                sharedOilAction == null
            )
            {
                ResolveSharedMaterialActions();
            }

            sharedMaterialInputResolved =
                sharedWatercolorAction != null &&
                sharedOilAction != null;
        }

        private void ResolveClarityContract()
        {
            clarityLevelProperty = null;

            if (clarityState == null)
            {
                return;
            }

            clarityLevelProperty =
                clarityState
                    .GetType()
                    .GetProperty(
                        "CurrentLevel",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);
        }

        private void RefreshRoles()
        {
            painterRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedPainterRole);

            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedFigureRole);
        }

        private void RefreshClarity()
        {
            clarityLevel = "Unknown";
            fullStain = false;

            if (
                clarityState == null ||
                clarityLevelProperty == null
            )
            {
                ResolveClarityContract();
                return;
            }

            try
            {
                object value =
                    clarityLevelProperty.GetValue(
                        clarityState);

                clarityLevel =
                    value != null
                        ? value.ToString()
                        : "Unknown";

                fullStain =
                    string.Equals(
                        clarityLevel,
                        "Stain",
                        StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                clarityLevel = "Unknown";
                fullStain = false;
            }
        }

        private void ResolvePainterCamera()
        {
            if (
                activeCamera != null &&
                activeCamera.isActiveAndEnabled &&
                activeCamera.gameObject.activeInHierarchy
            )
            {
                painterCameraResolved = true;
                return;
            }

            Camera main =
                Camera.main;

            if (
                main != null &&
                main.isActiveAndEnabled &&
                main.gameObject.activeInHierarchy
            )
            {
                activeCamera = main;
                painterCameraResolved = true;
                return;
            }

            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<
                    Camera>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            Camera best = null;
            int bestScore = int.MinValue;

            for (int index = 0;
                 index < cameras.Length;
                 index++)
            {
                Camera candidate =
                    cameras[index];

                if (
                    candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.isActiveAndEnabled
                )
                {
                    continue;
                }

                int score = 0;

                if (
                    candidate.name.IndexOf(
                        "Painter",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 300;
                }

                if (
                    candidate.CompareTag(
                        "MainCamera")
                )
                {
                    score += 100;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            activeCamera = best;
            painterCameraResolved =
                activeCamera != null;
        }

        private void RefreshSeamAim()
        {
            seamAimValid = false;
            aimedSeam = "None";
            aimedSeamReference = null;

            if (
                underlayerDive == null ||
                activeCamera == null ||
                Mouse.current == null
            )
            {
                return;
            }

            Vector2 pointer =
                Mouse.current.position.ReadValue();

            EvaluateSeamAim(
                underlayerDive.SeamA,
                pointer);

            EvaluateSeamAim(
                underlayerDive.SeamB,
                pointer);
        }

        private void EvaluateSeamAim(
            PrototypeUnderlayerSeam seam,
            Vector2 pointer)
        {
            if (
                seam == null ||
                !seam.IsConfigured ||
                seam.SurfaceAnchor == null
            )
            {
                return;
            }

            Vector3 screen =
                activeCamera.WorldToScreenPoint(
                    seam.SurfaceAnchor.position +
                    Vector3.up *
                    0.25f);

            if (screen.z <= 0f)
            {
                return;
            }

            float worldDistance =
                Vector3.Distance(
                    activeCamera.transform.position,
                    seam.SurfaceAnchor.position);

            if (
                worldDistance >
                seamMaximumCameraDistance
            )
            {
                return;
            }

            float pixelDistance =
                Vector2.Distance(
                    pointer,
                    new Vector2(
                        screen.x,
                        screen.y));

            if (
                pixelDistance >
                seamAimPixels
            )
            {
                return;
            }

            if (
                aimedSeamReference == null ||
                pixelDistance <
                    Vector2.Distance(
                        pointer,
                        new Vector2(
                            activeCamera.WorldToScreenPoint(
                                aimedSeamReference.SurfaceAnchor.position +
                                Vector3.up *
                                0.25f).x,
                            activeCamera.WorldToScreenPoint(
                                aimedSeamReference.SurfaceAnchor.position +
                                Vector3.up *
                                0.25f).y))
            )
            {
                aimedSeamReference = seam;
                aimedSeam =
                    seam.SeamId.ToString();
                seamAimValid = true;
            }
        }

        private void ProcessSharedPainterMaterialInputs()
        {
            if (
                !sharedMaterialInputResolved ||
                aimedSeamReference == null ||
                !seamAimValid
            )
            {
                return;
            }

            if (
                sharedWatercolorAction != null &&
                sharedWatercolorAction.enabled &&
                sharedWatercolorAction.WasPressedThisFrame()
            )
            {
                watercolorAttemptCount++;

                TryApplyWatercolorSeep(
                    aimedSeamReference,
                    "Shared M51.2 F4");

                return;
            }

            if (
                sharedOilAction != null &&
                sharedOilAction.enabled &&
                sharedOilAction.WasPressedThisFrame()
            )
            {
                oilAttemptCount++;

                TryApplyOilSeal(
                    aimedSeamReference,
                    "Shared M51.2 F10");
            }
        }

        private bool TryApplyOilSeal(
            PrototypeUnderlayerSeam seam,
            string source)
        {
            if (
                seam == null ||
                !seam.IsConfigured
            )
            {
                return Reject(
                    "Yağlı Boya: geçerli dikiş yok.");
            }

            if (seam.IsBraced)
            {
                return Reject(
                    $"Yağlı Boya reddedildi: Dikiş {seam.SeamId} Çerçeve Ankrajıyla açık tutuluyor.");
            }

            PrototypeUnderlayerSeam other =
                GetOtherSeam(
                    seam);

            if (
                underlayerDive != null &&
                underlayerDive.InUnderlayer &&
                other != null &&
                other.BlocksTraversal
            )
            {
                return Reject(
                    "Yağlı Boya reddedildi: Alt Katmandaki Figure için son güvenli çıkış kapatılamaz.");
            }

            seam.SealForSeconds(
                oilSealSeconds,
                source);

            oilSuccessCount++;

            lastAction =
                $"Yağlı Boya Dikiş {seam.SeamId}'i {oilSealSeconds:F1} sn mühürledi.";

            Debug.Log(
                "[M52.1 Oil Paint Seam Seal]\n" +
                $"Source={source}\n" +
                $"Seam={seam.SeamId}\n" +
                $"SealSeconds={oilSealSeconds:F2}\n" +
                "PreventsLastSafeExitSeal=True\n" +
                "BlocksPaletteKnifeEntryExit=True\n" +
                "BlocksStainCrackAccess=True\n" +
                "AddsNewPainterGameplayInputAction=False",
                this);

            return true;
        }

        private bool TryApplyWatercolorSeep(
            PrototypeUnderlayerSeam seam,
            string source)
        {
            if (
                seam == null ||
                !seam.IsConfigured ||
                seam.UnderlayerAnchor == null
            )
            {
                return Reject(
                    "Suluboya: geçerli dikiş / Alt Katman node'u yok.");
            }

            activeSeepSeam = seam;
            seepUntil =
                Time.unscaledTime +
                seepSeconds;

            watercolorSuccessCount++;

            lastAction =
                $"Suluboya Dikiş {seam.SeamId}'den Alt Katmana sızıyor.";

            Debug.Log(
                "[M52.1 Watercolor Underlayer Seep]\n" +
                $"Source={source}\n" +
                $"Seam={seam.SeamId}\n" +
                $"SeepSeconds={seepSeconds:F2}\n" +
                $"SeepRadius={seepRadius:F2}\n" +
                $"FlowSpeed={seepFlowSpeed:F2}\n" +
                "WatercolorRawDamageEnabled=False\n" +
                "UsesCharacterControllerMove=True",
                this);

            return true;
        }

        private void RefreshWatercolorSeep()
        {
            watercolorSeepActive =
                activeSeepSeam != null &&
                Time.unscaledTime <
                    seepUntil;

            seepRemainingSeconds =
                watercolorSeepActive
                    ? Mathf.Max(
                        0f,
                        seepUntil -
                        Time.unscaledTime)
                    : 0f;

            if (!watercolorSeepActive)
            {
                activeSeepSeam = null;
                HideSeepVisual();
                return;
            }

            UpdateSeepVisual(
                activeSeepSeam);

            if (
                underlayerDive == null ||
                !underlayerDive.InUnderlayer ||
                figureRoot == null ||
                characterController == null ||
                !characterController.enabled ||
                activeSeepSeam.UnderlayerAnchor == null
            )
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    figureRoot.position,
                    activeSeepSeam
                        .UnderlayerAnchor
                        .position);

            if (
                distance >
                seepRadius
            )
            {
                return;
            }

            Vector3 flow =
                GetFlowDirection(
                    activeSeepSeam);

            Vector3 delta =
                flow *
                seepFlowSpeed *
                Mathf.Max(
                    0f,
                    Time.deltaTime);

            characterController.Move(
                delta);

            seepAppliedDistance +=
                delta.magnitude;

            seepContactFrameCount++;

            lastAction =
                "Suluboya sızıntısı Figure'ı Alt Katman koridorunda itiyor.";
        }

        private void RefreshStainCrackAccess()
        {
            stainCrackReady = false;

            if (
                !figureRoleActive ||
                !fullStain ||
                underlayerDive == null ||
                figureRoot == null
            )
            {
                stainCandidate = null;
                stainCrackProgress = 0f;
                return;
            }

            PrototypeUnderlayerSeam candidate =
                FindNearestUsableStainSeam(
                    out float distance);

            if (
                candidate == null ||
                distance >
                    stainCrackRadius
            )
            {
                stainCandidate = null;
                stainCrackProgress = 0f;
                return;
            }

            if (
                candidate != stainCandidate
            )
            {
                stainCandidate = candidate;
                stainCrackProgress = 0f;
            }

            stainCrackReady = true;

            stainCrackProgress +=
                Mathf.Max(
                    0f,
                    Time.deltaTime) /
                Mathf.Max(
                    0.05f,
                    stainCrackHoldSeconds);

            stainCrackProgress =
                Mathf.Clamp01(
                    stainCrackProgress);

            if (
                stainCrackProgress <
                1f
            )
            {
                return;
            }

            bool succeeded;

            if (underlayerDive.InUnderlayer)
            {
                succeeded =
                    underlayerDive.TryExitViaExternalSeam(
                        candidate,
                        "M52.1 Full Stain crack seep");

                if (succeeded)
                {
                    stainExitCount++;
                    lastAction =
                        $"Full Stain Dikiş {candidate.SeamId}'den yüzeye sızdı.";
                }
            }
            else
            {
                succeeded =
                    underlayerDive.TryEnterViaExternalSeam(
                        candidate,
                        "M52.1 Full Stain crack seep");

                if (succeeded)
                {
                    stainEntryCount++;
                    lastAction =
                        $"Full Stain Dikiş {candidate.SeamId}'den Alt Katmana sızdı.";
                }
            }

            stainCrackProgress = 0f;
            stainCandidate = null;
        }

        private PrototypeUnderlayerSeam
            FindNearestUsableStainSeam(
                out float bestDistance)
        {
            bestDistance =
                float.PositiveInfinity;

            PrototypeUnderlayerSeam best = null;

            EvaluateStainSeam(
                underlayerDive.SeamA,
                ref best,
                ref bestDistance);

            EvaluateStainSeam(
                underlayerDive.SeamB,
                ref best,
                ref bestDistance);

            return best;
        }

        private void EvaluateStainSeam(
            PrototypeUnderlayerSeam seam,
            ref PrototypeUnderlayerSeam best,
            ref float bestDistance)
        {
            if (
                seam == null ||
                !seam.IsConfigured ||
                seam.BlocksTraversal
            )
            {
                return;
            }

            Transform anchor =
                underlayerDive.InUnderlayer
                    ? seam.UnderlayerAnchor
                    : seam.SurfaceAnchor;

            if (anchor == null)
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    figureRoot.position,
                    anchor.position);

            if (distance < bestDistance)
            {
                best = seam;
                bestDistance = distance;
            }
        }

        private PrototypeUnderlayerSeam
            GetOtherSeam(
                PrototypeUnderlayerSeam seam)
        {
            if (
                underlayerDive == null ||
                seam == null
            )
            {
                return null;
            }

            return
                seam ==
                    underlayerDive.SeamA
                    ? underlayerDive.SeamB
                    : underlayerDive.SeamA;
        }

        private Vector3 GetFlowDirection(
            PrototypeUnderlayerSeam seam)
        {
            PrototypeUnderlayerSeam other =
                GetOtherSeam(
                    seam);

            if (
                seam == null ||
                seam.UnderlayerAnchor == null ||
                other == null ||
                other.UnderlayerAnchor == null
            )
            {
                return Vector3.forward;
            }

            Vector3 flow =
                Vector3.ProjectOnPlane(
                    other.UnderlayerAnchor.position -
                    seam.UnderlayerAnchor.position,
                    Vector3.up);

            if (
                flow.sqrMagnitude <=
                0.0001f
            )
            {
                return Vector3.forward;
            }

            return flow.normalized;
        }

        private void EnsureSeepVisual()
        {
            if (
                seepRing != null &&
                seepFlow != null
            )
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                seepMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M52_1_WatercolorSeep_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject ringObject =
                new GameObject(
                    "M52_1_WatercolorSeepRing");

            ringObject.transform.SetParent(
                transform,
                false);

            seepRing =
                ringObject.AddComponent<
                    LineRenderer>();

            ConfigureSeepLine(
                seepRing,
                0.052f);

            seepRing.loop = true;
            seepRing.positionCount = 28;

            GameObject flowObject =
                new GameObject(
                    "M52_1_WatercolorFlow");

            flowObject.transform.SetParent(
                transform,
                false);

            seepFlow =
                flowObject.AddComponent<
                    LineRenderer>();

            ConfigureSeepLine(
                seepFlow,
                0.075f);

            seepFlow.loop = false;
            seepFlow.positionCount = 3;

            HideSeepVisual();
        }

        private void ConfigureSeepLine(
            LineRenderer line,
            float width)
        {
            line.useWorldSpace = true;
            line.alignment =
                LineAlignment.View;

            line.textureMode =
                LineTextureMode.Stretch;

            line.numCapVertices = 3;
            line.numCornerVertices = 3;

            line.shadowCastingMode =
                ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.widthMultiplier = width;

            line.sharedMaterial =
                seepMaterial;

            Color color =
                new Color(
                    0.26f,
                    0.52f,
                    0.92f,
                    0.88f);

            line.startColor = color;
            line.endColor = color;
        }

        private void UpdateSeepVisual(
            PrototypeUnderlayerSeam seam)
        {
            EnsureSeepVisual();

            if (
                seam == null ||
                seam.UnderlayerAnchor == null ||
                seepRing == null ||
                seepFlow == null
            )
            {
                return;
            }

            Vector3 center =
                seam.UnderlayerAnchor.position +
                Vector3.up *
                0.075f;

            int count =
                seepRing.positionCount;

            for (int index = 0;
                 index < count;
                 index++)
            {
                float angle =
                    index /
                    (float)count *
                    Mathf.PI *
                    2f;

                Vector3 offset =
                    new Vector3(
                        Mathf.Cos(angle),
                        0f,
                        Mathf.Sin(angle)) *
                    seepRadius;

                seepRing.SetPosition(
                    index,
                    center +
                    offset);
            }

            Vector3 flow =
                GetFlowDirection(
                    seam);

            seepFlow.SetPosition(
                0,
                center);

            seepFlow.SetPosition(
                1,
                center +
                flow *
                0.75f +
                Vector3.up *
                0.05f);

            seepFlow.SetPosition(
                2,
                center +
                flow *
                1.45f);

            seepRing.enabled = true;
            seepFlow.enabled = true;
        }

        private void HideSeepVisual()
        {
            if (seepRing != null)
            {
                seepRing.enabled = false;
            }

            if (seepFlow != null)
            {
                seepFlow.enabled = false;
            }
        }

        private bool Reject(
            string reason)
        {
            rejectedCounterplayCount++;
            lastAction = reason;
            return false;
        }

        private void RefreshHud()
        {
            bool painterRelevant =
                painterRoleActive &&
                (seamAimValid ||
                 watercolorSeepActive ||
                 (underlayerDive != null && underlayerDive.InUnderlayer));

            bool figureRelevant =
                figureRoleActive &&
                (watercolorSeepActive ||
                 seepContactFrameCount > 0 ||
                 stainCrackReady ||
                 stainCrackProgress > 0.01f ||
                 (underlayerDive != null && underlayerDive.InUnderlayer));

            bool visible =
                PrototypeRuntimeHudVisibilityUtility.Hold(
                    painterRelevant || figureRelevant,
                    ref hudVisibleUntilUnscaled,
                    contextualHudHoldSeconds);

            PrototypeRuntimeHudVisibilityUtility.ApplyCanvasGroup(
                statusGroup,
                visible);

            if (!visible)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    painterRoleActive
                        ? "ALT KATMAN • RESSAM KARŞI-OYUNU"
                        : fullStain
                            ? "ALT KATMAN • STAIN ÇATLAK SIZMASI"
                            : "ALT KATMAN • TEHDİT / SIZINTI";
            }

            if (stateText != null)
            {
                if (painterRoleActive)
                {
                    string sealA =
                        underlayerDive != null &&
                        underlayerDive.SeamA != null &&
                        underlayerDive.SeamA.IsBraced
                            ? $"A ANKRAJ {underlayerDive.SeamA.BraceRemainingSeconds:F1}s"
                            : underlayerDive != null &&
                              underlayerDive.SeamA != null &&
                              underlayerDive.SeamA.IsSealed
                                ? $"A MÜHÜRLÜ {underlayerDive.SeamA.SealedRemainingSeconds:F1}s"
                                : "A AÇIK";

                    string sealB =
                        underlayerDive != null &&
                        underlayerDive.SeamB != null &&
                        underlayerDive.SeamB.IsBraced
                            ? $"B ANKRAJ {underlayerDive.SeamB.BraceRemainingSeconds:F1}s"
                            : underlayerDive != null &&
                              underlayerDive.SeamB != null &&
                              underlayerDive.SeamB.IsSealed
                                ? $"B MÜHÜRLÜ {underlayerDive.SeamB.SealedRemainingSeconds:F1}s"
                                : "B AÇIK";

                    stateText.text =
                        $"HEDEF {(seamAimValid ? "DİKİŞ " + aimedSeam : "YOK")}   " +
                        $"INPUT {(sharedMaterialInputResolved ? "ORTAK" : "YOK")}\n" +
                        $"{sealA}   {sealB}   " +
                        $"SIZINTI {(watercolorSeepActive ? seepRemainingSeconds.ToString("F1") + "s" : "YOK")}\n" +
                        lastAction;
                }
                else
                {
                    stateText.text =
                        $"NETLİK {clarityLevel}   " +
                        $"ÇATLAK {(stainCrackReady ? "HAZIR" : "ARA")}   " +
                        $"İLERLEME %{stainCrackProgress * 100f:F0}\n" +
                        $"SIZINTI {(watercolorSeepActive ? seepRemainingSeconds.ToString("F1") + "s" : "YOK")}   " +
                        $"TEMAS {seepContactFrameCount}\n" +
                        lastAction;
                }
            }

            if (controlsText != null)
            {
                controlsText.text =
                    painterRoleActive
                        ? "DİKİŞE İMLEÇ • F4 SULUBOYA SIZDIR   F10 YAĞLI BOYA MÜHÜRLE\n" +
                          "M51.2 İLE AYNI INPUT SAHİBİ • YENİ TUŞ YOK"
                        : fullStain
                            ? "FULL STAIN • DİKİŞ/ÇATLAĞA TEMAS ET VE 0.38 sn KAL\n" +
                              "YENİ INPUT YOK • MÜHÜRLÜ DİKİŞTEN SIZAMAZ"
                            : "SULUBOYA SIZINTISINDAN KAÇ • MÜREKKEP KÖKÜ TEMASI GÜVENLİ DİKİŞE ATAR";
            }
        }

        private void OnDisable()
        {
            HideSeepVisual();
        }

        private void OnDestroy()
        {
            if (seepMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    seepMaterial);
            }
            else
            {
                DestroyImmediate(
                    seepMaterial);
            }

            seepMaterial = null;
        }
    }
}
