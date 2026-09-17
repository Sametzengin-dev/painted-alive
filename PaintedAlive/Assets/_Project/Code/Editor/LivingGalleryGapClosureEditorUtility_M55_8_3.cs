#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Figures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LivingGalleryGapClosureEditorUtility_M55_8_3
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";
    private const string RootName = "M55_8_LivingGallery_Continuation";
    private const string VisualRootName = "The_Living_Gallery_Visual";
    private const string CollisionRootName = "The_Living_Gallery_Collision";
    private const string GameplayRootName = "The_Living_Gallery_Gameplay";
    private const string ClosureRootName = "M55_8_3_UnityAuthoredGapClosure";

    private static readonly string[] SourceGapIds =
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

    private sealed class PortalSpec
    {
        public string VolumeId;
        public string[] Sources;

        public PortalSpec(string volumeId, params string[] sources)
        {
            VolumeId = volumeId;
            Sources = sources ?? Array.Empty<string>();
        }
    }

    private static readonly PortalSpec[] PortalSpecs =
    {
        new PortalSpec(
            "VIS_WestFoyer",
            "ARCH_Portal_WestFoyer",
            "ARCH_Portal_WestFoyer.001"),
        new PortalSpec(
            "VIS_WestLong",
            "ARCH_Portal_WestLong",
            "ARCH_Portal_WestLong.001"),
        new PortalSpec(
            "VIS_HiddenArchive",
            "BRIDGE_V02_Archive_Entry_Jamb",
            "BRIDGE_V02_Archive_West_Threshold"),
        new PortalSpec(
            "VIS_NorthSalon",
            "ARCH_Portal_NorthSalon",
            "ARCH_Portal_NorthSalon.001")
    };

    public struct Report
    {
        public bool ManifestPass;
        public int SourceGaps;
        public int UnityResolvedGaps;
        public int VisibilityPortals;
        public int VisibilityVolumes;
        public int InkPresenceSystems;
        public int InkPresenters;
        public int GateMetadata;
        public int PigmentPolicies;
        public int PaperCollisionPolicies;
        public int MovingCanvasFollowers;
        public int FallZones;
        public int RespawnCoordinators;
        public int RegionTransitions;
        public int HardErrors;
        public string Details;

        public bool Pass =>
            ManifestPass &&
            SourceGaps == 9 &&
            UnityResolvedGaps == 9 &&
            VisibilityPortals >= 8 &&
            VisibilityVolumes == 6 &&
            InkPresenceSystems == 1 &&
            InkPresenters == 1 &&
            GateMetadata == 3 &&
            PigmentPolicies == 3 &&
            PaperCollisionPolicies == 2 &&
            MovingCanvasFollowers == 2 &&
            FallZones == 1 &&
            RespawnCoordinators == 1 &&
            RegionTransitions == 2 &&
            HardErrors == 0;
    }

    [MenuItem(MenuRoot + "55.8.3 - Close Living Gallery Authored Gaps")]
    public static void ApplyMenu()
    {
        try
        {
            GameObject root = FindSceneObject(RootName);
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "M55.8.3",
                    "Living Gallery root bulunamadı. Önce M55.8 + M55.8.2 kurulumunu çalıştır.",
                    "Tamam");
                return;
            }

            Report report = Apply(root.transform, true);
            EditorUtility.DisplayDialog(
                "M55.8.3 Authored Gap Closure",
                Format(report),
                "Tamam");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "M55.8.3 Hatası",
                exception.Message,
                "Tamam");
        }
    }

    [MenuItem(MenuRoot + "55.8.3 - Diagnose Living Gallery Gap Closure")]
    public static void DiagnoseMenu()
    {
        GameObject root = FindSceneObject(RootName);
        Report report = root != null
            ? Apply(root.transform, false)
            : new Report
            {
                HardErrors = 1,
                Details = "Living Gallery root bulunamadı."
            };

        Debug.Log(
            "[M55.8.3 Living Gallery Gap Closure]\n" + Format(report),
            root);
        EditorUtility.DisplayDialog(
            "M55.8.3 Diagnose",
            Format(report),
            "Tamam");
    }

    public static Report Apply(Transform mapRoot, bool mutate)
    {
        Report report = new Report { SourceGaps = SourceGapIds.Length };
        if (mapRoot == null)
        {
            report.HardErrors++;
            report.Details = "Map root null.";
            return report;
        }

        Transform visual = FindExact(mapRoot, VisualRootName, false);
        Transform collision = FindExact(mapRoot, CollisionRootName, false);
        Transform gameplay = FindExact(mapRoot, GameplayRootName, false);
        if (visual == null || collision == null || gameplay == null)
        {
            report.HardErrors++;
            report.Details = "Visual/Collision/Gameplay rootlarından biri eksik.";
            return report;
        }

        LivingGalleryManifestEditorUtility_M55_8_2.Report manifestReport =
            LivingGalleryManifestEditorUtility_M55_8_2.Apply(
                visual,
                collision,
                gameplay,
                false);
        report.ManifestPass = manifestReport.Pass;
        if (!report.ManifestPass)
            report.HardErrors++;

        string manifestBaselineDetails = report.ManifestPass
            ? string.Empty
            : "\n\n[M55.8.2 BASELINE DETAIL]\n" +
              LivingGalleryManifestEditorUtility_M55_8_2.Format(manifestReport);

        if (mutate)
        {
            Transform existingClosure = FindExact(mapRoot, ClosureRootName, false);
            if (existingClosure != null)
                Undo.DestroyObjectImmediate(existingClosure.gameObject);

            GameObject closureObject = new GameObject(ClosureRootName);
            Undo.RegisterCreatedObjectUndo(
                closureObject,
                "Create Living Gallery gap closure");
            closureObject.transform.SetParent(mapRoot, false);

            BuildVisibilityClosure(
                closureObject.transform,
                visual,
                gameplay);
            BuildInkClosure(
                closureObject.transform,
                mapRoot,
                gameplay);
            BuildGateClosure(
                closureObject.transform,
                mapRoot,
                visual,
                gameplay);
            BuildPigmentClosure(mapRoot);
            BuildPaperClosure(mapRoot);
            BuildMovingCanvasClosure(
                closureObject.transform,
                mapRoot,
                visual,
                gameplay);
            BuildFallRespawnClosure(
                closureObject.transform,
                mapRoot,
                collision,
                gameplay);
            BuildRegionClosure(gameplay);

            LivingGalleryGapClosureState state =
                GetOrAdd<LivingGalleryGapClosureState>(mapRoot.gameObject);
            state.Configure(BuildResolutionRecords());
            EditorUtility.SetDirty(state);

            EditorSceneManager.MarkSceneDirty(mapRoot.gameObject.scene);
            AssetDatabase.SaveAssets();
        }

        CollectReport(mapRoot, ref report);
        report.Details = BuildDetails(mapRoot, report) + manifestBaselineDetails;
        return report;
    }

    private static void BuildVisibilityClosure(
        Transform closureRoot,
        Transform visual,
        Transform gameplay)
    {
        GameObject group = NewChild(closureRoot, "VisibilityGapClosure");
        GameObject portalGroup = NewChild(group.transform, "PortalRefreshTriggers");

        PainterVisibilityVolume[] volumes =
            closureRoot.root.GetComponentsInChildren<PainterVisibilityVolume>(true);

        foreach (PortalSpec spec in PortalSpecs)
        {
            PainterVisibilityVolume volume = volumes.FirstOrDefault(
                candidate => candidate != null &&
                             string.Equals(
                                 candidate.BindingId,
                                 spec.VolumeId,
                                 StringComparison.Ordinal));
            if (volume == null)
                continue;

            for (int index = 0; index < spec.Sources.Length; index++)
            {
                Transform source = FindExact(visual, spec.Sources[index], false);
                if (source == null || !TryGetWorldBounds(source, out Bounds bounds))
                    continue;

                CreatePortalRefreshTrigger(
                    portalGroup.transform,
                    spec.VolumeId + "_PORTAL_" + index,
                    bounds,
                    volume);
            }
        }

        CreateThresholdVisibilityVolume(
            group.transform,
            visual,
            "VIS_Entry_processional",
            "COVER_V02_Entry_Processional_OpaqueRoof",
            "BRIDGE_Entry_WestLoop",
            new[]
            {
                "COVER_V02_Entry_Processional_OpaqueRoof",
                "COVER_V02_Entry_OrientationScreen"
            });

        CreateThresholdVisibilityVolume(
            group.transform,
            visual,
            "VIS_Archive_threshold",
            "COVER_V02_Archive_Threshold_OpaqueRoof",
            "BRIDGE_V02_Archive_West_Threshold",
            new[]
            {
                "COVER_V02_Archive_Threshold_OpaqueRoof",
                "COVER_V02_Archive_Approach_Baffle_0",
                "COVER_V02_Archive_Approach_Baffle_1",
                "COVER_V02_Archive_Approach_Baffle_2"
            });
    }

    private static void CreatePortalRefreshTrigger(
        Transform parent,
        string name,
        Bounds sourceBounds,
        PainterVisibilityVolume volume)
    {
        GameObject go = NewChild(parent, name);
        go.transform.SetPositionAndRotation(sourceBounds.center, Quaternion.identity);

        Vector3 size = sourceBounds.size;
        size.y = Mathf.Max(size.y, 2.6f);

        // Portal refresh does not need to occupy the whole connected bridge.
        // Keep the larger horizontal axis thin so it behaves like a doorway.
        if (size.x >= size.z)
            size.x = Mathf.Min(size.x, 1.25f);
        else
            size.z = Mathf.Min(size.z, 1.25f);

        size.x = Mathf.Max(size.x, 0.6f);
        size.z = Mathf.Max(size.z, 0.6f);

        BoxCollider box = Undo.AddComponent<BoxCollider>(go);
        box.isTrigger = true;
        box.size = size;

        LivingGalleryVisibilityPortal portal =
            Undo.AddComponent<LivingGalleryVisibilityPortal>(go);
        portal.Configure(name, volume, box);
    }

    private static void CreateThresholdVisibilityVolume(
        Transform parent,
        Transform visual,
        string id,
        string roofName,
        string walkableName,
        string[] blockerNames)
    {
        Transform roof = FindExact(visual, roofName, false);
        Transform walkable = FindExact(visual, walkableName, false);
        if (roof == null || walkable == null ||
            !TryGetWorldBounds(roof, out Bounds roofBounds) ||
            !TryGetWorldBounds(walkable, out Bounds walkableBounds))
        {
            return;
        }

        GameObject go = NewChild(parent, id + "_UNITY_VOLUME");
        go.transform.SetPositionAndRotation(roof.position, roof.rotation);
        go.transform.localScale = Vector3.one;

        Matrix4x4 worldToLocal = go.transform.worldToLocalMatrix;
        Bounds roofLocal = TransformBounds(roofBounds, worldToLocal);
        Bounds walkLocal = TransformBounds(walkableBounds, worldToLocal);

        float minX = Mathf.Max(roofLocal.min.x, walkLocal.min.x);
        float maxX = Mathf.Min(roofLocal.max.x, walkLocal.max.x);
        float minZ = Mathf.Max(roofLocal.min.z, walkLocal.min.z);
        float maxZ = Mathf.Min(roofLocal.max.z, walkLocal.max.z);

        if (maxX <= minX + 0.1f)
        {
            minX = roofLocal.min.x;
            maxX = roofLocal.max.x;
        }
        if (maxZ <= minZ + 0.1f)
        {
            minZ = roofLocal.min.z;
            maxZ = roofLocal.max.z;
        }

        float minY = walkLocal.max.y - 0.18f;
        float maxY = roofLocal.max.y + 0.12f;
        if (maxY <= minY + 1.8f)
            maxY = minY + Mathf.Max(2.6f, roofLocal.size.y + 2f);

        BoxCollider box = Undo.AddComponent<BoxCollider>(go);
        box.isTrigger = true;
        box.center = new Vector3(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            (minZ + maxZ) * 0.5f);
        box.size = new Vector3(
            Mathf.Max(0.6f, maxX - minX),
            Mathf.Max(1.8f, maxY - minY),
            Mathf.Max(0.6f, maxZ - minZ));

        PainterVisibilityVolume volume =
            Undo.AddComponent<PainterVisibilityVolume>(go);
        volume.Configure(id, PainterFigureVisibility.Hidden, box);

        Transform[] blockers = blockerNames
            .Select(name => FindExact(visual, name, false))
            .Where(transform => transform != null)
            .ToArray();
        PainterOcclusionRegion region =
            Undo.AddComponent<PainterOcclusionRegion>(go);
        region.Configure(id, blockers);

        LivingGalleryAnchor anchor =
            Undo.AddComponent<LivingGalleryAnchor>(go);
        anchor.Configure(
            LivingGalleryAnchorKind.Visibility,
            id,
            "Unity-derived occupancy volume clipped from authored roof + walkable geometry.");
    }

    private static void BuildInkClosure(
        Transform closureRoot,
        Transform mapRoot,
        Transform gameplay)
    {
        InkReflectionDecoySystem system =
            mapRoot.GetComponentInChildren<InkReflectionDecoySystem>(true);
        if (system == null)
            return;

        system.ConfigurePresenceDetection(
            10.5f,
            14.5f,
            2.6f,
            1.6f,
            2.25f);
        EditorUtility.SetDirty(system);

        Transform pool = FindExact(gameplay, "GP_InkPool_Center", false);
        GameObject presenterObject =
            NewChild(closureRoot, "INK_DECOY_UnityPresenter");
        LivingGalleryInkDecoyPresenter presenter =
            Undo.AddComponent<LivingGalleryInkDecoyPresenter>(presenterObject);
        presenter.Configure(system, pool, 0.42f);
    }

    private static void BuildGateClosure(
        Transform closureRoot,
        Transform mapRoot,
        Transform visual,
        Transform gameplay)
    {
        GameObject group = NewChild(closureRoot, "FrameGateEndpointBindings");

        Dictionary<string, string[]> routes = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            {
                "FRAME_01",
                new[]
                {
                    "PLATFORM_03_EASEL_COURT",
                    "BRIDGE_Ring_East_Court",
                    "STAIR_East_Drying_Ascent",
                    "STAIR_East_Gallery_Ascent"
                }
            },
            {
                "FRAME_02",
                new[]
                {
                    "PLATFORM_08_EAST_GATE_LANDING",
                    "BRIDGE_North_East_Connection",
                    "STAIR_East_Upper_Stair_B",
                    "BRIDGE_East_Exit_Alternate",
                    "STAIR_V02_Archive_Final_Ascent"
                }
            },
            {
                "FRAME_03",
                new[]
                {
                    "PLATFORM_FrameGate03_Landing",
                    "PLATFORM_07_WEST_LANDING",
                    "BRIDGE_West_North_Connection"
                }
            }
        };

        for (int index = 1; index <= 3; index++)
        {
            string id = "FRAME_0" + index;
            RotatingFrameGate gate =
                mapRoot.GetComponentsInChildren<RotatingFrameGate>(true)
                    .FirstOrDefault(candidate =>
                        candidate != null &&
                        string.Equals(candidate.BindingId, id, StringComparison.Ordinal));
            Transform pivot = FindExact(
                gameplay,
                "GP_FrameGate_0" + index + "_Pivot",
                false);
            if (gate == null || pivot == null)
                continue;

            GameObject bindingObject = NewChild(group.transform, id + "_UnityRouteBinding");
            GameObject openObject = NewChild(bindingObject.transform, "OPEN_0deg_NEUTRAL_PIVOT");
            GameObject closedObject = NewChild(bindingObject.transform, "CLOSED_90deg_NEUTRAL_PIVOT");

            openObject.transform.SetPositionAndRotation(
                pivot.position,
                pivot.rotation * Quaternion.AngleAxis(0f, Vector3.up));
            closedObject.transform.SetPositionAndRotation(
                pivot.position,
                pivot.rotation * Quaternion.AngleAxis(90f, Vector3.up));

            Transform[] nearbyRoutes = routes[id]
                .Select(name => FindExact(visual, name, false))
                .Where(transform => transform != null)
                .ToArray();

            RotatingFrameGateBindingMetadata metadata =
                Undo.AddComponent<RotatingFrameGateBindingMetadata>(bindingObject);
            metadata.Configure(
                id,
                gate,
                openObject.transform,
                closedObject.transform,
                nearbyRoutes);
        }
    }

    private static void BuildPigmentClosure(Transform mapRoot)
    {
        PigmentChannel[] channels =
            mapRoot.GetComponentsInChildren<PigmentChannel>(true);
        foreach (PigmentChannel channel in channels)
        {
            if (channel == null)
                continue;
            PigmentRouteModifier modifier =
                GetOrAdd<PigmentRouteModifier>(channel.gameObject);
            modifier.ConfigureExplicitNoEffectPolicy(
                channel.ChannelId,
                "Source handoff authors flow source/destination/diverter but no route target/effect. " +
                "M55.8.3 therefore keeps route modification intentionally inactive instead of inferring gameplay from colour/proximity.");
            EditorUtility.SetDirty(modifier);
        }
    }

    private static void BuildPaperClosure(Transform mapRoot)
    {
        DryingPaperSurface[] papers =
            mapRoot.GetComponentsInChildren<DryingPaperSurface>(true);
        foreach (DryingPaperSurface paper in papers)
        {
            if (paper == null)
                continue;
            float sag = string.Equals(
                paper.BindingId,
                "PAPER_01",
                StringComparison.Ordinal)
                ? 0.14f
                : 0.10f;
            paper.ConfigureCoarseCollisionStates(sag);
            EditorUtility.SetDirty(paper);
        }
    }

    private static void BuildMovingCanvasClosure(
        Transform closureRoot,
        Transform mapRoot,
        Transform visual,
        Transform gameplay)
    {
        GameObject group = NewChild(closureRoot, "MovingCanvasCompanions");

        ConfigureMovingCanvasCompanion(
            group.transform,
            mapRoot,
            visual,
            gameplay,
            "MOVING_CANVAS_02",
            "GP_CanvasPivot_Wash01",
            new[]
            {
                "CANVAS_PaintCurtain_Wash01",
                "CANVAS_HangingStrap_Wash01",
                "CANVAS_HangingStrap_Wash01.001"
            });

        ConfigureMovingCanvasCompanion(
            group.transform,
            mapRoot,
            visual,
            gameplay,
            "MOVING_CANVAS_04",
            "GP_CanvasPivot_Wash03",
            new[]
            {
                "CANVAS_PaintCurtain_Wash03",
                "CANVAS_HangingStrap_Wash03",
                "CANVAS_HangingStrap_Wash03.001"
            });
    }

    private static void ConfigureMovingCanvasCompanion(
        Transform parent,
        Transform mapRoot,
        Transform visual,
        Transform gameplay,
        string sourceId,
        string pivotName,
        string[] followerNames)
    {
        MovingCanvasObstacle source =
            mapRoot.GetComponentsInChildren<MovingCanvasObstacle>(true)
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    candidate.name.IndexOf(
                        sourceId.EndsWith("02", StringComparison.Ordinal) ? "02" : "04",
                        StringComparison.Ordinal) >= 0);

        // If the component is hosted directly on the imported visual, name matching
        // is reliable; otherwise fall back to order-specific visual lookup.
        if (source == null)
        {
            string visualName = sourceId.EndsWith("02", StringComparison.Ordinal)
                ? "INT_HangingWetCanvas_02"
                : "INT_HangingWetCanvas_04";
            Transform sourceVisual = FindExact(visual, visualName, false);
            source = sourceVisual != null
                ? sourceVisual.GetComponent<MovingCanvasObstacle>()
                : null;
        }

        Transform pivot = FindExact(gameplay, pivotName, false);
        Transform[] followers = followerNames
            .Select(name => FindExact(visual, name, false))
            .Where(transform => transform != null)
            .ToArray();

        if (source == null || pivot == null || followers.Length == 0)
            return;

        source.ConfigureMotion(25f);
        EditorUtility.SetDirty(source);

        GameObject go = NewChild(parent, sourceId + "_WashFollower");
        MovingCanvasCompanionFollower follower =
            Undo.AddComponent<MovingCanvasCompanionFollower>(go);
        follower.Configure(source, pivot, followers);
    }

    private static void BuildFallRespawnClosure(
        Transform closureRoot,
        Transform mapRoot,
        Transform collision,
        Transform gameplay)
    {
        Transform entry = FindExact(gameplay, "GP_ENTRY_From_PrismaticReach", false);
        if (entry == null)
            return;

        GameObject respawnPointObject = NewChild(closureRoot, "RespawnPoint_Entry_UnityAuthored");
        respawnPointObject.transform.SetPositionAndRotation(
            entry.position,
            entry.rotation);
        LivingGalleryAnchor respawnAnchor =
            Undo.AddComponent<LivingGalleryAnchor>(respawnPointObject);
        respawnAnchor.Configure(
            LivingGalleryAnchorKind.Interaction,
            "UNITY_RESPAWN_ENTRY",
            "Unity-authored provisional respawn derived from the authored entry reference; not claimed as a Blender spawn helper.");

        FigureMotor figure = FindSceneComponent<FigureMotor>();
        LivingGalleryRespawnCoordinator coordinator =
            Undo.AddComponent<LivingGalleryRespawnCoordinator>(closureRoot.gameObject);
        coordinator.Configure(figure, respawnPointObject.transform, 0.08f);

        Collider[] colliders = collision.GetComponentsInChildren<Collider>(true)
            .Where(collider =>
                collider != null &&
                collider.enabled &&
                collider.gameObject.activeInHierarchy &&
                !collider.isTrigger)
            .ToArray();
        if (colliders.Length == 0)
            return;

        Bounds mapBounds = colliders[0].bounds;
        for (int index = 1; index < colliders.Length; index++)
            mapBounds.Encapsulate(colliders[index].bounds);

        Collider[] walkable = colliders
            .Where(collider => IsWalkableCollisionName(collider.name))
            .ToArray();
        float minimumWalkableY = walkable.Length > 0
            ? walkable.Min(collider => collider.bounds.min.y)
            : mapBounds.min.y;

        float fallTop = minimumWalkableY - 3.5f;
        float fallHeight = 60f;
        Vector3 center = new Vector3(
            mapBounds.center.x,
            fallTop - fallHeight * 0.5f,
            mapBounds.center.z);
        Vector3 size = new Vector3(
            mapBounds.size.x + 40f,
            fallHeight,
            mapBounds.size.z + 40f);

        GameObject zoneObject = NewChild(closureRoot, "FallZone_BelowPlayableGeometry_UnityAuthored");
        zoneObject.transform.SetPositionAndRotation(center, Quaternion.identity);
        BoxCollider box = Undo.AddComponent<BoxCollider>(zoneObject);
        box.isTrigger = true;
        box.size = size;

        LivingGalleryFallZone fallZone =
            Undo.AddComponent<LivingGalleryFallZone>(zoneObject);
        fallZone.Configure(
            "FALL_RESPAWN_UNITY",
            coordinator,
            box);
    }

    private static void BuildRegionClosure(Transform gameplay)
    {
        Transform entry = FindExact(gameplay, "GP_ENTRY_From_PrismaticReach", false);
        Transform exit = FindExact(gameplay, "GP_EXIT_To_NextRegion", false);

        if (entry != null)
        {
            LivingGalleryRegionTransition incoming =
                GetOrAdd<LivingGalleryRegionTransition>(entry.gameObject);
            incoming.ConfigureIncoming(
                "TRANSITION_ENTRY",
                "Prismatic Reach");
            EditorUtility.SetDirty(incoming);
        }

        if (exit != null)
        {
            LivingGalleryRegionTransition outgoing =
                GetOrAdd<LivingGalleryRegionTransition>(exit.gameObject);
            outgoing.ConfigureCurrentJourneyTerminal("TRANSITION_EXIT");
            EditorUtility.SetDirty(outgoing);
        }
    }

    private static LivingGalleryGapResolutionRecord[] BuildResolutionRecords()
    {
        return new[]
        {
            new LivingGalleryGapResolutionRecord(
                "VISIBILITY_PORTALS",
                LivingGalleryGapResolutionKind.ExistingRuntimeVolume,
                true,
                "Existing four HIDDEN occupancy volumes remain authoritative; Unity-only opening triggers now force overlap-safe occupancy refresh at authored portals/connected thresholds."),
            new LivingGalleryGapResolutionRecord(
                "THRESHOLD_VOLUMES",
                LivingGalleryGapResolutionKind.DerivedFromAuthoredGeometry,
                true,
                "Two HIDDEN threshold volumes are derived from the exact authored opaque roof + walkable bridge bounds. Thin roof colliders are not reused as presence triggers."),
            new LivingGalleryGapResolutionRecord(
                "INK_DETECTION",
                LivingGalleryGapResolutionKind.RuntimePositionDetection,
                true,
                "Figure presence is detected from existing FigureMotor world position inside the authored 10.5-14.5m ink ring; decoy location/orientation is derived only from authority-safe event seed and authored reflection zones."),
            new LivingGalleryGapResolutionRecord(
                "GATE_ENDPOINT_TRANSFORMS",
                LivingGalleryGapResolutionKind.ExplicitUnityRouteBinding,
                true,
                "Unity-only 0/90 degree endpoint references are created from each neutral pivot basis and handoff-listed nearby route transforms are bound directly."),
            new LivingGalleryGapResolutionRecord(
                "PIGMENT_EFFECTS",
                LivingGalleryGapResolutionKind.ExplicitNoEffectPolicy,
                true,
                "No target/effect pair exists in the source, so all three PigmentRouteModifier instances are explicitly None/unconfigured. Visual source-to-destination flow remains active; no colour-based gameplay inference is made."),
            new LivingGalleryGapResolutionRecord(
                "PAPER_COLLISION_STATES",
                LivingGalleryGapResolutionKind.GeneratedCoarseCollisionState,
                true,
                "The dedicated low-resolution BASIS proxy is cloned at runtime. DRY preserves BASIS; WET adds the authored 0.14m/0.10m centre sag and the coarse proxy interpolates with the visual state."),
            new LivingGalleryGapResolutionRecord(
                "CANVAS_SWING_SETUP",
                LivingGalleryGapResolutionKind.ExplicitMotionPolicy,
                true,
                "Both authored moving canvases use the handoff-proposed top-bar local-X swing, max 10 degrees, 25 deg/s. Wash overlays + their straps follow their own pivots; fixed hangers remain stationary and no hard collider is added."),
            new LivingGalleryGapResolutionRecord(
                "FALL_SPAWN_RESPAWN",
                LivingGalleryGapResolutionKind.ExistingCharacterAuthority,
                true,
                "A fall volume is derived below the lowest actual walkable collision proxy, not visual height. Respawn uses the authored ENTRY as an explicitly Unity-authored provisional point and FigureMotor.Teleport/ResetMotion as the existing local character authority."),
            new LivingGalleryGapResolutionRecord(
                "REGION_DESTINATION",
                LivingGalleryGapResolutionKind.CurrentJourneyTerminalPolicy,
                true,
                "ENTRY is explicitly Prismatic Reach incoming context. EXIT is the end of the currently implemented journey and remains owned by the existing journey/match exit; no fictional next-region ID is invented.")
        };
    }

    private static void CollectReport(Transform mapRoot, ref Report report)
    {
        LivingGalleryGapClosureState closureState =
            mapRoot.GetComponent<LivingGalleryGapClosureState>();
        report.UnityResolvedGaps = closureState != null
            ? closureState.ResolvedCount
            : 0;

        report.VisibilityPortals =
            mapRoot.GetComponentsInChildren<LivingGalleryVisibilityPortal>(true).Length;
        report.VisibilityVolumes =
            mapRoot.GetComponentsInChildren<PainterVisibilityVolume>(true).Length;

        InkReflectionDecoySystem[] inkSystems =
            mapRoot.GetComponentsInChildren<InkReflectionDecoySystem>(true);
        report.InkPresenceSystems = inkSystems.Count(system =>
            system != null && system.AutomaticPresenceDetection &&
            Mathf.Abs(system.PresenceInnerRadius - 10.5f) < 0.05f &&
            Mathf.Abs(system.PresenceOuterRadius - 14.5f) < 0.05f);
        report.InkPresenters =
            mapRoot.GetComponentsInChildren<LivingGalleryInkDecoyPresenter>(true)
                .Count(presenter => presenter != null && presenter.IsConfigured);

        report.GateMetadata =
            mapRoot.GetComponentsInChildren<RotatingFrameGateBindingMetadata>(true)
                .Count(metadata => metadata != null && metadata.IsResolved);
        report.PigmentPolicies =
            mapRoot.GetComponentsInChildren<PigmentRouteModifier>(true)
                .Count(modifier => modifier != null && modifier.HasResolvedPolicy);
        report.PaperCollisionPolicies =
            mapRoot.GetComponentsInChildren<DryingPaperSurface>(true)
                .Count(paper => paper != null && paper.CoarseCollisionStatesConfigured);
        report.MovingCanvasFollowers =
            mapRoot.GetComponentsInChildren<MovingCanvasCompanionFollower>(true)
                .Count(follower => follower != null && follower.IsConfigured);
        report.FallZones =
            mapRoot.GetComponentsInChildren<LivingGalleryFallZone>(true).Length;
        report.RespawnCoordinators =
            mapRoot.GetComponentsInChildren<LivingGalleryRespawnCoordinator>(true)
                .Count(coordinator => coordinator != null && coordinator.IsConfigured);
        report.RegionTransitions =
            mapRoot.GetComponentsInChildren<LivingGalleryRegionTransition>(true)
                .Count(transition => transition != null && transition.IsResolved);

        if (closureState == null || !closureState.AllResolved)
            report.HardErrors++;
        if (report.VisibilityVolumes != 6)
            report.HardErrors++;
        if (report.VisibilityPortals < 8)
            report.HardErrors++;
        if (report.InkPresenceSystems != 1 || report.InkPresenters != 1)
            report.HardErrors++;
        if (report.GateMetadata != 3)
            report.HardErrors++;
        if (report.PigmentPolicies != 3)
            report.HardErrors++;
        if (report.PaperCollisionPolicies != 2)
            report.HardErrors++;
        if (report.MovingCanvasFollowers != 2)
            report.HardErrors++;
        if (report.FallZones != 1 || report.RespawnCoordinators != 1)
            report.HardErrors++;
        if (report.RegionTransitions != 2)
            report.HardErrors++;
    }

    private static string BuildDetails(Transform mapRoot, Report report)
    {
        LivingGalleryGapClosureState state =
            mapRoot != null
                ? mapRoot.GetComponent<LivingGalleryGapClosureState>()
                : null;

        if (state == null)
            return "Gap closure state yok.";

        string[] lines = state.Resolutions.Select(record =>
            (record.resolved ? "[RESOLVED] " : "[MISSING] ") +
            record.gapId + " — " + record.kind + "\n  " + record.note)
            .ToArray();

        return string.Join("\n", lines);
    }

    public static string Format(Report report)
    {
        return
            "Result=" + (report.Pass ? "PASS" : "NEEDS_ATTENTION") + "\n" +
            "ManifestBaseline=" + (report.ManifestPass ? "PASS" : "FAIL") + "\n" +
            "SourceAuthoredGaps=" + report.SourceGaps + "\n" +
            "UnityResolvedGaps=" + report.UnityResolvedGaps + "/9\n" +
            "VisibilityPortals=" + report.VisibilityPortals + " (expected >=8)\n" +
            "VisibilityVolumes=" + report.VisibilityVolumes + "/6\n" +
            "InkPresenceSystems=" + report.InkPresenceSystems + "/1\n" +
            "InkPresenters=" + report.InkPresenters + "/1\n" +
            "GateEndpointBindings=" + report.GateMetadata + "/3\n" +
            "PigmentExplicitPolicies=" + report.PigmentPolicies + "/3\n" +
            "PaperCoarseCollisionPolicies=" + report.PaperCollisionPolicies + "/2\n" +
            "MovingCanvasFollowers=" + report.MovingCanvasFollowers + "/2\n" +
            "FallZones=" + report.FallZones + "/1\n" +
            "RespawnCoordinators=" + report.RespawnCoordinators + "/1\n" +
            "RegionTransitions=" + report.RegionTransitions + "/2\n" +
            "HardErrors=" + report.HardErrors + "\n\n" +
            report.Details;
    }

    private static bool IsWalkableCollisionName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return
            name.StartsWith("COLLISION_PLATFORM_", StringComparison.Ordinal) ||
            name.StartsWith("COLLISION_BRIDGE_", StringComparison.Ordinal) ||
            name.StartsWith("COLLISION_STAIR_", StringComparison.Ordinal) ||
            name.StartsWith("COLLISION_INT_TensionCanvas_", StringComparison.Ordinal) ||
            name.StartsWith("COLLISION_INT_DryingPaper_", StringComparison.Ordinal);
    }

    private static bool TryGetWorldBounds(Transform source, out Bounds bounds)
    {
        bounds = default;
        if (source == null)
            return false;

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            bool initialized = false;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                    continue;
                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            if (initialized)
                return true;
        }

        Collider[] colliders = source.GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
        {
            bool initialized = false;
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (collider == null)
                    continue;
                if (!initialized)
                {
                    bounds = collider.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
            return initialized;
        }

        return false;
    }

    private static Bounds TransformBounds(Bounds worldBounds, Matrix4x4 worldToLocal)
    {
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        Vector3 first = worldToLocal.MultiplyPoint3x4(corners[0]);
        Bounds local = new Bounds(first, Vector3.zero);
        for (int index = 1; index < corners.Length; index++)
            local.Encapsulate(worldToLocal.MultiplyPoint3x4(corners[index]));
        return local;
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
        if (component == null)
            component = Undo.AddComponent<T>(go);
        return component;
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        T[] components = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        return components.FirstOrDefault(component =>
            component != null &&
            component.gameObject.scene.IsValid() &&
            !EditorUtility.IsPersistent(component));
    }

    private static Transform FindExact(
        Transform root,
        string name,
        bool throwOnDuplicate)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        Transform[] matches = root.GetComponentsInChildren<Transform>(true)
            .Where(transform =>
                transform != null &&
                string.Equals(transform.name, name, StringComparison.Ordinal))
            .ToArray();

        if (matches.Length > 1 && throwOnDuplicate)
            throw new InvalidOperationException("Duplicate exact-name object: " + name);

        return matches.Length > 0 ? matches[0] : null;
    }

    private static GameObject FindSceneObject(string name)
    {
        GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        return objects.FirstOrDefault(go =>
            go != null &&
            go.scene.IsValid() &&
            !EditorUtility.IsPersistent(go) &&
            string.Equals(go.name, name, StringComparison.Ordinal));
    }
}
#endif
