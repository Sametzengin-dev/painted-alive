using PaintedAlive.Core.Prototypes;
using UnityEngine;

namespace PaintedAlive.Core.Encounters
{
    [DisallowMultipleComponent]
    public sealed class PrototypeEncounterRhythmHUD : MonoBehaviour
    {
        [SerializeField] private PrototypeEncounterRhythmDirector director;
        [SerializeField] private bool showCompactStrip = true;
        [SerializeField] private bool showTransitionBanner = true;

        private GUIStyle bannerTitleStyle;
        private GUIStyle bannerBodyStyle;
        private GUIStyle compactStyle;
        private GUIStyle pressureStyle;

        public void Configure(PrototypeEncounterRhythmDirector rhythmDirector)
        {
            director = rhythmDirector;
        }

        private void Awake()
        {
            if (director == null)
            {
                director = GetComponent<PrototypeEncounterRhythmDirector>();
            }
        }

        private void OnGUI()
        {
            if (director == null ||
                director.MatchController == null ||
                director.MatchController.State != PrototypeMatchState.Running)
            {
                return;
            }

            PrototypeEncounterRhythmSnapshot snapshot = director.CurrentSnapshot;
            if (snapshot.Phase == PrototypeEncounterPhase.Inactive ||
                snapshot.Phase == PrototypeEncounterPhase.Completed)
            {
                return;
            }

            EnsureStyles();

            if (showCompactStrip)
            {
                DrawCompactStrip(snapshot);
            }

            float bannerDuration = director.Config != null
                ? director.Config.TransitionBannerDuration
                : 2.4f;

            bool bannerActive =
                showTransitionBanner &&
                Time.unscaledTime - snapshot.TransitionTimeUnscaled <= bannerDuration;

            if (bannerActive)
            {
                DrawTransitionBanner(snapshot);
            }
        }

        private void DrawCompactStrip(PrototypeEncounterRhythmSnapshot snapshot)
        {
            const float width = 480f;
            const float height = 34f;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                12f,
                width,
                height);

            GUI.Box(rect, GUIContent.none);

            string node = snapshot.Phase == PrototypeEncounterPhase.FinalEscape
                ? "FİNAL"
                : $"DÜĞÜM {Mathf.Max(1, snapshot.EncounterIndex)}/3";

            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 6f, 310f, 22f),
                $"{node}  •  {GetTitle(snapshot.Phase)}",
                compactStyle);

            string pressure = snapshot.Phase == PrototypeEncounterPhase.Breath
                ? "BASKI: NEFES"
                : $"BASKI: %{snapshot.Pressure01 * 100f:0}";

            GUI.Label(
                new Rect(rect.x + 315f, rect.y + 6f, 155f, 22f),
                pressure,
                pressureStyle);
        }

        private void DrawTransitionBanner(PrototypeEncounterRhythmSnapshot snapshot)
        {
            const float width = 620f;
            const float height = 112f;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                58f,
                width,
                height);

            GUI.Box(rect, GUIContent.none);

            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 12f, width - 32f, 34f),
                GetTitle(snapshot.Phase),
                bannerTitleStyle);

            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 51f, width - 48f, 48f),
                GetDescription(snapshot.Phase),
                bannerBodyStyle);
        }

        private void EnsureStyles()
        {
            bannerTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };

            bannerBodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 15,
                wordWrap = true
            };

            compactStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            pressureStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 13
            };
        }

        private static string GetTitle(PrototypeEncounterPhase phase)
        {
            return phase switch
            {
                PrototypeEncounterPhase.Read => "ROTAYI OKU",
                PrototypeEncounterPhase.LightPressure => "İLK MÜDAHALE",
                PrototypeEncounterPhase.ToolResponse => "KARŞI ARACINI SEÇ",
                PrototypeEncounterPhase.CombinationPressure => "KOMBİNASYON BASKISI",
                PrototypeEncounterPhase.RescueAndEscape => "KURTAR VE KAÇ",
                PrototypeEncounterPhase.Breath => "NEFES PENCERESİ",
                PrototypeEncounterPhase.FinalEscape => "ÇERÇEVEYE KAÇ",
                _ => string.Empty
            };
        }

        private static string GetDescription(PrototypeEncounterPhase phase)
        {
            return phase switch
            {
                PrototypeEncounterPhase.Read =>
                    "Figür rotayı ve telegraph alanlarını inceler. Ressam darboğazı okur; ağır hamleyi henüz üst üste bindirmez.",
                PrototypeEncounterPhase.LightPressure =>
                    "Ressam düşük maliyetli ilk müdahaleyi yapar. Figür, rakibin hangi malzeme planını kurduğunu okumaya çalışır.",
                PrototypeEncounterPhase.ToolResponse =>
                    "Figür Palet Bıçağı, Sünger, Sabitleyici veya Çerçeve Tabancası karşılığını seçer. Kaynak kararı görünür hâle gelir.",
                PrototypeEncounterPhase.CombinationPressure =>
                    "Bölgenin en yoğun penceresi. Ressam sistemleri birleştirir; Figür tek araca güvenmek yerine takım ve rota çözümü arar.",
                PrototypeEncounterPhase.RescueAndEscape =>
                    "Baskının sonucu çözülür. Takım arkadaşını kurtar, kötü yüzeyi ters kullan veya düğümden çık.",
                PrototypeEncounterPhase.Breath =>
                    "Tam güvenli değildir. Yeni saldırı yığmak yerine konumu, pigmenti ve sonraki rotayı yeniden değerlendir.",
                PrototypeEncounterPhase.FinalEscape =>
                    "Son rota açık. Normal Figür olarak çerçeveyi geç; Leke varışı tek başına normal çıkış sayılmaz.",
                _ => string.Empty
            };
        }
    }
}
