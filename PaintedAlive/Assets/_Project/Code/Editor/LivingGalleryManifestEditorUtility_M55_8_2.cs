#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LivingGalleryManifestEditorUtility_M55_8_2
{
    public const string ManifestPath = "Assets/_Project/Data/Environment/LivingGallery/UNITY_BINDINGS_LIVING_GALLERY.json";
    public const string TextureFolder = "Assets/_Project/Art/Textures/Environment/LivingGallery";
    public const string MaterialFolder = "Assets/_Project/Materials/Environment/LivingGallery/Generated";

    [Serializable] public sealed class Counts { public int binding_records; public int scene_bound_records; public int unconfigured_system_records; public int collision_total; public int collision_active; public int collision_retired; public int materials; public int texture_files; }
    [Serializable] public sealed class BindingRecord { public string id; public string family; public string unity_system; public string[] objects; public string[] helpers; public string[] colliders; public string[] materials; public string purpose; public string status; }
    [Serializable] public sealed class GapRecord { public string id; public string[] affected; public string missing; public string action; }
    [Serializable] public sealed class CollisionRecord { public string name; public string group; public bool active; public string visual_source; public string parent; public string authored_role; public string authored_unity_collider; public string recommended_layer; }
    [Serializable] public sealed class Manifest { public string schema; public string source_sha256; public Counts counts; public string[] families; public BindingRecord[] binding_records; public GapRecord[] gaps; public CollisionRecord[] collision_records; public string[] retired_colliders; }

    public struct Report
    {
        public bool ManifestLoaded;
        public bool SchemaValid;
        public int BindingRecords;
        public int SceneBoundRecordsResolved;
        public int SceneBoundRecordsExpected;
        public int MissingBindingReferences;
        public int DuplicateBindingReferences;
        public int CollisionRecords;
        public int ActiveCollidersConfigured;
        public int RetiredCollidersDisabled;
        public int TextureFilesFound;
        public int TextureFilesExpected;
        public int TexturedMaterialsConfigured;
        public int AuthoredGaps;
        public int HardErrors;
        public string Details;
        public bool Pass => ManifestLoaded && SchemaValid && HardErrors == 0 && MissingBindingReferences == 0 && DuplicateBindingReferences == 0 && ActiveCollidersConfigured == 436 && RetiredCollidersDisabled == 1 && TextureFilesFound == TextureFilesExpected;
    }

    private static readonly Dictionary<string,string> TextureStem = new Dictionary<string,string>(StringComparer.Ordinal)
    {
        {"MAT_Brass_Aged","Brass_Aged"},{"MAT_Canvas_Raw","Canvas_Raw"},{"MAT_Canvas_Stained","Canvas_Stained"},
        {"MAT_Ink_Black","Ink_Black"},{"MAT_Iron_Dark","Iron_Dark"},{"MAT_Paper_Dry","Paper_Dry"},{"MAT_Paper_Wet","Paper_Wet"},
        {"MAT_Pigment_Blue","Pigment_Blue"},{"MAT_Pigment_Ochre","Pigment_Ochre"},{"MAT_Pigment_Violet","Pigment_Violet"},
        {"MAT_Plaster_Aged","Plaster_Aged"},{"MAT_Plaster_Dirty","Plaster_Dirty"},{"MAT_Stone_Gallery","Stone_Gallery"},
        {"MAT_Timber_Dark","Timber_Dark"},{"MAT_Timber_Worn","Timber_Worn"}
    };

    [MenuItem("Tools/Painted Alive/Milestones/55.8.2 - Apply Living Gallery Manifest + Textures")]
    public static void ApplyMenu()
    {
        GameObject root = FindSceneObject("M55_8_LivingGallery_Continuation");
        if (root == null) { EditorUtility.DisplayDialog("M55.8.2", "Living Gallery root bulunamadı. Önce M55.8 setup çalıştır.", "Tamam"); return; }
        Transform visual = FindExact(root.transform, "The_Living_Gallery_Visual", false);
        Transform collision = FindExact(root.transform, "The_Living_Gallery_Collision", false);
        Transform gameplay = FindExact(root.transform, "The_Living_Gallery_Gameplay", false);
        Report r = Apply(visual, collision, gameplay, true);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("M55.8.2 Manifest + Textures", Format(r), "Tamam");
    }

    [MenuItem("Tools/Painted Alive/Milestones/55.8.2 - Diagnose Living Gallery Manifest + Textures")]
    public static void DiagnoseMenu()
    {
        GameObject root = FindSceneObject("M55_8_LivingGallery_Continuation");
        Transform visual = root != null ? FindExact(root.transform, "The_Living_Gallery_Visual", false) : null;
        Transform collision = root != null ? FindExact(root.transform, "The_Living_Gallery_Collision", false) : null;
        Transform gameplay = root != null ? FindExact(root.transform, "The_Living_Gallery_Gameplay", false) : null;
        Report r = Apply(visual, collision, gameplay, false);
        Debug.Log("[M55.8.2 Manifest]\n" + Format(r), root);
        EditorUtility.DisplayDialog("M55.8.2 Diagnose", Format(r), "Tamam");
    }

    public static Report Apply(Transform visual, Transform collision, Transform gameplay, bool mutate)
    {
        Report report = new Report();
        TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
        if (text == null) { report.HardErrors++; report.Details = "Manifest TextAsset bulunamadı: " + ManifestPath; return report; }
        report.ManifestLoaded = true;
        Manifest manifest;
        try { manifest = JsonUtility.FromJson<Manifest>(text.text); }
        catch (Exception e) { report.HardErrors++; report.Details = "Manifest parse error: " + e.Message; return report; }
        if (manifest == null) { report.HardErrors++; report.Details = "Manifest parse sonucu null."; return report; }
        report.SchemaValid = string.Equals(manifest.schema, "painted_alive.living_gallery.unity_handoff.v1", StringComparison.Ordinal);
        if (!report.SchemaValid) report.HardErrors++;
        report.BindingRecords = manifest.binding_records != null ? manifest.binding_records.Length : 0;
        report.SceneBoundRecordsExpected = manifest.counts != null ? manifest.counts.scene_bound_records : 33;
        report.CollisionRecords = manifest.collision_records != null ? manifest.collision_records.Length : 0;
        report.AuthoredGaps = manifest.gaps != null ? manifest.gaps.Length : 0;
        report.TextureFilesExpected = manifest.counts != null ? manifest.counts.texture_files : 45;
        report.TextureFilesFound = CountTextureFiles();

        if (visual == null || collision == null || gameplay == null) { report.HardErrors++; report.Details = "Living Gallery Visual/Collision/Gameplay rootlarından biri eksik."; return report; }

        if (mutate)
        {
            ConfigureTextures();
            ConfigureMaterials(visual, ref report);
            ConfigureCollision(collision, manifest, ref report);
        }
        else
        {
            ValidateCollision(collision, manifest, ref report);
            ValidateMaterials(visual, ref report);
        }
        ValidateBindings(visual, collision, gameplay, manifest, ref report);
        report.Details = BuildGapSummary(manifest);
        return report;
    }

    private static void ConfigureCollision(Transform root, Manifest manifest, ref Report report)
    {
        if (manifest.collision_records == null) { report.HardErrors++; return; }
        int collisionLayer = LayerMask.NameToLayer("PrismaticCollision");
        int trampolineLayer = LayerMask.NameToLayer("Trampoline");
        foreach (CollisionRecord rec in manifest.collision_records)
        {
            Transform t = FindExact(root, rec.name, false);

            // Retired collision records are authoritative metadata. The Living Gallery
            // source FBX intentionally omits the single retired proxy, which is safer
            // than importing it and disabling it. Therefore "absent" and
            // "present-but-disabled" are both valid retired states.
            if (!rec.active)
            {
                if (t == null)
                {
                    report.RetiredCollidersDisabled++;
                    continue;
                }

                foreach (Renderer rr in t.GetComponents<Renderer>())
                {
                    rr.enabled = false;
                    EditorUtility.SetDirty(rr);
                }

                foreach (Collider c in t.GetComponents<Collider>())
                    c.enabled = false;

                t.gameObject.SetActive(false);
                EditorUtility.SetDirty(t.gameObject);
                report.RetiredCollidersDisabled++;
                continue;
            }

            if (t == null)
            {
                report.HardErrors++;
                continue;
            }

            foreach (Renderer rr in t.GetComponents<Renderer>()) { rr.enabled = false; EditorUtility.SetDirty(rr); }
            Collider[] existing = t.GetComponents<Collider>();
            t.gameObject.SetActive(true);
            int layer = string.Equals(rec.recommended_layer, "Trampoline", StringComparison.OrdinalIgnoreCase) ? trampolineLayer : collisionLayer;
            if (layer >= 0) t.gameObject.layer = layer;
            string kind = rec.authored_unity_collider ?? "StaticMeshCollider";
            bool wantsBox = kind.IndexOf("BoxCollider", StringComparison.OrdinalIgnoreCase) >= 0;
            if (wantsBox)
            {
                foreach (MeshCollider c in t.GetComponents<MeshCollider>()) Undo.DestroyObjectImmediate(c);
                BoxCollider box = t.GetComponent<BoxCollider>(); if (box == null) box = Undo.AddComponent<BoxCollider>(t.gameObject);
                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) { box.center = mf.sharedMesh.bounds.center; box.size = mf.sharedMesh.bounds.size; }
                box.isTrigger = false; box.enabled = true; EditorUtility.SetDirty(box);
            }
            else
            {
                foreach (BoxCollider c in t.GetComponents<BoxCollider>()) Undo.DestroyObjectImmediate(c);
                MeshFilter mf = t.GetComponent<MeshFilter>();
                MeshCollider mc = t.GetComponent<MeshCollider>(); if (mc == null) mc = Undo.AddComponent<MeshCollider>(t.gameObject);
                if (mf != null) mc.sharedMesh = mf.sharedMesh;
                mc.isTrigger = false; mc.enabled = true;
                mc.convex = kind.IndexOf("kinematic gate", StringComparison.OrdinalIgnoreCase) >= 0 || rec.group == "MOVING FRAME COLLISION";
                EditorUtility.SetDirty(mc);
            }
            report.ActiveCollidersConfigured++;
        }
    }

    private static void ValidateCollision(Transform root, Manifest manifest, ref Report report)
    {
        if (manifest.collision_records == null) { report.HardErrors++; return; }
        foreach (CollisionRecord rec in manifest.collision_records)
        {
            Transform t = FindExact(root, rec.name, false);

            // The retired proxy may be completely absent from the exported Collision
            // FBX. That is an explicitly safe state and must satisfy the manifest.
            if (!rec.active)
            {
                if (t == null)
                {
                    report.RetiredCollidersDisabled++;
                    continue;
                }

                bool safe = !t.gameObject.activeSelf ||
                            t.GetComponents<Collider>().All(c => !c.enabled);

                if (safe)
                    report.RetiredCollidersDisabled++;
                else
                    report.HardErrors++;

                continue;
            }

            if (t == null)
            {
                report.HardErrors++;
                continue;
            }

            if (t.GetComponents<Collider>().Any(c => c.enabled))
                report.ActiveCollidersConfigured++;
            else
                report.HardErrors++;
        }
    }

    private static void ConfigureTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[]{TextureFolder});
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
            string n = System.IO.Path.GetFileName(path);
            bool changed = false;
            if (n.EndsWith("_Normal.png", StringComparison.OrdinalIgnoreCase))
            {
                if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
                if (ti.sRGBTexture) { ti.sRGBTexture = false; changed = true; }
            }
            else if (n.EndsWith("_Mask.png", StringComparison.OrdinalIgnoreCase))
            {
                if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; changed = true; }
                if (ti.sRGBTexture) { ti.sRGBTexture = false; changed = true; }
            }
            else if (n.EndsWith("_BaseColor.png", StringComparison.OrdinalIgnoreCase))
            {
                if (!ti.sRGBTexture) { ti.sRGBTexture = true; changed = true; }
            }
            if (changed) ti.SaveAndReimport();
        }
    }

    private static void ConfigureMaterials(Transform visual, ref Report report)
    {
        EnsureFolder(MaterialFolder);
        Dictionary<string,Material> cache = new Dictionary<string,Material>(StringComparer.Ordinal);
        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            Material[] mats = r.sharedMaterials; bool changed = false;
            for (int i=0;i<mats.Length;i++)
            {
                string sourceName = NormalizeSourceMaterialName(mats[i] != null ? mats[i].name : null);
                if (string.IsNullOrEmpty(sourceName) || !TextureStem.ContainsKey(sourceName)) continue;
                if (!cache.TryGetValue(sourceName, out Material m)) { m = BuildMaterial(sourceName); cache[sourceName]=m; }
                if (m != null && mats[i] != m) { mats[i]=m; changed=true; }
            }
            if (changed) { r.sharedMaterials=mats; EditorUtility.SetDirty(r); }
        }
        report.TexturedMaterialsConfigured = cache.Count(kv => TextureStem.ContainsKey(kv.Key) && kv.Value != null && kv.Value.GetTexture("_BaseMap") != null);
    }

    private static void ValidateMaterials(Transform visual, ref Report report)
    {
        HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            foreach (Material m in r.sharedMaterials)
            {
                string n = NormalizeSourceMaterialName(m != null ? m.name : null);
                if (TextureStem.ContainsKey(n) && m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) found.Add(n);
            }
        report.TexturedMaterialsConfigured = found.Count;
        if (found.Count < TextureStem.Count) report.HardErrors++;
    }

    private static Material BuildMaterial(string sourceName)
    {
        string safe = Sanitize(sourceName); string path = MaterialFolder + "/MAT_LG_" + safe + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
        if (m == null) { if (shader == null) return null; m = new Material(shader); AssetDatabase.CreateAsset(m,path); }
        else if (shader != null) m.shader=shader;
        if (TextureStem.TryGetValue(sourceName, out string stem))
        {
            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder+"/"+stem+"_BaseColor.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder+"/"+stem+"_Normal.png");
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder+"/"+stem+"_Mask.png");
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", baseTex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", normal);
            if (normal != null) m.EnableKeyword("_NORMALMAP");
            if (m.HasProperty("_MetallicGlossMap")) m.SetTexture("_MetallicGlossMap", mask);
            if (mask != null) m.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 1f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 1f);
            SetNormalScale(m, sourceName);
        }
        else ApplyConstantMaterial(m, sourceName);
        EditorUtility.SetDirty(m); return m;
    }

    private static void SetNormalScale(Material m, string n)
    {
        if (!m.HasProperty("_BumpScale")) return;
        float v = (n=="MAT_Canvas_Raw" || n=="MAT_Canvas_Stained") ? 0.24f : 0.16f; m.SetFloat("_BumpScale",v);
    }
    private static void ApplyConstantMaterial(Material m,string n)
    {
        Color c=Color.gray; float met=0f, smooth=.35f;
        if(n=="Figure | graphite"){c=new Color(.034f,.041f,.048f,1);smooth=.48f;}
        else if(n=="Ink | reflection etching"){c=new Color(.055f,.066f,.072f,1);met=.5f;smooth=.68f;}
        else if(n=="Light | warm frosted glass"){c=new Color(.9f,.51f,.18f,1);smooth=.6f;if(m.HasProperty("_EmissionColor")){m.SetColor("_EmissionColor",new Color(1f,.57f,.24f,1)*3f);m.EnableKeyword("_EMISSION");}}
        else if(n=="MAT_Ivy_Muted"){c=new Color(.067f,.085f,.041f,1);smooth=.14f;}
        else if(n=="Stage | charcoal"){c=new Color(.025f,.032f,.041f,1);smooth=.05f;}
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c); if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",met); if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);
    }

    private static void ValidateBindings(Transform visual, Transform collision, Transform gameplay, Manifest manifest, ref Report report)
    {
        if (manifest.binding_records == null) { report.HardErrors++; return; }
        int sceneResolved=0;
        foreach (BindingRecord br in manifest.binding_records)
        {
            if (br == null || br.id == "FALL_RESPAWN_UNCONFIGURED") continue;
            bool ok=true;
            ok &= ValidateArray(br.objects, visual, ref report);
            ok &= ValidateArray(br.helpers, gameplay, ref report);
            ok &= ValidateArray(br.colliders, collision, ref report);
            if (ok) sceneResolved++;
        }
        report.SceneBoundRecordsResolved=sceneResolved;
        if(sceneResolved != report.SceneBoundRecordsExpected) report.HardErrors++;
    }
    private static bool ValidateArray(string[] names, Transform root, ref Report report)
    {
        if(names==null||names.Length==0)return true; bool ok=true;
        foreach(string n in names){ int c=CountExact(root,n); if(c==0){report.MissingBindingReferences++;ok=false;} else if(c>1){report.DuplicateBindingReferences++;ok=false;} }
        return ok;
    }
    private static int CountExact(Transform root,string n){ if(root==null)return 0; return root.GetComponentsInChildren<Transform>(true).Count(t=>t!=null&&t.name==n); }
    private static int CountTextureFiles(){ return AssetDatabase.FindAssets("t:Texture2D",new[]{TextureFolder}).Length; }
    private static string BuildGapSummary(Manifest m){ if(m.gaps==null||m.gaps.Length==0)return "Authored gap yok."; return "Authored gaps (tasarım verisi eksik; integration error değildir):\n- "+string.Join("\n- ",m.gaps.Select(g=>g.id+": "+g.missing).ToArray()); }
    public static string Format(Report r){ return "Result="+(r.Pass?"PASS_WITH_AUTHORED_GAPS":"NEEDS_ATTENTION")+"\nManifest="+(r.ManifestLoaded?"OK":"MISSING")+"\nSchema="+(r.SchemaValid?"OK":"INVALID")+"\nBindingRecords="+r.BindingRecords+"\nSceneBoundResolved="+r.SceneBoundRecordsResolved+"/"+r.SceneBoundRecordsExpected+"\nMissingRefs="+r.MissingBindingReferences+"\nDuplicateRefs="+r.DuplicateBindingReferences+"\nCollisionRecords="+r.CollisionRecords+"\nActiveColliders="+r.ActiveCollidersConfigured+"/436\nRetiredDisabledOrAbsent="+r.RetiredCollidersDisabled+"/1\nTextureFiles="+r.TextureFilesFound+"/"+r.TextureFilesExpected+"\nTexturedMaterials="+r.TexturedMaterialsConfigured+"/15\nAuthoredGaps="+r.AuthoredGaps+"\nHardErrors="+r.HardErrors+"\n\n"+r.Details; }

    private static string NormalizeSourceMaterialName(string n){ if(string.IsNullOrEmpty(n))return n; n=n.Replace(" (Instance)",""); if(n.StartsWith("MAT_LG_",StringComparison.Ordinal))n=n.Substring(7); return n; }
    private static string Sanitize(string s){ foreach(char c in System.IO.Path.GetInvalidFileNameChars())s=s.Replace(c,'_'); return s.Replace('|','_').Replace(' ','_'); }
    private static void EnsureFolder(string path){ string[] p=path.Split('/'); string cur=p[0]; for(int i=1;i<p.Length;i++){ string next=cur+"/"+p[i]; if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(cur,p[i]); cur=next; } }
    private static Transform FindExact(Transform root,string n,bool throwDup){ if(root==null)return null; var a=root.GetComponentsInChildren<Transform>(true).Where(t=>t!=null&&t.name==n).ToArray(); if(a.Length>1&&throwDup)throw new InvalidOperationException("Duplicate: "+n); return a.Length>0?a[0]:null; }
    private static GameObject FindSceneObject(string n){ foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(g!=null&&g.scene.IsValid()&&!EditorUtility.IsPersistent(g)&&g.name==n)return g; return null; }
}
#endif
