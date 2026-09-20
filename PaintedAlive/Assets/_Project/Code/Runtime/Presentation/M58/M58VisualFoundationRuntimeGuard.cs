using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PaintedAlive.Presentation.M58
{
    /// <summary>
    /// Runtime authority for the M58 visual foundation.
    ///
    /// Guarantees that:
    /// - the serialized M58 global Volume stays active,
    /// - its authoritative shared profile is restored if another runtime system replaces it,
    /// - every runtime camera has HDR + URP post-processing enabled,
    /// - every runtime camera's Volume Layer Mask includes the M58 Volume layer,
    /// - newly-created / role-switched cameras are repaired before they render.
    ///
    /// Presentation-only: it never enables/disables gameplay cameras and never
    /// changes camera transforms, FOV, culling masks, renderer data, networking,
    /// collision, or gameplay authority.
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

        private const float SafetyRefreshInterval = 0.50f;

        private static M58VisualFoundationRuntimeGuard instance;

        private float nextSafetyRefresh;
        private bool warnedMissingFoundation;

        private Volume cachedFoundationVolume;
        private VolumeProfile authoritativeProfile;
        private int authoritativeVolumeLayer = -1;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject guardObject =
                new GameObject(GuardName);

            DontDestroyOnLoad(guardObject);

            instance =
                guardObject.AddComponent<
                    M58VisualFoundationRuntimeGuard>();
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

            SceneManager.sceneLoaded +=
                HandleSceneLoaded;

            RenderPipelineManager.beginCameraRendering +=
                HandleBeginCameraRendering;

            RefreshFoundationReference();
            ApplyNow(true);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            SceneManager.sceneLoaded -=
                HandleSceneLoaded;

            RenderPipelineManager.beginCameraRendering -=
                HandleBeginCameraRendering;
        }

        private void Update()
        {
            if (Time.unscaledTime <
                nextSafetyRefresh)
            {
                return;
            }

            nextSafetyRefresh =
                Time.unscaledTime +
                SafetyRefreshInterval;

            ApplyNow(false);
        }

        private void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            cachedFoundationVolume = null;
            authoritativeProfile = null;
            authoritativeVolumeLayer = -1;
            warnedMissingFoundation = false;
            nextSafetyRefresh = 0f;

            // sceneLoaded is early enough to establish the authoritative M58
            // profile before normal gameplay Start() methods run.
            RefreshFoundationReference();
            ApplyNow(true);
        }

        private void HandleBeginCameraRendering(
            ScriptableRenderContext context,
            Camera camera)
        {
            if (camera == null ||
                camera.gameObject == null ||
                !camera.gameObject.scene.IsValid())
            {
                return;
            }

            // This is the critical Play Mode guarantee: even cameras created,
            // enabled or swapped by role/network systems are repaired before
            // their first rendered frame.
            EnsureCamera(camera);
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
                        "[M58.0ABC.2] Visual Foundation root/volume " +
                        "was not found in the loaded gameplay scene. " +
                        "Run 58.0ABC - REPAIR Visual Stack in Edit Mode.");
                }

                return;
            }

            warnedMissingFoundation = false;

            if (verbose ||
                repairedCameras > 0)
            {
                Debug.Log(
                    "[M58.0ABC.2] Runtime Visual Foundation LOCKED | " +
                    $"CamerasRepaired={repairedCameras} | " +
                    $"VolumeLayer={authoritativeVolumeLayer} | " +
                    $"Profile={(authoritativeProfile != null ? authoritativeProfile.name : "<null>")}");
            }
        }

        private bool EnsureFoundationActive()
        {
            if (cachedFoundationVolume == null)
                RefreshFoundationReference();

            if (cachedFoundationVolume == null)
                return false;

            GameObject volumeObject =
                cachedFoundationVolume.gameObject;

            Transform rootTransform =
                volumeObject.transform.parent;

            if (rootTransform != null &&
                rootTransform.name == FoundationRootName &&
                !rootTransform.gameObject.activeSelf)
            {
                rootTransform.gameObject.SetActive(true);
            }

            if (!volumeObject.activeSelf)
                volumeObject.SetActive(true);

            if (!cachedFoundationVolume.enabled)
                cachedFoundationVolume.enabled = true;

            if (!cachedFoundationVolume.isGlobal)
                cachedFoundationVolume.isGlobal = true;

            if (!Mathf.Approximately(
                    cachedFoundationVolume.weight,
                    1f))
            {
                cachedFoundationVolume.weight = 1f;
            }

            if (!Mathf.Approximately(
                    cachedFoundationVolume.priority,
                    10f))
            {
                cachedFoundationVolume.priority = 10f;
            }

            if (authoritativeProfile == null &&
                cachedFoundationVolume.sharedProfile != null)
            {
                authoritativeProfile =
                    cachedFoundationVolume.sharedProfile;
            }

            if (authoritativeProfile != null &&
                cachedFoundationVolume.sharedProfile !=
                    authoritativeProfile)
            {
                cachedFoundationVolume.sharedProfile =
                    authoritativeProfile;
            }

            authoritativeVolumeLayer =
                volumeObject.layer;

            return
                cachedFoundationVolume.sharedProfile != null;
        }

        private void RefreshFoundationReference()
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
                cachedFoundationVolume = null;
                return;
            }

            if (!root.activeSelf)
                root.SetActive(true);

            if (!volumeObject.activeSelf)
                volumeObject.SetActive(true);

            Volume volume =
                volumeObject.GetComponent<Volume>();

            if (volume == null)
            {
                cachedFoundationVolume = null;
                return;
            }

            cachedFoundationVolume = volume;
            authoritativeVolumeLayer =
                volumeObject.layer;

            if (volume.sharedProfile != null)
            {
                authoritativeProfile =
                    volume.sharedProfile;
            }
        }

        private int EnsureAllRuntimeCameras()
        {
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

                if (EnsureCamera(camera))
                    changed++;
            }

            return changed;
        }

        private bool EnsureCamera(Camera camera)
        {
            bool changed = false;

            if (!camera.allowHDR)
            {
                camera.allowHDR = true;
                changed = true;
            }

            UniversalAdditionalCameraData data =
                camera.GetComponent<
                    UniversalAdditionalCameraData>();

            if (data == null)
            {
                data =
                    camera.gameObject.AddComponent<
                        UniversalAdditionalCameraData>();

                changed = true;
            }

            if (!data.renderPostProcessing)
            {
                data.renderPostProcessing = true;
                changed = true;
            }

            int volumeLayer =
                authoritativeVolumeLayer;

            if (volumeLayer < 0 &&
                cachedFoundationVolume != null)
            {
                volumeLayer =
                    cachedFoundationVolume.gameObject.layer;
            }

            if (volumeLayer >= 0 &&
                volumeLayer < 32)
            {
                int requiredBit =
                    1 << volumeLayer;

                int currentMask =
                    data.volumeLayerMask.value;

                if ((currentMask &
                     requiredBit) == 0)
                {
                    data.volumeLayerMask =
                        currentMask |
                        requiredBit;

                    changed = true;
                }
            }

            return changed;
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
    }
}
