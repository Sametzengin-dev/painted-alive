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
    public static class SetupStainCrackTraversalMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Figures/" +
            "M31_StainCrackTraversalConfig.asset";
        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Ink";
        private const string BarrierMaterialPath =
            MaterialFolder + "/M31_CrackBarrier.mat";
        private const string CrackMaterialPath =
            MaterialFolder + "/M31_StainCrack.mat";
        private const string RootName =
            "M31_StainCrackPrototype";
        private const string EntryName =
            "M31_CrackEntry";
        private const string ExitName =
            "M31_CrackExit";
        private const string TransitVisualName =
            "M31_CrackTransitVisual";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "31 - Setup Stain Crack Traversal")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M31 Setup Play Mode dışında " +
                        "çalıştırılmalıdır.");
                }

                Prerequisites prerequisites =
                    ResolvePrerequisites();
                StainCrackTraversalConfig config =
                    GetOrCreateConfig();
                Material barrierMaterial =
                    GetOrCreateMaterial(
                        BarrierMaterialPath,
                        "M31_CrackBarrier",
                        new Color(
                            0.30f,
                            0.27f,
                            0.34f,
                            1f),
                        0.22f);
                Material crackMaterial =
                    GetOrCreateMaterial(
                        CrackMaterialPath,
                        "M31_StainCrack",
                        new Color(
                            0.015f,
                            0.16f,
                            0.17f,
                            1f),
                        0.82f);

                CrackPrototype prototype =
                    GetOrCreatePrototype(
                        prerequisites.Figure.transform,
                        barrierMaterial,
                        crackMaterial);
                Transform transitVisual =
                    GetOrCreateTransitVisual(
                        prerequisites.Figure.transform,
                        crackMaterial);
                StainCrackTraversalController controller =
                    prerequisites.Figure.GetComponent<
                        StainCrackTraversalController>();

                if (controller == null)
                {
                    controller =
                        Undo.AddComponent<
                            StainCrackTraversalController>(
                            prerequisites.Figure);
                }

                controller.Configure(
                    prerequisites.Clarity,
                    prerequisites.CharacterController,
                    prerequisites.RoleAuthority,
                    prerequisites.HijackController,
                    prerequisites.CrawlController,
                    prerequisites.CarryController,
                    config,
                    transitVisual);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(prototype.Entry);
                EditorUtility.SetDirty(prototype.Exit);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.Figure.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M31 Setup] Tamamlandı. Tam Leke ol; " +
                    "mor duvarın önündeki koyu ince çatlağa " +
                    "yaklaş ve E ile diğer tarafa geç. " +
                    "Normal Figür duvarı aşamaz.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "31 - Diagnose Stain Crack Traversal")]
        public static void Diagnose()
        {
            StainCrackTraversalController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    StainCrackTraversalController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            StainCrackPassage[] passages =
                UnityEngine.Object.FindObjectsByType<
                    StainCrackPassage>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M31 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"Passages={passages.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                StainCrackTraversalController controller =
                    controllers[i];
                Debug.Log(
                    "[M31 Diagnose Controller] " +
                    $"Path={GetPath(controller.transform)}, " +
                    $"Traversing={controller.IsTraversing}, " +
                    $"Progress={controller.NormalizedProgress:F2}, " +
                    $"Nearby=" +
                    $"{(controller.NearbyPassage != null)}, " +
                    $"Result={controller.LastResult}",
                    controller);
            }

            for (int i = 0; i < passages.Length; i++)
            {
                StainCrackPassage passage = passages[i];
                Debug.Log(
                    "[M31 Diagnose Passage] " +
                    $"Path={GetPath(passage.transform)}, " +
                    $"Linked={(passage.LinkedPassage != null)}, " +
                    $"Ready={passage.CanTraverse}",
                    passage);
            }
        }

        private static Prerequisites ResolvePrerequisites()
        {
            StainSpongeCarryController[] carryControllers =
                UnityEngine.Object.FindObjectsByType<
                    StainSpongeCarryController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (carryControllers.Length != 1 ||
                authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M31 tek M29 CarryController ve tek M21 " +
                    "RoleAuthority bekliyor. " +
                    $"Carry={carryControllers.Length}, " +
                    $"Authorities={authorities.Length}.");
            }

            GameObject figure =
                carryControllers[0].gameObject;
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

            if (clarity == null ||
                characterController == null ||
                hijack == null ||
                crawl == null)
            {
                throw new InvalidOperationException(
                    "M31, M29 ile aynı Figür kökünde " +
                    "FigureClarityState, CharacterController, " +
                    "M26 HijackController ve M28 " +
                    "CrawlController bekliyor. " +
                    $"Clarity={(clarity != null)}, " +
                    $"CharacterController=" +
                    $"{(characterController != null)}, " +
                    $"Hijack={(hijack != null)}, " +
                    $"Crawl={(crawl != null)}.");
            }

            return new Prerequisites(
                figure,
                clarity,
                characterController,
                authorities[0],
                hijack,
                crawl,
                carryControllers[0]);
        }

        private static StainCrackTraversalConfig
            GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Figures");
            StainCrackTraversalConfig config =
                AssetDatabase.LoadAssetAtPath<
                    StainCrackTraversalConfig>(
                    ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    StainCrackTraversalConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static CrackPrototype GetOrCreatePrototype(
            Transform figure,
            Material barrierMaterial,
            Material crackMaterial)
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create M31 crack prototype");
            }

            Vector3 flatForward =
                Vector3.ProjectOnPlane(
                    figure.forward,
                    Vector3.up).normalized;

            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }

            Vector3 ground =
                ResolveGroundPoint(
                    figure.position +
                    flatForward * 5.4f,
                    figure);
            root.transform.position =
                ground + Vector3.up * 1.5f;
            root.transform.rotation =
                Quaternion.LookRotation(
                    flatForward,
                    Vector3.up);

            Transform barrier =
                GetOrCreatePrimitive(
                    root.transform,
                    "Barrier",
                    PrimitiveType.Cube,
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(4.2f, 3f, 0.55f),
                    barrierMaterial,
                    true);
            barrier.gameObject.layer = 0;

            PassageParts entry =
                GetOrCreatePassage(
                    root.transform,
                    EntryName,
                    new Vector3(0f, -1.2f, -0.31f),
                    Quaternion.Euler(0f, 180f, 0f),
                    crackMaterial);
            PassageParts exit =
                GetOrCreatePassage(
                    root.transform,
                    ExitName,
                    new Vector3(0f, -1.2f, 0.31f),
                    Quaternion.identity,
                    crackMaterial);

            entry.Passage.Configure(
                exit.Passage,
                entry.ExitPoint,
                entry.Renderer,
                true);
            exit.Passage.Configure(
                entry.Passage,
                exit.ExitPoint,
                exit.Renderer,
                true);

            return new CrackPrototype(
                entry.Passage,
                exit.Passage);
        }

        private static PassageParts GetOrCreatePassage(
            Transform parent,
            string passageName,
            Vector3 localPosition,
            Quaternion localRotation,
            Material material)
        {
            Transform anchor = parent.Find(passageName);

            if (anchor == null)
            {
                GameObject created =
                    new GameObject(passageName);
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M31 crack passage");
                created.transform.SetParent(parent, false);
                anchor = created.transform;
            }

            anchor.localPosition = localPosition;
            anchor.localRotation = localRotation;
            anchor.localScale = Vector3.one;
            Transform visual =
                GetOrCreatePrimitive(
                    anchor,
                    "CrackVisual",
                    PrimitiveType.Cube,
                    new Vector3(0f, 1.05f, 0f),
                    Quaternion.identity,
                    new Vector3(0.24f, 1.65f, 0.045f),
                    material,
                    false);
            Transform exitPoint =
                GetOrCreatePoint(
                    anchor,
                    "SafeExitPoint",
                    new Vector3(0f, 0f, 1.05f));
            StainCrackPassage passage =
                anchor.GetComponent<StainCrackPassage>();

            if (passage == null)
            {
                passage =
                    Undo.AddComponent<
                        StainCrackPassage>(
                        anchor.gameObject);
            }

            return new PassageParts(
                passage,
                exitPoint,
                visual.GetComponent<Renderer>());
        }

        private static Transform GetOrCreateTransitVisual(
            Transform figure,
            Material material)
        {
            Transform visual =
                figure.Find(TransitVisualName);

            if (visual == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);
                created.name = TransitVisualName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M31 transit visual");
                created.transform.SetParent(figure, false);
                visual = created.transform;
            }

            visual.localPosition =
                new Vector3(0f, 0.16f, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localScale =
                new Vector3(0.62f, 0.08f, 0.62f);
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

        private static Transform GetOrCreatePrimitive(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            Transform result = parent.Find(objectName);

            if (result == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        primitiveType);
                created.name = objectName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M31 prototype object");
                created.transform.SetParent(parent, false);
                result = created.transform;
            }

            result.localPosition = localPosition;
            result.localRotation = localRotation;
            result.localScale = localScale;

            if (!keepCollider)
            {
                RemoveCollider(result.gameObject);
            }

            Renderer renderer =
                result.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return result;
        }

        private static Transform GetOrCreatePoint(
            Transform parent,
            string objectName,
            Vector3 localPosition)
        {
            Transform point = parent.Find(objectName);

            if (point == null)
            {
                GameObject created =
                    new GameObject(objectName);
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M31 point");
                created.transform.SetParent(parent, false);
                point = created.transform;
            }

            point.localPosition = localPosition;
            point.localRotation = Quaternion.identity;
            point.localScale = Vector3.one;
            return point;
        }

        private static Material GetOrCreateMaterial(
            string path,
            string materialName,
            Color color,
            float smoothness)
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
                        "M31 uygun Lit shader bulamadı.");
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
                material.SetFloat("_Metallic", 0.08f);
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
                StainSpongeCarryController carryController)
            {
                Figure = figure;
                Clarity = clarity;
                CharacterController = characterController;
                RoleAuthority = roleAuthority;
                HijackController = hijackController;
                CrawlController = crawlController;
                CarryController = carryController;
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
        }

        private readonly struct PassageParts
        {
            public PassageParts(
                StainCrackPassage passage,
                Transform exitPoint,
                Renderer renderer)
            {
                Passage = passage;
                ExitPoint = exitPoint;
                Renderer = renderer;
            }

            public StainCrackPassage Passage { get; }
            public Transform ExitPoint { get; }
            public Renderer Renderer { get; }
        }

        private readonly struct CrackPrototype
        {
            public CrackPrototype(
                StainCrackPassage entry,
                StainCrackPassage exit)
            {
                Entry = entry;
                Exit = exit;
            }

            public StainCrackPassage Entry { get; }
            public StainCrackPassage Exit { get; }
        }
    }
}
