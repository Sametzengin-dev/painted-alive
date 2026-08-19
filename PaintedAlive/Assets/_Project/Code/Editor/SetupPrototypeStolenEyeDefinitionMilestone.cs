#if UNITY_EDITOR
using System;
using PaintedAlive.Painters.Masterpiece;
using PaintedAlive.Painters.SideCanvas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeStolenEyeDefinitionMilestone
    {
        private const string RootName =
            "M50_1_StolenHeadPerceptionDefinitionRiskSpikeA2";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M50_1_StolenHeadPerceptionDefinitionPanel";

        private static readonly Color Ink =
            new Color(
                0.045f,
                0.040f,
                0.035f,
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
                0.44f,
                0.045f,
                1f);

        private static readonly Color Cyan =
            new Color(
                0.08f,
                0.82f,
                0.86f,
                1f);

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.1 - Apply Stolen Head Perception Definition Risk Spike A2")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M50.1 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeMasterpieceWorldDeploymentController deployment =
                    FindRequired<
                        PrototypeMasterpieceWorldDeploymentController>();

                PrototypeMasterpieceDefinitionSeveringController severing =
                    FindRequired<
                        PrototypeMasterpieceDefinitionSeveringController>();

                PrototypeLivingSideCanvasController sideCanvas =
                    FindRequired<
                        PrototypeLivingSideCanvasController>();

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

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

                PrototypeStolenEyeDefinitionController controller =
                    GetOrAdd<
                        PrototypeStolenEyeDefinitionController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(-20f, -174f),
                        new Vector2(410f, 138f),
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
                        "ÇALINMIŞ TANIM • HEAD / PERCEPTION",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.74f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -9f),
                        new Vector2(-24f, -5f),
                        Orange);

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Kesilmiş Head / Perception tanımı bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.24f),
                        new Vector2(1f, 0.76f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "E • BAĞLAMSAL ÇALINMIŞ TANIM",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.27f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 6f),
                        new Vector2(-24f, -2f),
                        Cyan);

                controller.Configure(
                    deployment,
                    severing,
                    sideCanvas,
                    figureMotor.transform,
                    figureCamera,
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
                    "[M50.1 Setup] Stolen Head / Perception Definition Risk Spike A2 ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Deployment={GetHierarchyPath(deployment.transform)}\n" +
                    $"Severing={GetHierarchyPath(severing.transform)}\n" +
                    $"SideCanvas={GetHierarchyPath(sideCanvas.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"FigureCamera={GetHierarchyPath(figureCamera.transform)}\n" +
                    "ContextInput=E\n" +
                    "PickupHoldSeconds=0.45\n" +
                    "MaximumPickupDistance=1.80\n" +
                    "MaximumCarrySlots=1\n" +
                    "PerceptionEchoDurationSeconds=6\n" +
                    "PainterDraftRevealEnabled=True\n" +
                    "WeakOrganRevealEnabled=True\n" +
                    "FigurePositionExposureEnabled=True\n" +
                    "RawDamageBonusEnabled=False\n" +
                    "InventoryListEnabled=False\n" +
                    "NewWeaponEnabled=False\n" +
                    "NetworkAuthorityEnabled=False\n" +
                    "LocalPrototypeAuthority=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M50.1 Hazır",
                    "Kesilmiş Head / Perception tanımını alma, tek slotta taşıma, " +
                    "6 saniyelik Algı Yankısı ve konum açığa çıkma riski kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M50.1 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.1 - Force Pickup Stolen Head Definition (Play Mode)")]
        public static void ForcePickup()
        {
            PrototypeStolenEyeDefinitionController controller =
                FindPlayModeController();

            controller?.ForcePickupForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.1 - Activate Perception Echo (Play Mode)")]
        public static void ActivateSight()
        {
            PrototypeStolenEyeDefinitionController controller =
                FindPlayModeController();

            controller?.ActivateEyeSightForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.1 - Drop Carried Definition (Play Mode)")]
        public static void DropDefinition()
        {
            PrototypeStolenEyeDefinitionController controller =
                FindPlayModeController();

            controller?.DropCarriedDefinition();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "50.1 - Diagnose Stolen Head Perception Definition")]
        public static void Diagnose()
        {
            PrototypeStolenEyeDefinitionController controller =
                FindOptional<
                    PrototypeStolenEyeDefinitionController>();

            PrototypeMasterpieceDefinitionSeveringController severing =
                FindOptional<
                    PrototypeMasterpieceDefinitionSeveringController>();

            PrototypeSeveredDefinitionToken token =
                controller != null
                    ? controller.Token
                    : null;

            string report =
                "[M50.1 Head / Perception Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"Severing={(severing != null ? "OK" : "MISSING")}\n" +
                $"ResolvedRole={(controller != null ? controller.ResolvedRole : "N/A")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"InputActionActive={(controller != null && controller.InputActionActive)}\n" +
                $"State={(controller != null ? controller.State.ToString() : "N/A")}\n" +
                $"Token={(token != null ? "OK" : "NONE")}\n" +
                $"TokenSettled={(token != null && token.Settled)}\n" +
                $"TokenCarried={(token != null && token.IsCarried)}\n" +
                $"TokenConsumed={(token != null && token.Consumed)}\n" +
                $"TokenPickupCount={(token != null ? token.PickupCount : 0)}\n" +
                $"TokenDropCount={(token != null ? token.DropCount : 0)}\n" +
                $"TokenConsumeCount={(token != null ? token.ConsumeCount : 0)}\n" +
                $"TokenAimed={(controller != null && controller.TokenAimed)}\n" +
                $"LineOfSightClear={(controller != null && controller.LineOfSightClear)}\n" +
                $"Carrying={(controller != null && controller.Carrying)}\n" +
                $"SightActive={(controller != null && controller.SightActive)}\n" +
                $"PositionExposed={(controller != null && controller.PositionExposed)}\n" +
                $"DraftSignalAvailable={(controller != null && controller.DraftSignalAvailable)}\n" +
                $"PickupProgress={(controller != null ? controller.PickupProgress : 0f):F3}\n" +
                $"TokenDistance={(controller != null ? controller.TokenDistance : -1f):F3}\n" +
                $"SightRemainingSeconds={(controller != null ? controller.SightRemainingSeconds : 0f):F2}\n" +
                $"PickupStartCount={(controller != null ? controller.PickupStartCount : 0)}\n" +
                $"PickupCancelCount={(controller != null ? controller.PickupCancelCount : 0)}\n" +
                $"PickupCompleteCount={(controller != null ? controller.PickupCompleteCount : 0)}\n" +
                $"SightActivationCount={(controller != null ? controller.SightActivationCount : 0)}\n" +
                $"SightConsumeCount={(controller != null ? controller.SightConsumeCount : 0)}\n" +
                $"ManualDropCount={(controller != null ? controller.ManualDropCount : 0)}\n" +
                $"RoleAutoDropCount={(controller != null ? controller.RoleAutoDropCount : 0)}\n" +
                $"WeakPointRevealCount={(controller != null ? controller.WeakPointRevealCount : 0)}\n" +
                $"DraftStrokeRevealCount={(controller != null ? controller.DraftStrokeRevealCount : 0)}\n" +
                $"RejectedUseCount={(controller != null ? controller.RejectedUseCount : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "StolenDefinitionSlotEnabled=True\n" +
                "MaximumCarrySlots=1\n" +
                "PerceptionEchoEnabled=True\n" +
                "HeadPerceptionTokenRequired=True\n" +
                "LegacyEyeClassNamesRetainedForSerialization=True\n" +
                "PainterDraftRevealEnabled=True\n" +
                "WeakOrganRevealEnabled=True\n" +
                "FigurePositionExposureEnabled=True\n" +
                "RawDamageBonusEnabled=False\n" +
                "InventoryListEnabled=False\n" +
                "NewWeaponEnabled=False\n" +
                "NetworkAuthorityEnabled=False\n" +
                "LocalPrototypeAuthority=True";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : severing);

            EditorUtility.DisplayDialog(
                "M50.1 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeStolenEyeDefinitionController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M50.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeStolenEyeDefinitionController controller =
                FindOptional<
                    PrototypeStolenEyeDefinitionController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M50.1 Direct Test] Controller bulunamadı.");
            }

            return controller;
        }

        private static Camera ResolveFigureCamera(
            Transform figureRoot)
        {
            Camera main =
                Camera.main;

            if (
                main != null &&
                main.gameObject.scene.IsValid()
            )
            {
                return main;
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
