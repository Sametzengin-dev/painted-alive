#if UNITY_EDITOR
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class DiagnosePrototypeWholeHeadVisualPartitionHotfix
    {
        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.0.2 - Diagnose Whole Head Partition")]
        public static void Diagnose()
        {
            PrototypeMasterpieceWorldInstance[] instances =
                Object.FindObjectsByType<
                    PrototypeMasterpieceWorldInstance>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            PrototypeMasterpieceWorldInstance instance =
                instances.Length > 0
                    ? instances[0]
                    : null;

            PrototypeMasterpieceDefinitionSeveringController[] cutters =
                Object.FindObjectsByType<
                    PrototypeMasterpieceDefinitionSeveringController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            PrototypeMasterpieceDefinitionSeveringController cutter =
                cutters.Length > 0
                    ? cutters[0]
                    : null;

            int liveHeadRenderers = 0;

            if (
                instance != null &&
                instance.TryGetPartRoot(
                    PrototypeMasterpiecePartKind.Head,
                    out Transform headRoot) &&
                headRoot != null
            )
            {
                liveHeadRenderers =
                    headRoot.GetComponentsInChildren<
                        LineRenderer>(
                            true).Length;
            }

            string report =
                "[M50.0.2 Whole Head Partition Diagnose]\n" +
                $"WorldInstance={(instance != null ? "OK" : "MISSING")}\n" +
                $"BuildSucceeded={(instance != null && instance.BuildSucceeded)}\n" +
                $"SemanticHeadPartitionEnabled={(instance != null && instance.SemanticHeadPartitionEnabled)}\n" +
                $"SemanticHeadVisualSegmentCount={(instance != null ? instance.SemanticHeadVisualSegmentCount : 0)}\n" +
                $"SemanticHeadRenderedPointCount={(instance != null ? instance.SemanticHeadRenderedPointCount : 0)}\n" +
                $"HeadHierarchyLineRendererCount={liveHeadRenderers}\n" +
                $"HeadRigAnchorResolved={(cutter != null && cutter.HeadRigAnchorResolved)}\n" +
                $"HeadRendererCount={(cutter != null ? cutter.HeadRendererCount : 0)}\n" +
                $"HeadSevered={(cutter != null && cutter.EyeSevered)}\n" +
                "VisualPartitionRule=CoreToHead semantic neck half-space\n" +
                "HeadMarkerLateralPlacementIndependent=True\n" +
                "NearestMarkerHeadOwnership=False";

            Debug.Log(
                report,
                instance != null
                    ? instance
                    : cutter);

            EditorUtility.DisplayDialog(
                "M50.0.2 Diagnose",
                report,
                "Tamam");
        }
    }
}
#endif
