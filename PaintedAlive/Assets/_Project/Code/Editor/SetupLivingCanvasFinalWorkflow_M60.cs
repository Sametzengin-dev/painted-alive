#if UNITY_EDITOR
using PaintedAlive.Painters.SideCanvas;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupLivingCanvasFinalWorkflow_M60
    {
        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "60 - Apply Final Living Canvas Workflow")]
        public static void Apply()
        {
            SetupLivingCanvasRealtimePolish_M59.Apply();

            Debug.Log(
                "[M60 Setup] Final Living Canvas workflow ready.\n" +
                "ColorPalette=6 persistent stroke colors\n" +
                "RigSnap=Closest visible line segment\n" +
                "RigEditing=Click and drag existing marker\n" +
                "ScalePreview=Live XYZ with held-key repeat\n" +
                "WorldPlacement=Near Figure with safe candidates\n" +
                "DeployVisibility=Material contract validated",
                Object.FindFirstObjectByType<
                    PrototypeLivingSideCanvasController>(
                        FindObjectsInactive.Include));
        }
    }
}
#endif
