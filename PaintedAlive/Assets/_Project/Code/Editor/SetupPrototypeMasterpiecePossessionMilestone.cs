#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpiecePossessionMilestone
    {
        private const string RootName =
            "M48_MasterpiecePossessionRiskSpikeD";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M48_MasterpiecePossessionPanel";

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
            "48 - Apply Masterpiece Possession Risk Spike D")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M48 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceWorldDeploymentController deployment =
                    FindRequired<
                        PrototypeMasterpieceWorldDeploymentController>();

                Camera mainCamera =
                    Camera.main;

                if (mainCamera == null)
                {
                    mainCamera =
                        FindRequired<Camera>();
                }

                MonoBehaviour painterBrush =
                    FindMonoBehaviourByTypeName(
                        "PainterBrushController");

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

                PrototypeMasterpiecePossessionController controller =
                    GetOrAdd<
                        PrototypeMasterpiecePossessionController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, 112f),
                        new Vector2(660f, 104f),
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
                        "BAŞ YAPIT • DOĞRUDAN KONTROL",
                        14,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.69f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(16f, -10f),
                        new Vector2(-28f, -8f),
                        Cyan);

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Önce M47 ile deploy et.",
                        11,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.25f),
                        new Vector2(1f, 0.72f),
                        new Vector2(0f, 1f),
                        new Vector2(16f, -2f),
                        new Vector2(-28f, -6f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "P • SAHİPLEN",
                        10,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.27f),
                        new Vector2(0f, 0f),
                        new Vector2(16f, 7f),
                        new Vector2(-28f, -3f),
                        Orange);

                controller.Configure(
                    deployment,
                    mainCamera,
                    painterBrush,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    group);

                EditorUtility.SetDirty(
                    title);

                EditorUtility.SetDirty(
                    state);

                EditorUtility.SetDirty(
                    controls);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M48 Setup] Masterpiece Possession Risk Spike D ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Deployment={GetHierarchyPath(deployment.transform)}\n" +
                    $"MainCamera={GetHierarchyPath(mainCamera.transform)}\n" +
                    $"PainterBrush={Describe(painterBrush)}\n" +
                    "KeyboardMouseOnly=True\n" +
                    "PossessionInput=P\n" +
                    "MovementInput=WASD\n" +
                    "LookInput=MouseDelta\n" +
                    "AttackPreviewInput=LeftMouse\n" +
                    "ReleaseInput=P/Escape\n" +
                    "KinematicMovement=True\n" +
                    "MovementLeash=True\n" +
                    "EnvironmentQueryCollision=True\n" +
                    "PainterInputSuspendedDuringPossession=True\n" +
                    "CinemachineBrainTemporarilySuspended=True\n" +
                    "M45KnownInputBlockersReused=True\n" +
                    "PainterRoleBehaviourListsReflected=True\n" +
                    "MainCameraDetachedDuringPossession=True\n" +
                    "CameraWrittenInLateUpdate=True\n" +
                    "PainterCameraRigFrozen=True\n" +
                    "AttackTelegraphPreview=True\n" +
                    "PerformanceTelemetry=True\n" +
                    "BossAIEnabled=False\n" +
                    "HitAuthorityEnabled=False\n" +
                    "DamageAuthorityEnabled=False\n" +
                    "ModerationLayerEnabled=False\n" +
                    "SafeSilhouetteFallbackEnabled=False\n" +
                    "NetworkIntegrationParked=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M48 Hazır",
                    "Baş Yapıt sahiplenme Risk Spike D kuruldu.\n\n" +
                    "Play Mode: V deploy → P sahiplen.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M48 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48 - Toggle Possession (Play Mode)")]
        public static void TogglePossession()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M48 Possession",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return;
            }

            PrototypeMasterpiecePossessionController controller =
                FindOptional<
                    PrototypeMasterpiecePossessionController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M48 Direct Test] Controller bulunamadı.");

                return;
            }

            controller.TogglePossession();

            Debug.Log(
                "[M48 Direct Test]\n" +
                $"Possessed={controller.Possessed}\n" +
                $"PossessionCount={controller.PossessionCount}\n" +
                $"ReleaseCount={controller.ReleaseCount}\n" +
                $"LastAction={controller.LastAction}",
                controller);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48 - Trigger Attack Preview (Play Mode)")]
        public static void TriggerAttackPreview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M48 Attack Preview",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return;
            }

            PrototypeMasterpiecePossessionController controller =
                FindOptional<
                    PrototypeMasterpiecePossessionController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M48 Attack Test] Controller bulunamadı.");

                return;
            }

            controller.TriggerAttackPreview();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "48 - Diagnose Masterpiece Possession")]
        public static void Diagnose()
        {
            PrototypeMasterpiecePossessionController controller =
                FindOptional<
                    PrototypeMasterpiecePossessionController>();

            PrototypeMasterpieceWorldDeploymentController deployment =
                FindOptional<
                    PrototypeMasterpieceWorldDeploymentController>();

            string report =
                "[M48 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"Deployment={(deployment != null ? "OK" : "MISSING")}\n" +
                $"WorldInstanceActive={(deployment != null && deployment.WorldInstanceActive)}\n" +
                $"Possessed={(controller != null && controller.Possessed)}\n" +
                $"InputActionsActive={(controller != null && controller.InputActionsActive)}\n" +
                $"CameraOverrideActive={(controller != null && controller.CameraOverrideActive)}\n" +
                $"InputAuthorityActive={(controller != null && controller.InputAuthorityActive)}\n" +
                $"AttackPreviewActive={(controller != null && controller.AttackPreviewActive)}\n" +
                $"PossessionCount={(controller != null ? controller.PossessionCount : 0)}\n" +
                $"ReleaseCount={(controller != null ? controller.ReleaseCount : 0)}\n" +
                $"RejectedPossessionCount={(controller != null ? controller.RejectedPossessionCount : 0)}\n" +
                $"MovementStepCount={(controller != null ? controller.MovementStepCount : 0)}\n" +
                $"BlockedMovementCount={(controller != null ? controller.BlockedMovementCount : 0)}\n" +
                $"TravelledDistance={(controller != null ? controller.TravelledDistance : 0f):F2}\n" +
                $"AttackPreviewCount={(controller != null ? controller.AttackPreviewCount : 0)}\n" +
                $"BlockedAttackCount={(controller != null ? controller.BlockedAttackCount : 0)}\n" +
                $"InputConflictCount={(controller != null ? controller.InputConflictCount : 0)}\n" +
                $"SuppressedBehaviourCount={(controller != null ? controller.SuppressedBehaviourCount : 0)}\n" +
                $"M45KnownBlockerCount={(controller != null ? controller.M45KnownBlockerCount : 0)}\n" +
                $"PainterRoleListBlockerCount={(controller != null ? controller.PainterRoleListBlockerCount : 0)}\n" +
                $"CameraRigBehaviourCount={(controller != null ? controller.CameraRigBehaviourCount : 0)}\n" +
                $"FrozenAuthorityTransformCount={(controller != null ? controller.FrozenAuthorityTransformCount : 0)}\n" +
                $"CameraReparented={(controller != null && controller.CameraReparented)}\n" +
                $"CameraLateWriteCount={(controller != null ? controller.CameraLateWriteCount : 0)}\n" +
                $"CameraAuthorityConflictCount={(controller != null ? controller.CameraAuthorityConflictCount : 0)}\n" +
                $"AverageFrameMilliseconds={(controller != null ? controller.AverageFrameMilliseconds : 0f):F2}\n" +
                $"MaximumFrameMilliseconds={(controller != null ? controller.MaximumFrameMilliseconds : 0f):F2}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "KeyboardMouseOnly=True\n" +
                "KinematicMovement=True\n" +
                "MovementLeash=True\n" +
                "EnvironmentQueryCollision=True\n" +
                "M45KnownInputBlockersReused=True\n" +
                "PainterRoleBehaviourListsReflected=True\n" +
                "MainCameraDetachedDuringPossession=True\n" +
                "CameraWrittenInLateUpdate=True\n" +
                "PainterCameraRigFrozen=True\n" +
                "AttackTelegraphPreview=True\n" +
                "BossAIEnabled=False\n" +
                "HitAuthorityEnabled=False\n" +
                "DamageAuthorityEnabled=False\n" +
                "ModerationLayerEnabled=False\n" +
                "SafeSilhouetteFallbackEnabled=False\n" +
                "NetworkIntegrationParked=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : deployment);

            EditorUtility.DisplayDialog(
                "M48 Diagnose",
                report,
                "Tamam");
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

        private static MonoBehaviour FindMonoBehaviourByTypeName(
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

                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.GetType().Name ==
                        typeName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string Describe(
            UnityEngine.Object value)
        {
            return value != null
                ? value.name
                : "None";
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
