using System;
using System.Collections.Generic;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    public static class SetupStainWatercolorFlowMilestone
    {
        private const string ConfigFolder =
            "Assets/_Project/Data/Figures/Stain";

        private const string ConfigPath =
            ConfigFolder + "/StainWatercolorFlowConfig.asset";

        [MenuItem("Tools/Painted Alive/Milestones/34 - Setup Stain Watercolor Flow")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("M34 Setup yalnız Play Mode kapalıyken çalıştırılabilir.");
                return;
            }

            try
            {
                FigureContext context = ResolveFigureContext();
                StainWatercolorFlowConfig config = GetOrCreateConfig();

                StainWatercolorFlowController controller =
                    GetOrAddComponent<StainWatercolorFlowController>(context.Root);

                StainWatercolorFlowTestSpawner testSpawner =
                    GetOrAddComponent<StainWatercolorFlowTestSpawner>(context.Root);

                controller.Configure(
                    config,
                    context.Clarity,
                    context.Input,
                    context.Motor,
                    context.CharacterController,
                    context.SurfaceCrawl,
                    context.FigureCamera,
                    context.ExclusiveBehaviours);

                testSpawner.Configure(context.WatercolorDebugSpawner);

                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(testSpawner);
                EditorUtility.SetDirty(context.Root);

                if (context.Root.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        context.Root.scene);
                }

                Selection.activeGameObject = context.Root;

                Debug.Log(
                    "[Milestone 34] Leke Suluboya Akış Taşınması kuruldu. " +
                    "M25-M33 Setup'ları yeniden çalıştırılmadı.",
                    context.Root);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/34 - Diagnose Stain Watercolor Flow")]
        public static void Diagnose()
        {
            StainWatercolorFlowController[] controllers =
                UnityEngine.Object.FindObjectsByType<StainWatercolorFlowController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            WatercolorFlowSourceAdapter[] sources =
                UnityEngine.Object.FindObjectsByType<WatercolorFlowSourceAdapter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (controllers.Length == 0)
            {
                Debug.LogWarning(
                    "[Milestone 34 Diagnose] Controller bulunamadı. Önce Setup çalıştır.");
                return;
            }

            for (int i = 0; i < controllers.Length; i++)
            {
                StainWatercolorFlowController controller = controllers[i];
                FigureClarityState clarity =
                    controller.GetComponent<FigureClarityState>();
                CharacterController characterController =
                    controller.GetComponent<CharacterController>();

                Debug.Log(
                    "[Milestone 34 Diagnose] " +
                    $"Figure={controller.name}, " +
                    $"Clarity={(clarity != null ? clarity.CurrentLevel.ToString() : "Missing")}, " +
                    $"Riding={controller.IsRidingFlow}, " +
                    $"Source={(controller.CurrentSource != null ? controller.CurrentSource.name : "None")}, " +
                    $"Velocity={controller.CurrentVelocity}, " +
                    $"CharacterControllerEnabled={(characterController != null && characterController.enabled)}, " +
                    $"DiscoveredFlowSources={sources.Length}",
                    controller);
            }
        }

        private static FigureContext ResolveFigureContext()
        {
            FigureMotor[] motors =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            FigureContext best = default;
            int bestScore = int.MinValue;

            for (int i = 0; i < motors.Length; i++)
            {
                FigureMotor motor = motors[i];
                if (motor == null || !motor.gameObject.scene.IsValid())
                {
                    continue;
                }

                GameObject root = motor.gameObject;
                FigureClarityState clarity = root.GetComponent<FigureClarityState>();
                FigureInputReader input = root.GetComponent<FigureInputReader>();
                CharacterController characterController =
                    root.GetComponent<CharacterController>();
                Behaviour crawl = FindSurfaceCrawlController(root);

                int score = 0;
                score += clarity != null ? 100 : 0;
                score += input != null ? 40 : 0;
                score += characterController != null ? 60 : 0;
                score += crawl != null ? 140 : 0;
                score += root.activeInHierarchy ? 10 : 0;
                score += HasComponentName(root, "InkStainSabotageController") ? 50 : 0;

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                best = new FigureContext
                {
                    Root = root,
                    Motor = motor,
                    Clarity = clarity,
                    Input = input,
                    CharacterController = characterController,
                    SurfaceCrawl = crawl,
                    FigureCamera = ResolveFigureCamera(root),
                    WatercolorDebugSpawner = FindBehaviour(root, "WatercolorFlowDebugSpawner"),
                    ExclusiveBehaviours = FindExclusiveBehaviours(root)
                };
            }

            if (best.Root == null ||
                best.Clarity == null ||
                best.Input == null ||
                best.CharacterController == null ||
                best.SurfaceCrawl == null)
            {
                throw new InvalidOperationException(
                    "M34, aynı Figür kökünde FigureMotor, FigureClarityState, " +
                    "FigureInputReader, CharacterController ve M28 surface crawl controller bekliyor. " +
                    $"Figure={(best.Root != null)}, " +
                    $"Clarity={(best.Clarity != null)}, " +
                    $"Input={(best.Input != null)}, " +
                    $"CharacterController={(best.CharacterController != null)}, " +
                    $"M28Crawl={(best.SurfaceCrawl != null)}.");
            }

            return best;
        }

        private static Behaviour FindSurfaceCrawlController(GameObject root)
        {
            Behaviour[] behaviours = root.GetComponents<Behaviour>();
            Behaviour best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                int score = 0;

                if (typeName.Equals("StainSurfaceCrawlController", StringComparison.OrdinalIgnoreCase))
                {
                    score = 100;
                }
                else if (typeName.IndexOf("Stain", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         typeName.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         typeName.IndexOf("Crawl", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = 80;
                }
                else if (typeName.IndexOf("Stain", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         typeName.IndexOf("Crawl", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = 40;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = behaviour;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static Camera ResolveFigureCamera(GameObject root)
        {
            Camera childCamera = root.GetComponentInChildren<Camera>(true);
            if (childCamera != null)
            {
                return childCamera;
            }

            MonoBehaviour[] allBehaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < allBehaviours.Length; i++)
            {
                MonoBehaviour behaviour = allBehaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName.IndexOf("RoleAuthority", StringComparison.OrdinalIgnoreCase) < 0 &&
                    typeName.IndexOf("Hijack", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                Camera camera = TryReadCameraReference(behaviour);
                if (camera != null)
                {
                    return camera;
                }
            }

            return Camera.main;
        }

        private static Camera TryReadCameraReference(MonoBehaviour behaviour)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            Type type = behaviour.GetType();
            string[] preferredNames =
            {
                "ActiveRoleCamera",
                "figureCamera",
                "FigureCamera",
                "playerCamera"
            };

            for (int i = 0; i < preferredNames.Length; i++)
            {
                System.Reflection.PropertyInfo property =
                    type.GetProperty(preferredNames[i], flags);

                if (property != null &&
                    typeof(Camera).IsAssignableFrom(property.PropertyType) &&
                    property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return property.GetValue(behaviour) as Camera;
                    }
                    catch (Exception)
                    {
                        // Continue with fields/fallbacks.
                    }
                }

                System.Reflection.FieldInfo field =
                    type.GetField(preferredNames[i], flags);

                if (field != null && typeof(Camera).IsAssignableFrom(field.FieldType))
                {
                    try
                    {
                        return field.GetValue(behaviour) as Camera;
                    }
                    catch (Exception)
                    {
                        // Continue with other candidates.
                    }
                }
            }

            return null;
        }

        private static Behaviour[] FindExclusiveBehaviours(GameObject root)
        {
            Behaviour[] behaviours = root.GetComponents<Behaviour>();
            var result = new List<Behaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour is StainWatercolorFlowController)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (ContainsAny(
                        typeName,
                        "Hijack",
                        "SpongeCarry",
                        "CrackTraversal",
                        "GripImprint"))
                {
                    result.Add(behaviour);
                }
            }

            return result.ToArray();
        }

        private static MonoBehaviour FindBehaviour(
            GameObject root,
            string exactTypeName)
        {
            MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null &&
                    behaviour.GetType().Name.Equals(
                        exactTypeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static bool HasComponentName(GameObject root, string exactTypeName)
        {
            return FindBehaviour(root, exactTypeName) != null;
        }

        private static StainWatercolorFlowConfig GetOrCreateConfig()
        {
            StainWatercolorFlowConfig config =
                AssetDatabase.LoadAssetAtPath<StainWatercolorFlowConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            EnsureFolder(ConfigFolder);
            config = ScriptableObject.CreateInstance<StainWatercolorFlowConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
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

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private struct FigureContext
        {
            public GameObject Root;
            public FigureMotor Motor;
            public FigureClarityState Clarity;
            public FigureInputReader Input;
            public CharacterController CharacterController;
            public Behaviour SurfaceCrawl;
            public Camera FigureCamera;
            public MonoBehaviour WatercolorDebugSpawner;
            public Behaviour[] ExclusiveBehaviours;
        }
    }
}
