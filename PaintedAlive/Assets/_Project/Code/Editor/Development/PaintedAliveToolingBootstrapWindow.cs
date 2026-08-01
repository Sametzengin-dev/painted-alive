#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace PaintedAlive.Editor.Development
{
    public sealed class PaintedAliveToolingBootstrapWindow : EditorWindow
    {
        private const string TriInspectorPackage = "com.codewriter.triinspector";
        private const string PrimeTweenPackage = "com.kyrylokuzyk.primetween";
        private const string DebugConsolePackage = "com.yasirkula.ingamedebugconsole";
        private const string AssetUsagePackage = "com.yasirkula.assetusagedetector";
        private const string MemoryProfilerPackage = "com.unity.memoryprofiler";
        private const string ProfileAnalyzerPackage = "com.unity.performance.profile-analyzer";

        private const string TriInspectorUrl =
            "https://github.com/codewriter-packages/Tri-Inspector.git";
        private const string DebugConsoleUrl =
            "https://github.com/yasirkula/UnityIngameDebugConsole.git";
        private const string AssetUsageUrl =
            "https://github.com/yasirkula/UnityAssetUsageDetector.git";

        private Vector2 scroll;
        private ToolStatus[] statuses = Array.Empty<ToolStatus>();

        [MenuItem("Tools/Painted Alive/Development Tools/43.0 - Open Tooling Bootstrap")]
        public static void Open()
        {
            PaintedAliveToolingBootstrapWindow window =
                GetWindow<PaintedAliveToolingBootstrapWindow>("PA Tooling");
            window.minSize = new Vector2(650f, 530f);
            window.RefreshStatuses();
            window.Show();
        }

        [MenuItem("Tools/Painted Alive/Development Tools/43.0 - Diagnose Tooling Readiness")]
        public static void Diagnose()
        {
            ToolingReport report = BuildReport();
            Debug.Log(FormatReport(report));
            WriteReport(report);
        }

        [MenuItem("Tools/Painted Alive/Development Tools/43.0 - Open Package Manager")]
        public static void OpenPackageManager()
        {
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }

        private void OnEnable()
        {
            RefreshStatuses();
        }

        private void OnFocus()
        {
            RefreshStatuses();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "M43.0 — Geliştirme Araçları Temeli",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Bu pencere paketleri otomatik kurmaz. Her aracı tek tek kurup Unity'nin " +
                "derlemesini tamamlamasını bekle. M42.2 FishNet/Steam ağı park edilmiş kalır.",
                MessageType.Info);

            DrawArchitectureGuard();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Durumları Yenile", GUILayout.Height(28f)))
                {
                    RefreshStatuses();
                }

                if (GUILayout.Button("Package Manager", GUILayout.Height(28f)))
                {
                    OpenPackageManager();
                }

                if (GUILayout.Button("JSON Raporu Yaz", GUILayout.Height(28f)))
                {
                    ToolingReport report = BuildReport();
                    string path = WriteReport(report);
                    EditorUtility.RevealInFinder(path);
                }
            }

            EditorGUILayout.Space(8f);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawSection(
                "A — Şimdi kurulacak çekirdek araçlar",
                statuses.Where(status => status.priority == ToolPriority.Core));

            DrawSection(
                "B — Temizlik ve ölçüm araçları",
                statuses.Where(status => status.priority == ToolPriority.Support));

            DrawSection(
                "C — Bilinçli olarak park edilenler",
                statuses.Where(status => status.priority == ToolPriority.Parked));

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Hazır kabul kriteri: Tri Inspector + PrimeTween kurulu; Fusion ve FishNet kurulu değil. " +
                "Debug Console ve Asset Usage Detector bundan sonra, profiler paketleri ise M43 HUD " +
                "ilk çalışır hâle geldiğinde eklenebilir.",
                MessageType.None);
        }

        private void DrawArchitectureGuard()
        {
            bool fusionFound = FindType("Fusion.NetworkRunner") != null;
            bool fishNetFound = FindType("FishNet.Managing.NetworkManager") != null;
            bool odinFound =
                FindType("Sirenix.OdinInspector.ButtonAttribute") != null ||
                FindType("Sirenix.Utilities.Editor.SirenixEditorGUI") != null;

            if (fusionFound || fishNetFound)
            {
                EditorGUILayout.HelpBox(
                    $"Ağ park kuralı ihlali: Fusion={fusionFound}, FishNet={fishNetFound}. " +
                    "M42.2 tekrar açılana kadar ağ SDK'sı projede bulunmamalı.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Ağ park kuralı temiz: Fusion ve FishNet algılanmadı.",
                    MessageType.Info);
            }

            if (odinFound)
            {
                EditorGUILayout.HelpBox(
                    "Odin Inspector algılandı. Tri Inspector ile birlikte kullanılabilir fakat " +
                    "bu proje için iki inspector framework'ü aynı anda önerilmiyor.",
                    MessageType.Warning);
            }
        }

        private void DrawSection(string title, IEnumerable<ToolStatus> rows)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            foreach (ToolStatus row in rows)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
                        statusStyle.normal.textColor = row.installed
                            ? new Color(0.25f, 0.72f, 0.34f)
                            : row.priority == ToolPriority.Parked
                                ? new Color(0.75f, 0.55f, 0.20f)
                                : new Color(0.85f, 0.34f, 0.30f);

                        EditorGUILayout.LabelField(
                            row.installed ? "HAZIR" :
                            row.priority == ToolPriority.Parked ? "PARK" : "EKSİK",
                            statusStyle,
                            GUILayout.Width(60f));

                        EditorGUILayout.LabelField(row.displayName, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();

                        if (!string.IsNullOrWhiteSpace(row.version))
                        {
                            EditorGUILayout.LabelField(
                                row.version,
                                EditorStyles.miniLabel,
                                GUILayout.Width(100f));
                        }
                    }

                    EditorGUILayout.LabelField(
                        row.description,
                        EditorStyles.wordWrappedMiniLabel);

                    if (!string.IsNullOrWhiteSpace(row.installReference))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Kurulum bilgisini kopyala", GUILayout.Width(175f)))
                            {
                                EditorGUIUtility.systemCopyBuffer = row.installReference;
                                ShowNotification(new GUIContent("Panoya kopyalandı"));
                            }

                            EditorGUILayout.SelectableLabel(
                                row.installReference,
                                EditorStyles.textField,
                                GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        }
                    }
                }
            }
        }

        private void RefreshStatuses()
        {
            statuses = BuildStatuses();
            Repaint();
        }

        private static ToolStatus[] BuildStatuses()
        {
            Dictionary<string, PackageManagerPackageInfo> packages = GetPackages();

            return new[]
            {
                MakePackageStatus(
                    "Tri Inspector",
                    TriInspectorPackage,
                    packages,
                    ToolPriority.Core,
                    "Config ve runtime read-only alanlarını özel editor yazmadan okunabilir hâle getirir.",
                    TriInspectorUrl,
                    "TriInspector.TriInspectorElement"),

                MakePackageStatus(
                    "PrimeTween",
                    PrimeTweenPackage,
                    packages,
                    ToolPriority.Core,
                    "M43 HUD geçişleri, telegraph, banner ve game-feel animasyonlarının temelidir.",
                    "Scoped Registry: https://registry.npmjs.org/ | Scope: com.kyrylokuzyk | Package: com.kyrylokuzyk.primetween@1.4.11",
                    "PrimeTween.Tween"),

                MakePackageStatus(
                    "In-game Debug Console",
                    DebugConsolePackage,
                    packages,
                    ToolPriority.Support,
                    "F tuşlarına yayılan debug işlemlerini zamanla komut konsoluna taşımak için kullanılacak.",
                    DebugConsoleUrl,
                    "IngameDebugConsole.DebugLogConsole"),

                MakePackageStatus(
                    "Asset Usage Detector",
                    AssetUsagePackage,
                    packages,
                    ToolPriority.Support,
                    "Eski milestone ve debug dosyalarını referans kırmadan temizlemeyi kolaylaştırır.",
                    AssetUsageUrl,
                    "AssetUsageDetector.AssetUsageDetectorWindow"),

                MakePackageStatus(
                    "Memory Profiler",
                    MemoryProfilerPackage,
                    packages,
                    ToolPriority.Support,
                    "HUD, boya ve yaratık yüklerinde snapshot karşılaştırması sağlar.",
                    "Unity Registry package: com.unity.memoryprofiler@1.1.9",
                    "Unity.MemoryProfiler.Editor.MemoryProfilerWindow"),

                MakePackageStatus(
                    "Profile Analyzer",
                    ProfileAnalyzerPackage,
                    packages,
                    ToolPriority.Support,
                    "Çok kareli CPU kayıtlarını ve iki performans koşusunu karşılaştırır.",
                    "Unity Registry package: com.unity.performance.profile-analyzer@1.2.4",
                    "UnityEditor.Performance.ProfileAnalyzer.ProfileAnalyzerWindow"),

                MakeTypeStatus(
                    "Fusion",
                    "Fusion.NetworkRunner",
                    ToolPriority.Parked,
                    "İptal edildi. Photon/Fusion üretim yolunda kullanılmayacak."),

                MakeTypeStatus(
                    "FishNet + Steam",
                    "FishNet.Managing.NetworkManager",
                    ToolPriority.Parked,
                    "M42.2 paketi saklanır fakat M43–M48 boyunca kurulmaz.")
            };
        }

        private static ToolStatus MakePackageStatus(
            string displayName,
            string packageName,
            IReadOnlyDictionary<string, PackageManagerPackageInfo> packages,
            ToolPriority priority,
            string description,
            string installReference,
            string fallbackType)
        {
            packages.TryGetValue(packageName, out PackageManagerPackageInfo info);
            bool installed = info != null || FindType(fallbackType) != null;

            return new ToolStatus
            {
                displayName = displayName,
                installed = installed,
                version = info != null ? info.version : installed ? "Assets" : string.Empty,
                packageName = packageName,
                priority = priority,
                description = description,
                installReference = installReference
            };
        }

        private static ToolStatus MakeTypeStatus(
            string displayName,
            string fullTypeName,
            ToolPriority priority,
            string description)
        {
            bool installed = FindType(fullTypeName) != null;
            return new ToolStatus
            {
                displayName = displayName,
                installed = installed,
                version = installed ? "ALGILANDI" : "Kurulu değil",
                priority = priority,
                description = description,
                installReference = string.Empty
            };
        }

        private static Dictionary<string, PackageManagerPackageInfo> GetPackages()
        {
            PackageManagerPackageInfo[] registered;
            try
            {
                registered = PackageManagerPackageInfo.GetAllRegisteredPackages();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[M43.0] Package listesi okunamadı: " + exception.Message);
                registered = Array.Empty<PackageManagerPackageInfo>();
            }

            return registered
                .Where(package => package != null && !string.IsNullOrWhiteSpace(package.name))
                .GroupBy(package => package.name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static ToolingReport BuildReport()
        {
            ToolStatus[] tools = BuildStatuses();
            bool fusionFound = FindType("Fusion.NetworkRunner") != null;
            bool fishNetFound = FindType("FishNet.Managing.NetworkManager") != null;

            return new ToolingReport
            {
                schemaVersion = "m43-development-tooling-baseline-1.0.0",
                utcCreatedAt = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                m42_2Parked = true,
                fusionFound = fusionFound,
                fishNetFound = fishNetFound,
                networkParkRulePassed = !fusionFound && !fishNetFound,
                triInspectorReady = FindInstalled(tools, "Tri Inspector"),
                primeTweenReady = FindInstalled(tools, "PrimeTween"),
                debugConsoleReady = FindInstalled(tools, "In-game Debug Console"),
                assetUsageDetectorReady = FindInstalled(tools, "Asset Usage Detector"),
                memoryProfilerReady = FindInstalled(tools, "Memory Profiler"),
                profileAnalyzerReady = FindInstalled(tools, "Profile Analyzer"),
                coreToolingReady =
                    FindInstalled(tools, "Tri Inspector") &&
                    FindInstalled(tools, "PrimeTween") &&
                    !fusionFound &&
                    !fishNetFound
            };
        }

        private static bool FindInstalled(IEnumerable<ToolStatus> tools, string name)
        {
            ToolStatus status = tools.FirstOrDefault(
                item => string.Equals(item.displayName, name, StringComparison.Ordinal));
            return status != null && status.installed;
        }

        private static string WriteReport(ToolingReport report)
        {
            string folder = Path.Combine(
                Application.persistentDataPath,
                "PlaytestTelemetry/M43_DevelopmentTooling");
            Directory.CreateDirectory(folder);

            string path = Path.Combine(
                folder,
                $"M43_Tooling_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");

            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log($"[M43.0] Tooling report written:\n{path}");
            return path;
        }

        private static string FormatReport(ToolingReport report)
        {
            return
                "[M43.0] DEVELOPMENT TOOLING READINESS\n" +
                $"Unity={report.unityVersion}\n" +
                $"Scene={report.activeScene}\n" +
                $"M42.2Parked={report.m42_2Parked}\n" +
                $"NetworkParkRulePassed={report.networkParkRulePassed}\n" +
                $"TriInspector={report.triInspectorReady}\n" +
                $"PrimeTween={report.primeTweenReady}\n" +
                $"DebugConsole={report.debugConsoleReady}\n" +
                $"AssetUsageDetector={report.assetUsageDetectorReady}\n" +
                $"MemoryProfiler={report.memoryProfilerReady}\n" +
                $"ProfileAnalyzer={report.profileAnalyzerReady}\n" +
                $"CoreToolingReady={report.coreToolingReady}";
        }

        private enum ToolPriority
        {
            Core,
            Support,
            Parked
        }

        [Serializable]
        private sealed class ToolStatus
        {
            public string displayName;
            public string packageName;
            public string version;
            public bool installed;
            public ToolPriority priority;
            public string description;
            public string installReference;
        }

        [Serializable]
        private sealed class ToolingReport
        {
            public string schemaVersion;
            public string utcCreatedAt;
            public string unityVersion;
            public string activeScene;
            public bool m42_2Parked;
            public bool fusionFound;
            public bool fishNetFound;
            public bool networkParkRulePassed;
            public bool triInspectorReady;
            public bool primeTweenReady;
            public bool debugConsoleReady;
            public bool assetUsageDetectorReady;
            public bool memoryProfilerReady;
            public bool profileAnalyzerReady;
            public bool coreToolingReady;
        }
    }
}
#endif
