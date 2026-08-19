#if UNITY_EDITOR
using System;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceClarityCounterplayMilestone
    {
        private const string RootName =
            "M49_2_MasterpieceClarityCounterplayBridge";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M49_2_ClarityCounterplayFeedback";

        private static readonly Color Ink =
            new Color(
                0.045f,
                0.040f,
                0.035f,
                0.94f);

        private static readonly Color Paper =
            new Color(
                0.96f,
                0.92f,
                0.82f,
                1f);

        private static readonly Color Orange =
            new Color(
                1f,
                0.33f,
                0.045f,
                1f);

        private static readonly Color Cyan =
            new Color(
                0.08f,
                0.76f,
                0.82f,
                1f);

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.2 - Apply Figure Clarity Counterplay Bridge")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M49.2 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceHitValidationController hitValidation =
                    FindRequired<
                        PrototypeMasterpieceHitValidationController>();

                PrototypeMasterpieceAutonomousEncounterController encounter =
                    FindRequired<
                        PrototypeMasterpieceAutonomousEncounterController>();

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                MonoBehaviour clarityState =
                    FindAuthoritativeClarity(
                        figureMotor);

                if (clarityState == null)
                {
                    throw new InvalidOperationException(
                        "FigureMotor hiyerarşisinde FigureClarityState bulunamadı.");
                }

                PrototypeFigureClarityImpactBridge clarityBridge =
                    GetOrAdd<
                        PrototypeFigureClarityImpactBridge>(
                            figureMotor.gameObject);

                clarityBridge.Configure(
                    clarityState);

                if (!clarityBridge.ContractResolved)
                {
                    throw new InvalidOperationException(
                        "FigureClarityState ApplyPaintExposure sözleşmesi çözülemedi: " +
                        clarityBridge.LastFailure);
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

                PrototypeMasterpieceClarityCounterplayController controller =
                    GetOrAdd<
                        PrototypeMasterpieceClarityCounterplayController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, 104f),
                        new Vector2(620f, 98f),
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
                        "M49.2",
                        15,
                        FontStyle.Bold,
                        TextAnchor.UpperCenter,
                        new Vector2(0f, 0.68f),
                        new Vector2(1f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(12f, -8f),
                        new Vector2(-24f, -4f),
                        Orange);

                Text result =
                    CreateText(
                        panel,
                        "ResultText",
                        "NETLİK SONUCU",
                        13,
                        FontStyle.Bold,
                        TextAnchor.MiddleCenter,
                        new Vector2(0f, 0.31f),
                        new Vector2(1f, 0.72f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(12f, 0f),
                        new Vector2(-24f, 0f),
                        Paper);

                Text counter =
                    CreateText(
                        panel,
                        "CounterText",
                        "Karşı hamle",
                        10,
                        FontStyle.Bold,
                        TextAnchor.LowerCenter,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.34f),
                        new Vector2(0.5f, 0f),
                        new Vector2(12f, 5f),
                        new Vector2(-24f, -2f),
                        Cyan);

                controller.Configure(
                    hitValidation,
                    encounter,
                    clarityBridge,
                    group,
                    title,
                    result,
                    counter);

                EditorUtility.SetDirty(
                    clarityBridge);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    group);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M49.2 Setup] Figure Clarity Counterplay Bridge ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"HitValidation={GetHierarchyPath(hitValidation.transform)}\n" +
                    $"Encounter={GetHierarchyPath(encounter.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"ClaritySource={GetHierarchyPath(clarityState.transform)}\n" +
                    $"ClaritySourceType={clarityState.GetType().FullName}\n" +
                    "ConfiguredExposurePerHit=10\n" +
                    "ExposureRegion=Torso\n" +
                    "UsesAuthoritativeFigureClarityState=True\n" +
                    "DodgeCounterplayEnabled=True\n" +
                    "Counterplay=Exit telegraph area\n" +
                    "NewParryInputAdded=False\n" +
                    "HealthSystemEnabled=False\n" +
                    "KnockbackEnabled=False\n" +
                    "ControlLockEnabled=False\n" +
                    "StunEnabled=False\n" +
                    "NetworkAuthorityEnabled=False\n" +
                    "LocalPrototypeAuthority=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M49.2 Hazır",
                    "M49.1 HIT sonucu otoriter FigureClarityState'e bağlandı. " +
                    "Telegraph dışına çıkma Netlik kaybını tamamen önleyen " +
                    "ilk karşı oyun kabul kapısıdır.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M49.2 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.2 - Apply Test Clarity Exposure (Play Mode)")]
        public static void ApplyTestExposure()
        {
            PrototypeMasterpieceClarityCounterplayController controller =
                FindPlayModeController();

            controller?.ApplyTestClarityExposure();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.2 - Reset Figure Clarity (Play Mode)")]
        public static void ResetClarity()
        {
            PrototypeMasterpieceClarityCounterplayController controller =
                FindPlayModeController();

            controller?.ResetFigureClarityForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.2 - Diagnose Figure Clarity Counterplay")]
        public static void Diagnose()
        {
            PrototypeMasterpieceClarityCounterplayController controller =
                FindOptional<
                    PrototypeMasterpieceClarityCounterplayController>();

            PrototypeFigureClarityImpactBridge clarityBridge =
                FindOptional<
                    PrototypeFigureClarityImpactBridge>();

            PrototypeMasterpieceHitValidationController hitValidation =
                FindOptional<
                    PrototypeMasterpieceHitValidationController>();

            bool roleResolved =
                PrototypeRoleAuthorityResolver.TryResolve(
                    out string role,
                    out bool isPainter);

            string report =
                "[M49.2 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ClarityBridge={(clarityBridge != null ? "OK" : "MISSING")}\n" +
                $"HitValidation={(hitValidation != null ? "OK" : "MISSING")}\n" +
                $"RoleSourceResolved={roleResolved}\n" +
                $"ResolvedRole={role}\n" +
                $"FigureRoleActive={(roleResolved && !isPainter)}\n" +
                $"ClarityContractResolved={(clarityBridge != null && clarityBridge.ContractResolved)}\n" +
                $"ClaritySourceEnabled={(clarityBridge != null && clarityBridge.SourceEnabled)}\n" +
                $"ClaritySourcePath={(clarityBridge != null ? clarityBridge.SourcePath : "N/A")}\n" +
                $"ClaritySourceType={(clarityBridge != null ? clarityBridge.SourceType : "N/A")}\n" +
                $"CurrentClarity={(clarityBridge != null ? clarityBridge.LastClarity : 0f):F2}\n" +
                $"MaximumClarity={(clarityBridge != null ? clarityBridge.LastMaximumClarity : 0f):F2}\n" +
                $"CurrentLevel={(clarityBridge != null ? clarityBridge.LastLevel : "N/A")}\n" +
                $"LastProcessedSequenceId={(controller != null ? controller.LastProcessedSequenceId : 0)}\n" +
                $"LastProcessedResult={(controller != null ? controller.LastProcessedResult.ToString() : "N/A")}\n" +
                $"ProcessedResultCount={(controller != null ? controller.ProcessedResultCount : 0)}\n" +
                $"ClarityHitCount={(controller != null ? controller.ClarityHitCount : 0)}\n" +
                $"ClarityWriteCount={(controller != null ? controller.ClarityWriteCount : 0)}\n" +
                $"ClarityWriteFailureCount={(controller != null ? controller.ClarityWriteFailureCount : 0)}\n" +
                $"ZeroDeltaHitCount={(controller != null ? controller.ZeroDeltaHitCount : 0)}\n" +
                $"SuccessfulDodgeCount={(controller != null ? controller.SuccessfulDodgeCount : 0)}\n" +
                $"OutOfReachMissCount={(controller != null ? controller.OutOfReachMissCount : 0)}\n" +
                $"OccludedMissCount={(controller != null ? controller.OccludedMissCount : 0)}\n" +
                $"TargetUnavailableCount={(controller != null ? controller.TargetUnavailableCount : 0)}\n" +
                $"AbortedResultCount={(controller != null ? controller.AbortedResultCount : 0)}\n" +
                $"TotalClarityLost={(controller != null ? controller.TotalClarityLost : 0f):F2}\n" +
                $"LastClarityBefore={(controller != null ? controller.LastClarityBefore : 0f):F2}\n" +
                $"LastClarityAfter={(controller != null ? controller.LastClarityAfter : 0f):F2}\n" +
                $"LastClarityDelta={(controller != null ? controller.LastClarityDelta : 0f):F2}\n" +
                $"LastLevelBefore={(controller != null ? controller.LastLevelBefore : "N/A")}\n" +
                $"LastLevelAfter={(controller != null ? controller.LastLevelAfter : "N/A")}\n" +
                $"LastMaterial={(controller != null ? controller.LastMaterial : "N/A")}\n" +
                $"LastAvailableCounter={(controller != null ? controller.LastAvailableCounter : "N/A")}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "ConfiguredExposurePerHit=10\n" +
                "ExposureRegion=Torso\n" +
                "ClarityWritesEnabled=True\n" +
                "UsesAuthoritativeFigureClarityState=True\n" +
                "DodgeCounterplayEnabled=True\n" +
                "NewParryInputAdded=False\n" +
                "HealthSystemEnabled=False\n" +
                "KnockbackEnabled=False\n" +
                "ControlLockEnabled=False\n" +
                "StunEnabled=False\n" +
                "NetworkAuthorityEnabled=False\n" +
                "LocalPrototypeAuthority=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : clarityBridge);

            EditorUtility.DisplayDialog(
                "M49.2 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeMasterpieceClarityCounterplayController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M49.2",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeMasterpieceClarityCounterplayController controller =
                FindOptional<
                    PrototypeMasterpieceClarityCounterplayController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M49.2 Direct Test] Controller bulunamadı.");
            }

            return controller;
        }

        private static MonoBehaviour FindAuthoritativeClarity(
            MonoBehaviour figureMotor)
        {
            if (figureMotor == null)
            {
                return null;
            }

            MonoBehaviour[] related =
                figureMotor.GetComponentsInChildren<
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
                        "FigureClarityState"
                )
                {
                    return candidate;
                }
            }

            related =
                figureMotor.GetComponentsInParent<
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
                        "FigureClarityState"
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

            MonoBehaviour best = null;
            int bestScore = int.MinValue;

            for (int index = 0;
                 index < all.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    all[index];

                if (
                    candidate == null ||
                    candidate.GetType().Name !=
                        "FigureClarityState" ||
                    !candidate.gameObject.scene.IsValid()
                )
                {
                    continue;
                }

                int score = 0;

                if (
                    candidate.transform ==
                    figureMotor.transform
                )
                {
                    score += 1000;
                }

                if (
                    candidate.transform.IsChildOf(
                        figureMotor.transform) ||
                    figureMotor.transform.IsChildOf(
                        candidate.transform)
                )
                {
                    score += 500;
                }

                if (candidate.enabled)
                {
                    score += 100;
                }

                if (candidate.gameObject.activeInHierarchy)
                {
                    score += 50;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
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
