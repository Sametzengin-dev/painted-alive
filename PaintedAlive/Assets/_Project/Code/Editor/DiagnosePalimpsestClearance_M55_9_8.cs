#if UNITY_EDITOR
using System.Linq;
using PaintedAlive.Environment.Palimpsest;
using UnityEditor;
using UnityEngine;

public static class DiagnosePalimpsestClearance_M55_9_8
{
    private const string MenuRoot =
        "Tools/Painted Alive/Milestones/";

    [MenuItem(MenuRoot + "55.9.8 - Diagnose Exact Palimpsest Clearance")]
    public static void Diagnose()
    {
        PalimpsestClearanceVolume[] volumes =
            UnityEngine.Object.FindObjectsByType<
                PalimpsestClearanceVolume>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(item =>
                item != null &&
                !EditorUtility.IsPersistent(item) &&
                item.gameObject.scene.IsValid())
            .ToArray();

        int configured = 0;
        int exactQuery = 0;
        int rotated = 0;
        int hardErrors = 0;

        foreach (PalimpsestClearanceVolume volume in volumes)
        {
            if (volume.TriggerVolume == null)
            {
                hardErrors++;
                continue;
            }

            configured++;

            if (volume.UsesExactOrientedBoxQuery)
                exactQuery++;
            else
                hardErrors++;

            if (Quaternion.Angle(
                    volume.TriggerVolume.transform.rotation,
                    Quaternion.identity) > 0.1f)
            {
                rotated++;
            }

            if (!volume.TriggerVolume.isTrigger)
                hardErrors++;
        }

        string result =
            hardErrors == 0 &&
            configured == 4 &&
            exactQuery == 4
                ? "CONFIG_PASS_RUNTIME_TEST_REQUIRED"
                : "NEEDS_ATTENTION";

        string report =
            "Painted Alive M55.9.8 — Exact Palimpsest Clearance\n" +
            "Result=" + result + "\n\n" +
            $"ClearanceVolumes={volumes.Length}/4\n" +
            $"ConfiguredTriggers={configured}/4\n" +
            $"ExactOrientedQueries={exactQuery}/4\n" +
            $"RotatedClearanceVolumes={rotated}\n" +
            "LegacyWorldAabbQuery=False\n" +
            $"HardErrors={hardErrors}\n\n" +
            "Important: Fold A / Great Fold authored clearances are rotated. " +
            "The old runtime used BoxCollider.bounds + identity rotation, which " +
            "queried the larger world AABB instead of the authored oriented box.\n\n" +
            "Runtime: if a Figure really occupies the authored clearance, the " +
            "activation must still be blocked and a red outline is drawn. " +
            "Move the Figure outside that red volume; safety is not bypassed.";

        Debug.Log(
            "[M55.9.8 Clearance Diagnose]\n" + report);

        EditorUtility.DisplayDialog(
            "M55.9.8 Exact Palimpsest Clearance",
            report,
            "Tamam");
    }
}
#endif
