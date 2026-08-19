#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeUnifiedHudMilestone
    {
        private const string CanvasName = "M43_UnifiedPlayerHUD";
        private const string ControllerName = "M43_UnifiedHudController";

        private static readonly Color Ink =
            new Color(0.055f, 0.047f, 0.050f, 0.94f);

        private static readonly Color Paper =
            new Color(0.93f, 0.88f, 0.76f, 1f);

        private static readonly Color FigureAccent =
            new Color(0.12f, 0.66f, 0.70f, 1f);

        private static readonly Color PainterAccent =
            new Color(0.82f, 0.19f, 0.13f, 1f);

        private static readonly Color Warning =
            new Color(0.95f, 0.62f, 0.18f, 1f);

        [MenuItem("Tools/Painted Alive/Milestones/43.1 - Setup Unified Player HUD Core")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M43.1 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                Component match = FindRequiredComponent("PrototypeMatchController");
                Component role = FindRequiredComponent("PrototypeRoleSwitcher");
                Component figureMotor = FindRequiredComponent("FigureMotor");
                Component clarity = FindRequiredRelatedComponent(
                    figureMotor,
                    "FigureClarityState");
                Component pigment = FindRequiredComponent("PainterPigmentReservoir");
                Component progress = FindRequiredComponent("FigureProgressTracker");

                Component score = FindOptionalContaining(
                    "JourneyProgressScore",
                    "JourneyScore",
                    "ProgressScore");

                Component encounter = FindOptionalComponent(
                    "PrototypeEncounterRhythmDirector");

                Component figureTool = FindOptionalRelatedContaining(
                    figureMotor,
                    "FigureToolLoadout",
                    "FigureTool",
                    "PaletteKnife",
                    "Fixative");

                Component painter = FindOptionalComponent(
                    "PainterBrushController");

                GameObject canvasObject = GetOrCreateRoot(CanvasName);
                Canvas canvas = GetOrAdd<Canvas>(canvasObject);
                CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
                GraphicRaycaster raycaster = GetOrAdd<GraphicRaycaster>(canvasObject);

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 120;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                raycaster.enabled = false;

                RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
                Stretch(canvasRect);

                GameObject controllerObject = GetOrCreateChild(
                    canvasObject.transform,
                    ControllerName);

                PrototypeUnifiedHudView view =
                    GetOrAdd<PrototypeUnifiedHudView>(controllerObject);

                PrototypeUnifiedHudController controller =
                    GetOrAdd<PrototypeUnifiedHudController>(controllerObject);

                RectTransform root = GetOrCreateRect(
                    canvasObject.transform,
                    "HUD_Root",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero);

                CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root.gameObject);
                rootGroup.blocksRaycasts = false;
                rootGroup.interactable = false;

                RectTransform topBar = CreatePanel(
                    root,
                    "TopBar",
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -22f),
                    new Vector2(820f, 88f),
                    Ink);

                Text roleText = CreateText(
                    topBar,
                    "RoleText",
                    "FIGURE",
                    20f,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(24f, 14f),
                    new Vector2(230f, 28f),
                    FigureAccent);

                Text timerText = CreateText(
                    topBar,
                    "TimerText",
                    "05:00",
                    30f,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(180f, 44f),
                    Paper);

                Text matchStateText = CreateText(
                    topBar,
                    "MatchStateText",
                    "BEKLİYOR",
                    16f,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-24f, 15f),
                    new Vector2(230f, 28f),
                    Warning);

                Text objectiveText = CreateText(
                    topBar,
                    "ObjectiveText",
                    "ÇERÇEVEYE İLERLE",
                    13f,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 13f),
                    new Vector2(740f, 25f),
                    Paper);

                RectTransform encounterStrip = CreatePanel(
                    root,
                    "EncounterStrip",
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -120f),
                    new Vector2(620f, 38f),
                    new Color(Ink.r, Ink.g, Ink.b, 0.86f));

                Text encounterText = CreateText(
                    encounterStrip,
                    "EncounterText",
                    "ROTAYI OKU",
                    14f,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Paper);

                RectTransform progressPanel = CreatePanel(
                    root,
                    "ProgressPanel",
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 28f),
                    new Vector2(620f, 64f),
                    Ink);

                Slider progressSlider = CreateSlider(
                    progressPanel,
                    "ProgressSlider",
                    new Vector2(20f, 27f),
                    new Vector2(580f, 12f),
                    FigureAccent);

                Text progressText = CreateText(
                    progressPanel,
                    "ProgressText",
                    "YOLCULUK %0 • SKOR 0",
                    13f,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -7f),
                    new Vector2(580f, 22f),
                    Paper);

                RectTransform figurePanelRect = CreatePanel(
                    root,
                    "FigurePanel",
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(24f, 24f),
                    new Vector2(360f, 152f),
                    Ink);

                CanvasGroup figurePanel = GetOrAdd<CanvasGroup>(
                    figurePanelRect.gameObject);

                CreateText(
                    figurePanelRect,
                    "FigureTitle",
                    "FIGURE DURUMU",
                    15f,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(18f, -14f),
                    new Vector2(300f, 24f),
                    FigureAccent);

                Slider claritySlider = CreateSlider(
                    figurePanelRect,
                    "ClaritySlider",
                    new Vector2(18f, 61f),
                    new Vector2(324f, 16f),
                    FigureAccent);

                Text clarityValueText = CreateText(
                    figurePanelRect,
                    "ClarityValueText",
                    "NETLİK 100 / 100",
                    13f,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(18f, -43f),
                    new Vector2(240f, 22f),
                    Paper);

                Text clarityStateText = CreateText(
                    figurePanelRect,
                    "ClarityStateText",
                    "CLEAN",
                    12f,
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-18f, -43f),
                    new Vector2(100f, 22f),
                    FigureAccent);

                Text figureToolText = CreateText(
                    figurePanelRect,
                    "FigureToolText",
                    "ARAÇ PALET BIÇAĞI",
                    13f,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(18f, 15f),
                    new Vector2(324f, 24f),
                    Paper);

                RectTransform painterPanelRect = CreatePanel(
                    root,
                    "PainterPanel",
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(-24f, 24f),
                    new Vector2(360f, 152f),
                    Ink);

                CanvasGroup painterPanel = GetOrAdd<CanvasGroup>(
                    painterPanelRect.gameObject);

                CreateText(
                    painterPanelRect,
                    "PainterTitle",
                    "PAINTER DURUMU",
                    15f,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(18f, -14f),
                    new Vector2(300f, 24f),
                    PainterAccent);

                Slider pigmentSlider = CreateSlider(
                    painterPanelRect,
                    "PigmentSlider",
                    new Vector2(18f, 61f),
                    new Vector2(324f, 16f),
                    PainterAccent);

                Text pigmentValueText = CreateText(
                    painterPanelRect,
                    "PigmentValueText",
                    "PİGMENT 100 / 100",
                    13f,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(18f, -43f),
                    new Vector2(324f, 22f),
                    Paper);

                Text painterMaterialText = CreateText(
                    painterPanelRect,
                    "PainterMaterialText",
                    "MALZEME YAĞLI BOYA",
                    13f,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(18f, 15f),
                    new Vector2(324f, 24f),
                    Paper);

                RectTransform contextPanelRect = CreatePanel(
                    root,
                    "ContextPanel",
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 108f),
                    new Vector2(760f, 46f),
                    new Color(0.13f, 0.07f, 0.04f, 0.94f));

                CanvasGroup contextPanel = GetOrAdd<CanvasGroup>(
                    contextPanelRect.gameObject);

                Text contextText = CreateText(
                    contextPanelRect,
                    "ContextText",
                    string.Empty,
                    14f,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    Warning);

                Undo.RecordObject(view, "Configure M43.1 HUD view");
                view.Configure(
                    rootGroup,
                    roleText,
                    timerText,
                    matchStateText,
                    objectiveText,
                    encounterText,
                    progressSlider,
                    progressText,
                    figurePanel,
                    claritySlider,
                    clarityValueText,
                    clarityStateText,
                    figureToolText,
                    painterPanel,
                    pigmentSlider,
                    pigmentValueText,
                    painterMaterialText,
                    contextPanel,
                    contextText);

                Undo.RecordObject(controller, "Configure M43.1 HUD controller");
                controller.Configure(
                    view,
                    match,
                    role,
                    clarity,
                    pigment,
                    progress,
                    score,
                    encounter,
                    figureTool,
                    painter);

                EditorUtility.SetDirty(view);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(canvasObject.scene);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M43.1 Setup] Unified Player HUD core ready.\n" +
                    $"Canvas={GetHierarchyPath(canvasObject.transform)}\n" +
                    $"ControllerCount={CountSceneObjects<PrototypeUnifiedHudController>()}\n" +
                    $"ViewCount={CountSceneObjects<PrototypeUnifiedHudView>()}\n" +
                    $"Match={Describe(match)}\n" +
                    $"Role={Describe(role)}\n" +
                    $"FigureMotor={Describe(figureMotor)}\n" +
                    $"Clarity={Describe(clarity)}\n" +
                    $"AuthoritativeClarityBound={controller.AuthoritativeClarityBound}\n" +
                    $"ClaritySourcePath={controller.ClaritySourcePath}\n" +
                    $"Pigment={Describe(pigment)}\n" +
                    $"Progress={Describe(progress)}\n" +
                    $"Score={Describe(score)}\n" +
                    $"Encounter={Describe(encounter)}\n" +
                    "Legacy milestone HUDs were not disabled.\n" +
                    "No timer, role, score, clarity, pigment or encounter authority was replaced.",
                    canvasObject);

                EditorUtility.DisplayDialog(
                    "M43.1 kurulumu tamamlandı",
                    "Birleşik HUD çekirdeği sahneye eklendi. Play Mode'da F1/F2 rol geçişi, " +
                    "Netlik, pigment, rota ilerlemesi ve maç durumunu doğrula. " +
                    "Eski debug HUD'ları bu aşamada bilinçli olarak açık bırakıldı.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M43.1 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/43.1 - Diagnose Unified Player HUD Core")]
        public static void Diagnose()
        {
            PrototypeUnifiedHudController controller =
                FindFirstSceneObject<PrototypeUnifiedHudController>();

            PrototypeUnifiedHudView view =
                FindFirstSceneObject<PrototypeUnifiedHudView>();

            GameObject canvas = GameObject.Find(CanvasName);

            Debug.Log(
                "[M43.1 Diagnose]\n" +
                $"Canvas={(canvas != null ? GetHierarchyPath(canvas.transform) : "MISSING")}\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ControllerCount={CountSceneObjects<PrototypeUnifiedHudController>()}\n" +
                $"View={(view != null ? "OK" : "MISSING")}\n" +
                $"ViewCount={CountSceneObjects<PrototypeUnifiedHudView>()}\n" +
                $"DataSourcesResolved={(controller != null && controller.DataSourcesResolved)}\n" +
                $"ResolvedRole={(controller != null ? controller.ResolvedRole : "N/A")}\n" +
                $"ResolvedMatchState={(controller != null ? controller.ResolvedMatchState : "N/A")}\n" +
                $"AuthoritativeClarityBound={(controller != null && controller.AuthoritativeClarityBound)}\n" +
                $"ClaritySourcePath={(controller != null ? controller.ClaritySourcePath : "N/A")}\n" +
                "LegacyHUDsPreserved=True\n" +
                "NetworkIntegrationParked=True");
        }

        private static Component FindRequiredRelatedComponent(
            Component anchor,
            string typeName)
        {
            Component found = FindRelatedComponent(
                anchor,
                typeName,
                exactName: true);

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"M43.1 {anchor.GetType().Name} hiyerarşisinde " +
                    $"{typeName} bulamadı.");
            }

            return found;
        }

        private static Component FindOptionalRelatedContaining(
            Component anchor,
            params string[] fragments)
        {
            for (int index = 0; index < fragments.Length; index++)
            {
                Component found = FindRelatedComponent(
                    anchor,
                    fragments[index],
                    exactName: false);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Component FindRelatedComponent(
            Component anchor,
            string requested,
            bool exactName)
        {
            if (anchor == null)
            {
                return null;
            }

            Component found = FindMatching(
                anchor.GetComponents<Component>(),
                requested,
                exactName);

            if (found != null)
            {
                return found;
            }

            Transform parent = anchor.transform.parent;
            while (parent != null)
            {
                found = FindMatching(
                    parent.GetComponents<Component>(),
                    requested,
                    exactName);

                if (found != null)
                {
                    return found;
                }

                parent = parent.parent;
            }

            return FindMatching(
                anchor.GetComponentsInChildren<Component>(true),
                requested,
                exactName);
        }

        private static Component FindMatching(
            Component[] components,
            string requested,
            bool exactName)
        {
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;
                bool matches = exactName
                    ? string.Equals(
                        typeName,
                        requested,
                        StringComparison.Ordinal)
                    : typeName.IndexOf(
                        requested,
                        StringComparison.OrdinalIgnoreCase) >= 0;

                if (matches)
                {
                    return component;
                }
            }

            return null;
        }

        private static Component FindRequiredComponent(string typeName)
        {
            Component found = FindOptionalComponent(typeName);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"M43.1 gerekli bileşeni bulamadı: {typeName}. " +
                    "M42'nin çalıştığı P00_PaintLab sahnesini aç.");
            }

            return found;
        }

        private static Component FindOptionalComponent(string typeName)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int index = 0; index < behaviours.Length; index++)
            {
                MonoBehaviour behaviour = behaviours[index];
                if (behaviour == null ||
                    EditorUtility.IsPersistent(behaviour) ||
                    !behaviour.gameObject.scene.IsValid())
                {
                    continue;
                }

                Type type = behaviour.GetType();
                if (string.Equals(type.Name, typeName, StringComparison.Ordinal) ||
                    string.Equals(type.FullName, typeName, StringComparison.Ordinal))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static Component FindOptionalContaining(params string[] fragments)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int fragmentIndex = 0; fragmentIndex < fragments.Length; fragmentIndex++)
            {
                string fragment = fragments[fragmentIndex];

                for (int index = 0; index < behaviours.Length; index++)
                {
                    MonoBehaviour behaviour = behaviours[index];
                    if (behaviour == null ||
                        EditorUtility.IsPersistent(behaviour) ||
                        !behaviour.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    if (behaviour.GetType().Name.IndexOf(
                            fragment,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return behaviour;
                    }
                }
            }

            return null;
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject created = new GameObject(
                name,
                typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
            return created;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject created = new GameObject(
                name,
                typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
            created.transform.SetParent(parent, false);
            return created;
        }

        private static RectTransform GetOrCreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject child = GetOrCreateChild(parent, name);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            RectTransform rect = GetOrCreateRect(
                parent,
                name,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta);

            Image image = GetOrAdd<Image>(rect.gameObject);
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string initialText,
            float fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            RectTransform rect = GetOrCreateRect(
                parent,
                name,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta);

            Text text = GetOrAdd<Text>(rect.gameObject);
            text.text = initialText;
            text.fontSize = Mathf.Max(1, Mathf.RoundToInt(fontSize));
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            }

            return text;
        }

        private static Slider CreateSlider(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Color fillColor)
        {
            RectTransform root = GetOrCreateRect(
                parent,
                name,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                anchoredPosition,
                size);

            Image background = GetOrAdd<Image>(root.gameObject);
            background.color = new Color(Paper.r, Paper.g, Paper.b, 0.20f);
            background.raycastTarget = false;

            Slider slider = GetOrAdd<Slider>(root.gameObject);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            RectTransform fillArea = GetOrCreateRect(
                root,
                "Fill Area",
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-4f, -4f));

            RectTransform fill = GetOrCreateRect(
                fillArea,
                "Fill",
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            Image fillImage = GetOrAdd<Image>(fill.gameObject);
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            slider.fillRect = fill;
            slider.targetGraphic = background;
            slider.handleRect = null;

            return slider;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(target);
        }

        private static T FindFirstSceneObject<T>() where T : Component
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int index = 0; index < objects.Length; index++)
            {
                T candidate = objects[index];
                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static int CountSceneObjects<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(candidate =>
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid());
        }

        private static string Describe(Component component)
        {
            return component != null
                ? $"{component.GetType().Name}@{GetHierarchyPath(component.transform)}"
                : "OPTIONAL_MISSING";
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
#endif
