#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Environment.Palimpsest;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using PaintedAlive.MatchFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupPalimpsestMap_M55_9
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";

    private const string VisualModelPath =
        "Assets/_Project/Art/Models/Environments/Palimpsest/The_Palimpsest_Visual.fbx";
    private const string CollisionModelPath =
        "Assets/_Project/Art/Models/Environments/Palimpsest/The_Palimpsest_Collision.fbx";
    private const string GameplayModelPath =
        "Assets/_Project/Art/Models/Environments/Palimpsest/The_Palimpsest_Gameplay.fbx";
    private const string ManifestPath =
        "Assets/_Project/Data/Environment/Palimpsest/UNITY_BINDINGS_PALIMPSEST.json";
    private const string MaterialMappingPath =
        "Assets/_Project/Data/Environment/Palimpsest/material_mapping.json";
    private const string PaintingBindingManifestPath =
        "Assets/_Project/Data/Environment/Palimpsest/PAINTING_BINDINGS_PALIMPSEST.json";
    private const string MaterialFolder =
        "Assets/_Project/Materials/Environment/Palimpsest/Generated";
    private const string PaintingFolder =
        "Assets/_Project/Art/Textures/Environments/Palimpsest/Paintings";

    private const string RootName = "M55_9_Palimpsest_Continuation";
    private const string VisualRootName = "The_Palimpsest_Visual";
    private const string CollisionRootName = "The_Palimpsest_Collision";
    private const string GameplayRootName = "The_Palimpsest_Gameplay";
    private const string RuntimeRootName = "Palimpsest_RuntimeBindings";

    private const string LivingGalleryRootName = "M55_8_LivingGallery_Continuation";
    private const string LivingGalleryExitName = "GP_EXIT_To_NextRegion";
    private const string EntryName = "GP_ENTRY_MAIN";
    private const string ExitName = "GP_EXIT_MAIN";
    private const string CollisionLayerName = "PrismaticCollision";
    private const string JourneyScoreConfigPath = "Assets/_Project/Data/Core/DA_PrototypeJourneyScore.asset";
    private const float ConnectionGap = 0f;

    // Source validation performed against the actual three FBXs supplied with M55.9.
    // None of these seven names is a critical gameplay binding; they are package-vs-manifest visual discrepancies.
    private static readonly string[] KnownNonCriticalVisualSourceDiscrepancies =
    {
        "Fold_A_MovingEdge.003",
        "Fold_A_SafeBypass_1_Upright.004",
        "Fold_A_SafeBypass_RampCollisionCarrier_01",
        "Fold_A_TransformedPreview",
        "GreatFold_TransformedPreview",
        "INT_Perspective_Frame",
        "Smear_B_Exit_RampCollisionCarrier_00"
    };

    private static readonly string[] ExpectedPaintingFiles =
    {
        "ART_Architecture_Study_01.png",
        "ART_Landscape_01.png",
        "ART_Underpaint_Figure_01.png",
        "ART_Unfinished_Masterwork_01.png"
    };

    private static readonly string[] RouteReferenceNames =
    {
        "GP_ROUTE_01",
        "GP_ROUTE_02",
        "GP_ROUTE_03",
        "GP_ROUTE_04",
        "GP_ROUTE_05",
        "GP_ROUTE_06"
    };

    private sealed class SetupStats
    {
        public int VisualRenderers;
        public int VisualColliders;
        public int CollisionMeshes;
        public int CollisionComponents;
        public int EnabledCollisionRenderers;
        public int GeneratedMaterials;
        public int PaintingTexturesFound;
        public bool MaterialMappingPresent;
        public bool JourneyRebound;
        public bool LivingGalleryTransitionUpdated;
    }

    [MenuItem(MenuRoot + "55.9 - Setup + Append The Palimpsest")]
    public static void Setup()
    {
        try
        {
            RequireEditMode();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                throw new InvalidOperationException("Aktif Unity scene bulunamadı.");

            ValidateManifestFile();
            ConfigureImporter(VisualModelPath, true, true);
            ConfigureImporter(CollisionModelPath, false, false);
            ConfigureImporter(GameplayModelPath, false, false);

            GameObject visualAsset = RequireModel(VisualModelPath);
            GameObject collisionAsset = RequireModel(CollisionModelPath);
            GameObject gameplayAsset = RequireModel(GameplayModelPath);

            Transform livingGalleryExit = FindLivingGalleryExit();
            if (livingGalleryExit == null)
            {
                throw new InvalidOperationException(
                    "Living Gallery exit bulunamadı. The Palimpsest, M55.8 Living Gallery'nin devamı olarak bağlanıyor.");
            }

            DestroyExistingSceneRoot(RootName);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Palimpsest continuation");
            SceneManager.MoveGameObjectToScene(root, scene);

            GameObject visual = InstantiateModel(visualAsset, root.transform, VisualRootName, scene);
            GameObject collision = InstantiateModel(collisionAsset, root.transform, CollisionRootName, scene);
            GameObject gameplay = InstantiateModel(gameplayAsset, root.transform, GameplayRootName, scene);

            int collisionLayer = EnsureLayer(CollisionLayerName);
            SetupStats stats = new SetupStats();
            ConfigureVisual(visual, stats);
            ConfigureCollision(collision, collisionLayer, stats);
            ConfigureGameplaySource(gameplay);

            Transform entry = RequireUnique(gameplay.transform, EntryName);
            Transform exit = RequireUnique(gameplay.transform, ExitName);
            AlignContinuationRoot(root.transform, entry, livingGalleryExit, ConnectionGap);

            GameObject runtime = NewChild(root.transform, RuntimeRootName);

            PalimpsestNetworkState networkState = GetOrAdd<PalimpsestNetworkState>(root);
            networkState.Configure(FindSceneComponent<PrototypeCoreMatchController>());
            networkState.SetProductionTransportBound(false);

            PalimpsestClearanceVolume foldClearance = BuildClearance(gameplay.transform, "GP_Fold_A_Clearance");
            PalimpsestClearanceVolume greatFoldClearance = BuildClearance(gameplay.transform, "GP_GreatFold_Clearance");
            PalimpsestClearanceVolume smearAClearance = BuildClearance(gameplay.transform, "GP_Smear_A_Clearance");
            PalimpsestClearanceVolume smearBClearance = BuildClearance(gameplay.transform, "GP_Smear_B_Clearance");

            CanvasFoldSystem foldA = BuildFoldA(
                runtime.transform,
                visual.transform,
                collision.transform,
                gameplay.transform,
                foldClearance,
                networkState);

            CanvasFoldSystem greatFold = BuildGreatFold(
                runtime.transform,
                visual.transform,
                collision.transform,
                gameplay.transform,
                greatFoldClearance,
                networkState);

            CanvasBacksideTraceSystem backsideTrace;
            CanvasBacksideRegion backside = BuildBackside(
                runtime.transform,
                gameplay.transform,
                out backsideTrace,
                networkState);

            UnderpaintingRevealSystem underpaintA = BuildUnderpaint(
                "A", runtime.transform, visual.transform, collision.transform, gameplay.transform, networkState);
            UnderpaintingRevealSystem underpaintB = BuildUnderpaint(
                "B", runtime.transform, visual.transform, collision.transform, gameplay.transform, networkState);

            PaintSmearSurface smearA = BuildSmear(
                "A", runtime.transform, visual.transform, collision.transform, gameplay.transform,
                smearAClearance, networkState);
            PaintSmearSurface smearB = BuildSmear(
                "B", runtime.transform, visual.transform, collision.transform, gameplay.transform,
                smearBClearance, networkState);

            PerspectiveTraversalBridge perspectiveBridge;
            PerspectiveFrameSystem perspective = BuildPerspective(
                runtime.transform,
                visual.transform,
                collision.transform,
                gameplay.transform,
                networkState,
                out perspectiveBridge);

            PalimpsestClearanceVolume[] clearanceVolumes =
            {
                foldClearance,
                greatFoldClearance,
                smearAClearance,
                smearBClearance
            };

            PalimpsestRespawnCoordinator respawnCoordinator = BuildFallRespawn(
                runtime.transform,
                gameplay.transform,
                clearanceVolumes,
                collisionLayer);

            BuildPainterInteractions(
                gameplay.transform,
                foldA,
                greatFold,
                underpaintA,
                underpaintB,
                smearA,
                smearB,
                perspective,
                foldClearance,
                greatFoldClearance,
                smearAClearance,
                smearBClearance);

            Transform[] routes = RouteReferenceNames
                .Select(name => RequireUnique(gameplay.transform, name))
                .ToArray();

            PalimpsestGameplayBinder binder = GetOrAdd<PalimpsestGameplayBinder>(root);
            binder.Configure(
                root.transform,
                visual.transform,
                collision.transform,
                gameplay.transform,
                entry,
                exit,
                routes,
                foldA,
                greatFold,
                backside,
                backsideTrace,
                underpaintA,
                underpaintB,
                smearA,
                smearB,
                perspective,
                perspectiveBridge,
                respawnCoordinator,
                networkState);

            stats.LivingGalleryTransitionUpdated = UpdateLivingGalleryOutgoingTransition();
            stats.JourneyRebound = RebindJourneyFinish(exit);
            stats.PaintingTexturesFound = CountPaintingTexturesFound();
            stats.MaterialMappingPresent = AssetDatabase.LoadAssetAtPath<TextAsset>(MaterialMappingPath) != null;
            bool paintingBindingManifestPresent =
                AssetDatabase.LoadAssetAtPath<TextAsset>(PaintingBindingManifestPath) != null;

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            float seamDistance = Vector3.Distance(entry.position, livingGalleryExit.position);
            float seamAngle = Quaternion.Angle(entry.rotation, livingGalleryExit.rotation);
            string sourceAssetState =
                stats.PaintingTexturesFound == 4 && stats.MaterialMappingPresent
                    ? "COMPLETE"
                    : stats.PaintingTexturesFound == 4 && paintingBindingManifestPresent
                        ? "PAINTINGS_COMPLETE_FULL_MAPPING_FALLBACK"
                        : "SOURCE_ASSETS_MISSING";

            string summary =
                "The Palimpsest, Living Gallery'nin DEVAMINA eklendi.\n\n" +
                $"Continuation seam: {seamDistance:F3} m / {seamAngle:F2} deg\n" +
                $"Visual renderers: {stats.VisualRenderers}\n" +
                $"Visual colliders remaining: {stats.VisualColliders}\n" +
                $"Collision meshes/components: {stats.CollisionMeshes}/{stats.CollisionComponents} (expected 80/80)\n" +
                $"Enabled collision renderers: {stats.EnabledCollisionRenderers}\n" +
                $"Generated URP fallback materials: {stats.GeneratedMaterials}\n" +
                $"Painting textures supplied: {stats.PaintingTexturesFound}/4\n" +
                $"Authoritative painting binding supplied: {paintingBindingManifestPresent}\n" +
                $"Full material_mapping supplied: {stats.MaterialMappingPresent}\n" +
                $"Source asset state: {sourceAssetState}\n" +
                $"Living Gallery outgoing transition -> Palimpsest: {stats.LivingGalleryTransitionUpdated}\n" +
                $"Journey/exit rebound to GP_EXIT_MAIN: {stats.JourneyRebound}\n" +
                "Gameplay: Fold A, Great Fold, Backside Works, Underpaint A/B, Smear A/B, Perspective, Fall/Respawn and Painter interaction adapters configured.\n\n" +
                "Not: Full material_mapping mevcutsa M55.9.3 exact URP reconstruction kullanılır. " +
                "CUSTOM_SHADER_REQUIRED yalnız güvenli geçici URP/Lit baseline alır; " +
                "source'ta olmayan volumetric haze uydurulmaz.";

            Debug.Log("[Painted Alive M55.9] " + summary.Replace("\n", " | "), root);
            EditorUtility.DisplayDialog("Painted Alive M55.9", summary, "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("M55.9 Palimpsest Setup Hatası", exception.Message, "Tamam");
        }
    }

    [MenuItem(MenuRoot + "55.9 - Diagnose The Palimpsest")]
    public static void Diagnose()
    {
        GameObject root = FindSceneObjectByName(RootName);
        Transform visual = root != null ? FindUnique(root.transform, VisualRootName, false) : null;
        Transform collision = root != null ? FindUnique(root.transform, CollisionRootName, false) : null;
        Transform gameplay = root != null ? FindUnique(root.transform, GameplayRootName, false) : null;
        Transform livingExit = FindLivingGalleryExit();
        Transform entry = gameplay != null ? FindUnique(gameplay, EntryName, false) : null;
        Transform exit = gameplay != null ? FindUnique(gameplay, ExitName, false) : null;

        int visualColliders = visual != null
            ? visual.GetComponentsInChildren<Collider>(true).Length
            : -1;
        int collisionMeshes = collision != null
            ? collision.GetComponentsInChildren<MeshFilter>(true).Count(filter => filter != null && filter.sharedMesh != null)
            : -1;
        int collisionComponents = collision != null
            ? collision.GetComponentsInChildren<MeshCollider>(true).Length
            : -1;
        int enabledCollisionRenderers = collision != null
            ? collision.GetComponentsInChildren<Renderer>(true).Count(renderer => renderer != null && renderer.enabled)
            : -1;

        float seamDistance = entry != null && livingExit != null
            ? Vector3.Distance(entry.position, livingExit.position)
            : -1f;
        float seamAngle = entry != null && livingExit != null
            ? Quaternion.Angle(entry.rotation, livingExit.rotation)
            : -1f;

        PalimpsestGameplayBinder binder = root != null ? root.GetComponent<PalimpsestGameplayBinder>() : null;
        PalimpsestNetworkState stateHub = root != null ? root.GetComponent<PalimpsestNetworkState>() : null;
        CanvasFoldSystem[] folds = root != null ? root.GetComponentsInChildren<CanvasFoldSystem>(true) : Array.Empty<CanvasFoldSystem>();
        CanvasBacksideRegion[] backside = root != null ? root.GetComponentsInChildren<CanvasBacksideRegion>(true) : Array.Empty<CanvasBacksideRegion>();
        UnderpaintingRevealSystem[] underpaints = root != null ? root.GetComponentsInChildren<UnderpaintingRevealSystem>(true) : Array.Empty<UnderpaintingRevealSystem>();
        PaintSmearSurface[] smears = root != null ? root.GetComponentsInChildren<PaintSmearSurface>(true) : Array.Empty<PaintSmearSurface>();
        PerspectiveFrameSystem[] perspectives = root != null ? root.GetComponentsInChildren<PerspectiveFrameSystem>(true) : Array.Empty<PerspectiveFrameSystem>();
        PalimpsestPainterWorldInteraction[] interactions = root != null ? root.GetComponentsInChildren<PalimpsestPainterWorldInteraction>(true) : Array.Empty<PalimpsestPainterWorldInteraction>();
        PalimpsestFallZone[] fallZones = root != null ? root.GetComponentsInChildren<PalimpsestFallZone>(true) : Array.Empty<PalimpsestFallZone>();
        PalimpsestRespawnPoint[] respawns = root != null ? root.GetComponentsInChildren<PalimpsestRespawnPoint>(true) : Array.Empty<PalimpsestRespawnPoint>();
        PalimpsestRegionVolume[] regions = root != null ? root.GetComponentsInChildren<PalimpsestRegionVolume>(true) : Array.Empty<PalimpsestRegionVolume>();

        int texturesFound = CountPaintingTexturesFound();
        bool mappingPresent = AssetDatabase.LoadAssetAtPath<TextAsset>(MaterialMappingPath) != null;
        bool paintingBindingManifestPresent =
            AssetDatabase.LoadAssetAtPath<TextAsset>(PaintingBindingManifestPath) != null;
        bool manifestPresent = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath) != null;
        int visualDiscrepancies = CountKnownVisualSourceDiscrepancies(visual, collision, gameplay);

        bool mechanismPass =
            folds.Length == 2 && folds.All(system => system != null && system.IsConfigured) &&
            backside.Length == 1 &&
            underpaints.Length == 2 && underpaints.All(system => system != null && system.IsConfigured) &&
            smears.Length == 2 && smears.All(system => system != null && system.IsConfigured) &&
            perspectives.Length == 1 && perspectives[0] != null && perspectives[0].IsConfigured &&
            interactions.Length == 8 &&
            fallZones.Length == 7 &&
            respawns.Length == 6 &&
            regions.Length == 6;

        bool corePass =
            root != null && visual != null && collision != null && gameplay != null &&
            entry != null && exit != null && livingExit != null &&
            seamDistance >= 0f && seamDistance <= 0.03f &&
            seamAngle >= 0f && seamAngle <= 0.75f &&
            ChildRootsAreLocalIdentity(root.transform, visual, collision, gameplay) &&
            visualColliders == 0 &&
            collisionMeshes == 80 &&
            collisionComponents == 80 &&
            enabledCollisionRenderers == 0 &&
            manifestPresent &&
            binder != null && binder.IsConfigured &&
            stateHub != null &&
            mechanismPass &&
            ValidateStateColliderExclusivity(collision);

        bool paintingAssetsComplete = texturesFound == 4 && paintingBindingManifestPresent;
        bool sourceAssetsComplete = paintingAssetsComplete && mappingPresent;
        string result = corePass
            ? sourceAssetsComplete
                ? "PASS"
                : paintingAssetsComplete
                    ? "PASS_WITH_FULL_MATERIAL_MAPPING_FALLBACK"
                    : "PASS_WITH_SOURCE_ASSET_GAPS"
            : "NEEDS_ATTENTION";

        string report =
            "Painted Alive M55.9 — The Palimpsest Diagnostics\n" +
            "Result=" + result + "\n\n" +
            $"Root={(root != null)}\n" +
            $"VisualRoot={(visual != null)}\n" +
            $"CollisionRoot={(collision != null)}\n" +
            $"GameplayRoot={(gameplay != null)}\n" +
            $"Manifest={(manifestPresent ? "OK" : "MISSING")}\n" +
            $"ContinuationSeamDistance={seamDistance:F3}m\n" +
            $"ContinuationSeamAngle={seamAngle:F2}deg\n" +
            $"ChildFBXRootsLocalIdentity={ChildRootsAreLocalIdentity(root != null ? root.transform : null, visual, collision, gameplay)}\n" +
            $"VisualColliders={visualColliders} expected=0\n" +
            $"CollisionMeshes={collisionMeshes}/80\n" +
            $"CollisionComponents={collisionComponents}/80\n" +
            $"EnabledCollisionRenderers={enabledCollisionRenderers} expected=0\n" +
            $"StateColliderFamiliesExclusive={ValidateStateColliderExclusivity(collision)}\n" +
            $"FoldSystems={folds.Length}/2\n" +
            $"BacksideRegions={backside.Length}/1\n" +
            $"UnderpaintSystems={underpaints.Length}/2\n" +
            $"SmearSystems={smears.Length}/2\n" +
            $"SmearBlendShapesResolved={smears.Count(system => system != null && system.ShapeKeyResolved)}/2\n" +
            $"PerspectiveSystems={perspectives.Length}/1\n" +
            $"PainterInteractions={interactions.Length}/8\n" +
            $"FallZones={fallZones.Length}/7\n" +
            $"RespawnPoints={respawns.Length}/6\n" +
            $"RegionVolumes={regions.Length}/6\n" +
            $"BinderConfigured={(binder != null && binder.IsConfigured)}\n" +
            $"NetworkStateAdapter={(stateHub != null)}\n" +
            $"ProductionNetworkTransportBound={(stateHub != null && stateHub.ProductionTransportBound)} (expected False until M56)\n" +
            $"PaintingTextures={texturesFound}/4\n" +
            $"PaintingBindingManifestPresent={paintingBindingManifestPresent}\n" +
            $"FullMaterialMappingPresent={mappingPresent}\n" +
            $"KnownNonCriticalSourceVisualDiscrepancies={visualDiscrepancies}/7\n\n" +
            "The seven source visual discrepancies were found in the supplied package before Unity integration and do not include a required helper/collider. " +
            "All 80 authoritative collision names and the gameplay helpers used by the mechanics are present in the supplied FBXs.\n\n" +
            "Runtime acceptance still requires Play Mode traversal/mechanism tests. M55.9 does not claim production multiplayer transport; it only exposes deterministic persistent-state snapshot/apply hooks for M56.";

        Debug.Log("[M55.9 Palimpsest Diagnose]\n" + report, root);
        EditorUtility.DisplayDialog("M55.9 Palimpsest Diagnose", report, "Tamam");
    }

    [MenuItem(MenuRoot + "55.9.1 - Realign Palimpsest Continuation")]
    public static void RealignContinuation()
    {
        RequireEditMode();
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
            throw new InvalidOperationException("Palimpsest continuation root bulunamadı.");

        Transform gameplay = RequireUnique(root.transform, GameplayRootName);
        Transform entry = RequireUnique(gameplay, EntryName);
        Transform target = FindLivingGalleryExit();
        if (target == null)
            throw new InvalidOperationException("Living Gallery exit bulunamadı.");

        Undo.RecordObject(root.transform, "Realign Palimpsest continuation");
        AlignContinuationRoot(root.transform, entry, target, ConnectionGap);
        Transform exit = RequireUnique(gameplay, ExitName);
        RebindJourneyFinish(exit);
        UpdateLivingGalleryOutgoingTransition();
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem(MenuRoot + "55.9.1 - Rebind Journey Exit To Palimpsest")]
    public static void RebindJourneyExit()
    {
        RequireEditMode();
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
            throw new InvalidOperationException("Palimpsest continuation root bulunamadı.");
        Transform gameplay = RequireUnique(root.transform, GameplayRootName);
        Transform exit = RequireUnique(gameplay, ExitName);
        bool rebound = RebindJourneyFinish(exit);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        EditorUtility.DisplayDialog("M55.9 Rebind", "Journey exit rebound=" + rebound, "Tamam");
    }

    [MenuItem(MenuRoot + "55.9.1 - Move Figure To Palimpsest Entry (Test Only)")]
    public static void MoveFigureToEntry()
    {
        RequireEditMode();
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
            throw new InvalidOperationException("Palimpsest continuation root bulunamadı.");
        Transform gameplay = RequireUnique(root.transform, GameplayRootName);
        Transform entry = RequireUnique(gameplay, EntryName);
        FigureMotor motor = FindSceneComponent<FigureMotor>();
        if (motor == null)
            throw new InvalidOperationException("FigureMotor bulunamadı.");

        Undo.RecordObject(motor.transform, "Move Figure to Palimpsest entry");
        motor.Teleport(entry.position + Vector3.up * 0.08f, entry.rotation);
        EditorUtility.SetDirty(motor.transform);
    }

    [MenuItem(MenuRoot + "55.9.1 - Select Palimpsest Root")]
    public static void SelectRoot()
    {
        GameObject root = FindSceneObjectByName(RootName);
        Selection.activeGameObject = root;
        if (root != null)
            EditorGUIUtility.PingObject(root);
    }

    private static CanvasFoldSystem BuildFoldA(
        Transform runtime,
        Transform visual,
        Transform collision,
        Transform gameplay,
        PalimpsestClearanceVolume clearance,
        PalimpsestNetworkState networkState)
    {
        GameObject group = NewChild(runtime, "Fold_A_Runtime");
        CanvasFoldSystem system = Undo.AddComponent<CanvasFoldSystem>(group);

        Transform[] movingVisuals = ReduceToTopLevelTargets(
            visual,
            visual.GetComponentsInChildren<Transform>(true)
                .Where(target =>
                    target != null &&
                    (target.name == "INT_Fold_A" ||
                     target.name.StartsWith("Fold_A_MovingEdge", StringComparison.Ordinal) ||
                     target.name.StartsWith("Fold_A_TipMast", StringComparison.Ordinal) ||
                     target.name.StartsWith("Fold_A_TensionToCorner", StringComparison.Ordinal) ||
                     target.name.StartsWith("Fold_A_TensionRod", StringComparison.Ordinal)))
                .ToArray());

        system.Configure(
            "FOLD_A",
            "OPEN",
            "FOLDED",
            RequireUnique(gameplay, "GP_Fold_A_Pivot"),
            RequireUnique(gameplay, "GP_Fold_A_State_Open"),
            RequireUnique(gameplay, "GP_Fold_A_State_Folded"),
            clearance,
            movingVisuals,
            RequireCollider(collision, "COL_Fold_A_Open"),
            RequireCollider(collision, "COL_Fold_A_Folded"),
            RequireCollider(collision, "COL_Junction_Main_Open"),
            RequireCollider(collision, "COL_Junction_Main_Folded"),
            65f,
            1.15f,
            networkState);
        return system;
    }

    private static CanvasFoldSystem BuildGreatFold(
        Transform runtime,
        Transform visual,
        Transform collision,
        Transform gameplay,
        PalimpsestClearanceVolume clearance,
        PalimpsestNetworkState networkState)
    {
        GameObject group = NewChild(runtime, "GreatFold_Runtime");
        CanvasFoldSystem system = Undo.AddComponent<CanvasFoldSystem>(group);
        Transform[] movingVisuals = ReduceToTopLevelTargets(
            visual,
            visual.GetComponentsInChildren<Transform>(true)
                .Where(target =>
                    target != null &&
                    (target.name == "INT_GreatFold" ||
                     target.name == "GreatFold_LowerLeaf" ||
                     target.name.StartsWith("GreatFold_MovingEdge", StringComparison.Ordinal) ||
                     target.name.StartsWith("GreatFold_TipMast", StringComparison.Ordinal) ||
                     target.name.StartsWith("GreatFold_TensionToCorner", StringComparison.Ordinal) ||
                     target.name.StartsWith("GreatFold_TensionRod", StringComparison.Ordinal)))
                .ToArray());

        system.Configure(
            "GREAT_FOLD",
            "A",
            "B",
            RequireUnique(gameplay, "GP_GreatFold_Pivot"),
            RequireUnique(gameplay, "GP_GreatFold_State_A"),
            RequireUnique(gameplay, "GP_GreatFold_State_B"),
            clearance,
            movingVisuals,
            RequireCollider(collision, "COL_GreatFold_State_A"),
            RequireCollider(collision, "COL_GreatFold_State_B"),
            RequireCollider(collision, "COL_Junction_Main_A"),
            RequireCollider(collision, "COL_Junction_Main_B"),
            65f,
            1.25f,
            networkState);
        return system;
    }

    private static CanvasBacksideRegion BuildBackside(
        Transform runtime,
        Transform gameplay,
        out CanvasBacksideTraceSystem trace,
        PalimpsestNetworkState networkState)
    {
        GameObject traceObject = NewChild(runtime, "BacksideTrace_Runtime");
        trace = Undo.AddComponent<CanvasBacksideTraceSystem>(traceObject);
        trace.Configure(
            new[]
            {
                RequireUnique(gameplay, "GP_Backside_Trace_A"),
                RequireUnique(gameplay, "GP_Backside_Trace_B"),
                RequireUnique(gameplay, "GP_Backside_Trace_C")
            },
            3f);

        Transform regionHelper = RequireUnique(gameplay, "GP_Backside_Region");
        BoxCollider trigger = GetOrAddBoxTrigger(regionHelper.gameObject);
        CanvasBacksideRegion region = GetOrAdd<CanvasBacksideRegion>(regionHelper.gameObject);
        region.Configure(
            "BACKSIDE_WORKS",
            trigger,
            RequireUnique(gameplay, "GP_Backside_Entry"),
            RequireUnique(gameplay, "GP_Backside_Exit"),
            trace,
            networkState);
        return region;
    }

    private static UnderpaintingRevealSystem BuildUnderpaint(
        string suffix,
        Transform runtime,
        Transform visual,
        Transform collision,
        Transform gameplay,
        PalimpsestNetworkState networkState)
    {
        GameObject group = NewChild(runtime, "Underpaint_" + suffix + "_Runtime");
        UnderpaintingRevealSystem system = Undo.AddComponent<UnderpaintingRevealSystem>(group);

        Transform[] current = ReduceToTopLevelTargets(
            visual,
            visual.GetComponentsInChildren<Transform>(true)
                .Where(target => target != null &&
                    (target.name == "CURRENT_LAYER_" + suffix ||
                     target.name.StartsWith("CURRENT_LAYER_" + suffix + "_", StringComparison.Ordinal)))
                .ToArray());
        Transform[] revealed = ReduceToTopLevelTargets(
            visual,
            visual.GetComponentsInChildren<Transform>(true)
                .Where(target => target != null &&
                    (target.name.StartsWith("UNDERPAINT_LAYER_" + suffix + "_Arch", StringComparison.Ordinal) ||
                     target.name.StartsWith("UNDERPAINT_LAYER_" + suffix + "_Jamb", StringComparison.Ordinal)))
                .ToArray());

        system.Configure(
            "UNDERPAINT_" + suffix,
            RequireUnique(gameplay, "GP_Underpaint_" + suffix + "_Current"),
            RequireUnique(gameplay, "GP_Underpaint_" + suffix + "_Revealed"),
            RequireUnique(gameplay, "GP_Underpaint_" + suffix + "_Reveal"),
            current,
            revealed,
            RequireCollider(collision, "COL_Underpaint_" + suffix + "_Current"),
            RequireCollider(collision, "COL_Underpaint_" + suffix + "_Revealed"),
            1.1f,
            networkState);
        return system;
    }

    private static PaintSmearSurface BuildSmear(
        string suffix,
        Transform runtime,
        Transform visual,
        Transform collision,
        Transform gameplay,
        PalimpsestClearanceVolume clearance,
        PalimpsestNetworkState networkState)
    {
        GameObject group = NewChild(runtime, "Smear_" + suffix + "_Runtime");
        PaintSmearSurface system = Undo.AddComponent<PaintSmearSurface>(group);

        Transform wetLayerTransform = RequireUnique(visual, "Smear_" + suffix + "_WetLayer");
        SkinnedMeshRenderer wetLayer = wetLayerTransform.GetComponent<SkinnedMeshRenderer>();
        if (wetLayer == null)
            wetLayer = wetLayerTransform.GetComponentInChildren<SkinnedMeshRenderer>(true);

        Transform[] movingVisuals = ReduceToTopLevelTargets(
            visual,
            visual.GetComponentsInChildren<Transform>(true)
                .Where(target => target != null &&
                    (target.name.StartsWith("Smear_" + suffix + "_ImpastoRidge", StringComparison.Ordinal) ||
                     target.name == "Smear_" + suffix + "_ScrapeBlade"))
                .ToArray());

        Collider extendedRoute = suffix == "A"
            ? FindCollider(collision, "COL_Junction_Main_Extended")
            : null;
        Collider retractedRoute = suffix == "A"
            ? FindCollider(collision, "COL_Junction_Main_Retracted")
            : null;

        system.Configure(
            "SMEAR_" + suffix,
            wetLayer,
            RequireUnique(gameplay, "GP_Smear_" + suffix + "_Extended"),
            RequireUnique(gameplay, "GP_Smear_" + suffix + "_Retracted"),
            clearance,
            movingVisuals,
            RequireCollider(collision, "COL_Smear_" + suffix + "_Extended"),
            RequireCollider(collision, "COL_Smear_" + suffix + "_Retracted"),
            extendedRoute,
            retractedRoute,
            7f,
            1.35f,
            networkState);
        return system;
    }

    private static PerspectiveFrameSystem BuildPerspective(
        Transform runtime,
        Transform visual,
        Transform collision,
        Transform gameplay,
        PalimpsestNetworkState networkState,
        out PerspectiveTraversalBridge bridge)
    {
        GameObject bridgeObject = NewChild(runtime, "PerspectiveBridge_Runtime");
        bridge = Undo.AddComponent<PerspectiveTraversalBridge>(bridgeObject);
        Transform pivot = RequireUnique(gameplay, "GP_Perspective_Frame_Pivot");
        Transform connection = RequireUnique(visual, "Perspective_Aligned_Connection");
        bridge.Configure(
            connection,
            RequireCollider(collision, "COL_Perspective_Aligned"),
            RequireCollider(collision, "COL_Perspective_Misaligned"),
            pivot,
            5f);

        GameObject frameObject = NewChild(runtime, "PerspectiveFrame_Runtime");
        PerspectiveFrameSystem system = Undo.AddComponent<PerspectiveFrameSystem>(frameObject);
        system.Configure(
            "PERSPECTIVE_EXIT",
            RequireUnique(gameplay, "INT_Perspective_Frame"),
            pivot,
            RequireUnique(gameplay, "GP_Perspective_State_Misaligned"),
            RequireUnique(gameplay, "GP_Perspective_State_Aligned"),
            RequireUnique(gameplay, "GP_Perspective_ViewReference"),
            RequireUnique(gameplay, "GP_Perspective_Target_A"),
            RequireUnique(gameplay, "GP_Perspective_Target_B"),
            bridge,
            1.5f,
            1.1f,
            networkState);
        return system;
    }

    private static PalimpsestRespawnCoordinator BuildFallRespawn(
        Transform runtime,
        Transform gameplay,
        PalimpsestClearanceVolume[] unsafeVolumes,
        int collisionLayer)
    {
        GameObject coordinatorObject = NewChild(runtime, "FallRespawn_Runtime");
        PalimpsestRespawnCoordinator coordinator =
            Undo.AddComponent<PalimpsestRespawnCoordinator>(coordinatorObject);

        PalimpsestRespawnPoint[] points = new PalimpsestRespawnPoint[6];
        for (int index = 0; index < 6; index++)
        {
            string suffix = (index + 1).ToString("00");
            Transform helper = RequireUnique(gameplay, "GP_Respawn_" + suffix);
            PalimpsestRespawnPoint point = GetOrAdd<PalimpsestRespawnPoint>(helper.gameObject);
            point.Configure("RESPAWN_" + suffix, 1.2f);
            points[index] = point;
        }

        LayerMask mask = new LayerMask
        {
            value = collisionLayer >= 0 ? 1 << collisionLayer : 0
        };
        coordinator.Configure(points, unsafeVolumes, mask);

        for (int index = 0; index < 6; index++)
        {
            string suffix = (index + 1).ToString("00");
            Transform regionHelper = RequireUnique(gameplay, "GP_Region_" + suffix + RegionNameSuffix(index + 1));
            BoxCollider regionCollider = GetOrAddBoxTrigger(regionHelper.gameObject);
            PalimpsestRegionVolume region = GetOrAdd<PalimpsestRegionVolume>(regionHelper.gameObject);
            region.Configure(index + 1, regionCollider, coordinator, points[index]);

            Transform fallHelper = RequireUnique(gameplay, "GP_FallZone_" + suffix);
            BoxCollider fallCollider = GetOrAddBoxTrigger(fallHelper.gameObject);
            PalimpsestFallZone fallZone = GetOrAdd<PalimpsestFallZone>(fallHelper.gameObject);
            fallZone.Configure("FALL_" + suffix, fallCollider, coordinator, points[index]);
        }

        Transform globalFall = RequireUnique(gameplay, "GP_FallZone_Global");
        BoxCollider globalCollider = GetOrAddBoxTrigger(globalFall.gameObject);
        PalimpsestFallZone globalZone = GetOrAdd<PalimpsestFallZone>(globalFall.gameObject);
        globalZone.Configure("FALL_GLOBAL", globalCollider, coordinator, null);
        return coordinator;
    }

    private static string RegionNameSuffix(int index)
    {
        switch (index)
        {
            case 1: return "_Stretched_Entrance";
            case 2: return "_Backside_Works";
            case 3: return "_Great_Fold";
            case 4: return "_Underpainting";
            case 5: return "_Smear_Works";
            case 6: return "_Perspective_Exit";
            default: return string.Empty;
        }
    }

    private static void BuildPainterInteractions(
        Transform gameplay,
        CanvasFoldSystem foldA,
        CanvasFoldSystem greatFold,
        UnderpaintingRevealSystem underpaintA,
        UnderpaintingRevealSystem underpaintB,
        PaintSmearSurface smearA,
        PaintSmearSurface smearB,
        PerspectiveFrameSystem perspective,
        PalimpsestClearanceVolume foldClearance,
        PalimpsestClearanceVolume greatFoldClearance,
        PalimpsestClearanceVolume smearAClearance,
        PalimpsestClearanceVolume smearBClearance)
    {
        BuildInteraction(gameplay, "PAINTER_Fold_A_Control", "FOLD_A",
            new MonoBehaviour[] { foldA }, foldClearance != null ? foldClearance.transform : null, 2.5f);
        BuildInteraction(gameplay, "PAINTER_GreatFold_Control", "GREAT_FOLD",
            new MonoBehaviour[] { greatFold }, greatFoldClearance != null ? greatFoldClearance.transform : null, 2.5f);
        BuildInteraction(gameplay, "PAINTER_Underpaint_A_Control", "UNDERPAINT_A",
            new MonoBehaviour[] { underpaintA }, RequireUnique(gameplay, "GP_Underpaint_A_Reveal"), 1.25f);
        BuildInteraction(gameplay, "PAINTER_Underpaint_B_Control", "UNDERPAINT_B",
            new MonoBehaviour[] { underpaintB }, RequireUnique(gameplay, "GP_Underpaint_B_Reveal"), 1.25f);
        // The source includes a generic Underpaint control but does not define a separate mechanic.
        // It therefore resolves to the first still-available authored A/B reveal, without inventing a third state.
        BuildInteraction(gameplay, "PAINTER_Underpaint_Control", "UNDERPAINT_GROUP",
            new MonoBehaviour[] { underpaintA, underpaintB }, RequireUnique(gameplay, "PAINTER_Underpaint_Control"), 1.25f);
        BuildInteraction(gameplay, "PAINTER_Smear_A_Control", "SMEAR_A",
            new MonoBehaviour[] { smearA }, smearAClearance != null ? smearAClearance.transform : null, 1.25f);
        BuildInteraction(gameplay, "PAINTER_Smear_B_Control", "SMEAR_B",
            new MonoBehaviour[] { smearB }, smearBClearance != null ? smearBClearance.transform : null, 1.25f);
        BuildInteraction(gameplay, "PAINTER_Perspective_Control", "PERSPECTIVE_EXIT",
            new MonoBehaviour[] { perspective }, RequireUnique(gameplay, "GP_Perspective_Frame_Pivot"), 1.25f);
    }

    private static void BuildInteraction(
        Transform gameplay,
        string helperName,
        string systemId,
        MonoBehaviour[] targets,
        Transform telegraphGeometry,
        float telegraphSeconds)
    {
        Transform helper = RequireUnique(gameplay, helperName);
        PalimpsestPainterWorldInteraction interaction =
            GetOrAdd<PalimpsestPainterWorldInteraction>(helper.gameObject);
        interaction.Configure(systemId, targets, telegraphGeometry, telegraphSeconds, true);
    }

    private static PalimpsestClearanceVolume BuildClearance(Transform gameplay, string helperName)
    {
        Transform helper = RequireUnique(gameplay, helperName);
        BoxCollider box = GetOrAddBoxTrigger(helper.gameObject);
        PalimpsestClearanceVolume clearance = GetOrAdd<PalimpsestClearanceVolume>(helper.gameObject);
        clearance.Configure(helperName, box);
        return clearance;
    }

    private static BoxCollider GetOrAddBoxTrigger(GameObject gameObject)
    {
        BoxCollider box = gameObject.GetComponent<BoxCollider>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider>(gameObject);
        box.center = Vector3.zero;
        box.size = Vector3.one * 2f;
        box.isTrigger = true;
        EditorUtility.SetDirty(box);
        return box;
    }

    private static void ConfigureVisual(GameObject visualRoot, SetupStats stats)
    {
        foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);
        }
        foreach (Camera camera in visualRoot.GetComponentsInChildren<Camera>(true))
            if (camera != null) camera.enabled = false;
        foreach (Light light in visualRoot.GetComponentsInChildren<Light>(true))
            if (light != null) light.enabled = false;

        EnsureAssetFolder(MaterialFolder);
        Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        stats.VisualRenderers = renderers.Length;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] targetMaterials = new Material[sourceMaterials.Length];
            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                Material source = sourceMaterials[index];
                if (source == null)
                    continue;

                if (!replacements.TryGetValue(source, out Material target))
                {
                    target = CreateOrUpdateUrpFallbackMaterial(source);
                    replacements.Add(source, target);
                    stats.GeneratedMaterials++;
                }
                targetMaterials[index] = target;
            }
            renderer.sharedMaterials = targetMaterials;
            EditorUtility.SetDirty(renderer);
        }

        stats.VisualColliders = visualRoot.GetComponentsInChildren<Collider>(true).Length;
    }

    private static void ConfigureCollision(GameObject collisionRoot, int collisionLayer, SetupStats stats)
    {
        foreach (Renderer renderer in collisionRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        MeshFilter[] filters = collisionRoot.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter != null && filter.sharedMesh != null)
            .ToArray();
        stats.CollisionMeshes = filters.Length;

        foreach (MeshFilter filter in filters)
        {
            GameObject go = filter.gameObject;
            if (collisionLayer >= 0)
                go.layer = collisionLayer;

            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider == null)
                collider = Undo.AddComponent<MeshCollider>(go);
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            collider.enabled = InitialCollisionEnabled(go.name);
            EditorUtility.SetDirty(collider);
        }

        stats.CollisionComponents = collisionRoot.GetComponentsInChildren<MeshCollider>(true).Length;
        stats.EnabledCollisionRenderers = collisionRoot.GetComponentsInChildren<Renderer>(true)
            .Count(renderer => renderer != null && renderer.enabled);
    }

    private static bool InitialCollisionEnabled(string name)
    {
        switch (name)
        {
            case "COL_Fold_A_Folded":
            case "COL_Junction_Main_Folded":
            case "COL_GreatFold_State_B":
            case "COL_Junction_Main_B":
            case "COL_Underpaint_A_Revealed":
            case "COL_Underpaint_B_Revealed":
            case "COL_Smear_A_Extended":
            case "COL_Smear_B_Extended":
            case "COL_Junction_Main_Extended":
            case "COL_Perspective_Aligned":
                return false;
            default:
                return true;
        }
    }

    private static void ConfigureGameplaySource(GameObject gameplayRoot)
    {
        foreach (Renderer renderer in gameplayRoot.GetComponentsInChildren<Renderer>(true))
            if (renderer != null) renderer.enabled = false;
        foreach (Collider collider in gameplayRoot.GetComponentsInChildren<Collider>(true))
            if (collider != null) Undo.DestroyObjectImmediate(collider);
        foreach (Camera camera in gameplayRoot.GetComponentsInChildren<Camera>(true))
            if (camera != null) camera.enabled = false;
        foreach (Light light in gameplayRoot.GetComponentsInChildren<Light>(true))
            if (light != null) light.enabled = false;
    }

    private static Material CreateOrUpdateUrpFallbackMaterial(Material source)
    {
        string path = MaterialFolder + "/MAT_PAL_" + SanitizeFileName(source.name) + ".mat";
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

        if (PalimpsestMaterialMappingUtility_M55_9_3.TryApplyMappedMaterial(
            source.name,
            target,
            out string mappedClassification))
        {
            EditorUtility.SetDirty(target);
            return target;
        }

        Color color = ReadSourceColor(source);
        if (IsExactPaintingMaterial(source.name))
            color = Color.white;
        else if (source.name.StartsWith("MAT_ART_", StringComparison.Ordinal))
            color = new Color(0.56f, 0.49f, 0.38f, 1f);
        if (source.name == "MAT_Charcoal")
            color = new Color(0.035f, 0.04f, 0.045f, 1f);
        if (source.name == "MAT_Gesso")
            color = new Color(0.82f, 0.79f, 0.69f, 1f);
        if (source.name == "MAT_Canvas_Wet")
            color = new Color(0.34f, 0.22f, 0.18f, 1f);

        SetColor(target, "_BaseColor", color);
        SetColor(target, "_Color", color);

        float metallic = 0f;
        float smoothness = 0.28f;
        if (source.name.Contains("Brass")) { metallic = 0.72f; smoothness = 0.48f; }
        else if (source.name.Contains("Steel")) { metallic = 0.78f; smoothness = 0.42f; }
        else if (source.name.Contains("Iron")) { metallic = 0.58f; smoothness = 0.30f; }
        else if (source.name.Contains("Wet")) { smoothness = 0.72f; }
        else if (source.name.Contains("Pigment")) { smoothness = 0.42f; }
        else if (source.name.Contains("Rope")) { smoothness = 0.12f; }
        else if (source.name.Contains("Timber")) { smoothness = 0.18f; }

        SetFloat(target, "_Metallic", metallic);
        SetFloat(target, "_Smoothness", smoothness);

        Texture baseMap = FindPaintingTextureForMaterial(source.name);
        if (baseMap == null)
            baseMap = ReadTexture(source, "_BaseMap") ?? ReadTexture(source, "_MainTex");
        SetTexture(target, "_BaseMap", baseMap);
        SetTexture(target, "_MainTex", baseMap);

        Texture normal = ReadTexture(source, "_BumpMap");
        SetTexture(target, "_BumpMap", normal);
        if (normal != null)
            target.EnableKeyword("_NORMALMAP");

        EditorUtility.SetDirty(target);
        return target;
    }

    private static Texture FindPaintingTextureForMaterial(string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
            return null;

        string file;
        switch (materialName)
        {
            case "MAT_ART_Architecture_Study_01":
            case "MAT_ART_Architecture_Study_01_Distant":
            case "MAT_ART_Architecture_Study_01_Distant.001":
                file = "ART_Architecture_Study_01.png";
                break;

            case "MAT_ART_Landscape_01":
            case "MAT_ART_Landscape_01_Distant":
                file = "ART_Landscape_01.png";
                break;

            case "MAT_ART_Underpaint_Figure_01":
            case "MAT_ART_Underpaint_Figure_01_Distant":
            case "MAT_ART_Underpaint_Figure_01_Distant.001":
                file = "ART_Underpaint_Figure_01.png";
                break;

            case "MAT_ART_Unfinished_Masterwork_01":
            case "MAT_ART_Unfinished_Masterwork_01_Distant":
            case "MAT_ART_Unfinished_Masterwork_01_Distant.001":
                file = "ART_Unfinished_Masterwork_01.png";
                break;

            default:
                return null;
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(PaintingFolder + "/" + file);
    }

    private static bool IsExactPaintingMaterial(string materialName)
    {
        return FindPaintingTextureForMaterial(materialName) != null;
    }

    private static int CountPaintingTexturesFound()
    {
        int count = 0;
        foreach (string file in ExpectedPaintingFiles)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(PaintingFolder + "/" + file) != null)
                count++;
        }
        return count;
    }

    private static bool UpdateLivingGalleryOutgoingTransition()
    {
        GameObject livingRoot = FindSceneObjectByName(LivingGalleryRootName);
        if (livingRoot == null)
            return false;
        Transform exit = FindUnique(livingRoot.transform, LivingGalleryExitName, false);
        if (exit == null)
            return false;

        LivingGalleryRegionTransition transition = GetOrAdd<LivingGalleryRegionTransition>(exit.gameObject);
        transition.ConfigureFutureRegion("TRANSITION_EXIT", "The Palimpsest");
        EditorUtility.SetDirty(transition);
        return true;
    }

    private static Transform FindLivingGalleryExit()
    {
        GameObject livingRoot = FindSceneObjectByName(LivingGalleryRootName);
        if (livingRoot != null)
        {
            Transform exit = FindUnique(livingRoot.transform, LivingGalleryExitName, false);
            if (exit != null)
                return exit;
        }
        GameObject fallback = FindSceneObjectByName(LivingGalleryExitName);
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
            Undo.RecordObject(m54Exit.transform, "Move M54 exit to Palimpsest final");
            m54Exit.transform.SetPositionAndRotation(finish.position, finish.rotation);
            EditorUtility.SetDirty(m54Exit.transform);
            changed = true;
        }

        PrototypeFrameExitGate gate = FindSceneComponent<PrototypeFrameExitGate>();
        if (gate != null)
        {
            Undo.RecordObject(gate.transform, "Move frame exit gate to Palimpsest final");
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
            FigureClarityState figure = tracker.Figure != null
                ? tracker.Figure
                : FindSceneComponent<FigureClarityState>();
            Transform start = tracker.RouteStart != null
                ? tracker.RouteStart
                : FindSceneObjectByName("Journey_Start")?.transform;
            PrototypeJourneyScoreConfig config =
                AssetDatabase.LoadAssetAtPath<PrototypeJourneyScoreConfig>(JourneyScoreConfigPath);

            if (figure != null && start != null && config != null)
            {
                Undo.RecordObject(tracker, "Extend journey to Palimpsest final");
                tracker.Configure(figure, start, finish, config);
                EditorUtility.SetDirty(tracker);
                changed = true;
            }
        }

        return changed;
    }

    private static bool ValidateStateColliderExclusivity(Transform collisionRoot)
    {
        if (collisionRoot == null)
            return false;

        string[][] families =
        {
            new[] { "COL_Fold_A_Open", "COL_Fold_A_Folded" },
            new[] { "COL_Junction_Main_Open", "COL_Junction_Main_Folded" },
            new[] { "COL_GreatFold_State_A", "COL_GreatFold_State_B" },
            new[] { "COL_Junction_Main_A", "COL_Junction_Main_B" },
            new[] { "COL_Underpaint_A_Current", "COL_Underpaint_A_Revealed" },
            new[] { "COL_Underpaint_B_Current", "COL_Underpaint_B_Revealed" },
            new[] { "COL_Smear_A_Extended", "COL_Smear_A_Retracted" },
            new[] { "COL_Smear_B_Extended", "COL_Smear_B_Retracted" },
            new[] { "COL_Junction_Main_Extended", "COL_Junction_Main_Retracted" },
            new[] { "COL_Perspective_Aligned", "COL_Perspective_Misaligned" }
        };

        foreach (string[] family in families)
        {
            int enabled = 0;
            foreach (string name in family)
            {
                Collider collider = FindCollider(collisionRoot, name);
                if (collider == null)
                    return false;
                if (collider.enabled)
                    enabled++;
            }
            if (enabled != 1)
                return false;
        }
        return true;
    }

    private static int CountKnownVisualSourceDiscrepancies(
        Transform visual,
        Transform collision,
        Transform gameplay)
    {
        int count = 0;
        foreach (string name in KnownNonCriticalVisualSourceDiscrepancies)
        {
            if (visual == null || FindUnique(visual, name, false) == null)
                count++;
        }
        return count;
    }

    private static void ValidateManifestFile()
    {
        TextAsset manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
        if (manifest == null)
            throw new InvalidOperationException("Palimpsest binding JSON bulunamadı: " + ManifestPath);

        string text = manifest.text;
        if (string.IsNullOrWhiteSpace(text) ||
            !text.Contains("\"schema_version\": 2") ||
            !text.Contains("\"map\": \"The Palimpsest\"") ||
            !text.Contains("\"FOLD_A\"") ||
            !text.Contains("\"PERSPECTIVE_EXIT\"") ||
            !text.Contains("\"GP_ENTRY_MAIN\"") ||
            !text.Contains("\"GP_EXIT_MAIN\""))
        {
            throw new InvalidOperationException("Palimpsest binding JSON schema/content beklenen kaynakla eşleşmiyor.");
        }
    }

    private static bool ChildRootsAreLocalIdentity(
        Transform root,
        Transform visual,
        Transform collision,
        Transform gameplay)
    {
        if (root == null || visual == null || collision == null || gameplay == null)
            return false;
        return IsLocalIdentity(visual) && IsLocalIdentity(collision) && IsLocalIdentity(gameplay);
    }

    private static bool IsLocalIdentity(Transform transform)
    {
        return transform != null &&
            transform.localPosition.sqrMagnitude <= 0.000001f &&
            Quaternion.Angle(transform.localRotation, Quaternion.identity) <= 0.001f &&
            (transform.localScale - Vector3.one).sqrMagnitude <= 0.000001f;
    }

    private static void AlignContinuationRoot(Transform root, Transform entry, Transform target, float gap)
    {
        if (root == null || entry == null || target == null)
            throw new ArgumentNullException("Palimpsest continuation alignment requires root, entry and target.");

        Vector3 entryRelativePosition = root.InverseTransformPoint(entry.position);
        Quaternion entryRelativeRotation = Quaternion.Inverse(root.rotation) * entry.rotation;
        Quaternion desiredRootRotation = target.rotation * Quaternion.Inverse(entryRelativeRotation);
        Vector3 desiredEntryPosition = target.position + target.forward * gap;
        Vector3 desiredRootPosition = desiredEntryPosition - desiredRootRotation * entryRelativePosition;
        root.SetPositionAndRotation(desiredRootPosition, desiredRootRotation);
    }

    private static Transform[] ReduceToTopLevelTargets(Transform searchRoot, Transform[] candidates)
    {
        HashSet<Transform> set = new HashSet<Transform>(candidates.Where(candidate => candidate != null));
        List<Transform> result = new List<Transform>();

        foreach (Transform candidate in set)
        {
            bool ancestorAlsoSelected = false;
            Transform parent = candidate.parent;
            while (parent != null && parent != searchRoot)
            {
                if (set.Contains(parent))
                {
                    ancestorAlsoSelected = true;
                    break;
                }
                parent = parent.parent;
            }
            if (!ancestorAlsoSelected)
                result.Add(candidate);
        }
        return result.ToArray();
    }

    private static Collider RequireCollider(Transform root, string name)
    {
        Collider collider = FindCollider(root, name);
        if (collider == null)
            throw new InvalidOperationException("Required Palimpsest collision missing: " + name);
        return collider;
    }

    private static Collider FindCollider(Transform root, string name)
    {
        Transform target = FindUnique(root, name, false);
        return target != null ? target.GetComponent<Collider>() : null;
    }

    private static Color ReadSourceColor(Material material)
    {
        if (material != null)
        {
            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color"))
                return material.GetColor("_Color");
        }
        return new Color(0.45f, 0.42f, 0.36f, 1f);
    }

    private static Texture ReadTexture(Material material, string property)
    {
        return material != null && material.HasProperty(property)
            ? material.GetTexture(property)
            : null;
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material != null && material.HasProperty(property))
            material.SetColor(property, value);
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private static void SetTexture(Material material, string property, Texture value)
    {
        if (material != null && material.HasProperty(property) && value != null)
            material.SetTexture(property, value);
    }

    private static void ConfigureImporter(string assetPath, bool importBlendShapes, bool importMaterials)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        }

        if (importer == null)
            return;

        bool changed = false;
        if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
        if (importer.importCameras) { importer.importCameras = false; changed = true; }
        if (importer.importLights) { importer.importLights = false; changed = true; }
        if (importer.importBlendShapes != importBlendShapes)
        {
            importer.importBlendShapes = importBlendShapes;
            changed = true;
        }

        ModelImporterMaterialImportMode desiredMaterialMode = importMaterials
            ? ModelImporterMaterialImportMode.ImportStandard
            : ModelImporterMaterialImportMode.None;
        if (importer.materialImportMode != desiredMaterialMode)
        {
            importer.materialImportMode = desiredMaterialMode;
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
            throw new InvalidOperationException("Required Palimpsest object missing: " + name);
        return result;
    }

    private static Transform FindUnique(Transform root, string name, bool throwOnDuplicate)
    {
        if (root == null)
            return null;
        List<Transform> matches = new List<Transform>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform target in all)
        {
            if (target != null && string.Equals(target.name, name, StringComparison.Ordinal))
                matches.Add(target);
        }
        if (matches.Count == 0)
            return null;
        if (matches.Count > 1 && throwOnDuplicate)
            throw new InvalidOperationException(
                $"Duplicate exact-name match '{name}' under {root.name}: {matches.Count}");
        return matches[0];
    }

    private static GameObject FindSceneObjectByName(string name)
    {
        GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
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
        T[] all = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
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

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
            return -1;

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null)
            return -1;

        for (int index = 8; index < layers.arraySize; index++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(layer.stringValue))
                continue;
            layer.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return index;
        }

        Debug.LogWarning("[M55.9] Layer slot kalmadı: " + layerName);
        return -1;
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unnamed";
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new string(
            value.Select(character =>
                invalid.Contains(character) || character == '|'
                    ? '_'
                    : character)
            .ToArray());
        while (sanitized.Contains("__"))
            sanitized = sanitized.Replace("__", "_");
        return sanitized.Trim(' ', '_');
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Bu setup Play Mode kapalıyken çalıştırılmalı.");
    }
}
#endif
