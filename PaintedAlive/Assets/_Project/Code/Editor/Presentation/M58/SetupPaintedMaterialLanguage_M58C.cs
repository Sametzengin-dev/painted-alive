#if UNITY_EDITOR
using UnityEditor;

namespace PaintedAlive.EditorTools.Presentation
{
    public static class SetupPaintedMaterialLanguage_M58C
    {
        [MenuItem("Tools/Painted Alive/Milestones/58.0C - Setup Painted Material Language")]
        public static void Setup()
        {
            M58VisualStackRepairCore.SetupLivingGalleryMaterials();
        }

        [MenuItem("Tools/Painted Alive/Milestones/58.0C - Diagnose Painted Material Language")]
        public static void Diagnose()
        {
            M58VisualStackRepairCore.DiagnoseAll();
        }
    }
}
#endif
