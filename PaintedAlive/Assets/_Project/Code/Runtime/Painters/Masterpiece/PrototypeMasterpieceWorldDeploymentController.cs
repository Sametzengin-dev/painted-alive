using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Painters.SideCanvas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(820)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceWorldDeploymentController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeLivingSideCanvasController sideCanvas;

        [SerializeField]
        private PrototypeMasterpieceAssemblyController assembly;

        [SerializeField]
        private PrototypeMasterpieceRouteAnchor routeAnchor;

        [SerializeField] private Transform worldInstanceParent;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Side Canvas Feedback")]
        [SerializeField] private CanvasGroup sideCanvasFeedbackGroup;
        [SerializeField] private Text sideCanvasFeedbackText;

        [Header("Deployment")]
        [SerializeField] private bool closeCanvasAfterDeploy = true;
        [SerializeField] private bool showProxyDebugAtSpawn = true;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeMasterpieceWorldInstance activeInstance;

        [SerializeField] private int deployCount;
        [SerializeField] private int recallCount;
        [SerializeField] private int rejectedDeployCount;
        [SerializeField] private int proxyToggleCount;
        [SerializeField] private int deployInputAttemptCount;
        [SerializeField] private int directDeployAttemptCount;
        [SerializeField] private int sideCanvasResolveCount;
        [SerializeField] private int sideCanvasReferenceChangeCount;
        [SerializeField] private int openSideCanvasCandidateCount;
        [SerializeField] private int assemblyResolveCount;
        [SerializeField] private int assemblyReferenceChangeCount;
        [SerializeField] private int assemblyPrepareAttemptCount;
        [SerializeField] private int assemblyPrepareSuccessCount;
        [SerializeField] private int lastSnapshotHash;
        [SerializeField] private float lastDeployAttemptAt = -100f;
        [SerializeField] private bool deployInputActionsActive;
        [SerializeField] private bool painterRoleActive;
        [SerializeField] private int roleRejectedActionCount;
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private bool sideCanvasResolvedOpen;
        [SerializeField] private bool assemblySourceMatchesSideCanvas;
        [SerializeField] private string sideCanvasSelectionReason =
            "Not resolved";
        [SerializeField] private string lastDeploySource = "None";
        [SerializeField] private string lastAssemblyPreparation =
            "Not prepared";
        [SerializeField] private string lastAction =
            "M46 assembly bekleniyor.";

        public bool WorldInstanceActive =>
            activeInstance != null;

        public PrototypeMasterpieceWorldInstance ActiveInstance =>
            activeInstance;

        public int DeployCount => deployCount;
        public int RecallCount => recallCount;
        public int RejectedDeployCount => rejectedDeployCount;
        public int ProxyToggleCount => proxyToggleCount;
        public int DeployInputAttemptCount => deployInputAttemptCount;
        public int DirectDeployAttemptCount => directDeployAttemptCount;
        public int SideCanvasResolveCount =>
            sideCanvasResolveCount;
        public int SideCanvasReferenceChangeCount =>
            sideCanvasReferenceChangeCount;
        public int OpenSideCanvasCandidateCount =>
            openSideCanvasCandidateCount;
        public bool SideCanvasResolvedOpen =>
            sideCanvasResolvedOpen;
        public string SideCanvasSelectionReason =>
            sideCanvasSelectionReason;
        public int AssemblyResolveCount => assemblyResolveCount;
        public int AssemblyReferenceChangeCount =>
            assemblyReferenceChangeCount;
        public int AssemblyPrepareAttemptCount =>
            assemblyPrepareAttemptCount;
        public int AssemblyPrepareSuccessCount =>
            assemblyPrepareSuccessCount;
        public bool AssemblySourceMatchesSideCanvas =>
            assemblySourceMatchesSideCanvas;
        public string LastAssemblyPreparation =>
            lastAssemblyPreparation;
        public int LastSnapshotHash => lastSnapshotHash;
        public float LastDeployAttemptAt => lastDeployAttemptAt;
        public bool DeployInputActionsActive => deployInputActionsActive;
        public bool PainterRoleActive => painterRoleActive;
        public int RoleRejectedActionCount => roleRejectedActionCount;
        public string ResolvedRole => resolvedRole;
        public string LastDeploySource => lastDeploySource;
        public string LastAction => lastAction;

        public bool StaticDeploymentOnly => true;
        public bool TriggerCollidersOnly => true;
        public bool BossAIEnabled => false;
        public bool DamageAuthorityEnabled => false;
        public bool NetworkIntegrationParked => true;

        private InputAction deployAction;
        private InputAction recallAction;
        private InputAction proxyAction;

        public void Configure(
            PrototypeLivingSideCanvasController configuredSideCanvas,
            PrototypeMasterpieceAssemblyController configuredAssembly,
            PrototypeMasterpieceRouteAnchor configuredRouteAnchor,
            Transform configuredWorldInstanceParent,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText,
            CanvasGroup configuredSideCanvasFeedbackGroup,
            Text configuredSideCanvasFeedbackText)
        {
            sideCanvas = configuredSideCanvas;
            assembly = configuredAssembly;
            routeAnchor = configuredRouteAnchor;
            worldInstanceParent =
                configuredWorldInstanceParent;

            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;
            sideCanvasFeedbackGroup =
                configuredSideCanvasFeedbackGroup;

            sideCanvasFeedbackText =
                configuredSideCanvasFeedbackText;

            RefreshHud();
        }

        private void Awake()
        {
            EnsureInputActions();
            RefreshHud();
        }

        private void OnEnable()
        {
            EnsureInputActions();
            RefreshRoleAuthority(forceInputRefresh: true);
            RefreshHud();
        }

        private void Update()
        {
            RefreshRoleAuthority(forceInputRefresh: false);
            RefreshHud();
        }

        private void RefreshRoleAuthority(bool forceInputRefresh)
        {
            bool previous = painterRoleActive;
            painterRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedRole);

            if (!forceInputRefresh && previous == painterRoleActive)
            {
                return;
            }

            if (painterRoleActive)
            {
                EnableInputActions();
            }
            else
            {
                DisableInputActions();
            }
        }

        private bool RequirePainterRole(string action)
        {
            RefreshRoleAuthority(forceInputRefresh: false);

            if (painterRoleActive)
            {
                return true;
            }

            roleRejectedActionCount++;
            lastAction =
                $"{action} yalnız Painter rolünde kullanılabilir.";

            Debug.Log(
                "[Role Authority Block]\n" +
                $"Action={action}\n" +
                $"ResolvedRole={resolvedRole}\n" +
                "RequiredRole=Painter\n" +
                "Severity=Info",
                this);

            RefreshHud();
            return false;
        }

        private void EnsureInputActions()
        {
            if (deployAction == null)
            {
                deployAction =
                    new InputAction(
                        "M47 Deploy",
                        InputActionType.Button,
                        "<Keyboard>/v");

                deployAction.performed +=
                    HandleDeployAction;
            }

            if (recallAction == null)
            {
                recallAction =
                    new InputAction(
                        "M47 Recall",
                        InputActionType.Button,
                        "<Keyboard>/x");

                recallAction.performed +=
                    HandleRecallAction;
            }

            if (proxyAction == null)
            {
                proxyAction =
                    new InputAction(
                        "M47 Proxy",
                        InputActionType.Button,
                        "<Keyboard>/o");

                proxyAction.performed +=
                    HandleProxyAction;
            }
        }

        private void EnableInputActions()
        {
            deployAction?.Enable();
            recallAction?.Enable();
            proxyAction?.Enable();

            deployInputActionsActive =
                deployAction != null &&
                deployAction.enabled &&
                recallAction != null &&
                recallAction.enabled &&
                proxyAction != null &&
                proxyAction.enabled;
        }

        private void DisableInputActions()
        {
            deployAction?.Disable();
            recallAction?.Disable();
            proxyAction?.Disable();
            deployInputActionsActive = false;
        }

        private void HandleDeployAction(
            InputAction.CallbackContext context)
        {
            if (!RequirePainterRole("Deploy"))
            {
                return;
            }

            deployInputAttemptCount++;
            TryDeployInternal(
                "Keyboard V");
        }

        private void HandleRecallAction(
            InputAction.CallbackContext context)
        {
            if (!RequirePainterRole("Geri çağırma"))
            {
                return;
            }

            Recall();
        }

        private void HandleProxyAction(
            InputAction.CallbackContext context)
        {
            if (!RequirePainterRole("Dünya proxy görünümü"))
            {
                return;
            }

            ToggleWorldProxyDebug();
        }

        public void TryDeploy()
        {
            if (!RequirePainterRole("Deploy"))
            {
                return;
            }

            directDeployAttemptCount++;
            TryDeployInternal(
                "Direct / Editor Menu");
        }

        private void TryDeployInternal(
            string source)
        {
            if (!RequirePainterRole("Deploy"))
            {
                return;
            }

            lastDeploySource = source;
            lastDeployAttemptAt =
                Time.unscaledTime;

            ResolveSideCanvasReference();
            ResolveAssemblyReference();

            assemblyPrepareAttemptCount++;

            string preparationReason =
                "Aynı Yan Tuvale bağlı M46 controller bulunamadı.";

            bool assemblyPrepared = false;

            if (assembly != null)
            {
                assemblyPrepared =
                    assembly.TryPrepareForDeployment(
                        out preparationReason);
            }

            lastAssemblyPreparation =
                preparationReason;

            if (assemblyPrepared)
            {
                assemblyPrepareSuccessCount++;
            }

            Debug.Log(
                "[M47 Deploy Attempt]\n" +
                $"Source={source}\n" +
                $"SideCanvasResolved={(sideCanvas != null)}\n" +
                $"SideCanvasOpen={(sideCanvas != null && sideCanvas.IsOpen)}\n" +
                $"SideCanvasMode={(sideCanvas != null ? sideCanvas.CurrentMode.ToString() : "N/A")}\n" +
                $"SideCanvasRigValid={(sideCanvas != null && sideCanvas.RigValid)}\n" +
                $"SideCanvasMarkers={(sideCanvas != null ? sideCanvas.MarkerCount : -1)}\n" +
                $"SideCanvasStrokes={(sideCanvas != null ? sideCanvas.StrokeCount : -1)}\n" +
                $"OpenSideCanvasCandidates={openSideCanvasCandidateCount}\n" +
                $"SideCanvasSelection={sideCanvasSelectionReason}\n" +
                $"AssemblyResolved={(assembly != null)}\n" +
                $"AssemblySourceMatches={assemblySourceMatchesSideCanvas}\n" +
                $"AssemblyPrepared={assemblyPrepared}\n" +
                $"AssemblyPreparation={preparationReason}\n" +
                $"AssemblyReady={(assembly != null && assembly.AssemblyReady)}\n" +
                $"PartCount={(assembly != null ? assembly.PartCount : -1)}\n" +
                $"ProxyCount={(assembly != null ? assembly.ProxyCount : -1)}\n" +
                $"DetachedParts={(assembly != null ? assembly.DetachedPartCount : -1)}\n" +
                $"Anchor={(routeAnchor != null ? routeAnchor.name : "MISSING")}",
                this);

            if (!assemblyPrepared)
            {
                Reject(
                    preparationReason);
                return;
            }

            if (activeInstance != null)
            {
                Reject(
                    "Önce X ile mevcut Baş Yapıtı geri çağır.");
                return;
            }

            if (routeAnchor == null)
            {
                Reject(
                    "M47 route anchor bulunamadı.");
                return;
            }

            if (!routeAnchor.ValidatePlacement(
                    worldInstanceParent,
                    out string placementReason))
            {
                Reject(
                    placementReason);
                return;
            }

            if (!PrototypeMasterpieceDeploymentSnapshot.TryCreate(
                    sideCanvas,
                    assembly,
                    out PrototypeMasterpieceDeploymentSnapshot snapshot,
                    out string snapshotReason))
            {
                Reject(
                    snapshotReason);
                return;
            }

            GameObject instanceObject =
                new GameObject(
                    $"M47_DeployedMasterpiece_{deployCount + 1:00}");

            if (worldInstanceParent != null)
            {
                instanceObject.transform.SetParent(
                    worldInstanceParent,
                    false);
            }

            PrototypeMasterpieceWorldInstance instance =
                instanceObject.AddComponent<
                    PrototypeMasterpieceWorldInstance>();

            if (!instance.Build(
                    snapshot,
                    routeAnchor,
                    out string buildReason))
            {
                Destroy(
                    instanceObject);

                Reject(
                    buildReason);
                return;
            }

            activeInstance = instance;
            activeInstance.SetProxyDebugVisible(
                showProxyDebugAtSpawn);

            deployCount++;
            lastSnapshotHash =
                snapshot.ContentHash;

            lastAction =
                "Deploy başarılı • Baş Yapıt route anchor'a aktarıldı.";

            Debug.Log(
                "[M47 Deploy Success]\n" +
                $"Source={lastDeploySource}\n" +
                $"SnapshotHash={snapshot.ContentHash}\n" +
                $"Parts={activeInstance.PartCount}\n" +
                $"Colliders={activeInstance.ColliderCount}\n" +
                $"Segments={activeInstance.VisualSegmentCount}",
                activeInstance);

            if (closeCanvasAfterDeploy &&
                sideCanvas != null &&
                sideCanvas.IsOpen)
            {
                sideCanvas.CloseSideCanvas();
            }

            RefreshHud();
        }

        private void ResolveSideCanvasReference()
        {
            sideCanvasResolveCount++;

            PrototypeLivingSideCanvasController[] candidates =
                UnityEngine.Object.FindObjectsByType<
                    PrototypeLivingSideCanvasController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            PrototypeLivingSideCanvasController best =
                null;

            int bestScore =
                int.MinValue;

            int openCount = 0;
            string bestReason =
                "No valid M45 Side Canvas controller found.";

            for (int index = 0;
                 index < candidates.Length;
                 index++)
            {
                PrototypeLivingSideCanvasController candidate =
                    candidates[index];

                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                int score = 0;
                var reasons =
                    new System.Collections.Generic.List<string>();

                if (candidate.isActiveAndEnabled)
                {
                    score += 5;
                    reasons.Add("Enabled");
                }

                if (candidate.IsOpen)
                {
                    score += 1000;
                    openCount++;
                    reasons.Add("Open");
                }

                if (candidate.CurrentMode ==
                    PrototypeSideCanvasMode.Preview)
                {
                    score += 250;
                    reasons.Add("Preview");
                }

                if (candidate.HasCompleteRig)
                {
                    score += 120;
                    reasons.Add("CompleteRig");
                }

                if (candidate.RigValid)
                {
                    score += 120;
                    reasons.Add("RigValid");
                }

                if (candidate.MarkerCount == 6)
                {
                    score += 80;
                    reasons.Add("Markers6");
                }

                if (candidate.StrokeCount > 0)
                {
                    score += 40;
                    reasons.Add("HasStrokes");
                }

                if (assembly != null &&
                    assembly.SideCanvasSource ==
                        candidate)
                {
                    score += 15;
                    reasons.Add("CurrentAssemblySource");
                }

                if (candidate == sideCanvas)
                {
                    score += 5;
                    reasons.Add("SerializedReference");
                }

                if (score <= bestScore)
                {
                    continue;
                }

                best = candidate;
                bestScore = score;
                bestReason =
                    $"Score={score} • " +
                    string.Join(
                        ",",
                        reasons) +
                    $" • {BuildHierarchyPath(candidate.transform)}";
            }

            openSideCanvasCandidateCount =
                openCount;

            if (best != sideCanvas)
            {
                sideCanvas = best;
                sideCanvasReferenceChangeCount++;
            }

            sideCanvasResolvedOpen =
                sideCanvas != null &&
                sideCanvas.IsOpen;

            sideCanvasSelectionReason =
                bestReason;
        }

        private static string BuildHierarchyPath(
            Transform target)
        {
            if (target == null)
            {
                return "NULL";
            }

            string path =
                target.name;

            while (target.parent != null)
            {
                target =
                    target.parent;

                path =
                    target.name +
                    "/" +
                    path;
            }

            return path;
        }

        private void ResolveAssemblyReference()
        {
            assemblyResolveCount++;

            PrototypeMasterpieceAssemblyController resolved =
                null;

            if (sideCanvas != null)
            {
                resolved =
                    sideCanvas.GetComponent<
                        PrototypeMasterpieceAssemblyController>();

                if (resolved == null)
                {
                    resolved =
                        sideCanvas.GetComponentInParent<
                            PrototypeMasterpieceAssemblyController>(
                                true);
                }
            }

            if (resolved == null)
            {
                PrototypeMasterpieceAssemblyController[] candidates =
                    UnityEngine.Object.FindObjectsByType<
                        PrototypeMasterpieceAssemblyController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                for (int index = 0;
                     index < candidates.Length;
                     index++)
                {
                    PrototypeMasterpieceAssemblyController candidate =
                        candidates[index];

                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.SideCanvasSource ==
                        sideCanvas)
                    {
                        resolved = candidate;

                        if (candidate.isActiveAndEnabled)
                        {
                            break;
                        }
                    }
                }
            }

            if (resolved != assembly)
            {
                assembly = resolved;
                assemblyReferenceChangeCount++;
            }

            assemblySourceMatchesSideCanvas =
                assembly != null &&
                assembly.SideCanvasSource ==
                    sideCanvas;
        }

        public void Recall()
        {
            if (!RequirePainterRole("Geri çağırma"))
            {
                return;
            }

            if (activeInstance == null)
            {
                lastAction =
                    "Geri çağrılacak aktif Baş Yapıt yok.";

                RefreshHud();
                return;
            }

            GameObject instanceObject =
                activeInstance.gameObject;

            activeInstance = null;
            recallCount++;
            lastAction =
                "Baş Yapıt tamamen geri çağrıldı.";

            Destroy(
                instanceObject);

            RefreshHud();
        }

        public void ToggleWorldProxyDebug()
        {
            if (!RequirePainterRole("Dünya proxy görünümü"))
            {
                return;
            }

            if (activeInstance == null)
            {
                lastAction =
                    "Proxy görünümü için aktif dünya instance'ı gerekli.";

                RefreshHud();
                return;
            }

            activeInstance.SetProxyDebugVisible(
                !activeInstance.ProxyDebugVisible);

            proxyToggleCount++;
            lastAction =
                activeInstance.ProxyDebugVisible
                    ? "Dünya proxy katmanı görünür."
                    : "Dünya proxy katmanı gizli.";

            RefreshHud();
        }

        private void Reject(string reason)
        {
            rejectedDeployCount++;
            lastAction =
                $"Deploy reddedildi: {reason}";

            Debug.LogWarning(
                "[M47 Deploy Rejected]\n" +
                $"Source={lastDeploySource}\n" +
                $"Reason={reason}\n" +
                $"AttemptCount={deployInputAttemptCount + directDeployAttemptCount}\n" +
                $"AnchorObstructions={(routeAnchor != null ? routeAnchor.LastObstructionCount : -1)}\n" +
                $"AnchorValidation={(routeAnchor != null ? routeAnchor.LastValidation : "N/A")}",
                this);

            RefreshHud();
        }

        private void RefreshHud()
        {
            if (statusGroup != null)
            {
                statusGroup.alpha = painterRoleActive ? 1f : 0f;
                statusGroup.interactable = false;
                statusGroup.blocksRaycasts = false;
            }

            if (!painterRoleActive)
            {
                if (sideCanvasFeedbackGroup != null)
                {
                    sideCanvasFeedbackGroup.alpha = 0f;
                    sideCanvasFeedbackGroup.interactable = false;
                    sideCanvasFeedbackGroup.blocksRaycasts = false;
                }

                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    activeInstance != null
                        ? "BAŞ YAPIT • DÜNYADA"
                        : "BAŞ YAPIT • DEPLOY HAZIRLIĞI";
            }

            if (stateText != null)
            {
                if (activeInstance != null)
                {
                    stateText.text =
                        $"PARÇA {activeInstance.PartCount}/6  " +
                        $"TRIGGER {activeInstance.ColliderCount}/6\n" +
                        $"SEGMENT {activeInstance.VisualSegmentCount}  " +
                        $"NOKTA {activeInstance.RenderedPointCount}\n" +
                        $"PROXY {(activeInstance.ProxyDebugVisible ? "AÇIK" : "GİZLİ")}  " +
                        $"HASH {activeInstance.SnapshotHash}\n" +
                        lastAction;
                }
                else
                {
                    bool assemblyReady =
                        assembly != null &&
                        assembly.AssemblyReady;

                    bool anchorClear =
                        routeAnchor != null &&
                        routeAnchor.LastPlacementClear;

                    stateText.text =
                        $"ASSEMBLY {(assemblyReady ? "HAZIR" : "BEKLİYOR")}  " +
                        $"ANCHOR {(anchorClear ? "TEMİZ" : "KONTROL BEKLİYOR")}\n" +
                        $"DEPLOY {deployCount}  RED {rejectedDeployCount}  " +
                        $"RECALL {recallCount}\n" +
                        lastAction;
                }
            }

            RefreshSideCanvasFeedback();

            if (controlsText != null)
            {
                controlsText.text =
                    "V • DEPLOY   X • GERİ ÇAĞIR   O • PROXY\n" +
                    "STATİK INSTANCE • TRIGGER COLLIDER • AI/HASAR YOK";
            }
        }

        private void RefreshSideCanvasFeedback()
        {
            bool visible =
                painterRoleActive &&
                sideCanvas != null &&
                sideCanvas.IsOpen;

            if (sideCanvasFeedbackGroup != null)
            {
                sideCanvasFeedbackGroup.alpha =
                    visible ? 1f : 0f;

                sideCanvasFeedbackGroup.interactable = false;
                sideCanvasFeedbackGroup.blocksRaycasts = false;
            }

            if (!visible ||
                sideCanvasFeedbackText == null)
            {
                return;
            }

            string headline;
            Color feedbackColor;

            if (activeInstance != null)
            {
                headline =
                    "DEPLOY BAŞARILI • X İLE GERİ ÇAĞIR";

                feedbackColor =
                    new Color(
                        0.10f,
                        0.82f,
                        0.58f,
                        1f);
            }
            else if (lastAction.StartsWith(
                         "Deploy reddedildi",
                         System.StringComparison.Ordinal))
            {
                headline =
                    lastAction.ToUpperInvariant();

                feedbackColor =
                    new Color(
                        1f,
                        0.34f,
                        0.12f,
                        1f);
            }
            else
            {
                bool ready =
                    sideCanvas != null &&
                    sideCanvas.IsOpen &&
                    assembly != null &&
                    assembly.AssemblyReady &&
                    assembly.DetachedPartCount == 0 &&
                    sideCanvas.RigValid &&
                    sideCanvas.CurrentMode ==
                        PrototypeSideCanvasMode.Preview;

                headline =
                    ready
                        ? "V • DEPLOY HAZIR"
                        : "DEPLOY BEKLİYOR • " +
                          lastAssemblyPreparation.ToUpperInvariant();

                feedbackColor =
                    ready
                        ? new Color(
                            1f,
                            0.54f,
                            0.06f,
                            1f)
                        : new Color(
                            0.94f,
                            0.90f,
                            0.80f,
                            1f);
            }

            sideCanvasFeedbackText.color =
                feedbackColor;

            sideCanvasFeedbackText.text =
                headline +
                "\\n" +
                $"INPUT {(deployInputActionsActive ? "AKTİF" : "KAPALI")}  " +
                $"V DENEME {deployInputAttemptCount}  " +
                $"DİREKT {directDeployAttemptCount}  " +
                $"RED {rejectedDeployCount}";
        }

        private void OnDestroy()
        {
            DisableInputActions();

            if (deployAction != null)
            {
                deployAction.performed -=
                    HandleDeployAction;

                deployAction.Dispose();
                deployAction = null;
            }

            if (recallAction != null)
            {
                recallAction.performed -=
                    HandleRecallAction;

                recallAction.Dispose();
                recallAction = null;
            }

            if (proxyAction != null)
            {
                proxyAction.performed -=
                    HandleProxyAction;

                proxyAction.Dispose();
                proxyAction = null;
            }
        }

        private void OnDisable()
        {
            DisableInputActions();

            if (activeInstance != null)
            {
                GameObject instanceObject =
                    activeInstance.gameObject;

                activeInstance = null;
                Destroy(
                    instanceObject);
            }
        }
    }
}
