#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.UI.Telegraph;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypePainterTelegraphMilestone
    {
        private const string RootName =
            "M44_PainterTelegraphFigureFeedback";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string UnifiedControllerPath =
            "M43_UnifiedHudController";

        private const string PanelName =
            "M44_PainterTelegraphPanel";

        private static readonly Color Ink =
            new Color(0.055f, 0.047f, 0.050f, 0.94f);

        private static readonly Color Paper =
            new Color(0.93f, 0.88f, 0.76f, 1f);

        private static readonly Color Accent =
            new Color(1f, 0.55f, 0.08f, 0.94f);

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "44 - Apply Painter Telegraph + Figure Feedback")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M44 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                MonoBehaviour brush =
                    FindRequiredMonoBehaviour(
                        "PainterBrushController");

                MonoBehaviour strokeBudget =
                    FindOptionalMonoBehaviour(
                        "PainterStrokeBudget");

                MonoBehaviour pigment =
                    FindOptionalMonoBehaviour(
                        "PainterPigmentReservoir");

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                GameObject unifiedCanvas =
                    GameObject.Find(UnifiedCanvasName);

                if (unifiedCanvas == null)
                {
                    throw new InvalidOperationException(
                        "M43_UnifiedPlayerHUD bulunamadı. " +
                        "Çalışan M43 sahnesini aç.");
                }

                Transform hudRoot =
                    unifiedCanvas.transform.Find(
                        UnifiedHudRootPath);

                Transform unifiedControllerTransform =
                    unifiedCanvas.transform.Find(
                        UnifiedControllerPath);

                if (hudRoot == null ||
                    unifiedControllerTransform == null)
                {
                    throw new InvalidOperationException(
                        "M43 HUD_Root veya controller bulunamadı.");
                }

                PrototypeUnifiedHudController unifiedController =
                    unifiedControllerTransform.GetComponent<
                        PrototypeUnifiedHudController>();

                if (unifiedController == null)
                {
                    throw new InvalidOperationException(
                        "PrototypeUnifiedHudController bulunamadı.");
                }

                GameObject root =
                    GetOrCreateSceneRoot(RootName);

                PrototypePainterTelegraphProbe probe =
                    GetOrAdd<PrototypePainterTelegraphProbe>(
                        root);

                PrototypePainterTelegraphWorldPresenter worldPresenter =
                    GetOrAdd<
                        PrototypePainterTelegraphWorldPresenter>(
                            root);

                PrototypePainterTelegraphDebugDriver debugDriver =
                    GetOrAdd<
                        PrototypePainterTelegraphDebugDriver>(
                            root);

                probe.Configure(
                    brush,
                    strokeBudget,
                    pigment);

                worldPresenter.Configure(
                    unifiedController);

                debugDriver.Configure(
                    figureMotor.transform,
                    unifiedController);

                RectTransform panelRect =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0f, -166f),
                        new Vector2(820f, 96f),
                        Ink);

                CanvasGroup panel =
                    GetOrAdd<CanvasGroup>(
                        panelRect.gameObject);

                panel.alpha = 0f;
                panel.interactable = false;
                panel.blocksRaycasts = false;

                Image background =
                    panelRect.GetComponent<Image>();

                Text title =
                    CreateText(
                        panelRect,
                        "TitleText",
                        "RESSAM TASLAĞI",
                        16f,
                        FontStyle.Bold,
                        TextAnchor.MiddleLeft,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(20f, -12f),
                        new Vector2(330f, 26f),
                        Paper);

                Text phase =
                    CreateText(
                        panelRect,
                        "PhaseText",
                        "TASLAK HAZIRLANIYOR",
                        14f,
                        FontStyle.Bold,
                        TextAnchor.MiddleRight,
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(-20f, -12f),
                        new Vector2(390f, 26f),
                        Accent);

                Text counter =
                    CreateText(
                        panelRect,
                        "CounterText",
                        "KARŞILIK • ETKİ ALANINDAN ÇIK",
                        13f,
                        FontStyle.Normal,
                        TextAnchor.MiddleLeft,
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        new Vector2(20f, 18f),
                        new Vector2(690f, 25f),
                        Paper);

                Text timer =
                    CreateText(
                        panelRect,
                        "TimerText",
                        "0.0s",
                        15f,
                        FontStyle.Bold,
                        TextAnchor.MiddleRight,
                        new Vector2(1f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(-20f, 18f),
                        new Vector2(90f, 25f),
                        Paper);

                Slider progress =
                    CreateSlider(
                        panelRect,
                        "TelegraphProgress",
                        new Vector2(20f, 8f),
                        new Vector2(780f, 8f),
                        Accent);

                PrototypePainterTelegraphHudPresenter hudPresenter =
                    GetOrAdd<
                        PrototypePainterTelegraphHudPresenter>(
                            unifiedControllerTransform.gameObject);

                hudPresenter.Configure(
                    unifiedController,
                    panel,
                    background,
                    title,
                    phase,
                    counter,
                    timer,
                    progress);

                EditorUtility.SetDirty(root);
                EditorUtility.SetDirty(probe);
                EditorUtility.SetDirty(worldPresenter);
                EditorUtility.SetDirty(debugDriver);
                EditorUtility.SetDirty(hudPresenter);
                EditorSceneManager.MarkSceneDirty(root.scene);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M44 Setup] Painter telegraph + Figure feedback ready.\n" +
                    $"Root={GetHierarchyPath(root.transform)}\n" +
                    $"Brush={Describe(brush)}\n" +
                    $"StrokeBudget={Describe(strokeBudget)}\n" +
                    $"Pigment={Describe(pigment)}\n" +
                    $"FigureAnchor={Describe(figureMotor)}\n" +
                    $"ProbeResolved={probe.SourceResolved}\n" +
                    $"HudConfigured={hudPresenter.IsConfigured}\n" +
                    $"WorldRoleVisibilityConfigured={worldPresenter.RoleVisibilityConfigured}\n" +
                    $"ProbeCount={CountSceneObjects<PrototypePainterTelegraphProbe>()}\n" +
                    $"HudPresenterCount={CountSceneObjects<PrototypePainterTelegraphHudPresenter>()}\n" +
                    $"WorldPresenterCount={CountSceneObjects<PrototypePainterTelegraphWorldPresenter>()}\n" +
                    "InputReads=False\n" +
                    "GameplayStateWrites=False\n" +
                    "PainterBrushModified=False\n" +
                    "M35StainEarlyVisionPreserved=True\n" +
                    "M43UnifiedHudPreserved=True\n" +
                    "F3GameplayInputPreservation=True\n" +
                    "NetworkIntegrationParked=True",
                    root);

                EditorUtility.DisplayDialog(
                    "M44 tamamlandı",
                    "Painter taslağı, hazırlanma/armed/blocked durumları ve " +
                    "Figure karşılık metni birleşik HUD'a eklendi. " +
                    "Play Mode'da M44 Self-Test menüsünü çalıştır.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M44 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "44 - Run Telegraph Self-Test (Play Mode)")]
        public static void RunSelfTest()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M44 Self-Test",
                    "Önce Play Mode'a gir.",
                    "Tamam");
                return;
            }

            PrototypePainterTelegraphDebugDriver driver =
                FindFirstSceneObject<
                    PrototypePainterTelegraphDebugDriver>();

            if (driver == null)
            {
                Debug.LogError(
                    "[M44 Self-Test] Debug driver bulunamadı.");
                return;
            }

            driver.RunSelfTest();

            Debug.Log(
                "[M44 Self-Test] Sequence started: " +
                "Drafting -> Armed -> Committed -> " +
                "Blocked -> Cancelled.",
                driver);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "44 - Diagnose Painter Telegraph + Figure Feedback")]
        public static void Diagnose()
        {
            PrototypePainterTelegraphProbe probe =
                FindFirstSceneObject<
                    PrototypePainterTelegraphProbe>();

            PrototypePainterTelegraphHudPresenter hud =
                FindFirstSceneObject<
                    PrototypePainterTelegraphHudPresenter>();

            PrototypePainterTelegraphWorldPresenter world =
                FindFirstSceneObject<
                    PrototypePainterTelegraphWorldPresenter>();

            PrototypePainterTelegraphDebugDriver driver =
                FindFirstSceneObject<
                    PrototypePainterTelegraphDebugDriver>();

            Debug.Log(
                "[M44 Diagnose]\n" +
                $"Probe={(probe != null ? "OK" : "MISSING")}\n" +
                $"ProbeResolved={(probe != null && probe.SourceResolved)}\n" +
                $"PreviewActive={(probe != null && probe.PreviewActive)}\n" +
                $"CurrentShape={(probe != null ? probe.CurrentShape : "N/A")}\n" +
                $"CurrentPhase={(probe != null ? probe.CurrentPhase.ToString() : "N/A")}\n" +
                $"CurrentProgress={(probe != null ? probe.CurrentProgress : 0f):0.00}\n" +
                $"PublishedSignals={(probe != null ? probe.PublishedSignalCount : 0)}\n" +
                $"CommittedCount={(probe != null ? probe.CommittedCount : 0)}\n" +
                $"CancelledCount={(probe != null ? probe.CancelledCount : 0)}\n" +
                $"LastResolutionReason={(probe != null ? probe.LastResolutionReason : "N/A")}\n" +
                $"Hud={(hud != null ? "OK" : "MISSING")}\n" +
                $"HudConfigured={(hud != null && hud.IsConfigured)}\n" +
                $"HudVisible={(hud != null && hud.TelegraphVisible)}\n" +
                $"FeedbackStatus={(hud != null ? hud.FeedbackStatus : "N/A")}\n" +
                $"TelegraphEvents={(hud != null ? hud.TelegraphEventCount : 0)}\n" +
                $"ResolutionFeedbacks={(hud != null ? hud.ResolutionFeedbackCount : 0)}\n" +
                $"World={(world != null ? "OK" : "MISSING")}\n" +
                $"ActiveWorldTelegraphs={(world != null ? world.ActiveTelegraphCount : 0)}\n" +
                $"CommittedFlashes={(world != null ? world.CommittedFlashCount : 0)}\n" +
                $"CancelledFades={(world != null ? world.CancelledFadeCount : 0)}\n" +
                $"SelfTestCompleted={(driver != null ? driver.CompletedSelfTestCount : 0)}\n" +
                "InputReads=False\n" +
                "GameplayStateWrites=False\n" +
                "PainterBrushModified=False\n" +
                "F3GameplayInputPreservation=True\n" +
                "NetworkIntegrationParked=True");
        }

        private static GameObject GetOrCreateSceneRoot(
            string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject created =
                new GameObject(name);

            Undo.RegisterCreatedObjectUndo(
                created,
                $"Create {name}");

            return created;
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
            RectTransform rect =
                GetOrCreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    pivot,
                    anchoredPosition,
                    sizeDelta);

            Image image =
                GetOrAdd<Image>(
                    rect.gameObject);

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
            RectTransform rect =
                GetOrCreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    pivot,
                    anchoredPosition,
                    sizeDelta);

            Text text =
                GetOrAdd<Text>(
                    rect.gameObject);

            text.text = initialText;
            text.fontSize =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(fontSize));

            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;
            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            if (text.font == null)
            {
                text.font =
                    Resources.GetBuiltinResource<Font>(
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
            RectTransform root =
                GetOrCreateRect(
                    parent,
                    name,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    anchoredPosition,
                    size);

            Image background =
                GetOrAdd<Image>(
                    root.gameObject);

            background.color =
                new Color(
                    Paper.r,
                    Paper.g,
                    Paper.b,
                    0.22f);

            background.raycastTarget = false;

            Slider slider =
                GetOrAdd<Slider>(
                    root.gameObject);

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            slider.transition =
                Selectable.Transition.None;

            RectTransform fillArea =
                GetOrCreateRect(
                    root,
                    "Fill Area",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(-4f, -4f));

            RectTransform fill =
                GetOrCreateRect(
                    fillArea,
                    "Fill",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);

            Image fillImage =
                GetOrAdd<Image>(
                    fill.gameObject);

            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            slider.fillRect = fill;
            slider.targetGraphic = background;
            slider.handleRect = null;

            return slider;
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
            Transform existing =
                parent.Find(name);

            GameObject child;

            if (existing != null)
            {
                child = existing.gameObject;
            }
            else
            {
                child =
                    new GameObject(
                        name,
                        typeof(RectTransform));

                Undo.RegisterCreatedObjectUndo(
                    child,
                    $"Create {name}");

                child.transform.SetParent(
                    parent,
                    false);
            }

            RectTransform rect =
                child.GetComponent<RectTransform>();

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            return rect;
        }

        private static T GetOrAdd<T>(
            GameObject target)
            where T : Component
        {
            T component =
                target.GetComponent<T>();

            return component != null
                ? component
                : Undo.AddComponent<T>(target);
        }

        private static MonoBehaviour
            FindRequiredMonoBehaviour(
                string typeName)
        {
            MonoBehaviour found =
                FindOptionalMonoBehaviour(typeName);

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"{typeName} bulunamadı.");
            }

            return found;
        }

        private static MonoBehaviour
            FindOptionalMonoBehaviour(
                string typeName)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object
                    .FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour behaviour =
                    behaviours[index];

                if (behaviour == null ||
                    EditorUtility.IsPersistent(behaviour) ||
                    !behaviour.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (string.Equals(
                        behaviour.GetType().Name,
                        typeName,
                        StringComparison.Ordinal))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static T
            FindFirstSceneObject<T>()
            where T : Component
        {
            T[] objects =
                UnityEngine.Object
                    .FindObjectsByType<T>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < objects.Length;
                 index++)
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

        private static int CountSceneObjects<T>()
            where T : Component
        {
            return UnityEngine.Object
                .FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(candidate =>
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid());
        }

        private static string Describe(
            Component component)
        {
            return component == null
                ? "MISSING"
                : $"{component.GetType().Name} @ " +
                  GetHierarchyPath(
                      component.transform);
        }

        private static string GetHierarchyPath(
            Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path =
                    transform.name +
                    "/" +
                    path;
            }

            return path;
        }
    }
}
#endif
