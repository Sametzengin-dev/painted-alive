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
    public static class SetupStainDirectionSignalMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Figures/" +
            "M33_StainDirectionSignalConfig.asset";
        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Ink";
        private const string SignalMaterialPath =
            MaterialFolder + "/M33_DirectionSignal.mat";
        private const string BoardMaterialPath =
            MaterialFolder + "/M33_SignalTestBoard.mat";
        private const string RootName =
            "M33_DirectionSignalPrototype";
        private const string BoardName =
            "M33_SignalTestBoard";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "33 - Setup Stain Direction Signal")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M33 Setup Play Mode dışında " +
                        "çalıştırılmalıdır.");
                }

                Prerequisites prerequisites =
                    ResolvePrerequisites();
                Material signalMaterial =
                    GetOrCreateMaterial(
                        SignalMaterialPath,
                        "M33_DirectionSignal",
                        new Color(0.04f, 0.92f, 0.78f, 1f),
                        0.82f,
                        0.08f,
                        new Color(0.01f, 0.32f, 0.28f, 1f));
                Material boardMaterial =
                    GetOrCreateMaterial(
                        BoardMaterialPath,
                        "M33_SignalTestBoard",
                        new Color(0.2f, 0.16f, 0.26f, 1f),
                        0.2f,
                        0f,
                        Color.black);
                StainDirectionSignalConfig config =
                    GetOrCreateConfig();
                config.ConfigureMaterial(signalMaterial);
                GetOrCreateTestBoard(
                    prerequisites.Figure.transform,
                    boardMaterial);

                StainDirectionSignalController controller =
                    prerequisites.Figure.GetComponent<
                        StainDirectionSignalController>();

                if (controller == null)
                {
                    controller =
                        Undo.AddComponent<
                            StainDirectionSignalController>(
                            prerequisites.Figure);
                }

                controller.Configure(
                    prerequisites.Clarity,
                    prerequisites.RoleAuthority,
                    prerequisites.HijackController,
                    prerequisites.CrawlController,
                    prerequisites.CarryController,
                    prerequisites.CrackController,
                    prerequisites.ImprintController,
                    prerequisites.FigureCamera,
                    config);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.Figure.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M33 Setup] Tamamlandı. Tam Leke ol; " +
                    "kamera merkezini bir yüzeye getir ve Q ile " +
                    "yaklaşık 6 saniyelik yön sinyali bırak.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "33 - Diagnose Stain Direction Signal")]
        public static void Diagnose()
        {
            StainDirectionSignalController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    StainDirectionSignalController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M33 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"ActiveSignals=" +
                $"{StainDirectionSignal.ActiveSignals.Count}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                StainDirectionSignalController controller =
                    controllers[i];
                Debug.Log(
                    "[M33 Diagnose Controller] " +
                    $"Path={GetPath(controller.transform)}, " +
                    $"Cooldown={controller.CooldownRemaining:F2}, " +
                    $"Result={controller.LastResult}",
                    controller);
            }

            for (int i = 0;
                 i < StainDirectionSignal.ActiveSignals.Count;
                 i++)
            {
                StainDirectionSignal signal =
                    StainDirectionSignal.ActiveSignals[i];

                if (signal == null)
                {
                    continue;
                }

                Debug.Log(
                    "[M33 Diagnose Signal] " +
                    $"Path={GetPath(signal.transform)}, " +
                    $"Remaining={signal.RemainingLifetime:F2}, " +
                    $"Direction={signal.SignalDirection}",
                    signal);
            }
        }

        private static Prerequisites ResolvePrerequisites()
        {
            StainGripImprintController[] imprintControllers =
                UnityEngine.Object.FindObjectsByType<
                    StainGripImprintController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (imprintControllers.Length != 1 ||
                authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M33 tek M32 ImprintController ve tek M21 " +
                    "RoleAuthority bekliyor. " +
                    $"Imprint={imprintControllers.Length}, " +
                    $"Authorities={authorities.Length}.");
            }

            GameObject figure =
                imprintControllers[0].gameObject;
            FigureClarityState clarity =
                figure.GetComponent<FigureClarityState>();
            InkStainCreatureHijackController hijack =
                figure.GetComponent<
                    InkStainCreatureHijackController>();
            StainSurfaceCrawlController crawl =
                figure.GetComponent<
                    StainSurfaceCrawlController>();
            StainSpongeCarryController carry =
                figure.GetComponent<
                    StainSpongeCarryController>();
            StainCrackTraversalController crack =
                figure.GetComponent<
                    StainCrackTraversalController>();
            Camera camera =
                ResolveFigureCamera(
                    figure,
                    authorities[0],
                    hijack);

            if (clarity == null ||
                hijack == null ||
                crawl == null ||
                carry == null ||
                crack == null ||
                camera == null)
            {
                throw new InvalidOperationException(
                    "M33, M32 ile aynı Figür kökünde " +
                    "FigureClarityState, M26 HijackController, " +
                    "M28 CrawlController, M29 CarryController, " +
                    "M31 CrackController ve Figür kamerası " +
                    "bekliyor. " +
                    $"Clarity={(clarity != null)}, " +
                    $"Hijack={(hijack != null)}, " +
                    $"Crawl={(crawl != null)}, " +
                    $"Carry={(carry != null)}, " +
                    $"Crack={(crack != null)}, " +
                    $"Camera={(camera != null)}.");
            }

            return new Prerequisites(
                figure,
                clarity,
                authorities[0],
                hijack,
                crawl,
                carry,
                crack,
                imprintControllers[0],
                camera);
        }

        private static Camera ResolveFigureCamera(
            GameObject figure,
            InkPainterRoleAuthority authority,
            InkStainCreatureHijackController hijack)
        {
            Camera camera =
                figure.GetComponentInChildren<Camera>(true);

            if (camera != null)
            {
                return camera;
            }

            if (authority != null)
            {
                SerializedObject serializedAuthority =
                    new SerializedObject(authority);
                SerializedProperty property =
                    serializedAuthority.FindProperty(
                        "figureCamera");
                camera = property != null
                    ? property.objectReferenceValue as Camera
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
                SerializedProperty property =
                    serializedHijack.FindProperty(
                        "figureCamera");
                camera = property != null
                    ? property.objectReferenceValue as Camera
                    : null;
            }

            return camera;
        }

        private static StainDirectionSignalConfig
            GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Figures");
            StainDirectionSignalConfig config =
                AssetDatabase.LoadAssetAtPath<
                    StainDirectionSignalConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    StainDirectionSignalConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static void GetOrCreateTestBoard(
            Transform figure,
            Material boardMaterial)
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create M33 direction signal prototype");
            }

            Vector3 forward =
                Vector3.ProjectOnPlane(
                    figure.forward,
                    Vector3.up).normalized;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    forward).normalized;
            Vector3 candidate =
                figure.position +
                forward * 4.5f -
                right * 3.2f;
            Vector3 ground =
                ResolveGroundPoint(candidate, figure);
            root.transform.position = ground;
            root.transform.rotation =
                Quaternion.LookRotation(
                    -forward,
                    Vector3.up);

            Transform board =
                root.transform.Find(BoardName);

            if (board == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);
                created.name = BoardName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M33 signal test board");
                created.transform.SetParent(root.transform, false);
                board = created.transform;
            }

            board.localPosition =
                new Vector3(0f, 1.45f, 0f);
            board.localRotation = Quaternion.identity;
            board.localScale =
                new Vector3(3.4f, 2.9f, 0.22f);
            board.gameObject.layer = 0;
            Renderer renderer =
                board.GetComponent<Renderer>();
            renderer.sharedMaterial = boardMaterial;
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
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
            float metallic,
            Color emission)
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
                        "M33 uygun Lit shader bulamadı.");
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

            if (emission.maxColorComponent > 0.001f)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor(
                        "_EmissionColor",
                        emission);
                }
            }

            EditorUtility.SetDirty(material);
            return material;
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
                InkPainterRoleAuthority roleAuthority,
                InkStainCreatureHijackController hijackController,
                StainSurfaceCrawlController crawlController,
                StainSpongeCarryController carryController,
                StainCrackTraversalController crackController,
                StainGripImprintController imprintController,
                Camera figureCamera)
            {
                Figure = figure;
                Clarity = clarity;
                RoleAuthority = roleAuthority;
                HijackController = hijackController;
                CrawlController = crawlController;
                CarryController = carryController;
                CrackController = crackController;
                ImprintController = imprintController;
                FigureCamera = figureCamera;
            }

            public GameObject Figure { get; }
            public FigureClarityState Clarity { get; }
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
            public StainGripImprintController ImprintController
            {
                get;
            }
            public Camera FigureCamera { get; }
        }
    }
}
