#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.Environment.Palimpsest;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Painters;
using PaintedAlive.Painters.Ink;
using PaintedAlive.Painters.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupPainterContinuity_M55_9_4
{
    private const string MenuRoot =
        "Tools/Painted Alive/Milestones/";

    [MenuItem(MenuRoot + "55.9.4 - Setup Painter Continuity + Local Recovery")]
    public static void Setup()
    {
        RequireEditMode();

        FigureMotor figure = FindExactlyOne<FigureMotor>("FigureMotor");
        InkPainterRoleAuthority authority =
            FindExactlyOne<InkPainterRoleAuthority>("InkPainterRoleAuthority");
        InkPainterIndependentCamera cameraController =
            FindExactlyOne<InkPainterIndependentCamera>(
                "InkPainterIndependentCamera");
        PainterBrushController brush =
            FindExactlyOne<PainterBrushController>("PainterBrushController");
        InkPainterNestController nest =
            FindExactlyOne<InkPainterNestController>(
                "InkPainterNestController");
        InkSystemManager manager =
            FindExactlyOne<InkSystemManager>("InkSystemManager");

        Camera painterCamera =
            cameraController.ControlledCamera != null
                ? cameraController.ControlledCamera
                : cameraController.GetComponent<Camera>();

        if (painterCamera == null)
            throw new InvalidOperationException(
                "Painter camera bulunamadı.");

        int environmentLayer =
            LayerMask.NameToLayer("PrismaticCollision");

        if (environmentLayer < 0)
            throw new InvalidOperationException(
                "PrismaticCollision layer bulunamadı. " +
                "Continuation map setup'ını önce doğrulayın.");

        // Keep all existing mask bits and add the continuation-map collision layer.
        SetLayerMaskBit(
            new SerializedObject(brush),
            "paintSurfaceMask",
            environmentLayer);
        SetLayerMaskBit(
            new SerializedObject(nest),
            "surfaceMask",
            environmentLayer);
        SetLayerMaskBit(
            new SerializedObject(manager),
            "navigationMask",
            environmentLayer);
        SetLayerMaskBit(
            new SerializedObject(manager),
            "visibilityMask",
            environmentLayer);

        SetObjectReference(
            new SerializedObject(brush),
            "outputCamera",
            painterCamera);
        SetObjectReference(
            new SerializedObject(nest),
            "painterCamera",
            painterCamera);

        brush.IncludeEnvironmentSurfaceLayers();
        nest.IncludeEnvironmentSurfaceLayers();
        manager.IncludeEnvironmentSurfaceLayers();

        authority.ConfigureWorldBrush(brush);
        EditorUtility.SetDirty(authority);

        PainterWorldActionController worldActions =
            painterCamera.GetComponent<PainterWorldActionController>();

        if (worldActions == null)
        {
            worldActions =
                Undo.AddComponent<PainterWorldActionController>(
                    painterCamera.gameObject);
        }

        worldActions.Configure(
            authority,
            painterCamera,
            brush);
        EditorUtility.SetDirty(worldActions);

        FigureVoidRecoveryController recovery =
            figure.GetComponent<FigureVoidRecoveryController>();

        if (recovery == null)
        {
            recovery =
                Undo.AddComponent<FigureVoidRecoveryController>(
                    figure.gameObject);
        }

        recovery.Configure(figure);
        EditorUtility.SetDirty(recovery);

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        AssetDatabase.SaveAssets();

        DiagnoseInternal(true);
    }

    [MenuItem(MenuRoot + "55.9.4 - Diagnose Painter Continuity + Local Recovery")]
    public static void Diagnose()
    {
        DiagnoseInternal(true);
    }

    private static void DiagnoseInternal(bool showDialog)
    {
        int hardErrors = 0;

        FigureMotor figure = FindOptional<FigureMotor>();
        InkPainterRoleAuthority authority =
            FindOptional<InkPainterRoleAuthority>();
        InkPainterIndependentCamera cameraController =
            FindOptional<InkPainterIndependentCamera>();
        PainterBrushController brush =
            FindOptional<PainterBrushController>();
        InkPainterNestController nest =
            FindOptional<InkPainterNestController>();
        InkSystemManager manager =
            FindOptional<InkSystemManager>();
        PainterWorldActionController worldActions =
            FindOptional<PainterWorldActionController>();
        FigureVoidRecoveryController recovery =
            figure != null
                ? figure.GetComponent<FigureVoidRecoveryController>()
                : null;

        Camera painterCamera =
            cameraController != null
                ? cameraController.ControlledCamera
                : null;

        int environmentLayer =
            LayerMask.NameToLayer("PrismaticCollision");

        bool brushCameraOk =
            brush != null &&
            painterCamera != null &&
            brush.OutputCamera == painterCamera;

        bool nestCameraOk =
            nest != null &&
            painterCamera != null &&
            nest.PainterCamera == painterCamera;

        bool brushMaskOk =
            brush != null &&
            PainterEnvironmentLayerUtility.ContainsLayer(
                brush.PaintSurfaceMask,
                environmentLayer);

        bool nestMaskOk =
            nest != null &&
            PainterEnvironmentLayerUtility.ContainsLayer(
                nest.SurfaceMask,
                environmentLayer);

        bool navigationMaskOk =
            manager != null &&
            PainterEnvironmentLayerUtility.ContainsLayer(
                manager.NavigationMask,
                environmentLayer);

        bool visibilityMaskOk =
            manager != null &&
            PainterEnvironmentLayerUtility.ContainsLayer(
                manager.VisibilityMask,
                environmentLayer);

        bool authorityBrushOk =
            authority != null &&
            authority.PainterBrushController == brush;

        int palimpsestInteractionCount =
            UnityEngine.Object.FindObjectsByType<
                PalimpsestPainterWorldInteraction>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Count(x => x != null);

        bool interactionCountOk =
            palimpsestInteractionCount == 8;

        bool worldActionsOk =
            worldActions != null &&
            painterCamera != null &&
            worldActions.gameObject == painterCamera.gameObject;

        bool recoveryOk =
            recovery != null;

        bool cameraTuningOk =
            cameraController != null &&
            cameraController.Config != null;

        bool[] checks =
        {
            figure != null,
            authority != null,
            painterCamera != null,
            brush != null,
            nest != null,
            manager != null,
            brushCameraOk,
            nestCameraOk,
            brushMaskOk,
            nestMaskOk,
            navigationMaskOk,
            visibilityMaskOk,
            authorityBrushOk,
            interactionCountOk,
            worldActionsOk,
            recoveryOk,
            cameraTuningOk
        };

        for (int index = 0; index < checks.Length; index++)
        {
            if (!checks[index])
                hardErrors++;
        }

        string result =
            hardErrors == 0
                ? "CONFIG_PASS_RUNTIME_NOT_VALIDATED"
                : "NEEDS_ATTENTION";

        string report =
            "Painted Alive M55.9.4 — Painter Continuity + Local Recovery\n" +
            "Result=" + result + "\n\n" +
            $"Figure={(figure != null)}\n" +
            $"PainterAuthority={(authority != null)}\n" +
            $"PainterCamera={(painterCamera != null)}\n" +
            $"PainterBrush={(brush != null)}\n" +
            $"InkNestController={(nest != null)}\n" +
            $"InkSystemManager={(manager != null)}\n" +
            $"BrushUsesPainterCamera={brushCameraOk}\n" +
            $"NestUsesPainterCamera={nestCameraOk}\n" +
            $"BrushPrismaticCollisionMask={brushMaskOk}\n" +
            $"NestPrismaticCollisionMask={nestMaskOk}\n" +
            $"CreatureNavigationPrismaticCollisionMask={navigationMaskOk}\n" +
            $"CreatureVisibilityPrismaticCollisionMask={visibilityMaskOk}\n" +
            $"AuthorityWorldBrushBound={authorityBrushOk}\n" +
            $"PalimpsestWorldInteractions={palimpsestInteractionCount}/8\n" +
            $"PainterWorldActionController={worldActionsOk}\n" +
            $"FigureVoidRecovery={recoveryOk}\n" +
            $"PainterCameraRuntimeTuning={cameraTuningOk}\n" +
            $"HardErrors={hardErrors}\n\n" +
            "Painter runtime controls:\n" +
            "LMB paint • F7 creature • RMB aimed Palimpsest world action\n" +
            "WASD camera • Q/E free height • R reframe\n" +
            "PageUp/PageDown precise remembered height • [/] FOV • Home reset\n\n" +
            "Figure local recovery:\n" +
            "Recent grounded poses are cached. A deep fall with no valid landing " +
            "below returns the Figure to the newest still-safe nearby pose. " +
            "Authored map FallZone/Respawn remains authoritative when it triggers first.\n\n" +
            "Runtime acceptance is NOT claimed by this diagnostic.";

        Debug.Log(
            "[M55.9.4 Painter Continuity]\n" + report,
            worldActions != null
                ? worldActions
                : authority);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "M55.9.4 Painter Continuity",
                report,
                "Tamam");
        }
    }

    private static void SetLayerMaskBit(
        SerializedObject serialized,
        string propertyName,
        int layer)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property == null)
            throw new InvalidOperationException(
                "Serialized LayerMask bulunamadı: " +
                propertyName);

        property.intValue |= 1 << layer;
        serialized.ApplyModifiedProperties();
    }

    private static void SetObjectReference(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property == null)
            throw new InvalidOperationException(
                "Serialized reference bulunamadı: " +
                propertyName);

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static T FindExactlyOne<T>(string label)
        where T : Component
    {
        T[] objects =
            UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(
                item =>
                    item != null &&
                    !EditorUtility.IsPersistent(item) &&
                    item.gameObject.scene.IsValid())
            .ToArray();

        if (objects.Length != 1)
        {
            throw new InvalidOperationException(
                label + " expected exactly 1, found " +
                objects.Length + ".");
        }

        return objects[0];
    }

    private static T FindOptional<T>()
        where T : Component
    {
        T[] objects =
            UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int index = 0; index < objects.Length; index++)
        {
            T candidate = objects[index];

            if (candidate != null &&
                !EditorUtility.IsPersistent(candidate) &&
                candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "M55.9.4 Setup Play Mode kapalıyken çalıştırılmalıdır.");
        }
    }
}
#endif
