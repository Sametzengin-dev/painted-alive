using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(25500)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceVisualSafetyController :
        MonoBehaviour
    {
        private const int BoneCount = 5;

        [Header("Source")]
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deployment;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Safe Presentation")]
        [SerializeField, Min(0.02f)]
        private float safeUnderlayWidth = 0.145f;

        [SerializeField, Min(0.01f)]
        private float safeOverlayWidth = 0.068f;

        [SerializeField, Min(0.04f)]
        private float endpointShapeRadius = 0.18f;

        [SerializeField] private Color safeUnderlayColor =
            new Color(0.96f, 0.93f, 0.82f, 1f);

        [SerializeField] private Color safeOverlayColor =
            new Color(0.04f, 0.035f, 0.03f, 1f);

        [SerializeField] private Color perceptionColor =
            new Color(0.06f, 0.78f, 0.84f, 1f);

        [SerializeField] private Color attackColor =
            new Color(1f, 0.42f, 0.05f, 1f);

        [SerializeField] private Color movementColor =
            new Color(0.30f, 0.43f, 0.92f, 1f);

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeMasterpieceWorldInstance activeInstance;

        [SerializeField]
        private PrototypeMasterpieceVisualPolicyMode currentMode =
            PrototypeMasterpieceVisualPolicyMode.Original;

        [SerializeField] private bool reportActive;
        [SerializeField] private bool automaticFallbackActive;
        [SerializeField] private bool inputActionsActive;
        [SerializeField] private bool originalVisualAvailable;
        [SerializeField] private int boundSnapshotHash;
        [SerializeField] private int modeChangeCount;
        [SerializeField] private int localReportCount;
        [SerializeField] private int localHideCount;
        [SerializeField] private int clearReportCount;
        [SerializeField] private int automaticFallbackCount;
        [SerializeField] private int safeVisualUpdateCount;
        [SerializeField] private int readyPuppetUpdateCount;
        [SerializeField] private int preservedColliderCount;
        [SerializeField] private int preservedCapabilityCount;
        [SerializeField] private string lastAction =
            "Dünya Baş Yapıtı bekleniyor.";

        [SerializeField]
        private List<PrototypeMasterpieceLocalReportRecord> localReports =
            new List<PrototypeMasterpieceLocalReportRecord>();

        private InputAction cycleModeAction;
        private InputAction reportAction;
        private InputAction clearReportAction;

        private GameObject visualRoot;
        private GameObject safeRoot;
        private GameObject readyRoot;
        private Material safetyMaterial;

        private readonly LineRenderer[] safeUnderlayBones =
            new LineRenderer[BoneCount];

        private readonly LineRenderer[] safeOverlayBones =
            new LineRenderer[BoneCount];

        private readonly LineRenderer[] readyUnderlayBones =
            new LineRenderer[BoneCount];

        private readonly LineRenderer[] readyOverlayBones =
            new LineRenderer[BoneCount];

        private readonly Dictionary<
            PrototypeMasterpiecePartKind,
            LineRenderer> safeShapes =
            new Dictionary<
                PrototypeMasterpiecePartKind,
                LineRenderer>();

        private readonly Dictionary<
            PrototypeMasterpiecePartKind,
            LineRenderer> readyShapes =
            new Dictionary<
                PrototypeMasterpiecePartKind,
                LineRenderer>();

        public PrototypeMasterpieceVisualPolicyMode CurrentMode =>
            currentMode;
        public bool ReportActive => reportActive;
        public bool AutomaticFallbackActive =>
            automaticFallbackActive;
        public bool InputActionsActive => inputActionsActive;
        public bool OriginalVisualAvailable =>
            originalVisualAvailable;
        public int BoundSnapshotHash => boundSnapshotHash;
        public int ModeChangeCount => modeChangeCount;
        public int LocalReportCount => localReportCount;
        public int LocalHideCount => localHideCount;
        public int ClearReportCount => clearReportCount;
        public int AutomaticFallbackCount =>
            automaticFallbackCount;
        public int SafeVisualUpdateCount =>
            safeVisualUpdateCount;
        public int ReadyPuppetUpdateCount =>
            readyPuppetUpdateCount;
        public int PreservedColliderCount =>
            preservedColliderCount;
        public int PreservedCapabilityCount =>
            preservedCapabilityCount;
        public int LocalReportRecordCount =>
            localReports.Count;
        public string LastAction => lastAction;

        public bool GameplayProxyModified => false;
        public bool CapabilityStateModified => false;
        public bool PossessionMovementModified => false;
        public bool SemanticContentScreeningPerformed => false;
        public bool NetworkReportSubmitted => false;
        public bool ReplayEventSubmitted => false;
        public bool LocalPresentationOnly => true;

        public void Configure(
            PrototypeMasterpieceWorldDeploymentController
                configuredDeployment,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            deployment = configuredDeployment;
            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveInstance();
            RefreshHud();
        }

        private void Awake()
        {
            EnsureInputActions();
            EnsureMaterial();
            ResolveInstance();
            RefreshHud();
        }

        private void OnEnable()
        {
            EnsureInputActions();
            EnableInputActions();
            ResolveInstance();
            RefreshHud();
        }

        private void Update()
        {
            ResolveInstance();
            RefreshHud();
        }

        private void LateUpdate()
        {
            if (activeInstance == null)
            {
                return;
            }

            if (currentMode ==
                PrototypeMasterpieceVisualPolicyMode.SafeSilhouette)
            {
                UpdateSafeSilhouette();
            }
            else if (currentMode ==
                PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback)
            {
                UpdateReadyPuppet();
            }
        }

        public void CycleVisualMode()
        {
            if (activeInstance == null)
            {
                lastAction =
                    "Görünüm modu için aktif dünya Baş Yapıtı gerekli.";

                RefreshHud();
                return;
            }

            PrototypeMasterpieceVisualPolicyMode requested;

            switch (currentMode)
            {
                case PrototypeMasterpieceVisualPolicyMode.Original:
                    requested =
                        PrototypeMasterpieceVisualPolicyMode.SafeSilhouette;
                    break;

                case PrototypeMasterpieceVisualPolicyMode.SafeSilhouette:
                    requested =
                        PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback;
                    break;

                default:
                    requested =
                        PrototypeMasterpieceVisualPolicyMode.Original;
                    break;
            }

            if (reportActive &&
                requested ==
                    PrototypeMasterpieceVisualPolicyMode.Original)
            {
                requested =
                    PrototypeMasterpieceVisualPolicyMode.SafeSilhouette;
            }

            SetMode(
                requested,
                "Yerel görünüm modu değiştirildi.");
        }

        public void ReportAndHideLocally()
        {
            if (activeInstance == null)
            {
                lastAction =
                    "Raporlanacak aktif dünya Baş Yapıtı yok.";

                RefreshHud();
                return;
            }

            if (!HasReportForHash(
                    activeInstance.SnapshotHash))
            {
                localReports.Add(
                    new PrototypeMasterpieceLocalReportRecord(
                        activeInstance.SnapshotHash,
                        "LOCAL_VISUAL_REPORT",
                        currentMode));

                localReportCount++;
            }

            reportActive = true;
            localHideCount++;

            SetMode(
                PrototypeMasterpieceVisualPolicyMode.SafeSilhouette,
                "Yerel rapor kaydedildi; özgün çizim bu istemcide gizlendi.");

            Debug.LogWarning(
                "[M48.1 Local Visual Report]\n" +
                $"SnapshotHash={activeInstance.SnapshotHash}\n" +
                "LocalHide=True\n" +
                "NetworkReportSubmitted=False\n" +
                "ReplayEventSubmitted=False\n" +
                "SemanticContentScreeningPerformed=False",
                activeInstance);
        }

        public void ClearLocalReportForPrototype()
        {
            if (activeInstance == null)
            {
                lastAction =
                    "Temizlenecek aktif snapshot yok.";

                RefreshHud();
                return;
            }

            int removed =
                localReports.RemoveAll(
                    record =>
                        record != null &&
                        record.SnapshotHash ==
                            activeInstance.SnapshotHash);

            if (removed <= 0)
            {
                lastAction =
                    "Bu snapshot için yerel rapor kaydı yok.";

                RefreshHud();
                return;
            }

            reportActive = false;
            clearReportCount++;

            SetMode(
                automaticFallbackActive
                    ? PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback
                    : PrototypeMasterpieceVisualPolicyMode.Original,
                "Prototip yerel rapor kaydı temizlendi.");
        }

        public void ForceSafeSilhouette()
        {
            if (activeInstance == null)
            {
                return;
            }

            SetMode(
                PrototypeMasterpieceVisualPolicyMode.SafeSilhouette,
                "Güvenli siluet doğrudan seçildi.");
        }

        public void ForceReadyPuppetFallback()
        {
            if (activeInstance == null)
            {
                return;
            }

            SetMode(
                PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback,
                "Hazır kukla fallback doğrudan seçildi.");
        }

        private void EnsureInputActions()
        {
            if (cycleModeAction == null)
            {
                cycleModeAction =
                    new InputAction(
                        "M48.1 Cycle Visual Policy",
                        InputActionType.Button,
                        "<Keyboard>/k");

                cycleModeAction.performed +=
                    HandleCycleMode;
            }

            if (reportAction == null)
            {
                reportAction =
                    new InputAction(
                        "M48.1 Report and Hide",
                        InputActionType.Button,
                        "<Keyboard>/j");

                reportAction.performed +=
                    HandleReport;
            }

            if (clearReportAction == null)
            {
                clearReportAction =
                    new InputAction(
                        "M48.1 Clear Local Report",
                        InputActionType.Button,
                        "<Keyboard>/u");

                clearReportAction.performed +=
                    HandleClearReport;
            }
        }

        private void EnableInputActions()
        {
            cycleModeAction?.Enable();
            reportAction?.Enable();
            clearReportAction?.Enable();

            inputActionsActive =
                cycleModeAction != null &&
                cycleModeAction.enabled &&
                reportAction != null &&
                reportAction.enabled &&
                clearReportAction != null &&
                clearReportAction.enabled;
        }

        private void DisableInputActions()
        {
            cycleModeAction?.Disable();
            reportAction?.Disable();
            clearReportAction?.Disable();
            inputActionsActive = false;
        }

        private void HandleCycleMode(
            InputAction.CallbackContext context)
        {
            CycleVisualMode();
        }

        private void HandleReport(
            InputAction.CallbackContext context)
        {
            ReportAndHideLocally();
        }

        private void HandleClearReport(
            InputAction.CallbackContext context)
        {
            ClearLocalReportForPrototype();
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
                boundSnapshotHash = 0;
                reportActive = false;
                automaticFallbackActive = false;
                originalVisualAvailable = false;
                preservedColliderCount = 0;
                preservedCapabilityCount = 0;
                lastAction =
                    "Dünya Baş Yapıtı bekleniyor.";

                return;
            }

            BindInstance(
                activeInstance);
        }

        private void BindInstance(
            PrototypeMasterpieceWorldInstance instance)
        {
            EnsureMaterial();
            CreateVisualRoots();

            boundSnapshotHash =
                instance.SnapshotHash;

            originalVisualAvailable =
                instance.BuildSucceeded &&
                instance.OriginalStrokeRendererCount > 0 &&
                instance.VisualSegmentCount > 0 &&
                instance.RenderedPointCount >= 2;

            preservedColliderCount =
                instance.ColliderCount;

            preservedCapabilityCount =
                CountCapabilities(
                    instance);

            reportActive =
                HasReportForHash(
                    instance.SnapshotHash);

            automaticFallbackActive =
                !originalVisualAvailable ||
                instance.PartCount != 6 ||
                instance.ColliderCount != 6 ||
                !instance.AllCollidersAreTriggers;

            if (automaticFallbackActive)
            {
                automaticFallbackCount++;

                SetMode(
                    PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback,
                    "Özgün görsel sözleşmesi eksik; hazır kukla fallback etkin.");
            }
            else if (reportActive)
            {
                SetMode(
                    PrototypeMasterpieceVisualPolicyMode.SafeSilhouette,
                    "Bu snapshot daha önce yerel raporlandı; özgün görünüm gizli.");
            }
            else
            {
                SetMode(
                    PrototypeMasterpieceVisualPolicyMode.Original,
                    "Özgün oyuncu çizimi gösteriliyor.");
            }
        }

        private int CountCapabilities(
            PrototypeMasterpieceWorldInstance instance)
        {
            int count = 0;

            Array values =
                Enum.GetValues(
                    typeof(
                        PrototypeMasterpieceCapability));

            foreach (object value in values)
            {
                var capability =
                    (PrototypeMasterpieceCapability)
                    value;

                if (instance.HasActiveCapability(
                        capability))
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasReportForHash(
            int snapshotHash)
        {
            for (int index = 0;
                 index < localReports.Count;
                 index++)
            {
                PrototypeMasterpieceLocalReportRecord record =
                    localReports[index];

                if (record != null &&
                    record.SnapshotHash ==
                        snapshotHash)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetMode(
            PrototypeMasterpieceVisualPolicyMode requested,
            string reason)
        {
            if (activeInstance == null)
            {
                return;
            }

            if (reportActive &&
                requested ==
                    PrototypeMasterpieceVisualPolicyMode.Original)
            {
                requested =
                    PrototypeMasterpieceVisualPolicyMode.SafeSilhouette;
            }

            if (automaticFallbackActive &&
                requested ==
                    PrototypeMasterpieceVisualPolicyMode.Original)
            {
                requested =
                    PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback;
            }

            bool changed =
                currentMode != requested;

            currentMode = requested;

            activeInstance.SetOriginalStrokeVisualsVisible(
                currentMode ==
                    PrototypeMasterpieceVisualPolicyMode.Original);

            if (safeRoot != null)
            {
                safeRoot.SetActive(
                    currentMode ==
                        PrototypeMasterpieceVisualPolicyMode.SafeSilhouette);
            }

            if (readyRoot != null)
            {
                readyRoot.SetActive(
                    currentMode ==
                        PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback);
            }

            if (changed)
            {
                modeChangeCount++;
            }

            lastAction = reason;

            if (currentMode ==
                PrototypeMasterpieceVisualPolicyMode.SafeSilhouette)
            {
                UpdateSafeSilhouette();
            }
            else if (currentMode ==
                PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback)
            {
                UpdateReadyPuppet();
            }

            RefreshHud();
        }

        private void CreateVisualRoots()
        {
            if (activeInstance == null)
            {
                return;
            }

            visualRoot =
                new GameObject(
                    "M48_1_VisualSafetyRuntime");

            visualRoot.transform.SetParent(
                activeInstance.transform,
                false);

            safeRoot =
                new GameObject(
                    "SafeSilhouette");

            safeRoot.transform.SetParent(
                visualRoot.transform,
                false);

            readyRoot =
                new GameObject(
                    "ReadyPuppetFallback");

            readyRoot.transform.SetParent(
                visualRoot.transform,
                false);

            for (int index = 0;
                 index < BoneCount;
                 index++)
            {
                safeUnderlayBones[index] =
                    CreateLine(
                        safeRoot.transform,
                        $"SafeUnderlayBone_{index}",
                        safeUnderlayWidth,
                        safeUnderlayColor);

                safeOverlayBones[index] =
                    CreateLine(
                        safeRoot.transform,
                        $"SafeOverlayBone_{index}",
                        safeOverlayWidth,
                        safeOverlayColor);

                readyUnderlayBones[index] =
                    CreateLine(
                        readyRoot.transform,
                        $"ReadyUnderlayBone_{index}",
                        safeUnderlayWidth,
                        safeUnderlayColor);

                readyOverlayBones[index] =
                    CreateLine(
                        readyRoot.transform,
                        $"ReadyOverlayBone_{index}",
                        safeOverlayWidth,
                        safeOverlayColor);
            }

            CreateShapeSet(
                safeRoot.transform,
                "Safe",
                safeShapes);

            CreateShapeSet(
                readyRoot.transform,
                "Ready",
                readyShapes);

            safeRoot.SetActive(false);
            readyRoot.SetActive(false);
        }

        private void CreateShapeSet(
            Transform parent,
            string prefix,
            Dictionary<
                PrototypeMasterpiecePartKind,
                LineRenderer> target)
        {
            target.Clear();

            Array values =
                Enum.GetValues(
                    typeof(
                        PrototypeMasterpiecePartKind));

            foreach (object value in values)
            {
                var kind =
                    (PrototypeMasterpiecePartKind)
                    value;

                target[kind] =
                    CreateLine(
                        parent,
                        $"{prefix}Shape_{kind}",
                        safeOverlayWidth,
                        ResolveShapeColor(kind));
            }
        }

        private LineRenderer CreateLine(
            Transform parent,
            string objectName,
            float width,
            Color color)
        {
            GameObject lineObject =
                new GameObject(
                    objectName);

            lineObject.transform.SetParent(
                parent,
                false);

            LineRenderer line =
                lineObject.AddComponent<
                    LineRenderer>();

            line.useWorldSpace = true;
            line.loop = false;
            line.alignment =
                LineAlignment.View;

            line.textureMode =
                LineTextureMode.Stretch;

            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.shadowCastingMode =
                ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = color;
            line.sharedMaterial =
                safetyMaterial;

            return line;
        }

        private void EnsureMaterial()
        {
            if (safetyMaterial != null)
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

            if (shader == null)
            {
                return;
            }

            safetyMaterial =
                new Material(
                    shader)
                {
                    name =
                        "M48_1_VisualSafety_Runtime",
                    hideFlags =
                        HideFlags.DontSave
                };

            if (safetyMaterial.HasProperty(
                    "_BaseColor"))
            {
                safetyMaterial.SetColor(
                    "_BaseColor",
                    Color.white);
            }

            if (safetyMaterial.HasProperty(
                    "_Color"))
            {
                safetyMaterial.SetColor(
                    "_Color",
                    Color.white);
            }
        }

        private void UpdateSafeSilhouette()
        {
            if (!TryGetProxyPoints(
                    out Vector3 core,
                    out Vector3 head,
                    out Vector3 leftAttack,
                    out Vector3 rightAttack,
                    out Vector3 leftContact,
                    out Vector3 rightContact))
            {
                SetMode(
                    PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback,
                    "Proxy merkezi okunamadı; hazır kukla fallback etkin.");

                return;
            }

            UpdateSkeleton(
                safeUnderlayBones,
                core,
                head,
                leftAttack,
                rightAttack,
                leftContact,
                rightContact);

            UpdateSkeleton(
                safeOverlayBones,
                core,
                head,
                leftAttack,
                rightAttack,
                leftContact,
                rightContact);

            UpdateShapeSet(
                safeShapes,
                core,
                head,
                leftAttack,
                rightAttack,
                leftContact,
                rightContact);

            safeVisualUpdateCount++;
        }

        private void UpdateReadyPuppet()
        {
            if (activeInstance == null)
            {
                return;
            }

            Transform root =
                activeInstance.transform;

            Vector3 core =
                root.TransformPoint(
                    new Vector3(
                        0f,
                        1.28f,
                        -0.04f));

            Vector3 head =
                root.TransformPoint(
                    new Vector3(
                        0f,
                        2.28f,
                        -0.04f));

            Vector3 leftAttack =
                root.TransformPoint(
                    new Vector3(
                        -0.92f,
                        1.52f,
                        -0.04f));

            Vector3 rightAttack =
                root.TransformPoint(
                    new Vector3(
                        0.92f,
                        1.52f,
                        -0.04f));

            Vector3 leftContact =
                root.TransformPoint(
                    new Vector3(
                        -0.46f,
                        0.12f,
                        -0.04f));

            Vector3 rightContact =
                root.TransformPoint(
                    new Vector3(
                        0.46f,
                        0.12f,
                        -0.04f));

            UpdateSkeleton(
                readyUnderlayBones,
                core,
                head,
                leftAttack,
                rightAttack,
                leftContact,
                rightContact);

            UpdateSkeleton(
                readyOverlayBones,
                core,
                head,
                leftAttack,
                rightAttack,
                leftContact,
                rightContact);

            UpdateShapeSet(
                readyShapes,
                core,
                head,
                leftAttack,
                rightAttack,
                leftContact,
                rightContact);

            readyPuppetUpdateCount++;
        }

        private bool TryGetProxyPoints(
            out Vector3 core,
            out Vector3 head,
            out Vector3 leftAttack,
            out Vector3 rightAttack,
            out Vector3 leftContact,
            out Vector3 rightContact)
        {
            core = default;
            head = default;
            leftAttack = default;
            rightAttack = default;
            leftContact = default;
            rightContact = default;

            return TryGetPartPoint(
                    PrototypeMasterpiecePartKind.Core,
                    out core) &&
                TryGetPartPoint(
                    PrototypeMasterpiecePartKind.Head,
                    out head) &&
                TryGetPartPoint(
                    PrototypeMasterpiecePartKind.LeftAttack,
                    out leftAttack) &&
                TryGetPartPoint(
                    PrototypeMasterpiecePartKind.RightAttack,
                    out rightAttack) &&
                TryGetPartPoint(
                    PrototypeMasterpiecePartKind.LeftContact,
                    out leftContact) &&
                TryGetPartPoint(
                    PrototypeMasterpiecePartKind.RightContact,
                    out rightContact);
        }

        private bool TryGetPartPoint(
            PrototypeMasterpiecePartKind kind,
            out Vector3 point)
        {
            point = default;

            if (activeInstance == null ||
                !activeInstance.TryGetWorldPart(
                    kind,
                    out PrototypeMasterpieceWorldPart part) ||
                part == null ||
                part.ProxyCollider == null)
            {
                return false;
            }

            point =
                part.ProxyCollider.bounds.center;

            point +=
                activeInstance.transform.forward *
                -0.045f;

            return true;
        }

        private static void UpdateSkeleton(
            LineRenderer[] lines,
            Vector3 core,
            Vector3 head,
            Vector3 leftAttack,
            Vector3 rightAttack,
            Vector3 leftContact,
            Vector3 rightContact)
        {
            if (lines == null ||
                lines.Length < BoneCount)
            {
                return;
            }

            SetSegment(
                lines[0],
                core,
                head);

            SetSegment(
                lines[1],
                core,
                leftAttack);

            SetSegment(
                lines[2],
                core,
                rightAttack);

            SetSegment(
                lines[3],
                core,
                leftContact);

            SetSegment(
                lines[4],
                core,
                rightContact);
        }

        private void UpdateShapeSet(
            Dictionary<
                PrototypeMasterpiecePartKind,
                LineRenderer> shapes,
            Vector3 core,
            Vector3 head,
            Vector3 leftAttack,
            Vector3 rightAttack,
            Vector3 leftContact,
            Vector3 rightContact)
        {
            SetShape(
                shapes,
                PrototypeMasterpiecePartKind.Core,
                core,
                endpointShapeRadius * 1.05f);

            SetShape(
                shapes,
                PrototypeMasterpiecePartKind.Head,
                head,
                endpointShapeRadius * 1.15f);

            SetShape(
                shapes,
                PrototypeMasterpiecePartKind.LeftAttack,
                leftAttack,
                endpointShapeRadius);

            SetShape(
                shapes,
                PrototypeMasterpiecePartKind.RightAttack,
                rightAttack,
                endpointShapeRadius);

            SetShape(
                shapes,
                PrototypeMasterpiecePartKind.LeftContact,
                leftContact,
                endpointShapeRadius * 0.92f);

            SetShape(
                shapes,
                PrototypeMasterpiecePartKind.RightContact,
                rightContact,
                endpointShapeRadius * 0.92f);
        }

        private void SetShape(
            Dictionary<
                PrototypeMasterpiecePartKind,
                LineRenderer> shapes,
            PrototypeMasterpiecePartKind kind,
            Vector3 center,
            float radius)
        {
            if (!shapes.TryGetValue(
                    kind,
                    out LineRenderer line) ||
                line == null)
            {
                return;
            }

            switch (kind)
            {
                case PrototypeMasterpiecePartKind.Head:
                    SetRegularPolygon(
                        line,
                        center,
                        radius,
                        20,
                        0f);
                    break;

                case PrototypeMasterpiecePartKind.LeftAttack:
                case PrototypeMasterpiecePartKind.RightAttack:
                    SetRegularPolygon(
                        line,
                        center,
                        radius,
                        3,
                        90f);
                    break;

                case PrototypeMasterpiecePartKind.LeftContact:
                case PrototypeMasterpiecePartKind.RightContact:
                    SetRegularPolygon(
                        line,
                        center,
                        radius,
                        4,
                        45f);
                    break;

                default:
                    SetRegularPolygon(
                        line,
                        center,
                        radius,
                        4,
                        0f);
                    break;
            }
        }

        private static void SetRegularPolygon(
            LineRenderer line,
            Vector3 center,
            float radius,
            int sides,
            float angleOffsetDegrees)
        {
            if (line == null)
            {
                return;
            }

            int safeSides =
                Mathf.Max(
                    3,
                    sides);

            line.loop = true;
            line.positionCount =
                safeSides;

            Vector3 right =
                Camera.main != null
                    ? Camera.main.transform.right
                    : Vector3.right;

            Vector3 up =
                Camera.main != null
                    ? Camera.main.transform.up
                    : Vector3.up;

            float offset =
                angleOffsetDegrees *
                Mathf.Deg2Rad;

            for (int index = 0;
                 index < safeSides;
                 index++)
            {
                float angle =
                    index /
                    (float)safeSides *
                    Mathf.PI *
                    2f +
                    offset;

                line.SetPosition(
                    index,
                    center +
                    right *
                    (
                        Mathf.Cos(angle) *
                        radius
                    ) +
                    up *
                    (
                        Mathf.Sin(angle) *
                        radius
                    ));
            }
        }

        private static void SetSegment(
            LineRenderer line,
            Vector3 start,
            Vector3 end)
        {
            if (line == null)
            {
                return;
            }

            line.loop = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private Color ResolveShapeColor(
            PrototypeMasterpiecePartKind kind)
        {
            switch (kind)
            {
                case PrototypeMasterpiecePartKind.Head:
                    return perceptionColor;

                case PrototypeMasterpiecePartKind.LeftAttack:
                case PrototypeMasterpiecePartKind.RightAttack:
                    return attackColor;

                case PrototypeMasterpiecePartKind.LeftContact:
                case PrototypeMasterpiecePartKind.RightContact:
                    return movementColor;

                default:
                    return safeOverlayColor;
            }
        }

        private void RefreshHud()
        {
            bool visible =
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
                    "BAŞ YAPIT • GÖRSEL GÜVENLİK";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"MOD {TranslateMode(currentMode)}   " +
                    $"RAPOR {(reportActive ? "AKTİF" : "YOK")}   " +
                    $"AUTO {(automaticFallbackActive ? "FALLBACK" : "NORMAL")}\n" +
                    $"ORİJİNAL RENDERER {activeInstance.OriginalStrokeRendererCount}   " +
                    $"COLLIDER {preservedColliderCount}/6   " +
                    $"CAP {preservedCapabilityCount}/6\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "K • GÖRÜNÜM   J • YEREL RAPOR/GİZLE   " +
                    "U • RAPORU SIFIRLA (PROTOTİP)\n" +
                    "YEREL SUNUM • SEMANTİK TARAMA YOK • AĞ RAPORU YOK";
            }
        }

        private static string TranslateMode(
            PrototypeMasterpieceVisualPolicyMode mode)
        {
            switch (mode)
            {
                case PrototypeMasterpieceVisualPolicyMode.SafeSilhouette:
                    return "GÜVENLİ SİLUET";

                case PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback:
                    return "HAZIR KUKLA";

                default:
                    return "ORİJİNAL";
            }
        }

        private void ReleaseCurrentInstance()
        {
            if (activeInstance != null)
            {
                activeInstance.SetOriginalStrokeVisualsVisible(
                    true);
            }

            if (visualRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        visualRoot);
                }
                else
                {
                    DestroyImmediate(
                        visualRoot);
                }
            }

            visualRoot = null;
            safeRoot = null;
            readyRoot = null;
            safeShapes.Clear();
            readyShapes.Clear();

            Array.Clear(
                safeUnderlayBones,
                0,
                safeUnderlayBones.Length);

            Array.Clear(
                safeOverlayBones,
                0,
                safeOverlayBones.Length);

            Array.Clear(
                readyUnderlayBones,
                0,
                readyUnderlayBones.Length);

            Array.Clear(
                readyOverlayBones,
                0,
                readyOverlayBones.Length);
        }

        private void OnDisable()
        {
            DisableInputActions();
            ReleaseCurrentInstance();
            activeInstance = null;
        }

        private void OnDestroy()
        {
            if (cycleModeAction != null)
            {
                cycleModeAction.performed -=
                    HandleCycleMode;

                cycleModeAction.Dispose();
                cycleModeAction = null;
            }

            if (reportAction != null)
            {
                reportAction.performed -=
                    HandleReport;

                reportAction.Dispose();
                reportAction = null;
            }

            if (clearReportAction != null)
            {
                clearReportAction.performed -=
                    HandleClearReport;

                clearReportAction.Dispose();
                clearReportAction = null;
            }

            if (safetyMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        safetyMaterial);
                }
                else
                {
                    DestroyImmediate(
                        safetyMaterial);
                }

                safetyMaterial = null;
            }
        }
    }
}
