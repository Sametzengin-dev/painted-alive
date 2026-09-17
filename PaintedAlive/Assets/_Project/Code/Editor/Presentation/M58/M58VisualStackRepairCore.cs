#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools.Presentation
{
    internal static class M58VisualStackRepairCore
    {
        internal const string RootName = "M58_VisualFoundation";
        internal const string VolumeName = "M58_GlobalVolume_PaintedAlive";
        internal const string FoundationProfilePath =
            "Assets/_Project/Art/Volumes/M58/VP_PaintedAlive_Global_M58.asset";

        internal const string BRootName = "M58B_LivingGallery_Lighting";
        internal const string BGradeName = "M58B_LivingGallery_Grade";

        internal const string TextureFolder =
            "Assets/_Project/Art/Textures/LivingGallery/M58Repair";

        internal const string MaterialFolder =
            "Assets/_Project/Art/Materials/M58/LivingGallery";

        internal static void RepairAll()
        {
            if (!ValidScene())
                return;

            CleanupM58DResiduals();
            SetupFoundation();
            SetupLivingGalleryLighting();
            SetupLivingGalleryMaterials();
            SaveCurrentScene();
            DiagnoseAll();
        }

        // ==============================================================
        // M58.0A — stable visual foundation
        // ==============================================================
        internal static void SetupFoundation()
        {
            if (!ValidScene())
                return;

            EnsureFolder("Assets/_Project/Art/Volumes/M58");

            GameObject root = FindOrCreateRoot(RootName);
            GameObject volumeObject = FindOrCreateChild(root.transform, VolumeName);

            Volume volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
                volume = Undo.AddComponent<Volume>(volumeObject);

            VolumeProfile profile =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(FoundationProfilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, FoundationProfilePath);
            }

            ConfigureFoundationProfile(profile);

            Undo.RecordObject(volume, "Repair M58 Visual Foundation");
            volume.enabled = true;
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.priority = 10f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);

            int updatedCameras = 0;

            foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (camera.gameObject.scene != SceneManager.GetActiveScene())
                    continue;

                bool changed = false;

                if (!camera.allowHDR)
                {
                    Undo.RecordObject(camera, "Enable M58 HDR");
                    camera.allowHDR = true;
                    changed = true;
                }

                UniversalAdditionalCameraData data =
                    camera.GetComponent<UniversalAdditionalCameraData>();

                if (data == null)
                {
                    data = Undo.AddComponent<UniversalAdditionalCameraData>(
                        camera.gameObject);
                    changed = true;
                }

                if (!data.renderPostProcessing)
                {
                    Undo.RecordObject(data, "Enable M58 Post Processing");
                    data.renderPostProcessing = true;
                    changed = true;
                }

                if (changed)
                {
                    updatedCameras++;
                    EditorUtility.SetDirty(camera);
                    EditorUtility.SetDirty(data);
                }
            }

            AssetDatabase.SaveAssets();
            SaveCurrentScene();

            Debug.Log(
                "[M58.0A REPAIR] PASS | " +
                $"Scene={SceneManager.GetActiveScene().name} | " +
                $"CamerasUpdated={updatedCameras} | " +
                "FoundationOwnedGrading=True | AutoSaved=True");
        }

        // ==============================================================
        // M58.0B — exact Living Gallery bounds, no whole-scene fallback
        // ==============================================================
        internal static void SetupLivingGalleryLighting()
        {
            if (!ValidScene())
                return;

            if (!TryExactLivingGalleryBounds(
                    out Bounds bounds,
                    out int matchedRenderers))
            {
                Debug.LogError(
                    "[M58.0B REPAIR] FAIL: Living Gallery exact-name renderers " +
                    "could not be resolved. No ActiveScene fallback was used.");
                return;
            }

            GameObject root = FindOrCreateRoot(RootName);

            Transform oldRig = FindRecursive(root.transform, BRootName);
            if (oldRig != null)
                Undo.DestroyObjectImmediate(oldRig.gameObject);

            GameObject rig = FindOrCreateChild(root.transform, BRootName);
            GameObject lightsRoot =
                FindOrCreateChild(rig.transform, "M58B_Lights");

            GameObject probesRoot =
                FindOrCreateChild(rig.transform, "M58B_ReflectionProbes");

            CreateAdaptiveLights(lightsRoot.transform, bounds);

            int probeCount =
                CreateAdaptiveReflectionProbes(probesRoot.transform, bounds);

            // Important repair:
            // M58.0A owns the global color grading. M58.0B must not shadow it.
            Transform grade = FindRecursive(rig.transform, BGradeName);
            if (grade != null)
                Undo.DestroyObjectImmediate(grade.gameObject);

            AssetDatabase.SaveAssets();
            SaveCurrentScene();

            Debug.Log(
                "[M58.0B REPAIR] PASS\n" +
                "BoundsSource=LivingGalleryExactManifestNames\n" +
                $"MatchedRenderers={matchedRenderers}\n" +
                $"BoundsCenter={bounds.center}\n" +
                $"BoundsSize={bounds.size}\n" +
                "Lights=3\n" +
                $"ReflectionProbes={probeCount}\n" +
                "HigherPriorityGradeVolume=False\n" +
                "FullSceneFallback=False\n" +
                "AutoSaved=True");
        }

        // ==============================================================
        // M58.0C — manifest-driven materials
        // ==============================================================
        internal static void SetupLivingGalleryMaterials()
        {
            if (!ValidScene())
                return;

            EnsureFolder(MaterialFolder);

            Dictionary<string, Material> variants =
                BuildAllMaterialVariants(
                    out int tripletsFound,
                    out int missingTextureFiles);

            Dictionary<string, List<Renderer>> sceneLookup =
                BuildRendererLookup();

            int exactObjectsFound = 0;
            int rendererSlotsReplaced = 0;
            int partialPreserved = 0;
            int unresolvedMultiMaterialSlots = 0;
            int duplicateNames = 0;

            foreach (KeyValuePair<string, string[]> pair
                     in M58LivingGalleryRepairData.ObjectMaterials)
            {
                if (!sceneLookup.TryGetValue(
                        pair.Key,
                        out List<Renderer> renderers))
                {
                    continue;
                }

                exactObjectsFound++;

                if (renderers.Count > 1)
                    duplicateNames += renderers.Count - 1;

                foreach (Renderer renderer in renderers)
                {
                    if (M58LivingGalleryRepairData.PartialVisibilityUsers
                        .Contains(pair.Key))
                    {
                        // These need their dedicated partial-visibility material
                        // instances. Do not accidentally make them ordinary opaque.
                        partialPreserved++;
                        continue;
                    }

                    rendererSlotsReplaced +=
                        ApplyExpectedMaterialsToRenderer(
                            renderer,
                            pair.Value,
                            variants,
                            ref unresolvedMultiMaterialSlots);
                }
            }

            int paperRefs = PatchDryingPaperReferences(variants);

            AssetDatabase.SaveAssets();
            SaveCurrentScene();

            if (rendererSlotsReplaced == 0)
            {
                Debug.LogError(
                    "[M58.0C REPAIR] NO_MATCHES: exact Living Gallery objects " +
                    "were inspected but no renderer material slot was repaired.");
                return;
            }

            Debug.Log(
                "[M58.0C REPAIR] PASS\n" +
                $"ExactManifestObjectsFound={exactObjectsFound}\n" +
                $"RendererSlotsReplaced={rendererSlotsReplaced}\n" +
                $"PartialVisibilityRenderersPreserved={partialPreserved}\n" +
                $"UnresolvedMultiMaterialSlots={unresolvedMultiMaterialSlots}\n" +
                $"DuplicateExactNames={duplicateNames}\n" +
                $"TextureTripletsFound={tripletsFound}\n" +
                $"MissingTextureFiles={missingTextureFiles}\n" +
                $"DryingPaperRefsUpdated={paperRefs}\n" +
                $"MaterialVariants={variants.Count}\n" +
                "AssignmentSource=ExactManifestObjectNames\n" +
                "AutoSaved=True");
        }

        // ==============================================================
        // Combined diagnose
        // ==============================================================
        internal static void DiagnoseAll()
        {
            Scene scene = SceneManager.GetActiveScene();

            GameObject root = GameObject.Find(RootName);
            GameObject volumeObject = GameObject.Find(VolumeName);

            Volume volume =
                volumeObject != null
                    ? volumeObject.GetComponent<Volume>()
                    : null;

            VolumeProfile profile =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                    FoundationProfilePath);

            int aHard = 0;

            if (root == null ||
                volume == null ||
                profile == null ||
                !volume.enabled ||
                !volume.isGlobal ||
                volume.sharedProfile != profile)
            {
                aHard++;
            }

            GameObject bRig =
                root != null
                    ? FindRecursive(root.transform, BRootName)?.gameObject
                    : null;

            int bLights =
                bRig != null
                    ? bRig.GetComponentsInChildren<Light>(true).Length
                    : 0;

            int bProbes =
                bRig != null
                    ? bRig.GetComponentsInChildren<ReflectionProbe>(true).Length
                    : 0;

            bool bHasExtraGrade =
                bRig != null &&
                FindRecursive(bRig.transform, BGradeName) != null;

            int bHard =
                bRig == null ||
                bLights != 3 ||
                bProbes < 1 ||
                bHasExtraGrade
                    ? 1
                    : 0;

            int cVariants = 0;

            if (AssetDatabase.IsValidFolder(MaterialFolder))
            {
                foreach (string guid in AssetDatabase.FindAssets(
                             "t:Material",
                             new[] { MaterialFolder }))
                {
                    Material material =
                        AssetDatabase.LoadAssetAtPath<Material>(
                            AssetDatabase.GUIDToAssetPath(guid));

                    if (material != null &&
                        material.name.EndsWith(
                            "_M58C_REPAIR",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        cVariants++;
                    }
                }
            }

            int cAssigned = 0;

            foreach (Renderer renderer
                     in UnityEngine.Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene != scene)
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null &&
                        material.name.EndsWith(
                            "_M58C_REPAIR",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        cAssigned++;
                    }
                }
            }

            int cHard =
                cVariants == 0 || cAssigned == 0
                    ? 1
                    : 0;

            int dResiduals = CountM58DResiduals();

            int hardErrors =
                aHard +
                bHard +
                cHard +
                (dResiduals > 0 ? 1 : 0);

            Debug.Log(
                "[M58 ABC Visual Stack Diagnose]\n" +
                $"M58A_Foundation={(aHard == 0 ? "PASS" : "FAIL")}\n" +
                $"M58B_Lighting={(bHard == 0 ? "PASS" : "FAIL")} | " +
                $"Lights={bLights} Probes={bProbes} " +
                $"ExtraGrade={bHasExtraGrade}\n" +
                $"M58C_Materials={(cHard == 0 ? "PASS" : "FAIL")} | " +
                $"Variants={cVariants} AssignedSlots={cAssigned}\n" +
                $"M58D_Residuals={dResiduals}\n" +
                $"HardErrors={hardErrors}\n" +
                $"STATUS={(hardErrors == 0 ? "VISUAL_STACK_PASS" : "CHECK_REQUIRED")}");
        }

        // ==============================================================
        // M58.0D cleanup, without touching A/B/C
        // ==============================================================
        internal static void CleanupM58DResiduals()
        {
            GameObject runtimeRoot =
                GameObject.Find("M58D_ReactivePaintedSurfaces");

            if (runtimeRoot != null)
                Undo.DestroyObjectImmediate(runtimeRoot);

            string[] ownedPaths =
            {
                "Assets/_Project/Art/Materials/Presentation/M58",
                "Assets/_Project/Art/Textures/Presentation/M58/Generated",
                "Assets/_Project/Code/Runtime/Presentation/M58/M58ReactivePaintRuntimeBinder.cs",
                "Assets/_Project/Code/Runtime/Presentation/M58/M58WaterblobVisualPulse.cs",
                "Assets/_Project/Code/Editor/Presentation/M58/SetupReactivePaintedSurfaces_M58D.cs",
            };

            foreach (string path in ownedPaths)
            {
                if (AssetDatabase.IsValidFolder(path) ||
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            AssetDatabase.SaveAssets();
        }

        static int CountM58DResiduals()
        {
            int result =
                GameObject.Find("M58D_ReactivePaintedSurfaces") != null
                    ? 1
                    : 0;

            result += AssetDatabase.FindAssets("MAT_M58D_").Length;
            return result;
        }

        // ==============================================================
        // Foundation profile
        // ==============================================================
        static void ConfigureFoundationProfile(VolumeProfile profile)
        {
            Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.active = true;
            color.postExposure.Override(-0.10f);
            color.contrast.Override(12f);
            color.hueShift.Override(0f);
            color.saturation.Override(5f);

            WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
            whiteBalance.active = true;
            whiteBalance.temperature.Override(7f);
            whiteBalance.tint.Override(2f);

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.Override(1.10f);
            bloom.intensity.Override(0.15f);
            bloom.scatter.Override(0.65f);
            bloom.clamp.Override(65472f);

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.Override(0.10f);
            vignette.smoothness.Override(0.35f);
            vignette.rounded.Override(false);

            FilmGrain filmGrain = GetOrAdd<FilmGrain>(profile);
            filmGrain.active = true;
            filmGrain.intensity.Override(0.035f);
            filmGrain.response.Override(0.75f);

            EditorUtility.SetDirty(profile);
        }

        static T GetOrAdd<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
                return component;

            return profile.Add<T>(true);
        }

        // ==============================================================
        // Exact LG bounds
        // ==============================================================
        static bool TryExactLivingGalleryBounds(
            out Bounds bounds,
            out int rendererCount)
        {
            Dictionary<string, List<Renderer>> lookup =
                BuildRendererLookup();

            bool initialized = false;
            rendererCount = 0;
            bounds = default;

            foreach (string objectName
                     in M58LivingGalleryRepairData.ObjectMaterials.Keys)
            {
                if (!lookup.TryGetValue(
                        objectName,
                        out List<Renderer> renderers))
                {
                    continue;
                }

                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null ||
                        !renderer.enabled ||
                        IsCollisionOrDebug(renderer.transform))
                    {
                        continue;
                    }

                    Bounds current = renderer.bounds;

                    if (!Finite(current.center) ||
                        !Finite(current.size) ||
                        current.size.sqrMagnitude < 0.01f)
                    {
                        continue;
                    }

                    if (!initialized)
                    {
                        bounds = current;
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(current);
                    }

                    rendererCount++;
                }
            }

            return initialized && rendererCount >= 20;
        }

        // ==============================================================
        // B lighting
        // ==============================================================
        static void CreateAdaptiveLights(
            Transform parent,
            Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            float longLength = Mathf.Max(size.x, size.z);
            float shortLength =
                Mathf.Max(1f, Mathf.Min(size.x, size.z));

            float vertical = Mathf.Max(4f, size.y);

            Vector3 longAxis =
                size.x >= size.z
                    ? Vector3.right
                    : Vector3.forward;

            Vector3 shortAxis =
                size.x >= size.z
                    ? Vector3.forward
                    : Vector3.right;

            Vector3 target =
                center +
                Vector3.up *
                Mathf.Clamp(vertical * 0.05f, 0.5f, 2.5f);

            float height =
                Mathf.Clamp(vertical * 0.80f + 5f, 8f, 28f);

            float range =
                Mathf.Clamp(
                    Mathf.Sqrt(
                        longLength * longLength +
                        vertical * vertical) * 1.15f,
                    20f,
                    120f);

            CreateSpot(
                parent,
                "M58B_Key_Warm",
                center
                - longAxis * longLength * 0.18f
                - shortAxis * shortLength * 0.22f
                + Vector3.up * height,
                target,
                new Color(1.00f, 0.93f, 0.84f),
                2.45f,
                range,
                82f,
                48f,
                LightShadows.Soft,
                0.70f);

            CreateSpot(
                parent,
                "M58B_Fill_Cool",
                center
                + longAxis * longLength * 0.22f
                + shortAxis * shortLength * 0.32f
                + Vector3.up * (height * 0.75f),
                target,
                new Color(0.82f, 0.90f, 1.00f),
                0.95f,
                range,
                95f,
                58f,
                LightShadows.None,
                0f);

            CreateSpot(
                parent,
                "M58B_Rim_Warm",
                center
                + longAxis * longLength * 0.38f
                - shortAxis * shortLength * 0.18f
                + Vector3.up * (height * 0.62f),
                center
                + Vector3.up *
                Mathf.Clamp(vertical * 0.18f, 1f, 4f),
                new Color(1.00f, 0.82f, 0.68f),
                1.15f,
                range,
                66f,
                36f,
                LightShadows.None,
                0f);
        }

        static void CreateSpot(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 target,
            Color color,
            float intensity,
            float range,
            float outerAngle,
            float innerAngle,
            LightShadows shadows,
            float shadowStrength)
        {
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent);
            go.transform.position = position;

            Vector3 direction = target - position;
            go.transform.rotation =
                direction.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up)
                    : Quaternion.identity;

            Light light = Undo.AddComponent<Light>(go);
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = outerAngle;
            light.innerSpotAngle = innerAngle;
            light.shadows = shadows;
            light.shadowStrength = shadowStrength;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.35f;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        static int CreateAdaptiveReflectionProbes(
            Transform parent,
            Bounds bounds)
        {
            Vector3 size = bounds.size;
            Vector3 center = bounds.center;

            bool xIsLong = size.x >= size.z;
            float longLength =
                xIsLong
                    ? size.x
                    : size.z;

            int count =
                longLength >= 60f
                    ? 3
                    : longLength >= 30f
                        ? 2
                        : 1;

            float overlap =
                Mathf.Clamp(longLength * 0.08f, 2f, 8f);

            float segmentLength =
                longLength / count;

            for (int i = 0; i < count; i++)
            {
                float localLong =
                    -longLength * 0.5f +
                    segmentLength * (i + 0.5f);

                Vector3 probeCenter = center;

                if (xIsLong)
                    probeCenter.x += localLong;
                else
                    probeCenter.z += localLong;

                GameObject go =
                    new GameObject(
                        $"M58B_ReflectionProbe_{i + 1:00}");

                Undo.RegisterCreatedObjectUndo(
                    go,
                    "Create M58B Reflection Probe");

                go.transform.SetParent(parent);
                go.transform.position = probeCenter;

                ReflectionProbe probe =
                    Undo.AddComponent<ReflectionProbe>(go);

                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode =
                    ReflectionProbeRefreshMode.OnAwake;

                probe.timeSlicingMode =
                    ReflectionProbeTimeSlicingMode.AllFacesAtOnce;

                probe.resolution = 128;
                probe.hdr = true;
                probe.boxProjection = true;
                probe.intensity = 0.80f;
                probe.importance = 110;

                probe.blendDistance =
                    Mathf.Clamp(
                        Mathf.Min(size.x, size.z) * 0.15f,
                        1f,
                        6f);

                probe.size =
                    new Vector3(
                        Mathf.Max(
                            4f,
                            xIsLong
                                ? segmentLength + overlap
                                : size.x + 4f),
                        Mathf.Max(8f, size.y + 6f),
                        Mathf.Max(
                            4f,
                            xIsLong
                                ? size.z + 4f
                                : segmentLength + overlap));
            }

            return count;
        }

        // ==============================================================
        // C materials
        // ==============================================================
        static Dictionary<string, Material>
            BuildAllMaterialVariants(
                out int tripletsFound,
                out int missingTextureFiles)
        {
            tripletsFound = 0;
            missingTextureFiles = 0;

            var variants =
                new Dictionary<string, Material>(
                    StringComparer.Ordinal);

            foreach (string canonical
                     in M58LivingGalleryRepairData.MaterialNames)
            {
                Material variant =
                    BuildVariant(
                        canonical,
                        out bool hasTextureTriplet,
                        out int missing);

                if (hasTextureTriplet)
                    tripletsFound++;

                missingTextureFiles += missing;

                if (variant != null)
                    variants[canonical] = variant;
            }

            return variants;
        }

        static Material BuildVariant(
            string canonical,
            out bool hasTextureTriplet,
            out int missingTextureFiles)
        {
            hasTextureTriplet = false;
            missingTextureFiles = 0;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                Debug.LogError(
                    "[M58.0C REPAIR] URP Lit shader not found.");
                return null;
            }

            string safeName = Sanitize(canonical);

            string materialPath =
                $"{MaterialFolder}/{safeName}_M58C_REPAIR.mat";

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    materialPath);

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = safeName + "_M58C_REPAIR"
                };

                AssetDatabase.CreateAsset(
                    material,
                    materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", Color.white);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.2f);

            if (material.HasProperty("_BumpScale"))
                material.SetFloat("_BumpScale", 0.16f);

            string stem =
                canonical.StartsWith(
                    "MAT_",
                    StringComparison.OrdinalIgnoreCase)
                    ? canonical.Substring(4)
                    : null;

            if (!string.IsNullOrEmpty(stem))
            {
                string baseName =
                    stem + "_BaseColor.png";

                string normalName =
                    stem + "_Normal.png";

                string maskName =
                    stem + "_Mask.png";

                Texture2D baseTexture =
                    LoadRepairTexture(
                        baseName,
                        false,
                        false);

                Texture2D normalTexture =
                    LoadRepairTexture(
                        normalName,
                        true,
                        false);

                Texture2D maskTexture =
                    LoadRepairTexture(
                        maskName,
                        false,
                        true);

                bool any =
                    baseTexture != null ||
                    normalTexture != null ||
                    maskTexture != null;

                if (any)
                {
                    hasTextureTriplet = true;

                    if (baseTexture == null)
                        missingTextureFiles++;

                    if (normalTexture == null)
                        missingTextureFiles++;

                    if (maskTexture == null)
                        missingTextureFiles++;

                    if (baseTexture != null)
                        material.SetTexture(
                            "_BaseMap",
                            baseTexture);

                    if (normalTexture != null)
                    {
                        material.SetTexture(
                            "_BumpMap",
                            normalTexture);

                        material.EnableKeyword(
                            "_NORMALMAP");

                        material.SetFloat(
                            "_BumpScale",
                            0.16f);
                    }

                    if (maskTexture != null)
                    {
                        // Authoritative Living Gallery handoff:
                        // R = Metallic, A = Smoothness.
                        material.SetTexture(
                            "_MetallicGlossMap",
                            maskTexture);

                        material.EnableKeyword(
                            "_METALLICSPECGLOSSMAP");

                        material.SetFloat(
                            "_Metallic",
                            1f);

                        material.SetFloat(
                            "_Smoothness",
                            1f);
                    }
                }
            }

            if (!hasTextureTriplet)
                ApplyConstantMaterial(canonical, material);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);

            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            return material;
        }

        static void ApplyConstantMaterial(
            string canonical,
            Material material)
        {
            if (canonical == "MAT_Ivy_Muted")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(
                        0.067f,
                        0.085f,
                        0.041f,
                        1f));

                material.SetFloat(
                    "_Smoothness",
                    0.14f);

                material.SetFloat(
                    "_Metallic",
                    0f);
            }
            else if (canonical == "Ink | reflection etching")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(
                        0.055f,
                        0.066f,
                        0.072f,
                        1f));

                material.SetFloat(
                    "_Smoothness",
                    0.68f);

                material.SetFloat(
                    "_Metallic",
                    0.5f);
            }
            else if (canonical == "Light | warm frosted glass")
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(
                        0.9f,
                        0.51f,
                        0.18f,
                        1f));

                material.SetFloat(
                    "_Smoothness",
                    0.6f);

                material.SetFloat(
                    "_Metallic",
                    0f);

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor(
                        "_EmissionColor",
                        new Color(
                            1f,
                            0.57f,
                            0.24f,
                            1f) * 3f);

                    material.EnableKeyword("_EMISSION");
                }
            }
        }

        static Texture2D LoadRepairTexture(
            string fileName,
            bool normal,
            bool mask)
        {
            string path =
                $"{TextureFolder}/{fileName}";

            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    path);

            if (texture == null)
                return null;

            TextureImporter importer =
                AssetImporter.GetAtPath(path)
                as TextureImporter;

            if (importer != null)
            {
                bool changed = false;

                TextureImporterType desiredType =
                    normal
                        ? TextureImporterType.NormalMap
                        : TextureImporterType.Default;

                if (importer.textureType != desiredType)
                {
                    importer.textureType = desiredType;
                    changed = true;
                }

                bool desiredSrgb =
                    !normal && !mask;

                if (importer.sRGBTexture != desiredSrgb)
                {
                    importer.sRGBTexture = desiredSrgb;
                    changed = true;
                }

                if (importer.wrapMode != TextureWrapMode.Repeat)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    changed = true;
                }

                if (importer.filterMode != FilterMode.Trilinear)
                {
                    importer.filterMode = FilterMode.Trilinear;
                    changed = true;
                }

                if (changed)
                    importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                path);
        }

        static int ApplyExpectedMaterialsToRenderer(
            Renderer renderer,
            string[] expected,
            Dictionary<string, Material> variants,
            ref int unresolved)
        {
            if (renderer == null ||
                expected == null ||
                expected.Length == 0)
            {
                return 0;
            }

            Material[] materials =
                renderer.sharedMaterials;

            if (materials == null ||
                materials.Length == 0)
            {
                return 0;
            }

            string[] resolved =
                new string[materials.Length];

            // Single-material authored object:
            // exact manifest identity is sufficient, regardless of Unity import name.
            if (expected.Length == 1)
            {
                for (int i = 0; i < resolved.Length; i++)
                    resolved[i] = expected[0];
            }
            else
            {
                // Multi-material objects are only changed when the current slot
                // provides enough identity. We do not guess slot ordering.
                for (int i = 0; i < materials.Length; i++)
                    resolved[i] =
                        ResolveCanonical(
                            materials[i],
                            expected);
            }

            bool changed = false;
            int changedCount = 0;

            for (int i = 0; i < materials.Length; i++)
            {
                string canonical = resolved[i];

                if (canonical == null)
                {
                    unresolved++;
                    continue;
                }

                if (!variants.TryGetValue(
                        canonical,
                        out Material variant) ||
                    variant == null)
                {
                    unresolved++;
                    continue;
                }

                if (materials[i] != variant)
                {
                    materials[i] = variant;
                    changed = true;
                    changedCount++;
                }
            }

            if (changed)
            {
                Undo.RecordObject(
                    renderer,
                    "Apply repaired Living Gallery materials");

                renderer.sharedMaterials =
                    materials;

                EditorUtility.SetDirty(renderer);
            }

            return changedCount;
        }

        static string ResolveCanonical(
            Material material,
            string[] expected)
        {
            if (material == null)
                return null;

            string blob =
                (material.name +
                 "|" +
                 AssetDatabase.GetAssetPath(material))
                .ToLowerInvariant();

            try
            {
                foreach (string propertyName
                         in material.GetTexturePropertyNames())
                {
                    Texture texture =
                        material.GetTexture(propertyName);

                    if (texture != null)
                        blob +=
                            "|" +
                            texture.name.ToLowerInvariant();
                }
            }
            catch
            {
                // If a shader does not expose a texture list,
                // material name/path is still usable.
            }

            foreach (string canonical in expected)
            {
                string stem =
                    CanonicalStem(canonical)
                    .ToLowerInvariant();

                if (blob.Contains(stem))
                    return canonical;

                if (Normalize(blob)
                    .Contains(
                        Normalize(canonical)))
                {
                    return canonical;
                }
            }

            return null;
        }

        static int PatchDryingPaperReferences(
            Dictionary<string, Material> variants)
        {
            int count = 0;

            foreach (MonoBehaviour behaviour
                     in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (behaviour == null ||
                    behaviour.gameObject.scene !=
                        SceneManager.GetActiveScene() ||
                    behaviour.GetType().Name !=
                        "DryingPaperSurface")
                {
                    continue;
                }

                SerializedObject serialized =
                    new SerializedObject(behaviour);

                SerializedProperty iterator =
                    serialized.GetIterator();

                bool enterChildren = true;
                bool changed = false;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.propertyType !=
                        SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    if (!(iterator.objectReferenceValue is Material))
                        continue;

                    string property =
                        (iterator.name +
                         "|" +
                         iterator.propertyPath)
                        .ToLowerInvariant();

                    string key =
                        property.Contains("wet")
                            ? "MAT_Paper_Wet"
                            : property.Contains("dry")
                                ? "MAT_Paper_Dry"
                                : null;

                    if (key == null)
                        continue;

                    if (variants.TryGetValue(
                            key,
                            out Material variant) &&
                        iterator.objectReferenceValue != variant)
                    {
                        iterator.objectReferenceValue =
                            variant;

                        changed = true;
                        count++;
                    }
                }

                if (changed)
                {
                    Undo.RecordObject(
                        behaviour,
                        "Patch repaired drying-paper materials");

                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(behaviour);
                }
            }

            return count;
        }

        // ==============================================================
        // Utilities
        // ==============================================================
        static Dictionary<string, List<Renderer>>
            BuildRendererLookup()
        {
            var result =
                new Dictionary<string, List<Renderer>>(
                    StringComparer.Ordinal);

            Scene scene =
                SceneManager.GetActiveScene();

            foreach (Renderer renderer
                     in UnityEngine.Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (renderer == null ||
                    renderer.gameObject.scene != scene)
                {
                    continue;
                }

                if (!result.TryGetValue(
                        renderer.gameObject.name,
                        out List<Renderer> list))
                {
                    list = new List<Renderer>();
                    result[renderer.gameObject.name] =
                        list;
                }

                list.Add(renderer);
            }

            return result;
        }

        static GameObject FindOrCreateRoot(string name)
        {
            GameObject existing =
                GameObject.Find(name);

            if (existing != null)
                return existing;

            GameObject created =
                new GameObject(name);

            Undo.RegisterCreatedObjectUndo(
                created,
                "Create " + name);

            return created;
        }

        static GameObject FindOrCreateChild(
            Transform parent,
            string name)
        {
            Transform existing =
                parent.Find(name);

            if (existing != null)
                return existing.gameObject;

            GameObject created =
                new GameObject(name);

            Undo.RegisterCreatedObjectUndo(
                created,
                "Create " + name);

            created.transform.SetParent(parent);
            created.transform.localPosition =
                Vector3.zero;

            created.transform.localRotation =
                Quaternion.identity;

            created.transform.localScale =
                Vector3.one;

            return created;
        }

        static Transform FindRecursive(
            Transform root,
            string name)
        {
            foreach (Transform transform
                     in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                    return transform;
            }

            return null;
        }

        static bool IsCollisionOrDebug(Transform transform)
        {
            Transform current = transform;

            while (current != null)
            {
                string name = current.name;

                if (name.StartsWith(
                        "COLLISION_",
                        StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf(
                        "debug",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf(
                        "gizmo",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        static bool Finite(Vector3 value)
        {
            return
                !float.IsNaN(value.x) &&
                !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) &&
                !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) &&
                !float.IsInfinity(value.z);
        }

        static string CanonicalStem(string canonical)
        {
            if (canonical.StartsWith(
                    "MAT_",
                    StringComparison.OrdinalIgnoreCase))
            {
                return canonical.Substring(4);
            }

            return canonical
                .Replace(" | ", "_")
                .Replace(" ", "_");
        }

        static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return new string(
                value
                    .ToLowerInvariant()
                    .Where(char.IsLetterOrDigit)
                    .ToArray());
        }

        static string Sanitize(string value)
        {
            string result =
                value
                    .Replace(" | ", "_")
                    .Replace("|", "_")
                    .Replace(" ", "_");

            foreach (char c in Path.GetInvalidFileNameChars())
                result = result.Replace(c, '_');

            return result;
        }

        static bool ValidScene()
        {
            Scene scene =
                SceneManager.GetActiveScene();

            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                Debug.LogError(
                    "[M58 Repair] No valid loaded scene.");

                return false;
            }

            return true;
        }

        static void SaveCurrentScene()
        {
            Scene scene =
                SceneManager.GetActiveScene();

            EditorSceneManager.MarkSceneDirty(scene);

            if (!string.IsNullOrEmpty(scene.path))
                EditorSceneManager.SaveScene(scene);
        }

        static void EnsureFolder(string assetPath)
        {
            string[] parts =
                assetPath.Split('/');

            string current =
                parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next =
                    current +
                    "/" +
                    parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
