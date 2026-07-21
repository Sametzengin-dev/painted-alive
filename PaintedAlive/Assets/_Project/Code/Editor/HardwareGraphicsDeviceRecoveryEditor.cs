using System.IO;
using PaintedAlive.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    [InitializeOnLoad]
    public static class HardwareGraphicsDeviceRecoveryEditor
    {
        private const string WarningSessionKey =
            "PaintedAlive.HardwareGpuWarningShown";

        private const string RestartMenuPath =
            "Tools/Painted Alive/Performance/" +
            "Restart Editor On Hardware GPU";

        static HardwareGraphicsDeviceRecoveryEditor()
        {
            EditorApplication.delayCall += WarnIfSoftwareRenderer;
        }

        [MenuItem(RestartMenuPath)]
        private static void RestartOnHardwareGpu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Painted Alive - Hardware GPU",
                    "Exit Play Mode before restarting the Editor.",
                    "OK");
                return;
            }

            if (!EditorSceneManager
                    .SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            AssetDatabase.SaveAssets();
            string projectPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));

            EditorApplication.OpenProject(
                projectPath,
                "-force-d3d11",
                "-force-device-index",
                "1");
        }

        [MenuItem(
            "Tools/Painted Alive/Performance/" +
            "Diagnose Graphics Device")]
        private static void DiagnoseGraphicsDevice()
        {
            Debug.Log(
                "[Painted Alive Graphics] " +
                $"Device={SystemInfo.graphicsDeviceName}, " +
                $"Vendor={SystemInfo.graphicsDeviceVendor}, " +
                $"API={SystemInfo.graphicsDeviceType}, " +
                $"SoftwareRenderer=" +
                HardwareGraphicsDeviceRecovery
                    .IsSoftwareRenderer());
        }

        private static void WarnIfSoftwareRenderer()
        {
            if (!HardwareGraphicsDeviceRecovery
                    .IsSoftwareRenderer() ||
                SessionState.GetBool(WarningSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(WarningSessionKey, true);
            Debug.LogError(
                "Painted Alive Editor is using Microsoft Basic " +
                "Render Driver. This is the cause of the severe " +
                "frame-presentation stall. Use '" +
                RestartMenuPath +
                "' before profiling or entering Play Mode.");
        }
    }
}
