#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceAutonomousEncounterMilestone
    {
        private const string RootName =
            "M49_0_MasterpieceAutonomousEncounterBridge";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M49_0_MasterpieceAutonomousEncounterPanel";

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

        private static readonly Color Orange =
            new Color(
                1f,
                0.28f,
                0.04f,
                1f);

        private static readonly Color Cyan =
            new Color(
                0.08f,
                0.70f,
                0.75f,
                1f);

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Apply Masterpiece Autonomous Encounter Bridge")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M49.0 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceWorldDeploymentController deployment =
                    FindRequired<
                        PrototypeMasterpieceWorldDeploymentController>();

                PrototypeMasterpiecePossessionController possession =
                    FindRequired<
                        PrototypeMasterpiecePossessionController>();

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

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

                PrototypeMasterpieceAutonomousEncounterController controller =
                    GetOrAdd<
                        PrototypeMasterpieceAutonomousEncounterController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        new Vector2(20f, 214f),
                        new Vector2(390f, 158f),
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
                        "BAŞ YAPIT • OTONOM ENCOUNTER",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.76f),
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
                        new Vector2(0f, 0.24f),
                        new Vector2(1f, 0.78f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -2f),
                        new Vector2(-24f, -4f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "I • OTONOMİ   P • SAHİPLENME",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.25f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 7f),
                        new Vector2(-24f, -3f),
                        Cyan);

                controller.Configure(
                    deployment,
                    possession,
                    figureMotor.transform,
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
                    "[M49.0 Setup] Masterpiece Autonomous Encounter Bridge ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Deployment={GetHierarchyPath(deployment.transform)}\n" +
                    $"Possession={GetHierarchyPath(possession.transform)}\n" +
                    $"FigureTarget={GetHierarchyPath(figureMotor.transform)}\n" +
                    "KeyboardMouseOnly=True\n" +
                    "AutonomyToggleInput=I\n" +
                    "AutonomyStartsEnabled=True\n" +
                    "DirectControlTakesPriority=True\n" +
                    "RoutePatrol=True\n" +
                    "FigurePursuit=True\n" +
                    "PerceptionCapabilityAffectsDetection=True\n" +
                    "MovementCapabilitiesAffectSpeed=True\n" +
                    "AttackCapabilitiesSelectOrgan=True\n" +
                    "CoreCapabilityStopsAI=True\n" +
                    "AttackTelegraph=True\n" +
                    "AttackSwingPreview=True\n" +
                    "HitAuthorityEnabled=False\n" +
                    "ClarityWritesEnabled=False\n" +
                    "DamageAuthorityEnabled=False\n" +
                    "TriggerCollidersPreserved=True\n" +
                    "NetworkIntegrationParked=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M49.0 Hazır",
                    "Otonom devriye, Figür takibi, capability bozulması ve " +
                    "telegraph'lı saldırı ön izlemesi kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M49.0 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Toggle Autonomy (Play Mode)")]
        public static void ToggleAutonomy()
        {
            PrototypeMasterpieceAutonomousEncounterController controller =
                FindPlayModeController();

            controller?.ToggleAutonomy();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Force Attack Decision (Play Mode)")]
        public static void ForceAttack()
        {
            PrototypeMasterpieceAutonomousEncounterController controller =
                FindPlayModeController();

            controller?.ForceAttackDecision();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Disable Left Movement Capability (Play Mode)")]
        public static void DisableLeftMovement()
        {
            SetCapability(
                PrototypeMasterpiecePartKind.LeftContact,
                false);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Disable Left Attack Capability (Play Mode)")]
        public static void DisableLeftAttack()
        {
            SetCapability(
                PrototypeMasterpiecePartKind.LeftAttack,
                false);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Disable Perception Capability (Play Mode)")]
        public static void DisablePerception()
        {
            SetCapability(
                PrototypeMasterpiecePartKind.Head,
                false);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Disable Core Capability (Play Mode)")]
        public static void DisableCore()
        {
            SetCapability(
                PrototypeMasterpiecePartKind.Core,
                false);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Restore All Capabilities (Play Mode)")]
        public static void RestoreAllCapabilities()
        {
            PrototypeMasterpieceAutonomousEncounterController controller =
                FindPlayModeController();

            int changed =
                controller != null
                    ? controller.RestoreAllCapabilitiesForTest()
                    : 0;

            Debug.Log(
                "[M49.0 Capability Restore]\n" +
                $"Changed={changed}",
                controller);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.0 - Diagnose Masterpiece Autonomous Encounter")]
        public static void Diagnose()
        {
            PrototypeMasterpieceAutonomousEncounterController controller =
                FindOptional<
                    PrototypeMasterpieceAutonomousEncounterController>();

            PrototypeMasterpieceWorldDeploymentController deployment =
                FindOptional<
                    PrototypeMasterpieceWorldDeploymentController>();

            string report =
                "[M49.0 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"WorldInstanceActive={(deployment != null && deployment.WorldInstanceActive)}\n" +
                $"AutonomyEnabled={(controller != null && controller.AutonomyEnabled)}\n" +
                $"State={(controller != null ? controller.State.ToString() : "N/A")}\n" +
                $"InputActionsActive={(controller != null && controller.InputActionsActive)}\n" +
                $"TargetAvailable={(controller != null && controller.TargetAvailable)}\n" +
                $"TargetDetected={(controller != null && controller.TargetDetected)}\n" +
                $"TargetDistance={(controller != null ? controller.TargetDistance : -1f):F2}\n" +
                $"MovementCapabilities={(controller != null ? controller.ActiveMovementCapabilityCount : 0)}/2\n" +
                $"AttackCapabilities={(controller != null ? controller.ActiveAttackCapabilityCount : 0)}/2\n" +
                $"PerceptionActive={(controller != null && controller.PerceptionActive)}\n" +
                $"CoreIntegrityActive={(controller != null && controller.CoreIntegrityActive)}\n" +
                $"InstanceBindCount={(controller != null ? controller.InstanceBindCount : 0)}\n" +
                $"PatrolTargetCount={(controller != null ? controller.PatrolTargetCount : 0)}\n" +
                $"PursuitEnterCount={(controller != null ? controller.PursuitEnterCount : 0)}\n" +
                $"AttackDecisionCount={(controller != null ? controller.AttackDecisionCount : 0)}\n" +
                $"AttackTelegraphCount={(controller != null ? controller.AttackTelegraphCount : 0)}\n" +
                $"AttackSwingCount={(controller != null ? controller.AttackSwingCount : 0)}\n" +
                $"AttackRecoveryCount={(controller != null ? controller.AttackRecoveryCount : 0)}\n" +
                $"AttackAbortCount={(controller != null ? controller.AttackAbortCount : 0)}\n" +
                $"PossessionSuspendCount={(controller != null ? controller.PossessionSuspendCount : 0)}\n" +
                $"PossessionResumeCount={(controller != null ? controller.PossessionResumeCount : 0)}\n" +
                $"BlockedMovementCount={(controller != null ? controller.BlockedMovementCount : 0)}\n" +
                $"GroundRejectCount={(controller != null ? controller.GroundRejectCount : 0)}\n" +
                $"LeashClampCount={(controller != null ? controller.LeashClampCount : 0)}\n" +
                $"CapabilityTestChangeCount={(controller != null ? controller.CapabilityTestChangeCount : 0)}\n" +
                $"TravelledDistance={(controller != null ? controller.TravelledDistance : 0f):F2}\n" +
                $"CurrentLeashDistance={(controller != null ? controller.CurrentLeashDistance : 0f):F2}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "BossAIPrototypeEnabled=True\n" +
                "DirectControlTakesPriority=True\n" +
                "TriggerCollidersPreserved=True\n" +
                "HitAuthorityEnabled=False\n" +
                "ClarityWritesEnabled=False\n" +
                "DamageAuthorityEnabled=False\n" +
                "NetworkIntegrationParked=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : deployment);

            EditorUtility.DisplayDialog(
                "M49.0 Diagnose",
                report,
                "Tamam");
        }

        private static void SetCapability(
            PrototypeMasterpiecePartKind kind,
            bool active)
        {
            PrototypeMasterpieceAutonomousEncounterController controller =
                FindPlayModeController();

            bool changed =
                controller != null &&
                controller.SetCapabilityForTest(
                    kind,
                    active);

            Debug.Log(
                "[M49.0 Capability Test]\n" +
                $"Part={kind}\n" +
                $"Active={active}\n" +
                $"Changed={changed}",
                controller);
        }

        private static PrototypeMasterpieceAutonomousEncounterController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M49.0",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeMasterpieceAutonomousEncounterController controller =
                FindOptional<
                    PrototypeMasterpieceAutonomousEncounterController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M49.0 Direct Test] Controller bulunamadı.");
            }

            return controller;
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
