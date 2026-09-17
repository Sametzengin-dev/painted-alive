#if UNITY_EDITOR
using UnityEditor;

namespace PaintedAlive.EditorTools.Presentation
{
    public static class SetupLivingGalleryLighting_M58B
    {
        [MenuItem("Tools/Painted Alive/Milestones/58.0B - Setup Living Gallery Lighting")]
        public static void Setup()
        {
            M58VisualStackRepairCore.SetupLivingGalleryLighting();
        }

        [MenuItem("Tools/Painted Alive/Milestones/58.0B - Diagnose Living Gallery Lighting")]
        public static void Diagnose()
        {
            M58VisualStackRepairCore.DiagnoseAll();
        }
    }
}
#endif
