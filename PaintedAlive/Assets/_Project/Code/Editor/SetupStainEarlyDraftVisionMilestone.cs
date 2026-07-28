#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.DraftVision;
using PaintedAlive.Painters.DraftVision;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupStainEarlyDraftVisionMilestone
    {
        private const string ConfigFolder =
            "Assets/_Project/Data/Figures/Stain";

        private const string ConfigPath =
            ConfigFolder + "/StainEarlyDraftVisionConfig.asset";

        private const string MaterialFolder =
            "Assets/_Project/Materials/Figures/Stain";

        private const string MaterialPath =
            MaterialFolder + "/MAT_Stain_EarlyDraftVision.mat";

        [MenuItem("Tools/Painted Alive/Milestones/35 - Setup Stain Early Draft Vision")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("M35 Setup yalnız Play Mode kapalıyken çalıştırılabilir.");
                return;
            }

            try
            {
                FigureContext figure = ResolveFigureContext();
                StainEarlyDraftVisionConfig config = GetOrCreateConfig();
                Material material = GetOrCreateMaterial();

                Undo.RecordObject(config, "Configure M35 Stain Draft Vision");
                config.SetDraftMaterial(material);
                EditorUtility.SetDirty(config);

                StainEarlyDraftVisionController vision =
                    GetOrAddComponent<StainEarlyDraftVisionController>(figure.Root);

                StainEarlyDraftVisionDebugEmitter debugEmitter =
                    GetOrAddComponent<StainEarlyDraftVisionDebugEmitter>(figure.Root);

                vision.Configure(
                    config,
                    figure.Clarity,
                    figure.FigureCamera,
                    figure.BlockedStates);

                debugEmitter.Configure(
                    config,
                    figure.Clarity,
                    figure.FigureCamera);

                MonoBehaviour brushController = FindPainterBrushController();
                PainterDraftTelegraphRelay relay = null;
                if (brushController != null)
                {
                    relay = GetOrAddComponent<PainterDraftTelegraphRelay>(
                        brushController.gameObject);
                    relay.Configure(brushController, config);
                    EditorUtility.SetDirty(relay);
                }
                else
                {
                    Debug.LogWarning(
                        "[Milestone 35] PainterBrushController bulunamadı. " +
                        "F10 debug testi çalışır; gerçek Painter preview relay'i için " +
                        "PainterBrushController bulunan sahnede Setup'ı tekrar yalnız M35 için çalıştır.");
                }

                AppendToFigureRoleBehaviours(vision, debugEmitter);

                EditorUtility.SetDirty(vision);
                EditorUtility.SetDirty(debugEmitter);
                EditorUtility.SetDirty(figure.Root);

                if (figure.Root.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        figure.Root.scene);
                }

                if (relay != null && relay.gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        relay.gameObject.scene);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeGameObject = figure.Root;

                Debug.Log(
                    "[Milestone 35] Leke Erken Ressam Taslak Görüşü kuruldu. " +
                    "M25-M34 Setup'ları yeniden çalıştırılmadı. " +
                    "Play Mode'da tam Leke iken F10 ile test et.",
                    figure.Root);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/35 - Diagnose Stain Early Draft Vision")]
        public static void Diagnose()
        {
            StainEarlyDraftVisionController[] visions =
                UnityEngine.Object.FindObjectsByType<StainEarlyDraftVisionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            PainterDraftTelegraphRelay[] relays =
                UnityEngine.Object.FindObjectsByType<PainterDraftTelegraphRelay>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (visions.Length == 0)
            {
                Debug.LogWarning(
                    "[Milestone 35 Diagnose] Vision controller bulunamadı. Önce M35 Setup çalıştır.");
                return;
            }

            for (int i = 0; i < visions.Length; i++)
            {
                StainEarlyDraftVisionController vision = visions[i];
                FigureClarityState clarity = vision.GetComponent<FigureClarityState>();

                Debug.Log(
                    "[Milestone 35 Diagnose] " +
                    $"Figure={vision.name}, " +
                    $"Clarity={(clarity != null ? clarity.CurrentLevel.ToString() : "Missing")}, " +
                    $"VisibleDrafts={vision.VisibleDraftCount}, " +
                    $"ReceivedDrafts={vision.ReceivedDraftCount}, " +
                    $"Relays={relays.Length}",
                    vision);
            }

            for (int i = 0; i < relays.Length; i++)
            {
                PainterDraftTelegraphRelay relay = relays[i];
                Debug.Log(
                    "[Milestone 35 Diagnose] " +
                    $"Relay={relay.name}, " +
                    $"Active={relay.IsRelaying}, " +
                    $"Points={relay.RelayedPointCount}",
                    relay);
            }
        }

        private static FigureContext ResolveFigureContext()
        {
            FigureClarityState[] clarityStates =
                UnityEngine.Object.FindObjectsByType<FigureClarityState>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            FigureContext best = default;
            int bestScore = int.MinValue;

            for (int i = 0; i < clarityStates.Length; i++)
            {
                FigureClarityState clarity = clarityStates[i];
                if (clarity == null || !clarity.gameObject.scene.IsValid())
                {
                    continue;
                }

                GameObject root = clarity.gameObject;
                int score = 100;
                score += root.GetComponent<FigureMotor>() != null ? 60 : 0;
                score += root.GetComponent<CharacterController>() != null ? 40 : 0;
                score += HasComponentName(root, "StainSurfaceCrawlController") ? 80 : 0;
                score += HasComponentName(root, "StainWatercolorFlowController") ? 50 : 0;
                score += root.activeInHierarchy ? 10 : 0;

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                best = new FigureContext
                {
                    Root = root,
                    Clarity = clarity,
                    FigureCamera = ResolveFigureCamera(root),
                    BlockedStates = FindBlockedStates(root)
                };
            }

            if (best.Root == null || best.Clarity == null)
            {
                throw new InvalidOperationException(
                    "M35 aynı Figür kökünde FigureClarityState bekliyor. " +
                    "M25-M34'ün kurulu olduğu ana oynanış sahnesini açıp yalnız M35 Setup çalıştır.");
            }

            return best;
        }

        private static Behaviour[] FindBlockedStates(GameObject root)
        {
            Behaviour[] behaviours = root.GetComponents<Behaviour>();
            var result = new List<Behaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    behaviour is StainEarlyDraftVisionController ||
                    behaviour is StainEarlyDraftVisionDebugEmitter)
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

        private static MonoBehaviour FindPainterBrushController()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            MonoBehaviour best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !behaviour.gameObject.scene.IsValid())
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (!typeName.Equals(
                        "PainterBrushController",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int score = 100;
                score += behaviour.gameObject.name.IndexOf(
                    "PaintRuntime",
                    StringComparison.OrdinalIgnoreCase) >= 0 ? 30 : 0;
                score += behaviour.gameObject.activeInHierarchy ? 10 : 0;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = behaviour;
                }
            }

            return best;
        }

        private static Camera ResolveFigureCamera(GameObject root)
        {
            Camera child = root.GetComponentInChildren<Camera>(true);
            if (child != null)
            {
                return child;
            }

            Camera main = Camera.main;
            if (main != null)
            {
                return main;
            }

            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null &&
                    camera.name.IndexOf("Figure", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return camera;
                }
            }

            return cameras.Length > 0 ? cameras[0] : null;
        }

        private static void AppendToFigureRoleBehaviours(
            Behaviour vision,
            Behaviour debugEmitter)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName.IndexOf("RoleSwitcher", StringComparison.OrdinalIgnoreCase) < 0 &&
                    typeName.IndexOf("RoleAuthority", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(behaviour);
                SerializedProperty array = serialized.FindProperty("figureBehaviours");
                if (array == null || !array.isArray)
                {
                    continue;
                }

                bool changed = false;
                changed |= AppendUnique(array, vision);
                changed |= AppendUnique(array, debugEmitter);

                if (changed)
                {
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(behaviour);
                }

                return;
            }
        }

        private static bool AppendUnique(
            SerializedProperty array,
            UnityEngine.Object value)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return false;
                }
            }

            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            array.GetArrayElementAtIndex(index).objectReferenceValue = value;
            return true;
        }

        private static StainEarlyDraftVisionConfig GetOrCreateConfig()
        {
            StainEarlyDraftVisionConfig config =
                AssetDatabase.LoadAssetAtPath<StainEarlyDraftVisionConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            EnsureFolder(ConfigFolder);
            config = ScriptableObject.CreateInstance<StainEarlyDraftVisionConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static Material GetOrCreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            EnsureFolder(MaterialFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "M35 taslak materyali için URP/Unlit veya Sprites/Default shader bulunamadı.");
            }

            material = new Material(shader)
            {
                name = "MAT_Stain_EarlyDraftVision",
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
            };

            Color color = new Color(0.12f, 0.95f, 0.88f, 0.46f);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static bool HasComponentName(GameObject root, string exactTypeName)
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
                    return true;
                }
            }

            return false;
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
            public FigureClarityState Clarity;
            public Camera FigureCamera;
            public Behaviour[] BlockedStates;
        }
    }
}
#endif
