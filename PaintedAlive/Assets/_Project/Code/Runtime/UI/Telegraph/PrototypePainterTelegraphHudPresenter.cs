using System;
using PaintedAlive.UI.UnifiedHUD;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI.Telegraph
{
    [DisallowMultipleComponent]
    public sealed class PrototypePainterTelegraphHudPresenter :
        MonoBehaviour
    {
        [Header("Role Source")]
        [SerializeField]
        private PrototypeUnifiedHudController unifiedHudController;

        [SerializeField]
        private bool showWhilePainterForPrototype;

        [Header("UI")]
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Image background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text phaseText;
        [SerializeField] private Text counterText;
        [SerializeField] private Text timerText;
        [SerializeField] private Slider progressSlider;

        [Header("Timing")]
        [SerializeField, Min(0.1f)]
        private float resolutionHoldDuration = 1.35f;

        [Header("Colors")]
        [SerializeField] private Color draftingColor =
            new Color(1f, 0.55f, 0.08f, 0.94f);

        [SerializeField] private Color armedColor =
            new Color(1f, 0.18f, 0.06f, 0.98f);

        [SerializeField] private Color blockedColor =
            new Color(0.48f, 0.48f, 0.50f, 0.94f);

        [SerializeField] private Color committedColor =
            new Color(0.18f, 0.72f, 0.46f, 0.96f);

        [Header("Runtime Read Only")]
        [SerializeField] private bool configured;
        [SerializeField] private bool telegraphVisible;
        [SerializeField] private string activeShape = string.Empty;
        [SerializeField] private string feedbackStatus = "Idle";
        [SerializeField] private int telegraphEventCount;
        [SerializeField] private int resolutionFeedbackCount;

        private PrototypePainterTelegraphSignal activeSignal;
        private PrototypePainterTelegraphOutcome latestOutcome;
        private bool hasActiveSignal;
        private bool hasOutcome;
        private float hideOutcomeAt;

        public bool IsConfigured => configured;
        public bool TelegraphVisible => telegraphVisible;
        public string ActiveShape => activeShape;
        public string FeedbackStatus => feedbackStatus;
        public int TelegraphEventCount => telegraphEventCount;
        public int ResolutionFeedbackCount => resolutionFeedbackCount;

        public void Configure(
            PrototypeUnifiedHudController configuredHudController,
            CanvasGroup configuredPanel,
            Image configuredBackground,
            Text configuredTitleText,
            Text configuredPhaseText,
            Text configuredCounterText,
            Text configuredTimerText,
            Slider configuredProgressSlider)
        {
            unifiedHudController = configuredHudController;
            panel = configuredPanel;
            background = configuredBackground;
            titleText = configuredTitleText;
            phaseText = configuredPhaseText;
            counterText = configuredCounterText;
            timerText = configuredTimerText;
            progressSlider = configuredProgressSlider;

            configured =
                unifiedHudController != null &&
                panel != null &&
                background != null &&
                titleText != null &&
                phaseText != null &&
                counterText != null &&
                timerText != null &&
                progressSlider != null;

            InitializeSlider();
            SetVisible(false);
        }

        private void Awake()
        {
            InitializeSlider();
            configured =
                unifiedHudController != null &&
                panel != null &&
                background != null &&
                titleText != null &&
                phaseText != null &&
                counterText != null &&
                timerText != null &&
                progressSlider != null;

            SetVisible(false);
        }

        private void OnEnable()
        {
            PrototypePainterTelegraphHub.TelegraphUpdated +=
                HandleTelegraphUpdated;

            PrototypePainterTelegraphHub.TelegraphResolved +=
                HandleTelegraphResolved;
        }

        private void OnDisable()
        {
            PrototypePainterTelegraphHub.TelegraphUpdated -=
                HandleTelegraphUpdated;

            PrototypePainterTelegraphHub.TelegraphResolved -=
                HandleTelegraphResolved;

            SetVisible(false);
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            bool roleCanSee = RoleCanSeeTelegraph();

            if (hasActiveSignal)
            {
                RenderActiveSignal(activeSignal);
                SetVisible(roleCanSee);
                return;
            }

            if (hasOutcome &&
                Time.unscaledTime < hideOutcomeAt)
            {
                RenderOutcome(latestOutcome);
                SetVisible(roleCanSee);
                return;
            }

            hasOutcome = false;
            feedbackStatus = "Idle";
            SetVisible(false);
        }

        private void HandleTelegraphUpdated(
            PrototypePainterTelegraphSignal signal)
        {
            activeSignal = signal;
            hasActiveSignal = true;
            hasOutcome = false;
            activeShape = signal.ShapeName;
            telegraphEventCount++;
        }

        private void HandleTelegraphResolved(
            PrototypePainterTelegraphOutcome outcome)
        {
            latestOutcome = outcome;
            hasActiveSignal = false;
            hasOutcome = true;
            hideOutcomeAt =
                Time.unscaledTime +
                resolutionHoldDuration;

            activeShape = outcome.ShapeName;
            resolutionFeedbackCount++;
        }

        private void RenderActiveSignal(
            PrototypePainterTelegraphSignal signal)
        {
            SetText(
                titleText,
                $"RESSAM {TranslateShape(signal.ShapeName)} TASLAĞI");

            SetSlider(signal.NormalizedProgress);

            switch (signal.Phase)
            {
                case PrototypePainterTelegraphPhase.Armed:
                    background.color = armedColor;
                    SetText(
                        phaseText,
                        "HAMLE HAZIR • BIRAKILINCA İNECEK");
                    feedbackStatus = "Armed";
                    break;

                case PrototypePainterTelegraphPhase.Blocked:
                    background.color = blockedColor;
                    SetText(
                        phaseText,
                        "HAMLE BLOKE • PİGMENT / BÜTÇE YETERSİZ");
                    feedbackStatus = "Blocked";
                    break;

                default:
                    background.color = draftingColor;
                    SetText(
                        phaseText,
                        "TASLAK HAZIRLANIYOR");
                    feedbackStatus = "Drafting";
                    break;
            }

            SetText(
                counterText,
                ResolveCounterHint(signal.ShapeName));

            SetText(
                timerText,
                signal.Phase ==
                PrototypePainterTelegraphPhase.Armed
                    ? "ŞİMDİ"
                    : signal.Phase ==
                      PrototypePainterTelegraphPhase.Blocked
                        ? "İPTAL BEKLENİYOR"
                        : signal.RemainingSeconds > 0.01f
                            ? $"{signal.RemainingSeconds:0.0}s"
                            : $"{Mathf.RoundToInt(signal.NormalizedProgress * 100f)}%");
        }

        private void RenderOutcome(
            PrototypePainterTelegraphOutcome outcome)
        {
            SetSlider(1f);

            if (outcome.Resolution ==
                PrototypePainterTelegraphResolution.Committed)
            {
                background.color = committedColor;
                SetText(
                    titleText,
                    $"{TranslateShape(outcome.ShapeName)} İNDİ");
                SetText(
                    phaseText,
                    "KARŞILIK PENCERESİ AÇIK");
                SetText(
                    counterText,
                    ResolveCounterHint(outcome.ShapeName));
                SetText(timerText, "ETKİ AKTİF");
                feedbackStatus = "Committed";
            }
            else
            {
                background.color = blockedColor;
                SetText(
                    titleText,
                    "RESSAM HAMLESİ İPTAL");
                SetText(
                    phaseText,
                    TranslateCancelReason(outcome.Reason));
                SetText(
                    counterText,
                    "ROTAYI KORU • BOŞA HARCANAN HAMLEYİ DEĞERLENDİR");
                SetText(timerText, "İPTAL");
                feedbackStatus = "Cancelled";
            }
        }

        private bool RoleCanSeeTelegraph()
        {
            if (showWhilePainterForPrototype ||
                unifiedHudController == null)
            {
                return true;
            }

            try
            {
                return !unifiedHudController
                    .BuildSnapshot()
                    .IsPainter;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private void InitializeSlider()
        {
            if (progressSlider == null)
            {
                return;
            }

            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.interactable = false;
            progressSlider.SetValueWithoutNotify(0f);
        }

        private void SetSlider(float value)
        {
            if (progressSlider != null)
            {
                progressSlider.SetValueWithoutNotify(
                    Mathf.Clamp01(value));
            }
        }

        private void SetVisible(bool visible)
        {
            telegraphVisible = visible;

            if (panel == null)
            {
                return;
            }

            panel.alpha = visible ? 1f : 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }

        private static void SetText(
            Text target,
            string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static string TranslateShape(
            string shapeName)
        {
            if (Contains(shapeName, "Ramp"))
            {
                return "RAMPA";
            }

            if (Contains(shapeName, "Wall"))
            {
                return "DUVAR";
            }

            return "BOYA";
        }

        private static string ResolveCounterHint(
            string shapeName)
        {
            if (Contains(shapeName, "Ramp"))
            {
                return
                    "KARŞILIK • YÜKSEKLİĞİ OKU • YANINDAN GEÇ • ANKRAJLA";
            }

            if (Contains(shapeName, "Wall"))
            {
                return
                    "KARŞILIK • YANDAN KAÇ • PALET BIÇAĞIYLA KES • ÇERÇEVEYLE ANKRAJLA";
            }

            return
                "KARŞILIK • ETKİ ALANINDAN ÇIK • MALZEMEYİ OKU • TAKIMLA TERSİNE ÇEVİR";
        }

        private static string TranslateCancelReason(
            string reason)
        {
            if (Contains(reason, "Pigment") ||
                Contains(reason, "Budget"))
            {
                return
                    "PİGMENT VEYA STROKE BÜTÇESİ YETMEDİ";
            }

            if (Contains(reason, "BeforeArmed"))
            {
                return
                    "TELEGRAPH TAMAMLANMADAN BIRAKILDI";
            }

            return
                "HAMLE UYGULANMADI";
        }

        private static bool Contains(
            string value,
            string fragment)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(
                    fragment,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
