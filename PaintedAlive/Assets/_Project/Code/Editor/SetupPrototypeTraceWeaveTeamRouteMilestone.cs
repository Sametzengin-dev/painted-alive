#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeTraceWeaveTeamRouteMilestone
    {
        private const string RootName =
            "M51_1_TraceWeaveTeamRouteRiskSpikeB2";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M51_1_TraceWeaveTeamRoutePanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.1 - Apply Trace Weave Team Route Risk Spike B2")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M51.1 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeTraceWeaveController traceWeave =
                    FindRequired<PrototypeTraceWeaveController>();

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour("FigureMotor");

                MonoBehaviour clarityState =
                    FindRelatedMonoBehaviour(
                        figureMotor.transform,
                        "FigureClarityState");

                GameObject unifiedCanvas =
                    GameObject.Find(UnifiedCanvasName);

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
                    GetOrCreateRoot(RootName);

                PrototypeTraceWeaveTeamRouteController controller =
                    GetOrAdd<PrototypeTraceWeaveTeamRouteController>(
                        root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(20f, -314f),
                        new Vector2(420f, 124f));

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
                        "İZ DOKUMA • TAKIM ROTASI / YÜK",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.72f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -9f),
                        new Vector2(-24f, -4f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Aktif İz Dokuma bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.25f),
                        new Vector2(1f, 0.74f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "ROTA YÖNÜNDE HAREKET ET",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 6f),
                        new Vector2(-24f, -2f));

                controller.Configure(
                    traceWeave,
                    figureMotor,
                    figureMotor.transform,
                    clarityState,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(root.scene);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M51.1 Setup] Trace Weave Team Route ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"TraceWeave={GetHierarchyPath(traceWeave.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    "TeamRouteUseEnabled=True\n" +
                    "DirectionalFollowAssistEnabled=True\n" +
                    "FollowAssistSpeed=0.85\n" +
                    "FigureEquivalentLoadKg=70\n" +
                    "HeavyLoadThresholdKg=150\n" +
                    "OverloadHoldSeconds=1.15\n" +
                    "HeavyLoadCollapseEnabled=True\n" +
                    "FullStainReceivesAssist=False\n" +
                    "MutatesFigureMovementConfig=False\n" +
                    "AppliesRawTransformTeleport=False\n" +
                    "PainterCounterplayIntegrated=False\n" +
                    "NetworkAuthorityEnabled=False",
                    controller);

                EditorUtility.DisplayDialog(
                    "M51.1 Hazır",
                    "İz Dokuma takım rotası kullanımı, yön takip desteği ve " +
                    "ağır yük altında erken çözülme prototipi kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "M51.1 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.1 - Simulate Heavy Load (Play Mode)")]
        public static void SimulateHeavyLoad()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M51.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");
                return;
            }

            PrototypeTraceWeaveTeamRouteController controller =
                FindOptional<PrototypeTraceWeaveTeamRouteController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M51.1] Team Route controller bulunamadı.");
                return;
            }

            controller.ApplySimulatedHeavyLoad(
                100f,
                2f);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.1 - Clear Simulated Load (Play Mode)")]
        public static void ClearSimulatedLoad()
        {
            PrototypeTraceWeaveTeamRouteController controller =
                FindOptional<PrototypeTraceWeaveTeamRouteController>();

            controller?.ClearSimulatedLoad();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "51.1 - Diagnose Trace Weave Team Route")]
        public static void Diagnose()
        {
            PrototypeTraceWeaveTeamRouteController controller =
                FindOptional<PrototypeTraceWeaveTeamRouteController>();

            PrototypeTraceWeaveController trace =
                FindOptional<PrototypeTraceWeaveController>();

            PrototypeTraceWeaveRoute route =
                trace != null
                    ? trace.ActiveRoute
                    : null;

            string report =
                "[M51.1 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ResolvedRole={(controller != null ? controller.ResolvedRole : "N/A")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"ClarityLevel={(controller != null ? controller.ClarityLevel : "N/A")}\n" +
                $"FullStainBlocked={(controller != null && controller.FullStainBlocked)}\n" +
                $"RouteAvailable={(controller != null && controller.RouteAvailable)}\n" +
                $"RouteVisualResolved={(controller != null && controller.RouteVisualResolved)}\n" +
                $"RouteActive={(route != null && route.ActiveRoute)}\n" +
                $"VelocityContractResolved={(controller != null && controller.VelocityContractResolved)}\n" +
                $"VelocityFallbackActive={(controller != null && controller.VelocityFallbackActive)}\n" +
                $"OnRoute={(controller != null && controller.OnRoute)}\n" +
                $"DirectionAligned={(controller != null && controller.DirectionAligned)}\n" +
                $"AssistActive={(controller != null && controller.AssistActive)}\n" +
                $"MovementSpeed={(controller != null ? controller.MovementSpeed : 0f):F2}\n" +
                $"DirectionAlignment={(controller != null ? controller.DirectionAlignment : 0f):F2}\n" +
                $"RouteDistance={(controller != null ? controller.RouteDistance : -1f):F2}\n" +
                $"RouteProgress={(controller != null ? controller.RouteProgress : 0f):F2}\n" +
                $"CurrentLoadKg={(controller != null ? controller.CurrentLoadKg : 0f):F1}\n" +
                $"Overloaded={(controller != null && controller.Overloaded)}\n" +
                $"OverloadProgress={(controller != null ? controller.OverloadProgress : 0f):F2}\n" +
                $"RouteEnterCount={(controller != null ? controller.RouteEnterCount : 0)}\n" +
                $"RouteExitCount={(controller != null ? controller.RouteExitCount : 0)}\n" +
                $"AssistFrameCount={(controller != null ? controller.AssistFrameCount : 0)}\n" +
                $"AssistedDistance={(controller != null ? controller.AssistedDistance : 0f):F2}\n" +
                $"OverloadCollapseCount={(controller != null ? controller.OverloadCollapseCount : 0)}\n" +
                $"LoadProbeCount={(controller != null ? controller.LoadProbeCount : 0)}\n" +
                $"SimulatedLoadUseCount={(controller != null ? controller.SimulatedLoadUseCount : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "TeamRouteUseEnabled=True\n" +
                "DirectionalFollowAssistEnabled=True\n" +
                "HeavyLoadCollapseEnabled=True\n" +
                "FullStainReceivesAssist=False\n" +
                "UsesFigureMotorVelocityReadOnly=True\n" +
                "MutatesFigureMovementConfig=False\n" +
                "AppliesRawTransformTeleport=False\n" +
                "PainterCounterplayIntegrated=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : trace);

            EditorUtility.DisplayDialog(
                "M51.1 Diagnose",
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
                root.GetComponentsInChildren<MonoBehaviour>(true);

            for (int index = 0;
                 index < related.Length;
                 index++)
            {
                MonoBehaviour candidate = related[index];

                if (
                    candidate != null &&
                    candidate.GetType().Name == typeName
                )
                {
                    return candidate;
                }
            }

            related =
                root.GetComponentsInParent<MonoBehaviour>(true);

            for (int index = 0;
                 index < related.Length;
                 index++)
            {
                MonoBehaviour candidate = related[index];

                if (
                    candidate != null &&
                    candidate.GetType().Name == typeName
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
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour candidate = behaviours[index];

                if (
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.GetType().Name == typeName
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
            GameObject existing = GameObject.Find(name);

            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(name);
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

            Image image = GetOrAdd<Image>(rect.gameObject);
            image.color = new Color(
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

            Text text = GetOrAdd<Text>(rect.gameObject);
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(
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
            Transform existing = parent.Find(name);

            GameObject child = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform));

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(
                    child,
                    $"Create {name}");
                child.transform.SetParent(parent, false);
            }

            RectTransform rect =
                child.GetComponent<RectTransform>();

            if (rect == null)
            {
                throw new InvalidOperationException(
                    $"{name} RectTransform içermiyor.");
            }

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
            T existing = target.GetComponent<T>();

            if (existing != null)
            {
                return existing;
            }

            T added = Undo.AddComponent<T>(target);

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
            T found = FindOptional<T>();

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
                T candidate = objects[index];

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
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
#endif
