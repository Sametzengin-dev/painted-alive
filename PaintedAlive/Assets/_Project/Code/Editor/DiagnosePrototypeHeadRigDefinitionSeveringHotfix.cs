#if UNITY_EDITOR
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class DiagnosePrototypeHeadRigDefinitionSeveringHotfix
    {
        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.0.1 - Diagnose Head Rig Severing")]
        public static void Diagnose()
        {
            PrototypeMasterpieceDefinitionSeveringController[] controllers =
                Object.FindObjectsByType<
                    PrototypeMasterpieceDefinitionSeveringController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            PrototypeMasterpieceDefinitionSeveringController controller =
                controllers.Length > 0
                    ? controllers[0]
                    : null;

            string report =
                "[M50.0.1 Head Rig Severing Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ResolvedRole={(controller != null ? controller.ResolvedRole : "N/A")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"PaletteKnifeActive={(controller != null && controller.PaletteKnifeActive)}\n" +
                $"PaletteKnifeContract={(controller != null ? controller.PaletteKnifeActivityContract : "N/A")}\n" +
                $"PrimaryToolAllowed={(controller != null && controller.PrimaryToolAllowed)}\n" +
                $"ActiveFigureCameraResolved={(controller != null && controller.ActiveFigureCameraResolved)}\n" +
                $"HeadRigAnchorResolved={(controller != null && controller.HeadRigAnchorResolved)}\n" +
                $"HeadRigCutAnchor={(controller != null ? controller.HeadRigCutAnchor.ToString("F3") : "N/A")}\n" +
                $"HeadRendererCount={(controller != null ? controller.HeadRendererCount : 0)}\n" +
                $"InputActionActive={(controller != null && controller.InputActionActive)}\n" +
                $"CutInputPressed={(controller != null && controller.CutInputPressed)}\n" +
                $"State={(controller != null ? controller.State.ToString() : "N/A")}\n" +
                $"EyeAimed={(controller != null && controller.EyeAimed)}\n" +
                $"LineOfSightClear={(controller != null && controller.LineOfSightClear)}\n" +
                $"Distance={(controller != null ? controller.EyeDistance : -1f):F2}\n" +
                $"ViewportAimOffset={(controller != null ? controller.ViewportAimOffset : -1f):F3}\n" +
                $"CutProgress={(controller != null ? controller.CutProgress : 0f):F3}\n" +
                $"Severed={(controller != null && controller.EyeSevered)}\n" +
                $"PerceptionCapabilityActive={(controller != null && controller.EyeCapabilityActive)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "SemanticTarget=HeadRigConnection\n" +
                "CapabilityTarget=Perception\n" +
                "VisualEyeRequired=False";

            Debug.Log(
                report,
                controller);

            EditorUtility.DisplayDialog(
                "M50.0.1 Diagnose",
                report,
                "Tamam");
        }
    }
}
#endif
