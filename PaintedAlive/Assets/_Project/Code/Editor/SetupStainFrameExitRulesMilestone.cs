#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    public static class SetupStainFrameExitRulesMilestone
    {
        private const string ConfigFolder = "Assets/_Project/Data/Figures";
        private const string ConfigPath = ConfigFolder + "/DA_StainFrameExitRules.asset";
        private const string GateName = "M36_PrototypeFrameExit";

        [MenuItem("Tools/Painted Alive/Milestones/36 - Setup Stain Frame Exit Rules")]
        public static void Setup()
        {
            try
            {
                if (!SceneManager.GetActiveScene().IsValid())
                {
                    throw new InvalidOperationException("Geçerli bir sahne açık değil.");
                }

                FigureMotor figureMotor = FindPrototypeFigure();
                FigureClarityState clarity =
                    figureMotor.GetComponent<FigureClarityState>();

                if (clarity == null)
                {
                    throw new InvalidOperationException(
                        "M36, FigureMotor ile aynı kökte FigureClarityState bulamadı.");
                }

                StainFrameExitConfig config = GetOrCreateConfig();
                PrototypeFrameExitGate gate = GetOrCreateGate(figureMotor.transform, config);

                FrameExitFeedbackHUD hud =
                    GetOrAddComponent<FrameExitFeedbackHUD>(figureMotor.gameObject);
                hud.Configure(config, clarity);

                StainFrameExitDebugProbe probe =
                    GetOrAddComponent<StainFrameExitDebugProbe>(figureMotor.gameObject);
                probe.Configure(clarity);

                EditorUtility.SetDirty(hud);
                EditorUtility.SetDirty(probe);
                EditorUtility.SetDirty(gate);
                EditorUtility.SetDirty(config);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();

                Debug.Log(
                    $"[Milestone 36] Kurulum tamamlandı. " +
                    $"Figure={figureMotor.name}, Clarity={clarity.CurrentLevel}, " +
                    $"Gate={gate.name}, Config={ConfigPath}.",
                    gate);

                EditorUtility.DisplayDialog(
                    "PAINTED ALIVE — M36",
                    "Leke Çerçeve Çıkış Kuralı kuruldu.\n\n" +
                    "Normal Figür kapıdan geçince normal çıkış sonucu üretir.\n" +
                    "Tam Leke kapıya ulaşınca yalnız destek varışı üretir; normal çıkış ve +250 puan verilmez.\n\n" +
                    "Sahneyi Ctrl+S ile kaydet.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M36 kurulamadı",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/36 - Diagnose Stain Frame Exit Rules")]
        public static void Diagnose()
        {
            FigureMotor figure = FindFirstSceneObject<FigureMotor>();
            FigureClarityState clarity = figure != null
                ? figure.GetComponent<FigureClarityState>()
                : null;
            FrameExitFeedbackHUD hud = figure != null
                ? figure.GetComponent<FrameExitFeedbackHUD>()
                : null;
            StainFrameExitDebugProbe probe = figure != null
                ? figure.GetComponent<StainFrameExitDebugProbe>()
                : null;
            PrototypeFrameExitGate gate = FindFirstSceneObject<PrototypeFrameExitGate>();
            StainFrameExitConfig config =
                AssetDatabase.LoadAssetAtPath<StainFrameExitConfig>(ConfigPath);

            Debug.Log(
                "[M36 Diagnose]\n" +
                $"Figure={(figure != null ? figure.name : "MISSING")}\n" +
                $"Clarity={(clarity != null ? clarity.CurrentLevel.ToString() : "MISSING")}\n" +
                $"Gate={(gate != null ? gate.name : "MISSING")}\n" +
                $"HUD={(hud != null ? "OK" : "MISSING")}\n" +
                $"Probe={(probe != null ? "OK" : "MISSING")}\n" +
                $"Config={(config != null ? ConfigPath : "MISSING")}\n" +
                $"LastOutcome={(probe != null ? probe.LastOutcome.ToString() : "Unknown")}\n" +
                $"LastScore={(probe != null ? probe.LastAwardedScore : 0)}\n" +
                $"CountsAsNormalExit={(probe != null && probe.LastCountsAsNormalExit)}\n" +
                $"GateLastFigure={(gate != null ? gate.LastFigureName : "Unknown")}");
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
                "M36 doğru prototip Figürü seçemedi. " +
                $"Sahnedeki FigureMotor sayısı={sceneFigures.Count}.");
        }

        private static StainFrameExitConfig GetOrCreateConfig()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder("Assets/_Project/Data", "Figures");

            StainFrameExitConfig config =
                AssetDatabase.LoadAssetAtPath<StainFrameExitConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<StainFrameExitConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static PrototypeFrameExitGate GetOrCreateGate(
            Transform figure,
            StainFrameExitConfig config)
        {
            GameObject gateObject = GameObject.Find(GateName);
            if (gateObject == null)
            {
                gateObject = new GameObject(GateName);
                Undo.RegisterCreatedObjectUndo(gateObject, "Create M36 Frame Exit");

                Transform parent = FindPreferredParent();
                if (parent != null)
                {
                    gateObject.transform.SetParent(parent, true);
                }

                Vector3 forward = Vector3.ProjectOnPlane(figure.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.001f)
                {
                    forward = Vector3.forward;
                }

                forward.Normalize();
                gateObject.transform.position = figure.position + forward * 8f;
                gateObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }

            BoxCollider trigger = GetOrAddComponent<BoxCollider>(gateObject);
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.45f, 0f);
            trigger.size = new Vector3(3.6f, 2.9f, 0.8f);

            Renderer[] renderers = BuildOrFindFrameVisuals(gateObject.transform);
            PrototypeFrameExitGate gate =
                GetOrAddComponent<PrototypeFrameExitGate>(gateObject);
            gate.Configure(config, renderers);

            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(gateObject);
            return gate;
        }

        private static Renderer[] BuildOrFindFrameVisuals(Transform gate)
        {
            var renderers = new List<Renderer>();
            CreateOrUpdateBar(gate, "Frame_Left", new Vector3(-1.65f, 1.45f, 0f), new Vector3(0.22f, 2.9f, 0.28f), renderers);
            CreateOrUpdateBar(gate, "Frame_Right", new Vector3(1.65f, 1.45f, 0f), new Vector3(0.22f, 2.9f, 0.28f), renderers);
            CreateOrUpdateBar(gate, "Frame_Top", new Vector3(0f, 2.82f, 0f), new Vector3(3.52f, 0.22f, 0.28f), renderers);
            return renderers.ToArray();
        }

        private static void CreateOrUpdateBar(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            List<Renderer> renderers)
        {
            Transform existing = parent.Find(name);
            GameObject bar;

            if (existing == null)
            {
                bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = name;
                Undo.RegisterCreatedObjectUndo(bar, "Create M36 Frame Bar");
                bar.transform.SetParent(parent, false);
            }
            else
            {
                bar = existing.gameObject;
            }

            bar.transform.localPosition = localPosition;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = localScale;

            Collider collider = bar.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.DestroyObjectImmediate(collider);
            }

            Renderer renderer = bar.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderers.Add(renderer);
            }
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

        private static T FindFirstSceneObject<T>() where T : Component
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < objects.Length; i++)
            {
                T candidate = objects[i];
                if (candidate != null &&
                    candidate.gameObject.scene.IsValid() &&
                    !EditorUtility.IsPersistent(candidate))
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
