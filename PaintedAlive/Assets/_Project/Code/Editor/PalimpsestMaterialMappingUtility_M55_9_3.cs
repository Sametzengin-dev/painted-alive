#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PalimpsestMaterialMappingUtility_M55_9_3
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";
    private const string SourceMappingPath =
        "Assets/_Project/Data/Environment/Palimpsest/material_mapping.json";
    private const string CompiledMappingPath =
        "Assets/_Project/Data/Environment/Palimpsest/material_mapping_unity.json";
    private const string PaintingFolder =
        "Assets/_Project/Art/Textures/Environments/Palimpsest/Paintings";
    private const string MaterialFolder =
        "Assets/_Project/Materials/Environment/Palimpsest/Generated";
    private const string PalimpsestRootName = "M55_9_Palimpsest_Continuation";
    private const string VisualRootName = "The_Palimpsest_Visual";
    private const string ExpectedSourceSha256 =
        "5befc4ee7565deeb98df9558849d879d29ef2e7a63fda787f7fa67ec8b186e8f";

    [Serializable]
    private sealed class MappingManifest
    {
        public string schema;
        public string sourceSha256;
        public int materialCount;
        public MappingRecord[] materials;
    }

    [Serializable]
    private sealed class MappingRecord
    {
        public string material;
        public string classification;
        public string[] objectsUsing;
        public string[] textureNames;
        public bool hasSurfaceInputs;
        public float[] baseColor;
        public float metallic;
        public float roughness;
        public float alpha;
        public float[] emissionColor;
        public float emissionStrength;
        public string[] nodes;
    }

    private sealed class MappingReport
    {
        public bool SourcePresent;
        public bool SourceHashOk;
        public bool CompiledPresent;
        public bool CompiledHashOk;
        public int Records;
        public int DirectUrp;
        public int RebuildUrp;
        public int CustomShaderRequired;
        public int SurfaceRecords;
        public int MaterialsValid;
        public int TextureRefsExpected;
        public int TextureRefsFound;
        public int VisualRenderers;
        public int VisualRenderersCovered;
        public int MappingOnlyAbsentRecords;
        public readonly List<string> Problems = new List<string>();

        public bool Pass =>
            SourcePresent &&
            SourceHashOk &&
            CompiledPresent &&
            CompiledHashOk &&
            Records == 30 &&
            DirectUrp == 9 &&
            RebuildUrp == 20 &&
            CustomShaderRequired == 1 &&
            MaterialsValid == SurfaceRecords &&
            TextureRefsFound == TextureRefsExpected &&
            Problems.Count == 0;
    }

    private static MappingManifest cachedManifest;
    private static Dictionary<string, MappingRecord> cachedByName;

    [MenuItem(MenuRoot + "55.9.3 - Apply Palimpsest Full Material Mapping")]
    public static void ApplyMenu()
    {
        RequireEditMode();

        MappingManifest manifest = LoadManifest(true);
        EnsureAssetFolder(MaterialFolder);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
            throw new InvalidOperationException(
                "Universal Render Pipeline/Lit shader bulunamadı.");

        int applied = 0;
        int skippedNoSurface = 0;

        foreach (MappingRecord record in manifest.materials)
        {
            if (record == null || string.IsNullOrEmpty(record.material))
                continue;

            if (!record.hasSurfaceInputs)
            {
                // material_mapping contains MAT_Atelier_Haze as a Blender volume.
                // The current exported Visual FBX does not contain Atmosphere_BelowRoutes.
                // Do not invent a surface reconstruction for missing volume data.
                skippedNoSurface++;
                continue;
            }

            Material target = LoadOrCreateGeneratedMaterial(record.material, lit);
            ApplyRecordToMaterial(record, target);
            applied++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject sceneRoot = FindSceneObjectByName(PalimpsestRootName);
        if (sceneRoot != null && SceneManager.GetActiveScene().IsValid())
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            $"[M55.9.3 Palimpsest Material Mapping] Applied={applied}, " +
            $"SkippedNoSurface={skippedNoSurface}, SourceSHA={ExpectedSourceSha256}",
            sceneRoot);

        DiagnoseInternal(true);
    }

    [MenuItem(MenuRoot + "55.9.3 - Diagnose Palimpsest Full Material Mapping")]
    public static void DiagnoseMenu()
    {
        DiagnoseInternal(true);
    }

    public static bool TryApplyMappedMaterial(
        string sourceMaterialName,
        Material target,
        out string classification)
    {
        classification = null;

        if (string.IsNullOrEmpty(sourceMaterialName) || target == null)
            return false;

        MappingManifest manifest;
        try
        {
            manifest = LoadManifest(false);
        }
        catch
        {
            return false;
        }

        if (cachedByName == null)
            BuildLookup(manifest);

        if (!cachedByName.TryGetValue(sourceMaterialName, out MappingRecord record) ||
            record == null)
            return false;

        classification = record.classification;

        if (!record.hasSurfaceInputs)
            return false;

        ApplyRecordToMaterial(record, target);
        return true;
    }

    private static void DiagnoseInternal(bool showDialog)
    {
        MappingReport report = new MappingReport();

        report.SourcePresent =
            AssetDatabase.LoadAssetAtPath<TextAsset>(SourceMappingPath) != null;
        report.SourceHashOk =
            report.SourcePresent && FileHashMatches(SourceMappingPath, ExpectedSourceSha256);

        MappingManifest manifest = null;
        try
        {
            manifest = LoadManifest(true);
            report.CompiledPresent = manifest != null;
            report.CompiledHashOk =
                manifest != null &&
                string.Equals(
                    manifest.sourceSha256,
                    ExpectedSourceSha256,
                    StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            report.Problems.Add("Compiled mapping: " + exception.Message);
        }

        if (!report.SourcePresent)
            report.Problems.Add("Authoritative material_mapping.json missing.");
        else if (!report.SourceHashOk)
            report.Problems.Add("Authoritative material_mapping.json SHA256 mismatch.");

        if (manifest != null && manifest.materials != null)
        {
            report.Records = manifest.materials.Length;

            foreach (MappingRecord record in manifest.materials)
            {
                if (record == null)
                    continue;

                switch (record.classification)
                {
                    case "DIRECT_URP":
                        report.DirectUrp++;
                        break;
                    case "REBUILD_URP":
                        report.RebuildUrp++;
                        break;
                    case "CUSTOM_SHADER_REQUIRED":
                        report.CustomShaderRequired++;
                        break;
                    default:
                        report.Problems.Add(
                            "Unknown classification: " + record.material +
                            " -> " + record.classification);
                        break;
                }

                if (!record.hasSurfaceInputs)
                {
                    // Current source omits Atmosphere_BelowRoutes / MAT_Atelier_Haze.
                    if (string.Equals(record.material, "MAT_Atelier_Haze", StringComparison.Ordinal))
                        report.MappingOnlyAbsentRecords++;
                    else
                        report.Problems.Add("No surface inputs: " + record.material);
                    continue;
                }

                report.SurfaceRecords++;

                foreach (string textureName in record.textureNames ?? Array.Empty<string>())
                {
                    report.TextureRefsExpected++;
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        PaintingFolder + "/" + textureName);
                    if (texture != null)
                        report.TextureRefsFound++;
                    else
                        report.Problems.Add(
                            "Mapped texture missing: " + record.material +
                            " -> " + textureName);
                }

                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    MaterialPath(record.material));

                if (material == null)
                {
                    report.Problems.Add("Generated material missing: " + record.material);
                    continue;
                }

                string reason;
                if (ValidateMaterial(record, material, out reason))
                    report.MaterialsValid++;
                else
                    report.Problems.Add(record.material + ": " + reason);
            }
        }

        Transform visual = FindPalimpsestVisualRoot();
        if (visual != null && manifest != null)
        {
            if (cachedByName == null)
                BuildLookup(manifest);

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            report.VisualRenderers = renderers.Length;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                bool covered = true;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    string sourceName = ResolveGeneratedSourceName(material);
                    if (sourceName == null ||
                        !cachedByName.ContainsKey(sourceName))
                    {
                        covered = false;
                        break;
                    }
                }

                if (covered)
                    report.VisualRenderersCovered++;
            }

            if (report.VisualRenderersCovered != report.VisualRenderers)
                report.Problems.Add(
                    $"Renderer material coverage {report.VisualRenderersCovered}/" +
                    $"{report.VisualRenderers}.");
        }

        string result = report.Pass ? "PASS" : "NEEDS_ATTENTION";
        string details =
            "Painted Alive M55.9.3 — Palimpsest Full Material Mapping\n" +
            "Result=" + result + "\n" +
            $"AuthoritativeMapping={(report.SourcePresent ? "PRESENT" : "MISSING")}\n" +
            $"AuthoritativeSHA={(report.SourceHashOk ? "OK" : "MISMATCH")}\n" +
            $"CompiledMapping={(report.CompiledPresent ? "PRESENT" : "MISSING")}\n" +
            $"CompiledSourceSHA={(report.CompiledHashOk ? "OK" : "MISMATCH")}\n" +
            $"MaterialRecords={report.Records}/30\n" +
            $"DIRECT_URP={report.DirectUrp}/9\n" +
            $"REBUILD_URP={report.RebuildUrp}/20\n" +
            $"CUSTOM_SHADER_REQUIRED={report.CustomShaderRequired}/1\n" +
            $"SurfaceMaterials={report.MaterialsValid}/{report.SurfaceRecords}\n" +
            $"MappedTextureRefs={report.TextureRefsFound}/{report.TextureRefsExpected}\n" +
            $"MappingOnlyAbsentRecords={report.MappingOnlyAbsentRecords}/1\n" +
            $"VisualRendererCoverage={report.VisualRenderersCovered}/{report.VisualRenderers}\n" +
            "CustomShaderFallback=MAT_Canvas_Wet -> URP/Lit temporary baseline\n" +
            "VolumeSourceAbsent=MAT_Atelier_Haze / Atmosphere_BelowRoutes\n";

        if (report.Problems.Count > 0)
            details += "\nProblems:\n- " + string.Join("\n- ", report.Problems);

        GameObject root = FindSceneObjectByName(PalimpsestRootName);
        Debug.Log("[M55.9.3 Full Material Mapping]\n" + details, root);

        if (showDialog)
            EditorUtility.DisplayDialog(
                "M55.9.3 Palimpsest Full Material Mapping",
                details,
                "Tamam");
    }

    private static MappingManifest LoadManifest(bool forceReload)
    {
        if (!forceReload && cachedManifest != null)
            return cachedManifest;

        TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(SourceMappingPath);
        if (source == null)
            throw new InvalidOperationException(
                "Authoritative material_mapping.json bulunamadı.");

        if (!FileHashMatches(SourceMappingPath, ExpectedSourceSha256))
            throw new InvalidOperationException(
                "Authoritative material_mapping.json SHA256 beklenen dosyayla eşleşmiyor.");

        TextAsset compiled = AssetDatabase.LoadAssetAtPath<TextAsset>(CompiledMappingPath);
        if (compiled == null)
            throw new InvalidOperationException(
                "material_mapping_unity.json bulunamadı.");

        MappingManifest manifest = JsonUtility.FromJson<MappingManifest>(compiled.text);
        if (manifest == null ||
            manifest.materials == null ||
            manifest.materials.Length != 30 ||
            manifest.materialCount != 30 ||
            !string.Equals(
                manifest.schema,
                "painted_alive.palimpsest.material_mapping_unity.v1",
                StringComparison.Ordinal) ||
            !string.Equals(
                manifest.sourceSha256,
                ExpectedSourceSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Compiled material mapping source/schema/count doğrulaması başarısız.");
        }

        cachedManifest = manifest;
        BuildLookup(manifest);
        return manifest;
    }

    private static void BuildLookup(MappingManifest manifest)
    {
        cachedByName = new Dictionary<string, MappingRecord>(StringComparer.Ordinal);
        foreach (MappingRecord record in manifest.materials)
        {
            if (record != null && !string.IsNullOrEmpty(record.material))
                cachedByName[record.material] = record;
        }
    }

    private static Material LoadOrCreateGeneratedMaterial(string sourceName, Shader shader)
    {
        string path = MaterialPath(sourceName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            material.name = sourceName;
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        return material;
    }

    private static void ApplyRecordToMaterial(MappingRecord record, Material material)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
            throw new InvalidOperationException(
                "Universal Render Pipeline/Lit shader bulunamadı.");

        Undo.RecordObject(material, "Apply Palimpsest material mapping");
        material.shader = lit;

        Texture2D mappedTexture = null;
        if (record.textureNames != null && record.textureNames.Length > 0)
        {
            // Palimpsest mapping only references the four supplied original paintings.
            mappedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                PaintingFolder + "/" + record.textureNames[0]);

            if (mappedTexture == null)
                throw new InvalidOperationException(
                    "Mapped texture missing: " + record.material +
                    " -> " + record.textureNames[0]);
        }

        // When Blender's Base Color input is texture-driven, the serialized socket
        // default is not the rendered color. White preserves the original painting.
        Color baseColor = mappedTexture != null
            ? Color.white
            : ColorFrom(record.baseColor, Color.white);

        SetColor(material, "_BaseColor", baseColor);
        SetColor(material, "_Color", baseColor);
        SetTexture(material, "_BaseMap", mappedTexture);
        SetTexture(material, "_MainTex", mappedTexture);

        SetFloat(material, "_Metallic", Mathf.Clamp01(record.metallic));
        SetFloat(material, "_Smoothness", Mathf.Clamp01(1f - record.roughness));

        Color emissionBase = ColorFrom(
            record.emissionColor,
            Color.black);

        if (record.emissionStrength > 0f)
        {
            Color hdrEmission = emissionBase * record.emissionStrength;
            hdrEmission.a = 1f;
            SetColor(material, "_EmissionColor", hdrEmission);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else
        {
            SetColor(material, "_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.None;
        }

        // All authoritative surface records currently have Alpha=1.
        // Keep them opaque; do not infer transparency from Blender node names.
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);

        string[] existingLabels = AssetDatabase.GetLabels(material)
            .Where(label => !label.StartsWith("PA_Palimpsest_", StringComparison.Ordinal))
            .ToArray();

        List<string> labels = new List<string>(existingLabels)
        {
            "PA_Palimpsest_" + record.classification
        };

        if (string.Equals(
            record.classification,
            "CUSTOM_SHADER_REQUIRED",
            StringComparison.Ordinal))
        {
            labels.Add("PA_Palimpsest_TemporaryShaderFallback");
        }

        AssetDatabase.SetLabels(material, labels.Distinct().ToArray());
        EditorUtility.SetDirty(material);
    }

    private static bool ValidateMaterial(
        MappingRecord record,
        Material material,
        out string reason)
    {
        reason = null;

        if (material.shader == null ||
            !string.Equals(
                material.shader.name,
                "Universal Render Pipeline/Lit",
                StringComparison.Ordinal))
        {
            reason = "Shader is not URP/Lit.";
            return false;
        }

        Texture expectedTexture = null;
        if (record.textureNames != null && record.textureNames.Length > 0)
        {
            expectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                PaintingFolder + "/" + record.textureNames[0]);
        }

        Texture actualTexture = material.HasProperty("_BaseMap")
            ? material.GetTexture("_BaseMap")
            : null;

        if (actualTexture != expectedTexture)
        {
            reason =
                "Base Map mismatch. Expected=" +
                (expectedTexture != null ? expectedTexture.name : "null") +
                ", Actual=" +
                (actualTexture != null ? actualTexture.name : "null");
            return false;
        }

        Color expectedBase = expectedTexture != null
            ? Color.white
            : ColorFrom(record.baseColor, Color.white);
        Color actualBase = material.HasProperty("_BaseColor")
            ? material.GetColor("_BaseColor")
            : Color.white;

        if (!Approximately(expectedBase, actualBase, 0.01f))
        {
            reason = "Base Color mismatch.";
            return false;
        }

        float metallic = material.HasProperty("_Metallic")
            ? material.GetFloat("_Metallic")
            : 0f;
        float smoothness = material.HasProperty("_Smoothness")
            ? material.GetFloat("_Smoothness")
            : 0f;

        if (Mathf.Abs(metallic - Mathf.Clamp01(record.metallic)) > 0.01f)
        {
            reason = "Metallic mismatch.";
            return false;
        }

        if (Mathf.Abs(
            smoothness - Mathf.Clamp01(1f - record.roughness)) > 0.01f)
        {
            reason = "Smoothness mismatch.";
            return false;
        }

        if (record.emissionStrength > 0f)
        {
            if (!material.IsKeywordEnabled("_EMISSION"))
            {
                reason = "Emission keyword missing.";
                return false;
            }

            Color expectedEmission =
                ColorFrom(record.emissionColor, Color.black) *
                record.emissionStrength;
            expectedEmission.a = 1f;

            Color actualEmission = material.HasProperty("_EmissionColor")
                ? material.GetColor("_EmissionColor")
                : Color.black;

            if (!Approximately(expectedEmission, actualEmission, 0.02f))
            {
                reason = "Emission mismatch.";
                return false;
            }
        }

        return true;
    }

    private static string ResolveGeneratedSourceName(Material material)
    {
        if (material == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(material);
        string file = Path.GetFileNameWithoutExtension(assetPath);
        const string prefix = "MAT_PAL_";

        if (!string.IsNullOrEmpty(file) &&
            file.StartsWith(prefix, StringComparison.Ordinal))
            return file.Substring(prefix.Length);

        return null;
    }

    private static Transform FindPalimpsestVisualRoot()
    {
        GameObject root = FindSceneObjectByName(PalimpsestRootName);
        if (root == null)
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        return all.FirstOrDefault(
            t => t != null &&
                 string.Equals(t.name, VisualRootName, StringComparison.Ordinal));
    }

    private static GameObject FindSceneObjectByName(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in all)
            {
                if (transform != null &&
                    string.Equals(transform.name, name, StringComparison.Ordinal))
                    return transform.gameObject;
            }
        }

        return null;
    }

    private static string MaterialPath(string sourceName)
    {
        return MaterialFolder + "/MAT_PAL_" +
               SanitizeFileName(sourceName) + ".mat";
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name;
    }

    private static Color ColorFrom(float[] values, Color fallback)
    {
        if (values == null || values.Length < 3)
            return fallback;

        return new Color(
            values[0],
            values[1],
            values[2],
            values.Length > 3 ? values[3] : 1f);
    }

    private static bool Approximately(Color a, Color b, float tolerance)
    {
        return Mathf.Abs(a.r - b.r) <= tolerance &&
               Mathf.Abs(a.g - b.g) <= tolerance &&
               Mathf.Abs(a.b - b.b) <= tolerance &&
               Mathf.Abs(a.a - b.a) <= tolerance;
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
            material.SetColor(property, value);
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private static void SetTexture(Material material, string property, Texture value)
    {
        if (material.HasProperty(property))
            material.SetTexture(property, value);
    }

    private static bool FileHashMatches(
        string assetPath,
        string expectedSha256)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
            return false;

        using (var stream = File.OpenRead(fullPath))
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(stream);
            string actual = BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLowerInvariant();

            return string.Equals(
                actual,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "M55.9.3 material mapping Play Mode kapalıyken uygulanmalıdır.");
    }
}
#endif
