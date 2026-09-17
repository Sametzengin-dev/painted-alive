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
    [DefaultExecutionOrder(240)]
    [DisallowMultipleComponent]
    public sealed class PrototypeUnderlayerDiveController :
        MonoBehaviour
    {
        [Header("Figure Sources")]
        [SerializeField]
        private Transform figureRoot;

        [SerializeField]
        private MonoBehaviour figureMotor;

        [SerializeField]
        private MonoBehaviour paletteKnifeController;

        [SerializeField]
        private MonoBehaviour traceWeaveRecorder;

        [Header("Seams")]
        [SerializeField]
        private PrototypeUnderlayerSeam seamA;

        [SerializeField]
        private PrototypeUnderlayerSeam seamB;

        [Header("Interaction")]
        [SerializeField, Min(0.5f)]
        private float seamUseRadius = 1.65f;

        [SerializeField, Min(0.25f)]
        private float underlayerSpawnInset = 1.25f;

        [SerializeField, Min(0.5f)]
        private float maximumUnderlayerGraphDistance = 4.25f;

        [SerializeField, Min(0.1f)]
        private float seamOpenSeconds = 2.4f;

        [Header("Surface Signal")]
        [SerializeField]
        private float surfaceSignalRadius = 0.46f;

        [SerializeField]
        private float surfaceSignalHeight = 0.18f;

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
        private PrototypeUnderlayerDiveState state =
            PrototypeUnderlayerDiveState.WaitingForFigure;

        [SerializeField]
        private bool figureRoleActive;

        [SerializeField]
        private bool inUnderlayer;

        [SerializeField]
        private bool paletteKnifeActive;

        [SerializeField]
        private bool paletteKnifeInputResolved;

        [SerializeField]
        private bool paletteKnifePressed;

        [SerializeField]
        private bool traceRecorderSuppressed;

        [SerializeField]
        private bool surfaceSignalVisible;

        [SerializeField]
        private bool safetyRecoveryArmed = true;

        [SerializeField]
        private int diveCount;

        [SerializeField]
        private int surfaceCount;

        [SerializeField]
        private int rejectedUseCount;

        [SerializeField]
        private int safetyRecoveryCount;

        [SerializeField]
        private int traceSuppressCount;

        [SerializeField]
        private int traceRestoreCount;

        [SerializeField]
        private float nearestSeamDistance = -1f;

        [SerializeField]
        private float underlayerProgress;

        [SerializeField]
        private float graphDistance;

        [SerializeField]
        private Vector3 projectedSurfacePosition;

        [SerializeField]
        private string nearestSeam = "None";
        private float hudVisibleUntilUnscaled;

        [SerializeField]
        private string resolvedRole = "Unknown";

        [SerializeField]
        private string paletteKnifeInputContract =
            "Unresolved";

        [SerializeField]
        private string lastAction =
            "Alt Katman dikişi bekleniyor.";

        private CharacterController characterController;
        private Rigidbody figureRigidbody;

        private InputActionReference paletteUseToolAction;

        private bool savedTraceRecorderEnabled;
        private bool traceRecorderStateCaptured;

        private PrototypeUnderlayerSeam currentNearestSeam;

        private LineRenderer surfaceSignalRing;
        private LineRenderer surfaceSignalBump;
        private Material surfaceSignalMaterial;

        public PrototypeUnderlayerDiveState State => state;
        public bool FigureRoleActive => figureRoleActive;
        public bool InUnderlayer => inUnderlayer;
        public bool PaletteKnifeActive => paletteKnifeActive;
        public bool PaletteKnifeInputResolved => paletteKnifeInputResolved;
        public bool PaletteKnifePressed => paletteKnifePressed;
        public bool TraceRecorderSuppressed => traceRecorderSuppressed;
        public bool SurfaceSignalVisible => surfaceSignalVisible;
        public int DiveCount => diveCount;
        public int SurfaceCount => surfaceCount;
        public int RejectedUseCount => rejectedUseCount;
        public int SafetyRecoveryCount => safetyRecoveryCount;
        public int TraceSuppressCount => traceSuppressCount;
        public int TraceRestoreCount => traceRestoreCount;
        public float NearestSeamDistance => nearestSeamDistance;
        public float UnderlayerProgress => underlayerProgress;
        public float GraphDistance => graphDistance;
        public Vector3 ProjectedSurfacePosition =>
            projectedSurfacePosition;
        public string NearestSeam => nearestSeam;
        public string ResolvedRole => resolvedRole;
        public string PaletteKnifeInputContract =>
            paletteKnifeInputContract;
        public string LastAction => lastAction;
        public Transform FigureRoot => figureRoot;
        public PrototypeUnderlayerSeam SeamA => seamA;
        public PrototypeUnderlayerSeam SeamB => seamB;

        public bool ControlledEntryOnly => true;
        public bool AnywhereWallPhaseEnabled => false;
        public bool UsesParallelTraversalGraph => true;
        public bool PaletteKnifeUsesExistingUseToolActionReadOnly => true;
        public bool AddsNewGameplayInputAction => false;
        public bool PainterCanSeeSurfaceSignal => true;
        public bool FigureFullyInvisibleToPainter => false;
        public bool SafeSeamRecoveryEnabled => true;
        public bool TraceWeavePortalJumpSuppressed => true;
        public bool FrameGunBraceOverridesOilSealTraversal => true;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            Transform configuredFigureRoot,
            MonoBehaviour configuredFigureMotor,
            MonoBehaviour configuredPaletteKnifeController,
            MonoBehaviour configuredTraceWeaveRecorder,
            PrototypeUnderlayerSeam configuredSeamA,
            PrototypeUnderlayerSeam configuredSeamB,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            figureRoot =
                configuredFigureRoot;

            figureMotor =
                configuredFigureMotor;

            paletteKnifeController =
                configuredPaletteKnifeController;

            traceWeaveRecorder =
                configuredTraceWeaveRecorder;

            seamA =
                configuredSeamA;

            seamB =
                configuredSeamB;

            statusGroup =
                configuredStatusGroup;

            titleText =
                configuredTitleText;

            stateText =
                configuredStateText;

            controlsText =
                configuredControlsText;

            ResolveFigureContracts();
            ResolvePaletteKnifeContract();
            EnsureSurfaceSignal();
            RefreshHud();
        }

        private void Awake()
        {
            ResolveFigureContracts();
            ResolvePaletteKnifeContract();
            EnsureSurfaceSignal();
        }

        private void OnEnable()
        {
            ResolveFigureContracts();
            ResolvePaletteKnifeContract();
            EnsureSurfaceSignal();
        }

        private void Update()
        {
            RefreshRole();
            RefreshPaletteKnifeState();
            RefreshNearestSeam();

            if (inUnderlayer)
            {
                RefreshUnderlayerProjection();
                CheckSafetyRecovery();
            }
            else
            {
                graphDistance = 0f;
                underlayerProgress = 0f;
                HideSurfaceSignal();
            }

            ProcessExistingPaletteKnifeAction();
            RefreshState();
            RefreshHud();
        }

        public bool ForceEnterFromA()
        {
            if (
                seamA == null ||
                !seamA.IsConfigured
            )
            {
                Reject(
                    "Force giriş için Seam A hazır değil.");

                return false;
            }

            return EnterUnderlayer(
                seamA,
                "Editor force A");
        }

        public bool ForceSurfaceNearest()
        {
            if (!inUnderlayer)
            {
                Reject(
                    "Figure şu anda Alt Katman'da değil.");

                return false;
            }

            PrototypeUnderlayerSeam target =
                FindNearestUnderlayerSeam();

            if (target == null)
            {
                Reject(
                    "Yakın güvenli çıkış dikişi bulunamadı.");

                return false;
            }

            return ExitUnderlayer(
                target,
                "Editor force surface");
        }

        public void ForceSafetyRecovery()
        {
            if (!inUnderlayer)
            {
                Reject(
                    "Safety recovery için Figure Alt Katman'da olmalı.");

                return;
            }

            RecoverToNearestSafeSurface(
                "Editor force recovery");
        }

        public bool TryEnterViaExternalSeam(
            PrototypeUnderlayerSeam seam,
            string source)
        {
            return EnterUnderlayer(
                seam,
                string.IsNullOrWhiteSpace(
                    source)
                    ? "External seam"
                    : source);
        }

        public bool TryExitViaExternalSeam(
            PrototypeUnderlayerSeam seam,
            string source)
        {
            return ExitUnderlayer(
                seam,
                string.IsNullOrWhiteSpace(
                    source)
                    ? "External seam"
                    : source);
        }

        public void ForceSafeSurfaceRecovery(
            string source)
        {
            if (!inUnderlayer)
            {
                return;
            }

            RecoverToNearestSafeSurface(
                string.IsNullOrWhiteSpace(
                    source)
                    ? "External safety recovery"
                    : source);
        }

        private void ResolveFigureContracts()
        {
            if (figureRoot == null)
            {
                return;
            }

            characterController =
                figureRoot.GetComponent<
                    CharacterController>();

            if (characterController == null)
            {
                characterController =
                    figureRoot.GetComponentInChildren<
                        CharacterController>(
                            true);
            }

            figureRigidbody =
                figureRoot.GetComponent<
                    Rigidbody>();

            if (figureRigidbody == null)
            {
                figureRigidbody =
                    figureRoot.GetComponentInChildren<
                        Rigidbody>(
                            true);
            }
        }

        private void ResolvePaletteKnifeContract()
        {
            paletteUseToolAction = null;
            paletteKnifeInputResolved = false;

            if (paletteKnifeController == null)
            {
                paletteKnifeInputContract =
                    "PaletteKnifeController missing";

                return;
            }

            Type runtimeType =
                paletteKnifeController.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            FieldInfo field =
                runtimeType.GetField(
                    "useToolAction",
                    flags);

            if (
                field != null &&
                typeof(InputActionReference)
                    .IsAssignableFrom(
                        field.FieldType)
            )
            {
                try
                {
                    paletteUseToolAction =
                        field.GetValue(
                            paletteKnifeController)
                        as InputActionReference;
                }
                catch (Exception)
                {
                    paletteUseToolAction =
                        null;
                }
            }

            paletteKnifeInputResolved =
                paletteUseToolAction != null &&
                paletteUseToolAction.action != null;

            paletteKnifeInputContract =
                paletteKnifeInputResolved
                    ? "Existing PaletteKnife useToolAction read-only"
                    : "useToolAction unresolved";
        }

        private void RefreshRole()
        {
            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedRole);
        }

        private void RefreshPaletteKnifeState()
        {
            paletteKnifePressed = false;

            if (paletteKnifeController == null)
            {
                paletteKnifeActive = false;
                return;
            }

            paletteKnifeActive =
                paletteKnifeController.enabled &&
                paletteKnifeController.gameObject.activeInHierarchy;

            if (
                paletteUseToolAction == null ||
                paletteUseToolAction.action == null
            )
            {
                ResolvePaletteKnifeContract();
            }
        }

        private void ProcessExistingPaletteKnifeAction()
        {
            if (
                !figureRoleActive ||
                !paletteKnifeActive ||
                !paletteKnifeInputResolved ||
                paletteUseToolAction == null ||
                paletteUseToolAction.action == null ||
                !paletteUseToolAction.action.enabled
            )
            {
                return;
            }

            paletteKnifePressed =
                paletteUseToolAction.action
                    .WasPressedThisFrame();

            if (!paletteKnifePressed)
            {
                return;
            }

            if (
                currentNearestSeam == null ||
                nearestSeamDistance >
                    seamUseRadius
            )
            {
                return;
            }

            if (inUnderlayer)
            {
                ExitUnderlayer(
                    currentNearestSeam,
                    "Existing PaletteKnife UseTool");

                return;
            }

            EnterUnderlayer(
                currentNearestSeam,
                "Existing PaletteKnife UseTool");
        }

        private void RefreshNearestSeam()
        {
            currentNearestSeam = null;
            nearestSeamDistance =
                float.PositiveInfinity;

            nearestSeam = "None";

            if (figureRoot == null)
            {
                return;
            }

            EvaluateSeamDistance(
                seamA);

            EvaluateSeamDistance(
                seamB);

            if (
                float.IsPositiveInfinity(
                    nearestSeamDistance)
            )
            {
                nearestSeamDistance = -1f;
            }
        }

        private void EvaluateSeamDistance(
            PrototypeUnderlayerSeam seam)
        {
            if (
                seam == null ||
                !seam.IsConfigured
            )
            {
                return;
            }

            Transform anchor =
                inUnderlayer
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

            if (
                distance <
                nearestSeamDistance
            )
            {
                nearestSeamDistance =
                    distance;

                currentNearestSeam =
                    seam;

                nearestSeam =
                    seam.SeamId.ToString();
            }
        }

        private bool EnterUnderlayer(
            PrototypeUnderlayerSeam seam,
            string source)
        {
            if (
                seam == null ||
                !seam.IsConfigured ||
                figureRoot == null
            )
            {
                Reject(
                    "Alt Katman giriş sözleşmesi eksik.");

                return false;
            }

            if (inUnderlayer)
            {
                Reject(
                    "Figure zaten Alt Katman'da.");

                return false;
            }

            if (seam.BlocksTraversal)
            {
                Reject(
                    $"Dikiş {seam.SeamId} Yağlı Boyayla mühürlü; Çerçeve Ankrajı gerekli.");

                return false;
            }

            seam.OpenForSeconds(
                seamOpenSeconds);

            Vector3 direction =
                GetUnderlayerDirectionAwayFromSeam(
                    seam);

            Vector3 destination =
                seam.UnderlayerAnchor.position +
                direction *
                underlayerSpawnInset;

            SuppressTraceRecorder();
            RelocateFigure(
                destination);

            inUnderlayer = true;
            safetyRecoveryArmed = true;
            diveCount++;

            lastAction =
                $"Alt Katman'a girildi • Dikiş {seam.SeamId}.";

            Debug.Log(
                "[M52.0 Underlayer Dive Enter]\n" +
                $"Source={source}\n" +
                $"Seam={seam.SeamId}\n" +
                $"Destination={destination:F3}\n" +
                "ControlledEntryOnly=True\n" +
                "AnywhereWallPhaseEnabled=False\n" +
                "UsesParallelTraversalGraph=True\n" +
                "PaletteKnifeUsesExistingUseToolActionReadOnly=True\n" +
                "AddsNewGameplayInputAction=False\n" +
                "TraceWeavePortalJumpSuppressed=True\n" +
                "NetworkAuthorityEnabled=False",
                this);

            return true;
        }

        private bool ExitUnderlayer(
            PrototypeUnderlayerSeam seam,
            string source)
        {
            if (
                seam == null ||
                !seam.IsConfigured ||
                figureRoot == null
            )
            {
                Reject(
                    "Alt Katman çıkış sözleşmesi eksik.");

                return false;
            }

            if (!inUnderlayer)
            {
                Reject(
                    "Figure yüzeyde; Alt Katman çıkışı kullanılamaz.");

                return false;
            }

            if (seam.BlocksTraversal)
            {
                Reject(
                    $"Dikiş {seam.SeamId} Yağlı Boyayla mühürlü; diğer çıkışı kullan veya Çerçeve Ankrajıyla açık tut.");

                return false;
            }

            seam.OpenForSeconds(
                seamOpenSeconds);

            Vector3 surfaceForward =
                seam.SurfaceAnchor.forward;

            Vector3 destination =
                seam.SurfaceAnchor.position +
                surfaceForward *
                0.85f +
                Vector3.up *
                0.08f;

            RelocateFigure(
                destination);

            inUnderlayer = false;
            surfaceCount++;

            RestoreTraceRecorder();
            HideSurfaceSignal();

            lastAction =
                $"Yüzeye çıkıldı • Dikiş {seam.SeamId}.";

            Debug.Log(
                "[M52.0 Underlayer Dive Exit]\n" +
                $"Source={source}\n" +
                $"Seam={seam.SeamId}\n" +
                $"Destination={destination:F3}\n" +
                "TraceRecorderRestored=True\n" +
                "SafeSurfaceExit=True",
                this);

            return true;
        }

        private void SuppressTraceRecorder()
        {
            if (
                traceWeaveRecorder == null ||
                traceRecorderStateCaptured
            )
            {
                return;
            }

            savedTraceRecorderEnabled =
                traceWeaveRecorder.enabled;

            traceRecorderStateCaptured = true;

            if (traceWeaveRecorder.enabled)
            {
                traceWeaveRecorder.enabled =
                    false;

                traceRecorderSuppressed = true;
                traceSuppressCount++;
            }
        }

        private void RestoreTraceRecorder()
        {
            if (
                traceWeaveRecorder == null ||
                !traceRecorderStateCaptured
            )
            {
                traceRecorderSuppressed = false;
                traceRecorderStateCaptured = false;
                return;
            }

            traceWeaveRecorder.enabled =
                savedTraceRecorderEnabled;

            if (traceRecorderSuppressed)
            {
                traceRestoreCount++;
            }

            traceRecorderSuppressed = false;
            traceRecorderStateCaptured = false;
        }

        private void RelocateFigure(
            Vector3 destination)
        {
            bool controllerWasEnabled =
                characterController != null &&
                characterController.enabled;

            if (controllerWasEnabled)
            {
                characterController.enabled =
                    false;
            }

            figureRoot.position =
                destination;

            if (
                figureRigidbody != null &&
                !figureRigidbody.isKinematic
            )
            {
#if UNITY_6000_0_OR_NEWER
                figureRigidbody.linearVelocity =
                    Vector3.zero;
#else
                figureRigidbody.velocity =
                    Vector3.zero;
#endif
                figureRigidbody.angularVelocity =
                    Vector3.zero;
            }

            if (controllerWasEnabled)
            {
                characterController.enabled =
                    true;
            }

            Physics.SyncTransforms();
        }

        private Vector3 GetUnderlayerDirectionAwayFromSeam(
            PrototypeUnderlayerSeam seam)
        {
            if (
                seamA == null ||
                seamB == null ||
                seamA.UnderlayerAnchor == null ||
                seamB.UnderlayerAnchor == null
            )
            {
                return Vector3.forward;
            }

            Vector3 a =
                seamA.UnderlayerAnchor.position;

            Vector3 b =
                seamB.UnderlayerAnchor.position;

            Vector3 direction =
                Vector3.ProjectOnPlane(
                    b - a,
                    Vector3.up);

            if (
                direction.sqrMagnitude <=
                0.0001f
            )
            {
                direction =
                    Vector3.forward;
            }
            else
            {
                direction.Normalize();
            }

            return
                seam.SeamId ==
                    PrototypeUnderlayerSeamId.A
                    ? direction
                    : -direction;
        }

        private void RefreshUnderlayerProjection()
        {
            if (
                figureRoot == null ||
                seamA == null ||
                seamB == null ||
                seamA.UnderlayerAnchor == null ||
                seamB.UnderlayerAnchor == null ||
                seamA.SurfaceAnchor == null ||
                seamB.SurfaceAnchor == null
            )
            {
                HideSurfaceSignal();
                return;
            }

            Vector3 underA =
                seamA.UnderlayerAnchor.position;

            Vector3 underB =
                seamB.UnderlayerAnchor.position;

            Vector3 segment =
                underB -
                underA;

            float lengthSquared =
                segment.sqrMagnitude;

            if (
                lengthSquared <=
                0.0001f
            )
            {
                underlayerProgress = 0f;
                graphDistance = 0f;
                HideSurfaceSignal();
                return;
            }

            float projection =
                Vector3.Dot(
                    figureRoot.position -
                    underA,
                    segment) /
                lengthSquared;

            underlayerProgress =
                Mathf.Clamp01(
                    projection);

            Vector3 nearest =
                Vector3.Lerp(
                    underA,
                    underB,
                    underlayerProgress);

            graphDistance =
                Vector3.Distance(
                    figureRoot.position,
                    nearest);

            projectedSurfacePosition =
                Vector3.Lerp(
                    seamA.SurfaceAnchor.position,
                    seamB.SurfaceAnchor.position,
                    underlayerProgress);

            UpdateSurfaceSignal(
                projectedSurfacePosition);
        }

        private void CheckSafetyRecovery()
        {
            if (
                !safetyRecoveryArmed ||
                graphDistance <=
                    maximumUnderlayerGraphDistance
            )
            {
                return;
            }

            RecoverToNearestSafeSurface(
                $"Graph distance {graphDistance:F2} m");
        }

        private void RecoverToNearestSafeSurface(
            string reason)
        {
            state =
                PrototypeUnderlayerDiveState.SafetyRecovery;

            PrototypeUnderlayerSeam seam =
                FindNearestUnderlayerSeam();

            if (
                seam == null ||
                seam.SurfaceAnchor == null
            )
            {
                Reject(
                    "Safety recovery güvenli dikiş bulamadı.");

                return;
            }

            Vector3 destination =
                seam.SurfaceAnchor.position +
                seam.SurfaceAnchor.forward *
                0.85f +
                Vector3.up *
                0.08f;

            RelocateFigure(
                destination);

            inUnderlayer = false;
            safetyRecoveryCount++;

            RestoreTraceRecorder();
            HideSurfaceSignal();

            lastAction =
                $"Güvenli dikişe geri alındı • {seam.SeamId}.";

            Debug.LogWarning(
                "[M52.0 Underlayer Safety Recovery]\n" +
                $"Reason={reason}\n" +
                $"Seam={seam.SeamId}\n" +
                $"RecoveryCount={safetyRecoveryCount}\n" +
                "SafeSeamRecoveryEnabled=True",
                this);
        }

        private PrototypeUnderlayerSeam
            FindNearestUnderlayerSeam()
        {
            if (
                figureRoot == null
            )
            {
                return null;
            }

            PrototypeUnderlayerSeam best = null;

            float bestDistance =
                float.PositiveInfinity;

            EvaluateUnderlayerRecoverySeam(
                seamA,
                ref best,
                ref bestDistance);

            EvaluateUnderlayerRecoverySeam(
                seamB,
                ref best,
                ref bestDistance);

            return best;
        }

        private void EvaluateUnderlayerRecoverySeam(
            PrototypeUnderlayerSeam seam,
            ref PrototypeUnderlayerSeam best,
            ref float bestDistance)
        {
            if (
                seam == null ||
                seam.UnderlayerAnchor == null
            )
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    figureRoot.position,
                    seam.UnderlayerAnchor.position);

            if (
                distance <
                bestDistance
            )
            {
                best = seam;
                bestDistance = distance;
            }
        }

        private void EnsureSurfaceSignal()
        {
            if (
                surfaceSignalRing != null &&
                surfaceSignalBump != null
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
                surfaceSignalMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M52_0_SurfaceSignal_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject ringObject =
                new GameObject(
                    "M52_0_SurfaceBulgeRing");

            ringObject.transform.SetParent(
                transform,
                false);

            surfaceSignalRing =
                ringObject.AddComponent<
                    LineRenderer>();

            ConfigureSignalLine(
                surfaceSignalRing,
                0.045f);

            surfaceSignalRing.loop = true;
            surfaceSignalRing.positionCount = 24;

            GameObject bumpObject =
                new GameObject(
                    "M52_0_SurfaceBulgeBump");

            bumpObject.transform.SetParent(
                transform,
                false);

            surfaceSignalBump =
                bumpObject.AddComponent<
                    LineRenderer>();

            ConfigureSignalLine(
                surfaceSignalBump,
                0.055f);

            surfaceSignalBump.loop = false;
            surfaceSignalBump.positionCount = 3;

            HideSurfaceSignal();
        }

        private void ConfigureSignalLine(
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
                surfaceSignalMaterial;

            Color color =
                new Color(
                    0.18f,
                    0.09f,
                    0.20f,
                    0.72f);

            line.startColor = color;
            line.endColor = color;
        }

        private void UpdateSurfaceSignal(
            Vector3 surfacePosition)
        {
            EnsureSurfaceSignal();

            if (
                surfaceSignalRing == null ||
                surfaceSignalBump == null
            )
            {
                return;
            }

            Vector3 center =
                surfacePosition +
                Vector3.up *
                0.055f;

            int count =
                surfaceSignalRing.positionCount;

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
                    surfaceSignalRadius;

                surfaceSignalRing.SetPosition(
                    index,
                    center +
                    offset);
            }

            surfaceSignalBump.SetPosition(
                0,
                center -
                Vector3.right *
                surfaceSignalRadius *
                0.42f);

            surfaceSignalBump.SetPosition(
                1,
                center +
                Vector3.up *
                surfaceSignalHeight);

            surfaceSignalBump.SetPosition(
                2,
                center +
                Vector3.right *
                surfaceSignalRadius *
                0.42f);

            surfaceSignalRing.enabled = true;
            surfaceSignalBump.enabled = true;
            surfaceSignalVisible = true;
        }

        private void HideSurfaceSignal()
        {
            if (surfaceSignalRing != null)
            {
                surfaceSignalRing.enabled = false;
            }

            if (surfaceSignalBump != null)
            {
                surfaceSignalBump.enabled = false;
            }

            surfaceSignalVisible = false;
        }

        private void RefreshState()
        {
            if (
                figureRoot == null
            )
            {
                state =
                    PrototypeUnderlayerDiveState.WaitingForFigure;

                return;
            }

            if (!figureRoleActive)
            {
                state =
                    PrototypeUnderlayerDiveState.PainterRole;

                return;
            }

            if (inUnderlayer)
            {
                state =
                    currentNearestSeam != null &&
                    nearestSeamDistance >= 0f &&
                    nearestSeamDistance <=
                        seamUseRadius
                        ? PrototypeUnderlayerDiveState
                            .UnderlayerExitInRange
                        : PrototypeUnderlayerDiveState
                            .Underlayer;

                return;
            }

            if (
                currentNearestSeam != null &&
                nearestSeamDistance >= 0f &&
                nearestSeamDistance <=
                    seamUseRadius
            )
            {
                state =
                    paletteKnifeActive
                        ? PrototypeUnderlayerDiveState
                            .SurfaceSeamInRange
                        : PrototypeUnderlayerDiveState
                            .PaletteKnifeRequired;

                return;
            }

            state =
                PrototypeUnderlayerDiveState.Surface;
        }

        private void Reject(
            string reason)
        {
            rejectedUseCount++;
            lastAction = reason;
        }

        private void RefreshHud()
        {
            bool relevantNow =
                figureRoleActive &&
                (inUnderlayer ||
                 state == PrototypeUnderlayerDiveState.SurfaceSeamInRange ||
                 state == PrototypeUnderlayerDiveState.UnderlayerExitInRange ||
                 state == PrototypeUnderlayerDiveState.SafetyRecovery ||
                 (nearestSeamDistance >= 0f &&
                  nearestSeamDistance <= seamUseRadius * 1.15f));

            bool visible =
                PrototypeRuntimeHudVisibilityUtility.Hold(
                    relevantNow,
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
                    inUnderlayer
                        ? "ALT KATMAN • PARALEL TRAVERSAL"
                        : "ALT KATMAN • DİKİŞ GİRİŞİ";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"DURUM {TranslateState(state)}   " +
                    $"DİKİŞ {nearestSeam}   " +
                    $"MESAFE {(nearestSeamDistance >= 0f ? nearestSeamDistance.ToString("F2") : "-")} m\n" +
                    $"PALET {(paletteKnifeActive ? "AKTİF" : "KAPALI")}   " +
                    $"INPUT {(paletteKnifeInputResolved ? "OK" : "YOK")}   " +
                    $"M51 TRACE {(traceRecorderSuppressed ? "ASKIDA" : "NORMAL")}\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    inUnderlayer
                        ? "DİĞER ALT DİKİŞE İLERLE • 1 PALET BIÇAĞI + MEVCUT E İLE YÜZEYE ÇIK\n" +
                          "RESSAM YÜZEYDE KABARIKLIK/GÖLGE SİNYALİNİ GÖREBİLİR"
                        : "1 • PALET BIÇAĞI   DİKİŞE YAKLAŞ   MEVCUT E • ALT KATMANA GİR\n" +
                          "YENİ GAMEPLAY TUŞU YOK • HER YERDEN DUVAR İÇİ GEÇİŞ YOK";
            }
        }

        private static string TranslateState(
            PrototypeUnderlayerDiveState value)
        {
            switch (value)
            {
                case PrototypeUnderlayerDiveState.PainterRole:
                    return "PAINTER ROLÜ";

                case PrototypeUnderlayerDiveState.Surface:
                    return "YÜZEY";

                case PrototypeUnderlayerDiveState.SurfaceSeamInRange:
                    return "GİRİŞ HAZIR";

                case PrototypeUnderlayerDiveState.PaletteKnifeRequired:
                    return "PALET BIÇAĞI GEREKLİ";

                case PrototypeUnderlayerDiveState.Underlayer:
                    return "ALT KATMAN";

                case PrototypeUnderlayerDiveState.UnderlayerExitInRange:
                    return "ÇIKIŞ HAZIR";

                case PrototypeUnderlayerDiveState.SafetyRecovery:
                    return "GÜVENLİ DÖNÜŞ";

                default:
                    return "BEKLİYOR";
            }
        }

        private void OnDisable()
        {
            paletteKnifePressed = false;

            if (
                !inUnderlayer
            )
            {
                HideSurfaceSignal();
            }
        }

        private void OnDestroy()
        {
            RestoreTraceRecorder();

            if (surfaceSignalMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    surfaceSignalMaterial);
            }
            else
            {
                DestroyImmediate(
                    surfaceSignalMaterial);
            }

            surfaceSignalMaterial = null;
        }
    }
}
