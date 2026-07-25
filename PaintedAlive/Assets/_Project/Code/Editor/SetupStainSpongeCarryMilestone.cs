using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainMovement;
using PaintedAlive.Figures.StainSupport;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools
{
    public static class SetupStainSpongeCarryMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Figures/" +
            "M29_StainSpongeCarryConfig.asset";
        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Ink";
        private const string SpongeMaterialPath =
            MaterialFolder + "/M29_RescueSponge.mat";
        private const string StainMaterialPath =
            MaterialFolder + "/M29_CarriedStain.mat";
        private const string PrototypeCarrierName =
            "M29_PrototypeRescueSponge";
        private const string SocketName =
            "M29_StainCarrySocket";
        private const string PortableVisualName =
            "M29_CarriedStainVisual";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "29 - Setup Stain Sponge Carry")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M29 Setup Play Mode dışında " +
                        "çalıştırılmalıdır.");
                }

                Prerequisites prerequisites =
                    ResolvePrerequisites();
                StainSpongeCarryConfig config =
                    GetOrCreateConfig();
                Material spongeMaterial =
                    GetOrCreateMaterial(
                        SpongeMaterialPath,
                        "M29_RescueSponge",
                        new Color(
                            0.10f,
                            0.48f,
                            0.43f,
                            1f),
                        0.28f);
                Material stainMaterial =
                    GetOrCreateMaterial(
                        StainMaterialPath,
                        "M29_CarriedStain",
                        new Color(
                            0.025f,
                            0.11f,
                            0.14f,
                            1f),
                        0.85f);

                StainSpongeCarryController carryController =
                    GetOrCreatePlayerController(
                        prerequisites,
                        config);
                int realCarrierCount =
                    ConfigureRealSpongeCarriers(
                        stainMaterial);
                StainSpongeCarrier prototypeCarrier =
                    GetOrCreatePrototypeCarrier(
                        prerequisites,
                        config,
                        spongeMaterial,
                        stainMaterial);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(spongeMaterial);
                EditorUtility.SetDirty(stainMaterial);
                EditorUtility.SetDirty(carryController);
                EditorUtility.SetDirty(prototypeCarrier);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.Figure.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M29 Setup] Tamamlandı. " +
                    $"GerçekSüngerTaşıyıcı={realCarrierCount}, " +
                    "TestTaşıyıcısı=1. Tam Leke olup turkuaz " +
                    "süngere yaklaş; E ile gir/çık, içerideyken " +
                    "Ok tuşlarıyla test taşıyıcısını hareket ettir.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "29 - Diagnose Stain Sponge Carry")]
        public static void Diagnose()
        {
            StainSpongeCarryController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    StainSpongeCarryController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            StainSpongeCarrier[] carriers =
                UnityEngine.Object.FindObjectsByType<
                    StainSpongeCarrier>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M29 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"Carriers={carriers.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                StainSpongeCarryController controller =
                    controllers[i];
                Debug.Log(
                    "[M29 Diagnose Controller] " +
                    $"Path={GetPath(controller.transform)}, " +
                    $"Carried={controller.IsCarried}, " +
                    $"Carrier=" +
                    $"{GetName(controller.CurrentCarrier)}, " +
                    $"Nearby=" +
                    $"{GetName(controller.NearbyCarrier)}, " +
                    $"Result={controller.LastResult}",
                    controller);
            }

            for (int i = 0; i < carriers.Length; i++)
            {
                StainSpongeCarrier carrier = carriers[i];
                Debug.Log(
                    "[M29 Diagnose Carrier] " +
                    $"Path={GetPath(carrier.transform)}, " +
                    $"Prototype={carrier.IsPrototypeCarrier}, " +
                    $"Active={carrier.isActiveAndEnabled}, " +
                    $"Occupied={carrier.HasPassenger}, " +
                    $"Owner={GetName(carrier.OwnerClarity)}",
                    carrier);
            }
        }

        private static Prerequisites ResolvePrerequisites()
        {
            StainSurfaceCrawlController[] crawlers =
                UnityEngine.Object.FindObjectsByType<
                    StainSurfaceCrawlController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (crawlers.Length != 1 ||
                authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M29 tek M28 CrawlController ve tek " +
                    "RoleAuthority bekliyor. " +
                    $"Crawlers={crawlers.Length}, " +
                    $"Authorities={authorities.Length}.");
            }

            GameObject figureObject =
                crawlers[0].gameObject;
            FigureClarityState clarity =
                figureObject.GetComponent<
                    FigureClarityState>();
            FigureMotor figure =
                figureObject.GetComponent<FigureMotor>();
            CharacterController characterController =
                figureObject.GetComponent<
                    CharacterController>();
            InkStainCreatureHijackController hijack =
                figureObject.GetComponent<
                    InkStainCreatureHijackController>();
            Camera camera =
                ResolveFigureCamera(
                    figureObject,
                    authorities[0],
                    hijack);

            if (clarity == null ||
                figure == null ||
                characterController == null ||
                hijack == null ||
                camera == null)
            {
                throw new InvalidOperationException(
                    "M29, M28 Figür kökünde Clarity, " +
                    "FigureMotor, CharacterController, " +
                    "M26 HijackController ve Figür kamerası " +
                    "bekliyor. " +
                    $"Clarity={(clarity != null)}, " +
                    $"Figure={(figure != null)}, " +
                    $"CharacterController=" +
                    $"{(characterController != null)}, " +
                    $"Hijack={(hijack != null)}, " +
                    $"Camera={(camera != null)}.");
            }

            return new Prerequisites(
                figure,
                clarity,
                characterController,
                camera,
                authorities[0],
                hijack,
                crawlers[0]);
        }

        private static Camera ResolveFigureCamera(
            GameObject figureObject,
            InkPainterRoleAuthority authority,
            InkStainCreatureHijackController hijack)
        {
            Camera camera =
                figureObject.GetComponentInChildren<
                    Camera>(true);

            if (camera != null)
            {
                return camera;
            }

            SerializedObject authorityObject =
                new SerializedObject(authority);
            SerializedProperty cameraProperty =
                authorityObject.FindProperty("figureCamera");
            camera = cameraProperty != null
                ? cameraProperty.objectReferenceValue as Camera
                : null;

            if (camera != null)
            {
                return camera;
            }

            SerializedObject hijackObject =
                new SerializedObject(hijack);
            cameraProperty =
                hijackObject.FindProperty("figureCamera");
            return cameraProperty != null
                ? cameraProperty.objectReferenceValue as Camera
                : null;
        }

        private static StainSpongeCarryConfig
            GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Figures");

            StainSpongeCarryConfig config =
                AssetDatabase.LoadAssetAtPath<
                    StainSpongeCarryConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    StainSpongeCarryConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static StainSpongeCarryController
            GetOrCreatePlayerController(
                Prerequisites prerequisites,
                StainSpongeCarryConfig config)
        {
            StainSpongeCarryController[] existing =
                UnityEngine.Object.FindObjectsByType<
                    StainSpongeCarryController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (existing.Length > 1)
            {
                throw new InvalidOperationException(
                    "Sahnede birden fazla M29 CarryController " +
                    "var. Kopyaları temizle.");
            }

            StainSpongeCarryController controller =
                existing.Length == 1
                    ? existing[0]
                    : Undo.AddComponent<
                        StainSpongeCarryController>(
                        prerequisites.Figure.gameObject);

            if (controller.gameObject !=
                prerequisites.Figure.gameObject)
            {
                throw new InvalidOperationException(
                    "M29 CarryController doğru Figür kökünde " +
                    "değil.");
            }

            controller.Configure(
                prerequisites.Clarity,
                prerequisites.Figure,
                prerequisites.CharacterController,
                prerequisites.Camera,
                prerequisites.RoleAuthority,
                prerequisites.Hijack,
                prerequisites.Crawl,
                config);
            return controller;
        }

        private static int ConfigureRealSpongeCarriers(
            Material stainMaterial)
        {
            SpongeController[] sponges =
                UnityEngine.Object.FindObjectsByType<
                    SpongeController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < sponges.Length; i++)
            {
                SpongeController sponge = sponges[i];
                StainSpongeCarrier carrier =
                    sponge.GetComponent<StainSpongeCarrier>();

                if (carrier == null)
                {
                    carrier =
                        Undo.AddComponent<
                            StainSpongeCarrier>(
                            sponge.gameObject);
                }

                Transform socket =
                    GetOrCreateSocket(sponge.transform);
                Renderer portableVisual =
                    GetOrCreatePortableVisual(
                        socket,
                        stainMaterial);
                FigureClarityState owner =
                    sponge.GetComponentInParent<
                        FigureClarityState>();
                carrier.Configure(
                    owner,
                    socket,
                    portableVisual,
                    false);
                EditorUtility.SetDirty(carrier);
            }

            return sponges.Length;
        }

        private static StainSpongeCarrier
            GetOrCreatePrototypeCarrier(
                Prerequisites prerequisites,
                StainSpongeCarryConfig config,
                Material spongeMaterial,
                Material stainMaterial)
        {
            GameObject prototype =
                GameObject.Find(PrototypeCarrierName);
            bool created = prototype == null;

            if (created)
            {
                prototype =
                    new GameObject(PrototypeCarrierName);
                Undo.RegisterCreatedObjectUndo(
                    prototype,
                    "Create M29 prototype rescue sponge");
            }

            StainSpongeCarrier carrier =
                prototype.GetComponent<StainSpongeCarrier>();

            if (carrier == null)
            {
                carrier =
                    Undo.AddComponent<StainSpongeCarrier>(
                        prototype);
            }

            Transform body =
                GetOrCreatePrimitive(
                    prototype.transform,
                    "SpongeBody",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.38f, 0f),
                    new Vector3(1.1f, 0.55f, 0.72f),
                    spongeMaterial);
            Transform handle =
                GetOrCreatePrimitive(
                    prototype.transform,
                    "SpongeHandle",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 0.82f, 0f),
                    new Vector3(0.17f, 0.28f, 0.17f),
                    spongeMaterial);
            handle.localRotation =
                Quaternion.Euler(0f, 0f, 90f);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(handle);

            Transform socket =
                GetOrCreateSocket(prototype.transform);
            socket.localPosition =
                new Vector3(0f, 0.74f, 0f);
            Renderer portableVisual =
                GetOrCreatePortableVisual(
                    socket,
                    stainMaterial);
            carrier.Configure(
                null,
                socket,
                portableVisual,
                true);

            PrototypeSpongeCarrierMover mover =
                prototype.GetComponent<
                    PrototypeSpongeCarrierMover>();

            if (mover == null)
            {
                mover =
                    Undo.AddComponent<
                        PrototypeSpongeCarrierMover>(
                        prototype);
            }

            mover.Configure(
                carrier,
                config,
                prerequisites.Camera);

            if (created)
            {
                PlacePrototypeNearFigure(
                    prototype.transform,
                    prerequisites.Figure.transform);
            }

            EditorUtility.SetDirty(mover);
            return carrier;
        }

        private static Transform GetOrCreateSocket(
            Transform parent)
        {
            Transform socket = parent.Find(SocketName);

            if (socket == null)
            {
                GameObject created =
                    new GameObject(SocketName);
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M29 Stain carry socket");
                created.transform.SetParent(parent, false);
                socket = created.transform;
                socket.localPosition =
                    new Vector3(0f, 0.3f, 0f);
            }

            return socket;
        }

        private static Renderer GetOrCreatePortableVisual(
            Transform socket,
            Material material)
        {
            Transform visual =
                socket.Find(PortableVisualName);

            if (visual == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);
                created.name = PortableVisualName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M29 carried Stain visual");
                created.transform.SetParent(socket, false);
                visual = created.transform;
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale =
                new Vector3(0.55f, 0.18f, 0.48f);
            RemoveCollider(visual.gameObject);

            Renderer renderer =
                visual.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.enabled = false;
            return renderer;
        }

        private static Transform GetOrCreatePrimitive(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
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
                    "Create M29 prototype visual");
                created.transform.SetParent(parent, false);
                result = created.transform;
            }

            result.localPosition = localPosition;
            result.localRotation = Quaternion.identity;
            result.localScale = localScale;
            RemoveCollider(result.gameObject);
            Renderer renderer =
                result.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return result;
        }

        private static void PlacePrototypeNearFigure(
            Transform prototype,
            Transform figure)
        {
            Vector3 flatForward =
                Vector3.ProjectOnPlane(
                    figure.forward,
                    Vector3.up).normalized;

            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }

            Vector3 candidate =
                figure.position + flatForward * 2.1f;
            RaycastHit[] hits =
                Physics.RaycastAll(
                    candidate + Vector3.up * 3f,
                    Vector3.down,
                    8f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null ||
                    hits[i].collider.transform.IsChildOf(
                        figure) ||
                    hits[i].distance >= nearest ||
                    hits[i].normal.y < 0.55f)
                {
                    continue;
                }

                nearest = hits[i].distance;
                best = hits[i];
            }

            prototype.position = best.collider != null
                ? best.point
                : candidate;
            prototype.rotation =
                Quaternion.LookRotation(
                    flatForward,
                    Vector3.up);
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
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "M29 için uyumlu shader bulunamadı.");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader == null)
            {
                material.shader = shader;
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

            return material;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();

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

        private static string GetName(
            UnityEngine.Object target)
        {
            return target != null ? target.name : "None";
        }

        private readonly struct Prerequisites
        {
            public Prerequisites(
                FigureMotor figure,
                FigureClarityState clarity,
                CharacterController characterController,
                Camera camera,
                InkPainterRoleAuthority roleAuthority,
                InkStainCreatureHijackController hijack,
                StainSurfaceCrawlController crawl)
            {
                Figure = figure;
                Clarity = clarity;
                CharacterController = characterController;
                Camera = camera;
                RoleAuthority = roleAuthority;
                Hijack = hijack;
                Crawl = crawl;
            }

            public FigureMotor Figure { get; }
            public FigureClarityState Clarity { get; }
            public CharacterController CharacterController
            {
                get;
            }
            public Camera Camera { get; }
            public InkPainterRoleAuthority RoleAuthority
            {
                get;
            }
            public InkStainCreatureHijackController Hijack
            {
                get;
            }
            public StainSurfaceCrawlController Crawl { get; }
        }
    }
}
