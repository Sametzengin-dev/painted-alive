#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools.Presentation
{
    /// <summary>
    /// Repairs/saves M58.0A around Play Mode transitions.
    ///
    /// Why this exists:
    /// runtime role cameras can be enabled after the original editor setup ran.
    /// Also, Play Mode restores serialized scene state when it exits. We make the
    /// saved M58 foundation authoritative both before entering Play and immediately
    /// after returning to Edit Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class M58VisualFoundationPlayModeKeeper
    {
        private const string InstalledKey =
            "PaintedAlive.M58.VisualFoundation.PersistenceInstalled";

        static M58VisualFoundationPlayModeKeeper()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;

            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0ABC.1 - Install PlayMode Persistence Hotfix")]
        public static void Install()
        {
            EditorPrefs.SetBool(
                InstalledKey,
                true);

            RepairFoundationAndSave(
                "manual install");

            Debug.Log(
                "[M58.0ABC.1] PlayMode persistence hotfix INSTALLED. " +
                "The Visual Foundation will now be repaired/saved before " +
                "Play Mode and again after returning to Edit Mode.");
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0ABC.1 - Diagnose PlayMode Persistence")]
        public static void Diagnose()
        {
            bool installed =
                EditorPrefs.GetBool(
                    InstalledKey,
                    false);

            GameObject root =
                FindSceneObjectIncludingInactive(
                    "M58_VisualFoundation");

            GameObject volume =
                FindSceneObjectIncludingInactive(
                    "M58_GlobalVolume_PaintedAlive");

            int hdrCameras = 0;
            int totalCameras = 0;

            foreach (Camera camera
                     in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (camera == null ||
                    camera.gameObject == null ||
                    !camera.gameObject.scene.IsValid())
                {
                    continue;
                }

                totalCameras++;

                if (camera.allowHDR)
                    hdrCameras++;
            }

            int hardErrors = 0;

            if (!installed)
                hardErrors++;

            if (root == null ||
                volume == null ||
                !root.activeSelf ||
                !volume.activeSelf)
            {
                hardErrors++;
            }

            Debug.Log(
                "[M58.0ABC.1 Diagnose]\n" +
                $"PersistenceInstalled={installed}\n" +
                $"FoundationRootPresent={root != null}\n" +
                $"FoundationRootActive={root != null && root.activeSelf}\n" +
                $"FoundationVolumePresent={volume != null}\n" +
                $"FoundationVolumeActive={volume != null && volume.activeSelf}\n" +
                $"HDRCameras={hdrCameras}/{totalCameras}\n" +
                $"HardErrors={hardErrors}\n" +
                $"STATUS={(hardErrors == 0 ? "PASS" : "CHECK_REQUIRED")}");
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(
                    InstalledKey,
                    false))
            {
                return;
            }

            if (state ==
                PlayModeStateChange.ExitingEditMode)
            {
                // Make sure the serialized scene contains the authoritative
                // foundation before Unity clones it for Play Mode.
                RepairFoundationAndSave(
                    "before Play Mode");
            }
            else if (state ==
                     PlayModeStateChange.EnteredEditMode)
            {
                // Unity has just restored the edit-time scene. Repair once more
                // in case a camera/Volume state was reverted or disabled.
                EditorApplication.delayCall +=
                    RepairAfterPlayMode;
            }
        }

        private static void RepairAfterPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            RepairFoundationAndSave(
                "after Play Mode");
        }

        private static void RepairFoundationAndSave(
            string reason)
        {
            if (EditorApplication.isPlaying)
                return;

            try
            {
                SetupPaintedAliveVisualFoundation_M58
                    .Setup();

                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M58.0ABC.1] Visual Foundation persistence repair PASS | " +
                    $"Reason={reason} | OpenScenesSaved=True");
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    "[M58.0ABC.1] Persistence repair FAILED | " +
                    $"Reason={reason}\n{ex}");
            }
        }

        private static GameObject
            FindSceneObjectIncludingInactive(
                string objectName)
        {
            foreach (GameObject candidate
                     in Resources.FindObjectsOfTypeAll<
                         GameObject>())
            {
                if (candidate == null ||
                    candidate.name != objectName ||
                    !candidate.scene.IsValid())
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
#endif
