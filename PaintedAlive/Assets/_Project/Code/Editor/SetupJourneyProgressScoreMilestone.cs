#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    public static class SetupJourneyProgressScoreMilestone
    {
        private const string ConfigFolder = "Assets/_Project/Data/Core";
        private const string ConfigPath = ConfigFolder + "/DA_PrototypeJourneyScore.asset";
        private const string RouteRootName = "M37_PrototypeJourneyRoute";
        private const string StartAnchorName = "Journey_Start";

        [MenuItem("Tools/Painted Alive/Milestones/37 - Setup Journey Progress and Score Ledger")]
        public static void Setup()
        {
            try
            {
                FigureMotor figureMotor = FindPrototypeFigure();
                FigureClarityState clarity =
                    figureMotor.GetComponent<FigureClarityState>();

                if (clarity == null)
                {
                    throw new InvalidOperationException(
                        "M37, FigureMotor ile aynı kökte FigureClarityState bulamadı.");
                }

                PrototypeFrameExitGate exitGate =
                    FindFirstSceneObject<PrototypeFrameExitGate>();

                if (exitGate == null)
                {
                    throw new InvalidOperationException(
                        "M37 mevcut M36 PrototypeFrameExitGate nesnesini bulamadı. " +
                        "Önce M36'nın kurulu ve çalışan durumda olduğundan emin ol.");
                }

                PrototypeJourneyScoreConfig config = GetOrCreateConfig();
                Transform startAnchor = GetOrCreateStartAnchor(figureMotor.transform);

                PrototypeJourneyScoreTracker tracker =
                    GetOrAddComponent<PrototypeJourneyScoreTracker>(figureMotor.gameObject);
                tracker.Configure(clarity, startAnchor, exitGate.transform, config);

                PrototypeJourneyScoreLedger ledger =
                    GetOrAddComponent<PrototypeJourneyScoreLedger>(figureMotor.gameObject);
                ledger.Configure(clarity);

                PrototypeJourneyScoreHUD hud =
                    GetOrAddComponent<PrototypeJourneyScoreHUD>(figureMotor.gameObject);
                hud.Configure(tracker, config, clarity);

                EditorUtility.SetDirty(tracker);
                EditorUtility.SetDirty(ledger);
                EditorUtility.SetDirty(hud);
                EditorUtility.SetDirty(config);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();

                Debug.Log(
                    $"[Milestone 37] Kurulum tamamlandı. " +
                    $"Figure={figureMotor.name}, Start={startAnchor.position}, " +
                    $"Finish={exitGate.transform.position}, Config={ConfigPath}.",
                    tracker);

                EditorUtility.DisplayDialog(
                    "PAINTED ALIVE — M37",
                    "Yolculuk mesafe skoru ve prototip skor defteri kuruldu.\n\n" +
                    "Mesafe geriye yürüyünce azalmaz.\n" +
                    "Normal Figür çıkışı mesafe skoruna M36 çıkış bonusunu ekler.\n" +
                    "Leke çıkışa ulaşınca mesafeyi korur fakat normal çıkış bonusu alamaz.\n\n" +
                    "Sahneyi Ctrl+S ile kaydet.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M37 kurulamadı",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/37 - Diagnose Journey Progress and Score Ledger")]
        public static void Diagnose()
        {
            FigureMotor figure = FindFirstSceneObject<FigureMotor>();
            PrototypeJourneyScoreTracker tracker = figure != null
                ? figure.GetComponent<PrototypeJourneyScoreTracker>()
                : null;
            PrototypeJourneyScoreLedger ledger = figure != null
                ? figure.GetComponent<PrototypeJourneyScoreLedger>()
                : null;
            PrototypeJourneyScoreHUD hud = figure != null
                ? figure.GetComponent<PrototypeJourneyScoreHUD>()
                : null;
            PrototypeFrameExitGate gate = FindFirstSceneObject<PrototypeFrameExitGate>();
            PrototypeJourneyScoreConfig config =
                AssetDatabase.LoadAssetAtPath<PrototypeJourneyScoreConfig>(ConfigPath);

            Debug.Log(
                "[M37 Diagnose]\n" +
                $"Figure={(figure != null ? figure.name : "MISSING")}\n" +
                $"Tracker={(tracker != null ? "OK" : "MISSING")}\n" +
                $"Ledger={(ledger != null ? "OK" : "MISSING")}\n" +
                $"HUD={(hud != null ? "OK" : "MISSING")}\n" +
                $"M36Gate={(gate != null ? gate.name : "MISSING")}\n" +
                $"Config={(config != null ? ConfigPath : "MISSING")}\n" +
                $"Start={(tracker != null && tracker.RouteStart != null ? tracker.RouteStart.name : "MISSING")}\n" +
                $"Finish={(tracker != null && tracker.RouteFinish != null ? tracker.RouteFinish.name : "MISSING")}\n" +
                $"Progress={(tracker != null ? tracker.MaximumProgress01.ToString("0.000") : "0")}\n" +
                $"DistanceScore={(tracker != null ? tracker.DistanceScore : 0)}\n" +
                $"ExitBonus={(tracker != null ? tracker.ExitBonus : 0)}\n" +
                $"TotalScore={(tracker != null ? tracker.TotalScore : 0)}\n" +
                $"LastOutcome={(tracker != null ? tracker.LastExitOutcome.ToString() : "Unknown")}\n" +
                $"NormalExit={(tracker != null && tracker.NormalExitCompleted)}\n" +
                $"LedgerEvents={(ledger != null ? ledger.RecordedEventCount : 0)}\n" +
                $"LastLedgerEvent={(ledger != null ? ledger.LastEvent : "None")}");
        }

        private static FigureMotor FindPrototypeFigure()
        {
            FigureMotor[] figures =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            var sceneFigures = new List<FigureMotor>();
            for (int i = 0; i < figures.Length; i++)
            {
                FigureMotor candidate = figures[i];
                if (candidate != null &&
                    candidate.gameObject.scene.IsValid() &&
                    !EditorUtility.IsPersistent(candidate))
                {
                    sceneFigures.Add(candidate);
                }
            }

            if (sceneFigures.Count == 1)
            {
                return sceneFigures[0];
            }

            for (int i = 0; i < sceneFigures.Count; i++)
            {
                if (sceneFigures[i].GetComponent<FigureClarityState>() != null)
                {
                    return sceneFigures[i];
                }
            }

            throw new InvalidOperationException(
                "M37 doğru prototip Figürü seçemedi. " +
                $"Sahnedeki FigureMotor sayısı={sceneFigures.Count}.");
        }

        private static PrototypeJourneyScoreConfig GetOrCreateConfig()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder("Assets/_Project/Data", "Core");

            PrototypeJourneyScoreConfig config =
                AssetDatabase.LoadAssetAtPath<PrototypeJourneyScoreConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PrototypeJourneyScoreConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static Transform GetOrCreateStartAnchor(Transform figure)
        {
            GameObject routeRoot = GameObject.Find(RouteRootName);
            if (routeRoot == null)
            {
                routeRoot = new GameObject(RouteRootName);
                Undo.RegisterCreatedObjectUndo(routeRoot, "Create M37 Journey Route");

                Transform parent = FindPreferredParent();
                if (parent != null)
                {
                    routeRoot.transform.SetParent(parent, true);
                }
            }

            Transform start = routeRoot.transform.Find(StartAnchorName);
            if (start == null)
            {
                GameObject startObject = new GameObject(StartAnchorName);
                Undo.RegisterCreatedObjectUndo(startObject, "Create M37 Journey Start");
                startObject.transform.SetParent(routeRoot.transform, true);
                start = startObject.transform;
            }

            start.position = figure.position;
            start.rotation = Quaternion.identity;
            start.localScale = Vector3.one;
            EditorUtility.SetDirty(start.gameObject);
            return start;
        }

        private static Transform FindPreferredParent()
        {
            GameObject environment = GameObject.Find("Environment");
            if (environment != null)
            {
                return environment.transform;
            }

            GameObject gameplay = GameObject.Find("Gameplay");
            return gameplay != null ? gameplay.transform : null;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = Undo.AddComponent<T>(target);
            }

            return component;
        }

        private static T FindFirstSceneObject<T>() where T : UnityEngine.Object
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < objects.Length; i++)
            {
                T candidate = objects[i];
                if (candidate is Component component &&
                    component.gameObject.scene.IsValid() &&
                    !EditorUtility.IsPersistent(component))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
