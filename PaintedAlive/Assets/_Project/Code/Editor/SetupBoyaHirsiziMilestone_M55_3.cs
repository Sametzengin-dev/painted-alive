#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using PaintedAlive.Paint.Ink.Thief;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupBoyaHirsiziMilestone_M55_3
    {
        private const string MenuRoot =
            "Tools/Painted Alive/Milestones/";

        private const string GlyphFolder =
            "Assets/_Project/Data/Paint/Ink/Glyphs";
        private const string CreatureFolder =
            "Assets/_Project/Data/Paint/Ink/Creatures";
        private const string LoadoutFolder =
            "Assets/_Project/Data/Paint/Ink/Loadouts";
        private const string ThiefFolder =
            "Assets/_Project/Data/Paint/Ink/Thief";

        private const string HandPath =
            GlyphFolder + "/InkGlyph_Hand.asset";
        private const string MouthPath =
            GlyphFolder + "/InkGlyph_Mouth.asset";
        private const string FootPath =
            GlyphFolder + "/InkGlyph_Foot.asset";
        private const string CreaturePath =
            CreatureFolder + "/InkCreature_BoyaHirsizi.asset";
        private const string LoadoutPath =
            LoadoutFolder + "/InkLoadout_BoyaHirsizi.asset";
        private const string ConfigPath =
            ThiefFolder + "/InkToolThiefConfig.asset";
        private const string PrefabPath =
            "Assets/_Project/Prefabs/Paint/Ink/Lekebacak.prefab";
        private const string InkMaterialPath =
            "Assets/_Project/Art/Materials/Ink/M_InkWet.mat";

        [MenuItem(MenuRoot + "55.3 - Setup Boya Hirsizi")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "[M55.3] Exit Play Mode before running setup.");
                return;
            }

            EnsureFolder(ThiefFolder);

            InkGlyphDefinition hand = CreateOrUpdateGlyph(
                HandPath,
                InkGlyphType.Hand,
                1,
                -1f,
                1f);
            InkGlyphDefinition mouth = CreateOrUpdateGlyph(
                MouthPath,
                InkGlyphType.Mouth,
                1,
                -1f,
                1f);
            InkGlyphDefinition foot =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(
                    FootPath);

            if (foot == null)
            {
                Debug.LogError(
                    "[M55.3] Existing Foot glyph asset was not found at " +
                    FootPath + ". M22 data is required.");
                return;
            }

            InkCreatureDefinition creature =
                CreateOrUpdateCreature(hand, mouth, foot);
            InkGlyphLoadoutDefinition loadout =
                CreateOrUpdateLoadout(creature);
            InkToolThiefConfig config = CreateOrUpdateConfig();

            bool prefabConfigured = ConfigureCreaturePrefab(config);
            int sceneControllers = AppendLoadoutToLoadedControllers(loadout);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[M55.3] Boya Hırsızı setup complete. " +
                $"PrefabConfigured={prefabConfigured}, " +
                $"LoadedLoadoutControllersUpdated={sceneControllers}. " +
                "Painter'da G ile 4. loadout'a geç, F7 ile spawn et.");
        }

        [MenuItem(MenuRoot + "55.3 - Diagnose Boya Hirsizi")]
        public static void Diagnose()
        {
            InkGlyphDefinition hand =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(HandPath);
            InkGlyphDefinition mouth =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(MouthPath);
            InkCreatureDefinition creature =
                AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(
                    CreaturePath);
            InkGlyphLoadoutDefinition loadout =
                AssetDatabase.LoadAssetAtPath<InkGlyphLoadoutDefinition>(
                    LoadoutPath);
            InkToolThiefConfig config =
                AssetDatabase.LoadAssetAtPath<InkToolThiefConfig>(ConfigPath);
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            bool prefabHasBehavior = prefab != null &&
                prefab.GetComponent<InkToolThiefController>() != null;
            bool prefabHasHand = HasGlyphZone(prefab, InkGlyphType.Hand);
            bool prefabHasMouth = HasGlyphZone(prefab, InkGlyphType.Mouth);
            bool prefabHasMouthSponge = prefab != null &&
                prefab.GetComponentInChildren<
                    InkToolThiefMouthSpongeSource>(true) != null;

            InkGlyphLoadoutController[] controllers =
                UnityEngine.Object.FindObjectsByType<InkGlyphLoadoutController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int loadedWithThief = 0;

            for (int i = 0; i < controllers.Length; i++)
            {
                if (ControllerContainsLoadout(controllers[i], loadout))
                {
                    loadedWithThief++;
                }
            }

            InkToolThiefController[] activeThieves =
                UnityEngine.Object.FindObjectsByType<InkToolThiefController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            int initializedThieves = 0;
            int carrying = 0;

            for (int i = 0; i < activeThieves.Length; i++)
            {
                InkToolThiefController thief = activeThieves[i];

                if (thief != null && thief.IsThiefDefinitionActive)
                {
                    initializedThieves++;

                    if (thief.IsCarryingTool)
                    {
                        carrying++;
                    }
                }
            }

            Debug.Log(
                "[M55.3 Diagnose]\n" +
                $"HandAsset={hand != null}\n" +
                $"MouthAsset={mouth != null}\n" +
                $"CreatureAsset={creature != null}\n" +
                $"LoadoutAsset={loadout != null}\n" +
                $"ConfigAsset={config != null}\n" +
                $"PrefabBehavior={prefabHasBehavior}\n" +
                $"PrefabHandZone={prefabHasHand}\n" +
                $"PrefabMouthZone={prefabHasMouth}\n" +
                $"PrefabMouthSponge={prefabHasMouthSponge}\n" +
                $"LoadedControllers={controllers.Length}\n" +
                $"ControllersWithBoyaHirsizi={loadedWithThief}\n" +
                $"RuntimeThieves={initializedThieves}\n" +
                $"RuntimeCarrying={carrying}");
        }

        private static InkGlyphDefinition CreateOrUpdateGlyph(
            string path,
            InkGlyphType type,
            int complexity,
            float durabilityModifier,
            float glyphDurability)
        {
            InkGlyphDefinition asset =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<InkGlyphDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("glyphType").enumValueIndex =
                (int)type;
            serialized.FindProperty("complexityCost").intValue = complexity;
            serialized.FindProperty("detectionRange").floatValue = 0f;
            serialized.FindProperty("targetRefreshInterval").floatValue =
                0.25f;
            serialized.FindProperty("requiresLineOfSight").boolValue = true;
            serialized.FindProperty("movementSpeed").floatValue = 0f;
            serialized.FindProperty("turnSpeedDegrees").floatValue = 0f;
            serialized.FindProperty("durabilityModifier").floatValue =
                durabilityModifier;
            serialized.FindProperty("glyphDurability").floatValue =
                glyphDurability;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static InkCreatureDefinition CreateOrUpdateCreature(
            InkGlyphDefinition hand,
            InkGlyphDefinition mouth,
            InkGlyphDefinition foot)
        {
            InkCreatureDefinition asset =
                AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(
                    CreaturePath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<InkCreatureDefinition>();
                AssetDatabase.CreateAsset(asset, CreaturePath);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("displayName").stringValue =
                "Boya Hırsızı";
            SerializedProperty glyphs = serialized.FindProperty("glyphs");
            glyphs.arraySize = 3;
            glyphs.GetArrayElementAtIndex(0).objectReferenceValue = hand;
            glyphs.GetArrayElementAtIndex(1).objectReferenceValue = mouth;
            glyphs.GetArrayElementAtIndex(2).objectReferenceValue = foot;
            serialized.FindProperty("baseDurability").floatValue = 18f;
            serialized.FindProperty("baseScale").floatValue = 0.95f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static InkGlyphLoadoutDefinition CreateOrUpdateLoadout(
            InkCreatureDefinition creature)
        {
            InkGlyphLoadoutDefinition asset =
                AssetDatabase.LoadAssetAtPath<InkGlyphLoadoutDefinition>(
                    LoadoutPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<
                    InkGlyphLoadoutDefinition>();
                AssetDatabase.CreateAsset(asset, LoadoutPath);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("loadoutId").enumValueIndex =
                (int)InkGlyphLoadoutId.BoyaHirsizi;
            serialized.FindProperty("displayName").stringValue =
                "Boya Hırsızı";
            serialized.FindProperty("shortDescription").stringValue =
                "El + Ağız + Ayak";
            serialized.FindProperty("creatureDefinition")
                .objectReferenceValue = creature;
            serialized.FindProperty("pigmentCost").floatValue = 45f;
            serialized.FindProperty("complexityCost").intValue = 3;
            serialized.FindProperty("accentColor").colorValue =
                new Color(0.94f, 0.42f, 0.1f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static InkToolThiefConfig CreateOrUpdateConfig()
        {
            InkToolThiefConfig asset =
                AssetDatabase.LoadAssetAtPath<InkToolThiefConfig>(ConfigPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<InkToolThiefConfig>();
                AssetDatabase.CreateAsset(asset, ConfigPath);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("detectionRange").floatValue = 8.5f;
            serialized.FindProperty("targetRefreshInterval").floatValue =
                0.2f;
            serialized.FindProperty("requiresLineOfSight").boolValue = true;
            serialized.FindProperty("stealDistance").floatValue = 1.15f;
            serialized.FindProperty("stealWindup").floatValue = 0.85f;
            serialized.FindProperty("stealRetryCooldown").floatValue = 1f;
            serialized.FindProperty("desiredEscapeDistance").floatValue = 6f;
            serialized.FindProperty("dropDistanceFromVictim").floatValue = 5f;
            serialized.FindProperty("minimumCarryDuration").floatValue = 2f;
            serialized.FindProperty("maximumCarryDuration").floatValue = 7f;
            serialized.FindProperty("pickupAutoReturnTime").floatValue = 12f;
            serialized.FindProperty("mouthInkAmount").floatValue = 0.65f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static bool ConfigureCreaturePrefab(
            InkToolThiefConfig config)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            if (root == null)
            {
                Debug.LogError(
                    "[M55.3] Generic Ink creature prefab not found: " +
                    PrefabPath);
                return false;
            }

            try
            {
                InkCreatureRuntime runtime =
                    root.GetComponent<InkCreatureRuntime>();

                if (runtime == null)
                {
                    Debug.LogError(
                        "[M55.3] InkCreatureRuntime missing on prefab.");
                    return false;
                }

                InkToolThiefController thief =
                    root.GetComponent<InkToolThiefController>();

                if (thief == null)
                {
                    thief = root.AddComponent<InkToolThiefController>();
                }

                thief.Configure(config);

                Material inkMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(InkMaterialPath);

                GameObject hand = EnsureGlyphObject(
                    root.transform,
                    "HandGlyph_M55_3",
                    PrimitiveType.Sphere,
                    new Vector3(0.32f, 0.43f, 0.2f),
                    new Vector3(0.22f, 0.18f, 0.2f),
                    runtime,
                    InkGlyphType.Hand,
                    inkMaterial);
                GameObject mouth = EnsureGlyphObject(
                    root.transform,
                    "MouthGlyph_M55_3",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.38f, 0.5f),
                    new Vector3(0.28f, 0.13f, 0.14f),
                    runtime,
                    InkGlyphType.Mouth,
                    inkMaterial);

                InkToolThiefMouthSpongeSource mouthSource =
                    mouth.GetComponent<InkToolThiefMouthSpongeSource>();

                if (mouthSource == null)
                {
                    mouthSource = mouth.AddComponent<
                        InkToolThiefMouthSpongeSource>();
                }

                mouthSource.Configure(runtime, config);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return hand != null && mouth != null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject EnsureGlyphObject(
            Transform root,
            string objectName,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            InkCreatureRuntime runtime,
            InkGlyphType glyphType,
            Material material)
        {
            Transform existing = root.Find(objectName);
            GameObject target;

            if (existing != null)
            {
                target = existing.gameObject;
            }
            else
            {
                target = GameObject.CreatePrimitive(primitive);
                target.name = objectName;
                target.transform.SetParent(root, false);
            }

            target.layer = root.gameObject.layer;
            target.transform.localPosition = localPosition;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = localScale;

            Collider collider = target.GetComponent<Collider>();

            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider>();
            }

            collider.isTrigger = true;

            Renderer renderer = target.GetComponent<Renderer>();

            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            InkGlyphHitZone hitZone =
                target.GetComponent<InkGlyphHitZone>();

            if (hitZone == null)
            {
                hitZone = target.AddComponent<InkGlyphHitZone>();
            }

            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(true);
            hitZone.Configure(
                runtime,
                glyphType,
                collider,
                renderers);
            return target;
        }

        private static int AppendLoadoutToLoadedControllers(
            InkGlyphLoadoutDefinition loadout)
        {
            InkGlyphLoadoutController[] controllers =
                UnityEngine.Object.FindObjectsByType<InkGlyphLoadoutController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int changed = 0;

            for (int i = 0; i < controllers.Length; i++)
            {
                InkGlyphLoadoutController controller = controllers[i];

                if (controller == null ||
                    ControllerContainsLoadout(controller, loadout))
                {
                    continue;
                }

                var serialized = new SerializedObject(controller);
                SerializedProperty loadouts =
                    serialized.FindProperty("loadouts");

                if (loadouts == null)
                {
                    continue;
                }

                int index = loadouts.arraySize;
                loadouts.InsertArrayElementAtIndex(index);
                loadouts.GetArrayElementAtIndex(index)
                    .objectReferenceValue = loadout;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);

                if (controller.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(
                        controller.gameObject.scene);
                }

                changed++;
            }

            return changed;
        }

        private static bool ControllerContainsLoadout(
            InkGlyphLoadoutController controller,
            InkGlyphLoadoutDefinition loadout)
        {
            if (controller == null || loadout == null)
            {
                return false;
            }

            var serialized = new SerializedObject(controller);
            SerializedProperty loadouts = serialized.FindProperty("loadouts");

            if (loadouts == null)
            {
                return false;
            }

            for (int i = 0; i < loadouts.arraySize; i++)
            {
                SerializedProperty element =
                    loadouts.GetArrayElementAtIndex(i);

                if (element.objectReferenceValue == loadout)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGlyphZone(
            GameObject prefab,
            InkGlyphType type)
        {
            if (prefab == null)
            {
                return false;
            }

            InkGlyphHitZone[] zones =
                prefab.GetComponentsInChildren<InkGlyphHitZone>(true);

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null && zones[i].GlyphType == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
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
    }
}
#endif
