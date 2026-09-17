#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.Environment.Palimpsest;
using UnityEditor;
using UnityEngine;

public static class DiagnosePalimpsestMechanics_M55_9_7
{
    private const string MenuRoot =
        "Tools/Painted Alive/Milestones/";

    [MenuItem(MenuRoot + "55.9.7 - Diagnose Palimpsest Mechanic Runtime Bindings")]
    public static void Diagnose()
    {
        PalimpsestPainterWorldInteraction[] interactions =
            UnityEngine.Object.FindObjectsByType<
                PalimpsestPainterWorldInteraction>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(item =>
                item != null &&
                !EditorUtility.IsPersistent(item) &&
                item.gameObject.scene.IsValid())
            .ToArray();

        CanvasFoldSystem[] folds =
            UnityEngine.Object.FindObjectsByType<CanvasFoldSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(IsSceneObject)
            .ToArray();

        UnderpaintingRevealSystem[] underpaints =
            UnityEngine.Object.FindObjectsByType<UnderpaintingRevealSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(IsSceneObject)
            .ToArray();

        PaintSmearSurface[] smears =
            UnityEngine.Object.FindObjectsByType<PaintSmearSurface>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(IsSceneObject)
            .ToArray();

        PerspectiveFrameSystem[] perspectives =
            UnityEngine.Object.FindObjectsByType<PerspectiveFrameSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(IsSceneObject)
            .ToArray();

        int hardErrors = 0;
        int interactionTargets = 0;
        int activatableTargets = 0;
        int foldVisualTargets = 0;
        int underpaintCurrentVisualTargets = 0;
        int underpaintRevealedVisualTargets = 0;
        int smearVisualTargets = 0;
        int smearValidTravel = 0;
        int perspectiveValidDelta = 0;

        foreach (PalimpsestPainterWorldInteraction interaction in interactions)
        {
            if (interaction == null)
                continue;

            interactionTargets += interaction.ConfiguredTargetCount;

            SerializedObject serialized =
                new SerializedObject(interaction);
            SerializedProperty targets =
                serialized.FindProperty("targets");

            if (targets == null || targets.arraySize == 0)
            {
                hardErrors++;
                continue;
            }

            for (int index = 0; index < targets.arraySize; index++)
            {
                UnityEngine.Object value =
                    targets.GetArrayElementAtIndex(index)
                        .objectReferenceValue;

                if (value is IPalimpsestPainterActivatable)
                    activatableTargets++;
                else
                    hardErrors++;
            }
        }

        foreach (CanvasFoldSystem fold in folds)
        {
            SerializedObject serialized = new SerializedObject(fold);
            SerializedProperty moving =
                serialized.FindProperty("movingVisuals");
            int count = moving != null ? moving.arraySize : 0;
            foldVisualTargets += count;

            if (!fold.IsConfigured || count == 0)
                hardErrors++;
        }

        foreach (UnderpaintingRevealSystem underpaint in underpaints)
        {
            SerializedObject serialized =
                new SerializedObject(underpaint);

            SerializedProperty current =
                serialized.FindProperty("currentVisuals");
            SerializedProperty revealed =
                serialized.FindProperty("revealedVisuals");
            SerializedProperty currentHelper =
                serialized.FindProperty("currentHelper");
            SerializedProperty revealedHelper =
                serialized.FindProperty("revealedHelper");

            int currentCount =
                current != null ? current.arraySize : 0;
            int revealedCount =
                revealed != null ? revealed.arraySize : 0;

            underpaintCurrentVisualTargets += currentCount;
            underpaintRevealedVisualTargets += revealedCount;

            Transform a =
                currentHelper != null
                    ? currentHelper.objectReferenceValue as Transform
                    : null;
            Transform b =
                revealedHelper != null
                    ? revealedHelper.objectReferenceValue as Transform
                    : null;

            if (!underpaint.IsConfigured ||
                currentCount == 0 ||
                revealedCount == 0 ||
                a == null ||
                b == null ||
                Vector3.Distance(a.position, b.position) < 0.5f)
            {
                hardErrors++;
            }
        }

        foreach (PaintSmearSurface smear in smears)
        {
            SerializedObject serialized =
                new SerializedObject(smear);

            SerializedProperty moving =
                serialized.FindProperty("movingVisuals");
            SerializedProperty extended =
                serialized.FindProperty("extendedHelper");
            SerializedProperty retracted =
                serialized.FindProperty("retractedHelper");

            int count =
                moving != null ? moving.arraySize : 0;
            smearVisualTargets += count;

            Transform a =
                extended != null
                    ? extended.objectReferenceValue as Transform
                    : null;
            Transform b =
                retracted != null
                    ? retracted.objectReferenceValue as Transform
                    : null;

            float travel =
                a != null && b != null
                    ? Vector3.Distance(a.position, b.position)
                    : 0f;

            if (travel >= 6.5f && travel <= 7.5f)
                smearValidTravel++;
            else
                hardErrors++;

            if (!smear.IsConfigured || !smear.ShapeKeyResolved || count == 0)
                hardErrors++;
        }

        foreach (PerspectiveFrameSystem perspective in perspectives)
        {
            SerializedObject serialized =
                new SerializedObject(perspective);

            Transform misaligned =
                serialized.FindProperty("misalignedState")
                    ?.objectReferenceValue as Transform;
            Transform aligned =
                serialized.FindProperty("alignedState")
                    ?.objectReferenceValue as Transform;

            float angle =
                misaligned != null && aligned != null
                    ? Quaternion.Angle(
                        misaligned.rotation,
                        aligned.rotation)
                    : 0f;

            if (angle >= 8f)
                perspectiveValidDelta++;
            else
                hardErrors++;

            if (!perspective.IsConfigured)
                hardErrors++;
        }

        string result =
            hardErrors == 0
                ? "CONFIG_PASS_RUNTIME_TEST_REQUIRED"
                : "NEEDS_ATTENTION";

        string report =
            "Painted Alive M55.9.7 — Palimpsest Mechanic Runtime Bindings\n" +
            "Result=" + result + "\n\n" +
            $"PainterInteractions={interactions.Length}/8\n" +
            $"ConfiguredInteractionTargets={interactionTargets}\n" +
            $"ActivatableTargets={activatableTargets}/{interactionTargets}\n" +
            $"FoldSystems={folds.Length}/2\n" +
            $"FoldMovingVisualTargets={foldVisualTargets}\n" +
            $"UnderpaintSystems={underpaints.Length}/2\n" +
            $"UnderpaintCurrentVisualTargets={underpaintCurrentVisualTargets}\n" +
            $"UnderpaintRevealedVisualTargets={underpaintRevealedVisualTargets}\n" +
            $"SmearSystems={smears.Length}/2\n" +
            $"SmearMovingVisualTargets={smearVisualTargets}\n" +
            $"SmearAuthoredTravelValid={smearValidTravel}/2\n" +
            $"PerspectiveSystems={perspectives.Length}/1\n" +
            $"PerspectiveStateDeltaValid={perspectiveValidDelta}/1\n" +
            $"HardErrors={hardErrors}\n\n" +
            "Runtime test: Painter aim + RMB should now always show Accepted/Blocked, " +
            "draw a fallback telegraph, and log state transition. " +
            "Context menu Debug Force Activate bypasses role only; authored safety remains.";

        Debug.Log("[M55.9.7 Mechanic Diagnose]\n" + report);
        EditorUtility.DisplayDialog(
            "M55.9.7 Palimpsest Mechanic Diagnose",
            report,
            "Tamam");
    }

    private static bool IsSceneObject(Component component)
    {
        return component != null &&
            !EditorUtility.IsPersistent(component) &&
            component.gameObject.scene.IsValid();
    }
}
#endif
