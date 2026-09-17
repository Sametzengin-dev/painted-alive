#if UNITY_EDITOR
using UnityEditor;

namespace PaintedAlive.EditorTools.Presentation
{
    public static class M58ABCVisualStackRepairMenu
    {
        [MenuItem("Tools/Painted Alive/Milestones/58.0ABC - REPAIR Visual Stack (Recommended)")]
        public static void Repair()
        {
            M58VisualStackRepairCore.RepairAll();
        }

        [MenuItem("Tools/Painted Alive/Milestones/58.0ABC - Diagnose Visual Stack")]
        public static void Diagnose()
        {
            M58VisualStackRepairCore.DiagnoseAll();
        }
    }
}
#endif
