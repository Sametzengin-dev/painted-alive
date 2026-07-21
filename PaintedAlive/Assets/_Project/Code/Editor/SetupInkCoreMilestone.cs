using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Watercolor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkCoreMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string ConfigPath =
            RootFolder + "/Data/Paint/Ink/InkSystemConfig.asset";
        private const string EyeGlyphPath =
            RootFolder + "/Data/Paint/Ink/Glyphs/InkGlyph_Eye.asset";
        private const string FootGlyphPath =
            RootFolder + "/Data/Paint/Ink/Glyphs/InkGlyph_Foot.asset";
        private const string LekebacakPath =
            RootFolder + "/Data/Paint/Ink/Creatures/InkCreature_Lekebacak.asset";
        private const string InkMaterialPath =
            RootFolder + "/Art/Materials/Ink/M_InkWet.mat";
        private const string EyeMaterialPath =
            RootFolder + "/Art/Materials/Ink/M_InkEyeWhite.mat";
        private const string SurfacePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/InkSurface.prefab";
        private const string CreaturePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/Lekebacak.prefab";
        private const string ManagerName = "[PaintedAlive] Ink Manager";

        [MenuItem("Tools/Painted Alive/Milestones/15 - Setup Ink Core")]
        public static void Run()
        {
            try
            {
                EnsureFolders();
                ValidatePrerequisites();

                FigureMotor targetFigure = ResolveTargetFigure();
                InkSystemConfig config = GetOrCreateConfig();
                InkGlyphDefinition eye = GetOrCreateGlyph(
                    EyeGlyphPath,
                    InkGlyphType.Eye);
                InkGlyphDefinition foot = GetOrCreateGlyph(
                    FootGlyphPath,
                    InkGlyphType.Foot);
                InkCreatureDefinition lekebacak =
                    GetOrCreateLekebacak(eye, foot);
                Material inkMaterial = GetOrCreateMaterial(
                    InkMaterialPath,
                    new Color(0.012f, 0.008f, 0.02f, 1f),
                    0.9f);
                Material eyeMaterial = GetOrCreateMaterial(
                    EyeMaterialPath,
                    new Color(0.92f, 0.9f, 0.82f, 1f),
                    0.32f);
                InkSurface surfacePrefab = CreateOrUpdateSurfacePrefab(
                    config,
                    inkMaterial);
                InkCreatureRuntime creaturePrefab =
                    CreateOrUpdateCreaturePrefab(
                        config,
                        inkMaterial,
                        eyeMaterial);
                InkSystemManager manager = GetOrCreateManager(
                    config,
                    lekebacak,
                    surfacePrefab,
                    creaturePrefab);
                InkDebugSpawner spawner = ConfigureDebugSpawner(
                    targetFigure,
                    manager);

                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(spawner);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 15] Mürekkep çekirdeği kuruldu. " +
                    $"TargetFigure={GetHierarchyPath(targetFigure.transform)}, " +
                    "Glyphs=Eye+Foot, Creature=Lekebacak, Limit=8. " +
                    "Sahneyi Ctrl+S ile kaydet; Play Mode'da F9 ile test et.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/15 - Diagnose Ink Core")]
        public static void Diagnose()
        {
            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkDebugSpawner[] spawners =
                UnityEngine.Object.FindObjectsByType<InkDebugSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkSurface[] surfaces =
                UnityEngine.Object.FindObjectsByType<InkSurface>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkCreatureRuntime[] creatures =
                UnityEngine.Object.FindObjectsByType<InkCreatureRuntime>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            WatercolorInkReaction[] reactions =
                UnityEngine.Object.FindObjectsByType<WatercolorInkReaction>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int glyphDefinitionCount =
                AssetDatabase.FindAssets("t:InkGlyphDefinition").Length;
            int lekebacakDefinitionCount = 0;
            string[] creatureGuids =
                AssetDatabase.FindAssets("t:InkCreatureDefinition");

            for (int i = 0; i < creatureGuids.Length; i++)
            {
                InkCreatureDefinition definition =
                    AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(
                        AssetDatabase.GUIDToAssetPath(creatureGuids[i]));

                if (definition != null &&
                    definition.ContainsGlyph(InkGlyphType.Eye) &&
                    definition.ContainsGlyph(InkGlyphType.Foot))
                {
                    lekebacakDefinitionCount++;
                }
            }

            int activeCreatureCount = managers.Length > 0
                ? managers[0].ActiveCreatureCount
                : creatures.Length;
            int creatureLimit = managers.Length > 0
                ? managers[0].CreatureLimit
                : 0;
            int duplicateComponents =
                Mathf.Max(0, managers.Length - 1) +
                Mathf.Max(0, spawners.Length - 1);

            Debug.Log(
                "[M15 Diagnose] " +
                $"InkManagers={managers.Length}, " +
                $"InkSurfaces={surfaces.Length}, " +
                $"ActiveCreatures={activeCreatureCount}, " +
                $"CreatureLimit={creatureLimit}, " +
                $"GlyphDefinitions={glyphDefinitionCount}, " +
                $"LekebacakDefinitions={lekebacakDefinitionCount}, " +
                $"WatercolorInkReactions={reactions.Length}, " +
                $"DuplicateComponents={duplicateComponents}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < creatures.Length; i++)
            {
                InkCreatureRuntime creature = creatures[i];
                Debug.Log(
                    "[M15 Diagnose Creature] " +
                    $"Path={GetHierarchyPath(creature.transform)}, " +
                    $"Definition=" +
                    $"{(creature.Definition != null ? creature.Definition.DisplayName : "Yok")}, " +
                    $"CurrentTarget=" +
                    $"{(creature.CurrentTarget != null ? creature.CurrentTarget.name : "Yok")}, " +
                    $"CurrentState={creature.CurrentState}, " +
                    $"CurrentSpeed={creature.CurrentSpeed:F2}, " +
                    $"WaterExposure={creature.WaterExposure:F2}, " +
                    $"SurfaceValid={creature.SurfaceValid}, " +
                    $"Eye={creature.HasGlyph(InkGlyphType.Eye)}, " +
                    $"Foot={creature.HasGlyph(InkGlyphType.Foot)}",
                    creature);
            }

            for (int i = 0; i < surfaces.Length; i++)
            {
                InkSurface surface = surfaces[i];
                Debug.Log(
                    "[M15 Diagnose Surface] " +
                    $"Path={GetHierarchyPath(surface.transform)}, " +
                    $"Radius={surface.CurrentRadius:F2}, " +
                    $"InkAmount={surface.InkAmount:F1}, " +
                    $"Wetness={surface.Wetness:F2}, " +
                    $"SurfaceValid={surface.IsInitialized}",
                    surface);
            }
        }

        private static void ValidatePrerequisites()
        {
            if (AssetDatabase.FindAssets("t:WatercolorFlowConfig").Length == 0)
            {
                throw new InvalidOperationException(
                    "M13 Watercolor Flow asset'i bulunamadı. " +
                    "Önce çalışan M13 kurulumunu doğrula.");
            }

            if (AssetDatabase.FindAssets("t:WatercolorReactionConfig").Length == 0)
            {
                throw new InvalidOperationException(
                    "M14 Watercolor Reactions asset'i bulunamadı. " +
                    "Önce çalışan M14 kurulumunu doğrula.");
            }
        }

        private static FigureMotor ResolveTargetFigure()
        {
            FigureMotor[] figures =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (figures.Length == 0)
            {
                throw new InvalidOperationException(
                    "Sahnede FigureMotor bulunamadı. Çalışan gameplay sahnesini aç.");
            }

            FigureMotor exactActiveMatch = null;
            int activeCount = 0;
            FigureMotor onlyActive = null;

            for (int i = 0; i < figures.Length; i++)
            {
                FigureMotor figure = figures[i];

                if (figure.gameObject.activeInHierarchy)
                {
                    activeCount++;
                    onlyActive = figure;
                }

                if (figure.gameObject.activeInHierarchy &&
                    string.Equals(
                        figure.gameObject.name,
                        "Figure_Player",
                        StringComparison.OrdinalIgnoreCase))
                {
                    exactActiveMatch = figure;
                }
            }

            if (exactActiveMatch != null)
            {
                return exactActiveMatch;
            }

            if (activeCount == 1)
            {
                return onlyActive;
            }

            throw new InvalidOperationException(
                "Birden fazla aktif FigureMotor bulundu ve Figure_Player adıyla " +
                "tek bir test Figürü seçilemedi. Yerel test Figürünü " +
                "Figure_Player olarak adlandırıp Setup'ı yeniden çalıştır.");
        }

        private static InkSystemConfig GetOrCreateConfig()
        {
            InkSystemConfig config =
                AssetDatabase.LoadAssetAtPath<InkSystemConfig>(ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<InkSystemConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            return config;
        }

        private static InkGlyphDefinition GetOrCreateGlyph(
            string path,
            InkGlyphType type)
        {
            InkGlyphDefinition glyph =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(path);

            if (glyph == null)
            {
                glyph = ScriptableObject.CreateInstance<InkGlyphDefinition>();
                AssetDatabase.CreateAsset(glyph, path);
            }

            var serialized = new SerializedObject(glyph);
            serialized.FindProperty("glyphType").enumValueIndex = (int)type;

            if (type == InkGlyphType.Eye)
            {
                serialized.FindProperty("complexityCost").intValue = 1;
                serialized.FindProperty("detectionRange").floatValue = 9f;
                serialized.FindProperty("targetRefreshInterval").floatValue = 0.22f;
                serialized.FindProperty("requiresLineOfSight").boolValue = true;
                serialized.FindProperty("durabilityModifier").floatValue = -1f;
            }
            else if (type == InkGlyphType.Foot)
            {
                serialized.FindProperty("complexityCost").intValue = 1;
                serialized.FindProperty("movementSpeed").floatValue = 4.1f;
                serialized.FindProperty("turnSpeedDegrees").floatValue = 480f;
                serialized.FindProperty("durabilityModifier").floatValue = -2f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(glyph);
            return glyph;
        }

        private static InkCreatureDefinition GetOrCreateLekebacak(
            InkGlyphDefinition eye,
            InkGlyphDefinition foot)
        {
            InkCreatureDefinition definition =
                AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(
                    LekebacakPath);

            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<InkCreatureDefinition>();
                AssetDatabase.CreateAsset(definition, LekebacakPath);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("displayName").stringValue = "Lekebacak";
            serialized.FindProperty("baseDurability").floatValue = 18f;
            serialized.FindProperty("baseScale").floatValue = 1f;
            SerializedProperty glyphs = serialized.FindProperty("glyphs");
            glyphs.arraySize = 2;
            glyphs.GetArrayElementAtIndex(0).objectReferenceValue = eye;
            glyphs.GetArrayElementAtIndex(1).objectReferenceValue = foot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static Material GetOrCreateMaterial(
            string path,
            Color color,
            float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Ink placeholder material için uyumlu shader bulunamadı.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader == null)
            {
                material.shader = shader;
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

        private static InkSurface CreateOrUpdateSurfacePrefab(
            InkSystemConfig config,
            Material inkMaterial)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "InkSurface";
            root.transform.localScale = new Vector3(1f, 0.025f, 1f);
            Collider primitiveCollider = root.GetComponent<Collider>();
            UnityEngine.Object.DestroyImmediate(primitiveCollider);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1f, 2f, 1f);
            Renderer renderer = root.GetComponent<Renderer>();
            renderer.sharedMaterial = inkMaterial;
            InkSurface surface = root.AddComponent<InkSurface>();
            WatercolorInkReaction reaction =
                root.AddComponent<WatercolorInkReaction>();
            SetReference(surface, "surfaceRenderer", renderer);
            SetReference(reaction, "config", config);
            SetReference(reaction, "inkSurface", surface);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                SurfacePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            if (saved == null)
            {
                throw new InvalidOperationException(
                    "InkSurface prefab oluşturulamadı.");
            }

            return saved.GetComponent<InkSurface>();
        }

        private static InkCreatureRuntime CreateOrUpdateCreaturePrefab(
            InkSystemConfig config,
            Material inkMaterial,
            Material eyeMaterial)
        {
            var root = new GameObject("Lekebacak");
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.36f, 0f);
            trigger.radius = 0.38f;

            GameObject body = CreatePrimitiveChild(
                root.transform,
                "InkBlob",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.42f, 0f),
                new Vector3(0.82f, 0.46f, 1.02f),
                inkMaterial);
            GameObject eye = CreatePrimitiveChild(
                root.transform,
                "EyeGlyph",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.52f, 0.45f),
                new Vector3(0.28f, 0.22f, 0.1f),
                eyeMaterial);
            CreatePrimitiveChild(
                eye.transform,
                "Pupil",
                PrimitiveType.Sphere,
                new Vector3(0f, 0f, 0.48f),
                new Vector3(0.44f, 0.52f, 0.28f),
                inkMaterial);
            CreateLeg(root.transform, "FootGlyph_Left", -0.2f, inkMaterial);
            CreateLeg(root.transform, "FootGlyph_Right", 0.2f, inkMaterial);

            InkCreatureRuntime runtime = root.AddComponent<InkCreatureRuntime>();
            WatercolorInkReaction reaction =
                root.AddComponent<WatercolorInkReaction>();
            SetReference(runtime, "visualRoot", root.transform);
            SetReference(runtime, "bodyRenderer", body.GetComponent<Renderer>());
            SetReference(reaction, "config", config);
            SetReference(reaction, "inkCreature", runtime);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                CreaturePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            if (saved == null)
            {
                throw new InvalidOperationException(
                    "Lekebacak prefab oluşturulamadı.");
            }

            return saved.GetComponent<InkCreatureRuntime>();
        }

        private static GameObject CreatePrimitiveChild(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject child = GameObject.CreatePrimitive(primitiveType);
            child.name = objectName;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            Collider collider = child.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            child.GetComponent<Renderer>().sharedMaterial = material;
            return child;
        }

        private static void CreateLeg(
            Transform parent,
            string objectName,
            float x,
            Material material)
        {
            var legObject = new GameObject(objectName);
            legObject.transform.SetParent(parent, false);
            LineRenderer line = legObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.positionCount = 4;
            line.startWidth = 0.055f;
            line.endWidth = 0.035f;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.SetPosition(0, new Vector3(x, 0.3f, 0f));
            line.SetPosition(1, new Vector3(x * 1.12f, 0.14f, 0.04f));
            line.SetPosition(2, new Vector3(x * 1.35f, 0.045f, 0.12f));
            line.SetPosition(3, new Vector3(x * 1.7f, 0.035f, 0.23f));
        }

        private static InkSystemManager GetOrCreateManager(
            InkSystemConfig config,
            InkCreatureDefinition definition,
            InkSurface surfacePrefab,
            InkCreatureRuntime creaturePrefab)
        {
            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (managers.Length > 1)
            {
                throw new InvalidOperationException(
                    "Sahnede birden fazla InkSystemManager var. " +
                    "M15 Diagnose ile duplicate nesneleri belirleyip teke indir.");
            }

            InkSystemManager manager;

            if (managers.Length == 1)
            {
                manager = managers[0];
            }
            else
            {
                var managerObject = new GameObject(ManagerName);
                Undo.RegisterCreatedObjectUndo(
                    managerObject,
                    "Create Ink Manager");
                manager = Undo.AddComponent<InkSystemManager>(managerObject);
            }

            SetReference(manager, "config", config);
            SetReference(manager, "lekebacakDefinition", definition);
            SetReference(manager, "inkSurfacePrefab", surfacePrefab);
            SetReference(manager, "inkCreaturePrefab", creaturePrefab);
            SetInteger(manager, "navigationMask", Physics.DefaultRaycastLayers);
            SetInteger(manager, "visibilityMask", Physics.DefaultRaycastLayers);
            manager.enabled = true;
            return manager;
        }

        private static InkDebugSpawner ConfigureDebugSpawner(
            FigureMotor targetFigure,
            InkSystemManager manager)
        {
            InkDebugSpawner[] allSpawners =
                UnityEngine.Object.FindObjectsByType<InkDebugSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkDebugSpawner spawner =
                targetFigure.GetComponent<InkDebugSpawner>();

            if (allSpawners.Length > 1 ||
                (allSpawners.Length == 1 && spawner == null))
            {
                throw new InvalidOperationException(
                    "Sahnede yanlış Figüre bağlı veya duplicate InkDebugSpawner var. " +
                    "M15 Diagnose ile tek bileşene indir.");
            }

            if (spawner == null)
            {
                spawner = Undo.AddComponent<InkDebugSpawner>(
                    targetFigure.gameObject);
            }

            SetReference(spawner, "targetFigure", targetFigure);
            SetReference(spawner, "inkManager", manager);
            SetInteger(spawner, "surfaceMask", Physics.DefaultRaycastLayers);
            spawner.enabled = true;
            EditorSceneManager.MarkSceneDirty(targetFigure.gameObject.scene);
            return spawner;
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} alanı bulunamadı.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} alanı bulunamadı.");
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder + "/Data", "Paint");
            EnsureFolder(RootFolder + "/Data/Paint", "Ink");
            EnsureFolder(RootFolder + "/Data/Paint/Ink", "Glyphs");
            EnsureFolder(RootFolder + "/Data/Paint/Ink", "Creatures");
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder + "/Art", "Materials");
            EnsureFolder(RootFolder + "/Art/Materials", "Ink");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "Paint");
            EnsureFolder(RootFolder + "/Prefabs/Paint", "Ink");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
