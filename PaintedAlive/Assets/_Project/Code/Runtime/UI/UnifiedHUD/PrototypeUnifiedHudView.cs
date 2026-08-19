using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.UI.UnifiedHUD
{
    [DisallowMultipleComponent]
    public sealed class PrototypeUnifiedHudView : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Text roleText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text matchStateText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text encounterText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text progressText;

        [Header("Figure")]
        [SerializeField] private CanvasGroup figurePanel;
        [SerializeField] private Slider claritySlider;
        [SerializeField] private Text clarityValueText;
        [SerializeField] private Text clarityStateText;
        [SerializeField] private Text figureToolText;

        [Header("Painter")]
        [SerializeField] private CanvasGroup painterPanel;
        [SerializeField] private Slider pigmentSlider;
        [SerializeField] private Text pigmentValueText;
        [SerializeField] private Text painterMaterialText;

        [Header("Context")]
        [SerializeField] private CanvasGroup contextPanel;
        [SerializeField] private Text contextText;

        public void Configure(
            CanvasGroup configuredRootGroup,
            Text configuredRoleText,
            Text configuredTimerText,
            Text configuredMatchStateText,
            Text configuredObjectiveText,
            Text configuredEncounterText,
            Slider configuredProgressSlider,
            Text configuredProgressText,
            CanvasGroup configuredFigurePanel,
            Slider configuredClaritySlider,
            Text configuredClarityValueText,
            Text configuredClarityStateText,
            Text configuredFigureToolText,
            CanvasGroup configuredPainterPanel,
            Slider configuredPigmentSlider,
            Text configuredPigmentValueText,
            Text configuredPainterMaterialText,
            CanvasGroup configuredContextPanel,
            Text configuredContextText)
        {
            rootGroup = configuredRootGroup;
            roleText = configuredRoleText;
            timerText = configuredTimerText;
            matchStateText = configuredMatchStateText;
            objectiveText = configuredObjectiveText;
            encounterText = configuredEncounterText;
            progressSlider = configuredProgressSlider;
            progressText = configuredProgressText;

            figurePanel = configuredFigurePanel;
            claritySlider = configuredClaritySlider;
            clarityValueText = configuredClarityValueText;
            clarityStateText = configuredClarityStateText;
            figureToolText = configuredFigureToolText;

            painterPanel = configuredPainterPanel;
            pigmentSlider = configuredPigmentSlider;
            pigmentValueText = configuredPigmentValueText;
            painterMaterialText = configuredPainterMaterialText;

            contextPanel = configuredContextPanel;
            contextText = configuredContextText;

            InitializeSlider(progressSlider);
            InitializeSlider(claritySlider);
            InitializeSlider(pigmentSlider);
        }

        private void Awake()
        {
            InitializeSlider(progressSlider);
            InitializeSlider(claritySlider);
            InitializeSlider(pigmentSlider);
        }

        public void Apply(PrototypeUnifiedHudSnapshot snapshot)
        {
            SetVisible(rootGroup, true);

            SetText(roleText, FormatRole(snapshot));
            SetText(timerText, FormatTime(snapshot.RemainingSeconds));
            SetText(matchStateText, TranslateMatchState(snapshot.MatchState));
            SetText(objectiveText, FormatObjective(snapshot));
            SetText(encounterText, FormatEncounter(snapshot));

            SetSlider(progressSlider, snapshot.NormalizedProgress);
            SetText(
                progressText,
                $"YOLCULUK  %{Mathf.RoundToInt(snapshot.NormalizedProgress * 100f)}" +
                $"   •   SKOR {snapshot.JourneyScore}");

            SetVisible(figurePanel, !snapshot.IsPainter);
            SetSlider(claritySlider, snapshot.NormalizedClarity);
            SetText(
                clarityValueText,
                $"NETLİK  {Mathf.RoundToInt(snapshot.Clarity)} / " +
                $"{Mathf.RoundToInt(snapshot.MaximumClarity)}");
            SetText(
                clarityStateText,
                string.IsNullOrWhiteSpace(snapshot.ClarityLevel)
                    ? "DURUM BİLİNMİYOR"
                    : snapshot.ClarityLevel.ToUpperInvariant());
            SetText(
                figureToolText,
                $"ARAÇ  {Fallback(snapshot.FigureTool, "PALET BIÇAĞI")}");

            SetVisible(painterPanel, snapshot.IsPainter);
            SetSlider(pigmentSlider, snapshot.NormalizedPigment);
            SetText(
                pigmentValueText,
                $"PİGMENT  {Mathf.RoundToInt(snapshot.Pigment)} / " +
                $"{Mathf.RoundToInt(snapshot.MaximumPigment)}");
            SetText(
                painterMaterialText,
                $"MALZEME  {Fallback(snapshot.PainterMaterial, "YAĞLI BOYA")}");

            bool hasContext = !string.IsNullOrWhiteSpace(snapshot.ContextStatus);
            SetVisible(contextPanel, hasContext);
            SetText(contextText, snapshot.ContextStatus);

        }

        private static string FormatRole(PrototypeUnifiedHudSnapshot snapshot)
        {
            if (snapshot.IsPainter)
            {
                return "PAINTER";
            }

            return snapshot.IsFullStain ? "LEKE DESTEĞİ" : "FIGURE";
        }

        private static string FormatObjective(PrototypeUnifiedHudSnapshot snapshot)
        {
            if (!snapshot.IsRunning)
            {
                return snapshot.MatchState switch
                {
                    "Waiting" => "ENTER • YENİ KOŞU",
                    "Countdown" => "HAZIRLAN",
                    "FigureEscaped" => "FIGURE ÇERÇEVEYE ULAŞTI",
                    "TimeExpired" => "SÜRE DOLDU",
                    _ => "OYUN DURUMU HAZIRLANIYOR"
                };
            }

            if (snapshot.IsPainter)
            {
                return "ROTAYI DEĞİŞTİR • KARŞILIK ALANI BIRAK";
            }

            if (snapshot.IsFullStain)
            {
                return "DESTEK OL • RESTORE EDİL • NORMAL ÇIKIŞI TAMAMLA";
            }

            return "ÇERÇEVEYE İLERLE • PAINTER HAMLESİNİ TERSİNE ÇEVİR";
        }

        private static string FormatEncounter(PrototypeUnifiedHudSnapshot snapshot)
        {
            string phase = Fallback(snapshot.EncounterPhase, "ROTAYI OKU");
            return snapshot.EncounterIndex > 0
                ? $"ENCOUNTER {snapshot.EncounterIndex}  •  {phase}"
                : phase;
        }

        private static string TranslateMatchState(string state)
        {
            return state switch
            {
                "Waiting" => "BEKLİYOR",
                "Countdown" => "GERİ SAYIM",
                "Running" => "KOŞU AKTİF",
                "FigureEscaped" => "ÇIKIŞ TAMAMLANDI",
                "TimeExpired" => "SÜRE DOLDU",
                _ => Fallback(state, "HAZIRLANIYOR").ToUpperInvariant()
            };
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainder = totalSeconds % 60;
            return $"{minutes:00}:{remainder:00}";
        }

        private static void InitializeSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.ToUpperInvariant();
        }
    }
}
