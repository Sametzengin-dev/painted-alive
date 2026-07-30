using UnityEngine;

namespace PaintedAlive.Core.Playtests.Validation
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlaytestAcceptanceHUD : MonoBehaviour
    {
        [SerializeField] private PrototypePlaytestAcceptanceGate gate;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle statusStyle;
        private GUIStyle boxStyle;

        public void Configure(PrototypePlaytestAcceptanceGate acceptanceGate)
        {
            gate = acceptanceGate;
        }

        private void Awake()
        {
            gate ??= GetComponent<PrototypePlaytestAcceptanceGate>();
        }

        private void OnGUI()
        {
            if (gate == null)
            {
                return;
            }

            PrototypeAcceptanceSnapshot snapshot = gate.CurrentSnapshot;
            if (!snapshot.CollectingReports &&
                !snapshot.ReviewActive &&
                !snapshot.ReviewCompleted)
            {
                return;
            }

            EnsureStyles();

            float width = Mathf.Min(680f, Screen.width - 40f);
            float height = snapshot.ReviewActive ? 250f : 190f;
            Rect area = new Rect(
                (Screen.width - width) * 0.5f,
                Mathf.Max(20f, Screen.height - height - 28f),
                width,
                height);

            GUI.Box(area, GUIContent.none, boxStyle);
            GUILayout.BeginArea(new Rect(
                area.x + 22f,
                area.y + 18f,
                area.width - 44f,
                area.height - 36f));

            GUILayout.Label("M41 — PROTOTİP KABUL KAPISI", titleStyle);
            GUILayout.Space(8f);

            if (snapshot.CollectingReports)
            {
                GUILayout.Label(
                    "M40 kabul raporu ve mevcut ayrıntılı telemetry birleştiriliyor...",
                    bodyStyle);
            }
            else if (snapshot.ReviewActive)
            {
                GUILayout.Label(
                    $"SORU {snapshot.QuestionIndex + 1}/{snapshot.QuestionCount}",
                    statusStyle);
                GUILayout.Space(5f);
                GUILayout.Label(snapshot.QuestionText, bodyStyle);
                GUILayout.Space(12f);
                GUILayout.Label("Y = EVET     N = HAYIR", statusStyle);
                GUILayout.Label(
                    "Sorular tamamlanmadan Enter ile yeni koşu başlatma.",
                    bodyStyle);
            }
            else if (snapshot.ReviewCompleted)
            {
                GUILayout.Label(
                    snapshot.CurrentRunPassed
                        ? "BU KOŞU KABUL EDİLDİ"
                        : "BU KOŞU KABUL EDİLMEDİ",
                    statusStyle);
                GUILayout.Space(5f);
                GUILayout.Label(
                    $"Tekrarlı doğrulama: {snapshot.AggregatePassingRuns}/" +
                    $"{snapshot.AggregateRequiredRuns} geçerli koşu " +
                    $"({snapshot.AggregateEvaluatedRuns} değerlendirildi)",
                    bodyStyle);
                GUILayout.Label(
                    snapshot.NetworkSpikeCandidateReady
                        ? "M42 ağ teknik spike'ına geçiş adayı hazır."
                        : "Ağ spike'ından önce tekrarlı kabul koşulları henüz tamamlanmadı.",
                    statusStyle);
                GUILayout.Label("Enter = yeni koşu", bodyStyle);
            }

            if (!string.IsNullOrWhiteSpace(snapshot.StatusMessage))
            {
                GUILayout.Space(8f);
                GUILayout.Label(snapshot.StatusMessage, bodyStyle);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };
        }
    }
}
