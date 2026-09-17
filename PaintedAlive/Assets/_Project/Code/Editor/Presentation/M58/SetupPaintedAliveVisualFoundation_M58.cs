#if UNITY_EDITOR
using UnityEditor;

namespace PaintedAlive.EditorTools.Presentation
{
    public static class SetupPaintedAliveVisualFoundation_M58
    {
        [MenuItem("Tools/Painted Alive/Milestones/58.0A - Setup Visual Foundation")]
        public static void Setup()
        {
            M58VisualStackRepairCore.SetupFoundation();
        }

        [MenuItem("Tools/Painted Alive/Milestones/58.0A - Diagnose Visual Foundation")]
        public static void Diagnose()
        {
            M58VisualStackRepairCore.DiagnoseAll();
        }
    }
}
#endif
