using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using PaintedAlive.Core.Prototypes;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Environment.Palimpsest;
using PaintedAlive.Figures;
using PaintedAlive.MatchFlow;
using PaintedAlive.Paint;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using PaintedAlive.Painters;
using PaintedAlive.Painters.Ink;
using PaintedAlive.Painters.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Networking.M56
{
    /// <summary>
    /// M56 friend-test listen-server foundation.
    ///
    /// Scope is intentionally 1 Figure + 1 Painter on a player-hosted Steam
    /// relay session. Roles are server-owned and swap atomically. Figure pose,
    /// Painter world actions, oil strokes and Ink creature spawn commands are
    /// mirrored so the two real PCs can exercise the vertical slice together.
    ///
    /// This is the M56 transport/role/movement foundation, not the final M57
    /// prediction/reconciliation or late-join persistent gameplay authority.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class M56FriendSession : MonoBehaviour,
        IPaintedAliveNetworkRoleRouter,
        IPaintedAliveNetworkGameplayRouter
    {
        private const int NoClient = -1;
        private const float FigureSendInterval = 1f / 30f;
        private const float PainterPresenceSendInterval = 1f / 12f;
        private const float FigureHardReconcileDistance = 2.5f;
        private const float RemoteFigureInterpolationSharpness = 18f;
        private const float MaximumSnapshotJump = 25f;
        private const int MaximumStrokePoints = 96;
        private const float FullRoleIntroDuration = 5.25f;
        private const float ShortRoleConfirmationDuration = 1.25f;
        private const float MinimumManualDismissDelay = 0.3f;

        private enum RolePresentation
        {
            None,
            FullIntro,
            ShortConfirmation
        }

        [Header("Network")]
        [SerializeField] private NetworkManager networkManager;

        [Header("Existing Painted Alive Runtime")]
        [SerializeField] private InkPainterRoleAuthority roleAuthority;
        [SerializeField] private FigureMotor figureMotor;
        [SerializeField] private PainterBrushController painterBrush;
        [SerializeField] private InkPainterNestController nestController;
        [SerializeField] private PrototypeCoreMatchController coreMatch;
        [SerializeField] private PrototypeMatchController prototypeMatch;

        [Header("Friend Test")]
        [SerializeField] private bool resetRoundWhenSecondPlayerJoins = true;
        [SerializeField] private bool showConnectionHud = true;
        [SerializeField] private string joinSteamId = string.Empty;

        [Header("Runtime - Read Only")]
        [SerializeField] private bool localAuthenticated;
        [SerializeField] private int localClientId = NoClient;
        [SerializeField] private int figureClientId = NoClient;
        [SerializeField] private int painterClientId = NoClient;
        [SerializeField] private int sessionGeneration;
        [SerializeField] private int roleRevision;
        [SerializeField] private uint actionRevision;
        [SerializeField] private string lastRoleFeedback = "Offline";
        [SerializeField] private string lastGameplayFeedback = "Offline";
        [SerializeField] private string connectionStatus = "Offline";
        [SerializeField] private string lastNetworkEvent = "Offline";
        [SerializeField] private string figurePlayerName = "—";
        [SerializeField] private string painterPlayerName = "—";

        [Header("Local M56 Presentation - Read Only")]
        [SerializeField] private bool developmentMenuOpen = true;
        [SerializeField] private bool hasSeenFigureIntro;
        [SerializeField] private bool hasSeenPainterIntro;
        [SerializeField] private RolePresentation rolePresentation;
        [SerializeField] private PaintedAliveLocalRole presentationRole;

        private readonly Dictionary<int, ulong> steamIds = new();
        private readonly Dictionary<int, string> playerNames = new();

        private float nextFigureSendAt;
        private float nextPainterPresenceSendAt;
        private uint localFigureSequence;
        private uint localPainterPresenceSequence;
        private uint serverFigureSequence;
        private uint localGameplaySequence;
        private uint localActionRequestId;
        private bool serverHasFigurePose;
        private Vector3 canonicalFigurePosition;
        private Quaternion canonicalFigureRotation = Quaternion.identity;
        private Vector3 canonicalFigureVelocity;
        private bool hasRemoteFigureTarget;
        private Vector3 remoteFigureTargetPosition;
        private Quaternion remoteFigureTargetRotation = Quaternion.identity;
        private Vector3 remoteFigureTargetVelocity;
        private bool hasRemotePainterPresence;
        private Vector3 remotePainterPosition;
        private Vector3 remotePainterFocusPoint;
        private Vector3 remotePainterForward = Vector3.forward;
        private bool remotePainterPainting;
        private float remotePainterPresenceReceivedAt;
        private bool registered;
        private bool twoPlayersWereReady;
        private bool hasAuthoritativeLocalRole;
        private PaintedAliveLocalRole authoritativeLocalRole;
        private float rolePresentationOpenedAt;
        private float rolePresentationEndsAt;
        private bool inputReleasePending;
        private int inputReleaseStartedFrame;
        private bool fullscreenStartupApplied;
        private bool closeMenuAfterPresentation;
        private int windowedWidth;
        private int windowedHeight;

        public bool NetworkSessionActive =>
            localAuthenticated &&
            networkManager != null &&
            networkManager.ClientManager != null &&
            networkManager.ClientManager.Started;

        public bool Connected => NetworkSessionActive;
        public bool GameplayInputSuppressed =>
            showConnectionHud &&
            (developmentMenuOpen ||
             rolePresentation != RolePresentation.None ||
             inputReleasePending);
        public int LocalClientId => localClientId;
        public int RoleRevision => roleRevision;
        public int SessionGeneration => sessionGeneration;
        public int ConnectedPlayerCount =>
            (figureClientId == NoClient ? 0 : 1) +
            (painterClientId == NoClient ? 0 : 1);
        public string LastRoleFeedback => lastRoleFeedback;
        public string LastGameplayFeedback => lastGameplayFeedback;

        public bool IsLocalFigure =>
            NetworkSessionActive &&
            localClientId == figureClientId;

        public bool IsLocalPainter =>
            NetworkSessionActive &&
            localClientId == painterClientId;

        public void Configure(
            NetworkManager manager,
            InkPainterRoleAuthority authority,
            FigureMotor figure,
            PainterBrushController brush,
            InkPainterNestController nest,
            PrototypeCoreMatchController core,
            PrototypeMatchController prototype)
        {
            networkManager = manager;
            roleAuthority = authority;
            figureMotor = figure;
            painterBrush = brush;
            nestController = nest;
            coreMatch = core;
            prototypeMatch = prototype;
        }

        private void Awake()
        {
            PaintedAliveNetworkRoleBridge.Register(this);
            PaintedAliveNetworkGameplayBridge.Register(this);

            if (networkManager == null)
                networkManager = GetComponent<NetworkManager>();

            roleAuthority ??= InkPainterRoleAuthority.ActiveInstance;
            figureMotor ??= FindFirstObjectByType<FigureMotor>(
                FindObjectsInactive.Include);
            painterBrush ??= FindFirstObjectByType<PainterBrushController>(
                FindObjectsInactive.Include);
            nestController ??= FindFirstObjectByType<InkPainterNestController>(
                FindObjectsInactive.Include);
            coreMatch ??= FindFirstObjectByType<PrototypeCoreMatchController>(
                FindObjectsInactive.Include);
            prototypeMatch ??= FindFirstObjectByType<PrototypeMatchController>(
                FindObjectsInactive.Include);

            if (figureMotor != null)
            {
                canonicalFigurePosition = figureMotor.transform.position;
                canonicalFigureRotation = figureMotor.transform.rotation;
                canonicalFigureVelocity = figureMotor.Velocity;
                serverHasFigurePose = true;
            }

            joinSteamId = PlayerPrefs.GetString(
                "PaintedAlive.M56.JoinSteamId",
                joinSteamId ?? string.Empty);

            developmentMenuOpen = true;
            rolePresentation = RolePresentation.None;
            inputReleasePending = false;
            ApplyPresentationCursorState();
            Debug.Log("[M56 UI] Offline menu opened", this);
        }

        private void Start()
        {
            ApplyFriendFullscreenStartup();
            RegisterNetworkCallbacks();
            RefreshConnectionStatus();

            if (Debug.isDebugBuild &&
                Array.Exists(
                    System.Environment.GetCommandLineArgs(),
                    argument => string.Equals(
                        argument,
                        "-m56-smoke-host",
                        StringComparison.OrdinalIgnoreCase)))
            {
                StartCoroutine(RunDevelopmentSmokeHost());
            }
        }

        private IEnumerator RunDevelopmentSmokeHost()
        {
            yield return null;
            yield return null;

            bool startupUiReady =
                developmentMenuOpen &&
                GameplayInputSuppressed &&
                Cursor.lockState == CursorLockMode.None &&
                Cursor.visible;

            if (!startupUiReady)
            {
                Debug.LogError(
                    "[M56 UX Smoke] FAIL_STARTUP | " +
                    $"Menu={developmentMenuOpen} Suppressed={GameplayInputSuppressed} " +
                    $"Cursor={Cursor.lockState}/{Cursor.visible}",
                    this);
                Application.Quit(4);
                yield break;
            }

            Debug.Log(
                "[M56 UX Smoke] STARTUP_PASS | " +
                "Menu=True Cursor=None/Visible GameplaySuppressed=True",
                this);

            float steamDeadline = Time.realtimeSinceStartup + 15f;
            while (!M56SteamBootstrap.Ready &&
                   Time.realtimeSinceStartup < steamDeadline)
            {
                yield return null;
            }

            if (!M56SteamBootstrap.Ready)
            {
                Debug.LogError(
                    "[M56 Smoke] FAIL | Steam=" +
                    M56SteamBootstrap.LastError,
                    this);
                Application.Quit(2);
                yield break;
            }

            StartHost();

            float hostDeadline = Time.realtimeSinceStartup + 15f;
            while ((!NetworkSessionActive ||
                    !IsLocalFigure ||
                    !hasAuthoritativeLocalRole ||
                    authoritativeLocalRole != PaintedAliveLocalRole.Figure) &&
                   Time.realtimeSinceStartup < hostDeadline)
            {
                yield return null;
            }

            if (NetworkSessionActive && IsLocalFigure)
            {
                bool authoritativeIntroReady =
                    hasAuthoritativeLocalRole &&
                    authoritativeLocalRole == PaintedAliveLocalRole.Figure &&
                    rolePresentation == RolePresentation.FullIntro &&
                    GameplayInputSuppressed;

                if (!authoritativeIntroReady)
                {
                    Debug.LogError(
                        "[M56 UX Smoke] FAIL_ROLE_INTRO | " +
                        $"Authoritative={hasAuthoritativeLocalRole} " +
                        $"Role={authoritativeLocalRole} " +
                        $"Presentation={rolePresentation} " +
                        $"Suppressed={GameplayInputSuppressed}",
                        this);
                    Application.Quit(5);
                    yield break;
                }

                Debug.Log(
                    "[M56 UX Smoke] FIGURE_INTRO_PASS | " +
                    "Source=AuthoritativeRoleState GameplaySuppressed=True",
                    this);

                float presentationDeadline =
                    Time.realtimeSinceStartup +
                    FullRoleIntroDuration + 3f;
                while (GameplayInputSuppressed &&
                       Time.realtimeSinceStartup < presentationDeadline)
                {
                    yield return null;
                }

                // The suppression flag clears in Update. Give role LateUpdate
                // and IMGUI one full frame to restore their final cursor mode
                // before sampling it for the smoke verdict.
                yield return new WaitForEndOfFrame();
                yield return null;

                bool cursorRestored =
                    !Application.isFocused ||
                    (Cursor.lockState == CursorLockMode.Locked &&
                     !Cursor.visible);
                bool gameplayReady =
                    !developmentMenuOpen &&
                    rolePresentation == RolePresentation.None &&
                    !GameplayInputSuppressed &&
                    cursorRestored;

                if (!gameplayReady)
                {
                    Debug.LogError(
                        "[M56 UX Smoke] FAIL_GAMEPLAY_TRANSITION | " +
                        $"Menu={developmentMenuOpen} " +
                        $"Presentation={rolePresentation} " +
                        $"Suppressed={GameplayInputSuppressed} " +
                        $"Focused={Application.isFocused} " +
                        $"Cursor={Cursor.lockState}/{Cursor.visible}",
                        this);
                    Application.Quit(6);
                    yield break;
                }

                Debug.Log(
                    "[M56 UX Smoke] GAMEPLAY_PASS | " +
                    "Menu=False Presentation=None GameplaySuppressed=False " +
                    $"Cursor={Cursor.lockState}/{Cursor.visible}",
                    this);
                Debug.Log(
                    "[M56 Smoke] PASS | " +
                    $"SteamID64={M56SteamBootstrap.LocalSteamId} " +
                    $"Client={localClientId} Role=Figure " +
                    $"Revision={roleRevision} Generation={sessionGeneration}",
                    this);
                Application.Quit(0);
            }
            else
            {
                Debug.LogError(
                    "[M56 Smoke] FAIL | " +
                    $"Status={connectionStatus} Event={lastNetworkEvent}",
                    this);
                Application.Quit(3);
            }
        }

        private void OnDestroy()
        {
            UnregisterNetworkCallbacks();
            PaintedAliveNetworkRoleBridge.Unregister(this);
            PaintedAliveNetworkGameplayBridge.Unregister(this);
        }

        private void Update()
        {
            RefreshConnectionStatus();
            UpdateLocalPresentation();
            UpdateRemoteFigureInterpolation();

            if (!NetworkSessionActive || roleAuthority == null)
            {
                return;
            }

            if (IsLocalPainter &&
                roleAuthority.CurrentRole == PaintedAliveLocalRole.InkPainter)
            {
                PublishPainterPresenceIfDue();
            }

            if (!IsLocalFigure ||
                figureMotor == null ||
                roleAuthority.CurrentRole != PaintedAliveLocalRole.Figure)
            {
                return;
            }

            if (Time.unscaledTime < nextFigureSendAt)
                return;

            nextFigureSendAt =
                Time.unscaledTime + FigureSendInterval;

            localFigureSequence++;

            M56FigureStateBroadcast snapshot =
                new M56FigureStateBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    Sequence = localFigureSequence,
                    RoleRevision = roleRevision,
                    OriginClientId = localClientId,
                    Position = figureMotor.transform.position,
                    Rotation = figureMotor.transform.rotation,
                    Velocity = figureMotor.Velocity
                };

            networkManager.ClientManager.Broadcast(
                snapshot,
                Channel.Unreliable);
        }

        private void LateUpdate()
        {
            ApplyPresentationCursorState();
        }

        public void RequestRole(PaintedAliveLocalRole desiredRole)
        {
            if (!NetworkSessionActive)
            {
                lastRoleFeedback =
                    "Role request ignored: network session active değil.";
                return;
            }

            M56RoleRequestBroadcast request =
                new M56RoleRequestBroadcast
                {
                    DesiredRole = (byte)desiredRole,
                    KnownRevision = roleRevision
                };

            lastRoleFeedback =
                "Role request → " + desiredRole;
            lastNetworkEvent =
                $"REQUEST role={desiredRole} knownRevision={roleRevision}";

            Debug.Log(
                "[M56 Role] REQUEST | " +
                $"Client={localClientId} Desired={desiredRole} " +
                $"KnownRevision={roleRevision}",
                this);

            networkManager.ClientManager.Broadcast(request);
        }

        public bool RequestWorldAction(string systemReference)
        {
            if (!NetworkSessionActive)
            {
                lastGameplayFeedback =
                    "World action: network session active değil.";
                return false;
            }

            if (!IsLocalPainter)
            {
                lastGameplayFeedback =
                    "World action blocked: server role Painter değil.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(systemReference))
            {
                lastGameplayFeedback =
                    "World action blocked: SystemReference boş.";
                return false;
            }

            localActionRequestId++;

            networkManager.ClientManager.Broadcast(
                new M56WorldActionRequestBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    RequestId = localActionRequestId,
                    KnownRoleRevision = roleRevision,
                    SystemReference = systemReference
                });

            lastGameplayFeedback =
                "Server validation → " + systemReference;
            return true;
        }

        public void PublishLocalOilStroke(
            int networkStrokeId,
            Vector3[] points,
            OilStrokeShape shape,
            OilStrokePressureProfile profile)
        {
            if (!NetworkSessionActive || !IsLocalPainter)
                return;

            if (points == null || points.Length < 2)
                return;

            localGameplaySequence++;

            networkManager.ClientManager.Broadcast(
                new M56OilStrokeBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    Sequence = localGameplaySequence,
                    RoleRevision = roleRevision,
                    OriginClientId = localClientId,
                    StrokeId = networkStrokeId,
                    Shape = (int)shape,
                    Points = points,
                    DrawSpeed = profile.AverageDrawSpeed,
                    Pressure = profile.PressureNormalized,
                    Width = profile.WidthMultiplier,
                    Height = profile.HeightMultiplier,
                    Pigment = profile.PigmentMultiplier,
                    CutResistance = profile.CutResistanceMultiplier,
                    LifecycleDuration = profile.LifecycleDurationMultiplier,
                    Budget = profile.BudgetMultiplier
                });
        }

        public void PublishLocalOilCut(
            int networkStrokeId,
            Vector3 point,
            float gapWidth)
        {
            if (!NetworkSessionActive ||
                !IsLocalFigure ||
                networkStrokeId <= 0)
            {
                return;
            }

            localGameplaySequence++;
            networkManager.ClientManager.Broadcast(
                new M56OilCutBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    Sequence = localGameplaySequence,
                    RoleRevision = roleRevision,
                    OriginClientId = localClientId,
                    StrokeId = networkStrokeId,
                    Point = point,
                    GapWidth = gapWidth
                });
        }

        public void PublishLocalOilStrokeClear()
        {
            if (!NetworkSessionActive || !IsLocalPainter)
                return;

            localGameplaySequence++;

            networkManager.ClientManager.Broadcast(
                new M56OilClearBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    Sequence = localGameplaySequence,
                    RoleRevision = roleRevision,
                    OriginClientId = localClientId
                });
        }

        public void PublishLocalOilPreview(
            uint previewId,
            bool visible,
            Vector3[] points,
            OilStrokeShape shape,
            float width)
        {
            if (!NetworkSessionActive || !IsLocalPainter)
                return;

            localGameplaySequence++;

            networkManager.ClientManager.Broadcast(
                new M56OilPreviewBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    Sequence = localGameplaySequence,
                    RoleRevision = roleRevision,
                    OriginClientId = localClientId,
                    PreviewId = previewId,
                    Visible = visible,
                    Shape = (int)shape,
                    Points = points,
                    Width = width
                },
                visible ? Channel.Unreliable : Channel.Reliable);
        }

        private void PublishPainterPresenceIfDue()
        {
            if (Time.unscaledTime < nextPainterPresenceSendAt ||
                painterBrush == null ||
                !painterBrush.TryGetNetworkPresence(
                    out Vector3 position,
                    out Vector3 focusPoint,
                    out Vector3 forward,
                    out bool painting))
            {
                return;
            }

            nextPainterPresenceSendAt =
                Time.unscaledTime + PainterPresenceSendInterval;
            localPainterPresenceSequence++;

            networkManager.ClientManager.Broadcast(
                new M56PainterPresenceBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    Sequence = localPainterPresenceSequence,
                    RoleRevision = roleRevision,
                    OriginClientId = localClientId,
                    Position = position,
                    FocusPoint = focusPoint,
                    Forward = forward,
                    Painting = painting
                },
                Channel.Unreliable);
        }

        public void PublishLocalInkCreature(
            InkGlyphLoadoutId loadoutId,
            Vector3 point,
            Vector3 normal,
            Vector3 facing)
        {
            if (!NetworkSessionActive || !IsLocalPainter)
                return;

            localGameplaySequence++;

            networkManager.ClientManager.Broadcast(
                new M56InkCreatureBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    Sequence = localGameplaySequence,
                    RoleRevision = roleRevision,
                    OriginClientId = localClientId,
                    LoadoutId = (int)loadoutId,
                    Point = point,
                    Normal = normal,
                    Facing = facing
                });
        }

        public void StartHost()
        {
            OpenDevelopmentMenu("Host requested");

            if (!CanStartSteamConnection())
                return;

            if (networkManager.ServerManager.Started ||
                networkManager.ClientManager.Started)
            {
                connectionStatus = "Network already started.";
                return;
            }

            networkManager.TransportManager.Transport.SetMaximumClients(2);

            bool serverStarted =
                networkManager.ServerManager.StartConnection();

            bool clientStarted =
                serverStarted &&
                networkManager.ClientManager.StartConnection();

            connectionStatus =
                serverStarted && clientStarted
                    ? "HOST starting via Steam Relay…"
                    : "HOST start failed. Console'u kontrol et.";
            lastNetworkEvent = connectionStatus;

            Debug.Log("[M56 UI] Host requested", this);

            if (!serverStarted || !clientStarted)
            {
                if (networkManager.ClientManager.Started)
                    networkManager.ClientManager.StopConnection();

                if (networkManager.ServerManager.Started)
                    networkManager.ServerManager.StopConnection(true);

                ResetLocalNetworkState(
                    "HOST start failed. Menüden tekrar deneyebilirsin.");
                OpenDevelopmentMenu("Host startup failed");
            }
        }

        public void JoinHost()
        {
            OpenDevelopmentMenu("Join requested");

            if (!CanStartSteamConnection())
                return;

            if (networkManager.ClientManager.Started ||
                networkManager.ServerManager.Started)
            {
                connectionStatus = "Network already started.";
                return;
            }

            string address = (joinSteamId ?? string.Empty).Trim();

            if (!ulong.TryParse(address, out ulong hostId) ||
                hostId == 0)
            {
                connectionStatus = "Geçerli Host SteamID64 gir.";
                return;
            }

            if (hostId == M56SteamBootstrap.LocalSteamId)
            {
                connectionStatus =
                    "Kendi SteamID'ne bağlanamazsın. İki PC + iki Steam hesabı gerekli.";
                return;
            }

            PlayerPrefs.SetString(
                "PaintedAlive.M56.JoinSteamId",
                address);
            PlayerPrefs.Save();

            networkManager.TransportManager.Transport.SetMaximumClients(2);
            networkManager.TransportManager.Transport.SetClientAddress(address);

            bool clientStarted =
                networkManager.ClientManager.StartConnection();

            connectionStatus = clientStarted
                ? "CLIENT connecting → " + address
                : "CLIENT start failed. Console'u kontrol et.";
            lastNetworkEvent = connectionStatus;

            Debug.Log("[M56 UI] Join requested", this);

            if (!clientStarted)
                OpenDevelopmentMenu("Join startup failed");
        }

        public void Disconnect()
        {
            if (networkManager == null)
                return;

            if (networkManager.ClientManager != null &&
                networkManager.ClientManager.Started)
            {
                networkManager.ClientManager.StopConnection();
            }

            if (networkManager.ServerManager != null &&
                networkManager.ServerManager.Started)
            {
                networkManager.ServerManager.StopConnection(true);
            }

            ResetLocalNetworkState("Disconnected");
        }

        private bool CanStartSteamConnection()
        {
            if (networkManager == null ||
                networkManager.TransportManager == null ||
                networkManager.TransportManager.Transport == null)
            {
                connectionStatus =
                    "M56 NetworkManager/Transport eksik. M56 Setup çalıştır.";
                return false;
            }

            if (!M56SteamBootstrap.Ready)
            {
                connectionStatus =
                    "Steam hazır değil: " + M56SteamBootstrap.LastError;
                return false;
            }

            return true;
        }

        private void RegisterNetworkCallbacks()
        {
            if (registered || networkManager == null)
                return;

            if (networkManager.ClientManager == null ||
                networkManager.ServerManager == null)
            {
                Debug.LogError(
                    "[M56] FishNet managers henüz initialize olmadı.",
                    this);
                return;
            }

            networkManager.ClientManager.OnAuthenticated +=
                Client_OnAuthenticated;
            networkManager.ClientManager.OnClientConnectionState +=
                Client_OnConnectionState;
            networkManager.ServerManager.OnRemoteConnectionState +=
                Server_OnRemoteConnectionState;

            networkManager.ServerManager.RegisterBroadcast<M56HelloBroadcast>(
                Server_OnHello);
            networkManager.ServerManager.RegisterBroadcast<M56RoleRequestBroadcast>(
                Server_OnRoleRequest);
            networkManager.ServerManager.RegisterBroadcast<M56FigureStateBroadcast>(
                Server_OnFigureState);
            networkManager.ServerManager.RegisterBroadcast<M56WorldActionRequestBroadcast>(
                Server_OnWorldActionRequest);
            networkManager.ServerManager.RegisterBroadcast<M56OilStrokeBroadcast>(
                Server_OnOilStroke);
            networkManager.ServerManager.RegisterBroadcast<M56OilPreviewBroadcast>(
                Server_OnOilPreview);
            networkManager.ServerManager.RegisterBroadcast<M56OilCutBroadcast>(
                Server_OnOilCut);
            networkManager.ServerManager.RegisterBroadcast<M56PainterPresenceBroadcast>(
                Server_OnPainterPresence);
            networkManager.ServerManager.RegisterBroadcast<M56OilClearBroadcast>(
                Server_OnOilClear);
            networkManager.ServerManager.RegisterBroadcast<M56InkCreatureBroadcast>(
                Server_OnInkCreature);

            networkManager.ClientManager.RegisterBroadcast<M56RoleStateBroadcast>(
                Client_OnRoleState);
            networkManager.ClientManager.RegisterBroadcast<M56FigureStateBroadcast>(
                Client_OnFigureState);
            networkManager.ClientManager.RegisterBroadcast<M56WorldActionCommitBroadcast>(
                Client_OnWorldActionCommit);
            networkManager.ClientManager.RegisterBroadcast<M56GameplayFeedbackBroadcast>(
                Client_OnGameplayFeedback);
            networkManager.ClientManager.RegisterBroadcast<M56OilStrokeBroadcast>(
                Client_OnOilStroke);
            networkManager.ClientManager.RegisterBroadcast<M56OilPreviewBroadcast>(
                Client_OnOilPreview);
            networkManager.ClientManager.RegisterBroadcast<M56OilCutBroadcast>(
                Client_OnOilCut);
            networkManager.ClientManager.RegisterBroadcast<M56PainterPresenceBroadcast>(
                Client_OnPainterPresence);
            networkManager.ClientManager.RegisterBroadcast<M56OilClearBroadcast>(
                Client_OnOilClear);
            networkManager.ClientManager.RegisterBroadcast<M56InkCreatureBroadcast>(
                Client_OnInkCreature);
            networkManager.ClientManager.RegisterBroadcast<M56RoundResetBroadcast>(
                Client_OnRoundReset);

            registered = true;
        }

        private void UnregisterNetworkCallbacks()
        {
            if (!registered || networkManager == null ||
                networkManager.ClientManager == null ||
                networkManager.ServerManager == null)
            {
                return;
            }

            networkManager.ClientManager.OnAuthenticated -=
                Client_OnAuthenticated;
            networkManager.ClientManager.OnClientConnectionState -=
                Client_OnConnectionState;
            networkManager.ServerManager.OnRemoteConnectionState -=
                Server_OnRemoteConnectionState;

            networkManager.ServerManager.UnregisterBroadcast<M56HelloBroadcast>(
                Server_OnHello);
            networkManager.ServerManager.UnregisterBroadcast<M56RoleRequestBroadcast>(
                Server_OnRoleRequest);
            networkManager.ServerManager.UnregisterBroadcast<M56FigureStateBroadcast>(
                Server_OnFigureState);
            networkManager.ServerManager.UnregisterBroadcast<M56WorldActionRequestBroadcast>(
                Server_OnWorldActionRequest);
            networkManager.ServerManager.UnregisterBroadcast<M56OilStrokeBroadcast>(
                Server_OnOilStroke);
            networkManager.ServerManager.UnregisterBroadcast<M56OilPreviewBroadcast>(
                Server_OnOilPreview);
            networkManager.ServerManager.UnregisterBroadcast<M56OilCutBroadcast>(
                Server_OnOilCut);
            networkManager.ServerManager.UnregisterBroadcast<M56PainterPresenceBroadcast>(
                Server_OnPainterPresence);
            networkManager.ServerManager.UnregisterBroadcast<M56OilClearBroadcast>(
                Server_OnOilClear);
            networkManager.ServerManager.UnregisterBroadcast<M56InkCreatureBroadcast>(
                Server_OnInkCreature);

            networkManager.ClientManager.UnregisterBroadcast<M56RoleStateBroadcast>(
                Client_OnRoleState);
            networkManager.ClientManager.UnregisterBroadcast<M56FigureStateBroadcast>(
                Client_OnFigureState);
            networkManager.ClientManager.UnregisterBroadcast<M56WorldActionCommitBroadcast>(
                Client_OnWorldActionCommit);
            networkManager.ClientManager.UnregisterBroadcast<M56GameplayFeedbackBroadcast>(
                Client_OnGameplayFeedback);
            networkManager.ClientManager.UnregisterBroadcast<M56OilStrokeBroadcast>(
                Client_OnOilStroke);
            networkManager.ClientManager.UnregisterBroadcast<M56OilPreviewBroadcast>(
                Client_OnOilPreview);
            networkManager.ClientManager.UnregisterBroadcast<M56OilCutBroadcast>(
                Client_OnOilCut);
            networkManager.ClientManager.UnregisterBroadcast<M56PainterPresenceBroadcast>(
                Client_OnPainterPresence);
            networkManager.ClientManager.UnregisterBroadcast<M56OilClearBroadcast>(
                Client_OnOilClear);
            networkManager.ClientManager.UnregisterBroadcast<M56InkCreatureBroadcast>(
                Client_OnInkCreature);
            networkManager.ClientManager.UnregisterBroadcast<M56RoundResetBroadcast>(
                Client_OnRoundReset);

            registered = false;
        }

        private void Client_OnAuthenticated()
        {
            localAuthenticated = true;
            localClientId = networkManager.ClientManager.Connection.ClientId;
            connectionStatus =
                "Authenticated • Client " + localClientId;
            lastNetworkEvent = connectionStatus;

            networkManager.ClientManager.Broadcast(
                new M56HelloBroadcast
                {
                    SteamId = M56SteamBootstrap.LocalSteamId,
                    PersonaName = M56SteamBootstrap.PersonaName
                });

            Debug.Log(
                "[M56] Client authenticated | " +
                $"ClientId={localClientId} | " +
                $"SteamID64={M56SteamBootstrap.LocalSteamId}",
                this);
        }

        private void Client_OnConnectionState(
            ClientConnectionStateArgs args)
        {
            connectionStatus =
                "Client state: " + args.ConnectionState;
            lastNetworkEvent = connectionStatus;

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                ResetLocalNetworkState("Client disconnected");
            }
        }

        private void Server_OnRemoteConnectionState(
            NetworkConnection connection,
            RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped)
                return;

            int clientId = connection.ClientId;
            lastNetworkEvent =
                "Remote client " + clientId + " disconnected";
            bool changed = false;

            steamIds.Remove(clientId);
            playerNames.Remove(clientId);

            if (figureClientId == clientId)
            {
                figureClientId = NoClient;
                changed = true;
            }

            if (painterClientId == clientId)
            {
                painterClientId = NoClient;
                changed = true;
            }

            // Keep the remaining player usable after a peer leaves.
            int remaining = GetAnyHelloClientId();
            if (remaining != NoClient)
            {
                if (figureClientId == NoClient && painterClientId == remaining)
                {
                    figureClientId = remaining;
                    painterClientId = NoClient;
                    changed = true;
                }
            }

            twoPlayersWereReady = false;

            if (changed)
            {
                roleRevision++;
                serverFigureSequence = 0;
                BroadcastRoleState("Peer disconnected");
            }
        }

        private void Server_OnHello(
            NetworkConnection sender,
            M56HelloBroadcast message,
            Channel channel)
        {
            int clientId = sender.ClientId;
            lastNetworkEvent =
                "Hello client=" + clientId + " steam=" + message.SteamId;
            steamIds[clientId] = message.SteamId;
            playerNames[clientId] =
                string.IsNullOrWhiteSpace(message.PersonaName)
                    ? "Client " + clientId
                    : message.PersonaName;

            bool changed = false;

            if (figureClientId == NoClient)
            {
                figureClientId = clientId;
                changed = true;
            }
            else if (clientId != figureClientId &&
                     painterClientId == NoClient)
            {
                painterClientId = clientId;
                changed = true;
            }

            if (changed)
            {
                roleRevision++;
                serverFigureSequence = 0;
            }

            bool nowReady =
                figureClientId != NoClient &&
                painterClientId != NoClient;

            bool becameReady = nowReady && !twoPlayersWereReady;

            twoPlayersWereReady = nowReady;
            BroadcastRoleState(
                nowReady
                    ? "1v1 ready"
                    : "Waiting for second player");

            // Role ownership/revision must reach clients before a round-reset
            // packet using that revision, otherwise the joiner correctly rejects
            // the reset as stale/newer-than-known.
            if (becameReady && resetRoundWhenSecondPlayerJoins)
            {
                ServerResetRound();
            }
        }

        private void Server_OnRoleRequest(
            NetworkConnection sender,
            M56RoleRequestBroadcast message,
            Channel channel)
        {
            int requester = sender.ClientId;

            Debug.Log(
                "[M56 Role] REQUEST | " +
                $"Client={requester} Desired={(PaintedAliveLocalRole)message.DesiredRole} " +
                $"KnownRevision={message.KnownRevision} ServerRevision={roleRevision}",
                this);

            if (requester != figureClientId &&
                requester != painterClientId)
            {
                SendGameplayFeedback(
                    sender,
                    0,
                    false,
                    "Role request rejected: assigned session player değilsin.");
                return;
            }

            if (message.KnownRevision != roleRevision)
            {
                lastNetworkEvent =
                    $"REJECT_STALE client={requester} known={message.KnownRevision} current={roleRevision}";
                Debug.LogWarning(
                    "[M56 Role] REJECT_STALE | " +
                    $"Client={requester} KnownRevision={message.KnownRevision} " +
                    $"ServerRevision={roleRevision}",
                    this);
                SendGameplayFeedback(
                    sender,
                    0,
                    false,
                    "Role request stale; current revision " + roleRevision);
                SendRoleState(sender, "Stale role request resync");
                return;
            }

            PaintedAliveLocalRole desired =
                message.DesiredRole == (byte)PaintedAliveLocalRole.InkPainter
                    ? PaintedAliveLocalRole.InkPainter
                    : PaintedAliveLocalRole.Figure;

            bool changed = false;

            if (desired == PaintedAliveLocalRole.Figure)
            {
                if (requester != figureClientId)
                {
                    int previousFigure = figureClientId;
                    figureClientId = requester;
                    painterClientId = previousFigure;
                    changed = true;
                }
            }
            else
            {
                if (requester != painterClientId)
                {
                    int previousPainter = painterClientId;
                    painterClientId = requester;
                    figureClientId = previousPainter;
                    changed = true;
                }
            }

            if (changed)
            {
                Debug.Log(
                    "[M56 Role] ACCEPT | " +
                    $"Client={requester} Desired={desired} Revision={roleRevision}",
                    this);
                roleRevision++;
                serverFigureSequence = 0;
                CaptureCanonicalFigureFromScene();
                lastNetworkEvent =
                    $"COMMIT r{roleRevision} Figure={figureClientId} Painter={painterClientId}";
                Debug.Log(
                    "[M56 Role] COMMIT | " +
                    $"Revision={roleRevision} Figure={figureClientId} " +
                    $"Painter={painterClientId}",
                    this);
                BroadcastRoleState(
                    "Atomic role swap requested by client " + requester);
            }
            else
            {
                SendRoleState(
                    sender,
                    "Requested role already active");
            }
        }

        private void Server_OnFigureState(
            NetworkConnection sender,
            M56FigureStateBroadcast message,
            Channel channel)
        {
            if (message.SessionGeneration != sessionGeneration ||
                sender.ClientId != figureClientId ||
                message.RoleRevision != roleRevision ||
                message.OriginClientId != sender.ClientId)
            {
                return;
            }

            if (message.Sequence <= serverFigureSequence ||
                !IsFinite(message.Position) ||
                !IsFinite(message.Velocity) ||
                !IsFinite(message.Rotation))
            {
                return;
            }

            if (serverHasFigurePose)
            {
                float jump = Vector3.Distance(
                    canonicalFigurePosition,
                    message.Position);

                if (jump > MaximumSnapshotJump)
                {
                    SendRoleState(
                        sender,
                        "Figure snapshot rejected: jump " +
                        jump.ToString("0.0") + "m");
                    return;
                }
            }

            serverFigureSequence = message.Sequence;
            canonicalFigurePosition = message.Position;
            canonicalFigureRotation = message.Rotation;
            canonicalFigureVelocity = message.Velocity;
            serverHasFigurePose = true;

            if (!IsServerLocalClient(sender.ClientId))
            {
                QueueRemoteFigurePose(
                    message.Position,
                    message.Rotation,
                    message.Velocity);
            }

            networkManager.ServerManager.Broadcast(
                message,
                true,
                Channel.Unreliable);
        }

        private void Client_OnFigureState(
            M56FigureStateBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision ||
                message.OriginClientId != figureClientId ||
                figureMotor == null)
            {
                return;
            }

            // Host process already applied remote Figure state in the server
            // handler, or owns the local Figure directly.
            if (networkManager.ServerManager.Started)
                return;

            if (IsLocalFigure)
            {
                float error = Vector3.Distance(
                    figureMotor.transform.position,
                    message.Position);

                if (error > FigureHardReconcileDistance)
                {
                    ApplyFigurePose(
                        message.Position,
                        message.Rotation,
                        message.Velocity);
                }
            }
            else
            {
                QueueRemoteFigurePose(
                    message.Position,
                    message.Rotation,
                    message.Velocity);
            }
        }

        private void Server_OnWorldActionRequest(
            NetworkConnection sender,
            M56WorldActionRequestBroadcast message,
            Channel channel)
        {
            if (!ValidatePainterSender(
                    sender,
                    message.SessionGeneration,
                    message.KnownRoleRevision,
                    out string validationReason))
            {
                SendGameplayFeedback(
                    sender,
                    message.RequestId,
                    false,
                    validationReason);
                return;
            }

            if (!TryFindWorldAction(
                    message.SystemReference,
                    out INetworkReplicatedPainterWorldAction action))
            {
                SendGameplayFeedback(
                    sender,
                    message.RequestId,
                    false,
                    "World action bulunamadı: " + message.SystemReference);
                return;
            }

            bool accepted =
                action.RequestActivateFromNetworkAuthority();

            if (!accepted)
            {
                SendGameplayFeedback(
                    sender,
                    message.RequestId,
                    false,
                    action.LastResult);
                return;
            }

            actionRevision++;

            networkManager.ServerManager.Broadcast(
                new M56WorldActionCommitBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    ActionRevision = actionRevision,
                    RoleRevision = roleRevision,
                    OriginClientId = sender.ClientId,
                    SystemReference = message.SystemReference
                });

            SendGameplayFeedback(
                sender,
                message.RequestId,
                true,
                "Accepted by host • " + message.SystemReference);
        }

        private void Client_OnWorldActionCommit(
            M56WorldActionCommitBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision)
            {
                return;
            }

            // Server world already started the authoritative command.
            if (networkManager.ServerManager.Started)
                return;

            if (TryFindWorldAction(
                    message.SystemReference,
                    out INetworkReplicatedPainterWorldAction action))
            {
                bool accepted =
                    action.RequestActivateFromNetworkAuthority();

                lastGameplayFeedback = accepted
                    ? "Replicated world action • " + message.SystemReference
                    : "Replicated action local replay blocked • " +
                      action.LastResult;
            }
        }

        private void Server_OnOilStroke(
            NetworkConnection sender,
            M56OilStrokeBroadcast message,
            Channel channel)
        {
            if (!ValidatePainterSender(
                    sender,
                    message.SessionGeneration,
                    message.RoleRevision,
                    out _) ||
                !ValidateStroke(message))
            {
                return;
            }

            if (!IsServerLocalClient(sender.ClientId))
                ApplyOilStroke(message);

            networkManager.ServerManager.Broadcast(message);
        }

        private void Client_OnOilStroke(
            M56OilStrokeBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision)
            {
                return;
            }

            if (networkManager.ServerManager.Started ||
                message.OriginClientId == localClientId)
            {
                return;
            }

            ApplyOilStroke(message);
        }

        private void Server_OnOilPreview(
            NetworkConnection sender,
            M56OilPreviewBroadcast message,
            Channel channel)
        {
            if (!ValidatePainterSender(
                    sender,
                    message.SessionGeneration,
                    message.RoleRevision,
                    out _) ||
                !ValidateOilPreview(message))
            {
                return;
            }

            if (!IsServerLocalClient(sender.ClientId))
                ApplyOilPreview(message);

            networkManager.ServerManager.Broadcast(
                message,
                channel: message.Visible
                    ? Channel.Unreliable
                    : Channel.Reliable);
        }

        private void Client_OnOilPreview(
            M56OilPreviewBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision ||
                networkManager.ServerManager.Started ||
                message.OriginClientId == localClientId)
            {
                return;
            }

            ApplyOilPreview(message);
        }

        private void Server_OnOilCut(
            NetworkConnection sender,
            M56OilCutBroadcast message,
            Channel channel)
        {
            if (!ValidateFigureSender(
                    sender,
                    message.SessionGeneration,
                    message.RoleRevision) ||
                message.StrokeId <= 0 ||
                !IsFinite(message.Point) ||
                !IsFinite(message.GapWidth) ||
                message.GapWidth < 0.05f ||
                message.GapWidth > 5f)
            {
                return;
            }

            if (!IsServerLocalClient(sender.ClientId))
                ApplyOilCut(message);

            networkManager.ServerManager.Broadcast(message);
        }

        private void Client_OnOilCut(
            M56OilCutBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision ||
                networkManager.ServerManager.Started ||
                message.OriginClientId == localClientId)
            {
                return;
            }

            ApplyOilCut(message);
        }

        private void Server_OnPainterPresence(
            NetworkConnection sender,
            M56PainterPresenceBroadcast message,
            Channel channel)
        {
            if (!ValidatePainterSender(
                    sender,
                    message.SessionGeneration,
                    message.RoleRevision,
                    out _) ||
                !ValidatePainterPresence(message))
            {
                return;
            }

            if (!IsServerLocalClient(sender.ClientId))
                ApplyPainterPresence(message);

            networkManager.ServerManager.Broadcast(
                message,
                channel: Channel.Unreliable);
        }

        private void Client_OnPainterPresence(
            M56PainterPresenceBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision ||
                networkManager.ServerManager.Started ||
                message.OriginClientId == localClientId)
            {
                return;
            }

            ApplyPainterPresence(message);
        }

        private void Server_OnOilClear(
            NetworkConnection sender,
            M56OilClearBroadcast message,
            Channel channel)
        {
            if (!ValidatePainterSender(
                    sender,
                    message.SessionGeneration,
                    message.RoleRevision,
                    out _))
            {
                return;
            }

            if (!IsServerLocalClient(sender.ClientId))
                painterBrush?.ApplyReplicatedClear();

            networkManager.ServerManager.Broadcast(message);
        }

        private void Client_OnOilClear(
            M56OilClearBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision ||
                networkManager.ServerManager.Started ||
                message.OriginClientId == localClientId)
            {
                return;
            }

            painterBrush?.ApplyReplicatedClear();
        }

        private void Server_OnInkCreature(
            NetworkConnection sender,
            M56InkCreatureBroadcast message,
            Channel channel)
        {
            if (!ValidatePainterSender(
                    sender,
                    message.SessionGeneration,
                    message.RoleRevision,
                    out _) ||
                !ValidateCreature(message))
            {
                return;
            }

            if (!IsServerLocalClient(sender.ClientId))
                ApplyInkCreature(message);

            networkManager.ServerManager.Broadcast(message);
        }

        private void Client_OnInkCreature(
            M56InkCreatureBroadcast message,
            Channel channel)
        {
            if (!NetworkSessionActive ||
                message.SessionGeneration != sessionGeneration ||
                message.RoleRevision != roleRevision ||
                networkManager.ServerManager.Started ||
                message.OriginClientId == localClientId)
            {
                return;
            }

            ApplyInkCreature(message);
        }

        private void Client_OnGameplayFeedback(
            M56GameplayFeedbackBroadcast message,
            Channel channel)
        {
            lastGameplayFeedback = message.Message;

            if (!message.Accepted)
                Debug.LogWarning("[M56] " + message.Message, this);
            else
                Debug.Log("[M56] " + message.Message, this);
        }

        private void Client_OnRoleState(
            M56RoleStateBroadcast message,
            Channel channel)
        {
            if (message.SessionGeneration < sessionGeneration ||
                (message.SessionGeneration == sessionGeneration &&
                 message.Revision < roleRevision))
                return;

            sessionGeneration = message.SessionGeneration;
            roleRevision = message.Revision;
            figureClientId = message.FigureClientId;
            painterClientId = message.PainterClientId;
            canonicalFigurePosition = message.FigurePosition;
            canonicalFigureRotation = message.FigureRotation;
            canonicalFigureVelocity = message.FigureVelocity;
            serverHasFigurePose = true;

            figurePlayerName = string.IsNullOrWhiteSpace(message.FigurePlayerName)
                ? "Client " + figureClientId
                : message.FigurePlayerName;
            painterPlayerName = string.IsNullOrWhiteSpace(message.PainterPlayerName)
                ? "Client " + painterClientId
                : message.PainterPlayerName;

            ApplyFigurePose(
                message.FigurePosition,
                message.FigureRotation,
                message.FigureVelocity);
            SetRemoteFigureTargetFromRoleState(message);

            if (roleAuthority != null)
            {
                if (localClientId == figureClientId)
                {
                    roleAuthority.ApplyNetworkRole(
                        PaintedAliveLocalRole.Figure,
                        roleRevision,
                        message.Reason);
                    lastRoleFeedback =
                        "Role = FIGURE • r" + roleRevision;
                    HandleAuthoritativeLocalRoleApplied(
                        PaintedAliveLocalRole.Figure);
                }
                else if (localClientId == painterClientId)
                {
                    roleAuthority.ApplyNetworkRole(
                        PaintedAliveLocalRole.InkPainter,
                        roleRevision,
                        message.Reason);
                    lastRoleFeedback =
                        "Role = PAINTER • r" + roleRevision;
                    HandleAuthoritativeLocalRoleApplied(
                        PaintedAliveLocalRole.InkPainter);
                }
            }

            Debug.Log(
                "[M56 Role] STATE_APPLIED | " +
                $"Generation={sessionGeneration} r{roleRevision} Figure={figureClientId} " +
                $"Painter={painterClientId} Local={localClientId} | " +
                message.Reason,
                this);

            lastNetworkEvent =
                $"STATE_APPLIED g{sessionGeneration} r{roleRevision} " +
                $"Figure={figureClientId} Painter={painterClientId}";
        }

        private void Client_OnRoundReset(
            M56RoundResetBroadcast message,
            Channel channel)
        {
            if (message.RoleRevision != roleRevision ||
                message.SessionGeneration < sessionGeneration)
                return;

            sessionGeneration = message.SessionGeneration;

            // Host/server already executed reset before broadcasting it.
            if (networkManager.ServerManager.Started)
                return;

            ApplyLocalRoundReset();
            ApplyFigurePose(
                message.FigurePosition,
                message.FigureRotation,
                Vector3.zero);
            hasRemoteFigureTarget = false;
        }

        private bool ValidatePainterSender(
            NetworkConnection sender,
            int knownGeneration,
            int knownRevision,
            out string reason)
        {
            if (sender == null || !sender.IsValid)
            {
                reason = "Invalid network connection.";
                return false;
            }

            if (knownGeneration != sessionGeneration)
            {
                reason =
                    "Stale session generation. Client=" +
                    knownGeneration + " Server=" + sessionGeneration;
                return false;
            }

            if (knownRevision != roleRevision)
            {
                reason =
                    "Stale role revision. Client=" +
                    knownRevision + " Server=" + roleRevision;
                return false;
            }

            if (sender.ClientId != painterClientId)
            {
                reason =
                    "Server rejected Painter command: client " +
                    sender.ClientId + " does not own Painter role.";
                return false;
            }

            reason = "OK";
            return true;
        }

        private bool ValidateFigureSender(
            NetworkConnection sender,
            int knownGeneration,
            int knownRevision)
        {
            return sender != null &&
                   sender.IsValid &&
                   knownGeneration == sessionGeneration &&
                   knownRevision == roleRevision &&
                   sender.ClientId == figureClientId;
        }

        private bool TryFindWorldAction(
            string systemReference,
            out INetworkReplicatedPainterWorldAction action)
        {
            action = null;

            if (string.IsNullOrWhiteSpace(systemReference))
                return false;

            PalimpsestPainterWorldInteraction[] palimpsest =
                FindObjectsByType<PalimpsestPainterWorldInteraction>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            for (int index = 0; index < palimpsest.Length; index++)
            {
                PalimpsestPainterWorldInteraction candidate =
                    palimpsest[index];

                if (candidate != null &&
                    string.Equals(
                        candidate.SystemReference,
                        systemReference,
                        StringComparison.Ordinal))
                {
                    action = candidate;
                    return true;
                }
            }

            LivingGalleryPainterWorldInteraction[] gallery =
                FindObjectsByType<LivingGalleryPainterWorldInteraction>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            for (int index = 0; index < gallery.Length; index++)
            {
                LivingGalleryPainterWorldInteraction candidate =
                    gallery[index];

                if (candidate != null &&
                    string.Equals(
                        candidate.SystemReference,
                        systemReference,
                        StringComparison.Ordinal))
                {
                    action = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool ValidateStroke(M56OilStrokeBroadcast message)
        {
            if (message.Points == null ||
                message.Points.Length < 2 ||
                message.Points.Length > MaximumStrokePoints ||
                message.StrokeId <= 0 ||
                (message.Shape != (int)OilStrokeShape.Wall &&
                 message.Shape != (int)OilStrokeShape.Ramp))
            {
                return false;
            }

            float total = 0f;
            for (int index = 0; index < message.Points.Length; index++)
            {
                if (!IsFinite(message.Points[index]))
                    return false;

                if (index > 0)
                {
                    float segment = Vector3.Distance(
                        message.Points[index - 1],
                        message.Points[index]);

                    if (segment > 5f)
                        return false;

                    total += segment;
                }
            }

            if (total > 80f)
                return false;

            return
                IsFinite(message.DrawSpeed) &&
                IsFinite(message.Pressure) &&
                IsFinite(message.Width) &&
                IsFinite(message.Height) &&
                IsFinite(message.Pigment) &&
                IsFinite(message.CutResistance) &&
                IsFinite(message.LifecycleDuration) &&
                IsFinite(message.Budget);
        }

        private bool ValidateOilPreview(M56OilPreviewBroadcast message)
        {
            if (!message.Visible)
                return IsFinite(message.Width);

            if (message.Points == null ||
                message.Points.Length == 0 ||
                message.Points.Length > MaximumStrokePoints ||
                (message.Shape != (int)OilStrokeShape.Wall &&
                 message.Shape != (int)OilStrokeShape.Ramp) ||
                !IsFinite(message.Width) ||
                message.Width < 0.02f ||
                message.Width > 2f)
            {
                return false;
            }

            float totalLength = 0f;
            for (int index = 0; index < message.Points.Length; index++)
            {
                if (!IsFinite(message.Points[index]))
                    return false;

                if (index > 0)
                {
                    float segmentLength = Vector3.Distance(
                        message.Points[index - 1],
                        message.Points[index]);

                    if (segmentLength > 5f)
                        return false;

                    totalLength += segmentLength;
                }
            }

            return totalLength <= 80f;
        }

        private void ApplyOilPreview(M56OilPreviewBroadcast message)
        {
            painterBrush?.ApplyReplicatedPreview(
                message.PreviewId,
                message.Visible,
                message.Points,
                (OilStrokeShape)message.Shape,
                message.Width);
        }

        private static bool ValidatePainterPresence(
            M56PainterPresenceBroadcast message)
        {
            return IsFinite(message.Position) &&
                   IsFinite(message.FocusPoint) &&
                   IsFinite(message.Forward) &&
                   message.Forward.sqrMagnitude > 0.25f;
        }

        private void ApplyPainterPresence(
            M56PainterPresenceBroadcast message)
        {
            remotePainterPosition = message.Position;
            remotePainterFocusPoint = message.FocusPoint;
            remotePainterForward = message.Forward.normalized;
            remotePainterPainting = message.Painting;
            remotePainterPresenceReceivedAt = Time.unscaledTime;
            hasRemotePainterPresence = true;
        }

        private void ApplyOilStroke(M56OilStrokeBroadcast message)
        {
            if (painterBrush == null)
                return;

            OilStrokePressureProfile profile =
                new OilStrokePressureProfile(
                    message.DrawSpeed,
                    message.Pressure,
                    message.Width,
                    message.Height,
                    message.Pigment,
                    message.CutResistance,
                    message.LifecycleDuration,
                    message.Budget);

            painterBrush.ApplyReplicatedStroke(
                message.StrokeId,
                message.Points,
                (OilStrokeShape)message.Shape,
                profile);
        }

        private void ApplyOilCut(M56OilCutBroadcast message)
        {
            painterBrush?.ApplyReplicatedCut(
                message.StrokeId,
                message.Point,
                message.GapWidth);
        }

        private bool ValidateCreature(M56InkCreatureBroadcast message)
        {
            return
                message.LoadoutId >= (int)InkGlyphLoadoutId.Lekebacak &&
                message.LoadoutId <= (int)InkGlyphLoadoutId.KesikAvci &&
                IsFinite(message.Point) &&
                IsFinite(message.Normal) &&
                IsFinite(message.Facing) &&
                message.Normal.sqrMagnitude > 0.25f;
        }

        private void ApplyInkCreature(M56InkCreatureBroadcast message)
        {
            nestController?.ApplyReplicatedCreature(
                (InkGlyphLoadoutId)message.LoadoutId,
                message.Point,
                message.Normal.normalized,
                message.Facing.normalized);
        }

        private void BroadcastRoleState(string reason)
        {
            CaptureCanonicalFigureFromScene();

            M56RoleStateBroadcast state = BuildRoleState(reason);
            networkManager.ServerManager.Broadcast(state);
        }

        private void SendRoleState(
            NetworkConnection connection,
            string reason)
        {
            CaptureCanonicalFigureFromScene();
            networkManager.ServerManager.Broadcast(
                connection,
                BuildRoleState(reason));
        }

        private M56RoleStateBroadcast BuildRoleState(string reason)
        {
            return new M56RoleStateBroadcast
            {
                SessionGeneration = sessionGeneration,
                Revision = roleRevision,
                FigureClientId = figureClientId,
                PainterClientId = painterClientId,
                FigurePosition = canonicalFigurePosition,
                FigureRotation = canonicalFigureRotation,
                FigureVelocity = canonicalFigureVelocity,
                FigurePlayerName = ResolvePlayerName(figureClientId),
                PainterPlayerName = ResolvePlayerName(painterClientId),
                Reason = reason ?? string.Empty
            };
        }

        private void SendGameplayFeedback(
            NetworkConnection connection,
            uint requestId,
            bool accepted,
            string message)
        {
            networkManager.ServerManager.Broadcast(
                connection,
                new M56GameplayFeedbackBroadcast
                {
                    RequestId = requestId,
                    Accepted = accepted,
                    Message = message ?? string.Empty
                });
        }

        private void ServerResetRound()
        {
            sessionGeneration++;
            actionRevision = 0;
            localGameplaySequence = 0;
            localActionRequestId = 0;
            ApplyLocalRoundReset();
            CaptureCanonicalFigureFromScene();
            BroadcastRoleState("Round reset generation " + sessionGeneration);

            networkManager.ServerManager.Broadcast(
                new M56RoundResetBroadcast
                {
                    SessionGeneration = sessionGeneration,
                    RoleRevision = roleRevision,
                    FigurePosition = canonicalFigurePosition,
                    FigureRotation = canonicalFigureRotation
                });
        }

        private void ApplyLocalRoundReset()
        {
            // PrototypeCoreMatchController is the current authority. The older
            // PrototypeMatchController remains only as a fallback for scenes
            // which have not migrated, avoiding a double reset when both exist.
            if (coreMatch != null)
            {
                coreMatch.ResetMatchAndBeginPreparation();
            }
            else if (prototypeMatch != null)
            {
                prototypeMatch.BeginNewMatch();
            }
        }

        private void CaptureCanonicalFigureFromScene()
        {
            if (figureMotor == null)
                return;

            canonicalFigurePosition = figureMotor.transform.position;
            canonicalFigureRotation = figureMotor.transform.rotation;
            canonicalFigureVelocity = figureMotor.Velocity;
            serverHasFigurePose = true;
        }

        private void ApplyFigurePose(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity)
        {
            if (figureMotor == null)
                return;

            figureMotor.ApplyNetworkPose(
                position,
                rotation,
                velocity);
        }

        private void QueueRemoteFigurePose(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity)
        {
            remoteFigureTargetPosition = position;
            remoteFigureTargetRotation = rotation;
            remoteFigureTargetVelocity = velocity;

            if (!hasRemoteFigureTarget ||
                figureMotor == null ||
                Vector3.Distance(
                    figureMotor.transform.position,
                    position) > FigureHardReconcileDistance)
            {
                ApplyFigurePose(position, rotation, velocity);
            }

            hasRemoteFigureTarget = true;
        }

        private void UpdateRemoteFigureInterpolation()
        {
            if (!hasRemoteFigureTarget ||
                !NetworkSessionActive ||
                IsLocalFigure ||
                figureMotor == null)
            {
                return;
            }

            float blend = 1f - Mathf.Exp(
                -RemoteFigureInterpolationSharpness *
                Mathf.Max(0f, Time.unscaledDeltaTime));

            ApplyFigurePose(
                Vector3.Lerp(
                    figureMotor.transform.position,
                    remoteFigureTargetPosition,
                    blend),
                Quaternion.Slerp(
                    figureMotor.transform.rotation,
                    remoteFigureTargetRotation,
                    blend),
                remoteFigureTargetVelocity);
        }

        private void SetRemoteFigureTargetFromRoleState(
            M56RoleStateBroadcast message)
        {
            hasRemoteFigureTarget = !IsLocalFigure;
            remoteFigureTargetPosition = message.FigurePosition;
            remoteFigureTargetRotation = message.FigureRotation;
            remoteFigureTargetVelocity = message.FigureVelocity;
        }

        private bool IsServerLocalClient(int clientId)
        {
            return
                networkManager != null &&
                networkManager.ServerManager != null &&
                networkManager.ServerManager.Started &&
                networkManager.ClientManager != null &&
                networkManager.ClientManager.Started &&
                networkManager.ClientManager.Connection.ClientId == clientId;
        }

        private int GetAnyHelloClientId()
        {
            foreach (int id in playerNames.Keys)
                return id;

            return NoClient;
        }

        private void RefreshRoleNames()
        {
            figurePlayerName = ResolvePlayerName(figureClientId);
            painterPlayerName = ResolvePlayerName(painterClientId);
        }

        private string ResolvePlayerName(int clientId)
        {
            if (clientId == NoClient)
                return "—";

            return playerNames.TryGetValue(clientId, out string value)
                ? value
                : "Client " + clientId;
        }

        private void RefreshConnectionStatus()
        {
            if (networkManager == null ||
                networkManager.ClientManager == null ||
                networkManager.ServerManager == null)
            {
                connectionStatus = "FishNet not ready";
                return;
            }

            if (localAuthenticated)
            {
                connectionStatus =
                    networkManager.ServerManager.Started
                        ? "HOST CONNECTED"
                        : "CLIENT CONNECTED";
            }
            else if (networkManager.ClientManager.Started)
            {
                connectionStatus = "Authenticating…";
            }
            else if (networkManager.ServerManager.Started)
            {
                connectionStatus = "Server started; local client waiting…";
            }
            else if (!M56SteamBootstrap.Ready)
            {
                connectionStatus =
                    "Steam offline • " + M56SteamBootstrap.LastError;
            }
        }

        private void ResetLocalNetworkState(string reason)
        {
            localAuthenticated = false;
            localClientId = NoClient;
            figureClientId = NoClient;
            painterClientId = NoClient;
            sessionGeneration = 0;
            roleRevision = 0;
            localFigureSequence = 0;
            serverFigureSequence = 0;
            localGameplaySequence = 0;
            localActionRequestId = 0;
            hasRemoteFigureTarget = false;
            lastRoleFeedback = reason;
            lastGameplayFeedback = reason;
            connectionStatus = reason;
            lastNetworkEvent = reason;
            steamIds.Clear();
            playerNames.Clear();
            RefreshRoleNames();
            hasAuthoritativeLocalRole = false;
            rolePresentation = RolePresentation.None;
            inputReleasePending = false;
            OpenDevelopmentMenu(reason);

            roleAuthority?.ApplyNetworkRole(
                PaintedAliveLocalRole.Figure,
                0,
                "Network session stopped");
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private void ApplyFriendFullscreenStartup()
        {
            if (fullscreenStartupApplied || Application.isBatchMode)
                return;

            fullscreenStartupApplied = true;
            int nativeWidth = Mathf.Max(1, Display.main.systemWidth);
            int nativeHeight = Mathf.Max(1, Display.main.systemHeight);
            windowedWidth = Mathf.Max(960, Mathf.RoundToInt(nativeWidth * 0.8f));
            windowedHeight = Mathf.Max(540, Mathf.RoundToInt(nativeHeight * 0.8f));
            Screen.SetResolution(
                nativeWidth,
                nativeHeight,
                FullScreenMode.FullScreenWindow);

            Debug.Log(
                $"[M56 UI] Fullscreen startup • FullScreenWindow {nativeWidth}x{nativeHeight}",
                this);
        }

        private void ToggleFullscreen()
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                Screen.SetResolution(
                    Mathf.Max(1, Display.main.systemWidth),
                    Mathf.Max(1, Display.main.systemHeight),
                    FullScreenMode.FullScreenWindow);
                Debug.Log("[M56 UI] F11 • borderless fullscreen", this);
            }
            else
            {
                Screen.SetResolution(
                    Mathf.Max(960, windowedWidth),
                    Mathf.Max(540, windowedHeight),
                    FullScreenMode.Windowed);
                Debug.Log("[M56 UI] F11 • windowed", this);
            }
        }

        private void UpdateLocalPresentation()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.f11Key.wasPressedThisFrame)
                ToggleFullscreen();

            if (rolePresentation != RolePresentation.None)
            {
                bool manualDismiss =
                    Time.unscaledTime - rolePresentationOpenedAt >=
                    MinimumManualDismissDelay &&
                    WasAnyDismissInputPressed();

                if (manualDismiss ||
                    Time.unscaledTime >= rolePresentationEndsAt)
                {
                    DismissRolePresentation(
                        manualDismiss ? "input" : "timeout");
                }

                return;
            }

            if (inputReleasePending)
            {
                if (Time.frameCount > inputReleaseStartedFrame &&
                    !IsAnyGameplayInputHeld())
                {
                    inputReleasePending = false;
                    Debug.Log(
                        "[M56 UI] Gameplay input enabled: " +
                        GetLocalRoleLabel(),
                        this);
                }

                return;
            }

            if (keyboard == null ||
                !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (!NetworkSessionActive || !hasAuthoritativeLocalRole)
            {
                OpenDevelopmentMenu("Offline ESC");
                return;
            }

            if (developmentMenuOpen)
            {
                developmentMenuOpen = false;
                BeginInputReleaseGuard();
                Debug.Log("[M56 UI] ESC menu closed", this);
            }
            else
            {
                OpenDevelopmentMenu("ESC");
                Debug.Log("[M56 UI] ESC menu opened", this);
            }
        }

        private void HandleAuthoritativeLocalRoleApplied(
            PaintedAliveLocalRole role)
        {
            if (hasAuthoritativeLocalRole &&
                authoritativeLocalRole == role)
            {
                return;
            }

            authoritativeLocalRole = role;
            hasAuthoritativeLocalRole = true;
            bool firstPresentation = role == PaintedAliveLocalRole.Figure
                ? !hasSeenFigureIntro
                : !hasSeenPainterIntro;

            presentationRole = role;
            rolePresentation = firstPresentation
                ? RolePresentation.FullIntro
                : RolePresentation.ShortConfirmation;
            rolePresentationOpenedAt = Time.unscaledTime;
            rolePresentationEndsAt = Time.unscaledTime +
                (firstPresentation
                    ? FullRoleIntroDuration
                    : ShortRoleConfirmationDuration);
            closeMenuAfterPresentation = true;

            if (role == PaintedAliveLocalRole.Figure)
                hasSeenFigureIntro = true;
            else
                hasSeenPainterIntro = true;

            if (firstPresentation)
                developmentMenuOpen = true;

            Debug.Log(
                "[M56 UI] Authoritative role applied: " +
                GetRoleLabel(role),
                this);
            Debug.Log(
                firstPresentation
                    ? $"[M56 UI] {GetRoleLabel(role)} intro opened"
                    : $"[M56 UI] Role confirmation: {GetRoleLabel(role)}",
                this);
        }

        private void DismissRolePresentation(string reason)
        {
            PaintedAliveLocalRole dismissedRole = presentationRole;
            bool wasFullIntro =
                rolePresentation == RolePresentation.FullIntro;
            rolePresentation = RolePresentation.None;

            if (closeMenuAfterPresentation &&
                NetworkSessionActive &&
                hasAuthoritativeLocalRole)
            {
                developmentMenuOpen = false;
            }

            closeMenuAfterPresentation = false;
            BeginInputReleaseGuard();

            Debug.Log(
                wasFullIntro
                    ? $"[M56 UI] {GetRoleLabel(dismissedRole)} intro dismissed • {reason}"
                    : $"[M56 UI] {GetRoleLabel(dismissedRole)} confirmation closed",
                this);
        }

        private void OpenDevelopmentMenu(string reason)
        {
            bool changed = !developmentMenuOpen;
            developmentMenuOpen = true;
            ApplyPresentationCursorState();

            if (changed)
            {
                Debug.Log(
                    "[M56 UI] Development menu opened • " + reason,
                    this);
            }
        }

        private void BeginInputReleaseGuard()
        {
            inputReleasePending = true;
            inputReleaseStartedFrame = Time.frameCount;
        }

        private static bool WasAnyDismissInputPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            return
                (keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
                (mouse != null &&
                 (mouse.leftButton.wasPressedThisFrame ||
                  mouse.rightButton.wasPressedThisFrame ||
                  mouse.middleButton.wasPressedThisFrame));
        }

        private static bool IsAnyGameplayInputHeld()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            return
                (keyboard != null && keyboard.anyKey.isPressed) ||
                (mouse != null &&
                 (mouse.leftButton.isPressed ||
                  mouse.rightButton.isPressed ||
                  mouse.middleButton.isPressed));
        }

        private void ApplyPresentationCursorState()
        {
            if (GameplayInputSuppressed ||
                !NetworkSessionActive ||
                !hasAuthoritativeLocalRole)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            // Both current role implementations use a locked, hidden pointer:
            // Figure consumes mouse delta; Painter aims from screen centre while
            // the independent camera consumes mouse delta.
            switch (authoritativeLocalRole)
            {
                case PaintedAliveLocalRole.InkPainter:
                case PaintedAliveLocalRole.Figure:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
            }
        }

        private string GetLocalRoleLabel()
        {
            return hasAuthoritativeLocalRole
                ? GetRoleLabel(authoritativeLocalRole)
                : "UNASSIGNED";
        }

        private static string GetRoleLabel(PaintedAliveLocalRole role)
        {
            return role == PaintedAliveLocalRole.InkPainter
                ? "PAINTER"
                : "FIGURE";
        }

        private void OnGUI()
        {
            if (!showConnectionHud)
                return;

            // IMGUI runs after gameplay LateUpdate, so this is the final cursor
            // arbitration point even for legacy systems with very late order.
            ApplyPresentationCursorState();

            float scale = Mathf.Clamp(Screen.height / 900f, 0.78f, 1.25f);

            if (!developmentMenuOpen &&
                rolePresentation == RolePresentation.None)
            {
                DrawPainterPresenceIndicator(scale);
            }

            if (rolePresentation != RolePresentation.None)
            {
                DrawRolePresentation(scale);
            }
            else if (developmentMenuOpen)
                DrawDevelopmentMenu(scale);
            else
                DrawCollapsedStatus(scale);
        }

        private void DrawPainterPresenceIndicator(float scale)
        {
            if (!IsLocalFigure ||
                !hasRemotePainterPresence ||
                Time.unscaledTime - remotePainterPresenceReceivedAt > 1.25f)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Vector3 screen = camera.WorldToScreenPoint(remotePainterFocusPoint);
            Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 point = new(screen.x, Screen.height - screen.y);
            Vector2 direction = point - center;

            if (screen.z < 0f)
                direction = -direction;

            if (direction.sqrMagnitude < 1f)
                direction = Vector2.up;

            float edge = 74f * scale;
            point = center + direction.normalized *
                Mathf.Min(direction.magnitude, Screen.height * 0.43f);
            point.x = Mathf.Clamp(point.x, edge, Screen.width - edge);
            point.y = Mathf.Clamp(point.y, edge, Screen.height - edge);

            float distance = figureMotor != null
                ? Vector3.Distance(
                    figureMotor.transform.position,
                    remotePainterFocusPoint)
                : Vector3.Distance(camera.transform.position, remotePainterPosition);

            string text = remotePainterPainting
                ? $"◆ RESSAM ÇİZİYOR  •  {Mathf.Round(distance / 5f) * 5f:0} m"
                : $"◇ RESSAM İZİ  •  {Mathf.Round(distance / 5f) * 5f:0} m";

            GUIStyle style = new(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(12f * scale)
            };

            Color previous = GUI.color;
            float pulse = remotePainterPainting
                ? 0.82f + Mathf.Sin(Time.unscaledTime * 8f) * 0.18f
                : 0.68f;
            GUI.color = new Color(0.12f, 0.88f, 0.92f, pulse);
            GUI.Box(
                new Rect(
                    point.x - 82f * scale,
                    point.y - 18f * scale,
                    164f * scale,
                    36f * scale),
                text,
                style);
            GUI.color = previous;
        }

        private void DrawDevelopmentMenu(float scale)
        {
            float width = Mathf.Min(460f * scale, Screen.width - 24f);
            float height = Mathf.Min(690f * scale, Screen.height - 24f);
            float x = Screen.width - width - 12f;
            float y = 12f;
            GUIStyle box = new(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = Mathf.RoundToInt(13f * scale)
            };

            GUI.BeginGroup(new Rect(x, y, width, height), GUIContent.none, box);
            GUILayout.BeginArea(new Rect(
                14f * scale,
                12f * scale,
                width - 28f * scale,
                height - 24f * scale));

            GUIStyle heading = new(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(17f * scale)
            };
            GUIStyle section = new(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(13f * scale)
            };

            GUILayout.Label("M56 • FRIEND 1v1 STEAM", heading);
            GUILayout.Label(connectionStatus);
            GUILayout.Label(
                $"Players: {ConnectedPlayerCount}/2 • Generation: {sessionGeneration} • Role revision: {roleRevision}");
            GUILayout.Label(
                $"Local: {GetLocalRoleLabel()} • Remote: " +
                (IsLocalFigure ? "PAINTER" : IsLocalPainter ? "FIGURE" : "UNASSIGNED"));
            GUILayout.Label("Last event: " + lastNetworkEvent);

            string localSteam = M56SteamBootstrap.LocalSteamId != 0
                ? M56SteamBootstrap.LocalSteamId.ToString()
                : "Steam unavailable";
            GUILayout.BeginHorizontal();
            GUILayout.Label("My SteamID64: " + localSteam);
            if (GUILayout.Button("COPY", GUILayout.Width(68f * scale)))
                GUIUtility.systemCopyBuffer = localSteam;
            GUILayout.EndHorizontal();

            if (!NetworkSessionActive)
            {
                if (GUILayout.Button("HOST • FIGURE FIRST"))
                    StartHost();

                GUILayout.BeginHorizontal();
                GUI.SetNextControlName("M56SteamIdField");
                joinSteamId = GUILayout.TextField(joinSteamId ?? string.Empty);
                if (GUILayout.Button("JOIN", GUILayout.Width(76f * scale)))
                    JoinHost();
                GUILayout.EndHorizontal();
                GUILayout.Label("Host SteamID64 değerini alana yaz veya yapıştır.");
            }
            else
            {
                GUILayout.Label($"Figure: {figurePlayerName}  [Client {figureClientId}]");
                GUILayout.Label($"Painter: {painterPlayerName}  [Client {painterClientId}]");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("F1 • FIGURE"))
                    RequestRole(PaintedAliveLocalRole.Figure);
                if (GUILayout.Button("F2 • PAINTER"))
                    RequestRole(PaintedAliveLocalRole.InkPainter);
                GUILayout.EndHorizontal();
                GUILayout.Label(lastRoleFeedback);
                GUILayout.Label(lastGameplayFeedback);
            }

            GUILayout.Space(6f * scale);
            GUILayout.Label("CONTROLS • " + GetLocalRoleLabel(), section);
            DrawControlHints(
                M56ControlHints.ForRole(
                    hasAuthoritativeLocalRole
                        ? authoritativeLocalRole
                        : PaintedAliveLocalRole.Figure),
                scale,
                compact: true);
            GUILayout.Label("F11 • FULLSCREEN / WINDOWED");

            if (NetworkSessionActive)
            {
                GUILayout.Space(5f * scale);
                if (GUILayout.Button("CLOSE MENU • ESC"))
                {
                    developmentMenuOpen = false;
                    BeginInputReleaseGuard();
                }
                if (GUILayout.Button("DISCONNECT"))
                    Disconnect();
            }

            GUILayout.EndArea();
            GUI.EndGroup();
        }

        private void DrawCollapsedStatus(float scale)
        {
            string text = $"M56 • {GetLocalRoleLabel()} • ESC MENU";
            GUIStyle style = new(GUI.skin.box)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(12f * scale)
            };
            Vector2 size = style.CalcSize(new GUIContent(text));
            GUI.Box(
                new Rect(
                    Screen.width - size.x - 28f * scale,
                    14f * scale,
                    size.x + 16f * scale,
                    30f * scale),
                text,
                style);
        }

        private void DrawRolePresentation(float scale)
        {
            bool full = rolePresentation == RolePresentation.FullIntro;
            float fadeIn = Mathf.Clamp01(
                (Time.unscaledTime - rolePresentationOpenedAt) / 0.18f);
            float fadeOut = Mathf.Clamp01(
                (rolePresentationEndsAt - Time.unscaledTime) / 0.2f);
            float alpha = Mathf.Min(fadeIn, fadeOut);
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none);

            float width = Mathf.Min(720f * scale, Screen.width - 40f);
            float desiredHeight = full ? 610f * scale : 185f * scale;
            float height = Mathf.Min(desiredHeight, Screen.height - 40f);
            Rect card = new(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            GUIStyle cardStyle = new(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.BeginGroup(card, GUIContent.none, cardStyle);
            GUILayout.BeginArea(new Rect(
                30f * scale,
                22f * scale,
                width - 60f * scale,
                height - 44f * scale));

            GUIStyle title = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt((full ? 34f : 28f) * scale)
            };
            GUIStyle subtitle = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(18f * scale)
            };
            GUIStyle body = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = Mathf.RoundToInt(14f * scale)
            };

            GUILayout.Label(GetRoleLabel(presentationRole), title);
            GUILayout.Label(
                presentationRole == PaintedAliveLocalRole.Figure
                    ? "ESCAPE THE PAINTING"
                    : "RESHAPE THE WORLD",
                subtitle);

            if (full)
            {
                GUILayout.Space(7f * scale);
                GUILayout.Label(
                    presentationRole == PaintedAliveLocalRole.Figure
                        ? "Move through the living artwork, use your tools, and reach the frame."
                        : "Control the painting itself. Aim at painted elements, change routes, and redirect the Figure.",
                    body);
                GUILayout.Space(10f * scale);
                DrawControlHints(
                    M56ControlHints.ForRole(presentationRole),
                    scale,
                    compact: false);
                GUILayout.FlexibleSpace();
                GUILayout.Label("PRESS ANY KEY • AUTO CONTINUE", body);
            }

            GUILayout.EndArea();
            GUI.EndGroup();
            GUI.color = previousColor;
        }

        private static void DrawControlHints(
            M56ControlHints.Hint[] hints,
            float scale,
            bool compact)
        {
            GUIStyle keyStyle = new(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                fontSize = Mathf.RoundToInt((compact ? 11f : 13f) * scale)
            };
            GUIStyle actionStyle = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt((compact ? 11f : 13f) * scale)
            };

            for (int i = 0; i < hints.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    hints[i].Binding,
                    keyStyle,
                    GUILayout.Width((compact ? 130f : 205f) * scale));
                GUILayout.Space(12f * scale);
                GUILayout.Label(hints[i].Action, actionStyle);
                GUILayout.EndHorizontal();
            }
        }
    }
}
