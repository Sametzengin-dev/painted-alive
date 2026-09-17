#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Environment.PrismaticReach;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupLivingGalleryMap_M55_8
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";

    private const string VisualModelPath =
        "Assets/_Project/Art/Models/Environments/LivingGallery/The_Living_Gallery_Visual.fbx";
    private const string CollisionModelPath =
        "Assets/_Project/Art/Models/Environments/LivingGallery/The_Living_Gallery_Collision.fbx";
    private const string GameplayModelPath =
        "Assets/_Project/Art/Models/Environments/LivingGallery/The_Living_Gallery_Gameplay.fbx";

    private const string MaterialFolder =
        "Assets/_Project/Materials/Environment/LivingGallery/Generated";

    private const string RootName = "M55_8_LivingGallery_Continuation";
    private const string VisualRootName = "The_Living_Gallery_Visual";
    private const string CollisionRootName = "The_Living_Gallery_Collision";
    private const string GameplayRootName = "The_Living_Gallery_Gameplay";
    private const string RuntimeRootName = "LivingGallery_RuntimeBindings";

    private const string PrismaticRootName = "M55_7_PrismaticReach_Continuation";
    private const string PrismaticExitName = "Route_EXIT_GATE";
    private const string EntryName = "GP_ENTRY_From_PrismaticReach";
    private const string ExitName = "GP_EXIT_To_NextRegion";

    private const string CollisionLayerName = "PrismaticCollision";
    private const string TrampolineLayerName = "Trampoline";
    private const string WaterVisualLayerName = "WaterVisual";
    private const string RetiredCollisionName = "COLLISION_ARCH_RailBarrier_08_EAST_GATE_LANDINGW0";
    private const string JourneyScoreConfigPath = "Assets/_Project/Data/Core/DA_PrototypeJourneyScore.asset";
    private const float ConnectionGap = 0f;

    private static readonly string[] RouteGuideNames =
    {
        "GP_RouteGuide_archive_interior",
        "GP_RouteGuide_concealed_archive_branch",
        "GP_RouteGuide_main_west_ink_upper",
        "GP_RouteGuide_west_interior"
    };

    private static readonly string[] AuthoredGapRecords =
    {
        "VISIBILITY_PORTALS",
        "THRESHOLD_VOLUMES",
        "INK_DETECTION",
        "GATE_ENDPOINT_TRANSFORMS",
        "PIGMENT_EFFECTS",
        "PAPER_COLLISION_STATES",
        "CANVAS_SWING_SETUP",
        "FALL_SPAWN_RESPAWN",
        "REGION_DESTINATION"
    };

    private sealed class VisibilitySpec
    {
        public readonly string Id;
        public readonly string Helper;
        public readonly float Width;
        public readonly float Depth;
        public readonly float Height;
        public readonly string[] Blockers;

        public VisibilitySpec(string id, string helper, float width, float depth, float height, params string[] blockers)
        {
            Id = id;
            Helper = helper;
            Width = width;
            Depth = depth;
            Height = height;
            Blockers = blockers ?? Array.Empty<string>();
        }
    }

    private static readonly VisibilitySpec[] VisibilitySpecs =
    {
        new VisibilitySpec(
            "VIS_WestFoyer", "GP_Cover_WestFoyer", 11f, 10f, 4.4f,
            "COVER_WestFoyer_OpaqueCeiling",
            "COVER_WestFoyer_SideWall",
            "COVER_WestFoyer_SideWall.001",
            "COVER_WestFoyer_SideWall.002",
            "COVER_WestFoyer_SideWall.003",
            "COVER_WestFoyer_SideWall.004",
            "COVER_WestFoyer_SideWall.005"),
        new VisibilitySpec(
            "VIS_WestLong", "GP_Cover_WestLong", 12f, 26f, 4.6f,
            "COVER_WestLong_OpaqueCeiling",
            "COVER_WestLong_SideWall",
            "COVER_WestLong_SideWall.001",
            "COVER_WestLong_SideWall.002",
            "COVER_WestLong_SideWall.003",
            "COVER_WestLong_SideWall.004",
            "COVER_WestLong_SideWall.005",
            "COVER_InternalScreen_WestLong_A",
            "COVER_InternalScreen_WestLong_B"),
        new VisibilitySpec(
            "VIS_HiddenArchive", "GP_Cover_HiddenArchive", 28f, 5.6f, 3.35f,
            "COVER_HiddenArchive_OpaqueCeiling",
            "COVER_HiddenArchive_SideWall",
            "COVER_HiddenArchive_SideWall.001",
            "COVER_InternalScreen_Archive_A",
            "COVER_InternalScreen_Archive_B"),
        new VisibilitySpec(
            "VIS_NorthSalon", "GP_Cover_NorthSalon", 17f, 9f, 5.1f,
            "COVER_NorthSalon_OpaqueCeiling",
            "COVER_NorthSalon_SideWall",
            "COVER_NorthSalon_SideWall.001",
            "COVER_NorthSalon_SideWall.002",
            "COVER_NorthSalon_SideWall.003")
    };

    [MenuItem(MenuRoot + "55.8 - Setup + Append Living Gallery")]
    public static void Setup()
    {
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                throw new InvalidOperationException("Aktif Unity scene bulunamadı.");

            ConfigureImporter(VisualModelPath, true);
            ConfigureImporter(CollisionModelPath, false);
            ConfigureImporter(GameplayModelPath, false);

            GameObject visualAsset = RequireModel(VisualModelPath);
            GameObject collisionAsset = RequireModel(CollisionModelPath);
            GameObject gameplayAsset = RequireModel(GameplayModelPath);

            Transform prismaticExit = FindPrismaticExit();
            if (prismaticExit == null)
                throw new InvalidOperationException(
                    "Prismatic Reach çıkışı bulunamadı. Living Gallery, Prismatic Reach'in devamı olarak bağlanıyor.");

            DestroyExistingSceneRoot(RootName);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Living Gallery continuation");
            SceneManager.MoveGameObjectToScene(root, scene);

            GameObject visual = InstantiateModel(visualAsset, root.transform, VisualRootName, scene);
            GameObject collision = InstantiateModel(collisionAsset, root.transform, CollisionRootName, scene);
            GameObject gameplay = InstantiateModel(gameplayAsset, root.transform, GameplayRootName, scene);

            int collisionLayer = LayerMask.NameToLayer(CollisionLayerName);
            int trampolineLayer = LayerMask.NameToLayer(TrampolineLayerName);
            int waterVisualLayer = LayerMask.NameToLayer(WaterVisualLayerName);

            VisualStats visualStats = ConfigureVisual(visual, waterVisualLayer);
            CollisionStats collisionStats = ConfigureCollision(collision, collisionLayer, trampolineLayer);
            ConfigureGameplaySource(gameplay);

            // M55.8.2: JSON is the authoritative machine-readable binding/collision manifest.
            // This pass also reconstructs the 15 texture-backed URP materials from the supplied library.
            LivingGalleryManifestEditorUtility_M55_8_2.Report manifestReport =
                LivingGalleryManifestEditorUtility_M55_8_2.Apply(
                    visual.transform, collision.transform, gameplay.transform, true);
            if (!manifestReport.Pass)
                throw new InvalidOperationException(
                    "Living Gallery manifest/texture validation failed.\n" +
                    LivingGalleryManifestEditorUtility_M55_8_2.Format(manifestReport));

            Transform entry = RequireUnique(gameplay.transform, EntryName);
            Transform exit = RequireUnique(gameplay.transform, ExitName);
            AlignContinuationRoot(root.transform, entry, prismaticExit, ConnectionGap);

            LivingGalleryAnchor entryAnchor = GetOrAdd<LivingGalleryAnchor>(entry.gameObject);
            entryAnchor.Configure(LivingGalleryAnchorKind.Entry, EntryName, "Incoming transition from Prismatic Reach; not an authored spawn.");
            LivingGalleryAnchor exitAnchor = GetOrAdd<LivingGalleryAnchor>(exit.gameObject);
            exitAnchor.Configure(LivingGalleryAnchorKind.Exit, ExitName, "Outgoing Living Gallery transition; next region ID is not authored yet.");

            Transform[] routeGuides = RouteGuideNames
                .Select(name => FindUnique(gameplay.transform, name, false))
                .Where(t => t != null)
                .ToArray();
            foreach (Transform route in routeGuides)
            {
                GetOrAdd<LivingGalleryAnchor>(route.gameObject)
                    .Configure(LivingGalleryAnchorKind.RouteGuide, route.name, "Authored route-reference data; not an automatic NavMesh link.");
            }

            GameObject runtime = new GameObject(RuntimeRootName);
            Undo.RegisterCreatedObjectUndo(runtime, "Create Living Gallery runtime bindings");
            runtime.transform.SetParent(root.transform, false);

            LivingGalleryGameplayBinder binder = GetOrAdd<LivingGalleryGameplayBinder>(root);
            binder.Configure(
                root.transform,
                visual.transform,
                collision.transform,
                gameplay.transform,
                entry,
                exit,
                routeGuides,
                AuthoredGapRecords);

            GetOrAdd<LivingGalleryNetworkState>(root);

            int trampolineCount = BuildTrampolines(runtime.transform, gameplay.transform, collision.transform);
            int visibilityCount = BuildVisibility(runtime.transform, visual.transform, gameplay.transform);
            int partialCount = BuildPartialVisibility(visual.transform, gameplay.transform);
            int movingCanvasCount = BuildMovingCanvases(visual.transform, gameplay.transform);
            int gateCount = BuildFrameGates(runtime.transform, visual.transform, collision.transform, gameplay.transform);
            int paperCount = BuildDryingPapers(runtime.transform, visual.transform, collision.transform);
            int pigmentCount = BuildPigmentFlow(runtime.transform, visual.transform, gameplay.transform);
            int inkZoneCount = BuildInkSystem(runtime.transform, visual.transform, gameplay.transform);

            bool journeyRebound = RebindJourneyFinish(exit);

            // M55.8.3: keep the source manifest immutable, but explicitly close
            // its nine Unity-side gaps with derived geometry / conservative
            // runtime policy. This is provenance-safe: source-authored gaps remain
            // visible in the JSON while the scene receives explicit Unity bindings.
            LivingGalleryGapClosureEditorUtility_M55_8_3.Report gapClosureReport =
                LivingGalleryGapClosureEditorUtility_M55_8_3.Apply(
                    root.transform,
                    true);
            if (!gapClosureReport.Pass)
                throw new InvalidOperationException(
                    "Living Gallery authored-gap closure failed.\n" +
                    LivingGalleryGapClosureEditorUtility_M55_8_3.Format(
                        gapClosureReport));

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            float seamDistance = Vector3.Distance(entry.position, prismaticExit.position + prismaticExit.forward * ConnectionGap);
            float seamAngle = Quaternion.Angle(entry.rotation, prismaticExit.rotation);

            string summary =
                "Living Gallery, mevcut Prismatic Reach'in DEVAMINA eklendi.\n\n" +
                $"Connection seam: {seamDistance:F3} m / {seamAngle:F2} deg\n" +
                $"Visual renderers: {visualStats.Renderers}\n" +
                $"Visual colliders remaining: {visualStats.CollidersRemaining}\n" +
                $"Active collision proxies: {collisionStats.ActiveColliders}\n" +
                $"Retired collision skipped: {collisionStats.RetiredSkipped}\n" +
                $"Enabled collision renderers: {collisionStats.EnabledRenderers}\n" +
                $"Trampolines: {trampolineCount}/2\n" +
                $"Base hidden visibility volumes: {visibilityCount}/4 (+2 Unity-derived threshold volumes in M55.8.3)\n" +
                $"Partial visibility surfaces: {partialCount}/9\n" +
                $"Moving canvases: {movingCanvasCount}/2\n" +
                $"Frame gates: {gateCount}/3\n" +
                $"Drying papers: {paperCount}/2\n" +
                $"Pigment channels: {pigmentCount}/3\n" +
                $"Ink reflection zones: {inkZoneCount}/3\n" +
                $"Journey/exit rebound: {journeyRebound}\n" +
                $"Manifest scene bindings: {manifestReport.SceneBoundRecordsResolved}/{manifestReport.SceneBoundRecordsExpected}\n" +
                $"Manifest active colliders: {manifestReport.ActiveCollidersConfigured}/436\n" +
                $"Texture files: {manifestReport.TextureFilesFound}/{manifestReport.TextureFilesExpected}\n" +
                $"Textured URP materials: {manifestReport.TexturedMaterialsConfigured}/15\n" +
                $"Unity-resolved source gaps: {gapClosureReport.UnityResolvedGaps}/9\n" +
                $"Final gap-closure result: {(gapClosureReport.Pass ? "PASS" : "NEEDS_ATTENTION")}\n\n" +
                "Not: JSON kaynak gerçekliği değişmedi; dokuz kayıt hâlâ source-authored gap olarak kalır. " +
                "M55.8.3 bunları Unity-side türetilmiş geometri veya açık konservatif policy ile çözer; " +
                "Blender'da varmış gibi yeniden etiketlemez.";

            Debug.Log("[M55.8 Living Gallery] " + summary.Replace("\n", " | "), root);
            EditorUtility.DisplayDialog("M55.8 Living Gallery Setup", summary, "Tamam");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("M55.8 Living Gallery Setup Hatası", ex.Message, "Tamam");
        }
    }

    [MenuItem(MenuRoot + "55.8 - Diagnose Living Gallery")]
    public static void Diagnose()
    {
        GameObject root = FindSceneObjectByName(RootName);
        Transform visual = root != null ? FindUnique(root.transform, VisualRootName, false) : null;
        Transform collision = root != null ? FindUnique(root.transform, CollisionRootName, false) : null;
        Transform gameplay = root != null ? FindUnique(root.transform, GameplayRootName, false) : null;
        Transform entry = gameplay != null ? FindUnique(gameplay, EntryName, false) : null;
        Transform exit = gameplay != null ? FindUnique(gameplay, ExitName, false) : null;
        Transform prismaticExit = FindPrismaticExit();

        LivingGalleryManifestEditorUtility_M55_8_2.Report manifestReport =
            LivingGalleryManifestEditorUtility_M55_8_2.Apply(visual, collision, gameplay, false);

        LivingGalleryGapClosureEditorUtility_M55_8_3.Report gapClosureReport =
            root != null
                ? LivingGalleryGapClosureEditorUtility_M55_8_3.Apply(
                    root.transform,
                    false)
                : new LivingGalleryGapClosureEditorUtility_M55_8_3.Report
                {
                    HardErrors = 1
                };

        int visualColliders = visual != null ? visual.GetComponentsInChildren<Collider>(true).Length : -1;
        int enabledCollisionRenderers = collision != null
            ? collision.GetComponentsInChildren<Renderer>(true).Count(r => r != null && r.enabled)
            : -1;
        int activeCollisionComponents = collision != null
            ? collision.GetComponentsInChildren<Collider>(true).Count(c => c != null && c.enabled && c.gameObject.activeInHierarchy)
            : -1;
        Transform retired = collision != null
            ? FindUnique(collision, RetiredCollisionName, false)
            : null;
        bool retiredSafe = retired == null || !retired.gameObject.activeSelf;

        int trampolines = root != null ? root.GetComponentsInChildren<PrismaticReachTrampolineSurface>(true).Length : 0;
        int visVolumes = root != null ? root.GetComponentsInChildren<PainterVisibilityVolume>(true).Length : 0;
        int partial = root != null ? root.GetComponentsInChildren<PainterPartialVisibilitySurface>(true).Length : 0;
        int moving = root != null ? root.GetComponentsInChildren<MovingCanvasObstacle>(true).Length : 0;
        int gates = root != null ? root.GetComponentsInChildren<RotatingFrameGate>(true).Length : 0;
        int papers = root != null ? root.GetComponentsInChildren<DryingPaperSurface>(true).Length : 0;
        int pigments = root != null ? root.GetComponentsInChildren<PigmentChannel>(true).Length : 0;
        int zones = root != null ? root.GetComponentsInChildren<InkReflectionZone>(true).Length : 0;

        float seamDistance = entry != null && prismaticExit != null
            ? Vector3.Distance(entry.position, prismaticExit.position + prismaticExit.forward * ConnectionGap)
            : -1f;
        float seamAngle = entry != null && prismaticExit != null
            ? Quaternion.Angle(entry.rotation, prismaticExit.rotation)
            : -1f;

        bool corePass = root != null && visual != null && collision != null && gameplay != null &&
                        entry != null && exit != null && prismaticExit != null &&
                        seamDistance >= 0f && seamDistance <= 0.03f &&
                        seamAngle >= 0f && seamAngle <= 0.75f &&
                        visualColliders == 0 && enabledCollisionRenderers == 0 && retiredSafe &&
                        trampolines == 2 && visVolumes == 6 && partial == 9 && moving == 2 &&
                        gates == 3 && papers == 2 && pigments == 3 && zones == 3 &&
                        manifestReport.Pass && gapClosureReport.Pass;

        string report =
            "Painted Alive M55.8 — Living Gallery Diagnostics\n" +
            "Result=" + (corePass ? "PASS" : "NEEDS_ATTENTION") + "\n\n" +
            $"Root={(root != null)}\n" +
            $"VisualRoot={(visual != null)}\n" +
            $"CollisionRoot={(collision != null)}\n" +
            $"GameplayRoot={(gameplay != null)}\n" +
            $"EntryAnchor={(entry != null)}\n" +
            $"ExitAnchor={(exit != null)}\n" +
            $"PrismaticExit={(prismaticExit != null)}\n" +
            $"ContinuationSeamDistance={seamDistance:F3}m\n" +
            $"ContinuationSeamAngle={seamAngle:F2}deg\n" +
            $"VisualColliders={visualColliders} expected=0\n" +
            $"EnabledCollisionRenderers={enabledCollisionRenderers} expected=0\n" +
            $"ActiveCollisionComponents={activeCollisionComponents} expected=436\n" +
            $"RetiredProxyAbsentOrInactive={retiredSafe}\n" +
            $"Trampolines={trampolines}/2\n" +
            $"VisibilityVolumes={visVolumes}/6 (4 authored-helper + 2 Unity-derived threshold)\n" +
            $"PartialSurfaces={partial}/9\n" +
            $"MovingCanvases={moving}/2\n" +
            $"FrameGates={gates}/3\n" +
            $"DryingPapers={papers}/2\n" +
            $"PigmentChannels={pigments}/3\n" +
            $"InkReflectionZones={zones}/3\n" +
            $"ManifestBindings={manifestReport.SceneBoundRecordsResolved}/{manifestReport.SceneBoundRecordsExpected}\n" +
            $"ManifestActiveColliders={manifestReport.ActiveCollidersConfigured}/436\n" +
            $"ManifestRetiredDisabled={manifestReport.RetiredCollidersDisabled}/1\n" +
            $"TextureFiles={manifestReport.TextureFilesFound}/{manifestReport.TextureFilesExpected}\n" +
            $"TexturedMaterials={manifestReport.TexturedMaterialsConfigured}/15\n" +
            $"SourceAuthoredGaps={gapClosureReport.SourceGaps}\n" +
            $"UnityResolvedGaps={gapClosureReport.UnityResolvedGaps}/9\n" +
            $"GapClosureHardErrors={gapClosureReport.HardErrors}\n\n" +
            "Source manifest remains authoritative and still records the original nine authored gaps. " +
            "M55.8.3 resolves each one explicitly on the Unity side without pretending the missing Blender data existed.\n\n" +
            LivingGalleryGapClosureEditorUtility_M55_8_3.Format(gapClosureReport);

        Debug.Log("[M55.8 Living Gallery Diagnose]\n" + report, root);
        EditorUtility.DisplayDialog("M55.8 Living Gallery Diagnose", report, "Tamam");
    }

    [MenuItem(MenuRoot + "55.8.1 - Realign Living Gallery Continuation")]
    public static void Realign()
    {
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
            throw new InvalidOperationException("Living Gallery root bulunamadı.");
        Transform gameplay = FindUnique(root.transform, GameplayRootName, true);
        Transform entry = RequireUnique(gameplay, EntryName);
        Transform prismaticExit = FindPrismaticExit();
        if (prismaticExit == null)
            throw new InvalidOperationException("Prismatic Reach exit bulunamadı.");
        Undo.RecordObject(root.transform, "Realign Living Gallery");
        AlignContinuationRoot(root.transform, entry, prismaticExit, ConnectionGap);
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    [MenuItem(MenuRoot + "55.8.1 - Rebind Journey Exit To Living Gallery")]
    public static void RebindExit()
    {
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
            throw new InvalidOperationException("Living Gallery root bulunamadı.");
        Transform gameplay = FindUnique(root.transform, GameplayRootName, true);
        Transform exit = RequireUnique(gameplay, ExitName);
        bool changed = RebindJourneyFinish(exit);
        EditorUtility.DisplayDialog("M55.8 Rebind", "Journey exit rebound=" + changed, "Tamam");
    }

    [MenuItem(MenuRoot + "55.8.1 - Move Figure To Living Gallery Entry (Test Only)")]
    public static void MoveFigureToEntry()
    {
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
            throw new InvalidOperationException("Living Gallery root bulunamadı.");
        Transform gameplay = FindUnique(root.transform, GameplayRootName, true);
        Transform entry = RequireUnique(gameplay, EntryName);
        FigureMotor motor = FindSceneComponent<FigureMotor>();
        if (motor == null)
            throw new InvalidOperationException("FigureMotor bulunamadı.");
        Undo.RecordObject(motor.transform, "Move Figure to Living Gallery entry");
        motor.Teleport(entry.position + Vector3.up * 0.05f, entry.rotation);
        EditorUtility.SetDirty(motor.transform);
    }

    [MenuItem(MenuRoot + "55.8.1 - Select Living Gallery Root")]
    public static void SelectRoot()
    {
        GameObject root = FindSceneObjectByName(RootName);
        Selection.activeGameObject = root;
        if (root != null)
            EditorGUIUtility.PingObject(root);
    }

    private static VisualStats ConfigureVisual(GameObject visualRoot, int waterLayer)
    {
        VisualStats stats = new VisualStats();

        foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);
        }

        foreach (Camera camera in visualRoot.GetComponentsInChildren<Camera>(true))
            if (camera != null) camera.enabled = false;
        foreach (Light light in visualRoot.GetComponentsInChildren<Light>(true))
            if (light != null) light.enabled = false;

        Dictionary<Material, Material> materialMap = new Dictionary<Material, Material>();
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        stats.Renderers = renderers.Length;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string n = renderer.gameObject.name;
            if (IsEditorReviewObject(n))
            {
                renderer.enabled = false;
                continue;
            }

            if (n.IndexOf("INK_POOL_Surface", StringComparison.OrdinalIgnoreCase) >= 0 && waterLayer >= 0)
                renderer.gameObject.layer = waterLayer;

            Material[] sources = renderer.sharedMaterials;
            Material[] replacements = new Material[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                Material source = sources[i];
                if (source == null)
                    continue;
                if (!materialMap.TryGetValue(source, out Material replacement))
                {
                    replacement = CreateOrUpdateUrpMaterial(source);
                    materialMap.Add(source, replacement);
                }
                replacements[i] = replacement;
            }
            renderer.sharedMaterials = replacements;
            EditorUtility.SetDirty(renderer);
        }

        stats.CollidersRemaining = visualRoot.GetComponentsInChildren<Collider>(true).Length;
        return stats;
    }

    private static CollisionStats ConfigureCollision(GameObject collisionRoot, int collisionLayer, int trampolineLayer)
    {
        CollisionStats stats = new CollisionStats();

        foreach (Renderer renderer in collisionRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        MeshFilter[] filters = collisionRoot.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            GameObject go = filter.gameObject;
            if (string.Equals(go.name, RetiredCollisionName, StringComparison.Ordinal))
            {
                go.SetActive(false);
                stats.RetiredSkipped++;
                continue;
            }

            if (collisionLayer >= 0)
                go.layer = collisionLayer;
            if (IsTrampolineCollision(go.name) && trampolineLayer >= 0)
                go.layer = trampolineLayer;

            MeshCollider meshCollider = go.GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = Undo.AddComponent<MeshCollider>(go);
            meshCollider.sharedMesh = filter.sharedMesh;
            meshCollider.enabled = true;
            meshCollider.isTrigger = false;
            meshCollider.convex = IsMovingGateCollision(go.name);
            EditorUtility.SetDirty(meshCollider);
            stats.ActiveColliders++;
        }

        for (int i = 1; i <= 3; i++)
        {
            Transform gateRoot = FindUnique(collisionRoot.transform, $"INT_FrameGate_0{i}", false);
            if (gateRoot == null) continue;
            Rigidbody rb = gateRoot.GetComponent<Rigidbody>();
            if (rb == null) rb = Undo.AddComponent<Rigidbody>(gateRoot.gameObject);
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        stats.EnabledRenderers = collisionRoot.GetComponentsInChildren<Renderer>(true).Count(r => r.enabled);
        return stats;
    }

    private static void ConfigureGameplaySource(GameObject gameplayRoot)
    {
        foreach (Renderer renderer in gameplayRoot.GetComponentsInChildren<Renderer>(true))
            if (renderer != null) renderer.enabled = false;
        foreach (Collider collider in gameplayRoot.GetComponentsInChildren<Collider>(true))
            if (collider != null) collider.enabled = false;
        foreach (Camera camera in gameplayRoot.GetComponentsInChildren<Camera>(true))
            if (camera != null) camera.enabled = false;
    }

    private static int BuildTrampolines(Transform runtimeRoot, Transform gameplayRoot, Transform collisionRoot)
    {
        GameObject group = NewChild(runtimeRoot, "Trampolines");
        int count = 0;
        count += BuildTrampoline(group.transform, "BOUNCE_WEST", gameplayRoot, collisionRoot,
            "TRAMPOLINE_WEST_CENTER", "TRAMPOLINE_WEST_LANDING", "COLLISION_INT_TensionCanvas_WEST") ? 1 : 0;
        count += BuildTrampoline(group.transform, "BOUNCE_EAST", gameplayRoot, collisionRoot,
            "TRAMPOLINE_EAST_CENTER", "TRAMPOLINE_EAST_LANDING", "COLLISION_INT_TensionCanvas_EAST") ? 1 : 0;
        return count;
    }

    private static bool BuildTrampoline(
        Transform parent,
        string id,
        Transform gameplayRoot,
        Transform collisionRoot,
        string centerName,
        string landingName,
        string collisionName)
    {
        Transform center = FindUnique(gameplayRoot, centerName, false);
        Transform landing = FindUnique(gameplayRoot, landingName, false);
        Transform collisionTransform = FindUnique(collisionRoot, collisionName, false);
        Collider collider = collisionTransform != null ? collisionTransform.GetComponent<Collider>() : null;
        if (center == null || landing == null || collider == null)
        {
            Debug.LogWarning($"[M55.8] {id} trampoline binding eksik.");
            return false;
        }

        GameObject go = NewChild(parent, id);
        PrismaticReachTrampolineSurface surface = Undo.AddComponent<PrismaticReachTrampolineSurface>(go);
        surface.Configure(center, landing, new[] { collider }, 3.0f, 1.5f, 0.30f, 0.18f, 0.65f, 0.25f);
        EditorUtility.SetDirty(surface);
        return true;
    }

    private static int BuildVisibility(Transform runtimeRoot, Transform visualRoot, Transform gameplayRoot)
    {
        int count = 0;
        foreach (VisibilitySpec spec in VisibilitySpecs)
        {
            Transform helper = FindUnique(gameplayRoot, spec.Helper, false);
            if (helper == null)
            {
                Debug.LogWarning("[M55.8] Visibility helper missing: " + spec.Helper);
                continue;
            }

            GameObject volume = NewChild(helper, "UNITY_" + spec.Id);
            float localVerticalOffset = spec.Height * 0.5f - 1.8f;
            volume.transform.localPosition = new Vector3(0f, localVerticalOffset, 0f);
            volume.transform.localRotation = Quaternion.identity;
            volume.transform.localScale = Vector3.one;

            BoxCollider box = Undo.AddComponent<BoxCollider>(volume);
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(spec.Width, spec.Height, spec.Depth);

            Rigidbody rb = Undo.AddComponent<Rigidbody>(volume);
            rb.isKinematic = true;
            rb.useGravity = false;

            PainterVisibilityVolume visibility = Undo.AddComponent<PainterVisibilityVolume>(volume);
            visibility.Configure(spec.Id, PainterFigureVisibility.Hidden, box);

            Transform[] blockers = spec.Blockers
                .Select(name => FindUnique(visualRoot, name, false))
                .Where(t => t != null)
                .ToArray();
            PainterOcclusionRegion region = Undo.AddComponent<PainterOcclusionRegion>(volume);
            region.Configure(spec.Id, blockers);
            count++;
        }
        return count;
    }

    private static int BuildPartialVisibility(Transform visualRoot, Transform gameplayRoot)
    {
        string[] names =
        {
            "CANVAS_PaintCurtain_01",
            "CANVAS_PaintCurtain_03",
            "CANVAS_PaintCurtain_05",
            "CANVAS_PaintCurtain_OchreDryingScreen",
            "CANVAS_PaintCurtain_OchreDryingWash",
            "CANVAS_PaintCurtain_Wash01",
            "CANVAS_PaintCurtain_Wash03",
            "INT_HangingWetCanvas_02",
            "INT_HangingWetCanvas_04"
        };

        Dictionary<string, string> pivots = new Dictionary<string, string>
        {
            { "CANVAS_PaintCurtain_01", "GP_CanvasPivot_01" },
            { "CANVAS_PaintCurtain_03", "GP_CanvasPivot_03" },
            { "CANVAS_PaintCurtain_05", "GP_CanvasPivot_05" },
            { "CANVAS_PaintCurtain_OchreDryingScreen", "GP_CanvasPivot_OchreDryingScreen" },
            { "CANVAS_PaintCurtain_OchreDryingWash", "GP_CanvasPivot_OchreDryingWash" },
            { "CANVAS_PaintCurtain_Wash01", "GP_CanvasPivot_Wash01" },
            { "CANVAS_PaintCurtain_Wash03", "GP_CanvasPivot_Wash03" }
        };

        int count = 0;
        foreach (string name in names)
        {
            Transform t = FindUnique(visualRoot, name, false);
            if (t == null) continue;
            Renderer renderer = t.GetComponent<Renderer>() ?? t.GetComponentInChildren<Renderer>(true);
            if (renderer == null) continue;
            PainterPartialVisibilitySurface partial = GetOrAdd<PainterPartialVisibilitySurface>(t.gameObject);
            partial.Configure("PARTIAL_" + name, renderer, 0.35f);

            if (pivots.TryGetValue(name, out string pivotName))
            {
                Transform pivot = FindUnique(gameplayRoot, pivotName, false);
                PaintCurtainVisibilitySurface curtain = GetOrAdd<PaintCurtainVisibilitySurface>(t.gameObject);
                curtain.Configure("CURTAIN_" + name, partial, pivot);
            }
            count++;
        }
        return count;
    }

    private static int BuildMovingCanvases(Transform visualRoot, Transform gameplayRoot)
    {
        string[] visualNames = { "INT_HangingWetCanvas_02", "INT_HangingWetCanvas_04" };
        string[] pivotNames = { "GP_CanvasPivot_02", "GP_CanvasPivot_04" };
        int count = 0;
        for (int i = 0; i < visualNames.Length; i++)
        {
            Transform visual = FindUnique(visualRoot, visualNames[i], false);
            Transform pivot = FindUnique(gameplayRoot, pivotNames[i], false);
            if (visual == null || pivot == null) continue;
            PainterPartialVisibilitySurface partial = visual.GetComponent<PainterPartialVisibilitySurface>();
            MovingCanvasObstacle moving = GetOrAdd<MovingCanvasObstacle>(visual.gameObject);
            moving.Configure("MOVING_CANVAS_0" + (i == 0 ? "2" : "4"), visual, pivot, 10f, partial);
            count++;
        }
        return count;
    }

    private static int BuildFrameGates(Transform runtimeRoot, Transform visualRoot, Transform collisionRoot, Transform gameplayRoot)
    {
        GameObject group = NewChild(runtimeRoot, "FrameGates");
        int count = 0;
        float[] heights = { 6.0f, 6.1f, 5.7f };
        for (int i = 1; i <= 3; i++)
        {
            string suffix = $"0{i}";
            Transform v = FindUnique(visualRoot, "INT_FrameGate_" + suffix, false);
            Transform c = FindUnique(collisionRoot, "INT_FrameGate_" + suffix, false);
            Transform pivot = FindUnique(gameplayRoot, "GP_FrameGate_" + suffix + "_Pivot", false);
            Transform clearance = FindUnique(gameplayRoot, "GP_FrameGate_" + suffix + "_Clearance", false);
            Transform movingCollision = FindUnique(collisionRoot, "COLLISION_INT_FrameGate_" + suffix + "_OpaqueArtwork", false);
            Collider collider = movingCollision != null ? movingCollision.GetComponent<Collider>() : null;
            if (v == null || c == null || pivot == null || clearance == null || collider == null)
            {
                Debug.LogWarning("[M55.8] FRAME_" + suffix + " binding eksik.");
                continue;
            }
            GameObject go = NewChild(group.transform, "FRAME_" + suffix);
            RotatingFrameGate gate = Undo.AddComponent<RotatingFrameGate>(go);
            gate.Configure("FRAME_" + suffix, v, c, pivot, clearance, collider, 0f, 90f, 1.5f, 1.25f, 2.54f, heights[i - 1]);
            count++;
        }
        return count;
    }

    private static int BuildDryingPapers(Transform runtimeRoot, Transform visualRoot, Transform collisionRoot)
    {
        GameObject group = NewChild(runtimeRoot, "DryingPapers");
        Material wet = LoadGeneratedMaterialBySourceName("MAT_Paper_Wet");
        Material dry = LoadGeneratedMaterialBySourceName("MAT_Paper_Dry");
        int count = 0;
        count += BuildPaper(group.transform, "PAPER_01", visualRoot, collisionRoot,
            "INT_DryingPaper_01", "COLLISION_INT_DryingPaper_01", DryingPaperState.Wet, wet, dry) ? 1 : 0;
        count += BuildPaper(group.transform, "PAPER_02", visualRoot, collisionRoot,
            "INT_DryingPaper_02", "COLLISION_INT_DryingPaper_02", DryingPaperState.Dry, wet, dry) ? 1 : 0;
        return count;
    }

    private static bool BuildPaper(
        Transform parent, string id, Transform visualRoot, Transform collisionRoot,
        string visualName, string collisionName, DryingPaperState initial, Material wet, Material dry)
    {
        Transform visual = FindUnique(visualRoot, visualName, false);
        Transform collision = FindUnique(collisionRoot, collisionName, false);
        SkinnedMeshRenderer renderer = visual != null
            ? visual.GetComponent<SkinnedMeshRenderer>() ?? visual.GetComponentInChildren<SkinnedMeshRenderer>(true)
            : null;
        MeshCollider collider = collision != null ? collision.GetComponent<MeshCollider>() : null;
        if (visual == null || collision == null || collider == null)
        {
            Debug.LogWarning("[M55.8] " + id + " paper binding eksik.");
            return false;
        }
        GameObject go = NewChild(parent, id);
        DryingPaperSurface paper = Undo.AddComponent<DryingPaperSurface>(go);
        paper.Configure(id, renderer, collider, wet, dry, initial, 0.65f, 1.0f, 1.5f);
        return true;
    }

    private static int BuildPigmentFlow(Transform runtimeRoot, Transform visualRoot, Transform gameplayRoot)
    {
        GameObject group = NewChild(runtimeRoot, "PigmentRouting");
        PigmentFlowController controller = Undo.AddComponent<PigmentFlowController>(group);
        List<PigmentChannel> channels = new List<PigmentChannel>();
        string[] ids = { "VIOLET", "ULTRAMARINE", "OCHRE" };
        foreach (string id in ids)
        {
            Transform source = FindUnique(gameplayRoot, "GP_Pigment_" + id + "_SOURCE", false);
            Transform destination = FindUnique(gameplayRoot, "GP_Pigment_" + id + "_DESTINATION", false);
            Transform diverter = FindUnique(gameplayRoot, "GP_Pigment_" + id + "_DIVERTER", false);
            if (source == null || destination == null || diverter == null)
                continue;

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true)
                .Where(r => r != null &&
                    (r.gameObject.name.StartsWith("PIGMENT_" + id + "_Flow_", StringComparison.Ordinal) ||
                     string.Equals(r.gameObject.name, "PIGMENT_" + id + "_VerticalFlow", StringComparison.Ordinal) ||
                     string.Equals(r.gameObject.name, "PIGMENT_Catch_" + id, StringComparison.Ordinal)))
                .ToArray();

            GameObject go = NewChild(group.transform, "PIGMENT_" + id);
            PigmentChannel channel = Undo.AddComponent<PigmentChannel>(go);
            channel.Configure("PIGMENT_" + id, source, destination, diverter, renderers, 1.5f);
            channels.Add(channel);
        }
        controller.Configure(channels.ToArray());
        return channels.Count;
    }

    private static int BuildInkSystem(Transform runtimeRoot, Transform visualRoot, Transform gameplayRoot)
    {
        GameObject group = NewChild(runtimeRoot, "InkReflectionDeception");
        Transform center = FindUnique(gameplayRoot, "GP_InkPool_Center", false);
        Renderer surface = FindRendererByName(visualRoot, "INK_POOL_Surface");
        List<InkReflectionZone> zones = new List<InkReflectionZone>();
        string[] suffixes = { "A", "B", "C" };
        foreach (string suffix in suffixes)
        {
            Transform helper = FindUnique(gameplayRoot, "GP_InkPool_ReflectionZone_" + suffix, false);
            if (helper == null) continue;
            InkReflectionZone zone = GetOrAdd<InkReflectionZone>(helper.gameObject);
            zone.Configure("INK_ZONE_" + suffix, 3.3f);
            zones.Add(zone);
        }
        InkReflectionDecoySystem system = Undo.AddComponent<InkReflectionDecoySystem>(group);
        system.Configure(surface, center, zones.ToArray());
        return zones.Count;
    }

    private static void AlignContinuationRoot(Transform root, Transform entry, Transform target, float gap)
    {
        if (root == null || entry == null || target == null)
            throw new ArgumentNullException("Living Gallery continuation alignment requires root, entry and target.");

        Vector3 entryRelativePosition = root.InverseTransformPoint(entry.position);
        Quaternion entryRelativeRotation = Quaternion.Inverse(root.rotation) * entry.rotation;
        Quaternion desiredRootRotation = target.rotation * Quaternion.Inverse(entryRelativeRotation);
        Vector3 desiredEntryPosition = target.position + target.forward * gap;
        Vector3 desiredRootPosition = desiredEntryPosition - desiredRootRotation * entryRelativePosition;
        root.SetPositionAndRotation(desiredRootPosition, desiredRootRotation);
    }

    private static Transform FindPrismaticExit()
    {
        GameObject prismaticRoot = FindSceneObjectByName(PrismaticRootName);
        if (prismaticRoot != null)
        {
            Transform exit = FindUnique(prismaticRoot.transform, PrismaticExitName, false);
            if (exit != null) return exit;
            Transform binding = FindUnique(prismaticRoot.transform, "PR_ANCHOR_EXIT_GATE", false);
            if (binding != null) return binding;
        }
        GameObject fallback = FindSceneObjectByName(PrismaticExitName);
        return fallback != null ? fallback.transform : null;
    }

    private static bool RebindJourneyFinish(Transform finish)
    {
        if (finish == null)
            return false;
        bool changed = false;

        GameObject m54Exit = FindSceneObjectByName("M54_FrameExit");
        if (m54Exit != null)
        {
            Undo.RecordObject(m54Exit.transform, "Move M54 exit to Living Gallery final");
            m54Exit.transform.SetPositionAndRotation(finish.position, finish.rotation);
            EditorUtility.SetDirty(m54Exit.transform);
            changed = true;
        }

        PrototypeFrameExitGate gate = FindSceneComponent<PrototypeFrameExitGate>();
        if (gate != null)
        {
            Undo.RecordObject(gate.transform, "Move frame exit gate to Living Gallery final");
            gate.transform.SetPositionAndRotation(finish.position, finish.rotation);
            BoxCollider trigger = gate.GetComponent<BoxCollider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
                EditorUtility.SetDirty(trigger);
            }
            EditorUtility.SetDirty(gate.transform);
            changed = true;
        }

        PrototypeJourneyScoreTracker tracker = FindSceneComponent<PrototypeJourneyScoreTracker>();
        if (tracker != null)
        {
            FigureClarityState figure = tracker.Figure != null ? tracker.Figure : FindSceneComponent<FigureClarityState>();
            Transform start = tracker.RouteStart != null ? tracker.RouteStart : FindSceneObjectByName("Journey_Start")?.transform;
            PrototypeJourneyScoreConfig config = AssetDatabase.LoadAssetAtPath<PrototypeJourneyScoreConfig>(JourneyScoreConfigPath);
            if (figure != null && start != null && config != null)
            {
                Undo.RecordObject(tracker, "Extend journey to Living Gallery final");
                tracker.Configure(figure, start, finish, config);
                EditorUtility.SetDirty(tracker);
                changed = true;
            }
        }
        return changed;
    }

    private static Material CreateOrUpdateUrpMaterial(Material source)
    {
        EnsureFolder(MaterialFolder);
        string safe = SanitizeFileName(source.name);
        string path = MaterialFolder + "/MAT_LG_" + safe + ".mat";
        Material target = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (target == null)
        {
            target = new Material(shader != null ? shader : source.shader);
            AssetDatabase.CreateAsset(target, path);
        }
        else if (shader != null)
        {
            target.shader = shader;
        }

        Color color = GetMaterialColor(source.name, source);
        if (target.HasProperty("_BaseColor")) target.SetColor("_BaseColor", color);
        if (target.HasProperty("_Color")) target.SetColor("_Color", color);

        GetMaterialSurfaceConstants(source.name, out float metallic, out float smoothness);
        if (target.HasProperty("_Metallic")) target.SetFloat("_Metallic", metallic);
        if (target.HasProperty("_Smoothness")) target.SetFloat("_Smoothness", smoothness);

        if (source.name.IndexOf("Light | warm frosted glass", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Color emission = new Color(1f, 0.57f, 0.24f, 1f) * 3f;
            if (target.HasProperty("_EmissionColor")) target.SetColor("_EmissionColor", emission);
            target.EnableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(target);
        return target;
    }

    private static Material LoadGeneratedMaterialBySourceName(string sourceName)
    {
        string path = MaterialFolder + "/MAT_LG_" + SanitizeFileName(sourceName) + ".mat";
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static Color GetMaterialColor(string name, Material source)
    {
        if (name.Contains("MAT_Brass_Aged")) return new Color(0.306f, 0.302f, 0.161f, 1f);
        if (name.Contains("MAT_Canvas_Raw")) return new Color(0.773f, 0.698f, 0.549f, 1f);
        if (name.Contains("MAT_Canvas_Stained")) return new Color(0.592f, 0.518f, 0.396f, 1f);
        if (name.Contains("MAT_Ink_Black")) return new Color(0.028f, 0.043f, 0.051f, 1f);
        if (name.Contains("MAT_Iron_Dark")) return new Color(0.145f, 0.157f, 0.141f, 1f);
        if (name.Contains("MAT_Ivy_Muted")) return new Color(0.067f, 0.085f, 0.041f, 1f);
        if (name.Contains("MAT_Paper_Dry")) return new Color(0.635f, 0.569f, 0.447f, 1f);
        if (name.Contains("MAT_Paper_Wet")) return new Color(0.404f, 0.384f, 0.329f, 1f);
        if (name.Contains("MAT_Pigment_Blue")) return new Color(0.114f, 0.208f, 0.435f, 1f);
        if (name.Contains("MAT_Pigment_Ochre")) return new Color(0.604f, 0.333f, 0.161f, 1f);
        if (name.Contains("MAT_Pigment_Violet")) return new Color(0.271f, 0.173f, 0.306f, 1f);
        if (name.Contains("MAT_Plaster_Aged")) return new Color(0.667f, 0.608f, 0.506f, 1f);
        if (name.Contains("MAT_Plaster_Dirty")) return new Color(0.459f, 0.428f, 0.357f, 1f);
        if (name.Contains("MAT_Stone_Gallery")) return new Color(0.478f, 0.451f, 0.396f, 1f);
        if (name.Contains("MAT_Timber_Dark")) return new Color(0.212f, 0.145f, 0.106f, 1f);
        if (name.Contains("MAT_Timber_Worn")) return new Color(0.439f, 0.318f, 0.204f, 1f);
        if (name.Contains("Figure | graphite")) return new Color(0.034f, 0.041f, 0.048f, 1f);
        if (name.Contains("Ink | reflection etching")) return new Color(0.055f, 0.066f, 0.072f, 1f);
        if (name.Contains("Light | warm frosted glass")) return new Color(0.9f, 0.51f, 0.18f, 1f);
        if (name.Contains("Stage | charcoal")) return new Color(0.025f, 0.032f, 0.041f, 1f);
        if (source != null)
        {
            if (source.HasProperty("_BaseColor")) return source.GetColor("_BaseColor");
            if (source.HasProperty("_Color")) return source.GetColor("_Color");
        }
        return Color.gray;
    }

    private static void GetMaterialSurfaceConstants(string name, out float metallic, out float smoothness)
    {
        metallic = 0f;
        smoothness = 0.35f;
        if (name.Contains("Ink | reflection etching")) { metallic = 0.5f; smoothness = 0.68f; }
        else if (name.Contains("Figure | graphite")) smoothness = 0.48f;
        else if (name.Contains("Light | warm frosted glass")) smoothness = 0.6f;
        else if (name.Contains("MAT_Ivy_Muted")) smoothness = 0.14f;
        else if (name.Contains("Stage | charcoal")) smoothness = 0.05f;
    }

    private static Renderer FindRendererByName(Transform root, string name)
    {
        Transform t = FindUnique(root, name, false);
        return t != null ? t.GetComponent<Renderer>() ?? t.GetComponentInChildren<Renderer>(true) : null;
    }

    private static bool IsEditorReviewObject(string name)
    {
        return name.StartsWith("CAM_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("REFERENCE_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("CHARACTER_REFERENCE", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("CHARACTER_REVIEW", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("PREVIEW_", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "DECOR_PaintingVoid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrampolineCollision(string name) =>
        string.Equals(name, "COLLISION_INT_TensionCanvas_WEST", StringComparison.Ordinal) ||
        string.Equals(name, "COLLISION_INT_TensionCanvas_EAST", StringComparison.Ordinal);

    private static bool IsMovingGateCollision(string name) =>
        name.StartsWith("COLLISION_INT_FrameGate_", StringComparison.Ordinal) &&
        name.EndsWith("_OpaqueArtwork", StringComparison.Ordinal);

    private static void ConfigureImporter(string assetPath, bool importBlendShapes)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
        if (importer.importCameras) { importer.importCameras = false; changed = true; }
        if (importer.importLights) { importer.importLights = false; changed = true; }
        if (importer.importBlendShapes != importBlendShapes) { importer.importBlendShapes = importBlendShapes; changed = true; }
        if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            changed = true;
        }
        if (changed)
            importer.SaveAndReimport();
    }

    private static GameObject RequireModel(string path)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            throw new InvalidOperationException("FBX import edilemedi: " + path);
        return asset;
    }

    private static GameObject InstantiateModel(GameObject asset, Transform parent, string name, Scene scene)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
        if (instance == null)
            throw new InvalidOperationException(name + " instantiate edilemedi.");
        Undo.RegisterCreatedObjectUndo(instance, "Instantiate " + name);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    private static GameObject NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static Transform RequireUnique(Transform root, string name)
    {
        Transform result = FindUnique(root, name, true);
        if (result == null)
            throw new InvalidOperationException("Required Living Gallery object missing: " + name);
        return result;
    }

    private static Transform FindUnique(Transform root, string name, bool throwOnDuplicate)
    {
        if (root == null)
            return null;
        List<Transform> matches = new List<Transform>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
            if (t != null && string.Equals(t.name, name, StringComparison.Ordinal))
                matches.Add(t);
        if (matches.Count == 0)
            return null;
        if (matches.Count > 1 && throwOnDuplicate)
            throw new InvalidOperationException($"Duplicate exact-name match '{name}' under {root.name}: {matches.Count}");
        return matches[0];
    }

    private static GameObject FindSceneObjectByName(string name)
    {
        GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in all)
        {
            if (go == null || !go.scene.IsValid() || EditorUtility.IsPersistent(go))
                continue;
            if (string.Equals(go.name, name, StringComparison.Ordinal))
                return go;
        }
        return null;
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        T[] all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (T item in all)
        {
            if (item == null || !item.gameObject.scene.IsValid() || EditorUtility.IsPersistent(item))
                continue;
            return item;
        }
        return null;
    }

    private static void DestroyExistingSceneRoot(string name)
    {
        GameObject existing = FindSceneObjectByName(name);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    private static void EnsureFolder(string path)
    {
        string normalized = path.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileName(string input)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string value = input ?? "Material";
        foreach (char c in invalid)
            value = value.Replace(c, '_');
        value = value.Replace('|', '_').Replace('/', '_').Replace('\\', '_');
        return value.Trim();
    }

    private sealed class VisualStats
    {
        public int Renderers;
        public int CollidersRemaining;
    }

    private sealed class CollisionStats
    {
        public int ActiveColliders;
        public int RetiredSkipped;
        public int EnabledRenderers;
    }
}
#endif
