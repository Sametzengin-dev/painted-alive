using UnityEngine;
using PaintedAlive.Figures;

namespace PaintedAlive.Figures.StainSupport.FrameExit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class FrameExitFeedbackHUD : MonoBehaviour
    {
        [SerializeField] private StainFrameExitConfig config;
        [SerializeField] private FigureClarityState clarityState;

        [Header("Runtime - Read Only")]
        [SerializeField] private FigureFrameExitOutcome currentOutcome;
        [SerializeField] private float visibleUntil;

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private string title = string.Empty;
        private string body = string.Empty;
        private Color accentColor = Color.cyan;

        public FigureFrameExitOutcome CurrentOutcome => currentOutcome;

        public void Configure(
            StainFrameExitConfig exitConfig,
            FigureClarityState figureClarity)
        {
            config = exitConfig;
            clarityState = figureClarity;
        }

        private void Awake()
        {
            clarityState ??= GetComponent<FigureClarityState>();
        }

        private void OnEnable()
        {
            FigureFrameExitRuleService.ExitEvaluated += HandleExitEvaluated;
        }

        private void OnDisable()
        {
            FigureFrameExitRuleService.ExitEvaluated -= HandleExitEvaluated;
        }

        private void HandleExitEvaluated(FigureFrameExitDecision decision)
        {
            if (clarityState == null || decision.Figure != clarityState)
            {
                return;
            }

            currentOutcome = decision.Outcome;
            visibleUntil = Time.unscaledTime +
                (config != null ? config.FeedbackDuration : 2.4f);

            if (decision.CountsAsNormalExit)
            {
                title = "ÇERÇEVE ÇIKIŞI";
                body = $"Normal Figür çıkışı kabul edildi  •  +{decision.AwardedScore} prototip puanı";
                accentColor = config != null
                    ? config.NormalExitColor
                    : Color.green;
            }
            else
            {
                title = "LEKE ÇIKIŞA ULAŞTI";
                body = "Destek varışı kaydedildi  •  Normal Figür puanı yok  •  Restore gerekli";
                accentColor = config != null
                    ? config.StainArrivalColor
                    : Color.cyan;
            }
        }

        private void OnGUI()
        {
            if (Time.unscaledTime >= visibleUntil || currentOutcome == FigureFrameExitOutcome.None)
            {
                return;
            }

            EnsureStyles();

            float width = Mathf.Min(620f, Screen.width - 40f);
            float x = (Screen.width - width) * 0.5f;
            Rect boxRect = new Rect(x, 32f, width, 92f);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.035f, 0.05f, 0.065f, 0.94f);
            GUI.Box(boxRect, GUIContent.none, boxStyle);

            GUI.color = accentColor;
            GUI.Label(
                new Rect(boxRect.x + 18f, boxRect.y + 13f, boxRect.width - 36f, 30f),
                title,
                titleStyle);

            GUI.color = Color.white;
            GUI.Label(
                new Rect(boxRect.x + 18f, boxRect.y + 48f, boxRect.width - 36f, 30f),
                body,
                bodyStyle);

            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (boxStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(8, 8, 8, 8)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }
    }
}
