#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceHitValidationMilestone
    {
        private const string RootName =
            "M49_1_MasterpieceHitValidationBridge";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M49_1_MasterpieceHitValidationPanel";

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
            "49.1 - Apply Masterpiece Hit Validation Bridge")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M49.1 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceWorldDeploymentController deployment =
                    FindRequired<
                        PrototypeMasterpieceWorldDeploymentController>();

                PrototypeMasterpieceAutonomousEncounterController encounter =
                    FindRequired<
                        PrototypeMasterpieceAutonomousEncounterController>();

                PrototypeMasterpiecePossessionController possession =
                    FindRequired<
                        PrototypeMasterpiecePossessionController>();

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                PrototypeFigureHitQueryVolume figureVolume =
                    GetOrAdd<
                        PrototypeFigureHitQueryVolume>(
                            figureMotor.gameObject);

                figureVolume.Configure(
                    figureMotor.transform);

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

                PrototypeMasterpieceHitValidationController controller =
                    GetOrAdd<
                        PrototypeMasterpieceHitValidationController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(-20f, -20f),
                        new Vector2(388f, 158f),
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
                        "BAŞ YAPIT • HIT DOĞRULAMA",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.77f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -10f),
                        new Vector2(-24f, -8f),
                        Orange);

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "M49.0 saldırı kararı bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.22f),
                        new Vector2(1f, 0.79f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -2f),
                        new Vector2(-24f, -4f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "H • QUERY HACİMLERİ",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.24f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 7f),
                        new Vector2(-24f, -3f),
                        Cyan);

                controller.Configure(
                    deployment,
                    encounter,
                    possession,
                    figureVolume,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    figureVolume);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    group);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M49.1 Setup] Masterpiece Hit Validation Bridge ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Encounter={GetHierarchyPath(encounter.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"FigureVolume={GetHierarchyPath(figureVolume.transform)}\n" +
                    "KeyboardMouseOnly=True\n" +
                    "DebugVolumeInput=H\n" +
                    "FigureQueryCapsule=True\n" +
                    "AttackQueryCapsule=True\n" +
                    "SingleResultPerSwing=True\n" +
                    "StrikeWindowNormalized=0.46\n" +
                    "WorldOcclusionQuery=True\n" +
                    "GameplayColliderAdded=False\n" +
                    "FigureMotorModified=False\n" +
                    "HitValidationEnabled=True\n" +
                    "GameplayHitAuthorityEnabled=False\n" +
                    "ClarityWritesEnabled=False\n" +
                    "DamageAuthorityEnabled=False\n" +
                    "KnockbackEnabled=False\n" +
                    "ControlLockEnabled=False\n" +
                    "NetworkIntegrationParked=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M49.1 Hazır",
                    "Figür query capsule, saldırı query capsule, tek sonuçlu " +
                    "HIT/MISS ve engel doğrulaması kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M49.1 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.1 - Toggle Hit Volume Debug (Play Mode)")]
        public static void ToggleDebug()
        {
            PrototypeMasterpieceHitValidationController controller =
                FindPlayModeController();

            controller?.ToggleDebugVolumes();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.1 - Force Attack Decision (Play Mode)")]
        public static void ForceAttack()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M49.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return;
            }

            PrototypeMasterpieceAutonomousEncounterController encounter =
                FindOptional<
                    PrototypeMasterpieceAutonomousEncounterController>();

            if (encounter == null)
            {
                Debug.LogError(
                    "[M49.1 Direct Test] M49.0 encounter bulunamadı.");

                return;
            }

            encounter.ForceAttackDecision();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.1 - Force Current Strike Validation (Play Mode)")]
        public static void ForceCurrentValidation()
        {
            PrototypeMasterpieceHitValidationController controller =
                FindPlayModeController();

            controller?.ForceValidateCurrentStrike();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.1 - Diagnose Masterpiece Hit Validation")]
        public static void Diagnose()
        {
            PrototypeMasterpieceHitValidationController controller =
                FindOptional<
                    PrototypeMasterpieceHitValidationController>();

            PrototypeFigureHitQueryVolume figureVolume =
                FindOptional<
                    PrototypeFigureHitQueryVolume>();

            PrototypeMasterpieceAutonomousEncounterController encounter =
                FindOptional<
                    PrototypeMasterpieceAutonomousEncounterController>();

            string report =
                "[M49.1 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"Encounter={(encounter != null ? "OK" : "MISSING")}\n" +
                $"FigureVolume={(figureVolume != null ? "OK" : "MISSING")}\n" +
                $"InputActionsActive={(controller != null && controller.InputActionsActive)}\n" +
                $"SequenceActive={(controller != null && controller.SequenceActive)}\n" +
                $"StrikePending={(controller != null && controller.StrikePending)}\n" +
                $"StrikeValidated={(controller != null && controller.StrikeValidated)}\n" +
                $"LastResult={(controller != null ? controller.LastResult.ToString() : "N/A")}\n" +
                $"SequenceId={(controller != null ? controller.SequenceId : 0)}\n" +
                $"TelegraphSnapshotCount={(controller != null ? controller.TelegraphSnapshotCount : 0)}\n" +
                $"ValidationCount={(controller != null ? controller.ValidationCount : 0)}\n" +
                $"HitCount={(controller != null ? controller.HitCount : 0)}\n" +
                $"MissCount={(controller != null ? controller.MissCount : 0)}\n" +
                $"AbortedCount={(controller != null ? controller.AbortedCount : 0)}\n" +
                $"DuplicateValidationPreventedCount={(controller != null ? controller.DuplicateValidationPreventedCount : 0)}\n" +
                $"OcclusionQueryCount={(controller != null ? controller.OcclusionQueryCount : 0)}\n" +
                $"VolumeBuildFailureCount={(controller != null ? controller.VolumeBuildFailureCount : 0)}\n" +
                $"DebugVolumesVisible={(controller != null && controller.DebugVolumesVisible)}\n" +
                $"LastCapsuleDistance={(controller != null ? controller.LastCapsuleDistance : -1f):F3}\n" +
                $"LastCombinedRadius={(controller != null ? controller.LastCombinedRadius : 0f):F3}\n" +
                $"LastStrikeProgress={(controller != null ? controller.LastStrikeProgress : 0f):F3}\n" +
                $"LastReason={(controller != null ? controller.LastReason : "N/A")}\n" +
                $"FigureVolumeSource={(figureVolume != null ? figureVolume.SourceKind : "N/A")}\n" +
                $"FigureVolumeAuthoritative={(figureVolume != null && figureVolume.AuthoritativeSourceFound)}\n" +
                $"FigureVolumeQueryCount={(figureVolume != null ? figureVolume.QueryCount : 0)}\n" +
                $"FigureVolumeRadius={(figureVolume != null ? figureVolume.LastRadius : 0f):F3}\n" +
                $"FigureVolumeHeight={(figureVolume != null ? figureVolume.LastHeight : 0f):F3}\n" +
                "HitValidationEnabled=True\n" +
                "SingleResultPerSwing=True\n" +
                "QueryOnlyVolumes=True\n" +
                "GameplayColliderAdded=False\n" +
                "FigureMotorModified=False\n" +
                "GameplayHitAuthorityEnabled=False\n" +
                "ClarityWritesEnabled=False\n" +
                "DamageAuthorityEnabled=False\n" +
                "KnockbackEnabled=False\n" +
                "ControlLockEnabled=False\n" +
                "NetworkIntegrationParked=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : encounter);

            EditorUtility.DisplayDialog(
                "M49.1 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeMasterpieceHitValidationController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M49.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeMasterpieceHitValidationController controller =
                FindOptional<
                    PrototypeMasterpieceHitValidationController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M49.1 Direct Test] Controller bulunamadı.");
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
