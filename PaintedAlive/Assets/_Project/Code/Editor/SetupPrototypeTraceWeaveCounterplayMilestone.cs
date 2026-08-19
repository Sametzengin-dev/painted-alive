#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeTraceWeaveCounterplayMilestone
    {
        private const string RootName =
            "M51_2_TraceWeaveCounterplayRiskSpikeB3";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M51_2_TraceWeaveCounterplayPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.2 - Apply Trace Weave Counterplay Risk Spike B3")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M51.2 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeTraceWeaveController traceWeave =
                    FindRequired<
                        PrototypeTraceWeaveController>();

                PrototypeTraceWeaveTeamRouteController teamRoute =
                    FindRequired<
                        PrototypeTraceWeaveTeamRouteController>();

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                MonoBehaviour paletteKnife =
                    FindRelatedMonoBehaviour(
                        figureMotor.transform,
                        "PaletteKnifeController");

                if (paletteKnife == null)
                {
                    throw new InvalidOperationException(
                        "FigureMotor ile ilişkili PaletteKnifeController bulunamadı.");
                }

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

                PrototypeTraceWeaveCounterplayController controller =
                    GetOrAdd<
                        PrototypeTraceWeaveCounterplayController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0f, -82f),
                        new Vector2(650f, 108f));

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
                        "İZ DOKUMA • KARŞI-OYUN",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperCenter,
                        new Vector2(0f, 0.72f),
                        new Vector2(1f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(12f, -7f),
                        new Vector2(-24f, -3f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Aktif İz Dokuma bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperCenter,
                        new Vector2(0f, 0.26f),
                        new Vector2(1f, 0.75f),
                        new Vector2(0.5f, 1f),
                        new Vector2(12f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "RESSAM F4/F10/F12 • FIGURE PALET BIÇAĞI + E",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerCenter,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.29f),
                        new Vector2(0.5f, 0f),
                        new Vector2(12f, 6f),
                        new Vector2(-24f, -2f));

                controller.Configure(
                    traceWeave,
                    teamRoute,
                    figureMotor.transform,
                    paletteKnife,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    controller);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M51.2 Setup] Trace Weave Counterplay ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"TraceWeave={GetHierarchyPath(traceWeave.transform)}\n" +
                    $"TeamRoute={GetHierarchyPath(teamRoute.transform)}\n" +
                    $"PaletteKnife={GetHierarchyPath(paletteKnife.transform)}\n" +
                    "WatercolorPrototypeBinding=F4\n" +
                    "OilPaintPrototypeBinding=F10\n" +
                    "EraserPrototypeBinding=F12\n" +
                    "PaletteKnifeUsesExistingUseToolActionReadOnly=True\n" +
                    "AddsSecondPaletteKnifeInputOwner=False\n" +
                    "WatercolorPreservesEndpoints=True\n" +
                    "OilAddedLoadKg=100\n" +
                    "EraserTargetedRemove=True\n" +
                    "SafePaletteKnifeEndpointFraction=0.22\n" +
                    "FinalPainterMaterialToolbarIntegrated=False\n" +
                    "NetworkAuthorityEnabled=False",
                    controller);

                EditorUtility.DisplayDialog(
                    "M51.2 Hazır",
                    "Suluboya bükme, Yağlı Boya yükü, Silgi kaldırma ve " +
                    "mevcut Palet Bıçağı inputuyla rota bağlantı düzenleme kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M51.2 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.2 - Force Watercolor Bend (Play Mode)")]
        public static void ForceWatercolor()
        {
            FindPlayModeController()?
                .ForceWatercolorBendForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.2 - Force Oil Load (Play Mode)")]
        public static void ForceOil()
        {
            FindPlayModeController()?
                .ForceOilLoadForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.2 - Force Eraser Remove (Play Mode)")]
        public static void ForceEraser()
        {
            FindPlayModeController()?
                .ForceEraserForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.2 - Force Palette Knife Connection (Play Mode)")]
        public static void ForcePaletteKnife()
        {
            FindPlayModeController()?
                .ForcePaletteKnifeConnectionForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.2 - Diagnose Trace Weave Counterplay")]
        public static void Diagnose()
        {
            PrototypeTraceWeaveCounterplayController controller =
                FindOptional<
                    PrototypeTraceWeaveCounterplayController>();

            PrototypeTraceWeaveController trace =
                FindOptional<
                    PrototypeTraceWeaveController>();

            PrototypeTraceWeaveRoute route =
                trace != null
                    ? trace.ActiveRoute
                    : null;

            string report =
                "[M51.2 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"Route={(route != null ? "OK" : "NONE")}\n" +
                $"RouteActive={(route != null && route.ActiveRoute)}\n" +
                $"RoutePointCount={(route != null ? route.RoutePointCount : 0)}\n" +
                $"PainterRoleActive={(controller != null && controller.PainterRoleActive)}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"PainterCameraResolved={(controller != null && controller.PainterCameraResolved)}\n" +
                $"PainterAimValid={(controller != null && controller.PainterAimValid)}\n" +
                $"PainterAimRouteDistance={(controller != null ? controller.PainterAimRouteDistance : -1f):F3}\n" +
                $"PaletteKnifeActive={(controller != null && controller.PaletteKnifeActive)}\n" +
                $"PaletteKnifeInputResolved={(controller != null && controller.PaletteKnifeInputResolved)}\n" +
                $"PaletteKnifeContract={(controller != null ? controller.PaletteKnifeActivityContract : "N/A")}\n" +
                $"WatercolorAttemptCount={(controller != null ? controller.WatercolorAttemptCount : 0)}\n" +
                $"WatercolorSuccessCount={(controller != null ? controller.WatercolorSuccessCount : 0)}\n" +
                $"OilAttemptCount={(controller != null ? controller.OilAttemptCount : 0)}\n" +
                $"OilSuccessCount={(controller != null ? controller.OilSuccessCount : 0)}\n" +
                $"EraserAttemptCount={(controller != null ? controller.EraserAttemptCount : 0)}\n" +
                $"EraserSuccessCount={(controller != null ? controller.EraserSuccessCount : 0)}\n" +
                $"PaletteKnifeAttemptCount={(controller != null ? controller.PaletteKnifeAttemptCount : 0)}\n" +
                $"PaletteKnifeConnectionCount={(controller != null ? controller.PaletteKnifeConnectionCount : 0)}\n" +
                $"PaletteKnifeBreakCount={(controller != null ? controller.PaletteKnifeBreakCount : 0)}\n" +
                $"RejectedCounterplayCount={(controller != null ? controller.RejectedCounterplayCount : 0)}\n" +
                $"RouteWatercolorBendCount={(route != null ? route.WatercolorBendCount : 0)}\n" +
                $"RouteEraserRemoveCount={(route != null ? route.EraserRemoveCount : 0)}\n" +
                $"RoutePaletteKnifeConnectionCount={(route != null ? route.PaletteKnifeConnectionCount : 0)}\n" +
                $"RoutePaletteKnifeBreakCount={(route != null ? route.PaletteKnifeBreakCount : 0)}\n" +
                $"RouteLastReaction={(route != null ? route.LastReaction : "N/A")}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "WatercolorCounterplayEnabled=True\n" +
                "OilPaintCounterplayEnabled=True\n" +
                "EraserCounterplayEnabled=True\n" +
                "PaletteKnifeRouteEditingEnabled=True\n" +
                "PaletteKnifeUsesExistingUseToolActionReadOnly=True\n" +
                "AddsSecondPaletteKnifeInputOwner=False\n" +
                "PainterPrototypeBindingsTemporary=True\n" +
                "FinalPainterMaterialToolbarIntegrated=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : trace);

            EditorUtility.DisplayDialog(
                "M51.2 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeTraceWeaveCounterplayController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M51.2",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeTraceWeaveCounterplayController controller =
                FindOptional<
                    PrototypeTraceWeaveCounterplayController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M51.2] Controller bulunamadı.");
            }

            return controller;
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

            MonoBehaviour[] all =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < all.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    all[index];

                if (
                    candidate != null &&
                    candidate.GetType().Name ==
                        typeName &&
                    candidate.gameObject.scene.IsValid()
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
            Vector2 sizeDelta)
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

            image.color =
                new Color(
                    0.045f,
                    0.040f,
                    0.035f,
                    0.95f);

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
            Vector2 sizeDelta)
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

            text.color =
                new Color(
                    0.93f,
                    0.94f,
                    0.82f,
                    1f);

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
                    $"{name} RectTransform içermiyor.");
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
                    $"{typeof(T).Name} eklenemedi.");
            }

            return added;
        }

        private static T FindRequired<T>()
            where T : Component
        {
            T found =
                FindOptional<T>();

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} bulunamadı.");
            }

            return found;
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

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;

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
