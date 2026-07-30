#if UNITY_EDITOR
using System;
using System.IO;
using PaintedAlive.Core.Playtests.Validation;
using PaintedAlive.Core.Prototypes;
using PaintedAlive.Network.Spike;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeNetworkTechnicalSpikeMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Network/DA_PrototypeNetworkSpike.asset";

        [MenuItem("Tools/Painted Alive/Milestones/42 - Setup Network Technical Spike Foundation")]
        public static void Setup()
        {
            try
            {
                PrototypeMatchController match =
                    FindRequiredSceneObject<PrototypeMatchController>(
                        "PrototypeMatchController");
                PrototypePlaytestAcceptanceGate gate =
                    FindRequiredSceneObject<PrototypePlaytestAcceptanceGate>(
                        "M41 acceptance gate");
                PrototypeNetworkSpikeConfig config = CreateOrLoadConfig();

                PrototypeNetworkSpikeHarness harness =
                    GetOrAddComponent<PrototypeNetworkSpikeHarness>(match.gameObject);
                PrototypeNetworkSpikeHUD hud =
                    GetOrAddComponent<PrototypeNetworkSpikeHUD>(match.gameObject);

                Undo.RecordObject(harness, "Configure M42 network spike harness");
                harness.Configure(match, gate, config);

                Undo.RecordObject(hud, "Configure M42 network spike HUD");
                hud.Configure(harness);

                EditorUtility.SetDirty(harness);
                EditorUtility.SetDirty(hud);
                EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[M42 Setup] Transport-neutral network spike foundation ready.\n" +
                    $"Match={GetHierarchyPath(match.transform)}\n" +
                    $"HarnessCount={CountSceneObjects<PrototypeNetworkSpikeHarness>()}\n" +
                    $"M41Gate=OK\n" +
                    $"M41CandidateReady={gate.NetworkSpikeCandidateReady}\n" +
                    $"FusionCandidate={config.FusionCandidate}\n" +
                    $"FishNetCandidate={config.FishNetCandidate}\n" +
                    "No network SDK was installed. No match, movement, paint or score authority was replaced.",
                    match);

                EditorUtility.DisplayDialog(
                    "M42 temel kurulumu tamamlandı",
                    "Fusion ve FishNet'in aynı sözleşmeyle ölçüleceği ortak " +
                    "stroke komutu, codec ve gecikme profili temeli eklendi. " +
                    "Henüz hiçbir ağ SDK'sı kurulmadı.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M42 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/42 - Diagnose Network Technical Spike Foundation")]
        public static void Diagnose()
        {
            PrototypeMatchController match =
                FindFirstSceneObject<PrototypeMatchController>();
            PrototypePlaytestAcceptanceGate gate =
                FindFirstSceneObject<PrototypePlaytestAcceptanceGate>();
            PrototypeNetworkSpikeHarness harness =
                FindFirstSceneObject<PrototypeNetworkSpikeHarness>();
            PrototypeNetworkSpikeHUD hud =
                FindFirstSceneObject<PrototypeNetworkSpikeHUD>();

            Debug.Log(
                "[M42 Diagnose]\n" +
                $"PrototypeMatchController={(match != null ? GetHierarchyPath(match.transform) : "MISSING")}\n" +
                $"MatchState={(match != null ? match.State.ToString() : "Unknown")}\n" +
                $"M41Gate={(gate != null ? "OK" : "MISSING")}\n" +
                $"M41CandidateReady={(gate != null && gate.NetworkSpikeCandidateReady)}\n" +
                $"Harness={(harness != null ? "OK" : "MISSING")}\n" +
                $"HarnessCount={CountSceneObjects<PrototypeNetworkSpikeHarness>()}\n" +
                $"HUD={(hud != null ? "OK" : "MISSING")}\n" +
                $"Running={(harness != null && harness.Running)}\n" +
                $"FoundationPassed={(harness != null && harness.LastFoundationPassed)}\n" +
                $"Report={(harness != null ? harness.LastReportPath : string.Empty)}");
        }

        [MenuItem("Tools/Painted Alive/Milestones/42 - Open Network Spike Reports Folder")]
        public static void OpenReportsFolder()
        {
            string path = Path.Combine(
                Application.persistentDataPath,
                "PlaytestTelemetry/M42_NetworkSpike");
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        private static PrototypeNetworkSpikeConfig CreateOrLoadConfig()
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Network");

            PrototypeNetworkSpikeConfig config =
                AssetDatabase.LoadAssetAtPath<PrototypeNetworkSpikeConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PrototypeNetworkSpikeConfig>();
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
                    $"M42 {label} bulamadı. M41'in çalıştığı ana prototip sahnesini aç.");
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
