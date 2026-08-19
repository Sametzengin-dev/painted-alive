#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceSensoryPolishMilestone
    {
        private const string RootName =
            "M48_2_MasterpieceSensoryPolishRiskSpikeF";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M48_2_MasterpieceSensoryPolishPanel";

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
            "48.2 - Apply Masterpiece Sensory Polish Risk Spike F")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M48.2 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceWorldDeploymentController deployment =
                    FindRequired<
                        PrototypeMasterpieceWorldDeploymentController>();

                PrototypeMasterpiecePossessionController possession =
                    FindRequired<
                        PrototypeMasterpiecePossessionController>();

                PrototypeMasterpieceVisualSafetyController visualSafety =
                    FindRequired<
                        PrototypeMasterpieceVisualSafetyController>();

                Camera mainCamera =
                    Camera.main;

                if (mainCamera == null)
                {
                    mainCamera =
                        FindRequired<Camera>();
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

                PrototypeMasterpieceSensoryPolishController controller =
                    GetOrAdd<
                        PrototypeMasterpieceSensoryPolishController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(20f, -20f),
                        new Vector2(370f, 144f),
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
                        "BAŞ YAPIT • DUYUSAL POLISH",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.73f),
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
                        new Vector2(0f, 0.25f),
                        new Vector2(1f, 0.75f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -2f),
                        new Vector2(-24f, -4f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "L • KALİTE   M • SES",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.27f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 7f),
                        new Vector2(-24f, -3f),
                        Cyan);

                controller.Configure(
                    deployment,
                    possession,
                    visualSafety,
                    mainCamera,
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
                    "[M48.2 Setup] Masterpiece Sensory Polish Risk Spike F ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Deployment={GetHierarchyPath(deployment.transform)}\n" +
                    $"Possession={GetHierarchyPath(possession.transform)}\n" +
                    $"VisualSafety={GetHierarchyPath(visualSafety.transform)}\n" +
                    $"MainCamera={GetHierarchyPath(mainCamera.transform)}\n" +
                    "KeyboardMouseOnly=True\n" +
                    "QualityInput=L\n" +
                    "AudioMuteInput=M\n" +
                    "RequestedQualityDefault=Auto\n" +
                    "QualityTiers=Full,Reduced,Minimal\n" +
                    "FrameTimeHysteresis=True\n" +
                    "DistanceBasedFootsteps=True\n" +
                    "PossessionCue=True\n" +
                    "ReleaseCue=True\n" +
                    "AttackAnticipationCue=True\n" +
                    "AttackReleaseCue=True\n" +
                    "VisualModeCue=True\n" +
                    "RuntimeGeneratedAudio=True\n" +
                    "ExternalAudioAssetsRequired=False\n" +
                    "RuntimePoolingEnabled=True\n" +
                    "GameplayStateWrites=False\n" +
                    "GameplayProxyModified=False\n" +
                    "CapabilityStateModified=False\n" +
                    "ClarityModified=False\n" +
                    "HitAuthorityEnabled=False\n" +
                    "DamageAuthorityEnabled=False\n" +
                    "BossAIEnabled=False\n" +
                    "FinalLocomotionAnimationIncluded=False\n" +
                    "NetworkIntegrationParked=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M48.2 Hazır",
                    "Sahiplenme, adım, saldırı ve görünüm geçişleri için " +
                    "pool tabanlı ses/VFX ve AUTO kalite telemetry kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M48.2 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.2 - Cycle Polish Quality (Play Mode)")]
        public static void CycleQuality()
        {
            PrototypeMasterpieceSensoryPolishController controller =
                FindPlayModeController();

            controller?.CycleQualityMode();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.2 - Toggle Procedural Audio (Play Mode)")]
        public static void ToggleAudio()
        {
            PrototypeMasterpieceSensoryPolishController controller =
                FindPlayModeController();

            controller?.ToggleAudioMute();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.2 - Trigger Sensory Test Burst (Play Mode)")]
        public static void TriggerTestBurst()
        {
            PrototypeMasterpieceSensoryPolishController controller =
                FindPlayModeController();

            controller?.TriggerTestBurst();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48.2 - Diagnose Masterpiece Sensory Polish")]
        public static void Diagnose()
        {
            PrototypeMasterpieceSensoryPolishController controller =
                FindOptional<
                    PrototypeMasterpieceSensoryPolishController>();

            PrototypeMasterpieceWorldDeploymentController deployment =
                FindOptional<
                    PrototypeMasterpieceWorldDeploymentController>();

            string report =
                "[M48.2 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"WorldInstanceActive={(deployment != null && deployment.WorldInstanceActive)}\n" +
                $"InputActionsActive={(controller != null && controller.InputActionsActive)}\n" +
                $"RequestedQuality={(controller != null ? controller.RequestedQuality.ToString() : "N/A")}\n" +
                $"ResolvedTier={(controller != null ? controller.ResolvedTier.ToString() : "N/A")}\n" +
                $"AudioMuted={(controller != null && controller.AudioMuted)}\n" +
                $"SmoothedFrameMilliseconds={(controller != null ? controller.SmoothedFrameMilliseconds : 0f):F2}\n" +
                $"PossessionCueCount={(controller != null ? controller.PossessionCueCount : 0)}\n" +
                $"ReleaseCueCount={(controller != null ? controller.ReleaseCueCount : 0)}\n" +
                $"FootstepCueCount={(controller != null ? controller.FootstepCueCount : 0)}\n" +
                $"AttackStartCueCount={(controller != null ? controller.AttackStartCueCount : 0)}\n" +
                $"AttackReleaseCueCount={(controller != null ? controller.AttackReleaseCueCount : 0)}\n" +
                $"VisualModeCueCount={(controller != null ? controller.VisualModeCueCount : 0)}\n" +
                $"ReportCueCount={(controller != null ? controller.ReportCueCount : 0)}\n" +
                $"QualityChangeCount={(controller != null ? controller.QualityChangeCount : 0)}\n" +
                $"AutomaticDowngradeCount={(controller != null ? controller.AutomaticDowngradeCount : 0)}\n" +
                $"AutomaticUpgradeCount={(controller != null ? controller.AutomaticUpgradeCount : 0)}\n" +
                $"PulsePoolCount={(controller != null ? controller.PulsePoolCount : 0)}\n" +
                $"ActivePulseCount={(controller != null ? controller.ActivePulseCount : 0)}\n" +
                $"PeakActivePulseCount={(controller != null ? controller.PeakActivePulseCount : 0)}\n" +
                $"DroppedPulseCount={(controller != null ? controller.DroppedPulseCount : 0)}\n" +
                $"AudioSourcePoolCount={(controller != null ? controller.AudioSourcePoolCount : 0)}\n" +
                $"GeneratedClipCount={(controller != null ? controller.GeneratedClipCount : 0)}\n" +
                $"AudioPlayCount={(controller != null ? controller.AudioPlayCount : 0)}\n" +
                $"TestBurstCount={(controller != null ? controller.TestBurstCount : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "DistanceBasedFootsteps=True\n" +
                "FrameTimeHysteresis=True\n" +
                "RuntimePoolingEnabled=True\n" +
                "ExternalAudioAssetsRequired=False\n" +
                "GameplayStateWrites=False\n" +
                "GameplayProxyModified=False\n" +
                "CapabilityStateModified=False\n" +
                "ClarityModified=False\n" +
                "HitAuthorityEnabled=False\n" +
                "DamageAuthorityEnabled=False\n" +
                "BossAIEnabled=False\n" +
                "FinalLocomotionAnimationIncluded=False\n" +
                "NetworkIntegrationParked=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : deployment);

            EditorUtility.DisplayDialog(
                "M48.2 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeMasterpieceSensoryPolishController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M48.2",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeMasterpieceSensoryPolishController controller =
                FindOptional<
                    PrototypeMasterpieceSensoryPolishController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M48.2 Direct Test] Controller bulunamadı.");
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
