using System;
using PaintedAlive.Figures.StainRestoration;
using PaintedAlive.Figures.StainSupport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools
{
    public static class SetupStainSpongeRestorationMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Figures/" +
            "M30_StainSpongeRestorationConfig.asset";
        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Ink";
        private const string PigmentMaterialPath =
            MaterialFolder + "/M30_CleanPigment.mat";
        private const string SurfaceMaterialPath =
            MaterialFolder + "/M30_RestorationSurface.mat";
        private const string DarkMaterialPath =
            MaterialFolder + "/M30_IndicatorBase.mat";
        private const string PrototypeCarrierName =
            "M29_PrototypeRescueSponge";
        private const string PigmentSourceName =
            "M30_CleanPigmentWell";
        private const string RestorationSurfaceName =
            "M30_RestorationCanvas";
        private const string IndicatorName =
            "M30_CleanPigmentIndicator";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "30 - Setup Stain Sponge Restoration")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M30 Setup Play Mode dışında " +
                        "çalıştırılmalıdır.");
                }

                StainSpongeCarrier[] carriers =
                    UnityEngine.Object.FindObjectsByType<
                        StainSpongeCarrier>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                if (carriers.Length == 0)
                {
                    throw new InvalidOperationException(
                        "M30 en az bir M29 " +
                        "StainSpongeCarrier bekliyor. " +
                        "Önce M29 Setup'ını tamamla.");
                }

                StainSpongeCarrier prototype =
                    FindPrototypeCarrier(carriers);

                if (prototype == null)
                {
                    throw new InvalidOperationException(
                        "M30 test akışı için " +
                        PrototypeCarrierName +
                        " nesnesini bulamadı. M29 Setup'ını " +
                        "bir kez çalıştır.");
                }

                StainSpongeRestorationConfig config =
                    GetOrCreateConfig();
                Material pigmentMaterial =
                    GetOrCreateMaterial(
                        PigmentMaterialPath,
                        "M30_CleanPigment",
                        new Color(
                            0.90f,
                            0.96f,
                            1f,
                            1f),
                        0.82f);
                Material surfaceMaterial =
                    GetOrCreateMaterial(
                        SurfaceMaterialPath,
                        "M30_RestorationSurface",
                        new Color(
                            0.60f,
                            0.87f,
                            0.77f,
                            1f),
                        0.28f);
                Material darkMaterial =
                    GetOrCreateMaterial(
                        DarkMaterialPath,
                        "M30_IndicatorBase",
                        new Color(
                            0.13f,
                            0.21f,
                            0.23f,
                            1f),
                        0.55f);

                int configuredCarrierCount = 0;

                for (int i = 0; i < carriers.Length; i++)
                {
                    if (ConfigureCarrier(
                            carriers[i],
                            config,
                            darkMaterial))
                    {
                        configuredCarrierCount++;
                    }
                }

                Vector3 flatRight =
                    Vector3.ProjectOnPlane(
                        prototype.transform.right,
                        Vector3.up).normalized;

                if (flatRight.sqrMagnitude < 0.001f)
                {
                    flatRight = Vector3.right;
                }

                Vector3 pigmentPosition =
                    ResolveGroundPoint(
                        prototype.transform.position -
                        flatRight * 3.1f,
                        prototype.transform);
                Vector3 surfacePosition =
                    ResolveGroundPoint(
                        prototype.transform.position +
                        flatRight * 3.1f,
                        prototype.transform);

                StainCleanPigmentSource source =
                    GetOrCreatePigmentSource(
                        pigmentPosition,
                        prototype.transform.rotation,
                        pigmentMaterial,
                        darkMaterial);
                StainRestorationSurface surface =
                    GetOrCreateRestorationSurface(
                        surfacePosition,
                        prototype.transform.rotation,
                        surfaceMaterial,
                        pigmentMaterial);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(source);
                EditorUtility.SetDirty(surface);
                EditorSceneManager.MarkSceneDirty(
                    prototype.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M30 Setup] Tamamlandı. " +
                    $"Taşıyıcı={configuredCarrierCount}, " +
                    "TemizPigment=1, RestoreYüzeyi=1. " +
                    "Leke olarak M29 süngerine gir; oklarla " +
                    "beyaz pigment haznesine, ardından yeşil " +
                    "restorasyon yüzeyine git ve 2,25 saniye " +
                    "üzerinde kal.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "30 - Diagnose Stain Sponge Restoration")]
        public static void Diagnose()
        {
            StainSpongeRestorationCarrier[] restorers =
                UnityEngine.Object.FindObjectsByType<
                    StainSpongeRestorationCarrier>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            StainCleanPigmentSource[] sources =
                UnityEngine.Object.FindObjectsByType<
                    StainCleanPigmentSource>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            StainRestorationSurface[] surfaces =
                UnityEngine.Object.FindObjectsByType<
                    StainRestorationSurface>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M30 Diagnose] " +
                $"Restorers={restorers.Length}, " +
                $"PigmentSources={sources.Length}, " +
                $"RestorationSurfaces={surfaces.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < restorers.Length; i++)
            {
                StainSpongeRestorationCarrier restorer =
                    restorers[i];
                StainSpongeCarrier carrier =
                    restorer.GetComponent<
                        StainSpongeCarrier>();
                Debug.Log(
                    "[M30 Diagnose Carrier] " +
                    $"Path={GetPath(restorer.transform)}, " +
                    $"Passenger=" +
                    $"{(carrier != null && carrier.HasPassenger)}, " +
                    $"Pigment={restorer.HasCleanPigment}, " +
                    $"Progress={restorer.NormalizedProgress:F2}, " +
                    $"Result={restorer.LastResult}",
                    restorer);
            }
        }

        private static bool ConfigureCarrier(
            StainSpongeCarrier carrier,
            StainSpongeRestorationConfig config,
            Material indicatorMaterial)
        {
            if (carrier == null)
            {
                return false;
            }

            StainSpongeRestorationCarrier restorer =
                carrier.GetComponent<
                    StainSpongeRestorationCarrier>();

            if (restorer == null)
            {
                restorer =
                    Undo.AddComponent<
                        StainSpongeRestorationCarrier>(
                        carrier.gameObject);
            }

            Transform indicator =
                carrier.transform.Find(IndicatorName);

            if (indicator == null)
            {
                GameObject created =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);
                created.name = IndicatorName;
                Undo.RegisterCreatedObjectUndo(
                    created,
                    "Create M30 pigment indicator");
                created.transform.SetParent(
                    carrier.transform,
                    false);
                indicator = created.transform;
            }

            indicator.localPosition =
                new Vector3(0f, 1.08f, 0f);
            indicator.localRotation = Quaternion.identity;
            indicator.localScale =
                Vector3.one * 0.16f;
            RemoveCollider(indicator.gameObject);
            Renderer renderer =
                indicator.GetComponent<Renderer>();
            renderer.sharedMaterial = indicatorMaterial;
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
            restorer.Configure(carrier, config, renderer);
            EditorUtility.SetDirty(restorer);
            return true;
        }

        private static StainCleanPigmentSource
            GetOrCreatePigmentSource(
                Vector3 position,
                Quaternion rotation,
                Material pigmentMaterial,
                Material darkMaterial)
        {
            GameObject root =
                GameObject.Find(PigmentSourceName);

            if (root == null)
            {
                root = new GameObject(PigmentSourceName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create M30 clean pigment source");
            }

            root.transform.position = position;
            root.transform.rotation = rotation;
            Transform baseVisual =
                GetOrCreatePrimitive(
                    root.transform,
                    "WellBase",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 0.09f, 0f),
                    new Vector3(1.18f, 0.09f, 1.18f),
                    darkMaterial);
            Transform pigmentVisual =
                GetOrCreatePrimitive(
                    root.transform,
                    "CleanPigment",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 0.22f, 0f),
                    new Vector3(0.82f, 0.15f, 0.82f),
                    pigmentMaterial);
            Transform point =
                GetOrCreatePoint(
                    root.transform,
                    "InteractionPoint",
                    new Vector3(0f, 0.2f, 0f));
            StainCleanPigmentSource source =
                root.GetComponent<StainCleanPigmentSource>();

            if (source == null)
            {
                source =
                    Undo.AddComponent<
                        StainCleanPigmentSource>(root);
            }

            source.Configure(point, true, 3);
            EditorUtility.SetDirty(baseVisual);
            EditorUtility.SetDirty(pigmentVisual);
            return source;
        }

        private static StainRestorationSurface
            GetOrCreateRestorationSurface(
                Vector3 position,
                Quaternion rotation,
                Material surfaceMaterial,
                Material accentMaterial)
        {
            GameObject root =
                GameObject.Find(RestorationSurfaceName);

            if (root == null)
            {
                root =
                    new GameObject(RestorationSurfaceName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create M30 restoration surface");
            }

            root.transform.position = position;
            root.transform.rotation = rotation;
            Transform canvas =
                GetOrCreatePrimitive(
                    root.transform,
                    "RestoreCanvas",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.035f, 0f),
                    new Vector3(2.45f, 0.07f, 2.45f),
                    surfaceMaterial);
            Transform center =
                GetOrCreatePrimitive(
                    root.transform,
                    "RestoreCenter",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 0.09f, 0f),
                    new Vector3(0.48f, 0.025f, 0.48f),
                    accentMaterial);
            Transform point =
                GetOrCreatePoint(
                    root.transform,
                    "RestorationPoint",
                    new Vector3(0f, 0.1f, 0f));
            StainRestorationSurface surface =
                root.GetComponent<StainRestorationSurface>();

            if (surface == null)
            {
                surface =
                    Undo.AddComponent<
                        StainRestorationSurface>(root);
            }

            surface.Configure(point, true);
            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(center);
            return surface;
        }

        private static StainSpongeCarrier FindPrototypeCarrier(
            StainSpongeCarrier[] carriers)
        {
            for (int i = 0; i < carriers.Length; i++)
            {
                if (carriers[i] != null &&
                    (carriers[i].IsPrototypeCarrier ||
                     carriers[i].name ==
                        PrototypeCarrierName))
                {
                    return carriers[i];
                }
            }

            return null;
        }

        private static StainSpongeRestorationConfig
            GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Figures");
            StainSpongeRestorationConfig config =
                AssetDatabase.LoadAssetAtPath<
                    StainSpongeRestorationConfig>(
                    ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    StainSpongeRestorationConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static Vector3 ResolveGroundPoint(
            Vector3 candidate,
            Transform ignoredRoot)
        {
            RaycastHit[] hits =
                Physics.RaycastAll(
                    candidate + Vector3.up * 4f,
                    Vector3.down,
                    12f,
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
                    "Create M30 prototype visual");
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
                    "Create M30 interaction point");
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
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "M30 için uyumlu shader bulunamadı.");
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
                material.SetFloat("_Smoothness", smoothness);
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
    }
}
