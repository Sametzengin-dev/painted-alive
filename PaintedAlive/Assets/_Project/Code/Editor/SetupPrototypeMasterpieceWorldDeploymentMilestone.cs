#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.Figures;
using PaintedAlive.Painters.Masterpiece;
using PaintedAlive.Painters.SideCanvas;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceWorldDeploymentMilestone
    {
        private const string RootName =
            "M47_MasterpieceWorldDeploymentRiskSpikeC";

        private const string AnchorName =
            "M47_MasterpieceRouteAnchor";

        private const string WorldParentName =
            "M47_DeployedMasterpieces";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M47_MasterpieceDeploymentPanel";

        private const string WorldStrokeMaterialPath =
            "Assets/_Project/Art/Materials/M60/MAT_LivingCanvasWorldStroke.mat";

        private static readonly Color Ink =
            new Color(
                0.055f,
                0.050f,
                0.045f,
                0.94f);

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
            "47 - Apply Masterpiece World Deployment Risk Spike C")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M47 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeLivingSideCanvasController sideCanvas =
                    FindRequired<
                        PrototypeLivingSideCanvasController>();

                PrototypeMasterpieceAssemblyController assembly =
                    FindAssemblyForSideCanvas(
                        sideCanvas);

                GameObject m45Root =
                    GameObject.Find(
                        "M45_LivingSideCanvasRiskSpikeA");

                if (m45Root == null)
                {
                    throw new InvalidOperationException(
                        "M45_LivingSideCanvasRiskSpikeA bulunamadı.");
                }

                Transform m45Canvas =
                    m45Root.transform.Find(
                        "M45_LivingSideCanvasCanvas");

                if (m45Canvas == null)
                {
                    throw new InvalidOperationException(
                        "M45_LivingSideCanvasCanvas bulunamadı.");
                }

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

                PrototypeMasterpieceWorldDeploymentController controller =
                    GetOrAdd<
                        PrototypeMasterpieceWorldDeploymentController>(
                            root);

                Transform worldParent =
                    GetOrCreateTransform(
                        root.transform,
                        WorldParentName);

                PrototypeMasterpieceRouteAnchor routeAnchor =
                    GetOrCreateAnchor(
                        root.transform,
                        figureMotor.transform);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(-20f, 42f),
                        new Vector2(330f, 148f),
                        Ink);

                CanvasGroup group =
                    GetOrAdd<CanvasGroup>(
                        panel.gameObject);

                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;

                Text title =
                    CreateText(
                        panel,
                        "TitleText",
                        "BAŞ YAPIT • DEPLOY HAZIRLIĞI",
                        15,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.72f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(16f, -12f),
                        new Vector2(-28f, -12f),
                        Orange);

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "ASSEMBLY BEKLİYOR",
                        12,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.20f),
                        new Vector2(1f, 0.73f),
                        new Vector2(0f, 1f),
                        new Vector2(16f, -4f),
                        new Vector2(-28f, -8f),
                        Paper);

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "V • DEPLOY   X • GERİ ÇAĞIR   O • PROXY",
                        11,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.22f),
                        new Vector2(0f, 0f),
                        new Vector2(16f, 8f),
                        new Vector2(-28f, -4f),
                        Cyan);

                RectTransform sideCanvasFeedbackPanel =
                    CreatePanel(
                        m45Canvas,
                        "M47_DeployFeedbackPanel",
                        new Vector2(0.71f, 0.915f),
                        new Vector2(0.98f, 0.975f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero,
                        new Color(
                            0.035f,
                            0.032f,
                            0.030f,
                            0.98f));

                CanvasGroup sideCanvasFeedbackGroup =
                    GetOrAdd<CanvasGroup>(
                        sideCanvasFeedbackPanel.gameObject);

                sideCanvasFeedbackGroup.alpha = 0f;
                sideCanvasFeedbackGroup.interactable = false;
                sideCanvasFeedbackGroup.blocksRaycasts = false;

                Text sideCanvasFeedbackText =
                    CreateText(
                        sideCanvasFeedbackPanel,
                        "FeedbackText",
                        "DEPLOY BEKLİYOR",
                        12,
                        FontStyle.Bold,
                        TextAnchor.MiddleLeft,
                        Vector2.zero,
                        Vector2.one,
                        new Vector2(0f, 0.5f),
                        new Vector2(12f, 0f),
                        new Vector2(-20f, -6f),
                        Orange);

                controller.Configure(
                    sideCanvas,
                    assembly,
                    routeAnchor,
                    worldParent,
                    figureMotor as FigureMotor,
                    GetOrCreateWorldStrokeMaterial(),
                    GetOrCreateWorldStrokeMaterial(),
                    group,
                    title,
                    state,
                    controls,
                    sideCanvasFeedbackGroup,
                    sideCanvasFeedbackText);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    routeAnchor);

                EditorUtility.SetDirty(
                    group);

                EditorUtility.SetDirty(
                    sideCanvasFeedbackGroup);

                EditorUtility.SetDirty(
                    sideCanvasFeedbackText);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M47 Setup] Masterpiece World Deployment Risk Spike C ready.\n" +
                    $"Root={GetHierarchyPath(root.transform)}\n" +
                    $"SideCanvas={GetHierarchyPath(sideCanvas.transform)}\n" +
                    $"Assembly={GetHierarchyPath(assembly.transform)}\n" +
                    $"AssemblySourceMatchesSideCanvas={assembly.SideCanvasSource == sideCanvas}\n" +
                    $"FigureMotor={Describe(figureMotor)}\n" +
                    $"RouteAnchor={GetHierarchyPath(routeAnchor.transform)}\n" +
                    $"AnchorPosition={routeAnchor.transform.position}\n" +
                    $"WorldParent={GetHierarchyPath(worldParent)}\n" +
                    $"ControllerCount={CountSceneObjects<PrototypeMasterpieceWorldDeploymentController>()}\n" +
                    $"AnchorCount={CountSceneObjects<PrototypeMasterpieceRouteAnchor>()}\n" +
                    "AddNewOnly=True\n" +
                    "StaticDeploymentOnly=True\n" +
                    "FixedProxyColliders=True\n" +
                    "TriggerCollidersOnly=True\n" +
                    "KinematicBody=True\n" +
                    "VisualStrokeTransferred=True\n" +
                    "VisualShapeControlsPower=False\n" +
                    "CloseCanvasAfterDeploy=True\n" +
                    "InputBinding=InputAction:<Keyboard>/v\n" +
                    "SideCanvasDeployFeedback=True\n" +
                    "DeployAttemptLogging=True\n" +
                    "DirectPlayModeDeployMenu=True\n" +
                    "AssemblyResolvedBySideCanvas=True\n" +
                    "AssemblyPreparedSynchronously=True\n" +
                    "BossAIEnabled=False\n" +
                    "DamageAuthorityEnabled=False\n" +
                    "M45FilesModified=False\n" +
                    "M46FilesModified=False\n" +
                    "NetworkIntegrationParked=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M47 tamamlandı",
                    "Route anchor, statik dünya deploy'u, altı trigger proxy " +
                    "ve geri çağırma katmanı hazır. Scene view'da " +
                    "M47_MasterpieceRouteAnchor konumunu kontrol et.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M47 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "47 - Try Deploy Now (Play Mode)")]
        public static void TryDeployNow()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M47 Deploy",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return;
            }

            PrototypeMasterpieceWorldDeploymentController controller =
                FindOptional<
                    PrototypeMasterpieceWorldDeploymentController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M47 Direct Deploy] Controller bulunamadı.");

                return;
            }

            controller.TryDeploy();

            Debug.Log(
                "[M47 Direct Deploy] TryDeploy çağrıldı.\\n" +
                $"WorldInstanceActive={controller.WorldInstanceActive}\\n" +
                $"DirectDeployAttemptCount={controller.DirectDeployAttemptCount}\\n" +
                $"RejectedDeployCount={controller.RejectedDeployCount}\\n" +
                $"LastAction={controller.LastAction}",
                controller);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "47 - Diagnose Masterpiece World Deployment")]
        public static void Diagnose()
        {
            PrototypeMasterpieceWorldDeploymentController controller =
                FindOptional<
                    PrototypeMasterpieceWorldDeploymentController>();

            PrototypeMasterpieceRouteAnchor anchor =
                FindOptional<
                    PrototypeMasterpieceRouteAnchor>();

            PrototypeMasterpieceWorldInstance instance =
                controller != null
                    ? controller.ActiveInstance
                    : null;

            Debug.Log(
                "[M47 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ControllerCount={CountSceneObjects<PrototypeMasterpieceWorldDeploymentController>()}\n" +
                $"Anchor={(anchor != null ? "OK" : "MISSING")}\n" +
                $"AnchorCount={CountSceneObjects<PrototypeMasterpieceRouteAnchor>()}\n" +
                $"AnchorPosition={(anchor != null ? anchor.transform.position.ToString() : "N/A")}\n" +
                $"AnchorLastClear={(anchor != null && anchor.LastPlacementClear)}\n" +
                $"AnchorObstructions={(anchor != null ? anchor.LastObstructionCount : 0)}\n" +
                $"AnchorValidation={(anchor != null ? anchor.LastValidation : "N/A")}\n" +
                $"WorldInstanceActive={(controller != null && controller.WorldInstanceActive)}\n" +
                $"DeployCount={(controller != null ? controller.DeployCount : 0)}\n" +
                $"RecallCount={(controller != null ? controller.RecallCount : 0)}\n" +
                $"RejectedDeployCount={(controller != null ? controller.RejectedDeployCount : 0)}\n" +
                $"ProxyToggleCount={(controller != null ? controller.ProxyToggleCount : 0)}\n" +
                $"DeployInputAttemptCount={(controller != null ? controller.DeployInputAttemptCount : 0)}\n" +
                $"DirectDeployAttemptCount={(controller != null ? controller.DirectDeployAttemptCount : 0)}\n" +
                $"DeployInputActionsActive={(controller != null && controller.DeployInputActionsActive)}\n" +
                $"SideCanvasResolveCount={(controller != null ? controller.SideCanvasResolveCount : 0)}\n" +
                $"SideCanvasReferenceChangeCount={(controller != null ? controller.SideCanvasReferenceChangeCount : 0)}\n" +
                $"OpenSideCanvasCandidateCount={(controller != null ? controller.OpenSideCanvasCandidateCount : 0)}\n" +
                $"SideCanvasResolvedOpen={(controller != null && controller.SideCanvasResolvedOpen)}\n" +
                $"SideCanvasSelectionReason={(controller != null ? controller.SideCanvasSelectionReason : "N/A")}\n" +
                $"AssemblyResolveCount={(controller != null ? controller.AssemblyResolveCount : 0)}\n" +
                $"AssemblyReferenceChangeCount={(controller != null ? controller.AssemblyReferenceChangeCount : 0)}\n" +
                $"AssemblySourceMatchesSideCanvas={(controller != null && controller.AssemblySourceMatchesSideCanvas)}\n" +
                $"AssemblyPrepareAttemptCount={(controller != null ? controller.AssemblyPrepareAttemptCount : 0)}\n" +
                $"AssemblyPrepareSuccessCount={(controller != null ? controller.AssemblyPrepareSuccessCount : 0)}\n" +
                $"LastAssemblyPreparation={(controller != null ? controller.LastAssemblyPreparation : "N/A")}\n" +
                $"LastDeploySource={(controller != null ? controller.LastDeploySource : "N/A")}\n" +
                $"LastDeployAttemptAt={(controller != null ? controller.LastDeployAttemptAt : -1f)}\n" +
                $"LastSnapshotHash={(controller != null ? controller.LastSnapshotHash : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                $"InstanceBuildSucceeded={(instance != null && instance.BuildSucceeded)}\n" +
                $"InstancePartCount={(instance != null ? instance.PartCount : 0)}\n" +
                $"InstanceColliderCount={(instance != null ? instance.ColliderCount : 0)}\n" +
                $"AllCollidersAreTriggers={(instance != null && instance.AllCollidersAreTriggers)}\n" +
                $"KinematicBody={(instance != null && instance.KinematicBody)}\n" +
                $"VisualSegmentCount={(instance != null ? instance.VisualSegmentCount : 0)}\n" +
                $"RenderedPointCount={(instance != null ? instance.RenderedPointCount : 0)}\n" +
                $"ProxyDebugVisible={(instance != null && instance.ProxyDebugVisible)}\n" +
                $"SnapshotHash={(instance != null ? instance.SnapshotHash : 0)}\n" +
                "InputBinding=InputAction:<Keyboard>/v\n" +
                "SideCanvasDeployFeedback=True\n" +
                "DeployAttemptLogging=True\n" +
                "DirectPlayModeDeployMenu=True\n" +
                "OpenSideCanvasResolvedAtDeploy=True\n" +
                "AssemblyResolvedBySideCanvas=True\n" +
                "AssemblyPreparedSynchronously=True\n" +
                "StaticDeploymentOnly=True\n" +
                "TriggerCollidersOnly=True\n" +
                "SolidCollisionEnabled=False\n" +
                "BossAIEnabled=False\n" +
                "DamageAuthorityEnabled=False\n" +
                "M45FilesModified=False\n" +
                "M46FilesModified=False\n" +
                "NetworkIntegrationParked=True");
        }

        private static PrototypeMasterpieceRouteAnchor
            GetOrCreateAnchor(
                Transform parent,
                Transform figureTransform)
        {
            Transform existing =
                parent.Find(
                    AnchorName);

            if (existing != null)
            {
                PrototypeMasterpieceRouteAnchor existingAnchor =
                    GetOrAdd<
                        PrototypeMasterpieceRouteAnchor>(
                            existing.gameObject);

                return existingAnchor;
            }

            GameObject anchorObject =
                new GameObject(
                    AnchorName);

            Undo.RegisterCreatedObjectUndo(
                anchorObject,
                $"Create {AnchorName}");

            anchorObject.transform.SetParent(
                parent,
                false);

            Vector3 forward =
                Vector3.ProjectOnPlane(
                    figureTransform.forward,
                    Vector3.up);

            if (forward.sqrMagnitude <
                0.001f)
            {
                forward =
                    Vector3.forward;
            }

            forward.Normalize();

            anchorObject.transform.position =
                figureTransform.position +
                forward * 6f;

            anchorObject.transform.rotation =
                Quaternion.LookRotation(
                    forward,
                    Vector3.up);

            return Undo.AddComponent<
                PrototypeMasterpieceRouteAnchor>(
                    anchorObject);
        }

        private static Material GetOrCreateWorldStrokeMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                WorldStrokeMaterialPath);

            Shader shader = Shader.Find(
                "PaintedAlive/M58 Final/Stroke Telegraph");

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                return material;
            }

            if (!AssetDatabase.IsValidFolder(
                    "Assets/_Project/Art/Materials/M60"))
            {
                AssetDatabase.CreateFolder(
                    "Assets/_Project/Art/Materials",
                    "M60");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "MAT_LivingCanvasWorldStroke"
                };

                AssetDatabase.CreateAsset(
                    material,
                    WorldStrokeMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_EdgeColor"))
                material.SetColor("_EdgeColor", Color.white);
            if (material.HasProperty("_DashFill"))
                material.SetFloat("_DashFill", 0.92f);
            if (material.HasProperty("_PulseStrength"))
                material.SetFloat("_PulseStrength", 0.04f);

            material.renderQueue = 3100;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
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

            GameObject created =
                new GameObject(
                    name);

            Undo.RegisterCreatedObjectUndo(
                created,
                $"Create {name}");

            return created;
        }

        private static Transform GetOrCreateTransform(
            Transform parent,
            string name)
        {
            Transform existing =
                parent.Find(
                    name);

            if (existing != null)
            {
                return existing;
            }

            GameObject child =
                new GameObject(
                    name);

            Undo.RegisterCreatedObjectUndo(
                child,
                $"Create {name}");

            child.transform.SetParent(
                parent,
                false);

            return child.transform;
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

        private static PrototypeMasterpieceAssemblyController
            FindAssemblyForSideCanvas(
                PrototypeLivingSideCanvasController sideCanvas)
        {
            if (sideCanvas == null)
            {
                throw new ArgumentNullException(
                    nameof(sideCanvas));
            }

            PrototypeMasterpieceAssemblyController direct =
                sideCanvas.GetComponent<
                    PrototypeMasterpieceAssemblyController>();

            if (direct != null)
            {
                return direct;
            }

            PrototypeMasterpieceAssemblyController[] candidates =
                UnityEngine.Object.FindObjectsByType<
                    PrototypeMasterpieceAssemblyController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < candidates.Length;
                 index++)
            {
                PrototypeMasterpieceAssemblyController candidate =
                    candidates[index];

                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.SideCanvasSource ==
                        sideCanvas)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "Aynı M45 Yan Tuval kaynağına bağlı " +
                "PrototypeMasterpieceAssemblyController bulunamadı.");
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

        private static MonoBehaviour
            FindRequiredMonoBehaviour(
                string typeName)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object
                    .FindObjectsByType<MonoBehaviour>(
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
                    string.Equals(
                        candidate.GetType().Name,
                        typeName,
                        StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"{typeName} bulunamadı.");
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

        private static string Describe(
            Component component)
        {
            return component == null
                ? "MISSING"
                : $"{component.GetType().Name} @ " +
                  GetHierarchyPath(
                      component.transform);
        }

        private static string GetHierarchyPath(
            Transform transform)
        {
            string path =
                transform.name;

            while (transform.parent != null)
            {
                transform =
                    transform.parent;

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
