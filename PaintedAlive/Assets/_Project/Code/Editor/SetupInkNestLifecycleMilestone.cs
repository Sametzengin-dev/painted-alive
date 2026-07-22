using System;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Ink.Lifecycle;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkNestLifecycleMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string InkDataFolder =
            RootFolder + "/Data/Paint/Ink";
        private const string ConfigPath =
            InkDataFolder + "/InkNestLifecycleConfig.asset";
        private const string SurfacePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/InkSurface.prefab";
        private const string CreaturePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/Lekebacak.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/19 - Setup Ink Nest Lifecycle")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M19 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                ValidatePrerequisites();
                EnsureFolder(InkDataFolder);
                InkNestLifecycleConfig config = GetOrCreateConfig();
                UpgradeSurfacePrefab(config);
                UpgradeCreaturePrefab(config);

                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M19 Setup] Ink nest lifecycle ready. " +
                    "CriticalGlyphDeath=EyeOrFoot, " +
                    $"DeathDuration={config.TotalDeathDuration:F2}s, " +
                    $"FirstNestSpawn={config.FirstSpawnDelay:F2}s, " +
                    $"SpawnInterval={config.SpawnInterval:F2}s, " +
                    $"MaxChildrenPerNest={config.MaximumActiveChildren}. " +
                    "Prefab assets were updated; the scene was not saved " +
                    "automatically.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/19 - Diagnose Ink Nest Lifecycle")]
        public static void Diagnose()
        {
            InkNestSpawner[] nests =
                UnityEngine.Object.FindObjectsByType<InkNestSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkCreatureDeathSequence[] deathSequences =
                UnityEngine.Object.FindObjectsByType<
                    InkCreatureDeathSequence>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int dyingCount = 0;
            int activeNestChildren = 0;
            int totalNestSpawns = 0;

            for (int i = 0; i < deathSequences.Length; i++)
            {
                if (deathSequences[i].IsDying)
                {
                    dyingCount++;
                }
            }

            for (int i = 0; i < nests.Length; i++)
            {
                activeNestChildren += nests[i].ActiveChildCount;
                totalNestSpawns += nests[i].TotalChildrenSpawned;
            }

            GameObject surfacePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    SurfacePrefabPath);
            GameObject creaturePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CreaturePrefabPath);
            bool surfacePrefabReady = surfacePrefab != null &&
                surfacePrefab.GetComponent<InkNestSpawner>() != null;
            bool creaturePrefabReady = creaturePrefab != null &&
                creaturePrefab.GetComponent<InkCreatureDeathSequence>() !=
                null;
            int configCount = AssetDatabase.FindAssets(
                "t:InkNestLifecycleConfig").Length;
            bool managerApiReady = typeof(InkSystemManager).GetMethod(
                "TrySpawnLekebacakFromNest") != null;
            int activeCreatures = managers.Length == 1
                ? managers[0].ActiveCreatureCount
                : 0;
            int creatureLimit = managers.Length == 1
                ? managers[0].CreatureLimit
                : 0;

            Debug.Log(
                "[M19 Diagnose] " +
                $"Configs={configCount}, " +
                $"ManagerApiReady={managerApiReady}, " +
                $"SurfacePrefabReady={surfacePrefabReady}, " +
                $"CreaturePrefabReady={creaturePrefabReady}, " +
                $"RuntimeNests={nests.Length}, " +
                $"DeathSequences={deathSequences.Length}, " +
                $"DyingCreatures={dyingCount}, " +
                $"ActiveNestChildren={activeNestChildren}, " +
                $"TotalNestSpawns={totalNestSpawns}, " +
                $"ActiveCreatures={activeCreatures}/{creatureLimit}, " +
                $"InkManagers={managers.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < nests.Length; i++)
            {
                InkNestSpawner nest = nests[i];
                Debug.Log(
                    "[M19 Diagnose Nest] " +
                    $"Path={GetHierarchyPath(nest.transform)}, " +
                    $"InkAmount=" +
                    $"{(nest.Surface != null ? nest.Surface.InkAmount : 0f):F1}, " +
                    $"ActiveChildren={nest.ActiveChildCount}, " +
                    $"TotalSpawned={nest.TotalChildrenSpawned}, " +
                    $"NextSpawnIn={nest.TimeUntilNextSpawn:F2}, " +
                    $"Telegraph={nest.SpawnTelegraphActive}, " +
                    $"LastResult={nest.LastSpawnResult}",
                    nest);
            }

            for (int i = 0; i < deathSequences.Length; i++)
            {
                InkCreatureDeathSequence sequence = deathSequences[i];

                if (!sequence.IsDying)
                {
                    continue;
                }

                Debug.Log(
                    "[M19 Diagnose Death] " +
                    $"Path={GetHierarchyPath(sequence.transform)}, " +
                    $"Cause={sequence.DeathCause}, " +
                    $"Remaining={sequence.RemainingLifetime:F2}",
                    sequence);
            }
        }

        private static void ValidatePrerequisites()
        {
            if (AssetDatabase.LoadAssetAtPath<InkSystemConfig>(
                    InkDataFolder + "/InkSystemConfig.asset") == null)
            {
                throw new InvalidOperationException(
                    "M15 InkSystemConfig bulunamadı.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    SurfacePrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CreaturePrefabPath) == null)
            {
                throw new InvalidOperationException(
                    "M15 InkSurface veya Lekebacak prefabı bulunamadı.");
            }

            if (typeof(InkCreatureRuntime).GetMethod("TryDamageGlyph") ==
                    null ||
                typeof(InkSurface).GetMethod("AbsorbInk") == null)
            {
                throw new InvalidOperationException(
                    "M16 Ink counterplay yükseltmesi bulunamadı.");
            }

            if (typeof(InkSystemManager).GetMethod(
                    "TrySpawnLekebacakFromNest") == null)
            {
                throw new InvalidOperationException(
                    "M19 ReplaceExisting/Assets içindeki " +
                    "InkSystemManager.cs projeye kopyalanmamış.");
            }

            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (managers.Length != 1)
            {
                throw new InvalidOperationException(
                    "M19 tam olarak bir InkSystemManager bekliyor; " +
                    $"bulunan={managers.Length}.");
            }
        }

        private static InkNestLifecycleConfig GetOrCreateConfig()
        {
            InkNestLifecycleConfig config =
                AssetDatabase.LoadAssetAtPath<InkNestLifecycleConfig>(
                    ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<
                    InkNestLifecycleConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            return config;
        }

        private static void UpgradeSurfacePrefab(
            InkNestLifecycleConfig config)
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(SurfacePrefabPath);

            try
            {
                InkSurface surface = root.GetComponent<InkSurface>();

                if (surface == null)
                {
                    throw new InvalidOperationException(
                        "InkSurface prefabında InkSurface bulunamadı.");
                }

                InkNestSpawner spawner =
                    root.GetComponent<InkNestSpawner>();

                if (spawner == null)
                {
                    spawner = root.AddComponent<InkNestSpawner>();
                }

                spawner.Configure(
                    surface,
                    root.GetComponent<Renderer>(),
                    config);
                EditorUtility.SetDirty(spawner);
                PrefabUtility.SaveAsPrefabAsset(root, SurfacePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpgradeCreaturePrefab(
            InkNestLifecycleConfig config)
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(CreaturePrefabPath);

            try
            {
                InkCreatureRuntime creature =
                    root.GetComponent<InkCreatureRuntime>();

                if (creature == null)
                {
                    throw new InvalidOperationException(
                        "Lekebacak prefabında InkCreatureRuntime bulunamadı.");
                }

                InkCreatureDeathSequence sequence =
                    root.GetComponent<InkCreatureDeathSequence>();

                if (sequence == null)
                {
                    sequence = root.AddComponent<
                        InkCreatureDeathSequence>();
                }

                sequence.Configure(creature, config);
                EditorUtility.SetDirty(sequence);
                PrefabUtility.SaveAsPrefabAsset(root, CreaturePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "Yok";
            }

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
