using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint;
using PaintedAlive.Paint.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkCounterplayMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string ConfigPath =
            RootFolder + "/Data/Paint/Ink/InkCounterplayConfig.asset";
        private const string EyeGlyphPath =
            RootFolder + "/Data/Paint/Ink/Glyphs/InkGlyph_Eye.asset";
        private const string FootGlyphPath =
            RootFolder + "/Data/Paint/Ink/Glyphs/InkGlyph_Foot.asset";
        private const string SurfacePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/InkSurface.prefab";
        private const string CreaturePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/Lekebacak.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/16 - Setup Ink Counterplay")]
        public static void Run()
        {
            try
            {
                ValidatePrerequisites();
                InkCounterplayConfig config = GetOrCreateConfig();
                ConfigureGlyphDurability(EyeGlyphPath, 1f);
                ConfigureGlyphDurability(FootGlyphPath, 1f);
                UpgradeCreaturePrefab();
                UpgradeSurfacePrefab();

                FigureMotor targetFigure = ResolveTargetFigure();
                InkPaletteKnifeBridge knifeBridge =
                    ConfigurePaletteKnifeBridge(targetFigure, config);
                InkFixativeBridge fixativeBridge =
                    ConfigureFixativeBridge(targetFigure, config);

                EditorUtility.SetDirty(knifeBridge);
                EditorUtility.SetDirty(fixativeBridge);
                EditorSceneManager.MarkSceneDirty(
                    targetFigure.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 16] Mürekkep karşı-oyun sistemi kuruldu. " +
                    "PaletteKnife=Glyph Damage, Fixative=Statue, " +
                    "FrameGun=Pin, Sponge=Ink Absorb. " +
                    "Sahneyi Ctrl+S ile kaydet ve Play Mode'da F9 ile " +
                    "Lekebacak oluşturarak gerçek Figür araçlarıyla test et.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/16 - Diagnose Ink Counterplay")]
        public static void Diagnose()
        {
            InkPaletteKnifeBridge[] knifeBridges =
                UnityEngine.Object.FindObjectsByType<InkPaletteKnifeBridge>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkFixativeBridge[] fixativeBridges =
                UnityEngine.Object.FindObjectsByType<InkFixativeBridge>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkCreatureRuntime[] creatures =
                UnityEngine.Object.FindObjectsByType<InkCreatureRuntime>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkSurface[] surfaces =
                UnityEngine.Object.FindObjectsByType<InkSurface>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkSurfaceSpongeSource[] spongeSources =
                UnityEngine.Object.FindObjectsByType<InkSurfaceSpongeSource>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            FrameGunController[] frameGuns =
                UnityEngine.Object.FindObjectsByType<FrameGunController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int prefabGlyphZones = CountCreaturePrefabGlyphZones();
            bool prefabHasSpongeSource = SurfacePrefabHasSpongeSource();
            int configCount =
                AssetDatabase.FindAssets("t:InkCounterplayConfig").Length;
            int duplicateComponents =
                Mathf.Max(0, knifeBridges.Length - 1) +
                Mathf.Max(0, fixativeBridges.Length - 1);

            Debug.Log(
                "[M16 Diagnose] " +
                $"CounterplayConfigs={configCount}, " +
                $"PaletteKnifeBridges={knifeBridges.Length}, " +
                $"FixativeBridges={fixativeBridges.Length}, " +
                $"FrameGuns={frameGuns.Length}, " +
                $"CreaturePrefabGlyphZones={prefabGlyphZones}, " +
                $"SurfacePrefabSpongeSource={prefabHasSpongeSource}, " +
                $"RuntimeCreatures={creatures.Length}, " +
                $"RuntimeSurfaces={surfaces.Length}, " +
                $"RuntimeSpongeSources={spongeSources.Length}, " +
                $"DuplicateComponents={duplicateComponents}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < creatures.Length; i++)
            {
                InkCreatureRuntime creature = creatures[i];
                Debug.Log(
                    "[M16 Diagnose Creature] " +
                    $"Path={GetHierarchyPath(creature.transform)}, " +
                    $"State={creature.CurrentState}, " +
                    $"EyeActive={creature.HasGlyph(InkGlyphType.Eye)}, " +
                    $"EyeDurability=" +
                    $"{creature.GetGlyphDurability(InkGlyphType.Eye):F2}, " +
                    $"FootActive={creature.HasGlyph(InkGlyphType.Foot)}, " +
                    $"FootDurability=" +
                    $"{creature.GetGlyphDurability(InkGlyphType.Foot):F2}, " +
                    $"ActiveGlyphs={creature.ActiveGlyphCount}, " +
                    $"Fixed={creature.IsFixed}, " +
                    $"Pinned={creature.IsPinned}, " +
                    $"Target=" +
                    $"{(creature.CurrentTarget != null ? creature.CurrentTarget.name : "Yok")}, " +
                    $"Speed={creature.CurrentSpeed:F2}",
                    creature);
            }

            for (int i = 0; i < surfaces.Length; i++)
            {
                InkSurface surface = surfaces[i];
                InkSurfaceSpongeSource source =
                    surface.GetComponent<InkSurfaceSpongeSource>();
                Debug.Log(
                    "[M16 Diagnose Surface] " +
                    $"Path={GetHierarchyPath(surface.transform)}, " +
                    $"InkAmount={surface.InkAmount:F2}, " +
                    $"Radius={surface.CurrentRadius:F2}, " +
                    $"SpongeSource={source != null}, " +
                    $"CanAbsorb={source != null && source.CanAbsorb}",
                    surface);
            }

            for (int i = 0; i < knifeBridges.Length; i++)
            {
                InkPaletteKnifeBridge bridge = knifeBridges[i];
                Debug.Log(
                    "[M16 Diagnose Knife] " +
                    $"Path={GetHierarchyPath(bridge.transform)}, " +
                    $"Cuts={bridge.SuccessfulCutCount}, " +
                    $"LastGlyph={bridge.LastCutGlyph}, " +
                    $"Disabled={bridge.LastCutDisabledGlyph}, " +
                    $"Remaining={bridge.LastRemainingDurability:F2}",
                    bridge);
            }

            for (int i = 0; i < fixativeBridges.Length; i++)
            {
                InkFixativeBridge bridge = fixativeBridges[i];
                Debug.Log(
                    "[M16 Diagnose Fixative] " +
                    $"Path={GetHierarchyPath(bridge.transform)}, " +
                    $"FixedCount={bridge.FixedCreatureCount}, " +
                    $"CurrentTarget=" +
                    $"{(bridge.CurrentTarget != null ? bridge.CurrentTarget.name : "Yok")}, " +
                    $"Distance={bridge.CurrentTargetDistance:F2}",
                    bridge);
            }
        }

        private static void ValidatePrerequisites()
        {
            if (AssetDatabase.LoadAssetAtPath<InkSystemConfig>(
                    RootFolder + "/Data/Paint/Ink/InkSystemConfig.asset") == null ||
                AssetDatabase.LoadAssetAtPath<InkCreatureDefinition>(
                    RootFolder +
                    "/Data/Paint/Ink/Creatures/InkCreature_Lekebacak.asset") == null)
            {
                throw new InvalidOperationException(
                    "M15 Ink Core asset'leri bulunamadı. Önce çalışan M15 " +
                    "kurulumunu tamamla.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    CreaturePrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    SurfacePrefabPath) == null)
            {
                throw new InvalidOperationException(
                    "M15 Lekebacak veya InkSurface prefabı bulunamadı.");
            }

            if (typeof(InkCreatureRuntime).GetMethod(
                    "TryDamageGlyph") == null ||
                typeof(InkSurface).GetMethod("AbsorbInk") == null)
            {
                throw new InvalidOperationException(
                    "M16 ReplaceExisting dosyaları projeye kopyalanmamış. " +
                    "ZIP içeriğini klasör yapısını koruyarak yeniden birleştir.");
            }
        }

        private static InkCounterplayConfig GetOrCreateConfig()
        {
            InkCounterplayConfig config =
                AssetDatabase.LoadAssetAtPath<InkCounterplayConfig>(
                    ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<
                    InkCounterplayConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            return config;
        }

        private static void ConfigureGlyphDurability(
            string path,
            float durability)
        {
            InkGlyphDefinition glyph =
                AssetDatabase.LoadAssetAtPath<InkGlyphDefinition>(path);

            if (glyph == null)
            {
                throw new InvalidOperationException(
                    $"M15 glyph asset'i bulunamadı: {path}");
            }

            var serialized = new SerializedObject(glyph);
            SerializedProperty property =
                serialized.FindProperty("glyphDurability");

            if (property == null)
            {
                throw new InvalidOperationException(
                    "InkGlyphDefinition.glyphDurability alanı bulunamadı. " +
                    "M16 replacement dosyasını kontrol et.");
            }

            property.floatValue = Mathf.Max(0.1f, durability);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(glyph);
        }

        private static void UpgradeCreaturePrefab()
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

                SphereCollider bodyCollider =
                    root.GetComponent<SphereCollider>();

                if (bodyCollider == null)
                {
                    bodyCollider = root.AddComponent<SphereCollider>();
                }

                bodyCollider.isTrigger = true;
                bodyCollider.center = new Vector3(0f, 0.36f, 0f);
                bodyCollider.radius = 0.38f;

                Transform anchorTarget =
                    root.transform.Find("FrameAnchorTarget");

                if (anchorTarget == null)
                {
                    var anchorObject = new GameObject("FrameAnchorTarget");
                    anchorObject.transform.SetParent(root.transform, false);
                    anchorTarget = anchorObject.transform;
                }

                anchorTarget.localPosition = new Vector3(0f, 0.36f, 0f);
                anchorTarget.localRotation = Quaternion.identity;
                anchorTarget.localScale = Vector3.one;
                SphereCollider anchorCollider =
                    anchorTarget.GetComponent<SphereCollider>();

                if (anchorCollider == null)
                {
                    anchorCollider = anchorTarget.gameObject.AddComponent<
                        SphereCollider>();
                }

                anchorCollider.isTrigger = false;
                anchorCollider.center = Vector3.zero;
                anchorCollider.radius = 0.22f;

                Rigidbody body = root.GetComponent<Rigidbody>();

                if (body == null)
                {
                    body = root.AddComponent<Rigidbody>();
                }

                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode =
                    CollisionDetectionMode.ContinuousSpeculative;

                Transform eye = root.transform.Find("EyeGlyph");
                Transform leftFoot =
                    root.transform.Find("FootGlyph_Left");
                Transform rightFoot =
                    root.transform.Find("FootGlyph_Right");

                if (eye == null || leftFoot == null || rightFoot == null)
                {
                    throw new InvalidOperationException(
                        "Lekebacak prefabındaki EyeGlyph/FootGlyph isimleri " +
                        "M15 yapısıyla eşleşmiyor.");
                }

                SphereCollider eyeCollider =
                    eye.GetComponent<SphereCollider>();

                if (eyeCollider == null)
                {
                    eyeCollider = eye.gameObject.AddComponent<
                        SphereCollider>();
                }

                eyeCollider.isTrigger = true;
                eyeCollider.radius = 0.62f;
                ConfigureHitZone(
                    eye.gameObject,
                    runtime,
                    InkGlyphType.Eye,
                    eyeCollider);
                ConfigureFootHitZone(leftFoot.gameObject, runtime, -1f);
                ConfigureFootHitZone(rightFoot.gameObject, runtime, 1f);

                PrefabUtility.SaveAsPrefabAsset(root, CreaturePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureFootHitZone(
            GameObject foot,
            InkCreatureRuntime runtime,
            float side)
        {
            BoxCollider collider = foot.GetComponent<BoxCollider>();

            if (collider == null)
            {
                collider = foot.AddComponent<BoxCollider>();
            }

            collider.isTrigger = true;
            collider.center = new Vector3(side * 0.27f, 0.15f, 0.11f);
            collider.size = new Vector3(0.28f, 0.34f, 0.52f);
            ConfigureHitZone(
                foot,
                runtime,
                InkGlyphType.Foot,
                collider);
        }

        private static void ConfigureHitZone(
            GameObject target,
            InkCreatureRuntime runtime,
            InkGlyphType glyphType,
            Collider hitCollider)
        {
            InkGlyphHitZone zone =
                target.GetComponent<InkGlyphHitZone>();

            if (zone == null)
            {
                zone = target.AddComponent<InkGlyphHitZone>();
            }

            zone.Configure(
                runtime,
                glyphType,
                hitCollider,
                target.GetComponentsInChildren<Renderer>(true));
            EditorUtility.SetDirty(zone);
        }

        private static void UpgradeSurfacePrefab()
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(SurfacePrefabPath);

            try
            {
                InkSurface surface = root.GetComponent<InkSurface>();

                if (surface == null)
                {
                    throw new InvalidOperationException(
                        "InkSurface prefabında InkSurface bileşeni bulunamadı.");
                }

                InkSurfaceSpongeSource source =
                    root.GetComponent<InkSurfaceSpongeSource>();

                if (source == null)
                {
                    source = root.AddComponent<InkSurfaceSpongeSource>();
                }

                source.Configure(surface);
                EditorUtility.SetDirty(source);
                PrefabUtility.SaveAsPrefabAsset(root, SurfacePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static InkPaletteKnifeBridge ConfigurePaletteKnifeBridge(
            FigureMotor targetFigure,
            InkCounterplayConfig config)
        {
            PaletteKnifeController[] controllers =
                targetFigure.GetComponentsInChildren<
                    PaletteKnifeController>(true);

            if (controllers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Target Figure altında tam bir PaletteKnifeController " +
                    $"bekleniyordu; bulunan={controllers.Length}.");
            }

            PaletteKnifeController controller = controllers[0];
            InkPaletteKnifeBridge bridge =
                controller.GetComponent<InkPaletteKnifeBridge>();

            if (bridge == null)
            {
                bridge = Undo.AddComponent<InkPaletteKnifeBridge>(
                    controller.gameObject);
            }

            SetReference(
                bridge,
                "paletteKnifeController",
                controller);
            SetReference(
                bridge,
                "outputCamera",
                GetReference<Camera>(controller, "outputCamera"));
            SetReference(
                bridge,
                "toolOrigin",
                GetReference<Transform>(controller, "toolOrigin"));
            SetReference(
                bridge,
                "useToolAction",
                GetReference<InputActionReference>(
                    controller,
                    "useToolAction"));
            SetReference(
                bridge,
                "clarityState",
                GetReference<FigureClarityState>(
                    controller,
                    "clarityState"));
            SetReference(bridge, "counterplayConfig", config);
            SetInteger(
                bridge,
                "inkMask",
                Physics.DefaultRaycastLayers);
            bridge.enabled = true;
            return bridge;
        }

        private static InkFixativeBridge ConfigureFixativeBridge(
            FigureMotor targetFigure,
            InkCounterplayConfig config)
        {
            FixativeSprayController[] controllers =
                targetFigure.GetComponentsInChildren<
                    FixativeSprayController>(true);

            if (controllers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Target Figure altında tam bir FixativeSprayController " +
                    $"bekleniyordu; bulunan={controllers.Length}.");
            }

            FixativeSprayController controller = controllers[0];
            InkFixativeBridge bridge =
                controller.GetComponent<InkFixativeBridge>();

            if (bridge == null)
            {
                bridge = Undo.AddComponent<InkFixativeBridge>(
                    controller.gameObject);
            }

            SetReference(bridge, "fixativeController", controller);
            SetReference(
                bridge,
                "outputCamera",
                GetReference<Camera>(controller, "outputCamera"));
            SetReference(
                bridge,
                "toolOrigin",
                GetReference<Transform>(controller, "toolOrigin"));
            SetReference(
                bridge,
                "useToolAction",
                GetReference<InputActionReference>(
                    controller,
                    "useToolAction"));
            SetReference(
                bridge,
                "clarityState",
                GetReference<FigureClarityState>(
                    controller,
                    "clarityState"));
            SetReference(
                bridge,
                "fixativeConfig",
                GetReference<FixativeSprayConfig>(controller, "config"));
            SetReference(
                bridge,
                "feedback",
                GetReference<FixativeSprayFeedback>(controller, "feedback"));
            SetReference(bridge, "counterplayConfig", config);
            SetInteger(
                bridge,
                "inkMask",
                Physics.DefaultRaycastLayers);
            bridge.enabled = true;
            return bridge;
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
                    "Sahnede FigureMotor bulunamadı.");
            }

            FigureMotor onlyActive = null;
            int activeCount = 0;

            for (int i = 0; i < figures.Length; i++)
            {
                FigureMotor figure = figures[i];

                if (!figure.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (string.Equals(
                        figure.gameObject.name,
                        "Figure_Player",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return figure;
                }

                activeCount++;
                onlyActive = figure;
            }

            if (activeCount == 1)
            {
                return onlyActive;
            }

            throw new InvalidOperationException(
                "Birden fazla aktif FigureMotor var. Yerel test Figürünü " +
                "Figure_Player olarak adlandır.");
        }

        private static int CountCreaturePrefabGlyphZones()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CreaturePrefabPath);
            return prefab != null
                ? prefab.GetComponentsInChildren<InkGlyphHitZone>(true).Length
                : 0;
        }

        private static bool SurfacePrefabHasSpongeSource()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath);
            return prefab != null &&
                   prefab.GetComponent<InkSurfaceSpongeSource>() != null;
        }

        private static T GetReference<T>(
            UnityEngine.Object target,
            string propertyName)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} alanı bulunamadı.");
            }

            T value = property.objectReferenceValue as T;

            if (value == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} atanmamış.");
            }

            return value;
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property =
                serialized.FindProperty(propertyName);

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
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} alanı bulunamadı.");
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
