using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.Presentation.M58
{
    /// <summary>
    /// Keeps the M58 visual foundation effective when gameplay switches/enables
    /// role cameras at runtime. Uses reflection for URP camera data so this
    /// runtime assembly does not require a new package/asmdef reference.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    public sealed class M58VisualFoundationRuntimeGuard : MonoBehaviour
    {
        private const string GuardName =
            "M58_VisualFoundation_RuntimeGuard";

        private const string FoundationRootName =
            "M58_VisualFoundation";

        private const string FoundationVolumeName =
            "M58_GlobalVolume_PaintedAlive";

        private const float RefreshInterval = 0.40f;

        private static M58VisualFoundationRuntimeGuard instance;
        private static Type universalCameraDataType;
        private static PropertyInfo renderPostProcessingProperty;

        private float nextRefresh;
        private bool warnedMissingFoundation;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject guardObject =
                GameObject.Find(GuardName);

            if (guardObject == null)
            {
                guardObject =
                    new GameObject(GuardName);

                DontDestroyOnLoad(guardObject);
            }

            instance =
                guardObject.GetComponent<
                    M58VisualFoundationRuntimeGuard>();

            if (instance == null)
            {
                instance =
                    guardObject.AddComponent<
                        M58VisualFoundationRuntimeGuard>();
            }
        }

        private void Awake()
        {
            if (instance != null &&
                instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            ResolveUrpTypes();

            SceneManager.sceneLoaded +=
                HandleSceneLoaded;

            ApplyNow(true);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            SceneManager.sceneLoaded -=
                HandleSceneLoaded;
        }

        private void Update()
        {
            if (Time.unscaledTime <
                nextRefresh)
            {
                return;
            }

            nextRefresh =
                Time.unscaledTime +
                RefreshInterval;

            ApplyNow(false);
        }

        private void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            nextRefresh = 0f;
            ApplyNow(true);
        }

        private void ApplyNow(bool verbose)
        {
            bool foundationFound =
                EnsureFoundationActive();

            int repairedCameras =
                EnsureAllRuntimeCameras();

            if (!foundationFound)
            {
                if (!warnedMissingFoundation)
                {
                    warnedMissingFoundation = true;

                    Debug.LogWarning(
                        "[M58.0ABC.1] Visual Foundation root/volume " +
                        "was not found in the loaded gameplay scene. " +
                        "Run the M58.0ABC repair setup in the editor.");
                }

                return;
            }

            warnedMissingFoundation = false;

            if (verbose ||
                repairedCameras > 0)
            {
                Debug.Log(
                    "[M58.0ABC.1] Runtime Visual Foundation active | " +
                    $"CamerasRepaired={repairedCameras}");
            }
        }

        private static bool EnsureFoundationActive()
        {
            GameObject root =
                FindSceneObjectIncludingInactive(
                    FoundationRootName);

            GameObject volumeObject =
                FindSceneObjectIncludingInactive(
                    FoundationVolumeName);

            if (root == null ||
                volumeObject == null)
            {
                return false;
            }

            if (!root.activeSelf)
                root.SetActive(true);

            if (!volumeObject.activeSelf)
                volumeObject.SetActive(true);

            Component[] components =
                volumeObject.GetComponents<Component>();

            for (int i = 0;
                 i < components.Length;
                 i++)
            {
                Component component =
                    components[i];

                if (component == null ||
                    !string.Equals(
                        component.GetType().Name,
                        "Volume",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (component is Behaviour behaviour &&
                    !behaviour.enabled)
                {
                    behaviour.enabled = true;
                }

                SetPropertyIfPresent(
                    component,
                    "isGlobal",
                    true);

                SetPropertyIfPresent(
                    component,
                    "weight",
                    1f);

                SetPropertyIfPresent(
                    component,
                    "priority",
                    10f);

                return true;
            }

            return false;
        }

        private static int EnsureAllRuntimeCameras()
        {
            ResolveUrpTypes();

            int changed = 0;

            Camera[] cameras =
                Resources.FindObjectsOfTypeAll<Camera>();

            for (int i = 0;
                 i < cameras.Length;
                 i++)
            {
                Camera camera =
                    cameras[i];

                if (camera == null ||
                    camera.gameObject == null ||
                    !camera.gameObject.scene.IsValid())
                {
                    continue;
                }

                bool cameraChanged = false;

                if (!camera.allowHDR)
                {
                    camera.allowHDR = true;
                    cameraChanged = true;
                }

                if (universalCameraDataType != null)
                {
                    Component cameraData =
                        camera.GetComponent(
                            universalCameraDataType);

                    if (cameraData == null)
                    {
                        try
                        {
                            cameraData =
                                camera.gameObject.AddComponent(
                                    universalCameraDataType);

                            cameraChanged = true;
                        }
                        catch
                        {
                            // A camera that cannot accept URP additional
                            // data is simply ignored. No gameplay object is
                            // otherwise modified.
                        }
                    }

                    if (cameraData != null &&
                        renderPostProcessingProperty != null)
                    {
                        object current =
                            renderPostProcessingProperty
                                .GetValue(
                                    cameraData,
                                    null);

                        if (current is bool enabled &&
                            !enabled)
                        {
                            renderPostProcessingProperty
                                .SetValue(
                                    cameraData,
                                    true,
                                    null);

                            cameraChanged = true;
                        }
                    }
                }

                if (cameraChanged)
                    changed++;
            }

            return changed;
        }

        private static void ResolveUrpTypes()
        {
            if (universalCameraDataType != null)
                return;

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0;
                 i < assemblies.Length;
                 i++)
            {
                Type candidate =
                    assemblies[i].GetType(
                        "UnityEngine.Rendering.Universal." +
                        "UniversalAdditionalCameraData",
                        false);

                if (candidate == null)
                    continue;

                universalCameraDataType =
                    candidate;

                renderPostProcessingProperty =
                    candidate.GetProperty(
                        "renderPostProcessing",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                break;
            }
        }

        private static GameObject
            FindSceneObjectIncludingInactive(
                string objectName)
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<
                    GameObject>();

            for (int i = 0;
                 i < objects.Length;
                 i++)
            {
                GameObject candidate =
                    objects[i];

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

        private static void SetPropertyIfPresent(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (property == null ||
                !property.CanWrite)
            {
                return;
            }

            try
            {
                property.SetValue(
                    target,
                    value,
                    null);
            }
            catch
            {
                // Keep the guard presentation-only and non-fatal.
            }
        }
    }
}
