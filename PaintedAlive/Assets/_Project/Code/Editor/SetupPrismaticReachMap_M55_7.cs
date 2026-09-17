#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PaintedAlive.Environment.PrismaticReach;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class SetupPrismaticReachMap_M55_7
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";

    private const string VisualModelPath =
        "Assets/_Project/Art/Models/Environments/PrismaticReach/Prismatic_Reach_Visual.fbx";
    private const string CollisionModelPath =
        "Assets/_Project/Art/Models/Environments/PrismaticReach/Prismatic_Reach_Collision.fbx";
    private const string GameplayModelPath =
        "Assets/_Project/Art/Models/Environments/PrismaticReach/Prismatic_Reach_Gameplay.fbx";

    private const string MaterialFolder =
        "Assets/_Project/Materials/Environment/PrismaticReach/Generated";

    private const string RootName = "M55_7_PrismaticReach_Continuation";
    private const string OldReplacementRootName = "M55_7_PrismaticReach";
    private const string VisualRootName = "Prismatic_Reach_Visual";
    private const string CollisionRootName = "Prismatic_Reach_Collision";
    private const string GameplayRootName = "Prismatic_Reach_Gameplay";
    private const string BindingsRootName = "GameplayBindings";
    private const string TrampolinesRootName = "Trampolines";
    private const string LegacyAtelierRootName = "M55_4_AtelierTestScene";

    private const string CollisionLayerName = "PrismaticCollision";
    private const string WaterLayerName = "WaterVisual";
    private const string TrampolineLayerName = "Trampoline";

    private const string AtelierGeneratedRootName = "M55_6_AtelierGameplayPass";
    private const string AtelierRouteRootName = "RouteAnchors";
    private const string AtelierFinalAnchorName = "Route_FINAL_FRAME";
    private const string AtelierStartAnchorName = "Route_START";
    private const string JourneyScoreConfigPath = "Assets/_Project/Data/Core/DA_PrototypeJourneyScore.asset";
    private const float ConnectionGap = 0.00f;

    private sealed class HelperSpec
    {
        public string SourceName;
        public string UnityName;
        public PrismaticReachAnchorKind Kind;
        public string Note;

        public HelperSpec(string sourceName, string unityName, PrismaticReachAnchorKind kind, string note)
        {
            SourceName = sourceName;
            UnityName = unityName;
            Kind = kind;
            Note = note;
        }
    }

    private static readonly HelperSpec[] HelperSpecs =
    {
        new HelperSpec("PR_ANCHOR_COURT_CHECKPOINT", "Checkpoint_PRISM_COURT", PrismaticReachAnchorKind.Checkpoint,
            "Open hub checkpoint. Paint compass is decorative."),
        new HelperSpec("PR_ANCHOR_EAST_PAVILION_ENCOUNTER", "Encounter_EAST_PAVILION", PrismaticReachAnchorKind.Encounter,
            "Optional encounter pocket; keep centre traversal clear."),
        new HelperSpec("PR_ANCHOR_ENTRY_SEAM", "Route_ENTRY_SEAM", PrismaticReachAnchorKind.Entry,
            "Start-side route seam / handoff marker."),
        new HelperSpec("PR_ANCHOR_EXIT_GATE", "Route_EXIT_GATE", PrismaticReachAnchorKind.Exit,
            "Future chapter exit trigger. Authored gate width is handled later by match flow."),
        new HelperSpec("PR_ANCHOR_FALL_RESPAWN", "Fall_RESPAWN", PrismaticReachAnchorKind.FallRespawn,
            "Respawn target for the later Fall Zone pass. No kill volume is created in M55.7."),
        new HelperSpec("PR_ANCHOR_SPAWN", "FigureSpawn_PRISMATIC_REACH", PrismaticReachAnchorKind.Spawn,
            "Player feet / checkpoint spawn."),
        new HelperSpec("PR_ANCHOR_WEST_BELVEDERE_ENCOUNTER", "Encounter_WEST_BELVEDERE", PrismaticReachAnchorKind.Encounter,
            "Optional encounter pocket; keep centre traversal clear."),
        new HelperSpec("TRAMPOLINE_CERULEAN_CENTER", "Trampoline_CERULEAN_CENTER", PrismaticReachAnchorKind.TrampolineCenter,
            "Bounce origin guide."),
        new HelperSpec("TRAMPOLINE_CERULEAN_LANDING", "Trampoline_CERULEAN_LANDING", PrismaticReachAnchorKind.TrampolineLanding,
            "Controlled launch target."),
        new HelperSpec("TRAMPOLINE_CERULEAN_LAUNCH", "Trampoline_CERULEAN_LAUNCH", PrismaticReachAnchorKind.TrampolineLaunch,
            "Directional launch guide."),
        new HelperSpec("TRAMPOLINE_VIOLET_CENTER", "Trampoline_VIOLET_CENTER", PrismaticReachAnchorKind.TrampolineCenter,
            "Bounce origin guide."),
        new HelperSpec("TRAMPOLINE_VIOLET_LANDING", "Trampoline_VIOLET_LANDING", PrismaticReachAnchorKind.TrampolineLanding,
            "Controlled launch target."),
        new HelperSpec("TRAMPOLINE_VIOLET_LAUNCH", "Trampoline_VIOLET_LAUNCH", PrismaticReachAnchorKind.TrampolineLaunch,
            "Directional launch guide.")
    };

    [MenuItem(MenuRoot + "55.7 - Setup + Append Prismatic Reach")]
    public static void SetupAsContinuation()
    {
        try
        {
            RequireEditMode();
            Scene scene = RequireActiveScene();

            ConfigureImporter(VisualModelPath);
            ConfigureImporter(CollisionModelPath);
            ConfigureImporter(GameplayModelPath);

            GameObject visualAsset = RequireModel(VisualModelPath);
            GameObject collisionAsset = RequireModel(CollisionModelPath);
            GameObject gameplayAsset = RequireModel(GameplayModelPath);

            GameObject oldReplacement = FindSceneObjectByName(OldReplacementRootName);
            if (oldReplacement != null)
                Undo.DestroyObjectImmediate(oldReplacement);

            GameObject previous = FindSceneObjectByName(RootName);
            if (previous != null)
                Undo.DestroyObjectImmediate(previous);

            GameObject legacy = FindSceneObjectByName(LegacyAtelierRootName);
            if (legacy == null)
                throw new InvalidOperationException("Mevcut Atelier map root bulunamadı: " + LegacyAtelierRootName);

            if (!legacy.activeSelf)
            {
                Undo.RecordObject(legacy, "Reactivate Atelier base map");
                legacy.SetActive(true);
                EditorUtility.SetDirty(legacy);
            }

            Transform atelierFinal = FindAtelierFinalAnchor(legacy.transform);
            if (atelierFinal == null)
                throw new InvalidOperationException("Atelier Route_FINAL_FRAME bulunamadı. M55.6 gameplay pass sahnede olmalı.");

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Prismatic Reach continuation root");
            SceneManager.MoveGameObjectToScene(root, scene);
            ResetTransform(root.transform);

            GameObject visual = InstantiateModel(visualAsset, root.transform, VisualRootName, scene);
            GameObject collision = InstantiateModel(collisionAsset, root.transform, CollisionRootName, scene);
            GameObject gameplay = InstantiateModel(gameplayAsset, root.transform, GameplayRootName, scene);

            int collisionLayer = EnsureLayer(CollisionLayerName);
            int waterLayer = EnsureLayer(WaterLayerName);
            int trampolineLayer = EnsureLayer(TrampolineLayerName);

            VisualStats visualStats = ConfigureVisualLayer(visual, waterLayer);
            CollisionStats collisionStats = ConfigureCollisionLayer(collision, collisionLayer, trampolineLayer);
            int helperCount = BuildGameplayBindings(gameplay.transform, root.transform);
            int trampolineCount = BuildNativeTrampolines(
                root.transform,
                collision.transform,
                trampolineLayer);

            Transform entry = FindBinding(root.transform, "PR_ANCHOR_ENTRY_SEAM");
            if (entry == null)
                throw new InvalidOperationException("Prismatic Reach ENTRY_SEAM helper bulunamadı.");

            AlignContinuationRoot(root.transform, entry, atelierFinal, ConnectionGap);
            bool journeyRebound = RebindExistingJourneyFinish(root.transform);
            Transform atelierStart = FindAtelierStartAnchor(legacy.transform);
            bool figureMovedToAtelierStart = TryMoveFigureTo(atelierStart);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Transform exit = FindBinding(root.transform, "PR_ANCHOR_EXIT_GATE");
            float seamDistance = Vector3.Distance(entry.position, atelierFinal.position + atelierFinal.forward * ConnectionGap);
            float seamAngle = Quaternion.Angle(entry.rotation, atelierFinal.rotation);

            string summary =
                "Prismatic Reach mevcut Atelier map'in DEVAMINA eklendi.\n\n" +
                "Atelier map aktif bırakıldı.\n" +
                "Figure yeni Prismatic spawn'ına taşınmadı; mevcut Atelier START noktasına geri kondu.\n" +
                "Prismatic parent root bağlantı için taşındı/döndürüldü; üç FBX child root local identity kaldı.\n\n" +
                $"Connection seam distance: {seamDistance:F3} m\n" +
                $"Connection seam angle: {seamAngle:F2} deg\n" +
                $"Visual mesh: {visualStats.Meshes}\n" +
                $"Water visual: {visualStats.WaterMeshes}\n" +
                $"Visual collider: {visualStats.CollidersRemaining}\n" +
                $"Collision mesh: {collisionStats.Meshes}\n" +
                $"MeshCollider: {collisionStats.MeshColliders}\n" +
                $"Enabled collision renderer: {collisionStats.EnabledRenderers}\n" +
                $"Gameplay helper bound: {helperCount}/13\n" +
                $"Native trampoline bindings: {trampolineCount}/2\n" +
                $"Journey/exit rebound to Prismatic exit: {journeyRebound}\n" +
                $"Figure moved to Atelier START: {figureMovedToAtelierStart}\n" +
                $"Prismatic exit found: {exit != null}\n\n" +
                "Şimdi sahnede M55_7_PrismaticReach_Continuation root'unu seçip bağlantıyı gözle kontrol et. Gerekirse parent root'u elle milimetrik düzelt; child FBX root'larına dokunma. Parent root'u elle taşırsan ardından 55.7.2 - Rebind Journey Exit To Prismatic çalıştır.";

            Debug.Log("[Painted Alive M55.7.2] " + summary.Replace("\n", " | "), root);
            EditorUtility.DisplayDialog("Painted Alive M55.7.2", summary, "Tamam");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("M55.7.2 Prismatic Continuation Setup Hatası", ex.Message, "Tamam");
        }
    }

    [MenuItem(MenuRoot + "55.7.2 - Realign Prismatic Continuation")]
    public static void RealignContinuation()
    {
        try
        {
            RequireEditMode();
            GameObject root = FindSceneObjectByName(RootName);
            GameObject legacy = FindSceneObjectByName(LegacyAtelierRootName);
            if (root == null || legacy == null)
                throw new InvalidOperationException("Atelier veya Prismatic continuation root bulunamadı.");

            Transform target = FindAtelierFinalAnchor(legacy.transform);
            Transform entry = FindBinding(root.transform, "PR_ANCHOR_ENTRY_SEAM");
            if (target == null || entry == null)
                throw new InvalidOperationException("Route_FINAL_FRAME veya PR_ANCHOR_ENTRY_SEAM bulunamadı.");

            Undo.RecordObject(root.transform, "Realign Prismatic continuation");
            AlignContinuationRoot(root.transform, entry, target, ConnectionGap);
            RebindExistingJourneyFinish(root.transform);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Painted Alive M55.7.2] Prismatic Reach tekrar Atelier final seam'e hizalandı.", root);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("M55.7.2 Realign Hatası", ex.Message, "Tamam");
        }
    }

    [MenuItem(MenuRoot + "55.7.2 - Rebind Journey Exit To Prismatic")]
    public static void RebindJourneyExitToPrismatic()
    {
        RequireEditMode();
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("M55.7.2", "Prismatic continuation root bulunamadı.", "Tamam");
            return;
        }

        bool rebound = RebindExistingJourneyFinish(root.transform);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Painted Alive M55.7.2] Manual alignment sonrası journey/exit rebind=" + rebound, root);
    }

    [MenuItem(MenuRoot + "55.7 - Diagnose Prismatic Reach")]
    public static void Diagnose()
    {
        GameObject root = FindSceneObjectByName(RootName);
        GameObject legacy = FindSceneObjectByName(LegacyAtelierRootName);
        Transform visual = root != null ? FindChildRecursive(root.transform, VisualRootName) : null;
        Transform collision = root != null ? FindChildRecursive(root.transform, CollisionRootName) : null;
        Transform gameplay = root != null ? FindChildRecursive(root.transform, GameplayRootName) : null;
        Transform bindings = root != null ? FindChildRecursive(root.transform, BindingsRootName) : null;
        Transform target = legacy != null ? FindAtelierFinalAnchor(legacy.transform) : null;
        Transform entry = root != null ? FindBinding(root.transform, "PR_ANCHOR_ENTRY_SEAM") : null;
        Transform exit = root != null ? FindBinding(root.transform, "PR_ANCHOR_EXIT_GATE") : null;

        int visualColliders = visual != null ? visual.GetComponentsInChildren<Collider>(true).Length : -1;
        int collisionMeshes = collision != null ? collision.GetComponentsInChildren<MeshFilter>(true).Count(x => x.sharedMesh != null) : -1;
        int collisionColliders = collision != null ? collision.GetComponentsInChildren<MeshCollider>(true).Length : -1;
        int enabledRenderers = collision != null ? collision.GetComponentsInChildren<Renderer>(true).Count(x => x.enabled) : -1;
        int helperBindings = bindings != null ? bindings.GetComponentsInChildren<PrismaticReachAnchor>(true).Length : -1;
        int waterVisuals = visual != null ? visual.GetComponentsInChildren<PrismaticReachVisualSemantic>(true).Count(x => x.Kind == PrismaticReachVisualKind.Water) : -1;
        Transform trampolineRoot = root != null ? FindDirectChild(root.transform, TrampolinesRootName) : null;
        PrismaticReachTrampolineSurface[] trampolineSurfaces = trampolineRoot != null
            ? trampolineRoot.GetComponentsInChildren<PrismaticReachTrampolineSurface>(true)
            : Array.Empty<PrismaticReachTrampolineSurface>();
        int trampolines = trampolineSurfaces.Length;
        int trampolineColliderRefs = trampolineSurfaces.Sum(
            x => x != null && x.SurfaceColliders != null
                ? x.SurfaceColliders.Count(c => c != null)
                : 0);
        int generatedTrampolineColliders = trampolineRoot != null
            ? trampolineRoot.GetComponentsInChildren<Collider>(true).Length
            : 0;
        bool trampolineReferencesValid =
            trampolineSurfaces.All(
                x => x != null &&
                     x.CenterReference != null &&
                     x.LandingTarget != null &&
                     x.SurfaceColliders != null &&
                     x.SurfaceColliders.Any(c => c != null));

        bool transformsClean = IsCleanRoot(visual) && IsCleanRoot(collision) && IsCleanRoot(gameplay);
        float seamDistance = target != null && entry != null
            ? Vector3.Distance(entry.position, target.position + target.forward * ConnectionGap)
            : -1f;
        float seamAngle = target != null && entry != null
            ? Quaternion.Angle(entry.rotation, target.rotation)
            : -1f;
        bool seamAligned = seamDistance >= 0f && seamDistance <= 0.02f && seamAngle <= 0.5f;
        bool atelierActive = legacy != null && legacy.activeSelf;

        bool pass = root != null && visual != null && collision != null && gameplay != null && bindings != null &&
                    transformsClean && visualColliders == 0 && collisionMeshes > 0 &&
                    collisionColliders == collisionMeshes && enabledRenderers == 0 && helperBindings == 13 &&
                    atelierActive && target != null && entry != null && exit != null && seamAligned &&
                    trampolines == 2 && trampolineReferencesValid &&
                    trampolineColliderRefs >= 2 && generatedTrampolineColliders == 0;

        string report =
            $"Result={(pass ? "PASS" : "CHECK")}\n" +
            $"AtelierStillActive={atelierActive}\n" +
            $"ChildFBXRootsLocalIdentity={transformsClean}\n" +
            $"ContinuationSeamDistance={seamDistance:F3}m\n" +
            $"ContinuationSeamAngle={seamAngle:F2}deg\n" +
            $"VisualColliders={visualColliders}\n" +
            $"WaterVisuals={waterVisuals}\n" +
            $"CollisionMeshes={collisionMeshes}\n" +
            $"MeshColliders={collisionColliders}\n" +
            $"EnabledCollisionRenderers={enabledRenderers}\n" +
            $"GameplayBindings={helperBindings}/13\n" +
            $"ExitAnchorFound={exit != null}\n" +
            $"NativeTrampolines={trampolines}/2\n" +
            $"TrampolineColliderReferences={trampolineColliderRefs}\n" +
            $"GeneratedTrampolineColliders={generatedTrampolineColliders} (expected 0)\n" +
            $"TrampolineReferencesValid={trampolineReferencesValid}";

        Debug.Log("[Painted Alive M55.7.2 Diagnose]\n" + report, root);
        EditorUtility.DisplayDialog("M55.7.2 Prismatic Continuation Diagnose", report, "Tamam");
    }

    [MenuItem(MenuRoot + "55.7 - Move Figure To Prismatic Spawn")]
    public static void MoveFigureToSpawn()
    {
        GameObject root = FindSceneObjectByName(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("M55.7", "Önce Prismatic Reach setup çalıştırılmalı.", "Tamam");
            return;
        }

        Transform spawn = FindBinding(root.transform, "PR_ANCHOR_SPAWN");
        bool moved = TryMoveFigureTo(spawn);
        if (!moved)
        {
            EditorUtility.DisplayDialog("M55.7", "Loaded Scene içinde FigureMotor bulunamadı.", "Tamam");
            return;
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Painted Alive M55.7] Figure Prismatic Reach spawn'a taşındı.");
    }

    [MenuItem(MenuRoot + "55.7 - Enable Trampolines")]
    public static void EnableTrampolines()
    {
        try
        {
            RequireEditMode();
            Scene scene = RequireActiveScene();
            GameObject root = FindSceneObjectByName(RootName);
            if (root == null)
                throw new InvalidOperationException("Önce Prismatic Reach setup çalıştırılmalı.");

            Transform collision = FindChildRecursive(root.transform, CollisionRootName);
            if (collision == null)
                throw new InvalidOperationException("Prismatic collision root bulunamadı.");

            int layer = EnsureLayer(TrampolineLayerName);
            int created = BuildNativeTrampolines(root.transform, collision, layer);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            string message =
                $"Controller-native trampoline surface configured: {created}/2\n\n" +
                "CENTER + LANDING helper refs are wired directly.\n" +
                "No generated trigger/visual collider is used.\n" +
                "Existing COL_TRAMPOLINE_* MeshColliders remain the only contact geometry.\n" +
                "FigureMotor keeps movement/gravity authority; launch is ballistic and target-based.";

            Debug.Log("[Painted Alive M55.7.3] " + message.Replace("\n", " | "), root);
            EditorUtility.DisplayDialog("M55.7.3 Native Trampolines", message, "Tamam");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("M55.7.3 Trampoline Hatası", ex.Message, "Tamam");
        }
    }

    [MenuItem(MenuRoot + "55.7.3 - Setup + Repair Native Trampolines")]
    public static void SetupOrRepairNativeTrampolines()
    {
        EnableTrampolines();
    }

    [MenuItem(MenuRoot + "55.7.3 - Diagnose Native Trampolines")]
    public static void DiagnoseNativeTrampolines()
    {
        GameObject root = FindSceneObjectByName(RootName);
        Transform visual = root != null ? FindChildRecursive(root.transform, VisualRootName) : null;
        Transform trampolineRoot = root != null ? FindDirectChild(root.transform, TrampolinesRootName) : null;

        PrismaticReachTrampolineSurface[] surfaces = trampolineRoot != null
            ? trampolineRoot.GetComponentsInChildren<PrismaticReachTrampolineSurface>(true)
            : Array.Empty<PrismaticReachTrampolineSurface>();

        int generatedColliders = trampolineRoot != null
            ? trampolineRoot.GetComponentsInChildren<Collider>(true).Length
            : 0;

        int visualSailColliders = visual != null
            ? visual.GetComponentsInChildren<Collider>(true)
                .Count(c => c != null &&
                    (c.name.IndexOf("VIOLET", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     c.name.IndexOf("CERULEAN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     c.name.IndexOf("SAIL", StringComparison.OrdinalIgnoreCase) >= 0))
            : -1;

        List<string> lines = new List<string>();
        bool pass = surfaces.Length == 2 && generatedColliders == 0 && visualSailColliders == 0;

        foreach (PrismaticReachTrampolineSurface surface in surfaces.OrderBy(x => x.name))
        {
            int refs = surface.SurfaceColliders != null
                ? surface.SurfaceColliders.Count(c => c != null)
                : 0;

            bool valid =
                surface.CenterReference != null &&
                surface.LandingTarget != null &&
                refs > 0;

            pass &= valid;

            lines.Add(
                $"{surface.name}: Valid={valid}, Colliders={refs}, " +
                $"Center={(surface.CenterReference != null ? surface.CenterReference.name : "NULL")}, " +
                $"Landing={(surface.LandingTarget != null ? surface.LandingTarget.name : "NULL")}, " +
                $"Apex={surface.ApexHeight:F2}m, MinDown={surface.MinimumDownwardVelocity:F2}m/s, " +
                $"TopNormalY>={surface.MinimumTopSurfaceNormalY:F2}, " +
                $"Steering={surface.InitialHorizontalSteeringFactor:F2}->{1f:F2} / {surface.SteeringRestoreDuration:F2}s");
        }

        string report =
            $"Result={(pass ? "PASS" : "CHECK")}\n" +
            $"NativeSurfaces={surfaces.Length}/2\n" +
            $"GeneratedTriggerOrProxyColliders={generatedColliders} (expected 0)\n" +
            $"VisualSailColliders={visualSailColliders} (expected 0)\n" +
            string.Join("\n", lines);

        Debug.Log("[Painted Alive M55.7.3 Diagnose]\n" + report, root);
        EditorUtility.DisplayDialog("M55.7.3 Native Trampoline Diagnose", report, "Tamam");
    }

    [MenuItem(MenuRoot + "55.7 - Disable Trampolines")]
    public static void DisableTrampolines()
    {
        GameObject root = FindSceneObjectByName(RootName);
        Transform trampolines = root != null ? FindDirectChild(root.transform, TrampolinesRootName) : null;
        if (trampolines != null)
            Undo.DestroyObjectImmediate(trampolines.gameObject);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Painted Alive M55.7.3] Trampoline gameplay bindings kaldırıldı. Imported solid sail collision değişmedi.");
    }

    [MenuItem(MenuRoot + "55.7.2 - Disable Prismatic Continuation")]
    public static void DisablePrismaticContinuation()
    {
        RequireEditMode();
        GameObject current = FindSceneObjectByName(RootName);
        GameObject legacy = FindSceneObjectByName(LegacyAtelierRootName);

        if (current != null)
        {
            Undo.RecordObject(current, "Disable Prismatic continuation");
            current.SetActive(false);
            EditorUtility.SetDirty(current);
        }

        if (legacy != null && !legacy.activeSelf)
        {
            Undo.RecordObject(legacy, "Restore Atelier base map");
            legacy.SetActive(true);
            EditorUtility.SetDirty(legacy);
        }

        Transform atelierFinal = legacy != null ? FindAtelierFinalAnchor(legacy.transform) : null;
        if (atelierFinal != null)
            RebindJourneyFinish(atelierFinal);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Painted Alive M55.7.2] Prismatic continuation devre dışı. Atelier aktif bırakıldı.");
    }

    [MenuItem(MenuRoot + "55.7.2 - Select Prismatic Continuation Root")]
    public static void SelectContinuationRoot()
    {
        GameObject root = FindSceneObjectByName(RootName);
        Selection.activeGameObject = root;
        if (root != null)
            EditorGUIUtility.PingObject(root);
    }

    private static Transform FindAtelierFinalAnchor(Transform atelierRoot)
    {
        if (atelierRoot == null)
            return null;

        Transform generated = FindChildRecursive(atelierRoot, AtelierGeneratedRootName);
        Transform routeRoot = generated != null ? FindChildRecursive(generated, AtelierRouteRootName) : null;
        Transform final = routeRoot != null ? FindChildRecursive(routeRoot, AtelierFinalAnchorName) : null;
        if (final != null)
            return final;

        return FindChildRecursive(atelierRoot, AtelierFinalAnchorName);
    }

    private static Transform FindAtelierStartAnchor(Transform atelierRoot)
    {
        if (atelierRoot == null)
            return null;

        Transform generated = FindChildRecursive(atelierRoot, AtelierGeneratedRootName);
        Transform routeRoot = generated != null ? FindChildRecursive(generated, AtelierRouteRootName) : null;
        Transform start = routeRoot != null ? FindChildRecursive(routeRoot, AtelierStartAnchorName) : null;
        if (start != null)
            return start;

        return FindChildRecursive(atelierRoot, AtelierStartAnchorName);
    }

    private static void AlignContinuationRoot(Transform continuationRoot, Transform entry, Transform target, float forwardGap)
    {
        if (continuationRoot == null || entry == null || target == null)
            throw new ArgumentNullException("Continuation alignment requires root, entry and target.");

        Vector3 entryLocalPosition = continuationRoot.InverseTransformPoint(entry.position);
        Quaternion entryLocalRotation = Quaternion.Inverse(continuationRoot.rotation) * entry.rotation;

        Quaternion desiredRootRotation = target.rotation * Quaternion.Inverse(entryLocalRotation);
        Vector3 desiredEntryPosition = target.position + target.forward * forwardGap;
        Vector3 desiredRootPosition = desiredEntryPosition - desiredRootRotation * entryLocalPosition;

        continuationRoot.SetPositionAndRotation(desiredRootPosition, desiredRootRotation);
        continuationRoot.localScale = Vector3.one;
        EditorUtility.SetDirty(continuationRoot);
    }

    private static bool RebindExistingJourneyFinish(Transform continuationRoot)
    {
        Transform exit = FindBinding(continuationRoot, "PR_ANCHOR_EXIT_GATE");
        return RebindJourneyFinish(exit);
    }

    private static bool RebindJourneyFinish(Transform finish)
    {
        if (finish == null)
            return false;

        bool changed = false;

        GameObject m54Exit = FindSceneObjectByName("M54_FrameExit");
        if (m54Exit != null)
        {
            Undo.RecordObject(m54Exit.transform, "Move M54 exit to Prismatic final");
            m54Exit.transform.SetPositionAndRotation(finish.position, finish.rotation);
            EditorUtility.SetDirty(m54Exit.transform);
            changed = true;
        }

        PrototypeFrameExitGate gate = FindSceneComponent<PrototypeFrameExitGate>();
        if (gate != null)
        {
            Undo.RecordObject(gate.transform, "Move frame exit gate to Prismatic final");
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
                Undo.RecordObject(tracker, "Extend journey to Prismatic final");
                tracker.Configure(figure, start, finish, config);
                EditorUtility.SetDirty(tracker);
                changed = true;
            }
        }

        return changed;
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        T[] items = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (T item in items)
        {
            if (item == null || !item.gameObject.scene.IsValid() || EditorUtility.IsPersistent(item))
                continue;
            return item;
        }
        return null;
    }

    private static VisualStats ConfigureVisualLayer(GameObject visualRoot, int waterLayer)
    {
        VisualStats stats = new VisualStats();

        foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);
        }

        Dictionary<string, Material> remaps = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            stats.Meshes++;
            PrismaticReachVisualKind kind = ClassifyVisual(renderer.gameObject.name);
            PrismaticReachVisualSemantic semantic = renderer.GetComponent<PrismaticReachVisualSemantic>();
            if (semantic == null)
                semantic = Undo.AddComponent<PrismaticReachVisualSemantic>(renderer.gameObject);
            semantic.Configure(kind);
            EditorUtility.SetDirty(semantic);

            if (kind == PrismaticReachVisualKind.Water)
            {
                stats.WaterMeshes++;
                if (waterLayer >= 0)
                    renderer.gameObject.layer = waterLayer;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] replacements = new Material[sourceMaterials.Length];
            bool changed = false;
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                if (source == null)
                {
                    replacements[i] = null;
                    continue;
                }

                string key = source.name.Trim();
                if (!remaps.TryGetValue(key, out Material replacement))
                {
                    replacement = CreateOrUpdateUrpMaterial(source, kind == PrismaticReachVisualKind.Water);
                    remaps[key] = replacement;
                }
                replacements[i] = replacement;
                if (replacement != source)
                    changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = replacements;
                EditorUtility.SetDirty(renderer);
            }
        }

        stats.CollidersRemaining = visualRoot.GetComponentsInChildren<Collider>(true).Length;
        return stats;
    }

    private static CollisionStats ConfigureCollisionLayer(GameObject collisionRoot, int collisionLayer, int trampolineLayer)
    {
        CollisionStats stats = new CollisionStats();

        foreach (Renderer renderer in collisionRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        foreach (MeshFilter filter in collisionRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            stats.Meshes++;
            bool trampoline = filter.gameObject.name.StartsWith("COL_TRAMPOLINE_", StringComparison.OrdinalIgnoreCase);
            int layer = trampoline ? trampolineLayer : collisionLayer;
            if (layer >= 0)
                filter.gameObject.layer = layer;

            foreach (Collider existing in filter.GetComponents<Collider>())
            {
                if (existing is MeshCollider)
                    continue;
                Undo.DestroyObjectImmediate(existing);
            }

            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (collider == null)
                collider = Undo.AddComponent<MeshCollider>(filter.gameObject);

            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            collider.enabled = true;
            EditorUtility.SetDirty(collider);
            stats.MeshColliders++;
        }

        stats.EnabledRenderers = collisionRoot.GetComponentsInChildren<Renderer>(true).Count(x => x.enabled);
        return stats;
    }

    private static int BuildGameplayBindings(Transform gameplaySource, Transform mapRoot)
    {
        Transform old = FindDirectChild(mapRoot, BindingsRootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        GameObject bindingsObject = new GameObject(BindingsRootName);
        Undo.RegisterCreatedObjectUndo(bindingsObject, "Create Prismatic gameplay bindings");
        bindingsObject.transform.SetParent(mapRoot, false);
        ResetTransform(bindingsObject.transform);

        int created = 0;
        foreach (HelperSpec spec in HelperSpecs)
        {
            Transform source = FindChildRecursive(gameplaySource, spec.SourceName);
            if (source == null)
            {
                Debug.LogWarning("[M55.7] Gameplay helper bulunamadı: " + spec.SourceName, gameplaySource);
                continue;
            }

            GameObject anchorObject = new GameObject(spec.UnityName);
            Undo.RegisterCreatedObjectUndo(anchorObject, "Create " + spec.UnityName);
            anchorObject.transform.SetParent(bindingsObject.transform, true);
            anchorObject.transform.SetPositionAndRotation(source.position, source.rotation);
            anchorObject.transform.localScale = Vector3.one;

            PrismaticReachAnchor anchor = Undo.AddComponent<PrismaticReachAnchor>(anchorObject);
            anchor.Configure(spec.Kind, spec.SourceName, spec.Note);
            EditorUtility.SetDirty(anchor);
            created++;
        }

        return created;
    }

    private static int BuildNativeTrampolines(
        Transform mapRoot,
        Transform collisionRoot,
        int trampolineLayer)
    {
        Transform existing = FindDirectChild(mapRoot, TrampolinesRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject trampolineRoot = new GameObject(TrampolinesRootName);
        Undo.RegisterCreatedObjectUndo(trampolineRoot, "Create native Prismatic trampolines");
        trampolineRoot.transform.SetParent(mapRoot, false);
        ResetTransform(trampolineRoot.transform);

        int created = 0;
        created += CreateTrampoline(mapRoot, collisionRoot, trampolineRoot.transform, "VIOLET", trampolineLayer) ? 1 : 0;
        created += CreateTrampoline(mapRoot, collisionRoot, trampolineRoot.transform, "CERULEAN", trampolineLayer) ? 1 : 0;
        return created;
    }

    private static bool CreateTrampoline(
        Transform mapRoot,
        Transform collisionRoot,
        Transform trampolineRoot,
        string id,
        int trampolineLayer)
    {
        Transform center = FindBinding(mapRoot, "TRAMPOLINE_" + id + "_CENTER");
        Transform landing = FindBinding(mapRoot, "TRAMPOLINE_" + id + "_LANDING");
        if (center == null || landing == null)
        {
            Debug.LogWarning(
                "[M55.7.3] " + id +
                " trampoline CENTER/LANDING helper set eksik.",
                mapRoot);
            return false;
        }

        Collider[] sourceColliders = collisionRoot
            .GetComponentsInChildren<Collider>(true)
            .Where(c =>
                c != null &&
                c.enabled &&
                !c.isTrigger &&
                c.name.StartsWith(
                    "COL_TRAMPOLINE_" + id,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (sourceColliders.Length == 0)
        {
            Debug.LogError(
                "[M55.7.3] " + id +
                " için dedicated COL_TRAMPOLINE_* collider bulunamadı. " +
                "Fallback/visual collider oluşturulmadı.",
                collisionRoot);
            return false;
        }

        GameObject bindingObject =
            new GameObject("TRAMPOLINE_" + id + "_Gameplay");

        Undo.RegisterCreatedObjectUndo(
            bindingObject,
            "Create " + id + " native trampoline binding");

        bindingObject.transform.SetParent(
            trampolineRoot,
            true);
        bindingObject.transform.SetPositionAndRotation(
            center.position,
            center.rotation);
        bindingObject.transform.localScale = Vector3.one;

        if (trampolineLayer >= 0)
            bindingObject.layer = trampolineLayer;

        // These values are deliberately conservative starting values. The
        // ballistic solver targets the authored LANDING Transform exactly;
        // apex/steering can be tuned per sail later without moving helpers.
        float apexHeight = 3.0f;
        float minimumDownwardVelocity = 2.0f;
        float minimumTopSurfaceNormalY = 0.45f;
        float initialSteering = 0.18f;
        float steeringRestoreDuration = 0.65f;
        float retriggerCooldown = 0.25f;

        PrismaticReachTrampolineSurface surface =
            Undo.AddComponent<PrismaticReachTrampolineSurface>(bindingObject);

        surface.Configure(
            center,
            landing,
            sourceColliders,
            apexHeight,
            minimumDownwardVelocity,
            minimumTopSurfaceNormalY,
            initialSteering,
            steeringRestoreDuration,
            retriggerCooldown);

        foreach (Collider collider in sourceColliders)
        {
            if (trampolineLayer >= 0)
                collider.gameObject.layer = trampolineLayer;
            EditorUtility.SetDirty(collider);
        }

        EditorUtility.SetDirty(surface);
        return true;
    }

    private static PrismaticReachVisualKind ClassifyVisual(string objectName)
    {
        string n = (objectName ?? string.Empty).ToLowerInvariant();
        if (n.Contains("water") || n.Contains("foamripple") || n.Contains("spray") || n.Contains("catchwater"))
            return PrismaticReachVisualKind.Water;
        if (n.Contains("sail_") || n.Contains("trampoline"))
            return PrismaticReachVisualKind.Trampoline;
        if (n.Contains("foliage") || n.Contains("garden") || n.Contains("grass") || n.Contains("vine") || n.Contains("blossom"))
            return PrismaticReachVisualKind.Vegetation;
        if (n.Contains("decor") || n.Contains("paint") || n.Contains("wayfinding") || n.Contains("ripple"))
            return PrismaticReachVisualKind.Decor;
        return PrismaticReachVisualKind.Environment;
    }

    private static Material CreateOrUpdateUrpMaterial(Material source, bool water)
    {
        EnsureAssetFolder(MaterialFolder);
        string safeName = SanitizeFileName(source != null ? source.name : "Unnamed");
        string assetPath = MaterialFolder + "/MAT_PR_" + safeName + ".mat";
        Material target = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("URP/Lit veya Standard shader bulunamadı.");

        if (target == null)
        {
            target = new Material(shader) { name = "MAT_PR_" + safeName };
            AssetDatabase.CreateAsset(target, assetPath);
        }
        else if (target.shader != shader)
        {
            target.shader = shader;
        }

        Color baseColor = ReadColor(source, "_BaseColor", ReadColor(source, "_Color", Color.white));
        Texture baseTexture = ReadTexture(source, "_BaseMap") ?? ReadTexture(source, "_MainTex");
        Texture normalTexture = ReadTexture(source, "_BumpMap");

        if (water && baseColor.a >= 0.99f)
            baseColor.a = 0.72f;

        SetColor(target, "_BaseColor", baseColor);
        SetColor(target, "_Color", baseColor);
        SetTexture(target, "_BaseMap", baseTexture);
        SetTexture(target, "_MainTex", baseTexture);
        SetTexture(target, "_BumpMap", normalTexture);

        float metallic = ReadFloat(source, "_Metallic", 0f);
        float smoothness = ReadFloat(source, "_Smoothness", ReadFloat(source, "_Glossiness", water ? 0.55f : 0.20f));
        SetFloat(target, "_Metallic", metallic);
        SetFloat(target, "_Smoothness", smoothness);
        SetFloat(target, "_Glossiness", smoothness);

        if (normalTexture != null)
            target.EnableKeyword("_NORMALMAP");
        else
            target.DisableKeyword("_NORMALMAP");

        if (water && target.shader.name.Contains("Universal Render Pipeline"))
        {
            target.SetFloat("_Surface", 1f);
            target.SetFloat("_Blend", 0f);
            target.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            target.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            target.SetFloat("_ZWrite", 0f);
            target.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            target.renderQueue = (int)RenderQueue.Transparent;
        }

        EditorUtility.SetDirty(target);
        return target;
    }

    private static void ConfigureImporter(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
        if (importer.importCameras) { importer.importCameras = false; changed = true; }
        if (importer.importLights) { importer.importLights = false; changed = true; }
        if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            changed = true;
        }
        if (changed)
            importer.SaveAndReimport();
    }

    private static GameObject InstantiateModel(GameObject asset, Transform parent, string instanceName, Scene scene)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
        if (instance == null)
            throw new InvalidOperationException(instanceName + " instantiate edilemedi.");

        Undo.RegisterCreatedObjectUndo(instance, "Instantiate " + instanceName);
        instance.name = instanceName;
        instance.transform.SetParent(parent, false);
        ResetTransform(instance.transform);
        return instance;
    }

    private static GameObject RequireModel(string path)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            throw new InvalidOperationException("FBX import edilemedi: " + path);
        return asset;
    }

    private static bool TryMoveFigureTo(Transform anchor)
    {
        if (anchor == null)
            return false;

        Component motor = FindSceneComponentByTypeName("FigureMotor");
        if (motor == null)
            return false;

        Vector3 position = anchor.position + Vector3.up * 0.04f;
        Quaternion rotation = anchor.rotation;

        MethodInfo teleport = motor.GetType().GetMethod(
            "Teleport",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Vector3), typeof(Quaternion) },
            null);

        Undo.RecordObject(motor.transform, "Move Figure to Prismatic Reach spawn");
        if (teleport != null)
        {
            teleport.Invoke(motor, new object[] { position, rotation });
        }
        else
        {
            CharacterController controller = motor.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            motor.transform.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = wasEnabled;
        }

        EditorUtility.SetDirty(motor.transform);
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

    private static Transform FindBinding(Transform mapRoot, string sourceHelperName)
    {
        if (mapRoot == null)
            return null;
        foreach (PrismaticReachAnchor anchor in mapRoot.GetComponentsInChildren<PrismaticReachAnchor>(true))
        {
            if (anchor != null && string.Equals(anchor.SourceHelperName, sourceHelperName, StringComparison.Ordinal))
                return anchor.transform;
        }
        return null;
    }

    private static GameObject FindSceneObjectByName(string exactName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
                continue;
            if (string.Equals(go.name, exactName, StringComparison.Ordinal))
                return go;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform root, string exactName)
    {
        if (root == null)
            return null;
        if (root.name == exactName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), exactName);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Transform FindDirectChild(Transform parent, string exactName)
    {
        if (parent == null)
            return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == exactName)
                return child;
        }
        return null;
    }

    private static void ResetTransform(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private static bool IsCleanRoot(Transform t)
    {
        if (t == null)
            return false;
        return t.localPosition.sqrMagnitude < 0.000001f &&
               Quaternion.Angle(t.localRotation, Quaternion.identity) < 0.001f &&
               (t.localScale - Vector3.one).sqrMagnitude < 0.000001f;
    }

    private static void RequireEditMode()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Bu işlem Play Mode dışında çalıştırılmalıdır.");
    }

    private static Scene RequireActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Aktif ve yüklü Unity Scene bulunamadı.");
        return scene;
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

        Debug.LogWarning("[M55.7] Layer slot kalmadı: " + layerName);
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

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unnamed";
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new string(value.Select(c => invalid.Contains(c) || c == '|' ? '_' : c).ToArray());
        while (sanitized.Contains("__"))
            sanitized = sanitized.Replace("__", "_");
        return sanitized.Trim(' ', '_');
    }

    private static Color ReadColor(Material material, string property, Color fallback)
    {
        return material != null && material.HasProperty(property) ? material.GetColor(property) : fallback;
    }

    private static float ReadFloat(Material material, string property, float fallback)
    {
        return material != null && material.HasProperty(property) ? material.GetFloat(property) : fallback;
    }

    private static Texture ReadTexture(Material material, string property)
    {
        return material != null && material.HasProperty(property) ? material.GetTexture(property) : null;
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

    private sealed class VisualStats
    {
        public int Meshes;
        public int WaterMeshes;
        public int CollidersRemaining;
    }

    private sealed class CollisionStats
    {
        public int Meshes;
        public int MeshColliders;
        public int EnabledRenderers;
    }
}
#endif
