using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Ink.Possession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkPossessionMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string InkDataFolder =
            RootFolder + "/Data/Paint/Ink";
        private const string ConfigPath =
            InkDataFolder + "/InkPossessionConfig.asset";
        private const string InkSystemConfigPath =
            InkDataFolder + "/InkSystemConfig.asset";
        private const string LekebacakDefinitionPath =
            InkDataFolder +
            "/Creatures/InkCreature_Lekebacak.asset";
        private const string CounterplayConfigPath =
            InkDataFolder + "/InkCounterplayConfig.asset";

        [MenuItem(
            "Tools/Painted Alive/Milestones/17 - Setup Ink Possession")]
        public static void Setup()
        {
            try
            {
                ValidatePrerequisites();
                EnsureFolder(InkDataFolder);

                InkPossessionConfig config = GetOrCreateConfig();
                FigureMotor targetFigure = ResolveTargetFigure();
                Camera sourceCamera = ResolveSourceCamera(targetFigure);
                InkPossessionController controller =
                    targetFigure.GetComponent<InkPossessionController>();

                if (controller == null)
                {
                    controller = Undo.AddComponent<InkPossessionController>(
                        targetFigure.gameObject);
                }

                SerializedObject serialized =
                    new SerializedObject(controller);
                SetReference(serialized, "targetFigure", targetFigure);
                SetReference(serialized, "sourceCamera", sourceCamera);
                SetReference(serialized, "config", config);
                SetMask(
                    serialized,
                    "navigationMask",
                    Physics.DefaultRaycastLayers);
                SetMask(
                    serialized,
                    "selectionMask",
                    Physics.DefaultRaycastLayers);
                SetMask(
                    serialized,
                    "cameraCollisionMask",
                    Physics.DefaultRaycastLayers);
                serialized.ApplyModifiedProperties();
                controller.enabled = true;

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(
                    controller.gameObject.scene);
                AssetDatabase.SaveAssets();

                InkPossessionController[] controllers =
                    UnityEngine.Object.FindObjectsByType<
                        InkPossessionController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Debug.Log(
                    "[M17 Setup] Ink possession ready. " +
                    $"Config={ConfigPath}, " +
                    $"TargetFigure={GetHierarchyPath(targetFigure.transform)}, " +
                    $"Camera={GetHierarchyPath(sourceCamera.transform)}, " +
                    $"Controllers={controllers.Length}. " +
                    "Scene was not saved automatically. Press Ctrl+S, then " +
                    "Play Mode: F9 spawn, aim at Lekebacak, F6 possess.",
                    controller);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/17 - Diagnose Ink Possession")]
        public static void Diagnose()
        {
            InkPossessionController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkCreatureRuntime[] creatures =
                UnityEngine.Object.FindObjectsByType<InkCreatureRuntime>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int configCount =
                AssetDatabase.FindAssets("t:InkPossessionConfig").Length;
            int activePossessions = 0;

            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i].IsPossessing)
                {
                    activePossessions++;
                }
            }

            Debug.Log(
                "[M17 Diagnose] " +
                $"PossessionConfigs={configCount}, " +
                $"PossessionControllers={controllers.Length}, " +
                $"InkManagers={managers.Length}, " +
                $"RuntimeCreatures={creatures.Length}, " +
                $"ActivePossessions={activePossessions}, " +
                $"DuplicateComponents={Mathf.Max(0, controllers.Length - 1)}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                InkPossessionController controller = controllers[i];
                Debug.Log(
                    "[M17 Diagnose Controller] " +
                    $"Path={GetHierarchyPath(controller.transform)}, " +
                    $"TargetFigure=" +
                    $"{GetObjectName(controller.TargetFigure)}, " +
                    $"Camera={GetObjectName(controller.SourceCamera)}, " +
                    $"Possessing={controller.IsPossessing}, " +
                    $"Creature=" +
                    $"{GetObjectName(controller.PossessedCreature)}, " +
                    $"Duration={controller.CurrentPossessionDuration:F2}, " +
                    $"MovementBlocked={controller.MovementBlocked}, " +
                    $"LastExitReason={controller.LastExitReason}",
                    controller);
            }

            for (int i = 0; i < creatures.Length; i++)
            {
                InkCreatureRuntime creature = creatures[i];
                InkPossessionController owner = FindOwner(
                    controllers,
                    creature);
                Debug.Log(
                    "[M17 Diagnose Creature] " +
                    $"Path={GetHierarchyPath(creature.transform)}, " +
                    $"Possessed={owner != null}, " +
                    $"EyeActive={creature.HasGlyph(InkGlyphType.Eye)}, " +
                    $"FootActive={creature.HasGlyph(InkGlyphType.Foot)}, " +
                    $"Fixed={creature.IsFixed}, " +
                    $"Pinned={creature.IsPinned}, " +
                    $"AIComponentEnabled={creature.enabled}, " +
                    $"AIState={creature.CurrentState}, " +
                    $"WaterExposure={creature.WaterExposure:F2}",
                    creature);
            }
        }

        private static void ValidatePrerequisites()
        {
            if (AssetDatabase.LoadAssetAtPath<InkSystemConfig>(
                    InkSystemConfigPath) == null ||
                AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(
                    LekebacakDefinitionPath) == null)
            {
                throw new InvalidOperationException(
                    "M15 Ink Core assets were not found. Complete the " +
                    "working M15 setup first.");
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    CounterplayConfigPath) == null ||
                typeof(InkCreatureRuntime).GetMethod("TryDamageGlyph") == null)
            {
                throw new InvalidOperationException(
                    "M16 Ink Counterplay was not detected. Install and test " +
                    "the working M16 package before M17.");
            }
        }

        private static InkPossessionConfig GetOrCreateConfig()
        {
            InkPossessionConfig config =
                AssetDatabase.LoadAssetAtPath<InkPossessionConfig>(
                    ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<
                    InkPossessionConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            return config;
        }

        private static FigureMotor ResolveTargetFigure()
        {
            FigureMotor[] figures =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (figures.Length == 0)
            {
                throw new InvalidOperationException(
                    "No FigureMotor was found in the open gameplay scene.");
            }

            FigureMotor onlyActive = null;
            int activeCount = 0;

            for (int i = 0; i < figures.Length; i++)
            {
                FigureMotor figure = figures[i];

                if (string.Equals(
                        figure.gameObject.name,
                        "Figure_Player",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return figure;
                }

                if (figure.gameObject.activeInHierarchy)
                {
                    activeCount++;
                    onlyActive = figure;
                }
            }

            if (activeCount == 1)
            {
                return onlyActive;
            }

            throw new InvalidOperationException(
                "Multiple active FigureMotor objects were found. Name the " +
                "local test Figure 'Figure_Player'.");
        }

        private static Camera ResolveSourceCamera(FigureMotor targetFigure)
        {
            Camera childCamera =
                targetFigure.GetComponentInChildren<Camera>(true);

            if (childCamera != null)
            {
                return childCamera;
            }

            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Camera onlyActive = null;
            int activeCount = 0;

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];

                if (!camera.gameObject.activeInHierarchy || !camera.enabled)
                {
                    continue;
                }

                activeCount++;
                onlyActive = camera;
            }

            if (activeCount == 1)
            {
                return onlyActive;
            }

            throw new InvalidOperationException(
                "The local gameplay Camera could not be resolved safely. " +
                "Keep one active Camera or parent it under Figure_Player.");
        }

        private static InkPossessionController FindOwner(
            InkPossessionController[] controllers,
            InkCreatureRuntime creature)
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i].PossessedCreature == creature)
                {
                    return controllers[i];
                }
            }

            return null;
        }

        private static string GetObjectName(UnityEngine.Object target)
        {
            return target != null ? target.name : "None";
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized property was not found: {propertyName}");
            }

            property.objectReferenceValue = value;
        }

        private static void SetMask(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized property was not found: {propertyName}");
            }

            property.intValue = value;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "None";
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
