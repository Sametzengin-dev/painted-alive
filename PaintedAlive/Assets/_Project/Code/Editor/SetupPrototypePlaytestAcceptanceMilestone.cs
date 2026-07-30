#if UNITY_EDITOR
using System;
using System.IO;
using PaintedAlive.Core.Playtests;
using PaintedAlive.Core.Playtests.Validation;
using PaintedAlive.Core.Prototypes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypePlaytestAcceptanceMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Core/DA_PrototypePlaytestAcceptance.asset";

        [MenuItem("Tools/Painted Alive/Milestones/41 - Setup Prototype Acceptance Gate")]
        public static void Setup()
        {
            try
            {
                PrototypeMatchController match =
                    FindRequiredSceneObject<PrototypeMatchController>(
                        "PrototypeMatchController");
                PrototypeOneVsOnePlaytestSession m40 =
                    FindRequiredSceneObject<PrototypeOneVsOnePlaytestSession>(
                        "M40 one-vs-one session");
                PrototypePlaytestTelemetry telemetry =
                    FindRequiredSceneObject<PrototypePlaytestTelemetry>(
                        "PrototypePlaytestTelemetry");

                PrototypePlaytestAcceptanceConfig config = CreateOrLoadConfig();
                PrototypePlaytestAcceptanceGate gate =
                    GetOrAddComponent<PrototypePlaytestAcceptanceGate>(match.gameObject);
                PrototypePlaytestAcceptanceHUD hud =
                    GetOrAddComponent<PrototypePlaytestAcceptanceHUD>(match.gameObject);

                Undo.RecordObject(gate, "Configure M41 prototype acceptance gate");
                gate.Configure(match, m40, telemetry, config);

                Undo.RecordObject(hud, "Configure M41 prototype acceptance HUD");
                hud.Configure(gate);

                EditorUtility.SetDirty(gate);
                EditorUtility.SetDirty(hud);
                EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[M41 Setup] Prototype acceptance gate ready.\n" +
                    $"Match={GetHierarchyPath(match.transform)}\n" +
                    $"GateCount={CountSceneObjects<PrototypePlaytestAcceptanceGate>()}\n" +
                    "M40=OK\n" +
                    "LegacyTelemetry=OK\n" +
                    $"EvaluationWindow={config.EvaluationWindow}\n" +
                    $"RequiredPassingRuns={config.RequiredPassingRuns}\n" +
                    "No match, score, encounter or telemetry authority was replaced.",
                    match);

                EditorUtility.DisplayDialog(
                    "M41 kurulumu tamamlandı",
                    "M40 ve mevcut ayrıntılı telemetry raporlarını birleştiren " +
                    "prototip kabul kapısı eklendi. Ana maç otoritesi değiştirilmedi.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M41 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/41 - Diagnose Prototype Acceptance Gate")]
        public static void Diagnose()
        {
            PrototypeMatchController match =
                FindFirstSceneObject<PrototypeMatchController>();
            PrototypeOneVsOnePlaytestSession m40 =
                FindFirstSceneObject<PrototypeOneVsOnePlaytestSession>();
            PrototypePlaytestTelemetry telemetry =
                FindFirstSceneObject<PrototypePlaytestTelemetry>();
            PrototypePlaytestAcceptanceGate gate =
                FindFirstSceneObject<PrototypePlaytestAcceptanceGate>();
            PrototypePlaytestAcceptanceHUD hud =
                FindFirstSceneObject<PrototypePlaytestAcceptanceHUD>();

            Debug.Log(
                "[M41 Diagnose]\n" +
                $"PrototypeMatchController={(match != null ? GetHierarchyPath(match.transform) : "MISSING")}\n" +
                $"MatchState={(match != null ? match.State.ToString() : "Unknown")}\n" +
                $"M40Session={(m40 != null ? "OK" : "MISSING")}\n" +
                $"LegacyTelemetry={(telemetry != null ? "OK" : "MISSING")}\n" +
                $"Gate={(gate != null ? "OK" : "MISSING")}\n" +
                $"GateCount={CountSceneObjects<PrototypePlaytestAcceptanceGate>()}\n" +
                $"HUD={(hud != null ? "OK" : "MISSING")}\n" +
                $"ReviewActive={(gate != null && gate.ReviewActive)}\n" +
                $"ReviewCompleted={(gate != null && gate.ReviewCompleted)}\n" +
                $"CurrentRunPassed={(gate != null && gate.CurrentRunPassed)}\n" +
                $"NetworkSpikeCandidateReady={(gate != null && gate.NetworkSpikeCandidateReady)}\n" +
                $"RunReport={(gate != null ? gate.CurrentRunReportPath : string.Empty)}\n" +
                $"AggregateReport={(gate != null ? gate.AggregateReportPath : string.Empty)}");
        }

        [MenuItem("Tools/Painted Alive/Milestones/41 - Open Acceptance Reports Folder")]
        public static void OpenReportsFolder()
        {
            string path = Path.Combine(
                Application.persistentDataPath,
                "PlaytestTelemetry/M41_Acceptance");
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        private static PrototypePlaytestAcceptanceConfig CreateOrLoadConfig()
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Core");

            PrototypePlaytestAcceptanceConfig config =
                AssetDatabase.LoadAssetAtPath<PrototypePlaytestAcceptanceConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PrototypePlaytestAcceptanceConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static T FindRequiredSceneObject<T>(string label)
            where T : UnityEngine.Object
        {
            T found = FindFirstSceneObject<T>();
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"M41 {label} bulamadı. M40'ın çalıştığı ana prototip sahnesini aç.");
            }

            return found;
        }

        private static T FindFirstSceneObject<T>() where T : UnityEngine.Object
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < objects.Length; i++)
            {
                T candidate = objects[i];
                if (candidate == null || EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                if (candidate is Component component && component.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static int CountSceneObjects<T>() where T : Component
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].gameObject.scene.IsValid())
                {
                    count++;
                }
            }

            return count;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
#endif
