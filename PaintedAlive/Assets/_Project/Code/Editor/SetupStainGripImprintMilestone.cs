using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainMovement;
using PaintedAlive.Figures.StainSupport;
using PaintedAlive.Figures.StainTraversal;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools
{
    public static class SetupStainGripImprintMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Figures/" +
            "M32_StainGripImprintConfig.asset";
        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Ink";
        private const string CleanMaterialPath =
            MaterialFolder + "/M32_CleanCanvas.mat";
        private const string MarkMaterialPath =
            MaterialFolder + "/M32_GripMark.mat";
        private const string SupportMaterialPath =
            MaterialFolder + "/M32_GripSupport.mat";
        private const string RootName =
            "M32_StainGripPrototype";
        private const string FloorName =
            "M32_CleanGripFloor";
        private const string WallName =
            "M32_CleanGripWall";
        private const string ImprintVisualName =
            "M32_GripImprintVisual";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "32 - Setup Stain Grip Imprint")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M32 Setup Play Mode dışında " +
                        "çalıştırılmalıdır.");
                }

                Prerequisites prerequisites =
                    ResolvePrerequisites();
                Material cleanMaterial =
                    GetOrCreateMaterial(
                        CleanMaterialPath,
                        "M32_CleanCanvas",
                        new Color(
                            0.78f,
                            0.75f,
                            0.67f,
                            1f),
                        0.22f,
                        0f);
                Material markMaterial =
                    GetOrCreateMaterial(
                        MarkMaterialPath,
                        "M32_GripMark",
                        new Color(
                            0.02f,
                            0.31f,
                            0.33f,
                            1f),
                        0.88f,
                        0.08f);
                Material supportMaterial =
                    GetOrCreateMaterial(
                        SupportMaterialPath,
                        "M32_GripSupport",
                        new Color(
                            0.04f,
                            0.52f,
                            0.49f,
                            1f),
                        0.7f,
                        0.12f);
                StainGripImprintConfig config =
                    GetOrCreateConfig();
                config.ConfigureMaterials(
                    markMaterial,
                    supportMaterial);

                PrototypeParts prototype =
                    GetOrCreatePrototype(
                        prerequisites.Figure.transform,
                        cleanMaterial);
                Transform imprintVisual =
                    GetOrCreateImprintVisual(
                        prerequisites.Figure.transform,
                        markMaterial);
                StainGripImprintController controller =
                    prerequisites.Figure.GetComponent<
                        StainGripImprintController>();

                if (controller == null)
                {
                    controller =
                        Undo.AddComponent<
                            StainGripImprintController>(
                            prerequisites.Figure);
                }

                controller.Configure(
                    prerequisites.Clarity,
                    prerequisites.CharacterController,
                    prerequisites.RoleAuthority,
                    prerequisites.HijackController,
                    prerequisites.CrawlController,
                    prerequisites.CarryController,
                    prerequisites.CrackController,
                    config,
                    imprintVisual);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(prototype.Floor);
                EditorUtility.SetDirty(prototype.Wall);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.Figure.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M32 Setup] Tamamlandı. Tam Leke ol; " +
                    "açık renk temiz platform veya duvar üzerinde " +
                    "E ile geçici tutunma izi bırak. İz yaklaşık " +
                    "8 saniye fiziksel destek sağlar.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "32 - Diagnose Stain Grip Imprint")]
        public static void Diagnose()
        {
            StainGripImprintController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    StainGripImprintController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            StainCleanGripSurface[] surfaces =
                UnityEngine.Object.FindObjectsByType<
                    StainCleanGripSurface>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M32 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"CleanSurfaces={surfaces.Length}, " +
                $"ActiveMarks=" +
                $"{StainGripMark.ActiveMarks.Count}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                StainGripImprintController controller =
                    controllers[i];
                Debug.Log(
                    "[M32 Diagnose Controller] " +
                    $"Path={GetPath(controller.transform)}, " +
                    $"Imprinting={controller.IsImprinting}, " +
                    $"Progress={controller.NormalizedProgress:F2}, " +
                    $"Nearby=" +
                    $"{(controller.NearbySurface != null)}, " +
                    $"Result={controller.LastResult}",
                    controller);
            }

            for (int i = 0;
                 i < StainGripMark.ActiveMarks.Count;
                 i++)
            {
                StainGripMark mark =
                    StainGripMark.ActiveMarks[i];

                if (mark == null)
                {
                    continue;
                }

                Debug.Log(
                    "[M32 Diagnose Mark] " +
                    $"Path={GetPath(mark.transform)}, " +
                    $"Remaining={mark.RemainingLifetime:F2}, " +
                    $"WallLedge={mark.CreatesWallLedge}",
                    mark);
            }
        }

        private static Prerequisites ResolvePrerequisites()
        {
            StainCrackTraversalController[] crackControllers =
                UnityEngine.Object.FindObjectsByType<
                    StainCrackTraversalController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (crackControllers.Length != 1 ||
                authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M32 tek M31 CrackController ve tek M21 " +
                    "RoleAuthority bekliyor. " +
                    $"Crack={crackControllers.Length}, " +
                    $"Authorities={authorities.Length}.");
            }

            GameObject figure =
                crackControllers[0].gameObject;
            FigureClarityState clarity =
                figure.GetComponent<FigureClarityState>();
            CharacterController characterController =
                figure.GetComponent<CharacterController>();
            InkStainCreatureHijackController hijack =
                figure.GetComponent<
                    InkStainCreatureHijackController>();
            StainSurfaceCrawlController crawl =
                figure.GetComponent<
                    StainSurfaceCrawlController>();
            StainSpongeCarryController carry =
                figure.GetComponent<
                    StainSpongeCarryController>();

            if (clarity == null ||
                characterController == null ||
                hijack == null ||
                crawl == null ||
                carry == null)
            {
                throw new InvalidOperationException(
                    "M32, M31 ile aynı Figür kökünde " +
                    "FigureClarityState, CharacterController, " +
                    "M26 HijackController, M28 CrawlController " +
                    "ve M29 CarryController bekliyor. " +
                    $"Clarity={(clarity != null)}, " +
                    $"CharacterController=" +
                    $"{(characterController != null)}, " +
                    $"Hijack={(hijack != null)}, " +
                    $"Crawl={(crawl != null)}, " +
                    $"Carry={(carry != null)}.");
            }

            return new Prerequisites(
                figure,
                clarity,
                characterController,
                authorities[0],
                hijack,
                crawl,
                carry,
                crackControllers[0]);
        }

        private static StainGripImprintConfig
            GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Figures");
            StainGripImprintConfig config =
                AssetDatabase.LoadAssetAtPath<
                    StainGripImprintConfig>(
                    ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    StainGripImprintConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static PrototypeParts GetOrCreatePrototype(
            Transform figure,
            Material cleanMaterial)
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create M32 grip prototype");
            }

            Vector3 flatForward =
                Vector3.ProjectOnPlane(
                    figure.forward,
                    Vector3.up).normalized;

            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }

            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    flatForward).normalized;
            Vector3 candidate =
                figure.position +
                flatForward * 3.2f +
                right * 3.8f;
            Vector3 ground =
                ResolveGroundPoint(candidate, figure);
            root.transform.position = ground;
            root.transform.rotation =
                Quaternion.LookRotation(
                    flatForward,
                    Vector3.up);

            Transform floor =
                GetOrCreatePrimitive(
                    root.transform,
                    FloorName,
                    new Vector3(0f, 0.12f, 0f),
                    Quaternion.identity,
                    new Vector3(3.6f, 0.24f, 3.2f),
                    cleanMaterial);
            Transform wall =
                GetOrCreatePrimitive(
                    root.transform,
                    WallName,
                    new Vector3(0f, 1.52f, 1.5f),
                    Quaternion.identity,
                    new Vector3(3.6f, 2.8f, 0.24f),
                    cleanMaterial);
            StainCleanGripSurface floorSurface =
                GetOrAddCleanSurface(
                    floor.gameObject,
                    "Temiz Zemin");
            StainCleanGripSurface wallSurface =
                GetOrAddCleanSurface(
                    wall.gameObject,
                    "Temiz Duvar");

            return new PrototypeParts(
                floorSurface,
                wallSurface);
        }

        private static StainCleanGripSurface
            GetOrAddCleanSurface(
                GameObject target,
                string label)
        {
            StainCleanGripSurface surface =
                target.GetComponent<StainCleanGripSurface>();

            if (surface == null)
            {
                surface =
                    Undo.AddComponent<
                        StainCleanGripSurface>(target);
            }

            surface.Configure(true, label);
            return surface;
        }

        private static Transform GetOrCreateImprintVisual(
            Transform figure,
            Material material)
        {
            Transform visual =
                figure.Find(ImprintVisualName);

            if (visual == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cylinder);
                created.name = ImprintVisualName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M32 imprint visual");
                created.transform.SetParent(figure, false);
                visual = created.transform;
            }

            visual.localPosition =
                new Vector3(0f, 0.08f, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localScale =
                new Vector3(0.68f, 0.025f, 0.68f);
            RemoveCollider(visual.gameObject);
            Renderer renderer =
                visual.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
            visual.gameObject.SetActive(false);
            return visual;
        }

        private static Transform GetOrCreatePrimitive(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            Transform result = parent.Find(objectName);

            if (result == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);
                created.name = objectName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M32 prototype object");
                created.transform.SetParent(parent, false);
                result = created.transform;
            }

            result.localPosition = localPosition;
            result.localRotation = localRotation;
            result.localScale = localScale;
            result.gameObject.layer = 0;
            Renderer renderer =
                result.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return result;
        }

        private static Vector3 ResolveGroundPoint(
            Vector3 candidate,
            Transform ignoredRoot)
        {
            RaycastHit[] hits =
                Physics.RaycastAll(
                    candidate + Vector3.up * 5f,
                    Vector3.down,
                    14f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                if (hit.collider == null ||
                    hit.distance >= nearest ||
                    hit.normal.y < 0.55f ||
                    IsPrototypeCollider(hit.collider.transform) ||
                    (ignoredRoot != null &&
                     hit.collider.transform.IsChildOf(
                         ignoredRoot)))
                {
                    continue;
                }

                nearest = hit.distance;
                best = hit;
            }

            return best.collider != null
                ? best.point
                : candidate;
        }

        private static bool IsPrototypeCollider(
            Transform target)
        {
            Transform current = target;

            while (current != null)
            {
                if (current.name == RootName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Material GetOrCreateMaterial(
            string path,
            string materialName,
            Color color,
            float smoothness,
            float metallic)
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");
            EnsureFolder(MaterialFolder);
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit") ??
                    Shader.Find("HDRP/Lit") ??
                    Shader.Find("Standard");

                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "M32 uygun Lit shader bulamadı.");
                }

                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat(
                    "_Smoothness",
                    smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider =
                target.GetComponent<Collider>();

            if (collider != null)
            {
                Undo.DestroyObjectImmediate(collider);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string folder = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static string GetPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            string path = target.name;
            Transform current = target.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private readonly struct Prerequisites
        {
            public Prerequisites(
                GameObject figure,
                FigureClarityState clarity,
                CharacterController characterController,
                InkPainterRoleAuthority roleAuthority,
                InkStainCreatureHijackController hijackController,
                StainSurfaceCrawlController crawlController,
                StainSpongeCarryController carryController,
                StainCrackTraversalController crackController)
            {
                Figure = figure;
                Clarity = clarity;
                CharacterController = characterController;
                RoleAuthority = roleAuthority;
                HijackController = hijackController;
                CrawlController = crawlController;
                CarryController = carryController;
                CrackController = crackController;
            }

            public GameObject Figure { get; }
            public FigureClarityState Clarity { get; }
            public CharacterController CharacterController { get; }
            public InkPainterRoleAuthority RoleAuthority { get; }
            public InkStainCreatureHijackController HijackController
            {
                get;
            }
            public StainSurfaceCrawlController CrawlController
            {
                get;
            }
            public StainSpongeCarryController CarryController
            {
                get;
            }
            public StainCrackTraversalController CrackController
            {
                get;
            }
        }

        private readonly struct PrototypeParts
        {
            public PrototypeParts(
                StainCleanGripSurface floor,
                StainCleanGripSurface wall)
            {
                Floor = floor;
                Wall = wall;
            }

            public StainCleanGripSurface Floor { get; }
            public StainCleanGripSurface Wall { get; }
        }
    }
}
