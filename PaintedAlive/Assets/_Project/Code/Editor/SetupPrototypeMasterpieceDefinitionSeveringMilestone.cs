#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceDefinitionSeveringMilestone
    {
        private const string RootName =
            "M50_0_EyeDefinitionSeveringRiskSpikeA";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M50_0_EyeDefinitionSeveringPanel";

        private static readonly Color Ink =
            new Color(
                0.050f,
                0.045f,
                0.040f,
                0.95f);

        private static readonly Color Paper =
            new Color(
                0.96f,
                0.92f,
                0.82f,
                1f);

        private static readonly Color Orange =
            new Color(
                1f,
                0.42f,
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
            "50.0 - Apply Eye Definition Severing Risk Spike A")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M50.0 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceWorldDeploymentController deployment =
                    FindRequired<
                        PrototypeMasterpieceWorldDeploymentController>();

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

                MonoBehaviour clarityState =
                    FindRelatedMonoBehaviour(
                        figureMotor.transform,
                        "FigureClarityState");

                Camera figureCamera =
                    ResolveFigureCamera(
                        figureMotor.transform);

                if (figureCamera == null)
                {
                    throw new InvalidOperationException(
                        "Aktif Figür kamerası bulunamadı.");
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

                PrototypeMasterpieceDefinitionSeveringController controller =
                    GetOrAdd<
                        PrototypeMasterpieceDefinitionSeveringController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(-20f, -20f),
                        new Vector2(410f, 132f),
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
                        "KARŞI-KOMPOZİSYON • GÖZ TANIMI",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.73f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -9f),
                        new Vector2(-24f, -5f),
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
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "1 • PALET BIÇAĞI   E BASILI TUT",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 6f),
                        new Vector2(-24f, -2f),
                        Cyan);

                controller.Configure(
                    deployment,
                    figureMotor.transform,
                    figureCamera,
                    paletteKnife,
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
                    "[M50.0 Setup] Eye Definition Severing Risk Spike A ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Deployment={GetHierarchyPath(deployment.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"PaletteKnife={GetHierarchyPath(paletteKnife.transform)}\n" +
                    $"FigureCamera={GetHierarchyPath(figureCamera.transform)}\n" +
                    $"FigureClarityState={(clarityState != null ? GetHierarchyPath(clarityState.transform) : "OPTIONAL")}\n" +
                    "KeyboardMouseOnly=True\n" +
                    "CutInput=E Hold\n" +
                    "CutHoldSeconds=1.15\n" +
                    "MaximumCutDistance=2.75\n" +
                    "TargetPart=Head\n" +
                    "TargetCapability=Perception\n" +
                    "PaletteKnifeRequired=True\n" +
                    "CounterCompositionEnabled=True\n" +
                    "CapabilityWriteEnabled=True\n" +
                    "CoreSeveringEnabled=False\n" +
                    "GameplayPickupEnabled=False\n" +
                    "DefinitionUseEnabled=False\n" +
                    "InventoryEnabled=False\n" +
                    "DamageSystemEnabled=False\n" +
                    "NetworkAuthorityEnabled=False\n" +
                    "LocalPrototypeAuthority=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M50.0 Hazır",
                    "Palet Bıçağıyla Göz tanımını kesme, ayrılan çizim parçası " +
                    "ve Perception capability kapatma prototipi kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M50.0 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.0 - Force Sever Eye Definition (Play Mode)")]
        public static void ForceSever()
        {
            PrototypeMasterpieceDefinitionSeveringController controller =
                FindPlayModeController();

            controller?.ForceSeverForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.0 - Restore Eye Definition (Play Mode)")]
        public static void RestoreEye()
        {
            PrototypeMasterpieceDefinitionSeveringController controller =
                FindPlayModeController();

            controller?.RestoreEyeForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.0 - Diagnose Eye Definition Severing")]
        public static void Diagnose()
        {
            PrototypeMasterpieceDefinitionSeveringController controller =
                FindOptional<
                    PrototypeMasterpieceDefinitionSeveringController>();

            PrototypeMasterpieceAutonomousEncounterController encounter =
                FindOptional<
                    PrototypeMasterpieceAutonomousEncounterController>();

            PrototypeMasterpieceWorldDeploymentController deployment =
                FindOptional<
                    PrototypeMasterpieceWorldDeploymentController>();

            PrototypeSeveredDefinitionToken token =
                controller != null
                    ? controller.DetachedToken
                    : null;

            string report =
                "[M50.0 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"WorldInstanceActive={(deployment != null && deployment.WorldInstanceActive)}\n" +
                $"ResolvedRole={(controller != null ? controller.ResolvedRole : "N/A")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"InputActionActive={(controller != null && controller.InputActionActive)}\n" +
                $"PaletteKnifeActive={(controller != null && controller.PaletteKnifeActive)}\n" +
                $"PrimaryToolAllowed={(controller != null && controller.PrimaryToolAllowed)}\n" +
                $"State={(controller != null ? controller.State.ToString() : "N/A")}\n" +
                $"EyeCapabilityActive={(controller != null && controller.EyeCapabilityActive)}\n" +
                $"EyeAimed={(controller != null && controller.EyeAimed)}\n" +
                $"LineOfSightClear={(controller != null && controller.LineOfSightClear)}\n" +
                $"EyeSevered={(controller != null && controller.EyeSevered)}\n" +
                $"CutProgress={(controller != null ? controller.CutProgress : 0f):F3}\n" +
                $"EyeDistance={(controller != null ? controller.EyeDistance : -1f):F3}\n" +
                $"ViewportAimOffset={(controller != null ? controller.ViewportAimOffset : 1f):F3}\n" +
                $"InstanceBindCount={(controller != null ? controller.InstanceBindCount : 0)}\n" +
                $"CutStartCount={(controller != null ? controller.CutStartCount : 0)}\n" +
                $"CutCancelCount={(controller != null ? controller.CutCancelCount : 0)}\n" +
                $"SeverCount={(controller != null ? controller.SeverCount : 0)}\n" +
                $"RestoreCount={(controller != null ? controller.RestoreCount : 0)}\n" +
                $"DetachedTokenCount={(controller != null ? controller.DetachedTokenCount : 0)}\n" +
                $"RoleRejectedCount={(controller != null ? controller.RoleRejectedCount : 0)}\n" +
                $"ToolRejectedCount={(controller != null ? controller.ToolRejectedCount : 0)}\n" +
                $"OcclusionRejectCount={(controller != null ? controller.OcclusionRejectCount : 0)}\n" +
                $"AimRejectCount={(controller != null ? controller.AimRejectCount : 0)}\n" +
                $"Token={(token != null ? "OK" : "NONE")}\n" +
                $"TokenSettled={(token != null && token.Settled)}\n" +
                $"TokenTravelledDistance={(token != null ? token.TravelledDistance : 0f):F3}\n" +
                $"EncounterPerceptionActive={(encounter != null && encounter.PerceptionActive)}\n" +
                $"EncounterTargetDetected={(encounter != null && encounter.TargetDetected)}\n" +
                $"EncounterTargetDistance={(encounter != null ? encounter.TargetDistance : -1f):F2}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "TargetPart=Head\n" +
                "TargetCapability=Perception\n" +
                "CounterCompositionEnabled=True\n" +
                "PaletteKnifeRequired=True\n" +
                "CapabilityWriteEnabled=True\n" +
                "CoreSeveringEnabled=False\n" +
                "GameplayPickupEnabled=False\n" +
                "DefinitionUseEnabled=False\n" +
                "InventoryEnabled=False\n" +
                "DamageSystemEnabled=False\n" +
                "NetworkAuthorityEnabled=False\n" +
                "LocalPrototypeAuthority=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : deployment);

            EditorUtility.DisplayDialog(
                "M50.0 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeMasterpieceDefinitionSeveringController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M50.0",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeMasterpieceDefinitionSeveringController controller =
                FindOptional<
                    PrototypeMasterpieceDefinitionSeveringController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M50.0 Direct Test] Controller bulunamadı.");
            }

            return controller;
        }

        private static Camera ResolveFigureCamera(
            Transform figureRoot)
        {
            Camera camera =
                Camera.main;

            if (
                camera != null &&
                camera.gameObject.scene.IsValid()
            )
            {
                return camera;
            }

            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<
                    Camera>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            Camera best = null;
            int bestScore = int.MinValue;

            for (int index = 0;
                 index < cameras.Length;
                 index++)
            {
                Camera candidate =
                    cameras[index];

                if (
                    candidate == null ||
                    !candidate.gameObject.scene.IsValid()
                )
                {
                    continue;
                }

                int score = 0;

                if (candidate.isActiveAndEnabled)
                {
                    score += 1000;
                }

                if (
                    figureRoot != null &&
                    (
                        candidate.transform.IsChildOf(
                            figureRoot) ||
                        figureRoot.IsChildOf(
                            candidate.transform)
                    )
                )
                {
                    score += 500;
                }

                if (
                    candidate.name.IndexOf(
                        "Figure",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 100;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
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
                        typeName ||
                    !candidate.gameObject.scene.IsValid()
                )
                {
                    continue;
                }

                int score = 0;

                if (candidate.enabled)
                {
                    score += 100;
                }

                if (candidate.gameObject.activeInHierarchy)
                {
                    score += 50;
                }

                if (
                    root != null &&
                    (
                        candidate.transform.IsChildOf(root) ||
                        root.IsChildOf(
                            candidate.transform)
                    )
                )
                {
                    score += 500;
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
