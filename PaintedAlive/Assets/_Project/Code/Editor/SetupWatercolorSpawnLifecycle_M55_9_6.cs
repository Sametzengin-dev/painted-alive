#if UNITY_EDITOR
using PaintedAlive.Figures.StainSupport;
using PaintedAlive.Paint.Watercolor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    public static class SetupWatercolorSpawnLifecycle_M55_9_6
    {
        private const string MenuRoot = "Painted Alive/M55.9.6/";

        [MenuItem(MenuRoot + "Apply Event-Driven Watercolor Spawn")]
        public static void Apply()
        {
            int flowSurfacesUpdated = 0;
            int testSpawnersUpdated = 0;

            WatercolorFlowSurface[] surfaces =
                Object.FindObjectsByType<WatercolorFlowSurface>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < surfaces.Length; i++)
            {
                WatercolorFlowSurface surface = surfaces[i];
                if (surface == null || !surface.gameObject.scene.IsValid())
                    continue;

                Undo.RecordObject(surface, "Disable Watercolor Auto Start");
                surface.SetInitializeOnStart(false);
                EditorUtility.SetDirty(surface);
                flowSurfacesUpdated++;
            }

            StainWatercolorFlowTestSpawner[] spawners =
                Object.FindObjectsByType<StainWatercolorFlowTestSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < spawners.Length; i++)
            {
                StainWatercolorFlowTestSpawner spawner = spawners[i];
                if (spawner == null || !spawner.gameObject.scene.IsValid())
                    continue;

                Undo.RecordObject(spawner, "Disable Automatic Watercolor Test Spawn");
                spawner.DisableAutomaticTestSpawn();
                EditorUtility.SetDirty(spawner);
                testSpawnersUpdated++;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[M55.9.6] Event-driven watercolor spawn applied. " +
                $"FlowSurfaces={flowSurfacesUpdated}, " +
                $"TestSpawners={testSpawnersUpdated}. " +
                "Normal match start will not spawn watercolor automatically.");
        }

        [MenuItem(MenuRoot + "Diagnose Watercolor Spawn Lifecycle")]
        public static void Diagnose()
        {
            int sceneFlowSurfaces = 0;
            int autoStartFlows = 0;
            int testSpawners = 0;
            int autoTestSpawners = 0;

            WatercolorFlowSurface[] surfaces =
                Object.FindObjectsByType<WatercolorFlowSurface>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < surfaces.Length; i++)
            {
                WatercolorFlowSurface surface = surfaces[i];
                if (surface == null || !surface.gameObject.scene.IsValid())
                    continue;

                sceneFlowSurfaces++;
                if (surface.InitializeOnStart)
                    autoStartFlows++;
            }

            StainWatercolorFlowTestSpawner[] spawners =
                Object.FindObjectsByType<StainWatercolorFlowTestSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < spawners.Length; i++)
            {
                StainWatercolorFlowTestSpawner spawner = spawners[i];
                if (spawner == null || !spawner.gameObject.scene.IsValid())
                    continue;

                testSpawners++;
                if (spawner.AllowAutomaticTestSpawn && spawner.SpawnOnPlayModeStart)
                    autoTestSpawners++;
            }

            bool pass = autoStartFlows == 0 && autoTestSpawners == 0;

            string report =
                "Painted Alive M55.9.6 — Watercolor Spawn Lifecycle\n" +
                $"Result={(pass ? "PASS" : "NEEDS_ATTENTION")}\n" +
                $"SceneFlowSurfaces={sceneFlowSurfaces}\n" +
                $"AutoStartFlowSurfaces={autoStartFlows} expected=0\n" +
                $"TestSpawners={testSpawners}\n" +
                $"AutomaticTestSpawners={autoTestSpawners} expected=0\n" +
                "SpawnPolicy=Explicit gameplay/debug action only\n" +
                "ManualDebugF8=Preserved\n" +
                "RuntimeValidation=Required";

            Debug.Log("[M55.9.6 Watercolor Spawn]\n" + report);
            EditorUtility.DisplayDialog(
                "M55.9.6 Watercolor Spawn Lifecycle",
                report,
                "Tamam");
        }
    }
}
#endif
