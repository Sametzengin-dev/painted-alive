using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint;
using PaintedAlive.Paint.Watercolor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.EditorTools
{
    public static class SetupWatercolorReactionMilestone
    {
        private const string RootFolder = "Assets/_Project";

        private const string ConfigPath =
            RootFolder +
            "/Data/Paint/WatercolorReactionConfig.asset";

        private const string ManagerName =
            "[PaintedAlive] Watercolor Reaction Manager";

        [MenuItem(
            "Tools/Painted Alive/Milestones/14 - Setup Watercolor Reactions")]
        public static void Run()
        {
            try
            {
                EnsureFolders();

                if (AssetDatabase.FindAssets(
                        "t:WatercolorFlowConfig").Length == 0)
                {
                    throw new InvalidOperationException(
                        "M13 Watercolor Flow kurulumu bulunamadı. " +
                        "Önce M13 Setup menüsünü çalıştır.");
                }

                FixativeSprayController[] fixatives =
                    UnityEngine.Object.FindObjectsByType<
                        FixativeSprayController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                if (fixatives.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Sahnede FixativeSprayController bulunamadı. " +
                        "Çalışan Figür prototip sahnesini aç.");
                }

                WatercolorReactionConfig config =
                    GetOrCreateConfig();
                WatercolorReactionCoordinator coordinator =
                    GetOrCreateCoordinator(config);

                foreach (FixativeSprayController fixative
                         in fixatives)
                {
                    ConfigureFixativeBridge(fixative);
                }

                EditorUtility.SetDirty(coordinator);
                EditorSceneManager.MarkSceneDirty(
                    coordinator.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 14] Suluboya reaksiyonları " +
                    $"kuruldu. {fixatives.Length} Sabitleyici " +
                    "köprüsü bağlandı. Test: F8 ile akış oluştur; " +
                    "ıslak stroke üzerinden geçir veya " +
                    "Sabitleyiciyle akışı hedefle.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/14 - Diagnose Watercolor Reactions")]
        public static void Diagnose()
        {
            WatercolorReactionCoordinator[] coordinators =
                UnityEngine.Object.FindObjectsByType<
                    WatercolorReactionCoordinator>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
            WatercolorFixativeBridge[] bridges =
                UnityEngine.Object.FindObjectsByType<
                    WatercolorFixativeBridge>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
            WatercolorFixedState[] fixedStates =
                UnityEngine.Object.FindObjectsByType<
                    WatercolorFixedState>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
            WatercolorOilReaction[] oilReactions =
                UnityEngine.Object.FindObjectsByType<
                    WatercolorOilReaction>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            int frozenCount = 0;

            foreach (WatercolorFixedState state in fixedStates)
            {
                if (state.IsFrozen)
                {
                    frozenCount++;
                }
            }

            Debug.Log(
                "[M14 Diagnose] " +
                $"Coordinators={coordinators.Length}, " +
                $"Bridges={bridges.Length}, " +
                $"OilReactions={oilReactions.Length}, " +
                $"FrozenSurfaces={frozenCount}, " +
                $"RegisteredReactions=" +
                $"{WatercolorReactionRegistry.ActiveReactions.Count}, " +
                $"Playing={Application.isPlaying}");

            foreach (WatercolorReactionCoordinator coordinator
                     in coordinators)
            {
                Debug.Log(
                    "[M14 Diagnose Coordinator] " +
                    $"Path={GetHierarchyPath(coordinator.transform)}, " +
                    $"Enabled={coordinator.enabled}, " +
                    $"DiscoveredOil=" +
                    $"{coordinator.DiscoveredOilStrokeCount}, " +
                    $"Active={coordinator.ActiveReactionCount}, " +
                    $"Applications=" +
                    $"{coordinator.AppliedReactionCount}",
                    coordinator);
            }

            foreach (WatercolorFixativeBridge bridge in bridges)
            {
                Debug.Log(
                    "[M14 Diagnose Fixative] " +
                    $"Path={GetHierarchyPath(bridge.transform)}, " +
                    $"Enabled={bridge.enabled}, " +
                    $"FixativeActive={bridge.FixativeIsActive}, " +
                    $"InputHeld={bridge.InputIsHeld}, " +
                    $"ActiveFlows={bridge.ActiveSurfaceCount}, " +
                    $"Target=" +
                    $"{(bridge.CurrentTarget != null ? bridge.CurrentTarget.name : "Yok")}, " +
                    $"Distance={bridge.CurrentTargetDistance:F2}, " +
                    $"Status={bridge.DetectionStatus}, " +
                    $"Frozen={bridge.FrozenSurfaceCount}",
                    bridge);
            }

            foreach (WatercolorOilReaction oil in oilReactions)
            {
                Debug.Log(
                    "[M14 Diagnose Oil] " +
                    $"Path={GetHierarchyPath(oil.transform)}, " +
                    $"Displacement={oil.CumulativeDisplacement:F2}, " +
                    $"Height={oil.CurrentHeightRatio:F2}, " +
                    $"Reactions={oil.ReactionCount}",
                    oil);
            }
        }

        private static WatercolorReactionConfig GetOrCreateConfig()
        {
            WatercolorReactionConfig existing =
                AssetDatabase.LoadAssetAtPath<
                    WatercolorReactionConfig>(ConfigPath);

            if (existing != null)
            {
                return existing;
            }

            var config = ScriptableObject.CreateInstance<
                WatercolorReactionConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static WatercolorReactionCoordinator
            GetOrCreateCoordinator(WatercolorReactionConfig config)
        {
            WatercolorReactionCoordinator[] existing =
                UnityEngine.Object.FindObjectsByType<
                    WatercolorReactionCoordinator>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
            WatercolorReactionCoordinator coordinator;

            if (existing.Length > 0)
            {
                coordinator = existing[0];
            }
            else
            {
                var managerObject = new GameObject(ManagerName);
                Undo.RegisterCreatedObjectUndo(
                    managerObject,
                    "Create Watercolor Reaction Manager");
                coordinator = Undo.AddComponent<
                    WatercolorReactionCoordinator>(managerObject);
            }

            SetReference(coordinator, "config", config);
            coordinator.enabled = true;
            return coordinator;
        }

        private static void ConfigureFixativeBridge(
            FixativeSprayController fixative)
        {
            GameObject host = fixative.gameObject;
            WatercolorFixativeBridge bridge =
                GetOrAddComponent<WatercolorFixativeBridge>(host);

            Camera camera =
                GetReference<Camera>(fixative, "outputCamera");
            Transform toolOrigin =
                GetReference<Transform>(fixative, "toolOrigin");
            InputActionReference useAction =
                GetReference<InputActionReference>(
                    fixative,
                    "useToolAction");
            FigureClarityState clarity =
                GetReference<FigureClarityState>(
                    fixative,
                    "clarityState");
            FixativeSprayConfig fixativeConfig =
                GetReference<FixativeSprayConfig>(
                    fixative,
                    "config");
            FixativeSprayFeedback feedback =
                GetReference<FixativeSprayFeedback>(
                    fixative,
                    "feedback");

            SetReference(
                bridge,
                "fixativeController",
                fixative);
            SetReference(
                bridge,
                "outputCamera",
                camera != null ? camera : Camera.main);
            SetReference(
                bridge,
                "toolOrigin",
                toolOrigin != null ? toolOrigin : host.transform);
            SetReference(bridge, "useToolAction", useAction);
            SetReference(bridge, "clarityState", clarity);
            SetReference(bridge, "config", fixativeConfig);
            SetReference(bridge, "feedback", feedback);
            SetInteger(
                bridge,
                "watercolorMask",
                Physics.DefaultRaycastLayers);

            bridge.enabled = true;
            EditorUtility.SetDirty(bridge);
            EditorSceneManager.MarkSceneDirty(host.scene);
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

        private static T GetReference<T>(
            UnityEngine.Object target,
            string propertyName)
            where T : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            return property.objectReferenceValue as T;
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder + "/Data", "Paint");
        }

        private static void EnsureFolder(
            string parent,
            string child)
        {
            string path = $"{parent}/{child}";

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            Transform parent = target.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
