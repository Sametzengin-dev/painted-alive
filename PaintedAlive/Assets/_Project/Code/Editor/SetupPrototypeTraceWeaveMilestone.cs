#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeTraceWeaveMilestone
    {
        private const string RootName =
            "M51_0_TraceWeaveRiskSpikeB1";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M51_0_TraceWeavePanel";

        private static readonly Color Ink =
            new Color(
                0.045f,
                0.040f,
                0.035f,
                0.95f);

        private static readonly Color Paper =
            new Color(
                0.96f,
                0.92f,
                0.82f,
                1f);

        private static readonly Color Thread =
            new Color(
                0.88f,
                0.90f,
                0.78f,
                1f);

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.0 - Apply Trace Weave Risk Spike B1")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M51.0 setup Play Mode dışında çalıştırılmalıdır.");
                }

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                MonoBehaviour clarityState =
                    FindRelatedMonoBehaviour(
                        figureMotor.transform,
                        "FigureClarityState");

                GameObject unifiedCanvas =
                    GameObject.Find(
                        UnifiedCanvasName);

                if (unifiedCanvas == null)
                {
                    throw new InvalidOperationException(
                        "M43_UnifiedPlayerHUD bulunamadı.");
                }

                Transform hudRoot =
                    unifiedCanvas.transform.Find(
                        UnifiedHudRootPath);

                if (hudRoot == null)
                {
                    throw new InvalidOperationException(
                        "M43 HUD_Root bulunamadı.");
                }

                GameObject root =
                    GetOrCreateRoot(
                        RootName);

                PrototypeTraceWeaveController controller =
                    GetOrAdd<
                        PrototypeTraceWeaveController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(20f, -160f),
                        new Vector2(420f, 138f),
                        Ink);

                CanvasGroup group =
                    GetOrAdd<CanvasGroup>(
                        panel.gameObject);

                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                Text title =
                    CreateText(
                        panel,
                        "TitleText",
                        "İZ DOKUMA • KONTUR İPLİĞİ PROTOTİPİ",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.74f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -9f),
                        new Vector2(-24f, -5f),
                        Thread);

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Son hareket izi kaydediliyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.24f),
                        new Vector2(1f, 0.76f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "T • SON 4 sn İZİ SABİTLE",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.27f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 6f),
                        new Vector2(-24f, -2f),
                        Thread);

                controller.Configure(
                    figureMotor.transform,
                    clarityState,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    group);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M51.0 Setup] Trace Weave Risk Spike B1 ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"FigureClarityState={(clarityState != null ? GetHierarchyPath(clarityState.transform) : "OPTIONAL")}\n" +
                    "HistorySeconds=4\n" +
                    "PrototypeBinding=T\n" +
                    "WeaveLifetimeSeconds=8\n" +
                    "MaximumActiveWeaves=1\n" +
                    "RequiresValidatedStartAnchor=True\n" +
                    "RequiresValidatedEndAnchor=True\n" +
                    "FreeAirGeometryEnabled=False\n" +
                    "UsesCapsuleSegmentProxies=True\n" +
                    "UsesMeshCollider=False\n" +
                    "FullStainCanWeave=False\n" +
                    "ContourThreadInventoryIntegrated=False\n" +
                    "PainterCounterplayIntegrated=False\n" +
                    "NetworkAuthorityEnabled=False\n" +
                    "LocalPrototypeAuthority=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M51.0 Hazır",
                    "Son 4 saniyelik Figure hareket izi kaydı, iki yüzey ankrajı " +
                    "ve capsule segmentli geçici rota prototipi kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M51.0 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.0 - Force Trace Weave (Play Mode)")]
        public static void ForceWeave()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M51.0",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return;
            }

            PrototypeTraceWeaveController controller =
                FindOptional<
                    PrototypeTraceWeaveController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M51.0] Controller bulunamadı.");

                return;
            }

            controller.TryWeaveNow();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.0 - Diagnose Trace Weave")]
        public static void Diagnose()
        {
            PrototypeTraceWeaveController controller =
                FindOptional<
                    PrototypeTraceWeaveController>();

            PrototypeTraceWeaveRoute route =
                controller != null
                    ? controller.ActiveRoute
                    : null;

            string report =
                "[M51.0 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ResolvedRole={(controller != null ? controller.ResolvedRole : "N/A")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"ClarityLevel={(controller != null ? controller.ClarityLevel : "N/A")}\n" +
                $"FullStainBlocked={(controller != null && controller.FullStainBlocked)}\n" +
                $"InputActionActive={(controller != null && controller.InputActionActive)}\n" +
                $"State={(controller != null ? controller.State.ToString() : "N/A")}\n" +
                $"StoredSampleCount={(controller != null ? controller.StoredSampleCount : 0)}\n" +
                $"RecordedHistorySeconds={(controller != null ? controller.RecordedHistorySeconds : 0f):F2}\n" +
                $"StartAnchorValid={(controller != null && controller.StartAnchorValid)}\n" +
                $"EndAnchorValid={(controller != null && controller.EndAnchorValid)}\n" +
                $"WeaveAttemptCount={(controller != null ? controller.WeaveAttemptCount : 0)}\n" +
                $"WeaveSuccessCount={(controller != null ? controller.WeaveSuccessCount : 0)}\n" +
                $"WeaveRejectCount={(controller != null ? controller.WeaveRejectCount : 0)}\n" +
                $"ReplacedWeaveCount={(controller != null ? controller.ReplacedWeaveCount : 0)}\n" +
                $"LastBuiltPointCount={(controller != null ? controller.LastBuiltPointCount : 0)}\n" +
                $"LastRouteLength={(controller != null ? controller.LastRouteLength : 0f):F2}\n" +
                $"Route={(route != null ? "OK" : "NONE")}\n" +
                $"RouteActive={(route != null && route.ActiveRoute)}\n" +
                $"RouteRemainingSeconds={(route != null ? route.RemainingSeconds : 0f):F2}\n" +
                $"RouteColliderSegments={(route != null ? route.ColliderSegmentCount : 0)}\n" +
                $"RouteVisualPoints={(route != null ? route.VisualPointCount : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "TraceWeaveEnabled=True\n" +
                "RecordedWindowSeconds=4\n" +
                "PrototypeBinding=T\n" +
                "RequiresValidatedStartAnchor=True\n" +
                "RequiresValidatedEndAnchor=True\n" +
                "FreeAirGeometryEnabled=False\n" +
                "MaximumActiveWeaves=1\n" +
                "UsesCapsuleSegmentProxies=True\n" +
                "UsesMeshCollider=False\n" +
                "FullStainCanWeave=False\n" +
                "ContourThreadInventoryIntegrated=False\n" +
                "PainterCounterplayIntegrated=False\n" +
                "NetworkAuthorityEnabled=False\n" +
                "LocalPrototypeAuthority=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : route);

            EditorUtility.DisplayDialog(
                "M51.0 Diagnose",
                report,
                "Tamam");
        }

        private static MonoBehaviour FindRelatedMonoBehaviour(
            Transform root,
            string typeName)
        {
            if (root == null)
            {
                return null;
            }

            MonoBehaviour[] related =
                root.GetComponentsInChildren<
                    MonoBehaviour>(
                        true);

            for (int index = 0;
                 index < related.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    related[index];

                if (
                    candidate != null &&
                    candidate.GetType().Name ==
                        typeName
                )
                {
                    return candidate;
                }
            }

            related =
                root.GetComponentsInParent<
                    MonoBehaviour>(
                        true);

            for (int index = 0;
                 index < related.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    related[index];

                if (
                    candidate != null &&
                    candidate.GetType().Name ==
                        typeName
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static MonoBehaviour FindRequiredMonoBehaviour(
            string typeName)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    behaviours[index];

                if (
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.GetType().Name ==
                        typeName
                )
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"{typeName} bulunamadı.");
        }

        private static GameObject GetOrCreateRoot(
            string name)
        {
            GameObject existing =
                GameObject.Find(
                    name);

            if (existing != null)
            {
                return existing;
            }

            GameObject root =
                new GameObject(
                    name);

            Undo.RegisterCreatedObjectUndo(
                root,
                $"Create {name}");

            return root;
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
            string value,
            int fontSize,
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

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            if (text.font == null)
            {
                text.font =
                    Resources.GetBuiltinResource<Font>(
                        "LegacyRuntime.ttf");
            }

            return text;
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
                parent.Find(
                    name);

            GameObject child =
                existing != null
                    ? existing.gameObject
                    : new GameObject(
                        name,
                        typeof(RectTransform));

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(
                    child,
                    $"Create {name}");

                child.transform.SetParent(
                    parent,
                    false);
            }

            RectTransform rect =
                child.GetComponent<
                    RectTransform>();

            if (rect == null)
            {
                throw new InvalidOperationException(
                    $"{GetHierarchyPath(child.transform)} RectTransform içermiyor.");
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;

            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            return rect;
        }

        private static T GetOrAdd<T>(
            GameObject target)
            where T : Component
        {
            T existing =
                target.GetComponent<T>();

            if (existing != null)
            {
                return existing;
            }

            T added =
                Undo.AddComponent<T>(
                    target);

            if (added == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} eklenemedi: " +
                    GetHierarchyPath(target.transform));
            }

            return added;
        }

        private static T FindOptional<T>()
            where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int index = 0;
                 index < objects.Length;
                 index++)
            {
                T candidate =
                    objects[index];

                if (
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid()
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetHierarchyPath(
            Transform target)
        {
            if (target == null)
            {
                return "NULL";
            }

            string path =
                target.name;

            while (target.parent != null)
            {
                target =
                    target.parent;

                path =
                    target.name +
                    "/" +
                    path;
            }

            return path;
        }
    }
}
#endif
