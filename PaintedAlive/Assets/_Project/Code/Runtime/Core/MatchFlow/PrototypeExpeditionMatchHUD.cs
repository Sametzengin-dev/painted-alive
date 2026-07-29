using UnityEngine;

namespace PaintedAlive.Core.MatchFlow
{
    [DisallowMultipleComponent]
    public sealed class PrototypeExpeditionMatchHUD : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PrototypeExpeditionMatchFlow matchFlow;
        [SerializeField] private PrototypeExpeditionMatchConfig config;

        private GUIStyle overlayStyle;
        private GUIStyle countdownStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle timerStyle;
        private GUIStyle resultBoxStyle;

        public void Configure(
            PrototypeExpeditionMatchFlow flow,
            PrototypeExpeditionMatchConfig matchConfig)
        {
            matchFlow = flow;
            config = matchConfig;
        }

        private void Awake()
        {
            if (matchFlow == null)
            {
                matchFlow = GetComponent<PrototypeExpeditionMatchFlow>();
            }
        }

        private void OnGUI()
        {
            if (matchFlow == null ||
                (config != null && !config.ShowPrototypeHud))
            {
                return;
            }

            EnsureStyles();

            switch (matchFlow.Phase)
            {
                case PrototypeExpeditionMatchPhase.Preparation:
                    DrawPreparation();
                    break;

                case PrototypeExpeditionMatchPhase.Active:
                    DrawActiveTimer();
                    break;

                case PrototypeExpeditionMatchPhase.Completed:
                    DrawCompletedResult();
                    break;
            }
        }

        private void DrawPreparation()
        {
            Color accent = config != null
                ? config.PreparationColor
                : Color.cyan;

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none,
                overlayStyle);
            GUI.color = previousColor;

            int countdown = Mathf.Max(
                1,
                Mathf.CeilToInt(matchFlow.PhaseTimeRemaining));

            countdownStyle.normal.textColor = accent;
            GUI.Label(
                new Rect(0f, Screen.height * 0.29f, Screen.width, 86f),
                countdown.ToString(),
                countdownStyle);

            headingStyle.normal.textColor = Color.white;
            GUI.Label(
                new Rect(0f, Screen.height * 0.47f, Screen.width, 38f),
                "EXPEDITION HAZIRLANIYOR",
                headingStyle);

            bodyStyle.normal.textColor = new Color(0.86f, 0.90f, 0.92f, 1f);
            GUI.Label(
                new Rect(0f, Screen.height * 0.54f, Screen.width, 64f),
                "Hareket, rol değişimi ve bütün aktif araç girişleri başlangıca kadar kilitli.",
                bodyStyle);
        }

        private void DrawActiveTimer()
        {
            Color accent = config != null
                ? config.ActiveColor
                : Color.yellow;

            float width = 210f;
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                16f,
                width,
                64f);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.88f);
            GUI.Box(panel, GUIContent.none, resultBoxStyle);
            GUI.color = previousColor;

            timerStyle.normal.textColor = accent;
            GUI.Label(
                new Rect(panel.x, panel.y + 5f, panel.width, 24f),
                "EXPEDITION",
                headingStyle);
            GUI.Label(
                new Rect(panel.x, panel.y + 27f, panel.width, 30f),
                FormatTime(matchFlow.RemainingActiveTime),
                timerStyle);
        }

        private void DrawCompletedResult()
        {
            PrototypeExpeditionMatchSnapshot snapshot =
                matchFlow.CurrentSnapshot;

            Color accent = matchFlow.CompletionReason ==
                PrototypeExpeditionCompletionReason.TimeExpired
                    ? (config != null ? config.TimeoutColor : Color.red)
                    : (config != null ? config.CompletedColor : Color.green);

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.76f);
            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none,
                overlayStyle);
            GUI.color = previousColor;

            float width = config != null ? config.ResultPanelWidth : 560f;
            float height = 388f;
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUI.color = new Color(0.055f, 0.065f, 0.075f, 0.97f);
            GUI.Box(panel, GUIContent.none, resultBoxStyle);
            GUI.color = previousColor;

            headingStyle.normal.textColor = accent;
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 40f),
                BuildResultHeading(),
                headingStyle);

            bodyStyle.normal.textColor = Color.white;
            float x = panel.x + 34f;
            float y = panel.y + 78f;
            float line = 34f;
            float labelWidth = panel.width - 68f;

            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Mesafe skoru                 {snapshot.DistanceScore}", bodyStyle);
            y += line;
            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Normal çıkış bonusu          +{snapshot.ExitBonus}", bodyStyle);
            y += line;
            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Toplam skor                   {snapshot.TotalScore}", bodyStyle);
            y += line;
            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Çıkan Figür oranı             {(snapshot.NormalExitCompleted ? "1/1" : "0/1")}", bodyStyle);
            y += line;
            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Tamamlanma süresi             {FormatTime(snapshot.ElapsedActiveTime)}", bodyStyle);
            y += line;
            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Kalan süre                    {FormatTime(snapshot.RemainingActiveTime)}", bodyStyle);
            y += line;
            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Son Netlik                    {snapshot.FinalClarity}", bodyStyle);
            y += line;
            GUI.Label(new Rect(x, y, labelWidth, 28f),
                $"Leke destek varışı            {(snapshot.StainArrivalRecorded ? "Evet" : "Hayır")}", bodyStyle);

            bodyStyle.normal.textColor = new Color(0.72f, 0.76f, 0.80f, 1f);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + height - 50f, panel.width - 48f, 30f),
                "Yeni koşu için Play Mode'u kapatıp yeniden başlat.",
                bodyStyle);
        }

        private string BuildResultHeading()
        {
            return matchFlow.CompletionReason switch
            {
                PrototypeExpeditionCompletionReason.NormalFigureExit =>
                    "ÇERÇEVE KAÇIŞI TAMAMLANDI",
                PrototypeExpeditionCompletionReason.TimeExpired =>
                    "SÜRE DOLDU — YOLCULUK SONLANDI",
                _ => "EXPEDITION TAMAMLANDI"
            };
        }

        private static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int wholeSeconds = Mathf.CeilToInt(seconds);
            int minutes = wholeSeconds / 60;
            int remainder = wholeSeconds % 60;
            return $"{minutes:00}:{remainder:00}";
        }

        private void EnsureStyles()
        {
            if (overlayStyle != null)
            {
                return;
            }

            overlayStyle = new GUIStyle(GUI.skin.box);
            resultBoxStyle = new GUIStyle(GUI.skin.box);

            countdownStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 72,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
