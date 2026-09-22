#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using PaintedAlive.Painters.SideCanvas;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupLivingCanvasRealtimePolish_M59
    {
        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "59 - Apply Living Canvas + Realtime Multiplayer Polish")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M59 kurulumu durduruldu",
                    "Kurulumu Play Mode dışında çalıştır.",
                    "Tamam");
                return;
            }

            try
            {
                if (UnityEngine.Object.FindFirstObjectByType<
                        PrototypeLivingSideCanvasController>(
                            FindObjectsInactive.Include) == null)
                {
                    SetupPrototypeLivingSideCanvasMilestone.Apply();
                }

                if (UnityEngine.Object.FindFirstObjectByType<
                        PrototypeMasterpieceAssemblyController>(
                            FindObjectsInactive.Include) == null)
                {
                    SetupPrototypeMasterpieceAssemblyMilestone.Apply();
                }

                // Re-run M47 even when it already exists so the runtime
                // placement reference and guaranteed-visible materials are
                // serialized into older scenes as well.
                SetupPrototypeMasterpieceWorldDeploymentMilestone.Apply();

                // Run M45 last so its new deploy button can bind to M47.
                SetupPrototypeLivingSideCanvasMilestone.Apply();

                Debug.Log(
                    "[M59 Setup] Living Canvas + Realtime Multiplayer Polish ready.\n" +
                    "RigThreshold=BrushRelative\n" +
                    "WorldDeployToolbar=True\n" +
                    "DeploymentScaleXYZ=True\n" +
                    "OilPreviewRate=15HzUnreliable\n" +
                    "CommittedOil=Reliable\n" +
                    "PaletteKnifeCuts=Reliable\n" +
                    "PainterPresenceRate=12HzUnreliable\n" +
                    "FigurePainterIndicator=DirectionalApproximate",
                    UnityEngine.Object.FindFirstObjectByType<
                        PrototypeLivingSideCanvasController>(
                            FindObjectsInactive.Include));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M59 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }
    }
}
#endif
