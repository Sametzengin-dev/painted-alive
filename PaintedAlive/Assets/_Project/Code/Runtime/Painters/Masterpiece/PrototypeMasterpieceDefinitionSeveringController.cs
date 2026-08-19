using System;
using System.Collections.Generic;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(-220)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceDefinitionSeveringController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deployment;

        [SerializeField] private Transform figureRoot;
        [SerializeField] private Camera figureCamera;
        [SerializeField] private MonoBehaviour paletteKnifeController;
        [SerializeField] private MonoBehaviour figureClarityState;

        [Header("Cut Contract")]
        [SerializeField, Min(0.2f)]
        private float cutHoldSeconds = 1.15f;

        [SerializeField, Min(0.5f)]
        private float maximumCutDistance = 2.75f;

        [SerializeField, Range(0.02f, 0.35f)]
        private float viewportAimTolerance = 0.115f;

        [SerializeField]
        private LayerMask lineOfSightMask = ~0;

        [SerializeField, Min(0f)]
        private float lineOfSightSkin = 0.08f;

        [Header("Detached Visual")]
        [SerializeField, Min(0f)]
        private float detachOutwardSpeed = 1.05f;

        [SerializeField, Min(0f)]
        private float detachUpwardSpeed = 1.35f;

        [SerializeField]
        private Vector3 detachAngularVelocity =
            new Vector3(
                18f,
                86f,
                -42f);

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeMasterpieceWorldInstance activeInstance;

        [SerializeField]
        private PrototypeMasterpieceWorldPart eyePart;

        [SerializeField] private Transform eyePartRoot;
        [SerializeField]
        private PrototypeSeveredDefinitionToken detachedToken;

        [SerializeField]
        private PrototypeDefinitionSeveringState state =
            PrototypeDefinitionSeveringState.WaitingForWorld;

        [SerializeField] private bool inputActionActive;
        [SerializeField] private bool figureRoleActive;
        [SerializeField] private bool paletteKnifeActive;
        [SerializeField] private bool primaryToolAllowed;
        [SerializeField] private bool eyeCapabilityActive;
        [SerializeField] private bool eyeAimed;
        [SerializeField] private bool lineOfSightClear;
        [SerializeField] private bool eyeSevered;
        [SerializeField] private bool paletteDebugSuppressed;
        [SerializeField] private int instanceBindCount;
        [SerializeField] private int cutStartCount;
        [SerializeField] private int cutCancelCount;
        [SerializeField] private int severCount;
        [SerializeField] private int restoreCount;
        [SerializeField] private int roleRejectedCount;
        [SerializeField] private int toolRejectedCount;
        [SerializeField] private int occlusionRejectCount;
        [SerializeField] private int aimRejectCount;
        [SerializeField] private int detachedTokenCount;
        [SerializeField] private float cutProgress;
        [SerializeField] private float eyeDistance;
        [SerializeField] private float viewportAimOffset;
        [SerializeField] private Vector3 eyeWorldCenter;
        [SerializeField] private Vector3 headRigCutAnchor;
        [SerializeField] private bool headRigAnchorResolved;
        [SerializeField] private bool activeFigureCameraResolved;
        [SerializeField] private bool cutInputPressed;
        [SerializeField] private int headRendererCount;
        [SerializeField] private int cutAnchorResolveCount;
        [SerializeField] private int runtimeCameraResolveCount;
        [SerializeField] private string paletteKnifeActivityContract = "Legacy enabled-state";
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private string lastAction =
            "Dünya Baş Yapıtı bekleniyor.";

        private readonly List<LineRenderer> sourceEyeRenderers =
            new List<LineRenderer>();

        private readonly List<LineRenderer> policyHeadRenderers =
            new List<LineRenderer>();

        private readonly RaycastHit[] lineOfSightHits =
            new RaycastHit[32];

        private InputAction cutAction;
        private Material highlightMaterial;
        private LineRenderer highlightRing;

        private PropertyInfo canUsePrimaryToolProperty;
        private FieldInfo paletteDebugField;
        private bool paletteDebugOriginalValue;
        private int boundSnapshotHash;

        public PrototypeDefinitionSeveringState State =>
            state;
        public bool InputActionActive =>
            inputActionActive;
        public bool FigureRoleActive =>
            figureRoleActive;
        public bool PaletteKnifeActive =>
            paletteKnifeActive;
        public bool PrimaryToolAllowed =>
            primaryToolAllowed;
        public bool EyeCapabilityActive =>
            eyeCapabilityActive;
        public bool EyeAimed => eyeAimed;
        public bool LineOfSightClear =>
            lineOfSightClear;
        public bool EyeSevered => eyeSevered;
        public int InstanceBindCount =>
            instanceBindCount;
        public int CutStartCount =>
            cutStartCount;
        public int CutCancelCount =>
            cutCancelCount;
        public int SeverCount =>
            severCount;
        public int RestoreCount =>
            restoreCount;
        public int RoleRejectedCount =>
            roleRejectedCount;
        public int ToolRejectedCount =>
            toolRejectedCount;
        public int OcclusionRejectCount =>
            occlusionRejectCount;
        public int AimRejectCount =>
            aimRejectCount;
        public int DetachedTokenCount =>
            detachedTokenCount;
        public float CutProgress =>
            cutProgress;
        public float EyeDistance =>
            eyeDistance;
        public float ViewportAimOffset =>
            viewportAimOffset;
        public Vector3 HeadRigCutAnchor =>
            headRigCutAnchor;
        public bool HeadRigAnchorResolved =>
            headRigAnchorResolved;
        public bool ActiveFigureCameraResolved =>
            activeFigureCameraResolved;
        public bool CutInputPressed =>
            cutInputPressed;
        public int HeadRendererCount =>
            headRendererCount;
        public int CutAnchorResolveCount =>
            cutAnchorResolveCount;
        public int RuntimeCameraResolveCount =>
            runtimeCameraResolveCount;
        public string PaletteKnifeActivityContract =>
            paletteKnifeActivityContract;
        public string ResolvedRole =>
            resolvedRole;
        public string LastAction =>
            lastAction;
        public PrototypeSeveredDefinitionToken DetachedToken =>
            detachedToken;

        public bool CounterCompositionEnabled => true;
        public bool PaletteKnifeRequired => true;
        public bool CapabilityWriteEnabled => true;
        public bool PerceptionCapabilityTargeted => true;
        public bool CoreSeveringEnabled => false;
        public bool GameplayPickupEnabled => false;
        public bool DefinitionUseEnabled => false;
        public bool InventoryEnabled => false;
        public bool DamageSystemEnabled => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeMasterpieceWorldDeploymentController
                configuredDeployment,
            Transform configuredFigureRoot,
            Camera configuredFigureCamera,
            MonoBehaviour configuredPaletteKnifeController,
            MonoBehaviour configuredFigureClarityState,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            deployment =
                configuredDeployment;

            figureRoot =
                configuredFigureRoot;

            figureCamera =
                configuredFigureCamera;

            paletteKnifeController =
                configuredPaletteKnifeController;

            figureClarityState =
                configuredFigureClarityState;

            statusGroup =
                configuredStatusGroup;

            titleText =
                configuredTitleText;

            stateText =
                configuredStateText;

            controlsText =
                configuredControlsText;

            ResolveReflectionContracts();
            EnsureInputAction();
            EnsureHighlight();
            ResolveInstance();
            RefreshHud();
        }

        private void Awake()
        {
            ResolveReflectionContracts();
            EnsureInputAction();
            EnsureHighlight();
            ResolveInstance();
            RefreshHud();
        }

        private void OnEnable()
        {
            ResolveReflectionContracts();
            EnsureInputAction();
            EnsureHighlight();
            ResolveInstance();
            RefreshHud();
        }

        private void Update()
        {
            ResolveInstance();
            RefreshAuthority();
            RefreshFigureCamera();
            RefreshEyeTarget();
            RefreshInputActionState();
            TickCutting();
            EnforceSeveredVisualState();
            UpdateHighlight();
            RefreshHud();
        }

        public bool ForceSeverForPrototype()
        {
            RefreshAuthority();
            ResolveInstance();

            if (!figureRoleActive)
            {
                roleRejectedCount++;
                lastAction =
                    "Doğrudan kesme testi yalnız Figure rolünde çalışır.";

                return false;
            }

            if (!paletteKnifeActive)
            {
                toolRejectedCount++;
                lastAction =
                    "Palet Bıçağı aktif değil.";

                return false;
            }

            return TryCompleteSever(
                "Editor force sever");
        }

        public bool RestoreEyeForPrototype()
        {
            if (
                activeInstance == null ||
                eyePart == null ||
                eyePartRoot == null
            )
            {
                lastAction =
                    "Restore edilecek aktif Baş tanımı yok.";

                return false;
            }

            eyePart.SetCapabilityActive(
                true,
                "M50.0 prototype restore");

            for (int index = 0;
                 index < sourceEyeRenderers.Count;
                 index++)
            {
                LineRenderer renderer =
                    sourceEyeRenderers[index];

                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            for (int index = 0;
                 index < policyHeadRenderers.Count;
                 index++)
            {
                LineRenderer renderer =
                    policyHeadRenderers[index];

                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            if (eyePart.ProxyCollider != null)
            {
                eyePart.ProxyCollider.enabled = true;
            }

            DestroyDetachedToken();

            eyeSevered = false;
            eyeCapabilityActive = true;
            cutProgress = 0f;
            restoreCount++;
            state =
                PrototypeDefinitionSeveringState.Ready;

            lastAction =
                "Baş tanımı prototip testi için geri bağlandı.";

            return true;
        }

        private void ResolveReflectionContracts()
        {
            if (figureClarityState != null)
            {
                canUsePrimaryToolProperty =
                    figureClarityState
                        .GetType()
                        .GetProperty(
                            "CanUsePrimaryTool",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);
            }
            else
            {
                canUsePrimaryToolProperty = null;
            }

            if (paletteKnifeController != null)
            {
                paletteDebugField =
                    paletteKnifeController
                        .GetType()
                        .GetField(
                            "logDebugMessages",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);
            }
            else
            {
                paletteDebugField = null;
            }
        }

        private void EnsureInputAction()
        {
            if (cutAction != null)
            {
                return;
            }

            cutAction =
                new InputAction(
                    "M50.0 Sever Eye Definition",
                    InputActionType.Button,
                    "<Keyboard>/e");
        }

        private void RefreshInputActionState()
        {
            bool shouldEnable =
                figureRoleActive &&
                paletteKnifeActive &&
                primaryToolAllowed &&
                activeInstance != null &&
                !eyeSevered;

            if (shouldEnable)
            {
                if (!cutAction.enabled)
                {
                    cutAction.Enable();
                }
            }
            else
            {
                if (cutAction.enabled)
                {
                    cutAction.Disable();
                }

                if (cutProgress > 0f)
                {
                    CancelCut(
                        "Rol, araç veya hedef otoritesi kesmeyi durdurdu.");
                }
            }

            inputActionActive =
                cutAction != null &&
                cutAction.enabled;
        }

        private void RefreshAuthority()
        {
            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedRole);

            paletteKnifeActive =
                ResolvePaletteKnifeActive();

            primaryToolAllowed =
                ReadPrimaryToolAllowed();
        }

        private bool ResolvePaletteKnifeActive()
        {
            if (
                paletteKnifeController == null ||
                !paletteKnifeController.gameObject.activeInHierarchy
            )
            {
                paletteKnifeActivityContract =
                    "Controller missing/inactive";

                return false;
            }

            Type runtimeType =
                paletteKnifeController.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            string[] candidateNames =
            {
                "IsSelected",
                "Selected",
                "IsEquipped",
                "Equipped",
                "IsActive",
                "Active",
                "ToolActive",
                "IsToolActive",
                "IsCurrentTool"
            };

            for (int index = 0;
                 index < candidateNames.Length;
                 index++)
            {
                string memberName =
                    candidateNames[index];

                PropertyInfo property =
                    runtimeType.GetProperty(
                        memberName,
                        flags);

                if (
                    property != null &&
                    property.PropertyType ==
                        typeof(bool)
                )
                {
                    try
                    {
                        object value =
                            property.GetValue(
                                paletteKnifeController);

                        if (value is bool active)
                        {
                            paletteKnifeActivityContract =
                                $"Property:{memberName}";

                            return active;
                        }
                    }
                    catch (Exception)
                    {
                        // Try the next compatible contract.
                    }
                }

                FieldInfo field =
                    runtimeType.GetField(
                        memberName,
                        flags);

                if (
                    field != null &&
                    field.FieldType ==
                        typeof(bool)
                )
                {
                    try
                    {
                        object value =
                            field.GetValue(
                                paletteKnifeController);

                        if (value is bool active)
                        {
                            paletteKnifeActivityContract =
                                $"Field:{memberName}";

                            return active;
                        }
                    }
                    catch (Exception)
                    {
                        // Try the next compatible contract.
                    }
                }
            }

            paletteKnifeActivityContract =
                "Fallback:MonoBehaviour.enabled";

            return paletteKnifeController.enabled;
        }

        private void RefreshFigureCamera()
        {
            if (
                figureCamera != null &&
                figureCamera.isActiveAndEnabled &&
                figureCamera.gameObject.activeInHierarchy
            )
            {
                activeFigureCameraResolved = true;
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
                if (figureCamera != main)
                {
                    runtimeCameraResolveCount++;
                }

                figureCamera = main;
                activeFigureCameraResolved = true;
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
                    figureRoot != null &&
                    (
                        candidate.transform.IsChildOf(
                            figureRoot) ||
                        figureRoot.IsChildOf(
                            candidate.transform)
                    )
                )
                {
                    score += 500;
                }

                if (
                    candidate.name.IndexOf(
                        "Figure",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 150;
                }

                if (candidate.CompareTag("MainCamera"))
                {
                    score += 100;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            if (best != null)
            {
                if (figureCamera != best)
                {
                    runtimeCameraResolveCount++;
                }

                figureCamera = best;
                activeFigureCameraResolved = true;
            }
            else
            {
                activeFigureCameraResolved = false;
            }
        }

        private bool ReadPrimaryToolAllowed()
        {
            if (
                figureClarityState == null ||
                canUsePrimaryToolProperty == null
            )
            {
                return true;
            }

            try
            {
                object value =
                    canUsePrimaryToolProperty.GetValue(
                        figureClarityState);

                return value is bool allowed
                    ? allowed
                    : true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private void ResolveInstance()
        {
            PrototypeMasterpieceWorldInstance candidate =
                deployment != null
                    ? deployment.ActiveInstance
                    : null;

            if (candidate == activeInstance)
            {
                return;
            }

            ReleaseCurrentInstance();
            activeInstance = candidate;

            if (activeInstance == null)
            {
                state =
                    PrototypeDefinitionSeveringState.WaitingForWorld;

                lastAction =
                    "Dünya Baş Yapıtı bekleniyor.";

                return;
            }

            if (
                !activeInstance.TryGetWorldPart(
                    PrototypeMasterpiecePartKind.Head,
                    out eyePart) ||
                eyePart == null ||
                !activeInstance.TryGetPartRoot(
                    PrototypeMasterpiecePartKind.Head,
                    out eyePartRoot) ||
                eyePartRoot == null
            )
            {
                state =
                    PrototypeDefinitionSeveringState.EyeUnavailable;

                lastAction =
                    "Head / Perception world part bulunamadı.";

                return;
            }

            boundSnapshotHash =
                activeInstance.SnapshotHash;

            sourceEyeRenderers.Clear();
            sourceEyeRenderers.AddRange(
                eyePartRoot.GetComponentsInChildren<
                    LineRenderer>(
                        true));

            RefreshPolicyHeadRenderers();
            ResolveHeadRigCutAnchor();

            eyeSevered =
                !eyePart.CapabilityActive;

            eyeCapabilityActive =
                eyePart.CapabilityActive;

            if (eyeSevered)
            {
                state =
                    PrototypeDefinitionSeveringState.Severed;

                lastAction =
                    "Bu deployment Baş/Algı capability kapalı olarak bağlandı.";
            }
            else
            {
                state =
                    PrototypeDefinitionSeveringState.Ready;

                lastAction =
                    "Baş tanımı kesilmeye hazır.";
            }

            instanceBindCount++;
        }

        private void RefreshPolicyHeadRenderers()
        {
            policyHeadRenderers.Clear();

            if (activeInstance == null)
            {
                return;
            }

            LineRenderer[] renderers =
                activeInstance.GetComponentsInChildren<
                    LineRenderer>(
                        true);

            for (int index = 0;
                 index < renderers.Length;
                 index++)
            {
                LineRenderer candidate =
                    renderers[index];

                if (
                    candidate == null ||
                    IsInside(
                        candidate.transform,
                        eyePartRoot)
                )
                {
                    continue;
                }

                string objectName =
                    candidate.gameObject.name;

                bool isHeadShape =
                    objectName.IndexOf(
                        "Shape_Head",
                        StringComparison.OrdinalIgnoreCase) >= 0;

                bool isHeadBone =
                    objectName.IndexOf(
                        "Bone_0",
                        StringComparison.OrdinalIgnoreCase) >= 0;

                if (
                    isHeadShape ||
                    isHeadBone
                )
                {
                    policyHeadRenderers.Add(
                        candidate);
                }
            }
        }

        private void ResolveHeadRigCutAnchor()
        {
            cutAnchorResolveCount++;
            headRigAnchorResolved = false;
            headRendererCount = 0;

            if (
                activeInstance == null ||
                eyePartRoot == null
            )
            {
                return;
            }

            Vector3 coreCenter =
                activeInstance.transform.position;

            if (
                activeInstance.TryGetWorldPart(
                    PrototypeMasterpiecePartKind.Core,
                    out PrototypeMasterpieceWorldPart corePart) &&
                corePart != null
            )
            {
                coreCenter =
                    corePart.ProxyCollider != null
                        ? corePart.ProxyCollider.bounds.center
                        : corePart.transform.position;
            }

            bool foundPoint = false;
            float bestSquaredDistance =
                float.PositiveInfinity;

            Vector3 bestPoint =
                eyePartRoot.position;

            EvaluateRendererPoints(
                sourceEyeRenderers,
                coreCenter,
                ref foundPoint,
                ref bestSquaredDistance,
                ref bestPoint);

            EvaluateRendererPoints(
                policyHeadRenderers,
                coreCenter,
                ref foundPoint,
                ref bestSquaredDistance,
                ref bestPoint);

            headRendererCount =
                sourceEyeRenderers.Count +
                policyHeadRenderers.Count;

            if (foundPoint)
            {
                headRigCutAnchor = bestPoint;
                headRigAnchorResolved = true;
                return;
            }

            if (
                eyePart != null &&
                eyePart.ProxyCollider != null
            )
            {
                headRigCutAnchor =
                    eyePart.ProxyCollider.ClosestPoint(
                        coreCenter);

                headRigAnchorResolved = true;
                return;
            }

            headRigCutAnchor =
                eyePartRoot.position;

            headRigAnchorResolved = true;
        }

        private static void EvaluateRendererPoints(
            List<LineRenderer> renderers,
            Vector3 referencePoint,
            ref bool foundPoint,
            ref float bestSquaredDistance,
            ref Vector3 bestPoint)
        {
            for (int rendererIndex = 0;
                 rendererIndex < renderers.Count;
                 rendererIndex++)
            {
                LineRenderer renderer =
                    renderers[rendererIndex];

                if (
                    renderer == null ||
                    renderer.positionCount <= 0
                )
                {
                    continue;
                }

                int pointCount =
                    renderer.positionCount;

                for (int pointIndex = 0;
                     pointIndex < pointCount;
                     pointIndex++)
                {
                    Vector3 rawPoint =
                        renderer.GetPosition(
                            pointIndex);

                    Vector3 worldPoint =
                        renderer.useWorldSpace
                            ? rawPoint
                            : renderer.transform.TransformPoint(
                                rawPoint);

                    float squaredDistance =
                        (
                            worldPoint -
                            referencePoint
                        ).sqrMagnitude;

                    if (
                        !foundPoint ||
                        squaredDistance <
                            bestSquaredDistance
                    )
                    {
                        foundPoint = true;
                        bestSquaredDistance =
                            squaredDistance;

                        bestPoint = worldPoint;
                    }
                }
            }
        }

        private void RefreshEyeTarget()
        {
            eyeAimed = false;
            lineOfSightClear = false;
            eyeDistance = -1f;
            viewportAimOffset = 1f;

            if (
                activeInstance == null ||
                eyePart == null ||
                eyePartRoot == null
            )
            {
                state =
                    PrototypeDefinitionSeveringState.EyeUnavailable;

                return;
            }

            eyeCapabilityActive =
                eyePart.CapabilityActive;

            if (
                eyeSevered ||
                !eyeCapabilityActive
            )
            {
                state =
                    PrototypeDefinitionSeveringState.Severed;

                return;
            }

            if (!figureRoleActive)
            {
                state =
                    PrototypeDefinitionSeveringState.PainterRole;

                return;
            }

            if (
                !paletteKnifeActive ||
                !primaryToolAllowed
            )
            {
                state =
                    PrototypeDefinitionSeveringState.FigureToolUnavailable;

                return;
            }

            if (
                figureCamera == null ||
                !figureCamera.isActiveAndEnabled ||
                figureRoot == null ||
                !headRigAnchorResolved
            )
            {
                state =
                    PrototypeDefinitionSeveringState.EyeUnavailable;

                return;
            }

            ResolveHeadRigCutAnchor();

            eyeWorldCenter =
                headRigCutAnchor;

            eyeDistance =
                Vector3.Distance(
                    figureRoot.position,
                    eyeWorldCenter);

            float effectiveCutDistance =
                Mathf.Max(
                    maximumCutDistance,
                    3.15f);

            if (eyeDistance >
                effectiveCutDistance)
            {
                state =
                    PrototypeDefinitionSeveringState.OutOfRange;

                return;
            }

            Vector3 viewport =
                figureCamera.WorldToViewportPoint(
                    eyeWorldCenter);

            if (viewport.z <= 0f)
            {
                state =
                    PrototypeDefinitionSeveringState.AimRequired;

                return;
            }

            viewportAimOffset =
                Vector2.Distance(
                    new Vector2(
                        viewport.x,
                        viewport.y),
                    new Vector2(
                        0.5f,
                        0.5f));

            float effectiveAimTolerance =
                Mathf.Max(
                    viewportAimTolerance,
                    0.20f);

            eyeAimed =
                viewportAimOffset <=
                effectiveAimTolerance;

            if (!eyeAimed)
            {
                state =
                    PrototypeDefinitionSeveringState.AimRequired;

                aimRejectCount++;
                return;
            }

            lineOfSightClear =
                HasClearLineOfSight(
                    eyeWorldCenter);

            if (!lineOfSightClear)
            {
                state =
                    PrototypeDefinitionSeveringState.Occluded;

                occlusionRejectCount++;
                return;
            }

            state =
                cutProgress > 0f
                    ? PrototypeDefinitionSeveringState.Cutting
                    : PrototypeDefinitionSeveringState.Ready;
        }

        private bool HasClearLineOfSight(
            Vector3 target)
        {
            if (figureCamera == null)
            {
                return false;
            }

            Vector3 origin =
                figureCamera.transform.position;

            Vector3 delta =
                target -
                origin;

            float distance =
                delta.magnitude;

            if (distance <= 0.001f)
            {
                return true;
            }

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    delta / distance,
                    lineOfSightHits,
                    distance,
                    lineOfSightMask,
                    QueryTriggerInteraction.Ignore);

            float nearestBlockingDistance =
                float.PositiveInfinity;

            for (int index = 0;
                 index < hitCount;
                 index++)
            {
                RaycastHit hit =
                    lineOfSightHits[index];

                Collider collider =
                    hit.collider;

                if (collider == null)
                {
                    continue;
                }

                Transform hitTransform =
                    collider.transform;

                if (
                    IsInside(
                        hitTransform,
                        activeInstance != null
                            ? activeInstance.transform
                            : null) ||
                    IsInside(
                        hitTransform,
                        figureRoot)
                )
                {
                    continue;
                }

                nearestBlockingDistance =
                    Mathf.Min(
                        nearestBlockingDistance,
                        hit.distance);
            }

            return nearestBlockingDistance >=
                distance -
                Mathf.Max(
                    0f,
                    lineOfSightSkin);
        }

        private void TickCutting()
        {
            bool canCut =
                state ==
                    PrototypeDefinitionSeveringState.Ready ||
                state ==
                    PrototypeDefinitionSeveringState.Cutting;

            cutInputPressed =
                inputActionActive &&
                cutAction.IsPressed();

            bool pressed =
                cutInputPressed;

            SuppressPaletteKnifeDebug(
                canCut);

            if (
                !canCut ||
                !pressed
            )
            {
                if (cutProgress > 0f)
                {
                    CancelCut(
                        canCut
                            ? "E bırakıldı; kesme ilerlemesi sıfırlandı."
                            : "Baş rig hedefi kaybedildi; kesme iptal.");
                }

                return;
            }

            if (cutProgress <= 0f)
            {
                cutStartCount++;
                lastAction =
                    "Palet Bıçağı Baş rig bağlantısını kesiyor.";
            }

            cutProgress +=
                Time.deltaTime /
                Mathf.Max(
                    0.05f,
                    cutHoldSeconds);

            cutProgress =
                Mathf.Clamp01(
                    cutProgress);

            state =
                PrototypeDefinitionSeveringState.Cutting;

            if (cutProgress >= 1f)
            {
                TryCompleteSever(
                    "Palette Knife hold completed");
            }
        }

        private bool TryCompleteSever(
            string source)
        {
            if (
                activeInstance == null ||
                eyePart == null ||
                eyePartRoot == null ||
                eyeSevered
            )
            {
                lastAction =
                    "Baş tanımı kesme için uygun değil.";

                return false;
            }

            PrototypeSeveredDefinitionToken token =
                CreateDetachedVisualToken();

            if (token == null)
            {
                lastAction =
                    "Kesilmiş Baş görseli üretilemedi; capability değiştirilmedi.";

                return false;
            }

            bool capabilityChanged =
                eyePart.SetCapabilityActive(
                    false,
                    "M50.0 Palette Knife Eye sever");

            if (!capabilityChanged)
            {
                Destroy(
                    token.gameObject);

                lastAction =
                    "Perception capability zaten kapalı.";

                return false;
            }

            for (int index = 0;
                 index < sourceEyeRenderers.Count;
                 index++)
            {
                LineRenderer renderer =
                    sourceEyeRenderers[index];

                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            RefreshPolicyHeadRenderers();

            for (int index = 0;
                 index < policyHeadRenderers.Count;
                 index++)
            {
                LineRenderer renderer =
                    policyHeadRenderers[index];

                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            if (eyePart.ProxyCollider != null)
            {
                eyePart.ProxyCollider.enabled = false;
            }

            detachedToken = token;
            detachedTokenCount++;
            eyeSevered = true;
            eyeCapabilityActive = false;
            cutProgress = 0f;
            severCount++;

            state =
                PrototypeDefinitionSeveringState.Severed;

            lastAction =
                "Baş tanımı kesildi; Perception capability kapandı.";

            SuppressPaletteKnifeDebug(
                false);

            Debug.Log(
                "[M50.0 Eye Definition Severed]\n" +
                $"Source={source}\n" +
                $"SnapshotHash={activeInstance.SnapshotHash}\n" +
                "PartKind=Head\n" +
                "Capability=Perception\n" +
                "CapabilityActive=False\n" +
                "SourceProxyEnabled=False\n" +
                "DetachedVisualCreated=True\n" +
                "GameplayPickupEnabled=False\n" +
                "DefinitionUseEnabled=False\n" +
                "DamageSystemEnabled=False\n" +
                "NetworkAuthorityEnabled=False",
                activeInstance);

            return true;
        }

        private PrototypeSeveredDefinitionToken
            CreateDetachedVisualToken()
        {
            if (
                eyePartRoot == null ||
                activeInstance == null
            )
            {
                return null;
            }

            List<LineRenderer> renderers =
                new List<LineRenderer>();

            for (int index = 0;
                 index < sourceEyeRenderers.Count;
                 index++)
            {
                LineRenderer candidate =
                    sourceEyeRenderers[index];

                if (
                    candidate != null &&
                    !renderers.Contains(candidate)
                )
                {
                    renderers.Add(candidate);
                }
            }

            RefreshPolicyHeadRenderers();

            for (int index = 0;
                 index < policyHeadRenderers.Count;
                 index++)
            {
                LineRenderer candidate =
                    policyHeadRenderers[index];

                if (
                    candidate != null &&
                    !renderers.Contains(candidate)
                )
                {
                    renderers.Add(candidate);
                }
            }

            if (renderers.Count == 0)
            {
                return null;
            }

            ResolveHeadRigCutAnchor();

            Vector3 center =
                headRigAnchorResolved
                    ? headRigCutAnchor
                    : eyePartRoot.position;

            GameObject tokenObject =
                new GameObject(
                    $"M50_SeveredEyeDefinition_{boundSnapshotHash}");

            tokenObject.transform.position =
                center;

            tokenObject.transform.rotation =
                Quaternion.identity;

            int copiedRendererCount = 0;

            for (int index = 0;
                 index < renderers.Count;
                 index++)
            {
                LineRenderer source =
                    renderers[index];

                if (
                    source == null ||
                    source.positionCount <= 0
                )
                {
                    continue;
                }

                GameObject lineObject =
                    new GameObject(
                        $"Detached_{source.gameObject.name}_{index:00}");

                lineObject.transform.SetParent(
                    tokenObject.transform,
                    false);

                LineRenderer clone =
                    lineObject.AddComponent<
                        LineRenderer>();

                CopyRendererSettings(
                    source,
                    clone);

                int pointCount =
                    source.positionCount;

                clone.positionCount =
                    pointCount;

                for (int pointIndex = 0;
                     pointIndex < pointCount;
                     pointIndex++)
                {
                    Vector3 sourcePoint =
                        source.GetPosition(
                            pointIndex);

                    Vector3 worldPoint =
                        source.useWorldSpace
                            ? sourcePoint
                            : source.transform.TransformPoint(
                                sourcePoint);

                    clone.SetPosition(
                        pointIndex,
                        tokenObject.transform.InverseTransformPoint(
                            worldPoint));
                }

                copiedRendererCount++;
            }

            if (copiedRendererCount <= 0)
            {
                Destroy(
                    tokenObject);

                return null;
            }

            PrototypeSeveredDefinitionToken token =
                tokenObject.AddComponent<
                    PrototypeSeveredDefinitionToken>();

            Vector3 outward =
                center -
                activeInstance.transform.position;

            outward =
                Vector3.ProjectOnPlane(
                    outward,
                    Vector3.up);

            if (outward.sqrMagnitude <=
                0.001f)
            {
                outward =
                    activeInstance.transform.forward;
            }
            else
            {
                outward.Normalize();
            }

            Vector3 initialVelocity =
                outward *
                detachOutwardSpeed +
                Vector3.up *
                detachUpwardSpeed;

            token.Configure(
                PrototypeMasterpiecePartKind.Head,
                PrototypeMasterpieceCapability.Perception,
                activeInstance.SnapshotHash,
                initialVelocity,
                detachAngularVelocity,
                activeInstance.transform,
                figureRoot);

            return token;
        }

        private static void CopyRendererSettings(
            LineRenderer source,
            LineRenderer target)
        {
            target.useWorldSpace = false;
            target.loop = source.loop;
            target.alignment = source.alignment;
            target.textureMode = source.textureMode;
            target.numCapVertices =
                source.numCapVertices;

            target.numCornerVertices =
                source.numCornerVertices;

            target.shadowCastingMode =
                ShadowCastingMode.Off;

            target.receiveShadows = false;
            target.widthMultiplier =
                source.widthMultiplier;

            target.widthCurve =
                source.widthCurve;

            target.colorGradient =
                source.colorGradient;

            target.sharedMaterial =
                source.sharedMaterial;

            target.enabled = true;
        }

        private void CancelCut(
            string reason)
        {
            if (cutProgress <= 0f)
            {
                return;
            }

            cutProgress = 0f;
            cutCancelCount++;
            lastAction = reason;
        }

        private void EnforceSeveredVisualState()
        {
            if (!eyeSevered)
            {
                return;
            }

            for (int index = 0;
                 index < sourceEyeRenderers.Count;
                 index++)
            {
                LineRenderer renderer =
                    sourceEyeRenderers[index];

                if (
                    renderer != null &&
                    renderer.enabled
                )
                {
                    renderer.enabled = false;
                }
            }

            RefreshPolicyHeadRenderers();

            for (int index = 0;
                 index < policyHeadRenderers.Count;
                 index++)
            {
                LineRenderer renderer =
                    policyHeadRenderers[index];

                if (
                    renderer != null &&
                    renderer.enabled
                )
                {
                    renderer.enabled = false;
                }
            }

            if (
                eyePart != null &&
                eyePart.ProxyCollider != null &&
                eyePart.ProxyCollider.enabled
            )
            {
                eyePart.ProxyCollider.enabled = false;
            }
        }

        private void EnsureHighlight()
        {
            if (highlightRing != null)
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default");
            }

            if (shader != null)
            {
                highlightMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M50_0_DefinitionSeverHighlight_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };

                if (highlightMaterial.HasProperty(
                        "_BaseColor"))
                {
                    highlightMaterial.SetColor(
                        "_BaseColor",
                        Color.white);
                }

                if (highlightMaterial.HasProperty(
                        "_Color"))
                {
                    highlightMaterial.SetColor(
                        "_Color",
                        Color.white);
                }
            }

            GameObject ringObject =
                new GameObject(
                    "M50_0_EyeDefinitionHighlight");

            ringObject.transform.SetParent(
                transform,
                false);

            highlightRing =
                ringObject.AddComponent<
                    LineRenderer>();

            highlightRing.useWorldSpace = true;
            highlightRing.loop = true;
            highlightRing.alignment =
                LineAlignment.View;

            highlightRing.textureMode =
                LineTextureMode.Stretch;

            highlightRing.numCapVertices = 3;
            highlightRing.numCornerVertices = 3;
            highlightRing.shadowCastingMode =
                ShadowCastingMode.Off;

            highlightRing.receiveShadows = false;
            highlightRing.sharedMaterial =
                highlightMaterial;

            highlightRing.widthMultiplier =
                0.045f;

            highlightRing.enabled = false;
        }

        private void UpdateHighlight()
        {
            if (
                highlightRing == null ||
                activeInstance == null ||
                eyePart == null ||
                !headRigAnchorResolved ||
                eyeSevered ||
                !figureRoleActive ||
                !paletteKnifeActive
            )
            {
                if (highlightRing != null)
                {
                    highlightRing.enabled = false;
                }

                return;
            }

            ResolveHeadRigCutAnchor();

            if (!headRigAnchorResolved)
            {
                highlightRing.enabled = false;
                return;
            }

            Vector3 center =
                headRigCutAnchor;

            float radius =
                state ==
                    PrototypeDefinitionSeveringState.Cutting
                    ? 0.34f
                    : 0.40f;

            const int segments = 28;

            highlightRing.positionCount =
                segments;

            Color color;

            if (
                state ==
                PrototypeDefinitionSeveringState.Cutting
            )
            {
                color =
                    new Color(
                        1f,
                        0.18f,
                        0.03f,
                        0.98f);

                radius *=
                    Mathf.Lerp(
                        1f,
                        0.72f,
                        cutProgress);
            }
            else if (
                state ==
                PrototypeDefinitionSeveringState.Ready
            )
            {
                color =
                    new Color(
                        1f,
                        0.55f,
                        0.06f,
                        0.96f);
            }
            else
            {
                color =
                    new Color(
                        0.08f,
                        0.76f,
                        0.82f,
                        0.72f);
            }

            highlightRing.startColor = color;
            highlightRing.endColor = color;

            Vector3 axisRight =
                figureCamera != null
                    ? figureCamera.transform.right
                    : Vector3.right;

            Vector3 axisUp =
                figureCamera != null
                    ? figureCamera.transform.up
                    : Vector3.up;

            for (int index = 0;
                 index < segments;
                 index++)
            {
                float angle =
                    index /
                    (float)segments *
                    Mathf.PI *
                    2f;

                highlightRing.SetPosition(
                    index,
                    center +
                    axisRight *
                    (
                        Mathf.Cos(angle) *
                        radius
                    ) +
                    axisUp *
                    (
                        Mathf.Sin(angle) *
                        radius
                    ));
            }

            highlightRing.enabled = true;
        }

        private void SuppressPaletteKnifeDebug(
            bool suppress)
        {
            if (
                paletteKnifeController == null ||
                paletteDebugField == null ||
                paletteDebugField.FieldType !=
                    typeof(bool)
            )
            {
                return;
            }

            try
            {
                if (
                    suppress &&
                    !paletteDebugSuppressed
                )
                {
                    object value =
                        paletteDebugField.GetValue(
                            paletteKnifeController);

                    paletteDebugOriginalValue =
                        value is bool enabled &&
                        enabled;

                    paletteDebugField.SetValue(
                        paletteKnifeController,
                        false);

                    paletteDebugSuppressed = true;
                }
                else if (
                    !suppress &&
                    paletteDebugSuppressed
                )
                {
                    paletteDebugField.SetValue(
                        paletteKnifeController,
                        paletteDebugOriginalValue);

                    paletteDebugSuppressed = false;
                }
            }
            catch (Exception)
            {
                paletteDebugSuppressed = false;
            }
        }

        private void RefreshHud()
        {
            bool visible =
                figureRoleActive &&
                activeInstance != null;

            if (statusGroup != null)
            {
                statusGroup.alpha =
                    visible ? 1f : 0f;

                statusGroup.interactable = false;
                statusGroup.blocksRaycasts = false;
            }

            if (!visible)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    "KARŞI-KOMPOZİSYON • BAŞ RİG TANIMI";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"DURUM {TranslateState(state)}   " +
                    $"KESME %{cutProgress * 100f:F0}   " +
                    $"MESAFE {(eyeDistance >= 0f ? eyeDistance.ToString("F1") : "-")} m\n" +
                    $"PALET BIÇAĞI {(paletteKnifeActive ? "AKTİF" : "KAPALI")}   " +
                    $"E {(cutInputPressed ? "BASILI" : "SERBEST")}   " +
                    $"ANCHOR {(headRigAnchorResolved ? "OK" : "YOK")}\n" +
                    $"ALGI CAP {(eyeCapabilityActive ? "AÇIK" : "KAPALI")}   " +
                    $"TOKEN {(detachedToken != null ? "DÜNYADA" : "YOK")}\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    eyeSevered
                        ? "BAŞ TANIMI AYRILDI • M50.1'DE TAŞINACAK\n" +
                          "PERCEPTION CAPABILITY KAPALI"
                        : "1 • PALET BIÇAĞI   E BASILI TUT • BOYUN / BAŞ BAĞLANTISINA NİŞAN AL\n" +
                          "CORE KESİLEMEZ • HASAR/ENVANTER/LOOT YOK";
            }
        }

        private static string TranslateState(
            PrototypeDefinitionSeveringState value)
        {
            switch (value)
            {
                case PrototypeDefinitionSeveringState.PainterRole:
                    return "PAINTER ROLÜ";

                case PrototypeDefinitionSeveringState.FigureToolUnavailable:
                    return "ARAÇ GEREKLİ";

                case PrototypeDefinitionSeveringState.EyeUnavailable:
                    return "GÖZ YOK";

                case PrototypeDefinitionSeveringState.OutOfRange:
                    return "MENZİL DIŞI";

                case PrototypeDefinitionSeveringState.AimRequired:
                    return "NİŞAN GEREKLİ";

                case PrototypeDefinitionSeveringState.Occluded:
                    return "ENGELLİ";

                case PrototypeDefinitionSeveringState.Ready:
                    return "HAZIR";

                case PrototypeDefinitionSeveringState.Cutting:
                    return "KESİLİYOR";

                case PrototypeDefinitionSeveringState.Severed:
                    return "KESİLDİ";

                default:
                    return "BEKLİYOR";
            }
        }

        private void ReleaseCurrentInstance()
        {
            SuppressPaletteKnifeDebug(
                false);

            sourceEyeRenderers.Clear();
            policyHeadRenderers.Clear();

            eyePart = null;
            eyePartRoot = null;
            eyeSevered = false;
            eyeCapabilityActive = false;
            headRigAnchorResolved = false;
            activeFigureCameraResolved = false;
            cutInputPressed = false;
            cutProgress = 0f;
            boundSnapshotHash = 0;

            DestroyDetachedToken();
            activeInstance = null;
        }

        private void DestroyDetachedToken()
        {
            if (detachedToken == null)
            {
                return;
            }

            GameObject tokenObject =
                detachedToken.gameObject;

            detachedToken = null;

            if (Application.isPlaying)
            {
                Destroy(
                    tokenObject);
            }
            else
            {
                DestroyImmediate(
                    tokenObject);
            }
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

        private void OnDisable()
        {
            SuppressPaletteKnifeDebug(
                false);

            if (cutAction != null)
            {
                cutAction.Disable();
            }

            inputActionActive = false;

            if (highlightRing != null)
            {
                highlightRing.enabled = false;
            }
        }

        private void OnDestroy()
        {
            SuppressPaletteKnifeDebug(
                false);

            if (cutAction != null)
            {
                cutAction.Dispose();
                cutAction = null;
            }

            if (highlightMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        highlightMaterial);
                }
                else
                {
                    DestroyImmediate(
                        highlightMaterial);
                }

                highlightMaterial = null;
            }

            DestroyDetachedToken();
        }
    }
}
