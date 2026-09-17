#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApplyPalimpsestPaintings_M55_9_2
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";
    private const string RootName = "M55_9_Palimpsest_Continuation";
    private const string VisualRootName = "The_Palimpsest_Visual";
    private const string PaintingFolder =
        "Assets/_Project/Art/Textures/Environments/Palimpsest/Paintings";
    private const string MaterialFolder =
        "Assets/_Project/Materials/Environment/Palimpsest/Generated";
    private const string PaintingManifestPath =
        "Assets/_Project/Data/Environment/Palimpsest/PAINTING_BINDINGS_PALIMPSEST.json";
    private const string VisualModelPath =
        "Assets/_Project/Art/Models/Environments/Palimpsest/The_Palimpsest_Visual.fbx";
    private const string ExpectedVisualSha256 =
        "c0affa94afd7d89b93915480c5dad8305e1ecf9d7d08a9e24112dd87a0c98b4d";

    [Serializable]
    private sealed class PaintingManifest
    {
        public string schema;
        public string map;
        public PaintingBinding[] bindings;
    }

    [Serializable]
    private sealed class PaintingBinding
    {
        public string texture;
        public string[] materials;
        public string[] objects;
    }

    private sealed class Diagnostic
    {
        public int TexturesFound;
        public int MaterialsBound;
        public int MaterialsExpected;
        public int ObjectsCovered;
        public int ObjectsExpected;
        public bool ManifestOk;
        public bool FixedVisualHashOk;
        public readonly List<string> Problems = new List<string>();

        public bool Pass =>
            ManifestOk &&
            FixedVisualHashOk &&
            TexturesFound == 4 &&
            MaterialsBound == MaterialsExpected &&
            ObjectsCovered == ObjectsExpected &&
            Problems.Count == 0;
    }

    [MenuItem(MenuRoot + "55.9.2 - Apply Palimpsest Paintings")]
    public static void ApplyMenu()
    {
        RequireEditMode();

        PaintingManifest manifest = LoadManifest();
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
            throw new InvalidOperationException(
                "Universal Render Pipeline/Lit shader bulunamadı. Palimpsest painting binding durduruldu.");

        EnsureAssetFolder(MaterialFolder);

        foreach (PaintingBinding binding in manifest.bindings)
        {
            string texturePath = PaintingFolder + "/" + binding.texture;
            ConfigurePaintingTexture(texturePath);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new InvalidOperationException("Painting texture bulunamadı: " + texturePath);

            foreach (string materialName in binding.materials)
            {
                Material material = LoadOrCreateGeneratedMaterial(materialName, lit);
                ApplyBaseMap(material, texture);
            }
        }

        GameObject root = FindSceneObjectByName(RootName);
        if (root != null)
        {
            Transform visual = FindUnique(root.transform, VisualRootName, false);
            if (visual != null)
                RebindListedObjects(visual, manifest);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (SceneManager.GetActiveScene().IsValid())
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        DiagnoseInternal(true);
    }

    [MenuItem(MenuRoot + "55.9.2 - Diagnose Palimpsest Paintings")]
    public static void DiagnoseMenu()
    {
        DiagnoseInternal(true);
    }

    private static void DiagnoseInternal(bool showDialog)
    {
        PaintingManifest manifest = null;
        Diagnostic diagnostic = new Diagnostic();

        try
        {
            manifest = LoadManifest();
            diagnostic.ManifestOk = true;
        }
        catch (Exception exception)
        {
            diagnostic.Problems.Add("Painting manifest: " + exception.Message);
        }

        diagnostic.FixedVisualHashOk = FileHashMatches(
            VisualModelPath,
            ExpectedVisualSha256);

        if (!diagnostic.FixedVisualHashOk)
            diagnostic.Problems.Add(
                "The_Palimpsest_Visual.fbx hash beklenen M55.9.2 fixed visual ile eşleşmiyor.");

        GameObject root = FindSceneObjectByName(RootName);
        Transform visual = root != null ? FindUnique(root.transform, VisualRootName, false) : null;

        if (manifest != null && manifest.bindings != null)
        {
            foreach (PaintingBinding binding in manifest.bindings)
            {
                string texturePath = PaintingFolder + "/" + binding.texture;
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture != null)
                    diagnostic.TexturesFound++;
                else
                    diagnostic.Problems.Add("Texture missing: " + binding.texture);

                foreach (string materialName in binding.materials ?? Array.Empty<string>())
                {
                    diagnostic.MaterialsExpected++;
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(
                        MaterialPath(materialName));

                    if (material == null)
                    {
                        diagnostic.Problems.Add("Generated material missing: " + materialName);
                        continue;
                    }

                    Texture baseMap = material.HasProperty("_BaseMap")
                        ? material.GetTexture("_BaseMap")
                        : null;

                    bool shaderOk =
                        material.shader != null &&
                        string.Equals(
                            material.shader.name,
                            "Universal Render Pipeline/Lit",
                            StringComparison.Ordinal);

                    if (shaderOk && baseMap == texture && IsWhiteBaseColor(material))
                    {
                        diagnostic.MaterialsBound++;
                    }
                    else
                    {
                        diagnostic.Problems.Add(
                            "Material binding incorrect: " + materialName +
                            " (URP/Lit=" + shaderOk +
                            ", BaseMap=" + (baseMap != null ? baseMap.name : "null") + ")");
                    }
                }

                foreach (string objectName in binding.objects ?? Array.Empty<string>())
                {
                    diagnostic.ObjectsExpected++;

                    if (visual == null)
                    {
                        diagnostic.Problems.Add(
                            "Scene visual root missing; object coverage doğrulanamadı: " + objectName);
                        continue;
                    }

                    Transform target = FindUnique(visual, objectName, false);
                    if (target == null)
                    {
                        diagnostic.Problems.Add("Painting object missing in scene: " + objectName);
                        continue;
                    }

                    if (ObjectUsesTexture(target, texture))
                        diagnostic.ObjectsCovered++;
                    else
                        diagnostic.Problems.Add(
                            "Painting object expected texture kullanmıyor: " +
                            objectName + " -> " + binding.texture);
                }
            }
        }

        string result = diagnostic.Pass ? "PASS" : "NEEDS_ATTENTION";
        string report =
            "Painted Alive M55.9.2 — Palimpsest Painting Bindings\n" +
            "Result=" + result + "\n" +
            $"PaintingBindingManifest={(diagnostic.ManifestOk ? "OK" : "FAIL")}\n" +
            $"FixedVisualHash={(diagnostic.FixedVisualHashOk ? "OK" : "MISMATCH")}\n" +
            $"PaintingTextures={diagnostic.TexturesFound}/4\n" +
            $"PaintingMaterials={diagnostic.MaterialsBound}/{diagnostic.MaterialsExpected}\n" +
            $"PaintingObjectCoverage={diagnostic.ObjectsCovered}/{diagnostic.ObjectsExpected}\n" +
            "Shader=Universal Render Pipeline/Lit\n" +
            "Slot=Base Map (_BaseMap)\n" +
            "BaseColor=White\n";

        if (diagnostic.Problems.Count > 0)
            report += "\nProblems:\n- " + string.Join("\n- ", diagnostic.Problems);

        Debug.Log("[M55.9.2 Palimpsest Paintings]\n" + report, root);

        if (showDialog)
            EditorUtility.DisplayDialog("M55.9.2 Palimpsest Paintings", report, "Tamam");
    }

    private static PaintingManifest LoadManifest()
    {
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PaintingManifestPath);
        if (asset == null)
            throw new InvalidOperationException(
                "Painting binding manifest bulunamadı: " + PaintingManifestPath);

        PaintingManifest manifest = JsonUtility.FromJson<PaintingManifest>(asset.text);
        if (manifest == null ||
            manifest.bindings == null ||
            manifest.bindings.Length != 4 ||
            !string.Equals(
                manifest.schema,
                "painted_alive.palimpsest.painting_bindings.v1",
                StringComparison.Ordinal) ||
            !string.Equals(manifest.map, "The Palimpsest", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Painting binding manifest schema/content beklenen kaynakla eşleşmiyor.");
        }

        int materialCount = manifest.bindings.Sum(
            binding => binding.materials != null ? binding.materials.Length : 0);
        int objectCount = manifest.bindings.Sum(
            binding => binding.objects != null ? binding.objects.Length : 0);

        if (materialCount != 11 || objectCount != 25)
            throw new InvalidOperationException(
                $"Painting binding manifest count mismatch. Materials={materialCount}/11, Objects={objectCount}/25.");

        return manifest;
    }

    private static void RebindListedObjects(Transform visual, PaintingManifest manifest)
    {
        foreach (PaintingBinding binding in manifest.bindings)
        {
            HashSet<string> expectedMaterials = new HashSet<string>(
                binding.materials ?? Array.Empty<string>(),
                StringComparer.Ordinal);

            foreach (string objectName in binding.objects ?? Array.Empty<string>())
            {
                Transform target = FindUnique(visual, objectName, false);
                if (target == null)
                    continue;

                Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    Material[] slots = renderer.sharedMaterials;
                    bool changed = false;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Material existing = slots[i];
                        string sourceName = ResolveSourceMaterialName(existing);
                        if (sourceName == null || !expectedMaterials.Contains(sourceName))
                            continue;

                        Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                            MaterialPath(sourceName));
                        if (targetMaterial != null && slots[i] != targetMaterial)
                        {
                            slots[i] = targetMaterial;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        Undo.RecordObject(renderer, "Bind Palimpsest painting material");
                        renderer.sharedMaterials = slots;
                        EditorUtility.SetDirty(renderer);
                    }
                }
            }
        }
    }

    private static string ResolveSourceMaterialName(Material material)
    {
        if (material == null)
            return null;

        string path = AssetDatabase.GetAssetPath(material);
        if (!string.IsNullOrEmpty(path))
        {
            string file = Path.GetFileNameWithoutExtension(path);
            const string prefix = "MAT_PAL_";
            if (file.StartsWith(prefix, StringComparison.Ordinal))
                return file.Substring(prefix.Length);
        }

        string name = material.name;
        if (name.StartsWith("MAT_PAL_", StringComparison.Ordinal))
            name = name.Substring("MAT_PAL_".Length);

        return name;
    }

    private static Material LoadOrCreateGeneratedMaterial(string materialName, Shader lit)
    {
        string path = MaterialPath(materialName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(lit);
            material.name = materialName;
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = lit;
            material.name = materialName;
        }

        return material;
    }

    private static void ApplyBaseMap(Material material, Texture2D texture)
    {
        Undo.RecordObject(material, "Apply Palimpsest painting");

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        // The texture should not be multiplied by the brown fallback tint.
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        EditorUtility.SetDirty(material);
    }

    private static void ConfigurePaintingTexture(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        bool dirty = false;

        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            dirty = true;
        }

        if (!importer.sRGBTexture)
        {
            importer.sRGBTexture = true;
            dirty = true;
        }

        if (!importer.mipmapEnabled)
        {
            importer.mipmapEnabled = true;
            dirty = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            dirty = true;
        }

        if (importer.filterMode != FilterMode.Trilinear)
        {
            importer.filterMode = FilterMode.Trilinear;
            dirty = true;
        }

        if (importer.maxTextureSize < 2048)
        {
            importer.maxTextureSize = 2048;
            dirty = true;
        }

        if (dirty)
            importer.SaveAndReimport();
    }

    private static bool ObjectUsesTexture(Transform target, Texture texture)
    {
        if (target == null || texture == null)
            return false;

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || !material.HasProperty("_BaseMap"))
                    continue;

                if (material.GetTexture("_BaseMap") == texture)
                    return true;
            }
        }

        return false;
    }

    private static bool IsWhiteBaseColor(Material material)
    {
        if (material == null)
            return false;

        Color color = Color.white;
        if (material.HasProperty("_BaseColor"))
            color = material.GetColor("_BaseColor");
        else if (material.HasProperty("_Color"))
            color = material.GetColor("_Color");

        return Mathf.Abs(color.r - 1f) < 0.01f &&
               Mathf.Abs(color.g - 1f) < 0.01f &&
               Mathf.Abs(color.b - 1f) < 0.01f;
    }

    private static bool FileHashMatches(string assetPath, string expectedSha256)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
            return false;

        using (var stream = File.OpenRead(fullPath))
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(stream);
            string actual = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string MaterialPath(string materialName)
    {
        return MaterialFolder + "/MAT_PAL_" + SanitizeFileName(materialName) + ".mat";
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name;
    }

    private static Transform FindUnique(Transform root, string name, bool throwOnDuplicate)
    {
        if (root == null)
            return null;

        Transform[] matches = root.GetComponentsInChildren<Transform>(true)
            .Where(target =>
                target != null &&
                string.Equals(target.name, name, StringComparison.Ordinal))
            .ToArray();

        if (matches.Length == 0)
            return null;

        if (matches.Length > 1 && throwOnDuplicate)
            throw new InvalidOperationException(
                $"Duplicate Palimpsest object '{name}' count={matches.Length}.");

        return matches[0];
    }

    private static GameObject FindSceneObjectByName(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform target in all)
            {
                if (target != null &&
                    string.Equals(target.name, name, StringComparison.Ordinal))
                    return target.gameObject;
            }
        }

        return null;
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
                "M55.9.2 painting binding Play Mode kapalıyken çalıştırılmalıdır.");
    }
}
#endif
