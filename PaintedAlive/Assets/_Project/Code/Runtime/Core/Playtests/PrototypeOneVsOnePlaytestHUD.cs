using PaintedAlive.Core.Prototypes;
using UnityEngine;

namespace PaintedAlive.Core.Playtests
{
    [DisallowMultipleComponent]
    public sealed class PrototypeOneVsOnePlaytestHUD : MonoBehaviour
    {
        [SerializeField] private PrototypeOneVsOnePlaytestSession session;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle passedStyle;
        private GUIStyle failedStyle;
        private GUIStyle pendingStyle;
        private GUIStyle resultStyle;

        public void Configure(PrototypeOneVsOnePlaytestSession playtestSession)
        {
            session = playtestSession;
        }

        private void Awake()
        {
            session ??= GetComponent<PrototypeOneVsOnePlaytestSession>();
        }

        private void OnGUI()
        {
            if (session == null ||
                session.Config == null ||
                !session.Config.ShowProtocolHUD)
            {
                return;
            }

            PrototypeMatchState state = session.MatchController != null
                ? session.MatchController.State
                : PrototypeMatchState.Waiting;

            if (state == PrototypeMatchState.Waiting)
            {
                return;
            }

            EnsureStyles();
            DrawProtocolPanel(state);

            if (state == PrototypeMatchState.FigureEscaped ||
                state == PrototypeMatchState.TimeExpired)
            {
                DrawResultPanel();
            }
        }

        private void DrawProtocolPanel(PrototypeMatchState state)
        {
            float width = Mathf.Min(430f, Screen.width * 0.34f);
            const float height = 250f;
            Rect rect = new Rect(
                Screen.width - width - 14f,
                54f,
                width,
                height);

            GUI.Box(rect, GUIContent.none);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 10f, width - 28f, 28f),
                "M40 — 5 DAKİKALIK 1v1 TEST",
                titleStyle);

            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 42f, width - 28f, 44f),
                state == PrototypeMatchState.Running
                    ? session.GetCurrentPrompt()
                    : "Geri sayım bitince üç farklı doğal karşı sonucu aynı koşuda doğrula.",
                bodyStyle);

            float y = rect.y + 92f;
            for (int i = 0; i < session.Outcomes.Count; i++)
            {
                PrototypeOneVsOneOutcomeRecord outcome = session.Outcomes[i];
                string marker = outcome.status switch
                {
                    PrototypeOneVsOneOutcomeStatus.Passed => "✓",
                    PrototypeOneVsOneOutcomeStatus.Failed => "✕",
                    _ => "○"
                };

                GUIStyle style = outcome.status switch
                {
                    PrototypeOneVsOneOutcomeStatus.Passed => passedStyle,
                    PrototypeOneVsOneOutcomeStatus.Failed => failedStyle,
                    _ => pendingStyle
                };

                GUI.Label(
                    new Rect(rect.x + 18f, y, width - 36f, 23f),
                    $"{marker} {PrototypeOneVsOnePlaytestSession.GetOutcomeTitle(outcome.outcome)}",
                    style);
                y += 25f;
            }

            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 178f, width - 28f, 28f),
                $"KANIT: {session.PassedOutcomeCount}/{session.Config.RequiredDistinctOutcomes}  •  " +
                $"F11 BAŞARILI  •  F12 BAŞARISIZ",
                bodyStyle);

            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 210f, width - 28f, 30f),
                session.StatusMessage,
                bodyStyle);
        }

        private void DrawResultPanel()
        {
            const float width = 620f;
            const float height = 150f;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f + 145f,
                width,
                height);

            GUI.Box(rect, GUIContent.none);
            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 14f, width - 32f, 42f),
                session.Accepted
                    ? "M40 KABUL — ÇEKİRDEK 1v1 TEZİ DOĞRULANDI"
                    : "M40 TEKRAR TEST — DOĞAL SONUÇ ÇEŞİTLİLİĞİ EKSİK",
                resultStyle);

            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 62f, width - 44f, 72f),
                $"Doğrulanan sonuç: {session.PassedOutcomeCount}/{session.Config.RequiredDistinctOutcomes}\n" +
                "Enter ile yeni koşuyu başlat. Ayrıntılı rapor persistentDataPath/PlaytestTelemetry/M40_OneVsOne klasörüne yazılır.",
                bodyStyle);
        }

        private void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            bodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };

            passedStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            passedStyle.normal.textColor = new Color(0.42f, 0.94f, 0.65f);

            failedStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            failedStyle.normal.textColor = new Color(1f, 0.48f, 0.42f);

            pendingStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14
            };

            resultStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }
    }
}
