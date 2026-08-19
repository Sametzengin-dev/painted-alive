#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceVisualSafetyMilestone
    {
        private const string RootName =
            "M48_1_MasterpieceVisualSafetyRiskSpikeE";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M48_1_MasterpieceVisualSafetyPanel";

        private static readonly Color Ink =
            new Color(
                0.055f,
                0.050f,
                0.045f,
                0.95f);

        private static readonly Color Paper =
            new Color(
                0.94f,
                0.90f,
                0.80f,
                1f);

        private static readonly Color Cyan =
            new Color(
                0.08f,
                0.70f,
                0.75f,
                1f);

        private static readonly Color Orange =
            new Color(
                1f,
                0.54f,
                0.06f,
                1f);

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.1 - Apply Masterpiece Visual Safety Risk Spike E")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M48.1 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceWorldDeploymentController deployment =
                    FindRequired<
                        PrototypeMasterpieceWorldDeploymentController>();

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

                PrototypeMasterpieceVisualSafetyController controller =
                    GetOrAdd<
                        PrototypeMasterpieceVisualSafetyController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(1f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(-20f, 214f),
                        new Vector2(348f, 126f),
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
                        "BAŞ YAPIT • GÖRSEL GÜVENLİK",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.72f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -10f),
                        new Vector2(-24f, -8f),
                        Orange);

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Dünya Baş Yapıtı bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.26f),
                        new Vector2(1f, 0.74f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -2f),
                        new Vector2(-24f, -4f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "K • GÖRÜNÜM   J • RAPOR/GİZLE   U • SIFIRLA",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 7f),
                        new Vector2(-24f, -3f),
                        Cyan);

                controller.Configure(
                    deployment,
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
                    "[M48.1 Setup] Masterpiece Visual Safety Risk Spike E ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Deployment={GetHierarchyPath(deployment.transform)}\n" +
                    "KeyboardMouseOnly=True\n" +
                    "CycleVisualModeInput=K\n" +
                    "LocalReportHideInput=J\n" +
                    "PrototypeClearReportInput=U\n" +
                    "OriginalStrokeVisibilityControllable=True\n" +
                    "SafeSilhouetteFromFixedProxies=True\n" +
                    "ReadyPuppetFallback=True\n" +
                    "ColorIndependentEndpointShapes=True\n" +
                    "GameplayProxyModified=False\n" +
                    "CapabilityStateModified=False\n" +
                    "PossessionMovementModified=False\n" +
                    "SemanticContentScreeningPerformed=False\n" +
                    "NetworkReportSubmitted=False\n" +
                    "ReplayEventSubmitted=False\n" +
                    "LocalPresentationOnly=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M48.1 Hazır",
                    "Görsel güvenlik, yerel rapor/gizleme ve hazır kukla " +
                    "fallback prototipi kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M48.1 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.1 - Cycle Visual Mode (Play Mode)")]
        public static void CycleVisualMode()
        {
            PrototypeMasterpieceVisualSafetyController controller =
                FindPlayModeController();

            controller?.CycleVisualMode();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.1 - Report and Hide Locally (Play Mode)")]
        public static void ReportAndHide()
        {
            PrototypeMasterpieceVisualSafetyController controller =
                FindPlayModeController();

            controller?.ReportAndHideLocally();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.1 - Clear Local Report (Play Mode)")]
        public static void ClearLocalReport()
        {
            PrototypeMasterpieceVisualSafetyController controller =
                FindPlayModeController();

            controller?.ClearLocalReportForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.1 - Diagnose Masterpiece Visual Safety")]
        public static void Diagnose()
        {
            PrototypeMasterpieceVisualSafetyController controller =
                FindOptional<
                    PrototypeMasterpieceVisualSafetyController>();

            PrototypeMasterpieceWorldDeploymentController deployment =
                FindOptional<
                    PrototypeMasterpieceWorldDeploymentController>();

            PrototypeMasterpieceWorldInstance instance =
                deployment != null
                    ? deployment.ActiveInstance
                    : null;

            string report =
                "[M48.1 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"WorldInstanceActive={(instance != null)}\n" +
                $"InputActionsActive={(controller != null && controller.InputActionsActive)}\n" +
                $"CurrentMode={(controller != null ? controller.CurrentMode.ToString() : "N/A")}\n" +
                $"BoundSnapshotHash={(controller != null ? controller.BoundSnapshotHash : 0)}\n" +
                $"ReportActive={(controller != null && controller.ReportActive)}\n" +
                $"AutomaticFallbackActive={(controller != null && controller.AutomaticFallbackActive)}\n" +
                $"OriginalVisualAvailable={(controller != null && controller.OriginalVisualAvailable)}\n" +
                $"OriginalStrokeRendererCount={(instance != null ? instance.OriginalStrokeRendererCount : 0)}\n" +
                $"OriginalStrokeVisualsVisible={(instance != null && instance.OriginalStrokeVisualsVisible)}\n" +
                $"ModeChangeCount={(controller != null ? controller.ModeChangeCount : 0)}\n" +
                $"LocalReportCount={(controller != null ? controller.LocalReportCount : 0)}\n" +
                $"LocalHideCount={(controller != null ? controller.LocalHideCount : 0)}\n" +
                $"ClearReportCount={(controller != null ? controller.ClearReportCount : 0)}\n" +
                $"LocalReportRecordCount={(controller != null ? controller.LocalReportRecordCount : 0)}\n" +
                $"AutomaticFallbackCount={(controller != null ? controller.AutomaticFallbackCount : 0)}\n" +
                $"SafeVisualUpdateCount={(controller != null ? controller.SafeVisualUpdateCount : 0)}\n" +
                $"ReadyPuppetUpdateCount={(controller != null ? controller.ReadyPuppetUpdateCount : 0)}\n" +
                $"PreservedColliderCount={(controller != null ? controller.PreservedColliderCount : 0)}\n" +
                $"PreservedCapabilityCount={(controller != null ? controller.PreservedCapabilityCount : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "GameplayProxyModified=False\n" +
                "CapabilityStateModified=False\n" +
                "PossessionMovementModified=False\n" +
                "SemanticContentScreeningPerformed=False\n" +
                "NetworkReportSubmitted=False\n" +
                "ReplayEventSubmitted=False\n" +
                "LocalPresentationOnly=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : deployment);

            EditorUtility.DisplayDialog(
                "M48.1 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeMasterpieceVisualSafetyController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M48.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeMasterpieceVisualSafetyController controller =
                FindOptional<
                    PrototypeMasterpieceVisualSafetyController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M48.1 Direct Test] Controller bulunamadı.");
            }

            return controller;
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
                    $"{GetHierarchyPath(child.transform)} " +
                    "RectTransform içermiyor.");
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
                    GetHierarchyPath(
                        target.transform));
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

                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid())
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
