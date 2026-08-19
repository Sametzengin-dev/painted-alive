#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.Painters.Masterpiece;
using PaintedAlive.Painters.SideCanvas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceAssemblyMilestone
    {
        private const string M45RootName =
            "M45_LivingSideCanvasRiskSpikeA";

        private const string M45CanvasName =
            "M45_LivingSideCanvasCanvas";

        private const string PreviewPanelName =
            "PreviewPanel";

        private static readonly Color Ink =
            new Color(
                0.055f,
                0.050f,
                0.045f,
                0.96f);

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
            "46 - Apply Masterpiece Cutout Assembly Risk Spike B")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M46 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeLivingSideCanvasController sideCanvas =
                    FindRequired<
                        PrototypeLivingSideCanvasController>();

                GameObject m45Root =
                    GameObject.Find(
                        M45RootName);

                if (m45Root == null)
                {
                    throw new InvalidOperationException(
                        "M45 root bulunamadı. " +
                        "Çalışan M45 sahnesini aç.");
                }

                Transform canvas =
                    m45Root.transform.Find(
                        M45CanvasName);

                if (canvas == null)
                {
                    throw new InvalidOperationException(
                        "M45 canvas bulunamadı.");
                }

                Transform previewPanel =
                    canvas.Find(
                        PreviewPanelName);

                if (previewPanel == null)
                {
                    throw new InvalidOperationException(
                        "M45 PreviewPanel bulunamadı.");
                }

                PrototypeMasterpieceAssemblyController controller =
                    GetOrAdd<
                        PrototypeMasterpieceAssemblyController>(
                            m45Root);

                RectTransform assemblyRect =
                    GetOrCreateRect(
                        previewPanel,
                        "M46_AssemblyGraphic",
                        new Vector2(0.04f, 0.29f),
                        new Vector2(0.96f, 0.84f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero);

                RawImage assemblyImage =
                    GetOrAdd<RawImage>(
                        assemblyRect.gameObject);

                assemblyImage.color =
                    Color.white;

                assemblyImage.raycastTarget =
                    false;

                PrototypeMasterpieceAssemblyRenderer renderer =
                    GetOrAdd<
                        PrototypeMasterpieceAssemblyRenderer>(
                            assemblyRect.gameObject);

                RectTransform statusPanel =
                    CreatePanel(
                        previewPanel,
                        "M46_StatusPanel",
                        new Vector2(0.03f, 0.02f),
                        new Vector2(0.97f, 0.28f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero,
                        Ink);

                CanvasGroup statusGroup =
                    GetOrAdd<CanvasGroup>(
                        statusPanel.gameObject);

                statusGroup.alpha = 1f;
                statusGroup.interactable = false;
                statusGroup.blocksRaycasts = false;

                Text statusText =
                    CreateText(
                        statusPanel,
                        "StatusText",
                        "M46 • PARÇALI BAŞ YAPIT",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.44f),
                        new Vector2(0.63f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -10f),
                        new Vector2(-20f, -12f),
                        Paper);

                Text capabilityText =
                    CreateText(
                        statusPanel,
                        "CapabilityText",
                        "○ ÇEKİRDEK\n○ ALGI\n○ SOL SALDIRI\n○ SAĞ SALDIRI\n○ SOL HAREKET\n○ SAĞ HAREKET",
                        11,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0.64f, 0.20f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(4f, -9f),
                        new Vector2(-16f, -8f),
                        Cyan);

                Text controlsText =
                    CreateText(
                        statusPanel,
                        "ControlsText",
                        "B • ORGAN SÖK   N • GERİ TAK   P • PROXY\nWORLD SPAWN KAPALI • AI YOK",
                        10,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.42f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 8f),
                        new Vector2(-24f, -4f),
                        Orange);

                controller.Configure(
                    sideCanvas,
                    renderer,
                    statusGroup,
                    statusText,
                    capabilityText,
                    controlsText);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    renderer);

                EditorUtility.SetDirty(
                    statusGroup);

                EditorSceneManager.MarkSceneDirty(
                    m45Root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M46 Setup] Masterpiece Cutout Assembly Risk Spike B ready.\n" +
                    $"M45={GetHierarchyPath(sideCanvas.transform)}\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Renderer={GetHierarchyPath(renderer.transform)}\n" +
                    $"ControllerCount={CountSceneObjects<PrototypeMasterpieceAssemblyController>()}\n" +
                    $"RendererCount={CountSceneObjects<PrototypeMasterpieceAssemblyRenderer>()}\n" +
                    "SixPartAssembly=True\n" +
                    "StrokeDrivenCutout=True\n" +
                    "FixedProxyContract=True\n" +
                    "ProxyCountTarget=6\n" +
                    "DetachableOrgans=5\n" +
                    "CoreDetachable=False\n" +
                    "ExclusivePreviewOwnership=True\n" +
                    "M45PuppetHiddenOnlyWhileAssemblyVisible=True\n" +
                    "PreviewTitleSwapsByMode=True\n" +
                    "VisualShapeControlsPower=False\n" +
                    "GameplayCollidersCreated=False\n" +
                    "WorldSpawnEnabled=False\n" +
                    "BossAIEnabled=False\n" +
                    "M45FilesModified=False\n" +
                    "NetworkIntegrationParked=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M46 tamamlandı",
                    "M45 çizimi artık altı parçalı cutout gövde, " +
                    "sabit proxy sözleşmesi ve organ sökme testi üretir. " +
                    "Play Mode'da M45 demo taslağını aç ve B/N/P tuşlarını dene.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "M46 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "46 - Diagnose Masterpiece Cutout Assembly")]
        public static void Diagnose()
        {
            PrototypeMasterpieceAssemblyController controller =
                FindOptional<
                    PrototypeMasterpieceAssemblyController>();

            PrototypeMasterpieceAssemblyRenderer renderer =
                FindOptional<
                    PrototypeMasterpieceAssemblyRenderer>();

            Debug.Log(
                "[M46 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ControllerCount={CountSceneObjects<PrototypeMasterpieceAssemblyController>()}\n" +
                $"AssemblyReady={(controller != null && controller.AssemblyReady)}\n" +
                $"AssemblyRevision={(controller != null ? controller.AssemblyRevision : 0)}\n" +
                $"RebuildCount={(controller != null ? controller.RebuildCount : 0)}\n" +
                $"PartCount={(controller != null ? controller.PartCount : 0)}\n" +
                $"ProxyCount={(controller != null ? controller.ProxyCount : 0)}\n" +
                $"DetachedPartCount={(controller != null ? controller.DetachedPartCount : 0)}\n" +
                $"ActiveCapabilityCount={(controller != null ? controller.ActiveCapabilityCount : 0)}\n" +
                $"DetachActionCount={(controller != null ? controller.DetachActionCount : 0)}\n" +
                $"RestoreActionCount={(controller != null ? controller.RestoreActionCount : 0)}\n" +
                $"ProxyOverlayVisible={(controller != null && controller.ProxyOverlayVisible)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                $"Renderer={(renderer != null ? "OK" : "MISSING")}\n" +
                $"RendererRawImage={(renderer != null && renderer.UsesStandardRawImage)}\n" +
                $"RendererTextureReady={(renderer != null && renderer.TextureReady)}\n" +
                $"AssemblyVisible={(renderer != null && renderer.AssemblyVisible)}\n" +
                $"RenderedPartCount={(renderer != null ? renderer.RenderedPartCount : 0)}\n" +
                $"RendererRebuildCount={(renderer != null ? renderer.RebuildCount : 0)}\n" +
                $"ExclusivePreviewOwnership={(renderer != null && renderer.ExclusivePreviewOwnershipActive)}\n" +
                $"M45PuppetSuppressed={(renderer != null && renderer.M45PuppetSuppressed)}\n" +
                $"PreviewTitleSwapped={(renderer != null && renderer.PreviewTitleSwapped)}\n" +
                "NoPreviewLayerOverlap=True\n" +
                "SixPartAssembly=True\n" +
                "FixedProxyContract=True\n" +
                "VisualShapeControlsPower=False\n" +
                "GameplayCollidersCreated=False\n" +
                "WorldSpawnEnabled=False\n" +
                "BossAIEnabled=False\n" +
                "M45FilesModified=False\n" +
                "NetworkIntegrationParked=True");
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
                parent.Find(name);

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
                UnityEngine.Object
                    .FindObjectsByType<T>(
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
