#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// M55.4 — Atelier test environment integration.
///
/// Goals:
/// - Add the Blender FBX to the current prototype scene without rewriting gameplay systems.
/// - Keep Figure tools, Oil paint, Watercolor, Stain crawl and Frame Gun contracts intact.
/// - Add collision only where gameplay needs it; decorative paint/foliage/water stays visual-only.
/// - Mark only intended canvas/traversal surfaces as PaintSurface so Painter raycasts continue to work.
/// - Keep the old prototype environment untouched. The new START platform is aligned 2.5 cm above world Y=0
///   so an existing prototype Ground can remain present during migration without winning most surface raycasts.
///
/// This setup is intentionally AddNew-only and idempotent. Re-running repairs the generated integration root.
/// </summary>
public static class SetupAtelierTestSceneIntegration_M55_4
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";

    private const string ModelPath =
        "Assets/_Project/Art/Models/Environments/Atelier/Painted_Alive_Atelier_.fbx";

    private const string MaterialFolder =
        "Assets/_Project/Materials/Environment/AtelierTest";

    private const string IntegrationRootName =
        "M55_4_AtelierTestScene";

    private const string ModelInstanceName =
        "MODEL_Atelier_Heights";

    private const string AnchorsRootName =
        "GameplayAnchors";

    private const string FigureSpawnName =
        "FigureSpawn_START";

    private const string UpperGardenAnchorName =
        "Target_UPPER_GARDEN";

    private const string PaintSurfaceLayerName =
        "PaintSurface";

    // Keeps the Atelier start plane slightly above an old prototype plane at Y=0,
    // avoiding destructive edits to legacy Ground while migrating.
    private const float StartSurfaceWorldY = 0.025f;
    private const float FigureSpawnLift = 0.055f;

    private enum CollisionKind
    {
        None,
        PaintableMesh,
        SolidMesh,
        SolidBox
    }

    private sealed class MaterialSpec
    {
        public string SourceName;
        public string AssetName;
        public Color Color;
        public float Metallic;
        public float Smoothness;
        public bool DoubleSided;
    }

    private static readonly MaterialSpec[] MaterialSpecs =
    {
        Spec("Canvas limestone | warm ivory", "MAT_Atelier_CanvasLimestone", 0.720f, 0.665f, 0.510f, 0.00f, 0.16f),
        Spec("Cut stone edge | chalk", "MAT_Atelier_CutStone", 0.880f, 0.830f, 0.700f, 0.00f, 0.12f),
        Spec("Foliage | deep", "MAT_Atelier_FoliageDeep", 0.060f, 0.160f, 0.090f, 0.00f, 0.05f, true),
        Spec("Exposed timber | honey oak", "MAT_Atelier_Timber", 0.290f, 0.135f, 0.048f, 0.00f, 0.14f),
        Spec("Floating chalk | painterly strata", "MAT_Atelier_FloatingChalk", 0.550f, 0.570f, 0.520f, 0.00f, 0.10f),
        Spec("Distant mountains | blue atmospheric wash", "MAT_Atelier_DistantMountains", 0.210f, 0.400f, 0.520f, 0.00f, 0.08f),
        Spec("Foliage | olive", "MAT_Atelier_FoliageOlive", 0.290f, 0.360f, 0.045f, 0.00f, 0.05f, true),
        Spec("Frame trim | ochre gilt", "MAT_Atelier_FrameTrim", 0.710f, 0.400f, 0.090f, 0.30f, 0.30f),
        Spec("Hanging linen | stained canvas", "MAT_Atelier_HangingLinen", 0.930f, 0.865f, 0.670f, 0.00f, 0.08f, true),
        Spec("Pigment | cerulean", "MAT_Atelier_PigmentCerulean", 0.035f, 0.430f, 0.750f, 0.00f, 0.28f),
        Spec("Pigment | marigold", "MAT_Atelier_PigmentMarigold", 0.980f, 0.430f, 0.045f, 0.00f, 0.26f),
        Spec("Pigment | violet", "MAT_Atelier_PigmentViolet", 0.380f, 0.130f, 0.670f, 0.00f, 0.28f),
        Spec("Hand laid limestone | subtle joints", "MAT_Atelier_HandLaidStone", 0.830f, 0.780f, 0.640f, 0.00f, 0.13f),
        Spec("Waterfall | pale blue strokes", "MAT_Atelier_Waterfall", 0.350f, 0.790f, 0.970f, 0.00f, 0.34f, true),
        Spec("Watercolor | turquoise water", "MAT_Atelier_Watercolor", 0.025f, 0.480f, 0.540f, 0.00f, 0.38f, true),
        Spec("Blossoms | orchid", "MAT_Atelier_Blossoms", 0.660f, 0.200f, 0.490f, 0.00f, 0.06f, true),
        Spec("Structure | dark bronze", "MAT_Atelier_DarkStructure", 0.072f, 0.108f, 0.110f, 0.18f, 0.22f),
    };

    [MenuItem(MenuRoot + "55.4 - Setup / Repair Atelier Test Scene")]
    public static void SetupOrRepair()
    {
        try
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("M55.4 setup Play Mode dışında çalıştırılmalıdır.");

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Aktif ve yüklü bir Unity Scene bulunamadı.");

            ConfigureModelImporter();

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null)
                throw new InvalidOperationException("Atelier FBX import edilemedi: " + ModelPath);

            int paintSurfaceLayer = EnsureLayer(PaintSurfaceLayerName);
            if (paintSurfaceLayer < 0)
                throw new InvalidOperationException("PaintSurface layer oluşturulamadı/bulunamadı.");

            Dictionary<string, Material> materialMap = CreateOrUpdateMaterials();

            GameObject integrationRoot = GetOrCreateRoot(scene, IntegrationRootName);
            GameObject modelInstance = GetOrCreateModelInstance(scene, integrationRoot, modelAsset);

            // Keep the imported model transform clean before alignment.
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            Renderer startRenderer = FindRendererByExactName(modelInstance.transform, "PLATFORM | START");
            if (startRenderer == null)
                throw new InvalidOperationException("FBX içinde 'PLATFORM | START' bulunamadı. Blender obje isimleri değişmiş olabilir.");

            AlignStartPlatformToPrototypeOrigin(modelInstance.transform, startRenderer);

            IntegrationStats stats = ApplyEnvironmentRules(modelInstance, materialMap, paintSurfaceLayer);

            Transform anchorsRoot = GetOrCreateChild(integrationRoot.transform, AnchorsRootName);
            Renderer refreshedStart = FindRendererByExactName(modelInstance.transform, "PLATFORM | START");
            Renderer upperGarden = FindRendererByExactName(modelInstance.transform, "PLATFORM | UPPER_GARDEN");

            Transform spawn = ConfigureSurfaceAnchor(
                anchorsRoot,
                FigureSpawnName,
                refreshedStart,
                FigureSpawnLift);

            ConfigureSurfaceAnchor(
                anchorsRoot,
                UpperGardenAnchorName,
                upperGarden,
                0.10f);

            // Moving only the Figure root keeps every existing child tool/reference intact.
            // If no FigureMotor is present, setup still succeeds and the anchor remains available.
            bool figureMoved = TryMoveFigureTo(spawn);

            EditorUtility.SetDirty(integrationRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                "Atelier test sahnesi entegre edildi.\n\n" +
                "Existing gameplay scripts modified: 0\n" +
                "Legacy Ground automatically disabled: HAYIR\n" +
                "Figure moved to START anchor: " + figureMoved + "\n" +
                "PaintSurface layer: " + paintSurfaceLayer + "\n" +
                "Mesh renderers: " + stats.Renderers + "\n" +
                "Paintable mesh colliders: " + stats.PaintableMeshColliders + "\n" +
                "Solid mesh colliders: " + stats.SolidMeshColliders + "\n" +
                "Solid box colliders: " + stats.SolidBoxColliders + "\n" +
                "Visual-only meshes: " + stats.VisualOnly + "\n" +
                "Material slots remapped: " + stats.MaterialSlotsRemapped + "\n\n" +
                "Şimdi 55.4 - Diagnose Atelier Test Scene çalıştır.";

            Debug.Log("[Painted Alive M55.4] " + summary.Replace("\n", " | "), integrationRoot);
            EditorUtility.DisplayDialog("Painted Alive M55.4", summary, "Tamam");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("M55.4 Setup Hatası", ex.Message, "Tamam");
        }
    }

    [MenuItem(MenuRoot + "55.4 - Move Figure To Atelier START")]
    public static void MoveFigureToStart()
    {
        GameObject root = GameObject.Find(IntegrationRootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("M55.4", "Önce Atelier setup çalıştırılmalı.", "Tamam");
            return;
        }

        Transform spawn = FindChildRecursive(root.transform, FigureSpawnName);
        if (spawn == null)
        {
            EditorUtility.DisplayDialog("M55.4", "FigureSpawn_START bulunamadı.", "Tamam");
            return;
        }

        bool moved = TryMoveFigureTo(spawn);
        if (!moved)
        {
            EditorUtility.DisplayDialog("M55.4", "Loaded Scene içinde FigureMotor bulunamadı.", "Tamam");
            return;
        }

        EditorSceneManager.MarkAllScenesDirty();
        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();

        Debug.Log("[Painted Alive M55.4] Figure Atelier START noktasına taşındı.");
    }

    [MenuItem(MenuRoot + "55.4 - Diagnose Atelier Test Scene")]
    public static void Diagnose()
    {
        GameObject integrationRoot = GameObject.Find(IntegrationRootName);
        GameObject modelInstance = integrationRoot != null
            ? FindChildRecursive(integrationRoot.transform, ModelInstanceName)?.gameObject
            : null;

        int paintLayer = LayerMask.NameToLayer(PaintSurfaceLayerName);
        List<string> lines = new List<string>();
        bool pass = true;

        AddCheck(lines, ref pass, integrationRoot != null, "IntegrationRoot", integrationRoot != null ? GetHierarchyPath(integrationRoot.transform) : "MISSING");
        AddCheck(lines, ref pass, modelInstance != null, "ModelInstance", modelInstance != null ? GetHierarchyPath(modelInstance.transform) : "MISSING");
        AddCheck(lines, ref pass, paintLayer >= 0, "PaintSurfaceLayer", paintLayer >= 0 ? paintLayer.ToString() : "MISSING");

        if (modelInstance != null)
        {
            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = modelInstance.GetComponentsInChildren<Collider>(true);
            MeshCollider[] meshColliders = modelInstance.GetComponentsInChildren<MeshCollider>(true);
            BoxCollider[] boxColliders = modelInstance.GetComponentsInChildren<BoxCollider>(true);

            Renderer start = FindRendererByExactName(modelInstance.transform, "PLATFORM | START");
            AddCheck(lines, ref pass, start != null, "StartPlatform", start != null ? BoundsText(start.bounds) : "MISSING");

            if (start != null)
            {
                float yError = Mathf.Abs(start.bounds.max.y - StartSurfaceWorldY);
                AddCheck(lines, ref pass, yError <= 0.015f, "StartAlignment", "topY=" + start.bounds.max.y.ToString("0.000") + " target=" + StartSurfaceWorldY.ToString("0.000"));
            }

            int expectedPaintables = 0;
            int missingPaintableColliders = 0;
            int wrongPaintableLayers = 0;
            int decorativeColliderCount = 0;

            foreach (MeshFilter filter in modelInstance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null)
                    continue;

                CollisionKind kind = Classify(filter.gameObject.name);
                Collider collider = filter.GetComponent<Collider>();

                if (kind == CollisionKind.PaintableMesh)
                {
                    expectedPaintables++;
                    if (collider == null)
                        missingPaintableColliders++;
                    if (filter.gameObject.layer != paintLayer)
                        wrongPaintableLayers++;
                }
                else if (kind == CollisionKind.None && collider != null)
                {
                    decorativeColliderCount++;
                }
            }

            AddCheck(lines, ref pass, renderers.Length >= 1000, "RendererCount", renderers.Length.ToString());
            AddCheck(lines, ref pass, colliders.Length > 0, "GameplayColliderCount", colliders.Length.ToString());
            AddCheck(lines, ref pass, missingPaintableColliders == 0, "PaintableColliders", "expected=" + expectedPaintables + " missing=" + missingPaintableColliders);
            AddCheck(lines, ref pass, wrongPaintableLayers == 0, "PaintableLayers", "wrong=" + wrongPaintableLayers);

            lines.Add("INFO | MeshColliders | " + meshColliders.Length);
            lines.Add("INFO | BoxColliders | " + boxColliders.Length);
            lines.Add("INFO | DecorativeObjectsWithCollider | " + decorativeColliderCount + " (should normally be 0 unless manually added)");

            Bounds mapBounds;
            if (TryGetCombinedRendererBounds(modelInstance, out mapBounds))
                lines.Add("INFO | MapBounds | center=" + mapBounds.center.ToString("F1") + " size=" + mapBounds.size.ToString("F1"));
        }

        Transform spawn = integrationRoot != null
            ? FindChildRecursive(integrationRoot.transform, FigureSpawnName)
            : null;
        AddCheck(lines, ref pass, spawn != null, "FigureSpawn", spawn != null ? spawn.position.ToString("F3") : "MISSING");

        Component figureMotor = FindSceneComponentByTypeName("FigureMotor");
        lines.Add("INFO | FigureMotor | " + (figureMotor != null ? GetHierarchyPath(figureMotor.transform) : "NOT FOUND"));
        if (figureMotor != null && spawn != null)
            lines.Add("INFO | FigureDistanceToStart | " + Vector3.Distance(figureMotor.transform.position, spawn.position).ToString("0.00") + "m");

        string legacyGroundWarning = FindLegacyGroundWarning(integrationRoot);
        lines.Add("INFO | LegacyGround | " + legacyGroundWarning);

        lines.Add("INFO | ExistingGameplayCodeOverwritten | False");
        lines.Add("INFO | ExistingToolLoadoutModified | False");
        lines.Add("INFO | PainterPaintSurfaceContract | Preserved via PaintSurface layer");
        lines.Add("INFO | DecorativePaintWaterFoliageCollision | Disabled by classification");

        string report =
            "Painted Alive M55.4 Atelier Diagnostics\n" +
            "Result=" + (pass ? "PASS" : "NEEDS_ATTENTION") + "\n\n" +
            string.Join("\n", lines) +
            "\n\nManual Play Mode checks are still required.";

        Debug.Log("[Painted Alive M55.4]\n" + report, integrationRoot);
        EditorUtility.DisplayDialog("M55.4 Diagnostics", report, "Tamam");
    }

    [MenuItem(MenuRoot + "55.4 - Remove Generated Atelier Scene Root")]
    public static void RemoveGeneratedRoot()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("M55.4", "Bu işlem Play Mode dışında yapılmalıdır.", "Tamam");
            return;
        }

        GameObject root = GameObject.Find(IntegrationRootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("M55.4", "Generated Atelier root zaten yok.", "Tamam");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "M55.4 - Remove",
                "Yalnız generated '" + IntegrationRootName + "' scene root silinecek.\nFBX, materyaller ve mevcut gameplay objeleri korunacak.",
                "Sil",
                "İptal"))
        {
            return;
        }

        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Painted Alive M55.4] Generated Atelier scene root removed. Existing gameplay objects were untouched.");
    }

    private static void ConfigureModelImporter()
    {
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.importAnimation)
        {
            importer.importAnimation = false;
            changed = true;
        }
        if (importer.importCameras)
        {
            importer.importCameras = false;
            changed = true;
        }
        if (importer.importLights)
        {
            importer.importLights = false;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static GameObject GetOrCreateRoot(Scene scene, string name)
    {
        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(x => x.name == name);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Create " + name);
        return root;
    }

    private static GameObject GetOrCreateModelInstance(Scene scene, GameObject root, GameObject modelAsset)
    {
        Transform existing = FindChildRecursive(root.transform, ModelInstanceName);
        if (existing != null)
            return existing.gameObject;

        GameObject instance = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
        if (instance == null)
            throw new InvalidOperationException("FBX scene instance oluşturulamadı.");

        Undo.RegisterCreatedObjectUndo(instance, "Instantiate Atelier FBX");
        instance.name = ModelInstanceName;
        instance.transform.SetParent(root.transform, true);
        return instance;
    }

    private static void AlignStartPlatformToPrototypeOrigin(Transform modelRoot, Renderer startRenderer)
    {
        Bounds b = startRenderer.bounds;
        Vector3 currentSurfaceCenter = new Vector3(b.center.x, b.max.y, b.center.z);
        Vector3 desiredSurfaceCenter = new Vector3(0f, StartSurfaceWorldY, 0f);
        modelRoot.position += desiredSurfaceCenter - currentSurfaceCenter;
    }

    private static IntegrationStats ApplyEnvironmentRules(
        GameObject modelInstance,
        Dictionary<string, Material> materialMap,
        int paintSurfaceLayer)
    {
        IntegrationStats stats = new IntegrationStats();

        foreach (MeshFilter filter in modelInstance.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            stats.Renderers += filter.GetComponent<Renderer>() != null ? 1 : 0;

            CollisionKind kind = Classify(filter.gameObject.name);
            switch (kind)
            {
                case CollisionKind.PaintableMesh:
                    EnsureMeshCollider(filter);
                    filter.gameObject.layer = paintSurfaceLayer;
                    stats.PaintableMeshColliders++;
                    break;

                case CollisionKind.SolidMesh:
                    EnsureMeshCollider(filter);
                    stats.SolidMeshColliders++;
                    break;

                case CollisionKind.SolidBox:
                    EnsureBoxCollider(filter);
                    stats.SolidBoxColliders++;
                    break;

                default:
                    stats.VisualOnly++;
                    break;
            }
        }

        foreach (Renderer renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] shared = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < shared.Length; i++)
            {
                Material source = shared[i];
                if (source == null)
                    continue;

                string key = NormalizeMaterialName(source.name);
                Material replacement;
                if (!materialMap.TryGetValue(key, out replacement) || replacement == null)
                    continue;

                if (shared[i] != replacement)
                {
                    shared[i] = replacement;
                    changed = true;
                    stats.MaterialSlotsRemapped++;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = shared;
                EditorUtility.SetDirty(renderer);
            }
        }

        return stats;
    }

    private static CollisionKind Classify(string objectName)
    {
        string name = objectName ?? string.Empty;

        // Core traversal + intentional canvas walls are Painter-addressable.
        if (Starts(name, "PLATFORM |") ||
            Starts(name, "BRIDGE |") ||
            Starts(name, "RAMP |") ||
            Starts(name, "STAIR |") ||
            Starts(name, "CANVAS_WALL |"))
        {
            return CollisionKind.PaintableMesh;
        }

        // Main cliff masses define traversal boundaries. Fractured spurs stay visual-only
        // to avoid hundreds of noisy micro-collision triangles.
        if (Starts(name, "CLIFF |") &&
            name.IndexOf("fractured spur", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return CollisionKind.SolidMesh;
        }

        // The central planter is a meaningful large obstacle.
        if (Starts(name, "ATRIUM | octagonal tree planter"))
            return CollisionKind.SolidMesh;

        // Structural detail receives cheap local-space box collision rather than a dense MeshCollider.
        if (Starts(name, "ARCHITECTURE |") ||
            Starts(name, "FRAME |") ||
            Starts(name, "COVER |") ||
            Starts(name, "SUPPLY |") ||
            ContainsStructuralToken(name))
        {
            return CollisionKind.SolidBox;
        }

        // PAINT overlays, POOL sheets/foam/lilies, WATERFALL strokes, foliage, linen drapes,
        // flowers and distant mountains remain visual-only. This is deliberate: these thin meshes
        // must not steal tool raycasts or create CharacterController snagging.
        return CollisionKind.None;
    }

    private static bool ContainsStructuralToken(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("| rail") ||
               n.Contains("| post") ||
               n.Contains("edge post") ||
               n.Contains("timber stile") ||
               n.Contains("upright") ||
               n.Contains("lintel") ||
               n.Contains("buttress") ||
               n.Contains("| diagonal") ||
               n.Contains("| body");
    }

    private static bool Starts(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureMeshCollider(MeshFilter filter)
    {
        Collider existing = filter.GetComponent<Collider>();
        if (existing != null)
            return;

        MeshCollider collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
        collider.sharedMesh = filter.sharedMesh;
        collider.convex = false;
        EditorUtility.SetDirty(collider);
    }

    private static void EnsureBoxCollider(MeshFilter filter)
    {
        Collider existing = filter.GetComponent<Collider>();
        if (existing != null)
            return;

        BoxCollider collider = Undo.AddComponent<BoxCollider>(filter.gameObject);
        Bounds meshBounds = filter.sharedMesh.bounds;
        collider.center = meshBounds.center;
        collider.size = meshBounds.size;
        EditorUtility.SetDirty(collider);
    }

    private static Dictionary<string, Material> CreateOrUpdateMaterials()
    {
        EnsureAssetFolder(MaterialFolder);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("URP/Lit veya Standard shader bulunamadı.");

        Dictionary<string, Material> result =
            new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        foreach (MaterialSpec spec in MaterialSpecs)
        {
            string path = MaterialFolder + "/" + spec.AssetName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = spec.AssetName };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", spec.Color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", spec.Color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", spec.Metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", spec.Smoothness);
            if (spec.DoubleSided && material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);

            material.doubleSidedGI = spec.DoubleSided;
            EditorUtility.SetDirty(material);
            result[spec.SourceName] = material;
        }

        return result;
    }

    private static MaterialSpec Spec(
        string source,
        string asset,
        float r,
        float g,
        float b,
        float metallic,
        float smoothness,
        bool doubleSided = false)
    {
        return new MaterialSpec
        {
            SourceName = source,
            AssetName = asset,
            Color = new Color(r, g, b, 1f),
            Metallic = metallic,
            Smoothness = smoothness,
            DoubleSided = doubleSided
        };
    }

    private static Transform ConfigureSurfaceAnchor(
        Transform anchorsRoot,
        string anchorName,
        Renderer surface,
        float lift)
    {
        Transform anchor = GetOrCreateChild(anchorsRoot, anchorName);
        if (surface != null)
        {
            Bounds b = surface.bounds;
            anchor.position = new Vector3(b.center.x, b.max.y + lift, b.center.z);
            anchor.rotation = Quaternion.identity;
        }
        return anchor;
    }

    private static bool TryMoveFigureTo(Transform anchor)
    {
        if (anchor == null)
            return false;

        Component motor = FindSceneComponentByTypeName("FigureMotor");
        if (motor == null)
            return false;

        Transform figure = motor.transform;
        CharacterController controller = figure.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        Undo.RecordObject(figure, "Move Figure to Atelier START");
        figure.position = anchor.position;

        if (controller != null)
            controller.enabled = wasEnabled;

        MethodInfo resetMotion = motor.GetType().GetMethod(
            "ResetMotion",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (resetMotion != null && resetMotion.GetParameters().Length == 0)
        {
            try { resetMotion.Invoke(motor, null); }
            catch (Exception ex) { Debug.LogWarning("[M55.4] FigureMotor.ResetMotion invoke skipped: " + ex.Message); }
        }

        EditorUtility.SetDirty(figure);
        return true;
    }

    private static Component FindSceneComponentByTypeName(string typeName)
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || !behaviour.gameObject.scene.IsValid())
                continue;
            if (string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal))
                return behaviour;
        }
        return null;
    }

    private static Renderer FindRendererByExactName(Transform root, string exactName)
    {
        if (root == null)
            return null;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && string.Equals(renderer.gameObject.name, exactName, StringComparison.Ordinal))
                return renderer;
        }
        return null;
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform existing = FindDirectChild(parent, childName);
        if (existing != null)
            return existing;

        GameObject child = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(child, "Create " + childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            return -1;

        SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null)
            return -1;

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }
        }

        return -1;
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string NormalizeMaterialName(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;

        const string instanceSuffix = " (Instance)";
        if (source.EndsWith(instanceSuffix, StringComparison.Ordinal))
            source = source.Substring(0, source.Length - instanceSuffix.Length);
        return source.Trim();
    }

    private static bool TryGetCombinedRendererBounds(GameObject root, out Bounds result)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            result = default;
            return false;
        }

        bool initialized = false;
        result = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;
            if (!initialized)
            {
                result = renderer.bounds;
                initialized = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }
        return initialized;
    }

    private static string FindLegacyGroundWarning(GameObject integrationRoot)
    {
        GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (GameObject go in all)
        {
            if (go == null || !go.scene.IsValid())
                continue;
            if (integrationRoot != null && go.transform.IsChildOf(integrationRoot.transform))
                continue;
            if (!string.Equals(go.name, "Ground", StringComparison.OrdinalIgnoreCase))
                continue;

            Collider c = go.GetComponent<Collider>();
            Renderer r = go.GetComponent<Renderer>();
            return "FOUND at " + GetHierarchyPath(go.transform) +
                   " | Collider=" + (c != null && c.enabled) +
                   " | Renderer=" + (r != null && r.enabled) +
                   " | AutomaticallyModified=False";
        }

        return "No exact legacy Ground object found.";
    }

    private static void AddCheck(
        List<string> lines,
        ref bool pass,
        bool condition,
        string label,
        string detail)
    {
        pass &= condition;
        lines.Add((condition ? "PASS" : "FAIL") + " | " + label + " | " + detail);
    }

    private static string BoundsText(Bounds b)
    {
        return "center=" + b.center.ToString("F2") + " size=" + b.size.ToString("F2");
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return "<null>";

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private struct IntegrationStats
    {
        public int Renderers;
        public int PaintableMeshColliders;
        public int SolidMeshColliders;
        public int SolidBoxColliders;
        public int VisualOnly;
        public int MaterialSlotsRemapped;
    }
}
#endif
