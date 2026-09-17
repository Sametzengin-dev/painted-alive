#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Painters.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupLivingGalleryPainterMechanics_M55_9_9
{
    private const string MenuRoot =
        "Tools/Painted Alive/Milestones/";

    private const string InteractionRootName =
        "M55_9_9_LivingGalleryPainterInteractions";

    [MenuItem(MenuRoot + "55.9.9 - Setup Living Gallery Painter Mechanics")]
    public static void Setup()
    {
        RequireEditMode();

        LivingGalleryGameplayBinder binder =
            FindExactlyOne<LivingGalleryGameplayBinder>(
                "LivingGalleryGameplayBinder");

        if (binder.MapRoot == null ||
            binder.VisualRoot == null ||
            binder.CollisionRoot == null ||
            binder.GameplayRoot == null)
        {
            throw new InvalidOperationException(
                "Living Gallery binder map/visual/collision/gameplay root eksik.");
        }

        Transform existing = FindDirectChild(
            binder.MapRoot,
            InteractionRootName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject interactionRoot =
            new GameObject(InteractionRootName);
        Undo.RegisterCreatedObjectUndo(
            interactionRoot,
            "Create Living Gallery Painter interactions");
        interactionRoot.transform.SetParent(
            binder.MapRoot,
            false);

        BuildFrameGateInteractions(
            binder.MapRoot,
            binder.VisualRoot,
            binder.CollisionRoot,
            binder.GameplayRoot,
            interactionRoot.transform);

        BuildPaperInteractions(
            binder.MapRoot,
            binder.GameplayRoot,
            interactionRoot.transform);

        BuildMovingCanvasInteractions(
            binder.MapRoot,
            binder.GameplayRoot,
            interactionRoot.transform);

        EditorUtility.SetDirty(binder.MapRoot);
        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        DiagnoseInternal(showDialog: true);
    }

    [MenuItem(MenuRoot + "55.9.9 - Diagnose Living Gallery Painter Mechanics")]
    public static void Diagnose()
    {
        DiagnoseInternal(showDialog: true);
    }

    private static void BuildFrameGateInteractions(
        Transform mapRoot,
        Transform visualRoot,
        Transform collisionRoot,
        Transform gameplayRoot,
        Transform parent)
    {
        for (int index = 1; index <= 3; index++)
        {
            string suffix = "0" + index;
            string id = "FRAME_" + suffix;

            RotatingFrameGate gate =
                mapRoot.GetComponentsInChildren<RotatingFrameGate>(true)
                    .FirstOrDefault(candidate =>
                        candidate != null &&
                        string.Equals(
                            candidate.BindingId,
                            id,
                            StringComparison.Ordinal));

            Transform visualGateRoot = FindExact(
                visualRoot,
                "INT_FrameGate_" + suffix);

            Transform collisionGateRoot = FindExact(
                collisionRoot,
                "INT_FrameGate_" + suffix);

            Transform pivot = FindExact(
                gameplayRoot,
                "GP_FrameGate_" + suffix + "_Pivot");

            Transform clearance = FindExact(
                gameplayRoot,
                "GP_FrameGate_" + suffix + "_Clearance");

            Transform movingCollision = FindExact(
                collisionRoot,
                "COLLISION_INT_FrameGate_" + suffix + "_OpaqueArtwork");

            Collider movingCollider =
                movingCollision != null
                    ? movingCollision.GetComponent<Collider>()
                    : null;

            if (gate == null ||
                visualGateRoot == null ||
                collisionGateRoot == null ||
                pivot == null ||
                clearance == null ||
                movingCollider == null)
            {
                throw new InvalidOperationException(
                    id +
                    " exact runtime binding eksik. " +
                    $"Gate={(gate != null)}, " +
                    $"Visual={(visualGateRoot != null)}, " +
                    $"Collision={(collisionGateRoot != null)}, " +
                    $"Pivot={(pivot != null)}, " +
                    $"Clearance={(clearance != null)}, " +
                    $"MovingCollider={(movingCollider != null)}");
            }

            float clearanceHeight =
                index == 1 ? 6.0f :
                index == 2 ? 6.1f :
                5.7f;

            // M55.9.9 originally only created Painter interactions and assumed
            // the M55.8 gate's private scene references had survived every map
            // rebuild. In the current scene the three components exist, but
            // IsConfigured=False. Rebind the authoritative exact FBX/helper
            // references before exposing the gate to Painter input.
            gate.Configure(
                id,
                visualGateRoot,
                collisionGateRoot,
                pivot,
                clearance,
                movingCollider,
                0f,
                90f,
                1.5f,
                1.25f,
                2.54f,
                clearanceHeight);

            // Conservative initial policy: all route gates begin OPEN.
            gate.ConfigurePainterInitialState(
                RotatingFrameGateState.Open);

            EditorUtility.SetDirty(gate);

            GameObject anchor = NewInteractionAnchor(
                parent,
                "PAINTER_LG_" + id,
                pivot);

            LivingGalleryPainterWorldInteraction interaction =
                Undo.AddComponent<LivingGalleryPainterWorldInteraction>(
                    anchor);

            interaction.ConfigureFrameGate(
                id,
                "Frame Gate " + suffix,
                gate,
                clearance,
                1.5f);

            EditorUtility.SetDirty(interaction);
        }
    }

    private static void BuildPaperInteractions(
        Transform mapRoot,
        Transform gameplayRoot,
        Transform parent)
    {
        for (int index = 1; index <= 2; index++)
        {
            string suffix = "0" + index;
            string id = "PAPER_" + suffix;

            DryingPaperSurface paper =
                mapRoot.GetComponentsInChildren<DryingPaperSurface>(true)
                    .FirstOrDefault(candidate =>
                        candidate != null &&
                        string.Equals(
                            candidate.BindingId,
                            id,
                            StringComparison.Ordinal));

            Transform stateHelper = FindExact(
                gameplayRoot,
                "GP_DryingPaper_" + suffix + "_State");

            if (paper == null || stateHelper == null)
            {
                throw new InvalidOperationException(
                    id + " paper/state-helper binding eksik.");
            }

            GameObject anchor = NewInteractionAnchor(
                parent,
                "PAINTER_LG_" + id,
                stateHelper);

            LivingGalleryPainterWorldInteraction interaction =
                Undo.AddComponent<LivingGalleryPainterWorldInteraction>(
                    anchor);

            interaction.ConfigureDryingPaper(
                id,
                "Drying Paper " + suffix,
                paper,
                stateHelper,
                1.5f);

            EditorUtility.SetDirty(interaction);
        }
    }

    private static void BuildMovingCanvasInteractions(
        Transform mapRoot,
        Transform gameplayRoot,
        Transform parent)
    {
        string[] suffixes = { "02", "04" };

        foreach (string suffix in suffixes)
        {
            string id = "MOVING_CANVAS_" + suffix;

            MovingCanvasObstacle canvas =
                mapRoot.GetComponentsInChildren<MovingCanvasObstacle>(true)
                    .FirstOrDefault(candidate =>
                        candidate != null &&
                        string.Equals(
                            candidate.BindingId,
                            id,
                            StringComparison.Ordinal));

            Transform pivot = FindExact(
                gameplayRoot,
                "GP_CanvasPivot_" + suffix);

            if (canvas == null || pivot == null)
            {
                throw new InvalidOperationException(
                    id + " canvas/pivot binding eksik.");
            }

            GameObject anchor = NewInteractionAnchor(
                parent,
                "PAINTER_LG_" + id,
                pivot);

            LivingGalleryPainterWorldInteraction interaction =
                Undo.AddComponent<LivingGalleryPainterWorldInteraction>(
                    anchor);

            // Source authors the 10° limit, but no separate warning duration.
            // 1.0 s is an explicit Unity readability policy, not source metadata.
            interaction.ConfigureMovingCanvas(
                id,
                "Hanging Canvas " + suffix,
                canvas,
                pivot,
                1.0f);

            EditorUtility.SetDirty(interaction);
        }
    }

    private static GameObject NewInteractionAnchor(
        Transform parent,
        string name,
        Transform source)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(
            go,
            "Create Living Gallery Painter action");
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(
            source.position,
            source.rotation);
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static void DiagnoseInternal(bool showDialog)
    {
        LivingGalleryGameplayBinder binder =
            FindOptional<LivingGalleryGameplayBinder>();

        Transform mapRoot = binder != null
            ? binder.MapRoot
            : null;

        int hardErrors = 0;

        LivingGalleryPainterWorldInteraction[] interactions =
            mapRoot != null
                ? mapRoot.GetComponentsInChildren<
                    LivingGalleryPainterWorldInteraction>(true)
                : Array.Empty<LivingGalleryPainterWorldInteraction>();

        RotatingFrameGate[] gates =
            mapRoot != null
                ? mapRoot.GetComponentsInChildren<RotatingFrameGate>(true)
                : Array.Empty<RotatingFrameGate>();

        DryingPaperSurface[] papers =
            mapRoot != null
                ? mapRoot.GetComponentsInChildren<DryingPaperSurface>(true)
                : Array.Empty<DryingPaperSurface>();

        MovingCanvasObstacle[] canvases =
            mapRoot != null
                ? mapRoot.GetComponentsInChildren<MovingCanvasObstacle>(true)
                : Array.Empty<MovingCanvasObstacle>();

        MovingCanvasCompanionFollower[] followers =
            mapRoot != null
                ? mapRoot.GetComponentsInChildren<MovingCanvasCompanionFollower>(true)
                : Array.Empty<MovingCanvasCompanionFollower>();

        PigmentRouteModifier[] pigmentPolicies =
            mapRoot != null
                ? mapRoot.GetComponentsInChildren<PigmentRouteModifier>(true)
                : Array.Empty<PigmentRouteModifier>();

        PainterWorldActionController worldController =
            FindOptional<PainterWorldActionController>();

        int gateInteractions = interactions.Count(item =>
            item != null &&
            item.ActionKind == LivingGalleryPainterActionKind.FrameGate);
        int paperInteractions = interactions.Count(item =>
            item != null &&
            item.ActionKind == LivingGalleryPainterActionKind.DryingPaper);
        int canvasInteractions = interactions.Count(item =>
            item != null &&
            item.ActionKind == LivingGalleryPainterActionKind.MovingCanvas);

        int gateConfigured = gates.Count(gate =>
            gate != null &&
            gate.IsConfigured &&
            gate.UsesPivotPreservingRotation);

        string gateBindingDetail = string.Join(
            "\n",
            gates
                .Where(gate => gate != null)
                .OrderBy(gate => gate.BindingId)
                .Select(gate =>
                {
                    SerializedObject serialized =
                        new SerializedObject(gate);

                    bool visual =
                        serialized.FindProperty("visualGateRoot")
                            ?.objectReferenceValue != null;
                    bool collision =
                        serialized.FindProperty("collisionGateRoot")
                            ?.objectReferenceValue != null;
                    bool pivot =
                        serialized.FindProperty("pivot")
                            ?.objectReferenceValue != null;
                    bool clearance =
                        serialized.FindProperty("clearanceReference")
                            ?.objectReferenceValue != null;
                    bool moving =
                        serialized.FindProperty("movingCollider")
                            ?.objectReferenceValue != null;

                    return
                        $"{gate.BindingId}: " +
                        $"Configured={gate.IsConfigured}, " +
                        $"PivotPreserving={gate.UsesPivotPreservingRotation}, " +
                        $"Visual={visual}, Collision={collision}, " +
                        $"Pivot={pivot}, Clearance={clearance}, " +
                        $"MovingCollider={moving}";
                }));

        int paperConfigured = papers.Count(paper =>
            paper != null &&
            paper.CoarseCollisionStatesConfigured);

        int canvasConfigured = canvases.Count(canvas =>
            canvas != null &&
            canvas.IsConfigured);

        int exposedPigmentInteractions = interactions.Count(item =>
            item != null &&
            item.SystemReference.StartsWith(
                "PIGMENT_",
                StringComparison.Ordinal));

        int trampolineCount = 0;
        if (mapRoot != null)
        {
            trampolineCount = mapRoot
                .GetComponentsInChildren<MonoBehaviour>(true)
                .Count(component =>
                    component != null &&
                    string.Equals(
                        component.GetType().Name,
                        "PrismaticReachTrampolineSurface",
                        StringComparison.Ordinal));
        }

        if (binder == null) hardErrors++;
        if (interactions.Length != 7) hardErrors++;
        if (gateInteractions != 3) hardErrors++;
        if (paperInteractions != 2) hardErrors++;
        if (canvasInteractions != 2) hardErrors++;
        if (gates.Length != 3 || gateConfigured != 3) hardErrors++;
        if (papers.Length != 2 || paperConfigured != 2) hardErrors++;
        if (canvases.Length != 2 || canvasConfigured != 2) hardErrors++;
        if (followers.Length != 2) hardErrors++;
        if (pigmentPolicies.Length != 3) hardErrors++;
        if (exposedPigmentInteractions != 0) hardErrors++;
        if (trampolineCount != 2) hardErrors++;
        if (worldController == null) hardErrors++;

        string result = hardErrors == 0
            ? "CONFIG_PASS_RUNTIME_TEST_REQUIRED"
            : "NEEDS_ATTENTION";

        string report =
            "Painted Alive M55.9.9 — Living Gallery Painter Mechanics\n" +
            "Result=" + result + "\n\n" +
            $"PainterWorldController={(worldController != null)}\n" +
            $"LivingGalleryPainterInteractions={interactions.Length}/7\n" +
            $"GateInteractions={gateInteractions}/3\n" +
            $"PaperInteractions={paperInteractions}/2\n" +
            $"CanvasInteractions={canvasInteractions}/2\n" +
            $"FrameGates={gates.Length}/3\n" +
            $"GatePivotPreservingRuntime={gateConfigured}/3\n" +
            (gateConfigured == 3
                ? string.Empty
                : "GateBindingDetail:\n" + gateBindingDetail + "\n") +
            $"DryingPapers={papers.Length}/2\n" +
            $"PaperCoarseCollisionPolicies={paperConfigured}/2\n" +
            $"MovingCanvases={canvases.Length}/2\n" +
            $"MovingCanvasFollowers={followers.Length}/2\n" +
            $"FigureTrampolinesPassive={trampolineCount}/2\n" +
            $"PigmentExplicitNoEffectPolicies={pigmentPolicies.Length}/3\n" +
            $"PigmentActionsExposed={exposedPigmentInteractions} expected=0\n" +
            $"HardErrors={hardErrors}\n\n" +
            "Painter RMB controls: 3 Frame Gates + 2 Drying Papers + 2 Hanging Canvases.\n" +
            "Not exposed as Painter commands: trampolines (Figure contact), pigment routes " +
            "(no authored route effect), paint curtains/visibility/ink deception (passive information systems).\n" +
            "Runtime acceptance still required.";

        Debug.Log(
            "[M55.9.9 Living Gallery Painter Mechanics]\n" + report,
            binder);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "M55.9.9 Living Gallery Painter Mechanics",
                report,
                "Tamam");
        }
    }

    private static Transform FindExact(
        Transform root,
        string exactName)
    {
        if (root == null)
            return null;

        Transform[] matches = root
            .GetComponentsInChildren<Transform>(true)
            .Where(item =>
                item != null &&
                string.Equals(
                    item.name,
                    exactName,
                    StringComparison.Ordinal))
            .ToArray();

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                exactName + " expected exactly 1, found " +
                matches.Length + ".");
        }

        return matches[0];
    }

    private static Transform FindDirectChild(
        Transform parent,
        string exactName)
    {
        if (parent == null)
            return null;

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child != null &&
                string.Equals(
                    child.name,
                    exactName,
                    StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static T FindExactlyOne<T>(string label)
        where T : Component
    {
        T[] values = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(item => IsSceneComponent(item))
            .ToArray();

        if (values.Length != 1)
        {
            throw new InvalidOperationException(
                label + " expected exactly 1, found " +
                values.Length + ".");
        }

        return values[0];
    }

    private static T FindOptional<T>()
        where T : Component
    {
        return UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => IsSceneComponent(item));
    }

    private static bool IsSceneComponent<T>(T component)
        where T : Component
    {
        return component != null &&
            !EditorUtility.IsPersistent(component) &&
            component.gameObject.scene.IsValid();
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "M55.9.9 Setup Play Mode kapalıyken çalıştırılmalıdır.");
        }
    }
}
#endif
