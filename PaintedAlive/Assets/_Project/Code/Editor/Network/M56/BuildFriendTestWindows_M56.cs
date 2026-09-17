using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PaintedAlive.EditorTools.Networking.M56
{
    public static class BuildFriendTestWindows_M56
    {
        private const string MenuRoot = "Painted Alive/M56.0/";
        private const string BuildFolder = "Builds/PaintedAlive_M56_FriendTest";
        private const string ExeName = "PaintedAlive_M56_FriendTest.exe";
        private const string ZipPath = "Builds/PaintedAlive_M56_FriendTest.zip";

        [MenuItem(MenuRoot + "3 - Build Windows Friend Test + ZIP")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Build almadan önce Play Mode'u kapat.");

            SetupFriend1v1Steam_M56.Setup();

            if (!DiagnoseFriend1v1Steam_M56.Diagnose(showDialog: false))
            {
                if (Application.isBatchMode)
                {
                    throw new InvalidOperationException(
                        "M56 Build blocked: Diagnose NEEDS_ATTENTION.");
                }

                EditorUtility.DisplayDialog(
                    "M56 Build blocked",
                    "M56 Diagnose NEEDS_ATTENTION. Önce dependency/setup hatalarını düzelt.",
                    "Tamam");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget !=
                BuildTarget.StandaloneWindows64)
            {
                bool switched =
                    EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Standalone,
                        BuildTarget.StandaloneWindows64);

                if (!switched)
                {
                    if (Application.isBatchMode)
                    {
                        throw new InvalidOperationException(
                            "M56 Build blocked: Windows 64 target switch failed.");
                    }

                    EditorUtility.DisplayDialog(
                        "M56 Build blocked",
                        "Windows 64 build target'a geçilemedi.",
                        "Tamam");
                    return;
                }
            }

            string[] scenes =
                EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToArray();

            if (scenes.Length == 0)
            {
                if (Application.isBatchMode)
                {
                    throw new InvalidOperationException(
                        "M56 Build blocked: enabled build scene yok.");
                }

                EditorUtility.DisplayDialog(
                    "M56 Build blocked",
                    "Build Settings içinde enabled scene yok.",
                    "Tamam");
                return;
            }

            string projectRoot =
                Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildRoot =
                Path.GetFullPath(Path.Combine(projectRoot, BuildFolder));
            string exePath = Path.Combine(buildRoot, ExeName);

            if (Directory.Exists(buildRoot))
                Directory.Delete(buildRoot, true);

            Directory.CreateDirectory(buildRoot);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            };

            FullScreenMode previousFullscreenMode =
                PlayerSettings.fullScreenMode;
            bool previousNativeResolution =
                PlayerSettings.defaultIsNativeResolution;
            bool previousResizableWindow =
                PlayerSettings.resizableWindow;

            BuildReport report;
            try
            {
                PlayerSettings.fullScreenMode =
                    FullScreenMode.FullScreenWindow;
                PlayerSettings.defaultIsNativeResolution = true;
                PlayerSettings.resizableWindow = true;

                Debug.Log(
                    "[M56 Build] Player startup override | " +
                    "Mode=FullScreenWindow NativeResolution=True");

                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                PlayerSettings.fullScreenMode = previousFullscreenMode;
                PlayerSettings.defaultIsNativeResolution =
                    previousNativeResolution;
                PlayerSettings.resizableWindow = previousResizableWindow;

                Debug.Log(
                    "[M56 Build] Project PlayerSettings restored | " +
                    $"Mode={previousFullscreenMode} " +
                    $"NativeResolution={previousNativeResolution} " +
                    $"Resizable={previousResizableWindow}");
            }

            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(
                    "[M56 Build] FAILED | " +
                    $"Result={summary.result} Errors={summary.totalErrors} " +
                    $"Warnings={summary.totalWarnings}");

                if (Application.isBatchMode)
                {
                    throw new InvalidOperationException(
                        $"M56 Build failed: Result={summary.result} " +
                        $"Errors={summary.totalErrors} " +
                        $"Warnings={summary.totalWarnings}");
                }

                EditorUtility.DisplayDialog(
                    "M56 Build failed",
                    $"Result={summary.result}\nErrors={summary.totalErrors}\nWarnings={summary.totalWarnings}",
                    "Tamam");
                return;
            }

            File.WriteAllText(
                Path.Combine(buildRoot, "steam_appid.txt"),
                "480" + System.Environment.NewLine);

            File.WriteAllText(
                Path.Combine(buildRoot, "README_FRIEND_TEST.txt"),
                BuildReadme());

            string absoluteZip =
                Path.GetFullPath(Path.Combine(projectRoot, ZipPath));

            Directory.CreateDirectory(
                Path.GetDirectoryName(absoluteZip) ?? projectRoot);

            if (File.Exists(absoluteZip))
                File.Delete(absoluteZip);

            ZipFile.CreateFromDirectory(
                buildRoot,
                absoluteZip,
                System.IO.Compression.CompressionLevel.Optimal,
                includeBaseDirectory: false);

            Debug.Log(
                "[M56 Build] SUCCESS\n" +
                $"Build={exePath}\n" +
                $"ZIP={absoluteZip}\n" +
                $"Size={summary.totalSize / (1024f * 1024f):0.0} MB\n" +
                "Send the ZIP to the second PC. Both users must run Steam on separate accounts.");

            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(absoluteZip);

                EditorUtility.DisplayDialog(
                    "M56 Friend build hazır",
                    "Build ve paylaşılabilir ZIP oluşturuldu:\n\n" +
                    absoluteZip +
                    "\n\nHost önce oyunu açıp HOST'a basar. Friend aynı ZIP'i açıp Host SteamID64 ile JOIN yapar.",
                    "Tamam");
            }
        }

        private static string BuildReadme()
        {
            return
                "PAINTED ALIVE — M56 FRIEND 1v1 STEAM TEST\r\n" +
                "===========================================\r\n\r\n" +
                "TEST APP ID: 480 (Spacewar) — production AppID değildir.\r\n\r\n" +
                "Gerekenler:\r\n" +
                "- 2 Windows PC\r\n" +
                "- 2 ayrı Steam hesabı\r\n" +
                "- Steam her iki PC'de de açık ve online\r\n" +
                "- İki oyuncu da aynı ZIP/build sürümünü kullanmalı\r\n\r\n" +
                "BAŞLANGIÇ / MENÜ:\r\n" +
                "- Oyun native masaüstü çözünürlüğünde borderless fullscreen açılır.\r\n" +
                "- M56 bağlantı menüsü açık, fare görünür ve serbest başlar.\r\n" +
                "- F11 fullscreen/windowed geçişi yapar.\r\n" +
                "- Aktif oturumda ESC menüyü açar/kapatır; menü açıkken oyun girdisi durur.\r\n" +
                "- Otoriter rol geldikten sonra rol kartını herhangi bir tuşla kapat veya otomatik geçmesini bekle.\r\n\r\n" +
                "HOST:\r\n" +
                "1. PaintedAlive_M56_FriendTest.exe aç.\r\n" +
                "2. Sağ üst M56 panelinde HOST • FIGURE FIRST bas.\r\n" +
                "3. My SteamID64 değerini COPY ile arkadaşına gönder.\r\n\r\n" +
                "JOINER:\r\n" +
                "1. Aynı build'i aç.\r\n" +
                "2. Host SteamID64 değerini text alanına yaz/yapıştır.\r\n" +
                "3. JOIN bas.\r\n\r\n" +
                "Başlangıç rolleri: Host=FIGURE, Joiner=PAINTER.\r\n" +
                "F1=Figure isteği, F2=Painter isteği.\r\n" +
                "Rol değişimi server'da atomik swap'tir: biri Painter olunca diğeri Figure olur.\r\n\r\n" +
                "FIGURE KONTROLLERİ:\r\n" +
                "- WASD hareket, Mouse bakış, Space zıplama, Left Shift koşu\r\n" +
                "- 1 Palette Knife, 2 Fixative, 3 Frame Gun, 4 Sponge\r\n" +
                "- E aktif aracı kullan, R Sponge release/restoration\r\n" +
                "- F1/F2 rol isteği, ESC menü\r\n\r\n" +
                "PAINTER KONTROLLERİ:\r\n" +
                "- WASD kamera, Q/E aşağı/yukarı, Mouse bakış/aim\r\n" +
                "- LMB boya, RMB hedeflenen world action\r\n" +
                "- 1 Wall stroke, 2 Ramp stroke, F7 Ink creature, R reframe/clear paint\r\n" +
                "- F1/F2 rol isteği, ESC menü\r\n\r\n" +
                "TEST:\r\n" +
                "1. Host başlangıçta Figure, joiner başlangıçta Painter olmalı.\r\n" +
                "2. Figure hareketi iki PC'de görünmeli.\r\n" +
                "3. F1/F2 ile atomik rol swap yap; Figure konumu korunmalı.\r\n" +
                "4. Palimpsest Painter action iki PC'de aynı sonucu vermeli.\r\n" +
                "5. Living Gallery Frame Gate action senkron olmalı.\r\n" +
                "6. Drying Paper WET=0.65 / DRY=1.0 davranışı senkron olmalı.\r\n" +
                "7. Moving Canvas action senkron olmalı.\r\n" +
                "8. Round reset iki PC'yi tutarlı duruma döndürmeli.\r\n" +
                "9. Joiner çıkınca host bekleme durumunda stabil kalmalı.\r\n\r\n" +
                "M56 sync scope:\r\n" +
                "- Server-owned role allocation + atomic role swap\r\n" +
                "- Figure pose/velocity friend-test replication\r\n" +
                "- Painter Oil strokes/clear\r\n" +
                "- Ink creature spawn commands\r\n" +
                "- Palimpsest + Living Gallery Painter world actions\r\n" +
                "- Round reset when second player joins\r\n\r\n" +
                "Production multiplayer tamamlandı anlamına gelmez. M57+ prediction, full gameplay state, scoring, late-join ve daha geniş authority hardening yapılacaktır.\r\n";
        }
    }
}
