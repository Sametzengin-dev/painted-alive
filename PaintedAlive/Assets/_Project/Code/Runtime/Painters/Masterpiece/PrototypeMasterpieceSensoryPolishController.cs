using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(26000)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceSensoryPolishController :
        MonoBehaviour
    {
        private sealed class PulseState
        {
            public GameObject GameObject;
            public LineRenderer Renderer;
            public bool Active;
            public bool GroundPlane;
            public Transform FollowTarget;
            public Vector3 Position;
            public Vector3 FollowOffset;
            public Color Color;
            public float StartedAt;
            public float Duration;
            public float StartRadius;
            public float EndRadius;
            public float StartWidth;
            public float EndWidth;
            public int SegmentCount;
        }

        [Header("Sources")]
        [SerializeField]
        private PrototypeMasterpieceWorldDeploymentController deployment;

        [SerializeField]
        private PrototypeMasterpiecePossessionController possession;

        [SerializeField]
        private PrototypeMasterpieceVisualSafetyController visualSafety;

        [SerializeField] private Camera mainCamera;

        [Header("HUD")]
        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text controlsText;

        [Header("Quality")]
        [SerializeField]
        private PrototypeMasterpiecePolishQualityMode requestedQuality =
            PrototypeMasterpiecePolishQualityMode.Auto;

        [SerializeField, Min(0.1f)]
        private float qualityEvaluationInterval = 1f;

        [SerializeField, Min(1f)]
        private float fullToReducedFrameMs = 22f;

        [SerializeField, Min(1f)]
        private float reducedToMinimalFrameMs = 31f;

        [SerializeField, Min(1f)]
        private float reducedToFullFrameMs = 18.5f;

        [SerializeField, Min(1f)]
        private float minimalToReducedFrameMs = 26f;

        [Header("Footstep Rhythm")]
        [SerializeField, Min(0.2f)]
        private float fullStepDistance = 0.56f;

        [SerializeField, Min(0.2f)]
        private float reducedStepDistance = 0.72f;

        [SerializeField, Min(0.2f)]
        private float minimalStepDistance = 0.94f;

        [Header("Audio")]
        [SerializeField, Range(0f, 1f)]
        private float masterVolume = 0.62f;

        [SerializeField, Range(0f, 1f)]
        private float footstepVolume = 0.34f;

        [SerializeField, Range(0f, 1f)]
        private float attackVolume = 0.55f;

        [SerializeField, Min(1f)]
        private float audioMaximumDistance = 22f;

        [Header("VFX")]
        [SerializeField, Range(4, 24)]
        private int pulsePoolSize = 14;

        [SerializeField, Min(0.01f)]
        private float groundLift = 0.035f;

        [SerializeField] private Color possessionColor =
            new Color(1f, 0.48f, 0.05f, 1f);

        [SerializeField] private Color releaseColor =
            new Color(0.08f, 0.72f, 0.78f, 1f);

        [SerializeField] private Color footstepColor =
            new Color(0.94f, 0.90f, 0.78f, 1f);

        [SerializeField] private Color attackColor =
            new Color(1f, 0.19f, 0.035f, 1f);

        [SerializeField] private Color safeModeColor =
            new Color(0.08f, 0.76f, 0.82f, 1f);

        [SerializeField] private Color fallbackModeColor =
            new Color(0.38f, 0.43f, 0.94f, 1f);

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeMasterpieceWorldInstance activeInstance;

        [SerializeField]
        private PrototypeMasterpieceResolvedPolishTier resolvedTier =
            PrototypeMasterpieceResolvedPolishTier.Full;

        [SerializeField] private bool inputActionsActive;
        [SerializeField] private bool audioMuted;
        [SerializeField] private bool previousPossessed;
        [SerializeField] private bool previousAttackActive;
        [SerializeField] private bool previousReportActive;
        [SerializeField]
        private PrototypeMasterpieceVisualPolicyMode previousVisualMode =
            PrototypeMasterpieceVisualPolicyMode.Original;

        [SerializeField] private int possessionCueCount;
        [SerializeField] private int releaseCueCount;
        [SerializeField] private int footstepCueCount;
        [SerializeField] private int attackStartCueCount;
        [SerializeField] private int attackReleaseCueCount;
        [SerializeField] private int visualModeCueCount;
        [SerializeField] private int reportCueCount;
        [SerializeField] private int qualityChangeCount;
        [SerializeField] private int automaticDowngradeCount;
        [SerializeField] private int automaticUpgradeCount;
        [SerializeField] private int droppedPulseCount;
        [SerializeField] private int activePulseCount;
        [SerializeField] private int peakActivePulseCount;
        [SerializeField] private int generatedClipCount;
        [SerializeField] private int audioPlayCount;
        [SerializeField] private int testBurstCount;
        [SerializeField] private float smoothedFrameMilliseconds;
        [SerializeField] private float lastObservedTravelDistance;
        [SerializeField] private float stepDistanceAccumulator;
        [SerializeField] private string lastAction =
            "M48 dünya Baş Yapıtı bekleniyor.";

        private readonly List<PulseState> pulsePool =
            new List<PulseState>();

        private readonly List<AudioSource> audioSources =
            new List<AudioSource>();

        private InputAction cycleQualityAction;
        private InputAction toggleAudioAction;

        private Material pulseMaterial;
        private AudioClip possessionClip;
        private AudioClip releaseClip;
        private AudioClip footstepClip;
        private AudioClip attackChargeClip;
        private AudioClip attackReleaseClip;
        private AudioClip modeChangeClip;

        private int nextAudioSourceIndex;
        private bool nextFootUsesLeft = true;
        private PrototypeMasterpiecePartKind activeAttackPart =
            PrototypeMasterpiecePartKind.LeftAttack;

        private float nextQualityEvaluationAt;
        private int boundSnapshotHash;

        public PrototypeMasterpiecePolishQualityMode RequestedQuality =>
            requestedQuality;
        public PrototypeMasterpieceResolvedPolishTier ResolvedTier =>
            resolvedTier;
        public bool InputActionsActive => inputActionsActive;
        public bool AudioMuted => audioMuted;
        public int PossessionCueCount => possessionCueCount;
        public int ReleaseCueCount => releaseCueCount;
        public int FootstepCueCount => footstepCueCount;
        public int AttackStartCueCount => attackStartCueCount;
        public int AttackReleaseCueCount => attackReleaseCueCount;
        public int VisualModeCueCount => visualModeCueCount;
        public int ReportCueCount => reportCueCount;
        public int QualityChangeCount => qualityChangeCount;
        public int AutomaticDowngradeCount =>
            automaticDowngradeCount;
        public int AutomaticUpgradeCount =>
            automaticUpgradeCount;
        public int DroppedPulseCount => droppedPulseCount;
        public int ActivePulseCount => activePulseCount;
        public int PeakActivePulseCount => peakActivePulseCount;
        public int GeneratedClipCount => generatedClipCount;
        public int AudioPlayCount => audioPlayCount;
        public int TestBurstCount => testBurstCount;
        public float SmoothedFrameMilliseconds =>
            smoothedFrameMilliseconds;
        public int PulsePoolCount => pulsePool.Count;
        public int AudioSourcePoolCount => audioSources.Count;
        public string LastAction => lastAction;

        public bool GameplayStateWrites => false;
        public bool GameplayProxyModified => false;
        public bool CapabilityStateModified => false;
        public bool ClarityModified => false;
        public bool DamageAuthorityEnabled => false;
        public bool HitAuthorityEnabled => false;
        public bool BossAIEnabled => false;
        public bool ExternalAudioAssetsRequired => false;
        public bool RuntimePoolingEnabled => true;
        public bool FinalLocomotionAnimationIncluded => false;
        public bool NetworkIntegrationParked => true;

        public void Configure(
            PrototypeMasterpieceWorldDeploymentController
                configuredDeployment,
            PrototypeMasterpiecePossessionController
                configuredPossession,
            PrototypeMasterpieceVisualSafetyController
                configuredVisualSafety,
            Camera configuredMainCamera,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            deployment = configuredDeployment;
            possession = configuredPossession;
            visualSafety = configuredVisualSafety;
            mainCamera = configuredMainCamera;
            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            EnsureRuntimeResources();
            ResolveInstance();
            RefreshHud();
        }

        private void Awake()
        {
            EnsureInputActions();
            EnsureRuntimeResources();
            ResolveInstance();
            RefreshHud();
        }

        private void OnEnable()
        {
            EnsureInputActions();
            EnableInputActions();
            EnsureRuntimeResources();
            ResolveInstance();
            RefreshHud();
        }

        private void Update()
        {
            UpdateFrameTelemetry();
            ResolveInstance();
            EvaluateQuality();
            DetectPossessionTransition();
            DetectAttackTransition();
            DetectVisualPolicyTransition();
            DetectFootsteps();
            UpdatePulsePool();
            RefreshHud();
        }

        public void CycleQualityMode()
        {
            switch (requestedQuality)
            {
                case PrototypeMasterpiecePolishQualityMode.Auto:
                    requestedQuality =
                        PrototypeMasterpiecePolishQualityMode.Full;
                    break;

                case PrototypeMasterpiecePolishQualityMode.Full:
                    requestedQuality =
                        PrototypeMasterpiecePolishQualityMode.Reduced;
                    break;

                case PrototypeMasterpiecePolishQualityMode.Reduced:
                    requestedQuality =
                        PrototypeMasterpiecePolishQualityMode.Minimal;
                    break;

                default:
                    requestedQuality =
                        PrototypeMasterpiecePolishQualityMode.Auto;
                    break;
            }

            qualityChangeCount++;
            ApplyRequestedQuality(
                countAutomaticChange: false);

            lastAction =
                $"Efekt kalite isteği {requestedQuality}.";

            RefreshHud();
        }

        public void ToggleAudioMute()
        {
            audioMuted =
                !audioMuted;

            lastAction =
                audioMuted
                    ? "M48.2 prosedürel sesleri kapatıldı."
                    : "M48.2 prosedürel sesleri açıldı.";

            RefreshHud();
        }

        public void TriggerTestBurst()
        {
            if (activeInstance == null)
            {
                lastAction =
                    "Test efekti için aktif dünya Baş Yapıtı gerekli.";

                RefreshHud();
                return;
            }

            Vector3 core =
                ResolvePartPosition(
                    PrototypeMasterpiecePartKind.Core,
                    activeInstance.transform.position +
                    Vector3.up *
                    1.2f);

            SpawnPossessionBurst(
                core);

            SpawnAttackPulse(
                ResolvePartPosition(
                    PrototypeMasterpiecePartKind.RightAttack,
                    core +
                    activeInstance.transform.right));

            PlayWorldClip(
                modeChangeClip,
                core,
                0.65f,
                1f);

            testBurstCount++;
            lastAction =
                "M48.2 duyusal test patlaması çalıştırıldı.";
        }

        private void EnsureInputActions()
        {
            if (cycleQualityAction == null)
            {
                cycleQualityAction =
                    new InputAction(
                        "M48.2 Cycle Polish Quality",
                        InputActionType.Button,
                        "<Keyboard>/l");

                cycleQualityAction.performed +=
                    HandleCycleQuality;
            }

            if (toggleAudioAction == null)
            {
                toggleAudioAction =
                    new InputAction(
                        "M48.2 Toggle Procedural Audio",
                        InputActionType.Button,
                        "<Keyboard>/m");

                toggleAudioAction.performed +=
                    HandleToggleAudio;
            }
        }

        private void EnableInputActions()
        {
            cycleQualityAction?.Enable();
            toggleAudioAction?.Enable();

            inputActionsActive =
                cycleQualityAction != null &&
                cycleQualityAction.enabled &&
                toggleAudioAction != null &&
                toggleAudioAction.enabled;
        }

        private void DisableInputActions()
        {
            cycleQualityAction?.Disable();
            toggleAudioAction?.Disable();
            inputActionsActive = false;
        }

        private void HandleCycleQuality(
            InputAction.CallbackContext context)
        {
            CycleQualityMode();
        }

        private void HandleToggleAudio(
            InputAction.CallbackContext context)
        {
            ToggleAudioMute();
        }

        private void EnsureRuntimeResources()
        {
            if (mainCamera == null)
            {
                mainCamera =
                    Camera.main;
            }

            EnsurePulseMaterial();
            EnsurePulsePool();
            EnsureAudioPool();
            EnsureGeneratedClips();
        }

        private void EnsurePulseMaterial()
        {
            if (pulseMaterial != null)
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

            pulseMaterial =
                new Material(
                    shader)
                {
                    name =
                        "M48_2_SensoryPulse_Runtime",
                    hideFlags =
                        HideFlags.DontSave
                };

            if (pulseMaterial.HasProperty(
                    "_BaseColor"))
            {
                pulseMaterial.SetColor(
                    "_BaseColor",
                    Color.white);
            }

            if (pulseMaterial.HasProperty(
                    "_Color"))
            {
                pulseMaterial.SetColor(
                    "_Color",
                    Color.white);
            }
        }

        private void EnsurePulsePool()
        {
            int targetCount =
                Mathf.Clamp(
                    pulsePoolSize,
                    4,
                    24);

            while (pulsePool.Count <
                   targetCount)
            {
                int index =
                    pulsePool.Count;

                GameObject pulseObject =
                    new GameObject(
                        $"M48_2_PooledPulse_{index:00}");

                pulseObject.transform.SetParent(
                    transform,
                    false);

                LineRenderer line =
                    pulseObject.AddComponent<
                        LineRenderer>();

                line.useWorldSpace = true;
                line.loop = true;
                line.alignment =
                    LineAlignment.View;

                line.textureMode =
                    LineTextureMode.Stretch;

                line.numCapVertices = 3;
                line.numCornerVertices = 3;
                line.shadowCastingMode =
                    ShadowCastingMode.Off;

                line.receiveShadows = false;
                line.sharedMaterial =
                    pulseMaterial;

                line.enabled = false;

                pulsePool.Add(
                    new PulseState
                    {
                        GameObject = pulseObject,
                        Renderer = line
                    });
            }
        }

        private void EnsureAudioPool()
        {
            const int sourceCount = 5;

            while (audioSources.Count <
                   sourceCount)
            {
                int index =
                    audioSources.Count;

                GameObject audioObject =
                    new GameObject(
                        $"M48_2_AudioSource_{index:00}");

                audioObject.transform.SetParent(
                    transform,
                    false);

                AudioSource source =
                    audioObject.AddComponent<
                        AudioSource>();

                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                source.rolloffMode =
                    AudioRolloffMode.Linear;

                source.minDistance = 1.2f;
                source.maxDistance =
                    audioMaximumDistance;

                audioSources.Add(
                    source);
            }
        }

        private void EnsureGeneratedClips()
        {
            if (possessionClip != null)
            {
                return;
            }

            possessionClip =
                CreateProceduralClip(
                    "M48_2_Possession_In",
                    0.34f,
                    165f,
                    360f,
                    0.13f,
                    0.68f,
                    1101);

            releaseClip =
                CreateProceduralClip(
                    "M48_2_Possession_Out",
                    0.30f,
                    310f,
                    135f,
                    0.16f,
                    0.56f,
                    1102);

            footstepClip =
                CreateProceduralClip(
                    "M48_2_Paper_Footstep",
                    0.11f,
                    92f,
                    62f,
                    0.62f,
                    0.46f,
                    1103);

            attackChargeClip =
                CreateProceduralClip(
                    "M48_2_Attack_Charge",
                    0.17f,
                    220f,
                    560f,
                    0.10f,
                    0.58f,
                    1104);

            attackReleaseClip =
                CreateProceduralClip(
                    "M48_2_Attack_Release",
                    0.19f,
                    610f,
                    120f,
                    0.48f,
                    0.66f,
                    1105);

            modeChangeClip =
                CreateProceduralClip(
                    "M48_2_Mode_Change",
                    0.13f,
                    380f,
                    470f,
                    0.08f,
                    0.44f,
                    1106);

            generatedClipCount = 6;
        }

        private static AudioClip CreateProceduralClip(
            string clipName,
            float duration,
            float startFrequency,
            float endFrequency,
            float noiseAmount,
            float amplitude,
            int seed)
        {
            const int sampleRate = 44100;

            int sampleCount =
                Mathf.Max(
                    64,
                    Mathf.CeilToInt(
                        duration *
                        sampleRate));

            float[] samples =
                new float[sampleCount];

            var random =
                new System.Random(
                    seed);

            double phase = 0d;

            for (int index = 0;
                 index < sampleCount;
                 index++)
            {
                float t =
                    index /
                    (float)
                    Mathf.Max(
                        1,
                        sampleCount - 1);

                float frequency =
                    Mathf.Lerp(
                        startFrequency,
                        endFrequency,
                        t);

                phase +=
                    Math.PI *
                    2d *
                    frequency /
                    sampleRate;

                float attack =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            t /
                            0.08f));

                float release =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            (1f - t) /
                            0.24f));

                float envelope =
                    attack *
                    release;

                float tone =
                    (float)
                    Math.Sin(
                        phase);

                float noise =
                    (float)
                    (
                        random.NextDouble() *
                        2d -
                        1d
                    );

                float paperFlutter =
                    Mathf.Sin(
                        t *
                        Mathf.PI *
                        19f) *
                    noiseAmount *
                    0.12f;

                samples[index] =
                    (
                        tone *
                        (1f - noiseAmount) +
                        noise *
                        noiseAmount +
                        paperFlutter
                    ) *
                    envelope *
                    amplitude;
            }

            AudioClip clip =
                AudioClip.Create(
                    clipName,
                    sampleCount,
                    1,
                    sampleRate,
                    false);

            clip.SetData(
                samples,
                0);

            return clip;
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

            activeInstance = candidate;
            boundSnapshotHash =
                activeInstance != null
                    ? activeInstance.SnapshotHash
                    : 0;

            previousPossessed =
                possession != null &&
                possession.Possessed;

            previousAttackActive =
                possession != null &&
                possession.AttackPreviewActive;

            previousVisualMode =
                visualSafety != null
                    ? visualSafety.CurrentMode
                    : PrototypeMasterpieceVisualPolicyMode.Original;

            previousReportActive =
                visualSafety != null &&
                visualSafety.ReportActive;

            lastObservedTravelDistance =
                possession != null
                    ? possession.TravelledDistance
                    : 0f;

            stepDistanceAccumulator = 0f;
            nextFootUsesLeft = true;

            if (activeInstance == null)
            {
                DeactivateAllPulses();
                lastAction =
                    "M48 dünya Baş Yapıtı bekleniyor.";
            }
            else
            {
                lastAction =
                    "M48.2 duyusal polish dünya instance'ına bağlandı.";
            }
        }

        private void DetectPossessionTransition()
        {
            bool current =
                possession != null &&
                possession.Possessed;

            if (current ==
                previousPossessed)
            {
                return;
            }

            previousPossessed =
                current;

            if (current)
            {
                HandlePossessionStarted();
            }
            else
            {
                HandlePossessionReleased();
            }
        }

        private void HandlePossessionStarted()
        {
            if (activeInstance == null)
            {
                return;
            }

            Vector3 core =
                ResolvePartPosition(
                    PrototypeMasterpiecePartKind.Core,
                    activeInstance.transform.position +
                    Vector3.up *
                    1.2f);

            SpawnPossessionBurst(
                core);

            PlayWorldClip(
                possessionClip,
                core,
                0.78f,
                1f);

            possessionCueCount++;
            lastObservedTravelDistance =
                possession != null
                    ? possession.TravelledDistance
                    : 0f;

            stepDistanceAccumulator = 0f;
            lastAction =
                "Sahiplenme giriş ses/VFX işareti oynatıldı.";
        }

        private void HandlePossessionReleased()
        {
            if (activeInstance == null)
            {
                return;
            }

            Vector3 core =
                ResolvePartPosition(
                    PrototypeMasterpiecePartKind.Core,
                    activeInstance.transform.position +
                    Vector3.up *
                    1.2f);

            SpawnPulse(
                core,
                releaseColor,
                0.32f,
                1.32f,
                0.42f,
                0.085f,
                0.015f,
                groundPlane: false,
                followTarget: null,
                Vector3.zero);

            PlayWorldClip(
                releaseClip,
                core,
                0.70f,
                1f);

            releaseCueCount++;
            lastAction =
                "Sahiplenme çıkış ses/VFX işareti oynatıldı.";
        }

        private void DetectAttackTransition()
        {
            bool current =
                possession != null &&
                possession.AttackPreviewActive;

            if (current ==
                previousAttackActive)
            {
                return;
            }

            previousAttackActive =
                current;

            if (activeInstance == null)
            {
                return;
            }

            if (current)
            {
                activeAttackPart =
                    possession.AttackPreviewCount %
                    2 ==
                    1
                        ? PrototypeMasterpiecePartKind.LeftAttack
                        : PrototypeMasterpiecePartKind.RightAttack;

                Vector3 point =
                    ResolvePartPosition(
                        activeAttackPart,
                        activeInstance.transform.position +
                        Vector3.up *
                        1.25f);

                SpawnAttackPulse(
                    point);

                PlayWorldClip(
                    attackChargeClip,
                    point,
                    attackVolume,
                    1f);

                attackStartCueCount++;
                lastAction =
                    "Saldırı anticipation işareti oynatıldı.";
            }
            else
            {
                Vector3 point =
                    ResolvePartPosition(
                        activeAttackPart,
                        activeInstance.transform.position +
                        Vector3.up *
                        1.25f);

                SpawnPulse(
                    point,
                    attackColor,
                    0.18f,
                    1.05f,
                    0.25f,
                    0.12f,
                    0.018f,
                    groundPlane: false,
                    followTarget: null,
                    Vector3.zero);

                PlayWorldClip(
                    attackReleaseClip,
                    point,
                    attackVolume,
                    1f);

                attackReleaseCueCount++;
                lastAction =
                    "Saldırı release/recovery işareti oynatıldı.";
            }
        }

        private void DetectVisualPolicyTransition()
        {
            if (visualSafety == null ||
                activeInstance == null)
            {
                return;
            }

            PrototypeMasterpieceVisualPolicyMode currentMode =
                visualSafety.CurrentMode;

            bool currentReport =
                visualSafety.ReportActive;

            if (currentMode !=
                previousVisualMode)
            {
                previousVisualMode =
                    currentMode;

                Vector3 core =
                    ResolvePartPosition(
                        PrototypeMasterpiecePartKind.Core,
                        activeInstance.transform.position +
                        Vector3.up *
                        1.2f);

                SpawnVisualModeCue(
                    core,
                    currentMode);

                PlayWorldClip(
                    modeChangeClip,
                    core,
                    0.58f,
                    ResolveModePitch(
                        currentMode));

                visualModeCueCount++;
                lastAction =
                    $"Görsel mod değişim işareti: {currentMode}.";
            }

            if (currentReport &&
                !previousReportActive)
            {
                Vector3 head =
                    ResolvePartPosition(
                        PrototypeMasterpiecePartKind.Head,
                        activeInstance.transform.position +
                        Vector3.up *
                        2f);

                SpawnPulse(
                    head,
                    safeModeColor,
                    0.14f,
                    0.92f,
                    0.48f,
                    0.11f,
                    0.012f,
                    groundPlane: false,
                    followTarget: null,
                    Vector3.zero);

                reportCueCount++;
                lastAction =
                    "Yerel rapor/gizleme görsel işareti oynatıldı.";
            }

            previousReportActive =
                currentReport;
        }

        private void DetectFootsteps()
        {
            if (
                activeInstance == null ||
                possession == null ||
                !possession.Possessed
            )
            {
                lastObservedTravelDistance =
                    possession != null
                        ? possession.TravelledDistance
                        : 0f;

                stepDistanceAccumulator = 0f;
                return;
            }

            float observed =
                possession.TravelledDistance;

            float delta =
                observed -
                lastObservedTravelDistance;

            lastObservedTravelDistance =
                observed;

            if (delta < 0f)
            {
                stepDistanceAccumulator = 0f;
                return;
            }

            stepDistanceAccumulator +=
                delta;

            float threshold =
                ResolveStepDistance();

            if (stepDistanceAccumulator <
                threshold)
            {
                return;
            }

            stepDistanceAccumulator -=
                threshold;

            PrototypeMasterpiecePartKind foot =
                nextFootUsesLeft
                    ? PrototypeMasterpiecePartKind.LeftContact
                    : PrototypeMasterpiecePartKind.RightContact;

            nextFootUsesLeft =
                !nextFootUsesLeft;

            Vector3 point =
                ResolvePartPosition(
                    foot,
                    activeInstance.transform.position);

            point.y +=
                groundLift;

            SpawnFootstepCue(
                point);

            float pitch =
                0.93f +
                Mathf.Sin(
                    footstepCueCount *
                    1.731f) *
                0.065f;

            PlayWorldClip(
                footstepClip,
                point,
                footstepVolume,
                pitch);

            footstepCueCount++;
            lastAction =
                $"{foot} zemin temas işareti.";
        }

        private void SpawnPossessionBurst(
            Vector3 position)
        {
            SpawnPulse(
                position,
                possessionColor,
                0.24f,
                1.48f,
                0.48f,
                0.115f,
                0.018f,
                groundPlane: false,
                followTarget: null,
                Vector3.zero);

            if (resolvedTier ==
                PrototypeMasterpieceResolvedPolishTier.Full)
            {
                SpawnPulse(
                    position,
                    releaseColor,
                    0.12f,
                    1.04f,
                    0.58f,
                    0.065f,
                    0.010f,
                    groundPlane: false,
                    followTarget: null,
                    Vector3.zero);
            }
        }

        private void SpawnFootstepCue(
            Vector3 position)
        {
            if (resolvedTier ==
                PrototypeMasterpieceResolvedPolishTier.Minimal)
            {
                return;
            }

            SpawnPulse(
                position,
                footstepColor,
                0.06f,
                resolvedTier ==
                    PrototypeMasterpieceResolvedPolishTier.Full
                    ? 0.43f
                    : 0.32f,
                0.30f,
                0.055f,
                0.008f,
                groundPlane: true,
                followTarget: null,
                Vector3.zero);
        }

        private void SpawnAttackPulse(
            Vector3 position)
        {
            SpawnPulse(
                position,
                attackColor,
                0.12f,
                0.86f,
                0.32f,
                0.105f,
                0.018f,
                groundPlane: false,
                followTarget: null,
                Vector3.zero);

            if (resolvedTier ==
                PrototypeMasterpieceResolvedPolishTier.Full)
            {
                SpawnPulse(
                    position,
                    possessionColor,
                    0.05f,
                    0.54f,
                    0.22f,
                    0.055f,
                    0.008f,
                    groundPlane: false,
                    followTarget: null,
                    Vector3.zero);
            }
        }

        private void SpawnVisualModeCue(
            Vector3 position,
            PrototypeMasterpieceVisualPolicyMode mode)
        {
            Color color;
            int pulseCount;

            switch (mode)
            {
                case PrototypeMasterpieceVisualPolicyMode.SafeSilhouette:
                    color = safeModeColor;
                    pulseCount = 2;
                    break;

                case PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback:
                    color = fallbackModeColor;
                    pulseCount = 3;
                    break;

                default:
                    color = footstepColor;
                    pulseCount = 1;
                    break;
            }

            if (resolvedTier ==
                PrototypeMasterpieceResolvedPolishTier.Minimal)
            {
                pulseCount = 1;
            }
            else if (
                resolvedTier ==
                    PrototypeMasterpieceResolvedPolishTier.Reduced &&
                pulseCount > 2
            )
            {
                pulseCount = 2;
            }

            for (int index = 0;
                 index < pulseCount;
                 index++)
            {
                SpawnPulse(
                    position,
                    color,
                    0.14f +
                    index *
                    0.10f,
                    0.72f +
                    index *
                    0.24f,
                    0.36f +
                    index *
                    0.08f,
                    0.075f,
                    0.010f,
                    groundPlane: false,
                    followTarget: null,
                    Vector3.zero);
            }
        }

        private bool SpawnPulse(
            Vector3 position,
            Color color,
            float startRadius,
            float endRadius,
            float duration,
            float startWidth,
            float endWidth,
            bool groundPlane,
            Transform followTarget,
            Vector3 followOffset)
        {
            int activeBudget =
                ResolveActivePulseBudget();

            if (activePulseCount >=
                activeBudget)
            {
                droppedPulseCount++;
                return false;
            }

            PulseState pulse =
                FindAvailablePulse();

            if (pulse == null ||
                pulse.Renderer == null)
            {
                droppedPulseCount++;
                return false;
            }

            pulse.Active = true;
            pulse.GroundPlane = groundPlane;
            pulse.FollowTarget = followTarget;
            pulse.Position = position;
            pulse.FollowOffset = followOffset;
            pulse.Color = color;
            pulse.StartedAt =
                Time.unscaledTime;

            pulse.Duration =
                Mathf.Max(
                    0.05f,
                    duration);

            pulse.StartRadius =
                Mathf.Max(
                    0.01f,
                    startRadius);

            pulse.EndRadius =
                Mathf.Max(
                    pulse.StartRadius,
                    endRadius);

            pulse.StartWidth =
                Mathf.Max(
                    0.002f,
                    startWidth);

            pulse.EndWidth =
                Mathf.Max(
                    0.001f,
                    endWidth);

            pulse.SegmentCount =
                ResolvePulseSegmentCount();

            pulse.Renderer.positionCount =
                pulse.SegmentCount;

            pulse.Renderer.enabled = true;
            pulse.GameObject.SetActive(true);

            activePulseCount++;
            peakActivePulseCount =
                Mathf.Max(
                    peakActivePulseCount,
                    activePulseCount);

            UpdatePulseGeometry(
                pulse,
                0f);

            return true;
        }

        private PulseState FindAvailablePulse()
        {
            for (int index = 0;
                 index < pulsePool.Count;
                 index++)
            {
                PulseState pulse =
                    pulsePool[index];

                if (pulse != null &&
                    !pulse.Active)
                {
                    return pulse;
                }
            }

            return null;
        }

        private void UpdatePulsePool()
        {
            activePulseCount = 0;

            float now =
                Time.unscaledTime;

            for (int index = 0;
                 index < pulsePool.Count;
                 index++)
            {
                PulseState pulse =
                    pulsePool[index];

                if (pulse == null ||
                    !pulse.Active)
                {
                    continue;
                }

                float elapsed =
                    now -
                    pulse.StartedAt;

                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        pulse.Duration);

                if (progress >= 1f)
                {
                    DeactivatePulse(
                        pulse);

                    continue;
                }

                UpdatePulseGeometry(
                    pulse,
                    progress);

                activePulseCount++;
            }

            peakActivePulseCount =
                Mathf.Max(
                    peakActivePulseCount,
                    activePulseCount);
        }

        private void UpdatePulseGeometry(
            PulseState pulse,
            float progress)
        {
            if (pulse?.Renderer == null)
            {
                return;
            }

            Vector3 center =
                pulse.FollowTarget != null
                    ? pulse.FollowTarget.TransformPoint(
                        pulse.FollowOffset)
                    : pulse.Position;

            float eased =
                1f -
                Mathf.Pow(
                    1f -
                    progress,
                    3f);

            float radius =
                Mathf.Lerp(
                    pulse.StartRadius,
                    pulse.EndRadius,
                    eased);

            float width =
                Mathf.Lerp(
                    pulse.StartWidth,
                    pulse.EndWidth,
                    progress);

            float fade =
                1f -
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress);

            Color color =
                pulse.Color;

            color.a *=
                fade;

            pulse.Renderer.startColor =
                color;

            pulse.Renderer.endColor =
                color;

            pulse.Renderer.widthMultiplier =
                width;

            int segments =
                Mathf.Max(
                    8,
                    pulse.SegmentCount);

            pulse.Renderer.positionCount =
                segments;

            Vector3 axisA;
            Vector3 axisB;

            if (pulse.GroundPlane)
            {
                axisA =
                    activeInstance != null
                        ? activeInstance.transform.right
                        : Vector3.right;

                axisB =
                    activeInstance != null
                        ? activeInstance.transform.forward
                        : Vector3.forward;
            }
            else
            {
                Camera camera =
                    mainCamera != null
                        ? mainCamera
                        : Camera.main;

                axisA =
                    camera != null
                        ? camera.transform.right
                        : Vector3.right;

                axisB =
                    camera != null
                        ? camera.transform.up
                        : Vector3.up;
            }

            for (int pointIndex = 0;
                 pointIndex < segments;
                 pointIndex++)
            {
                float angle =
                    pointIndex /
                    (float)segments *
                    Mathf.PI *
                    2f;

                Vector3 point =
                    center +
                    axisA *
                    (
                        Mathf.Cos(angle) *
                        radius
                    ) +
                    axisB *
                    (
                        Mathf.Sin(angle) *
                        radius
                    );

                pulse.Renderer.SetPosition(
                    pointIndex,
                    point);
            }
        }

        private void DeactivatePulse(
            PulseState pulse)
        {
            if (pulse == null)
            {
                return;
            }

            pulse.Active = false;
            pulse.FollowTarget = null;

            if (pulse.Renderer != null)
            {
                pulse.Renderer.enabled = false;
            }

            if (pulse.GameObject != null)
            {
                pulse.GameObject.SetActive(false);
            }
        }

        private void DeactivateAllPulses()
        {
            for (int index = 0;
                 index < pulsePool.Count;
                 index++)
            {
                DeactivatePulse(
                    pulsePool[index]);
            }

            activePulseCount = 0;
        }

        private void PlayWorldClip(
            AudioClip clip,
            Vector3 position,
            float volume,
            float pitch)
        {
            if (
                audioMuted ||
                clip == null ||
                audioSources.Count == 0
            )
            {
                return;
            }

            AudioSource source =
                audioSources[
                    nextAudioSourceIndex %
                    audioSources.Count];

            nextAudioSourceIndex =
                (
                    nextAudioSourceIndex +
                    1
                ) %
                audioSources.Count;

            source.transform.position =
                position;

            source.pitch =
                Mathf.Clamp(
                    pitch,
                    0.65f,
                    1.45f);

            source.volume =
                Mathf.Clamp01(
                    masterVolume *
                    volume);

            source.maxDistance =
                audioMaximumDistance;

            source.Stop();
            source.clip = clip;
            source.Play();
            audioPlayCount++;
        }

        private Vector3 ResolvePartPosition(
            PrototypeMasterpiecePartKind kind,
            Vector3 fallback)
        {
            if (
                activeInstance != null &&
                activeInstance.TryGetWorldPart(
                    kind,
                    out PrototypeMasterpieceWorldPart part) &&
                part != null &&
                part.ProxyCollider != null
            )
            {
                return part.ProxyCollider.bounds.center;
            }

            return fallback;
        }

        private void UpdateFrameTelemetry()
        {
            float frameMs =
                Time.unscaledDeltaTime *
                1000f;

            if (smoothedFrameMilliseconds <=
                0f)
            {
                smoothedFrameMilliseconds =
                    frameMs;
            }
            else
            {
                smoothedFrameMilliseconds =
                    Mathf.Lerp(
                        smoothedFrameMilliseconds,
                        frameMs,
                        0.055f);
            }
        }

        private void EvaluateQuality()
        {
            if (Time.unscaledTime <
                nextQualityEvaluationAt)
            {
                return;
            }

            nextQualityEvaluationAt =
                Time.unscaledTime +
                Mathf.Max(
                    0.1f,
                    qualityEvaluationInterval);

            ApplyRequestedQuality(
                countAutomaticChange: true);
        }

        private void ApplyRequestedQuality(
            bool countAutomaticChange)
        {
            PrototypeMasterpieceResolvedPolishTier previous =
                resolvedTier;

            switch (requestedQuality)
            {
                case PrototypeMasterpiecePolishQualityMode.Full:
                    resolvedTier =
                        PrototypeMasterpieceResolvedPolishTier.Full;
                    break;

                case PrototypeMasterpiecePolishQualityMode.Reduced:
                    resolvedTier =
                        PrototypeMasterpieceResolvedPolishTier.Reduced;
                    break;

                case PrototypeMasterpiecePolishQualityMode.Minimal:
                    resolvedTier =
                        PrototypeMasterpieceResolvedPolishTier.Minimal;
                    break;

                default:
                    resolvedTier =
                        ResolveAutomaticTier(
                            resolvedTier);
                    break;
            }

            if (resolvedTier ==
                previous)
            {
                return;
            }

            qualityChangeCount++;

            if (
                countAutomaticChange &&
                requestedQuality ==
                    PrototypeMasterpiecePolishQualityMode.Auto
            )
            {
                if ((int)resolvedTier >
                    (int)previous)
                {
                    automaticDowngradeCount++;
                }
                else
                {
                    automaticUpgradeCount++;
                }
            }

            lastAction =
                $"M48.2 efekt tier'i {resolvedTier}.";
        }

        private PrototypeMasterpieceResolvedPolishTier
            ResolveAutomaticTier(
                PrototypeMasterpieceResolvedPolishTier current)
        {
            float frameMs =
                possession != null &&
                possession.Possessed &&
                possession.AverageFrameMilliseconds >
                    0f
                    ? possession.AverageFrameMilliseconds
                    : smoothedFrameMilliseconds;

            switch (current)
            {
                case PrototypeMasterpieceResolvedPolishTier.Full:
                    return frameMs >
                            fullToReducedFrameMs
                        ? PrototypeMasterpieceResolvedPolishTier.Reduced
                        : current;

                case PrototypeMasterpieceResolvedPolishTier.Reduced:
                    if (frameMs >
                        reducedToMinimalFrameMs)
                    {
                        return PrototypeMasterpieceResolvedPolishTier.Minimal;
                    }

                    if (frameMs <
                        reducedToFullFrameMs)
                    {
                        return PrototypeMasterpieceResolvedPolishTier.Full;
                    }

                    return current;

                default:
                    return frameMs <
                            minimalToReducedFrameMs
                        ? PrototypeMasterpieceResolvedPolishTier.Reduced
                        : current;
            }
        }

        private float ResolveStepDistance()
        {
            switch (resolvedTier)
            {
                case PrototypeMasterpieceResolvedPolishTier.Reduced:
                    return reducedStepDistance;

                case PrototypeMasterpieceResolvedPolishTier.Minimal:
                    return minimalStepDistance;

                default:
                    return fullStepDistance;
            }
        }

        private int ResolveActivePulseBudget()
        {
            switch (resolvedTier)
            {
                case PrototypeMasterpieceResolvedPolishTier.Reduced:
                    return Mathf.Min(
                        8,
                        pulsePool.Count);

                case PrototypeMasterpieceResolvedPolishTier.Minimal:
                    return Mathf.Min(
                        4,
                        pulsePool.Count);

                default:
                    return Mathf.Min(
                        12,
                        pulsePool.Count);
            }
        }

        private int ResolvePulseSegmentCount()
        {
            switch (resolvedTier)
            {
                case PrototypeMasterpieceResolvedPolishTier.Reduced:
                    return 24;

                case PrototypeMasterpieceResolvedPolishTier.Minimal:
                    return 16;

                default:
                    return 32;
            }
        }

        private static float ResolveModePitch(
            PrototypeMasterpieceVisualPolicyMode mode)
        {
            switch (mode)
            {
                case PrototypeMasterpieceVisualPolicyMode.SafeSilhouette:
                    return 1.08f;

                case PrototypeMasterpieceVisualPolicyMode.ReadyPuppetFallback:
                    return 0.86f;

                default:
                    return 1f;
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
                    "BAŞ YAPIT • DUYUSAL POLISH";
            }

            if (stateText != null)
            {
                stateText.text =
                    $"KALİTE {requestedQuality}/{resolvedTier}   " +
                    $"SES {(audioMuted ? "KAPALI" : "AÇIK")}   " +
                    $"FRAME {smoothedFrameMilliseconds:F1} ms\n" +
                    $"ADIM {footstepCueCount}   " +
                    $"SALDIRI {attackStartCueCount}/{attackReleaseCueCount}   " +
                    $"PULSE {activePulseCount}/{ResolveActivePulseBudget()} " +
                    $"(TEPE {peakActivePulseCount})\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "L • KALİTE AUTO/FULL/REDUCED/MINIMAL   " +
                    "M • SES AÇ/KAPAT\n" +
                    "POOL TABANLI • GAMEPLAY WRITE YOK • FİNAL ANİMASYON SONRA";
            }
        }

        private void OnDisable()
        {
            DisableInputActions();
            DeactivateAllPulses();
        }

        private void OnDestroy()
        {
            if (cycleQualityAction != null)
            {
                cycleQualityAction.performed -=
                    HandleCycleQuality;

                cycleQualityAction.Dispose();
                cycleQualityAction = null;
            }

            if (toggleAudioAction != null)
            {
                toggleAudioAction.performed -=
                    HandleToggleAudio;

                toggleAudioAction.Dispose();
                toggleAudioAction = null;
            }

            DestroyRuntimeClip(
                possessionClip);

            DestroyRuntimeClip(
                releaseClip);

            DestroyRuntimeClip(
                footstepClip);

            DestroyRuntimeClip(
                attackChargeClip);

            DestroyRuntimeClip(
                attackReleaseClip);

            DestroyRuntimeClip(
                modeChangeClip);

            if (pulseMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(
                        pulseMaterial);
                }
                else
                {
                    DestroyImmediate(
                        pulseMaterial);
                }

                pulseMaterial = null;
            }
        }

        private static void DestroyRuntimeClip(
            AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    clip);
            }
            else
            {
                DestroyImmediate(
                    clip);
            }
        }
    }
}
