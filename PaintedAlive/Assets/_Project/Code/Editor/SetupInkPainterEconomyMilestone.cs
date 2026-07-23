using System;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Paint.Ink.Lifecycle;
using PaintedAlive.Paint.Ink.Possession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkPainterEconomyMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string InkDataFolder =
            RootFolder + "/Data/Paint/Ink";
        private const string InkMaterialFolder =
            RootFolder + "/Art/Materials/Ink";
        private const string ConfigPath =
            InkDataFolder + "/InkPainterEconomyConfig.asset";
        private const string PreviewMaterialPath =
            InkMaterialFolder + "/M_InkNestPreview.mat";
        private const string SurfacePrefabPath =
            RootFolder + "/Prefabs/Paint/Ink/InkSurface.prefab";
        private const string PreviewName = "InkNestPreview_M20";
        private const string HudName = "M20_InkPainterHUD";

        [MenuItem(
            "Tools/Painted Alive/Milestones/20 - Setup Ink Painter Economy")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M20 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                Prerequisites prerequisites = ValidatePrerequisites();
                EnsureFolder(InkDataFolder);
                EnsureFolder(InkMaterialFolder);
                InkPainterEconomyConfig config = GetOrCreateConfig();
                Material previewMaterial = GetOrCreatePreviewMaterial();
                GameObject host =
                    prerequisites.PossessionController.gameObject;
                InkPainterEconomy economy =
                    GetOrAdd<InkPainterEconomy>(host);
                economy.Configure(
                    config,
                    prerequisites.Manager,
                    prerequisites.PossessionController);

                PreviewReferences preview = GetOrCreatePreview(
                    host.transform,
                    previewMaterial);
                InkPainterNestController controller =
                    GetOrAdd<InkPainterNestController>(host);
                controller.Configure(
                    prerequisites.PossessionController.SourceCamera,
                    prerequisites.Manager,
                    economy,
                    config,
                    preview.Root,
                    preview.Renderer);

                HudReferences hud = GetOrCreateHud(
                    prerequisites.PossessionController,
                    economy,
                    controller);
                hud.Hud.Configure(
                    economy,
                    controller,
                    hud.PigmentText,
                    hud.ComplexityText,
                    hud.StateText,
                    hud.PigmentFill,
                    hud.ComplexityFill);

                economy.enabled = true;
                controller.enabled = true;
                hud.Hud.enabled = true;
                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(previewMaterial);
                EditorUtility.SetDirty(economy);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(hud.Hud);
                EditorSceneManager.MarkSceneDirty(host.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M20 Setup] Ink Painter economy ready. " +
                    $"Pigment={config.StartingPigment:F0}/" +
                    $"{config.PigmentCapacity:F0}, " +
                    $"Regen={config.RegenerationPerSecond:F1}/s, " +
                    $"NestCost={config.NestPlacementCost:F0}, " +
                    $"Complexity={config.MaximumComplexity}, " +
                    $"PossessionDrain=" +
                    $"{config.PossessionDrainPerSecond:F1}/s. " +
                    "Scene was not saved automatically. Press Ctrl+S; " +
                    "Play Mode: hold F7 on a valid floor and release.",
                    controller);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/20 - Diagnose Ink Painter Economy")]
        public static void Diagnose()
        {
            InkPainterEconomy[] economies =
                UnityEngine.Object.FindObjectsByType<InkPainterEconomy>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterNestController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterNestController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterHud[] huds =
                UnityEngine.Object.FindObjectsByType<InkPainterHud>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkNestSpawner[] nests =
                UnityEngine.Object.FindObjectsByType<InkNestSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterEconomyConfig config =
                AssetDatabase.LoadAssetAtPath<InkPainterEconomyConfig>(
                    ConfigPath);
            int duplicateComponents =
                Mathf.Max(0, economies.Length - 1) +
                Mathf.Max(0, controllers.Length - 1) +
                Mathf.Max(0, huds.Length - 1);
            bool managerApiReady = typeof(InkSystemManager).GetProperty(
                "LastSpawnRejection") != null;

            Debug.Log(
                "[M20 Diagnose] " +
                $"Configs={(config != null ? 1 : 0)}, " +
                $"Economies={economies.Length}, " +
                $"NestControllers={controllers.Length}, " +
                $"HUDs={huds.Length}, " +
                $"InkManagers={managers.Length}, " +
                $"RuntimeNests={nests.Length}, " +
                $"ManagerComplexityApi={managerApiReady}, " +
                $"DuplicateComponents={duplicateComponents}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < economies.Length; i++)
            {
                InkPainterEconomy economy = economies[i];
                InkPainterEconomyConfig economyConfig = economy.Config;
                Debug.Log(
                    "[M20 Diagnose Economy] " +
                    $"Path={GetHierarchyPath(economy.transform)}, " +
                    $"Pigment={economy.CurrentPigment:F1}/" +
                    $"{(economyConfig != null ? economyConfig.PigmentCapacity : 0f):F1}, " +
                    $"Complexity={economy.CurrentComplexity}/" +
                    $"{(economyConfig != null ? economyConfig.MaximumComplexity : 0)}, " +
                    $"Nests={economy.ActiveNestCount}, " +
                    $"Creatures={economy.ActiveCreatureCount}, " +
                    $"Casting={economy.CastInProgress}, " +
                    $"Possessing={economy.PossessionActive}, " +
                    $"CanCreateNest={economy.CanCreateNest()}, " +
                    $"CanAddCreature={economy.CanAddCreature()}, " +
                    $"LastEvent={economy.LastEconomyEvent}",
                    economy);
            }

            for (int i = 0; i < controllers.Length; i++)
            {
                InkPainterNestController controller = controllers[i];
                Debug.Log(
                    "[M20 Diagnose Controller] " +
                    $"Path={GetHierarchyPath(controller.transform)}, " +
                    $"Camera={GetObjectName(controller.PainterCamera)}, " +
                    $"Manager={GetObjectName(controller.InkManager)}, " +
                    $"Casting={controller.IsCasting}, " +
                    $"TargetValid={controller.TargetValid}, " +
                    $"CastProgress={controller.CastProgress:F2}, " +
                    $"Target={controller.TargetPoint}, " +
                    $"LastResult={controller.LastResult}",
                    controller);
            }

            for (int i = 0; i < nests.Length; i++)
            {
                InkNestSpawner nest = nests[i];
                Debug.Log(
                    "[M20 Diagnose Nest] " +
                    $"Path={GetHierarchyPath(nest.transform)}, " +
                    $"Children={nest.ActiveChildCount}, " +
                    $"NextSpawnIn={nest.TimeUntilNextSpawn:F2}, " +
                    $"Telegraph={nest.SpawnTelegraphActive}, " +
                    $"LastResult={nest.LastSpawnResult}",
                    nest);
            }
        }

        private static Prerequisites ValidatePrerequisites()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    SurfacePrefabPath) == null)
            {
                throw new InvalidOperationException(
                    "M15 InkSurface prefabı bulunamadı.");
            }

            if (typeof(InkSystemManager).GetMethod(
                    "TrySpawnLekebacakFromNest") == null ||
                typeof(InkNestSpawner).GetProperty(
                    "TimeUntilNextSpawn") == null)
            {
                throw new InvalidOperationException(
                    "M19 Ink Nest Lifecycle bulunamadı. M19 paketinin " +
                    "ReplaceExisting ve AddNew klasörlerini doğrula.");
            }

            if (typeof(InkSystemManager).GetProperty(
                    "LastSpawnRejection") == null)
            {
                throw new InvalidOperationException(
                    "M20 ReplaceExisting/Assets dosyaları henüz " +
                    "kopyalanmamış.");
            }

            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPossessionController[] possessions =
                UnityEngine.Object.FindObjectsByType<
                    InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (managers.Length != 1)
            {
                throw new InvalidOperationException(
                    "M20 tam olarak bir InkSystemManager bekliyor; " +
                    $"bulunan={managers.Length}.");
            }

            if (possessions.Length != 1 ||
                possessions[0].SourceCamera == null)
            {
                throw new InvalidOperationException(
                    "M20, kamerası atanmış tek bir çalışan M17 " +
                    "InkPossessionController bekliyor.");
            }

            return new Prerequisites(managers[0], possessions[0]);
        }

        private static InkPainterEconomyConfig GetOrCreateConfig()
        {
            InkPainterEconomyConfig config =
                AssetDatabase.LoadAssetAtPath<InkPainterEconomyConfig>(
                    ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<
                    InkPainterEconomyConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            return config;
        }

        private static Material GetOrCreatePreviewMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                PreviewMaterialPath);

            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(
                "Universal Render Pipeline/Unlit");
            shader ??= Shader.Find("Unlit/Color");
            shader ??= Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "M20 önizleme materyali için uygun shader bulunamadı.");
            }

            material = new Material(shader)
            {
                name = "M_InkNestPreview"
            };
            Color color = new Color(0.32f, 0.04f, 0.48f, 0.52f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            AssetDatabase.CreateAsset(material, PreviewMaterialPath);
            return material;
        }

        private static PreviewReferences GetOrCreatePreview(
            Transform host,
            Material material)
        {
            Transform existing = host.Find(PreviewName);
            GameObject root;

            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.name = PreviewName;
                Undo.RegisterCreatedObjectUndo(root, "Create M20 preview");
                root.transform.SetParent(host, false);
            }

            Collider collider = root.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = root.GetComponent<Renderer>();

            if (renderer == null)
            {
                throw new InvalidOperationException(
                    "M20 preview Renderer oluşturulamadı.");
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            root.transform.localScale = new Vector3(1.8f, 0.012f, 1.8f);
            root.SetActive(false);
            EditorUtility.SetDirty(root);
            return new PreviewReferences(root, renderer);
        }

        private static HudReferences GetOrCreateHud(
            InkPossessionController possession,
            InkPainterEconomy economy,
            InkPainterNestController controller)
        {
            Canvas canvas = ResolveCanvas(possession);
            Transform existing = canvas.transform.Find(HudName);
            GameObject root;

            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = new GameObject(
                    HudName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(root, "Create M20 HUD");
                root.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(330f, 142f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.018f, 0.012f, 0.026f, 0.88f);
            background.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            Text title = GetOrCreateText(
                rect,
                "Title",
                font,
                "MÜREKKEP / CANLANDIRICI",
                15,
                new Vector2(14f, -10f),
                new Vector2(302f, 22f));
            title.color = new Color(0.8f, 0.62f, 0.96f, 1f);
            Text pigmentText = GetOrCreateText(
                rect,
                "PigmentText",
                font,
                "PİGMENT",
                13,
                new Vector2(14f, -38f),
                new Vector2(302f, 20f));
            Image pigmentFill = GetOrCreateBar(
                rect,
                "PigmentBar",
                new Vector2(14f, -61f),
                new Vector2(302f, 8f),
                new Color(0.53f, 0.12f, 0.77f, 1f));
            Text complexityText = GetOrCreateText(
                rect,
                "ComplexityText",
                font,
                "KARMAŞIKLIK",
                13,
                new Vector2(14f, -76f),
                new Vector2(302f, 20f));
            Image complexityFill = GetOrCreateBar(
                rect,
                "ComplexityBar",
                new Vector2(14f, -99f),
                new Vector2(302f, 8f),
                new Color(0.06f, 0.72f, 0.8f, 1f));
            Text stateText = GetOrCreateText(
                rect,
                "StateText",
                font,
                "F7 BASILI TUT",
                11,
                new Vector2(14f, -113f),
                new Vector2(302f, 20f));
            stateText.color = new Color(0.78f, 0.8f, 0.86f, 1f);
            InkPainterHud hud = root.GetComponent<InkPainterHud>();

            if (hud == null)
            {
                hud = Undo.AddComponent<InkPainterHud>(root);
            }

            root.SetActive(true);
            EditorUtility.SetDirty(root);
            return new HudReferences(
                hud,
                pigmentText,
                complexityText,
                stateText,
                pigmentFill,
                complexityFill);
        }

        private static Canvas ResolveCanvas(
            InkPossessionController possession)
        {
            Canvas childCanvas = possession.TargetFigure != null
                ? possession.TargetFigure.GetComponentInChildren<Canvas>(true)
                : null;

            if (childCanvas != null)
            {
                return childCanvas;
            }

            Canvas[] canvases =
                UnityEngine.Object.FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (canvases.Length > 0)
            {
                return canvases[0];
            }

            GameObject canvasObject = new GameObject(
                "PaintedAlive_RuntimeHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD Canvas");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        private static Text GetOrCreateText(
            RectTransform parent,
            string name,
            Font font,
            string content,
            int fontSize,
            Vector2 position,
            Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject gameObject = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            if (existing == null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = gameObject.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Image GetOrCreateBar(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color fillColor)
        {
            Transform existing = parent.Find(name);
            GameObject backgroundObject = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            if (existing == null)
            {
                backgroundObject.transform.SetParent(parent, false);
            }

            RectTransform backgroundRect =
                (RectTransform)backgroundObject.transform;
            backgroundRect.anchorMin = new Vector2(0f, 1f);
            backgroundRect.anchorMax = new Vector2(0f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            backgroundRect.anchoredPosition = position;
            backgroundRect.sizeDelta = size;
            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.1f, 0.15f, 0.95f);
            background.raycastTarget = false;
            Transform fillTransform = backgroundRect.Find("Fill");
            GameObject fillObject = fillTransform != null
                ? fillTransform.gameObject
                : new GameObject(
                    "Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            if (fillTransform == null)
            {
                fillObject.transform.SetParent(backgroundRect, false);
            }

            RectTransform fillRect = (RectTransform)fillObject.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.GetComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
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

        private static string GetObjectName(UnityEngine.Object target)
        {
            return target != null ? target.name : "Yok";
        }

        private readonly struct Prerequisites
        {
            public Prerequisites(
                InkSystemManager manager,
                InkPossessionController possessionController)
            {
                Manager = manager;
                PossessionController = possessionController;
            }

            public InkSystemManager Manager { get; }
            public InkPossessionController PossessionController { get; }
        }

        private readonly struct PreviewReferences
        {
            public PreviewReferences(GameObject root, Renderer renderer)
            {
                Root = root;
                Renderer = renderer;
            }

            public GameObject Root { get; }
            public Renderer Renderer { get; }
        }

        private readonly struct HudReferences
        {
            public HudReferences(
                InkPainterHud hud,
                Text pigmentText,
                Text complexityText,
                Text stateText,
                Image pigmentFill,
                Image complexityFill)
            {
                Hud = hud;
                PigmentText = pigmentText;
                ComplexityText = complexityText;
                StateText = stateText;
                PigmentFill = pigmentFill;
                ComplexityFill = complexityFill;
            }

            public InkPainterHud Hud { get; }
            public Text PigmentText { get; }
            public Text ComplexityText { get; }
            public Text StateText { get; }
            public Image PigmentFill { get; }
            public Image ComplexityFill { get; }
        }
    }
}
