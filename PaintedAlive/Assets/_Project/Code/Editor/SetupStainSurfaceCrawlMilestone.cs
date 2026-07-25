using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainMovement;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Paint.Ink.StainSabotage;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupStainSurfaceCrawlMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Figures/" +
            "M28_StainSurfaceCrawlConfig.asset";
        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Ink";
        private const string MaterialPath =
            MaterialFolder + "/M_PlayerStain_M28.mat";
        private const string StainVisualName =
            "M28_PlayerStainVisual";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "28 - Setup Stain Surface Crawl")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M28 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                Prerequisites prerequisites =
                    ResolvePrerequisites();
                StainSurfaceCrawlConfig config =
                    GetOrCreateConfig();
                Material stainMaterial =
                    GetOrCreateStainMaterial();
                Transform stainVisual =
                    GetOrCreateStainVisual(
                        prerequisites.Figure.transform,
                        stainMaterial);
                StainSurfaceCrawlController controller =
                    GetOrCreateController(
                        prerequisites,
                        config,
                        stainVisual);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(stainMaterial);
                EditorUtility.SetDirty(stainVisual);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.Figure.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M28 Setup] Tamamlandı. Tam Leke formunda " +
                    "WASD ile zemin ve dik duvarlarda sürün; " +
                    "Space ile yüzeyi bırak.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "28 - Diagnose Stain Surface Crawl")]
        public static void Diagnose()
        {
            StainSurfaceCrawlController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    StainSurfaceCrawlController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M28 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                StainSurfaceCrawlController controller =
                    controllers[i];
                Debug.Log(
                    "[M28 Diagnose Controller] " +
                    $"Path={GetPath(controller.transform)}, " +
                    $"Crawling={controller.IsCrawling}, " +
                    $"HasSurface={controller.HasSurface}, " +
                    $"Surface={controller.SurfaceType}, " +
                    $"Visual={controller.HasStainVisual}, " +
                    $"Normal={controller.SurfaceNormal}, " +
                    $"Result={controller.LastResult}",
                    controller);
            }
        }

        private static Prerequisites ResolvePrerequisites()
        {
            InkStainSabotageController[] sabotageControllers =
                UnityEngine.Object.FindObjectsByType<
                    InkStainSabotageController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (sabotageControllers.Length != 1 ||
                authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M28 tek M25 SabotageController ve tek " +
                    "RoleAuthority bekliyor. " +
                    $"Sabotage={sabotageControllers.Length}, " +
                    $"Authorities={authorities.Length}.");
            }

            GameObject figureObject =
                sabotageControllers[0].gameObject;
            FigureMotor figure =
                figureObject.GetComponent<FigureMotor>();
            FigureClarityState clarity =
                figureObject.GetComponent<FigureClarityState>();
            CharacterController characterController =
                figureObject.GetComponent<CharacterController>();
            InkStainCreatureHijackController hijack =
                figureObject.GetComponent<
                    InkStainCreatureHijackController>();
            Camera camera =
                ResolveFigureCamera(
                    figureObject,
                    authorities[0],
                    hijack);

            if (figure == null ||
                clarity == null ||
                characterController == null ||
                camera == null ||
                hijack == null)
            {
                throw new InvalidOperationException(
                    "M28, M25 ile aynı Figür kökünde FigureMotor, " +
                    "FigureClarityState, CharacterController, Camera " +
                    "ve M26 HijackController bekliyor. " +
                    $"Figure={(figure != null)}, " +
                    $"Clarity={(clarity != null)}, " +
                    $"CharacterController={(characterController != null)}, " +
                    $"Camera={(camera != null)}, " +
                    $"Hijack={(hijack != null)}.");
            }

            return new Prerequisites(
                figure,
                clarity,
                characterController,
                camera,
                authorities[0],
                hijack);
        }

        private static Camera ResolveFigureCamera(
            GameObject figureObject,
            InkPainterRoleAuthority authority,
            InkStainCreatureHijackController hijack)
        {
            Camera camera =
                figureObject.GetComponentInChildren<Camera>(true);

            if (camera != null)
            {
                return camera;
            }

            if (authority != null)
            {
                SerializedObject serializedAuthority =
                    new SerializedObject(authority);
                SerializedProperty figureCameraProperty =
                    serializedAuthority.FindProperty("figureCamera");
                camera = figureCameraProperty != null
                    ? figureCameraProperty.objectReferenceValue as Camera
                    : null;
            }

            if (camera != null)
            {
                return camera;
            }

            if (hijack != null)
            {
                SerializedObject serializedHijack =
                    new SerializedObject(hijack);
                SerializedProperty figureCameraProperty =
                    serializedHijack.FindProperty("figureCamera");
                camera = figureCameraProperty != null
                    ? figureCameraProperty.objectReferenceValue as Camera
                    : null;
            }

            return camera;
        }

        private static StainSurfaceCrawlConfig
            GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Figures");

            StainSurfaceCrawlConfig config =
                AssetDatabase.LoadAssetAtPath<
                    StainSurfaceCrawlConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    StainSurfaceCrawlConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static StainSurfaceCrawlController
            GetOrCreateController(
                Prerequisites prerequisites,
                StainSurfaceCrawlConfig config,
                Transform stainVisual)
        {
            StainSurfaceCrawlController[] existing =
                UnityEngine.Object.FindObjectsByType<
                    StainSurfaceCrawlController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (existing.Length > 1)
            {
                throw new InvalidOperationException(
                    "Sahnede birden fazla M28 CrawlController var. " +
                    "Kopyaları temizleyip Setup'ı yeniden çalıştır.");
            }

            StainSurfaceCrawlController controller =
                existing.Length == 1
                    ? existing[0]
                    : Undo.AddComponent<
                        StainSurfaceCrawlController>(
                        prerequisites.Figure.gameObject);

            if (controller.gameObject !=
                prerequisites.Figure.gameObject)
            {
                throw new InvalidOperationException(
                    "Mevcut M28 CrawlController doğru Figür " +
                    "kökünde değil.");
            }

            controller.Configure(
                prerequisites.Clarity,
                prerequisites.Figure,
                prerequisites.CharacterController,
                prerequisites.Camera,
                prerequisites.RoleAuthority,
                prerequisites.Hijack,
                config,
                stainVisual);
            return controller;
        }

        private static Material GetOrCreateStainMaterial()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");
            EnsureFolder(MaterialFolder);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    MaterialPath);
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "M28 Leke görseli için uyumlu shader bulunamadı.");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "M_PlayerStain_M28"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader == null)
            {
                material.shader = shader;
            }

            Color stainColor =
                new Color(0.035f, 0.12f, 0.15f, 1f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", stainColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", stainColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.82f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0.05f);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    new Color(0.01f, 0.08f, 0.10f, 1f));
                material.EnableKeyword("_EMISSION");
            }

            return material;
        }

        private static Transform GetOrCreateStainVisual(
            Transform figureRoot,
            Material material)
        {
            Transform visual =
                figureRoot.Find(StainVisualName);

            if (visual == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);
                created.name = StainVisualName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M28 player Stain visual");
                created.transform.SetParent(
                    figureRoot,
                    false);
                visual = created.transform;
            }

            Collider visualCollider =
                visual.GetComponent<Collider>();

            if (visualCollider != null)
            {
                Undo.DestroyObjectImmediate(visualCollider);
            }

            visual.localPosition =
                new Vector3(0f, 0.075f, 0f);
            visual.localRotation =
                Quaternion.identity;
            visual.localScale =
                new Vector3(0.92f, 0.11f, 0.78f);

            Renderer renderer =
                visual.GetComponent<Renderer>();

            if (renderer == null)
            {
                throw new InvalidOperationException(
                    "M28 Leke görselinde Renderer bulunamadı.");
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.enabled = false;
            return visual;
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

        private static string GetPath(Transform target)
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

        private readonly struct Prerequisites
        {
            public Prerequisites(
                FigureMotor figure,
                FigureClarityState clarity,
                CharacterController characterController,
                Camera camera,
                InkPainterRoleAuthority roleAuthority,
                InkStainCreatureHijackController hijack)
            {
                Figure = figure;
                Clarity = clarity;
                CharacterController = characterController;
                Camera = camera;
                RoleAuthority = roleAuthority;
                Hijack = hijack;
            }

            public FigureMotor Figure { get; }
            public FigureClarityState Clarity { get; }
            public CharacterController CharacterController { get; }
            public Camera Camera { get; }
            public InkPainterRoleAuthority RoleAuthority { get; }
            public InkStainCreatureHijackController Hijack { get; }
        }
    }
}
