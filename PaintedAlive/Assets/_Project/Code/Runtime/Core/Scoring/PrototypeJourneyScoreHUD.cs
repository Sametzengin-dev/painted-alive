using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEngine;

namespace PaintedAlive.Core.Scoring
{
    [DisallowMultipleComponent]
    public sealed class PrototypeJourneyScoreHUD : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PrototypeJourneyScoreTracker tracker;
        [SerializeField] private PrototypeJourneyScoreConfig config;
        [SerializeField] private FigureClarityState clarity;

        private GUIStyle titleStyle;
        private GUIStyle valueStyle;
        private GUIStyle statusStyle;
        private GUIStyle boxStyle;
        private MonoBehaviour cachedPainterBrush;

        public void Configure(
            PrototypeJourneyScoreTracker scoreTracker,
            PrototypeJourneyScoreConfig scoreConfig,
            FigureClarityState figureClarity)
        {
            tracker = scoreTracker;
            config = scoreConfig;
            clarity = figureClarity;
        }

        private void Awake()
        {
            if (tracker == null)
            {
                tracker = GetComponent<PrototypeJourneyScoreTracker>();
            }

            if (clarity == null)
            {
                clarity = GetComponent<FigureClarityState>();
            }

            CachePainterBrush();
        }

        private void OnGUI()
        {
            if (tracker == null ||
                (config != null && !config.ShowPrototypeHud) ||
                IsPainterRoleActive())
            {
                return;
            }

            EnsureStyles();

            Vector2 position = config != null
                ? config.HudPosition
                : new Vector2(18f, 18f);

            float width = config != null ? config.HudWidth : 360f;
            Rect panel = new Rect(position.x, position.y, width, 128f);
            GUI.Box(panel, GUIContent.none, boxStyle);

            int maximumDistance = config != null
                ? config.MaximumDistanceScore
                : 1000;

            GUI.Label(
                new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 24f),
                "YOLCULUK SKORU — PROTOTİP",
                titleStyle);

            GUI.Label(
                new Rect(panel.x + 14f, panel.y + 40f, panel.width - 28f, 22f),
                $"Mesafe  {tracker.DistanceScore} / {maximumDistance}",
                valueStyle);

            GUI.Label(
                new Rect(panel.x + 14f, panel.y + 64f, panel.width - 28f, 22f),
                $"Çıkış   +{tracker.ExitBonus}     Toplam  {tracker.TotalScore}",
                valueStyle);

            GUI.Label(
                new Rect(panel.x + 14f, panel.y + 91f, panel.width - 28f, 24f),
                BuildStatusText(),
                statusStyle);
        }

        private string BuildStatusText()
        {
            if (tracker.NormalExitCompleted)
            {
                return "NORMAL FİGÜR ÇIKIŞI TAMAMLANDI";
            }

            if (tracker.LastExitOutcome == FigureFrameExitOutcome.StainSupportArrival)
            {
                return "LEKE VARIŞI — NORMAL ÇIKIŞ BONUSU İÇİN RESTORE GEREKİYOR";
            }

            if (clarity != null && clarity.CurrentLevel == FigureClarityLevel.Stain)
            {
                return "LEKE FORMU — MESAFE KORUNUR, NORMAL ÇIKIŞ BONUSU VERİLMEZ";
            }

            return "ÇERÇEVEYE İLERLE";
        }

        private bool IsPainterRoleActive()
        {
            if (cachedPainterBrush == null)
            {
                CachePainterBrush();
            }

            return cachedPainterBrush != null && cachedPainterBrush.enabled;
        }

        private void CachePainterBrush()
        {
            MonoBehaviour[] behaviours =
                FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour candidate = behaviours[i];
                if (candidate != null &&
                    candidate.GetType().Name == "PainterBrushController")
                {
                    cachedPainterBrush = candidate;
                    return;
                }
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
        }
    }
}
