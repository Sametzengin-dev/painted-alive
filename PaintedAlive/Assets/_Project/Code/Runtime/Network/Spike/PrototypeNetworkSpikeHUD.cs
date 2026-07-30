using UnityEngine;

namespace PaintedAlive.Network.Spike
{
    [DisallowMultipleComponent]
    public sealed class PrototypeNetworkSpikeHUD : MonoBehaviour
    {
        [SerializeField] private PrototypeNetworkSpikeHarness harness;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle resultStyle;

        public void Configure(PrototypeNetworkSpikeHarness spikeHarness)
        {
            harness = spikeHarness;
        }

        private void OnGUI()
        {
            if (harness == null)
            {
                return;
            }

            EnsureStyles();
            PrototypeNetworkSpikeSnapshot snapshot = harness.GetSnapshot();

            const float width = 430f;
            const float height = 170f;
            Rect panel = new Rect(Screen.width - width - 20f, 20f, width, height);
            GUI.Box(panel, GUIContent.none);

            GUILayout.BeginArea(new Rect(
                panel.x + 14f,
                panel.y + 10f,
                panel.width - 28f,
                panel.height - 20f));

            GUILayout.Label("M42 — AĞ TEKNİK SPIKE TEMELİ", titleStyle);
            GUILayout.Label(
                snapshot.M41Ready
                    ? "M41: ağ spike adayı hazır"
                    : "M41: tekrarlı kabul kapısı henüz hazır değil",
                bodyStyle);
            GUILayout.Space(4f);
            GUILayout.Label(
                "Aktif maç dışında Ctrl + Shift + N: 0 / 100 / 150 ms profillerini çalıştır",
                bodyStyle);
            GUILayout.Label(
                $"Durum: {snapshot.Status}",
                resultStyle);

            if (snapshot.HasReport)
            {
                GUILayout.Label(
                    $"Temel profiller: {snapshot.PassedProfiles}/{snapshot.TotalProfiles}",
                    bodyStyle);
                GUILayout.Label(
                    snapshot.FoundationPassed
                        ? "Sonuç: ortak komut ve ölçüm temeli geçti"
                        : "Sonuç: temel eşiklerden biri geçmedi",
                    resultStyle);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            bodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            resultStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }
    }
}
