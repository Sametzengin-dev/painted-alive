#if UNITY_EDITOR
using System;
using PaintedAlive.Core.Encounters;
using PaintedAlive.Core.Playtests;
using PaintedAlive.Core.Prototypes;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Paint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeOneVsOnePlaytestMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Core/DA_PrototypeOneVsOnePlaytest.asset";

        [MenuItem("Tools/Painted Alive/Milestones/40 - Setup Five Minute One Vs One Playtest")]
        public static void Setup()
        {
            try
            {
                PrototypeMatchController match =
                    FindRequiredSceneObject<PrototypeMatchController>("PrototypeMatchController");
                PrototypeRoleSwitcher roleSwitcher =
                    FindRequiredSceneObject<PrototypeRoleSwitcher>("PrototypeRoleSwitcher");
                FigureProgressTracker progress =
                    FindRequiredSceneObject<FigureProgressTracker>("FigureProgressTracker");
                PrototypeJourneyScoreTracker score =
                    FindRequiredSceneObject<PrototypeJourneyScoreTracker>("M37 score tracker");
                PrototypeMatchExpeditionBridge bridge =
                    FindRequiredSceneObject<PrototypeMatchExpeditionBridge>("M38.1 bridge");
                PrototypeEncounterRhythmDirector director =
                    FindRequiredSceneObject<PrototypeEncounterRhythmDirector>("M39 director");
                PrototypeEncounterRhythmLedger ledger =
                    FindRequiredSceneObject<PrototypeEncounterRhythmLedger>("M39 ledger");
                OilStrokeSystem strokes =
                    FindRequiredSceneObject<OilStrokeSystem>("OilStrokeSystem");
                PrototypePlaytestTelemetry telemetry =
                    FindFirstSceneObject<PrototypePlaytestTelemetry>();

                PrototypeOneVsOnePlaytestConfig config = CreateOrLoadConfig();
                SetAuthoritativeMatchDuration(match, config.ExpectedMatchDuration);

                PrototypeOneVsOnePlaytestSession session =
                    GetOrAddComponent<PrototypeOneVsOnePlaytestSession>(match.gameObject);
                PrototypeOneVsOnePlaytestHUD hud =
                    GetOrAddComponent<PrototypeOneVsOnePlaytestHUD>(match.gameObject);

                Undo.RecordObject(session, "Configure M40 1v1 playtest session");
                session.Configure(
                    match,
                    roleSwitcher,
                    progress,
                    score,
                    bridge,
                    director,
                    ledger,
                    telemetry,
                    strokes,
                    config);

                Undo.RecordObject(hud, "Configure M40 1v1 playtest HUD");
                hud.Configure(session);

                EditorUtility.SetDirty(session);
                EditorUtility.SetDirty(hud);
                EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[M40 Setup] Five-minute 1v1 playtest protocol ready.\n" +
                    $"Match={GetHierarchyPath(match.transform)}\n" +
                    $"SessionCount={CountSceneObjects<PrototypeOneVsOnePlaytestSession>()}\n" +
                    $"M39Director={(director != null ? "OK" : "MISSING")}\n" +
                    $"M37Score={(score != null ? "OK" : "MISSING")}\n" +
                    $"LegacyTelemetry={(telemetry != null ? "OK" : "NOT FOUND")}\n" +
                    $"ExpectedDuration={config.ExpectedMatchDuration:0}s\n" +
                    "Old match/reset authority was not replaced.",
                    match);

                EditorUtility.DisplayDialog(
                    "M40 kurulumu tamamlandı",
                    "Mevcut PrototypeMatchController tek maç otoritesi olarak kaldı. " +
                    "5 dakikalık 1v1 test protokolü ve kabul HUD'u eklendi.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M40 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/40 - Diagnose Five Minute One Vs One Playtest")]
        public static void Diagnose()
        {
            PrototypeMatchController match = FindFirstSceneObject<PrototypeMatchController>();
            PrototypeOneVsOnePlaytestSession session =
                FindFirstSceneObject<PrototypeOneVsOnePlaytestSession>();
            PrototypeOneVsOnePlaytestHUD hud =
                FindFirstSceneObject<PrototypeOneVsOnePlaytestHUD>();
            PrototypePlaytestTelemetry telemetry =
                FindFirstSceneObject<PrototypePlaytestTelemetry>();
            PrototypeEncounterRhythmDirector director =
                FindFirstSceneObject<PrototypeEncounterRhythmDirector>();

            Debug.Log(
                "[M40 Diagnose]\n" +
                $"PrototypeMatchController={(match != null ? GetHierarchyPath(match.transform) : "MISSING")}\n" +
                $"MatchState={(match != null ? match.State.ToString() : "Unknown")}\n" +
                $"TimeRemaining={(match != null ? match.TimeRemaining : 0f):0.0}\n" +
                $"Session={(session != null ? "OK" : "MISSING")}\n" +
                $"SessionCount={CountSceneObjects<PrototypeOneVsOnePlaytestSession>()}\n" +
                $"HUD={(hud != null ? "OK" : "MISSING")}\n" +
                $"M39Director={(director != null ? "OK" : "MISSING")}\n" +
                $"LegacyTelemetry={(telemetry != null ? "OK" : "NOT FOUND")}\n" +
                $"Outcomes={(session != null ? session.PassedOutcomeCount : 0)}/" +
                $"{(session != null && session.Config != null ? session.Config.RequiredDistinctOutcomes : 3)}\n" +
                $"Accepted={(session != null && session.Accepted)}\n" +
                $"ReportPath={(session != null ? session.LastReportPath : string.Empty)}");
        }

        private static PrototypeOneVsOnePlaytestConfig CreateOrLoadConfig()
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Core");

            PrototypeOneVsOnePlaytestConfig config =
                AssetDatabase.LoadAssetAtPath<PrototypeOneVsOnePlaytestConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PrototypeOneVsOnePlaytestConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void SetAuthoritativeMatchDuration(
            PrototypeMatchController match,
            float expectedDuration)
        {
            SerializedObject matchObject = new SerializedObject(match);
            SerializedProperty configProperty = matchObject.FindProperty("config");
            if (configProperty == null || configProperty.objectReferenceValue == null)
            {
                Debug.LogWarning(
                    "[M40 Setup] PrototypeMatchController config asset'i bulunamadı; " +
                    "maç süresi değiştirilmedi.",
                    match);
                return;
            }

            SerializedObject matchConfig =
                new SerializedObject(configProperty.objectReferenceValue);
            SerializedProperty durationProperty =
                matchConfig.FindProperty("matchDuration");
            if (durationProperty == null)
            {
                Debug.LogWarning(
                    "[M40 Setup] matchDuration alanı bulunamadı; süre değiştirilmedi.",
                    configProperty.objectReferenceValue);
                return;
            }

            durationProperty.floatValue = Mathf.Max(30f, expectedDuration);
            matchConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configProperty.objectReferenceValue);
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
                    $"M40 {label} bulamadı. M39'un çalıştığı ana prototip sahnesini aç.");
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
