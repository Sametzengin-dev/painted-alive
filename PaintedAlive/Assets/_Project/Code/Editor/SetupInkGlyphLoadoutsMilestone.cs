using System;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using PaintedAlive.Paint.Ink.Lifecycle;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkGlyphLoadoutsMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string InkDataFolder =
            RootFolder + "/Data/Paint/Ink";
        private const string GlyphFolder =
            InkDataFolder + "/Glyphs";
        private const string CreatureFolder =
            InkDataFolder + "/Creatures";
        private const string LoadoutFolder =
            InkDataFolder + "/Loadouts";
        private const string MaterialFolder =
            RootFolder + "/Art/Materials/Ink";
        private const string EyeGlyphPath =
            GlyphFolder + "/InkGlyph_Eye.asset";
        private const string FootGlyphPath =
            GlyphFolder + "/InkGlyph_Foot.asset";
        private const string ShellGlyphPath =
            GlyphFolder + "/InkGlyph_Shell.asset";
        private const string BrokenLineGlyphPath =
            GlyphFolder + "/InkGlyph_BrokenLine.asset";
        private const string LekebacakPath =
            CreatureFolder + "/InkCreature_Lekebacak.asset";
        private const string KabukluPath =
            CreatureFolder + "/InkCreature_Kabuklu.asset";
        private const string KesikAvciPath =
            CreatureFolder + "/InkCreature_KesikAvci.asset";
        private const string LekebacakLoadoutPath =
            LoadoutFolder + "/InkLoadout_Lekebacak.asset";
        private const string KabukluLoadoutPath =
            LoadoutFolder + "/InkLoadout_Kabuklu.asset";
        private const string KesikAvciLoadoutPath =
            LoadoutFolder + "/InkLoadout_KesikAvci.asset";
        private const string ShellMaterialPath =
            MaterialFolder + "/M_InkShell_M22.mat";
        private const string BrokenLineMaterialPath =
            MaterialFolder + "/M_InkBrokenLine_M22.mat";
        private const string CreaturePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/Lekebacak.prefab";
        private const string HudPanelName = "M22_GlyphLoadoutPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/22 - Setup Ink Glyph Loadouts")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M22 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                Prerequisites prerequisites = ValidatePrerequisites();
                EnsureFolder(GlyphFolder);
                EnsureFolder(CreatureFolder);
                EnsureFolder(LoadoutFolder);
                EnsureFolder(MaterialFolder);

                InkGlyphDefinition eye =
                    LoadRequiredGlyph(EyeGlyphPath, "Göz");
                InkGlyphDefinition foot =
                    LoadRequiredGlyph(FootGlyphPath, "Ayak");
                InkGlyphDefinition shell = GetOrCreateGlyph(
                    ShellGlyphPath,
                    InkGlyphType.Shell,
                    2,
                    8f,
                    2.4f);
                InkGlyphDefinition brokenLine = GetOrCreateGlyph(
                    BrokenLineGlyphPath,
                    InkGlyphType.BrokenLine,
                    1,
                    0f,
                    1f);
                InkCreatureDefinition lekebacak =
                    AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(
                        LekebacakPath);

                if (lekebacak == null)
                {
                    throw new InvalidOperationException(
                        "M15 Lekebacak tanımı bulunamadı.");
                }

                InkCreatureDefinition kabuklu = GetOrCreateCreature(
                    KabukluPath,
                    "Kabuklu",
                    22f,
                    1.12f,
                    eye,
                    foot,
                    shell);
                InkCreatureDefinition kesikAvci = GetOrCreateCreature(
                    KesikAvciPath,
                    "Kesik Avcı",
                    17f,
                    0.95f,
                    eye,
                    foot,
                    brokenLine);
                InkGlyphLoadoutDefinition[] loadouts =
                {
                    GetOrCreateLoadout(
                        LekebacakLoadoutPath,
                        InkGlyphLoadoutId.Lekebacak,
                        "Lekebacak",
                        "Göz + Ayak",
                        lekebacak,
                        35f,
                        2,
                        new Color(0.55f, 0.12f, 0.78f, 1f)),
                    GetOrCreateLoadout(
                        KabukluLoadoutPath,
                        InkGlyphLoadoutId.Kabuklu,
                        "Kabuklu",
                        "Göz + Ayak + Kabuk",
                        kabuklu,
                        48f,
                        4,
                        new Color(0.12f, 0.68f, 0.78f, 1f)),
                    GetOrCreateLoadout(
                        KesikAvciLoadoutPath,
                        InkGlyphLoadoutId.KesikAvci,
                        "Kesik Avcı",
                        "Göz + Ayak + Kesik Çizgi",
                        kesikAvci,
                        42f,
                        3,
                        new Color(0.76f, 0.18f, 0.62f, 1f))
                };

                Material shellMaterial = GetOrCreateMaterial(
                    ShellMaterialPath,
                    new Color(0.035f, 0.16f, 0.2f, 1f),
                    0.82f);
                Material brokenLineMaterial = GetOrCreateMaterial(
                    BrokenLineMaterialPath,
                    new Color(0.72f, 0.08f, 0.56f, 1f),
                    0.48f);
                UpgradeCreaturePrefab(
                    shellMaterial,
                    brokenLineMaterial);

                InkGlyphLoadoutController controller =
                    GetOrAdd<InkGlyphLoadoutController>(
                        prerequisites.NestController.gameObject);
                controller.Configure(
                    prerequisites.RoleAuthority,
                    prerequisites.Economy,
                    prerequisites.NestController,
                    loadouts);
                prerequisites.NestController.ConfigureLoadouts(controller);

                HudReferences hud = GetOrCreateHud(
                    prerequisites,
                    controller);
                MarkDirty(
                    controller,
                    prerequisites.NestController,
                    hud.Hud,
                    hud.Panel,
                    shell,
                    brokenLine,
                    kabuklu,
                    kesikAvci);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.NestController.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M22 Setup] Tamamlandı. F2: Painter, G: dizilim " +
                    "değiştir, F7: seçili yuvayı fırça yönüne kur. " +
                    "Lekebacak=2, Kabuklu=4, Kesik Avcı=3 karmaşıklık.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/22 - Diagnose Ink Glyph Loadouts")]
        public static void Diagnose()
        {
            InkGlyphLoadoutController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkGlyphLoadoutController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkGlyphLoadoutHud[] huds =
                UnityEngine.Object.FindObjectsByType<InkGlyphLoadoutHud>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CreaturePrefabPath);
            int zones = prefab != null
                ? prefab.GetComponentsInChildren<InkGlyphHitZone>(true).Length
                : 0;
            bool managerApi =
                typeof(InkSystemManager).GetMethod(
                    "TrySpawnCreature") != null &&
                typeof(InkSystemManager).GetMethod(
                    "TrySpawnCreatureFromNest") != null;
            bool economyApi =
                typeof(InkPainterEconomy).GetMethod(
                    "CanCreateNest",
                    new[] { typeof(int) }) != null;

            Debug.Log(
                "[M22 Diagnose] " +
                $"Controllers={controllers.Length}, HUDs={huds.Length}, " +
                $"PrefabGlyphZones={zones}, ManagerApi={managerApi}, " +
                $"EconomyApi={economyApi}, Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                InkGlyphLoadoutController controller = controllers[i];
                InkGlyphLoadoutDefinition loadout =
                    controller.ActiveLoadout;
                Debug.Log(
                    "[M22 Diagnose Controller] " +
                    $"Path={GetHierarchyPath(controller.transform)}, " +
                    $"Selected={(loadout != null ? loadout.DisplayName : "Yok")}, " +
                    $"Pigment={controller.ActivePigmentCost:F0}, " +
                    $"Complexity={controller.ActiveComplexityCost}, " +
                    $"Reason={controller.LastSelectionReason}",
                    controller);
            }

            InkNestSpawner[] nests =
                UnityEngine.Object.FindObjectsByType<InkNestSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < nests.Length; i++)
            {
                InkNestSpawner nest = nests[i];
                Debug.Log(
                    "[M22 Diagnose Nest] " +
                    $"Path={GetHierarchyPath(nest.transform)}, " +
                    $"SpawnDefinition=" +
                    $"{(nest.SpawnDefinition != null ? nest.SpawnDefinition.DisplayName : "Yok")}, " +
                    $"Children={nest.ActiveChildCount}",
                    nest);
            }
        }

        private static Prerequisites ValidatePrerequisites()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    CreaturePrefabPath) == null)
            {
                throw new InvalidOperationException(
                    "M15 Lekebacak prefabı bulunamadı.");
            }

            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterEconomy[] economies =
                UnityEngine.Object.FindObjectsByType<InkPainterEconomy>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterNestController[] nests =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterNestController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (managers.Length != 1 ||
                economies.Length != 1 ||
                nests.Length != 1 ||
                authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M22 tek Manager, Economy, NestController ve " +
                    "RoleAuthority bekliyor. M20–M21 kurulumunu doğrula. " +
                    $"Manager={managers.Length}, Economy={economies.Length}, " +
                    $"Nest={nests.Length}, Authority={authorities.Length}.");
            }

            if (typeof(InkSystemManager).GetMethod(
                    "TrySpawnCreature") == null ||
                typeof(InkPainterEconomy).GetMethod(
                    "CanCreateNest",
                    new[] { typeof(int) }) == null)
            {
                throw new InvalidOperationException(
                    "M22 ReplaceExisting/Assets dosyaları henüz " +
                    "kopyalanmamış.");
            }

            return new Prerequisites(
                managers[0],
                economies[0],
                nests[0],
                authorities[0]);
        }

        private static InkGlyphDefinition LoadRequiredGlyph(
            string path,
            string displayName)
        {
            InkGlyphDefinition glyph =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(path);

            if (glyph == null)
            {
                throw new InvalidOperationException(
                    $"{displayName} sembolü bulunamadı: {path}");
            }

            return glyph;
        }

        private static InkGlyphDefinition GetOrCreateGlyph(
            string path,
            InkGlyphType type,
            int complexity,
            float durabilityModifier,
            float glyphDurability)
        {
            InkGlyphDefinition glyph =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(path);

            if (glyph == null)
            {
                glyph =
                    ScriptableObject.CreateInstance<InkGlyphDefinition>();
                AssetDatabase.CreateAsset(glyph, path);
            }

            var serialized = new SerializedObject(glyph);
            serialized.FindProperty("glyphType").enumValueIndex = (int)type;
            serialized.FindProperty("complexityCost").intValue = complexity;
            serialized.FindProperty("durabilityModifier").floatValue =
                durabilityModifier;
            serialized.FindProperty("glyphDurability").floatValue =
                glyphDurability;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(glyph);
            return glyph;
        }

        private static InkCreatureDefinition GetOrCreateCreature(
            string path,
            string displayName,
            float baseDurability,
            float baseScale,
            params InkGlyphDefinition[] glyphDefinitions)
        {
            InkCreatureDefinition definition =
                AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(path);

            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<InkCreatureDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("baseDurability").floatValue =
                baseDurability;
            serialized.FindProperty("baseScale").floatValue = baseScale;
            SerializedProperty glyphs = serialized.FindProperty("glyphs");
            glyphs.arraySize = glyphDefinitions.Length;

            for (int i = 0; i < glyphDefinitions.Length; i++)
            {
                glyphs.GetArrayElementAtIndex(i).objectReferenceValue =
                    glyphDefinitions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static InkGlyphLoadoutDefinition GetOrCreateLoadout(
            string path,
            InkGlyphLoadoutId id,
            string displayName,
            string description,
            InkCreatureDefinition creature,
            float pigmentCost,
            int complexityCost,
            Color accent)
        {
            InkGlyphLoadoutDefinition loadout =
                AssetDatabase.LoadAssetAtPath<InkGlyphLoadoutDefinition>(
                    path);

            if (loadout == null)
            {
                loadout = ScriptableObject.CreateInstance<
                    InkGlyphLoadoutDefinition>();
                AssetDatabase.CreateAsset(loadout, path);
            }

            var serialized = new SerializedObject(loadout);
            serialized.FindProperty("loadoutId").enumValueIndex = (int)id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("shortDescription").stringValue =
                description;
            serialized.FindProperty("creatureDefinition")
                .objectReferenceValue = creature;
            serialized.FindProperty("pigmentCost").floatValue = pigmentCost;
            serialized.FindProperty("complexityCost").intValue =
                complexityCost;
            serialized.FindProperty("accentColor").colorValue = accent;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loadout);
            return loadout;
        }

        private static Material GetOrCreateMaterial(
            string path,
            Color color,
            float smoothness)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "M22 materyali için uygun shader bulunamadı.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void UpgradeCreaturePrefab(
            Material shellMaterial,
            Material brokenLineMaterial)
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(CreaturePrefabPath);

            try
            {
                InkCreatureRuntime runtime =
                    root.GetComponent<InkCreatureRuntime>();

                if (runtime == null)
                {
                    throw new InvalidOperationException(
                        "Lekebacak prefabında InkCreatureRuntime bulunamadı.");
                }

                if (root.GetComponent<InkBrokenLineCloak>() == null)
                {
                    root.AddComponent<InkBrokenLineCloak>();
                }

                ConfigureShellGlyph(
                    root.transform,
                    runtime,
                    shellMaterial);
                ConfigureBrokenLineGlyph(
                    root.transform,
                    runtime,
                    brokenLineMaterial);
                PrefabUtility.SaveAsPrefabAsset(root, CreaturePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureShellGlyph(
            Transform root,
            InkCreatureRuntime runtime,
            Material material)
        {
            Transform existing = root.Find("ShellGlyph_M22");
            GameObject shell = existing != null
                ? existing.gameObject
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "ShellGlyph_M22";
            shell.transform.SetParent(root, false);
            shell.transform.localPosition = new Vector3(0f, 0.43f, -0.02f);
            shell.transform.localRotation = Quaternion.identity;
            shell.transform.localScale = new Vector3(0.94f, 0.5f, 1.15f);
            Renderer renderer = shell.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            SphereCollider hitCollider = shell.GetComponent<SphereCollider>();

if (hitCollider == null)
{
    hitCollider = shell.AddComponent<SphereCollider>();
}
            hitCollider.isTrigger = true;
            hitCollider.center = Vector3.zero;
            hitCollider.radius = 0.56f;
            ConfigureHitZone(
                shell,
                runtime,
                InkGlyphType.Shell,
                hitCollider);
        }

        private static void ConfigureBrokenLineGlyph(
            Transform root,
            InkCreatureRuntime runtime,
            Material material)
        {
            Transform existing = root.Find("BrokenLineGlyph_M22");
            GameObject marker;

            if (existing != null)
            {
                marker = existing.gameObject;
            }
            else
            {
                marker = new GameObject("BrokenLineGlyph_M22");
                marker.transform.SetParent(root, false);
            }

            marker.transform.localPosition = new Vector3(0f, 0.72f, 0.08f);
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one;

            for (int i = 0; i < 3; i++)
            {
                string name = "Dash_" + i;
                Transform dashTransform = marker.transform.Find(name);
                GameObject dash = dashTransform != null
                    ? dashTransform.gameObject
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = name;
                dash.transform.SetParent(marker.transform, false);
                dash.transform.localPosition =
                    new Vector3((i - 1) * 0.25f, 0f, 0f);
                dash.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    i == 1 ? -8f : 8f);
                dash.transform.localScale =
                    new Vector3(0.16f, 0.055f, 0.055f);
                dash.GetComponent<Renderer>().sharedMaterial = material;
                Collider dashCollider = dash.GetComponent<Collider>();

                if (dashCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(dashCollider);
                }
            }

            BoxCollider hitCollider = marker.GetComponent<BoxCollider>();

if (hitCollider == null)
{
    hitCollider = marker.AddComponent<BoxCollider>();
}
            hitCollider.isTrigger = true;
            hitCollider.center = Vector3.zero;
            hitCollider.size = new Vector3(0.78f, 0.22f, 0.24f);
            ConfigureHitZone(
                marker,
                runtime,
                InkGlyphType.BrokenLine,
                hitCollider);
        }

        private static void ConfigureHitZone(
            GameObject target,
            InkCreatureRuntime runtime,
            InkGlyphType type,
            Collider collider)
        {
            InkGlyphHitZone zone =
                target.GetComponent<InkGlyphHitZone>() ??
                target.AddComponent<InkGlyphHitZone>();
            zone.Configure(
                runtime,
                type,
                collider,
                target.GetComponentsInChildren<Renderer>(true));
            EditorUtility.SetDirty(zone);
        }

        private static HudReferences GetOrCreateHud(
            Prerequisites prerequisites,
            InkGlyphLoadoutController controller)
        {
            InkPainterHud economyHud =
                UnityEngine.Object.FindFirstObjectByType<InkPainterHud>(
                    FindObjectsInactive.Include);
            Canvas canvas = economyHud != null
                ? economyHud.GetComponentInParent<Canvas>(true)
                : UnityEngine.Object.FindFirstObjectByType<Canvas>(
                    FindObjectsInactive.Include);

            if (canvas == null)
            {
                throw new InvalidOperationException(
                    "M22 HUD için Canvas bulunamadı.");
            }

            Transform existing = canvas.transform.Find(HudPanelName);
            GameObject panel = existing != null
                ? existing.gameObject
                : new GameObject(
                    HudPanelName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(panel, "Create M22 HUD");
                panel.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24f, -176f);
            rect.sizeDelta = new Vector2(330f, 74f);
            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.018f, 0.012f, 0.026f, 0.88f);
            background.raycastTarget = false;
            Image accent = GetOrCreateImage(
                rect,
                "Accent",
                new Vector2(0f, 0f),
                new Vector2(5f, 74f));
            Text text = GetOrCreateText(
                rect,
                "LoadoutText",
                new Vector2(14f, -8f),
                new Vector2(304f, 58f));
            InkGlyphLoadoutHud hud =
                GetOrAdd<InkGlyphLoadoutHud>(
                    prerequisites.NestController.gameObject);
            hud.Configure(
                prerequisites.RoleAuthority,
                controller,
                panel,
                text,
                accent);
            panel.SetActive(false);
            return new HudReferences(hud, panel);
        }

        private static Image GetOrCreateImage(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject target = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            if (existing == null)
            {
                target.transform.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)target.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = target.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Text GetOrCreateText(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject target = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            if (existing == null)
            {
                target.transform.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)target.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = target.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.fontSize = 12;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.9f, 0.88f, 0.94f, 1f);
            text.raycastTarget = false;
            return text;
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(target);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void MarkDirty(params UnityEngine.Object[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    EditorUtility.SetDirty(targets[i]);
                }
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "Yok";
            }

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }

        private readonly struct Prerequisites
        {
            public Prerequisites(
                InkSystemManager manager,
                InkPainterEconomy economy,
                InkPainterNestController nestController,
                InkPainterRoleAuthority roleAuthority)
            {
                Manager = manager;
                Economy = economy;
                NestController = nestController;
                RoleAuthority = roleAuthority;
            }

            public InkSystemManager Manager { get; }
            public InkPainterEconomy Economy { get; }
            public InkPainterNestController NestController { get; }
            public InkPainterRoleAuthority RoleAuthority { get; }
        }

        private readonly struct HudReferences
        {
            public HudReferences(
                InkGlyphLoadoutHud hud,
                GameObject panel)
            {
                Hud = hud;
                Panel = panel;
            }

            public InkGlyphLoadoutHud Hud { get; }
            public GameObject Panel { get; }
        }
    }
}
