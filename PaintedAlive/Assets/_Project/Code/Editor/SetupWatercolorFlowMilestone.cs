using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Watercolor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools
{
    public static class SetupWatercolorFlowMilestone
    {
        private const string RootFolder = "Assets/_Project";

        private const string ConfigPath =
            RootFolder + "/Data/Paint/WatercolorFlowConfig.asset";

        private const string MaterialPath =
            RootFolder +
            "/Art/Materials/Watercolor/M_WatercolorFlow.mat";

        private const string PrefabPath =
            RootFolder +
            "/Prefabs/Paint/WatercolorFlowSurface.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/13 - Setup Watercolor Flow")]
        public static void Run()
        {
            try
            {
                EnsureFolders();

                FigureMotor[] motors =
                    UnityEngine.Object.FindObjectsByType<FigureMotor>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                if (motors.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Sahnede FigureMotor bulunamadı. " +
                        "Çalışan prototip sahnesini aç.");
                }

                WatercolorFlowConfig config = GetOrCreateConfig();
                Material material = GetOrCreateMaterial();
                WatercolorFlowSurface prefab =
                    GetOrCreateFlowPrefab(config, material);

                foreach (FigureMotor motor in motors)
                {
                    ConfigureFigure(motor, prefab);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 13] Suluboya akış sistemi " +
                    $"kuruldu. {motors.Length} Figür güncellendi. " +
                    "Play Mode: F8 ile önüne akış bırak.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/13 - Diagnose Watercolor Flow")]
        public static void Diagnose()
        {
            WatercolorFlowInteractor[] interactors =
                UnityEngine.Object.FindObjectsByType<
                    WatercolorFlowInteractor>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
            WatercolorFlowSurface[] surfaces =
                UnityEngine.Object.FindObjectsByType<
                    WatercolorFlowSurface>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            Debug.Log(
                "[M13 Diagnose] " +
                $"Interactors={interactors.Length}, " +
                $"SceneSurfaces={surfaces.Length}, " +
                $"ActiveSurfaces=" +
                $"{WatercolorFlowSurface.ActiveSurfaces.Count}, " +
                $"Playing={Application.isPlaying}");

            foreach (WatercolorFlowInteractor interactor
                     in interactors)
            {
                Debug.Log(
                    "[M13 Diagnose Figure] " +
                    $"Path={GetHierarchyPath(interactor.transform)}, " +
                    $"Enabled={interactor.enabled}, " +
                    $"InFlow={interactor.IsInWatercolor}, " +
                    $"Influence={interactor.CurrentInfluence:F2}, " +
                    $"Direction=" +
                    $"{interactor.CurrentFlowDirection}",
                    interactor);
            }

            foreach (WatercolorFlowSurface surface in surfaces)
            {
                Debug.Log(
                    "[M13 Diagnose Surface] " +
                    $"Path={GetHierarchyPath(surface.transform)}, " +
                    $"Enabled={surface.enabled}, " +
                    $"Amount={surface.AvailableAmount:F1}, " +
                    $"Nodes={surface.NodeCount}, " +
                    $"Length={surface.CurrentLength:F2}",
                    surface);
            }
        }

        private static void ConfigureFigure(
            FigureMotor motor,
            WatercolorFlowSurface prefab)
        {
            WatercolorFlowInteractor interactor =
                GetOrAddComponent<WatercolorFlowInteractor>(
                    motor.gameObject);
            WatercolorFlowDebugSpawner spawner =
                GetOrAddComponent<WatercolorFlowDebugSpawner>(
                    motor.gameObject);

            SetReference(interactor, "figureMotor", motor);
            SetReference(
                interactor,
                "clarityState",
                motor.GetComponent<FigureClarityState>());

            SetReference(spawner, "targetFigure", motor);
            SetReference(spawner, "flowSurfacePrefab", prefab);
            SetInteger(
                spawner,
                "surfaceMask",
                Physics.DefaultRaycastLayers);

            interactor.enabled = true;
            spawner.enabled = true;
            EditorUtility.SetDirty(interactor);
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(motor.gameObject.scene);
        }

        private static WatercolorFlowConfig GetOrCreateConfig()
        {
            WatercolorFlowConfig existing =
                AssetDatabase.LoadAssetAtPath<WatercolorFlowConfig>(
                    ConfigPath);

            if (existing != null)
            {
                return existing;
            }

            var config =
                ScriptableObject.CreateInstance<
                    WatercolorFlowConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static Material GetOrCreateMaterial()
        {
            Material existing =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find(
                "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Unlit shader bulunamadı.");
            }

            var material = new Material(shader)
            {
                name = "M_WatercolorFlow",
                renderQueue = 3000
            };
            var baseColor =
                new Color(0.08f, 0.58f, 0.95f, 0.64f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static WatercolorFlowSurface GetOrCreateFlowPrefab(
            WatercolorFlowConfig config,
            Material material)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            if (existing != null)
            {
                WatercolorFlowSurface existingSurface =
                    existing.GetComponent<WatercolorFlowSurface>();

                if (existingSurface == null)
                {
                    throw new InvalidOperationException(
                        "WatercolorFlowSurface prefab gerekli " +
                        "bileşeni içermiyor.");
                }

                SetReference(existingSurface, "config", config);
                return existingSurface;
            }

            var flowObject = new GameObject(
                "WatercolorFlowSurface");
            MeshFilter filter =
                flowObject.AddComponent<MeshFilter>();
            MeshRenderer renderer =
                flowObject.AddComponent<MeshRenderer>();
            MeshCollider collider =
                flowObject.AddComponent<MeshCollider>();
            WatercolorFlowSurface surface =
                flowObject.AddComponent<WatercolorFlowSurface>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            collider.convex = false;

            SetReference(surface, "config", config);
            SetReference(surface, "meshFilter", filter);
            SetReference(surface, "meshRenderer", renderer);
            SetReference(surface, "meshCollider", collider);
            SetInteger(
                surface,
                "surfaceMask",
                Physics.DefaultRaycastLayers);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                flowObject,
                PrefabPath);
            UnityEngine.Object.DestroyImmediate(flowObject);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "WatercolorFlowSurface prefab oluşturulamadı.");
            }

            return prefab.GetComponent<WatercolorFlowSurface>();
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
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder + "/Art", "Materials");
            EnsureFolder(
                RootFolder + "/Art/Materials",
                "Watercolor");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder + "/Data", "Paint");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "Paint");
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
