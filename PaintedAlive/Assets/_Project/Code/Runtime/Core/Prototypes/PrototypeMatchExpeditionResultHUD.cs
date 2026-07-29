using UnityEngine;

namespace PaintedAlive.Core.Prototypes
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMatchExpeditionResultHUD : MonoBehaviour
    {
        [SerializeField] private PrototypeMatchExpeditionBridge bridge;
        [SerializeField] private bool showResultPanel = true;
        [SerializeField, Min(420f)] private float panelWidth = 560f;
        [SerializeField, Min(260f)] private float panelHeight = 330f;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle hintStyle;

        public void Configure(PrototypeMatchExpeditionBridge expeditionBridge)
        {
            bridge = expeditionBridge;
        }

        private void Awake()
        {
            if (bridge == null)
            {
                bridge = GetComponent<PrototypeMatchExpeditionBridge>();
            }
        }

        private void OnGUI()
        {
            if (!showResultPanel || bridge == null)
            {
                return;
            }

            PrototypeExpeditionResultSnapshot snapshot = bridge.CurrentSnapshot;
            if (!snapshot.IsCompleted)
            {
                return;
            }

            EnsureStyles();

            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Box(panel, GUIContent.none);

            GUILayout.BeginArea(new Rect(
                panel.x + 28f,
                panel.y + 24f,
                panel.width - 56f,
                panel.height - 48f));

            string title = snapshot.Reason ==
                PrototypeExpeditionResultReason.NormalFigureExit
                ? "EXPEDITION TAMAMLANDI"
                : "SÜRE DOLDU";

            GUILayout.Label(title, titleStyle);
            GUILayout.Space(16f);

            GUILayout.Label(
                $"Mesafe skoru: {snapshot.Score.DistanceScore}\n" +
                $"Normal çıkış bonusu: +{snapshot.Score.ExitBonus}\n" +
                $"Toplam skor: {snapshot.Score.TotalScore}\n" +
                $"Çıkan Figür: {(snapshot.Score.NormalExitCompleted ? "1/1" : "0/1")}\n" +
                $"Tamamlanma süresi: {FormatTime(snapshot.ElapsedTime)}\n" +
                $"Kalan süre: {FormatTime(snapshot.RemainingTime)}\n" +
                $"Son Netlik: {snapshot.FinalClarity}\n" +
                $"Leke destek varışı: {(snapshot.StainArrivalDuringRun ? "Evet" : "Hayır")}",
                bodyStyle);

            GUILayout.FlexibleSpace();
            GUILayout.Label(
                "Yeni koşu için ENTER — reset ve süreyi mevcut PrototypeMatchController yönetir.",
                hintStyle);

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };

            bodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                richText = true
            };

            hintStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                wordWrap = true
            };
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
