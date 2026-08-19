#if UNITY_EDITOR
using System;
using PaintedAlive.MatchFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeCoreMatchFoundationMilestone
    {
        private const string RootName =
            "M54_0_CoreMatchFoundation";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M54_0_CoreMatchPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "54.0 - Apply Feature Freeze Core Match Foundation")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M54.0 setup Play Mode dışında çalıştırılmalıdır.");
                }

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                MonoBehaviour clarityState =
                    FindRelatedClarityState(
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

                ClearGeneratedChild(
                    root.transform,
                    "MatchAnchors");

                GameObject anchors =
                    new GameObject(
                        "MatchAnchors");

                Undo.RegisterCreatedObjectUndo(
                    anchors,
                    "Create M54.0 Match Anchors");

                anchors.transform.SetParent(
                    root.transform,
                    false);

                Vector3 startPosition =
                    ResolveGroundPoint(
                        figureMotor.transform.position,
                        figureMotor.transform);

                Vector3 forward =
                    Vector3.ProjectOnPlane(
                        figureMotor.transform.forward,
                        Vector3.up);

                if (
                    forward.sqrMagnitude <
                    0.001f
                )
                {
                    forward =
                        Vector3.forward;
                }
                else
                {
                    forward.Normalize();
                }

                Vector3 exitPosition =
                    ResolveBestExitPosition(
                        startPosition,
                        forward,
                        figureMotor.transform);

                Transform startPoint =
                    CreateAnchor(
                        anchors.transform,
                        "M54_MatchStart",
                        startPosition,
                        Quaternion.LookRotation(
                            Vector3.ProjectOnPlane(
                                exitPosition -
                                startPosition,
                                Vector3.up).normalized,
                            Vector3.up));

                Transform exitPoint =
                    CreateAnchor(
                        anchors.transform,
                        "M54_FrameExit",
                        exitPosition,
                        startPoint.rotation);

                CreateStartMarker(
                    startPoint);

                PrototypeCoreMatchExitTrigger exitTrigger =
                    CreateExitGate(
                        exitPoint,
                        figureMotor.transform);

                PrototypeCoreMatchController controller =
                    GetOrAdd<
                        PrototypeCoreMatchController>(
                            root);

                controller.Configure(
                    figureMotor.transform,
                    clarityState,
                    startPoint,
                    exitPoint,
                    exitTrigger);

                exitTrigger.Configure(
                    controller,
                    figureMotor.transform);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(20f, -20f),
                        new Vector2(560f, 126f));

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
                        "CORE MATCH • FEATURE FREEZE",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.76f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -7f),
                        new Vector2(-24f, -3f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Core Match Foundation bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.45f),
                        new Vector2(1f, 0.79f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f));

                Text score =
                    CreateText(
                        panel,
                        "ScoreText",
                        "MESAFE 0/1000   ÇIKIŞ —   TOPLAM 0",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.20f),
                        new Vector2(1f, 0.50f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f));

                Text contract =
                    CreateText(
                        panel,
                        "ContractText",
                        "LEKE MAÇI BİTİRMEZ • RESSAM KILL PUANI YOK • YENİ INPUT YOK",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.23f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 5f),
                        new Vector2(-24f, -2f));

                PrototypeCoreMatchHud hud =
                    GetOrAdd<
                        PrototypeCoreMatchHud>(
                            panel.gameObject);

                hud.Configure(
                    controller,
                    group,
                    title,
                    state,
                    score,
                    contract);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    exitTrigger);

                EditorUtility.SetDirty(
                    hud);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M54.0 Setup] Feature Freeze + Core Match Foundation ready.\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"ClarityState={(clarityState != null ? clarityState.GetType().Name : "NONE")}\n" +
                    $"Start={startPosition:F3}\n" +
                    $"Exit={exitPosition:F3}\n" +
                    $"TestDistance={Vector3.Distance(startPosition, exitPosition):F2}\n" +
                    "FeatureFreezeActive=True\n" +
                    "NewMajorGameplayMechanicsAllowed=False\n" +
                    "UsesGddDistanceScoreContract=True\n" +
                    "UsesGddExitBonusContract=True\n" +
                    "PainterKillScoreEnabled=False\n" +
                    "StainEndsMatch=False\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "NetworkAuthorityEnabled=False\n" +
                    "MultiplayerFoundationQueued=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M54.0 Hazır",
                    "Feature freeze etkin. Başlangıç → aktif maç → çerçeve çıkışı / süre " +
                    "sonu → sonuç → reset omurgası kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M54.0 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "54.0 - Reset Core Match (Play Mode)")]
        public static void ResetMatch()
        {
            PrototypeCoreMatchController controller =
                FindPlayModeController();

            if (controller != null)
            {
                controller.ResetMatchAndBeginPreparation();
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "54.0 - Force Time Expiry (Play Mode)")]
        public static void ForceTimeExpiry()
        {
            PrototypeCoreMatchController controller =
                FindPlayModeController();

            if (controller != null)
            {
                controller.ForceTimeExpiry();
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "54.0 - Diagnose Core Match Foundation")]
        public static void Diagnose()
        {
            PrototypeCoreMatchController controller =
                FindOptional<
                    PrototypeCoreMatchController>();

            PrototypeCoreMatchExitTrigger exitTrigger =
                FindOptional<
                    PrototypeCoreMatchExitTrigger>();

            string report =
                "[M54.0 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ExitTrigger={(exitTrigger != null ? "OK" : "MISSING")}\n" +
                $"Phase={(controller != null ? controller.Phase.ToString() : "N/A")}\n" +
                $"Result={(controller != null ? controller.Result.ToString() : "N/A")}\n" +
                $"MatchRunId={(controller != null ? controller.MatchRunId : 0)}\n" +
                $"PreparationRemaining={(controller != null ? controller.PreparationRemainingSeconds : 0f):F2}\n" +
                $"RoundRemaining={(controller != null ? controller.RoundRemainingSeconds : 0f):F2}\n" +
                $"BestProgress={(controller != null ? controller.BestProgressNormalized : 0f):F3}\n" +
                $"DistanceScore={(controller != null ? controller.DistanceScore : 0)}\n" +
                $"Escaped={(controller != null && controller.Escaped)}\n" +
                $"CurrentScore={(controller != null ? controller.CurrentScore : 0)}\n" +
                $"ClarityLevel={(controller != null ? controller.ClarityLevel : "N/A")}\n" +
                $"NormalizedClarity={(controller != null ? controller.NormalizedClarity : -1f):F3}\n" +
                $"FigureExitCount={(controller != null ? controller.FigureExitCount : 0)}\n" +
                $"TimeExpiredCount={(controller != null ? controller.TimeExpiredCount : 0)}\n" +
                $"ResetCount={(controller != null ? controller.ResetCount : 0)}\n" +
                $"ValidExitTriggerCount={(exitTrigger != null ? exitTrigger.ValidExitTriggerCount : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "FeatureFreezeActive=True\n" +
                "NewMajorGameplayMechanicsAllowed=False\n" +
                "ExistingSystemsIntegrationPriority=True\n" +
                "CoreMatchProductionPriority=True\n" +
                "AnimationProductionPassQueued=True\n" +
                "MultiplayerFoundationQueued=True\n" +
                "NewLiveCompositionTemplatesFrozen=True\n" +
                "PainterKillScoreEnabled=False\n" +
                "StainEndsMatch=False\n" +
                "LocalStraightLineProgressProxy=True\n" +
                "FinalRouteGraphScoringImplemented=False\n" +
                "FullWorldStateResetImplemented=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                controller);

            EditorUtility.DisplayDialog(
                "M54.0 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeCoreMatchController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M54.0",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeCoreMatchController controller =
                FindOptional<
                    PrototypeCoreMatchController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M54.0] Core Match controller bulunamadı.");
            }

            return controller;
        }

        private static Vector3 ResolveBestExitPosition(
            Vector3 start,
            Vector3 preferredForward,
            Transform figureRoot)
        {
            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    preferredForward);

            Vector3[] directions =
            {
                preferredForward,
                (preferredForward + right).normalized,
                right,
                (preferredForward - right).normalized,
                -right,
                -preferredForward
            };

            const float targetDistance = 12f;

            for (int index = 0;
                 index < directions.Length;
                 index++)
            {
                Vector3 desired =
                    start +
                    directions[index] *
                    targetDistance;

                Vector3 grounded =
                    ResolveGroundPoint(
                        desired,
                        figureRoot);

                float horizontal =
                    Vector3.Distance(
                        Vector3.ProjectOnPlane(
                            grounded,
                            Vector3.up),
                        Vector3.ProjectOnPlane(
                            start,
                            Vector3.up));

                if (horizontal >= 8f)
                {
                    return grounded;
                }
            }

            return
                ResolveGroundPoint(
                    start +
                    preferredForward *
                    8f,
                    figureRoot);
        }

        private static Vector3 ResolveGroundPoint(
            Vector3 desired,
            Transform figureRoot)
        {
            Vector3 origin =
                desired +
                Vector3.up *
                8f;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    origin,
                    Vector3.down,
                    24f,
                    ~0,
                    QueryTriggerInteraction.Ignore);

            Array.Sort(
                hits,
                (
                    left,
                    right) =>
                    left.distance.CompareTo(
                        right.distance));

            for (int index = 0;
                 index < hits.Length;
                 index++)
            {
                RaycastHit hit =
                    hits[index];

                if (
                    hit.collider == null ||
                    (
                        figureRoot != null &&
                        (
                            hit.collider.transform ==
                                figureRoot ||
                            hit.collider.transform.IsChildOf(
                                figureRoot)
                        )
                    )
                )
                {
                    continue;
                }

                if (
                    Vector3.Dot(
                        hit.normal,
                        Vector3.up) <
                    0.45f
                )
                {
                    continue;
                }

                return hit.point;
            }

            return desired;
        }

        private static Transform CreateAnchor(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject anchor =
                new GameObject(
                    name);

            Undo.RegisterCreatedObjectUndo(
                anchor,
                $"Create {name}");

            anchor.transform.SetParent(
                parent,
                true);

            anchor.transform.position =
                position;

            anchor.transform.rotation =
                rotation;

            return anchor.transform;
        }

        private static void CreateStartMarker(
            Transform startPoint)
        {
            GameObject marker =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            marker.name =
                "M54_StartLineVisual";

            Undo.RegisterCreatedObjectUndo(
                marker,
                "Create M54 Start Marker");

            marker.transform.SetParent(
                startPoint,
                false);

            marker.transform.localPosition =
                new Vector3(
                    0f,
                    0.035f,
                    0f);

            marker.transform.localScale =
                new Vector3(
                    3.2f,
                    0.07f,
                    0.18f);

            RemoveCollider(
                marker);

            ApplyColor(
                marker,
                new Color(
                    0.38f,
                    0.82f,
                    0.92f,
                    1f));

            CreateWorldLabel(
                startPoint,
                "M54 MATCH START",
                new Vector3(
                    0f,
                    1.35f,
                    0f));
        }

        private static PrototypeCoreMatchExitTrigger CreateExitGate(
            Transform exitPoint,
            Transform figureRoot)
        {
            GameObject gateRoot =
                new GameObject(
                    "M54_FrameExitGate");

            Undo.RegisterCreatedObjectUndo(
                gateRoot,
                "Create M54 Exit Gate");

            gateRoot.transform.SetParent(
                exitPoint,
                false);

            gateRoot.transform.localPosition =
                Vector3.zero;

            gateRoot.transform.localRotation =
                Quaternion.identity;

            CreateGateBar(
                gateRoot.transform,
                "LeftPost",
                new Vector3(
                    -1.45f,
                    1.45f,
                    0f),
                new Vector3(
                    0.18f,
                    2.9f,
                    0.18f));

            CreateGateBar(
                gateRoot.transform,
                "RightPost",
                new Vector3(
                    1.45f,
                    1.45f,
                    0f),
                new Vector3(
                    0.18f,
                    2.9f,
                    0.18f));

            CreateGateBar(
                gateRoot.transform,
                "TopBar",
                new Vector3(
                    0f,
                    2.85f,
                    0f),
                new Vector3(
                    3.08f,
                    0.18f,
                    0.18f));

            GameObject triggerObject =
                new GameObject(
                    "ExitTrigger");

            triggerObject.transform.SetParent(
                gateRoot.transform,
                false);

            triggerObject.transform.localPosition =
                new Vector3(
                    0f,
                    1.4f,
                    0f);

            BoxCollider trigger =
                triggerObject.AddComponent<
                    BoxCollider>();

            trigger.isTrigger = true;

            trigger.size =
                new Vector3(
                    2.65f,
                    2.8f,
                    0.9f);

            PrototypeCoreMatchExitTrigger exitTrigger =
                triggerObject.AddComponent<
                    PrototypeCoreMatchExitTrigger>();

            exitTrigger.Configure(
                null,
                figureRoot);

            CreateWorldLabel(
                exitPoint,
                "FRAME EXIT\n+250",
                new Vector3(
                    0f,
                    3.55f,
                    0f));

            return exitTrigger;
        }

        private static void CreateGateBar(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject bar =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            bar.name = name;

            Undo.RegisterCreatedObjectUndo(
                bar,
                $"Create {name}");

            bar.transform.SetParent(
                parent,
                false);

            bar.transform.localPosition =
                localPosition;

            bar.transform.localScale =
                localScale;

            RemoveCollider(
                bar);

            ApplyColor(
                bar,
                new Color(
                    0.96f,
                    0.78f,
                    0.28f,
                    1f));
        }

        private static void RemoveCollider(
            GameObject target)
        {
            Collider collider =
                target.GetComponent<
                    Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    collider);
            }
        }

        private static void ApplyColor(
            GameObject target,
            Color color)
        {
            Renderer renderer =
                target.GetComponent<
                    Renderer>();

            if (renderer == null)
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Standard");
            }

            if (shader == null)
            {
                return;
            }

            Material material =
                new Material(
                    shader);

            material.color =
                color;

            renderer.sharedMaterial =
                material;
        }

        private static void CreateWorldLabel(
            Transform parent,
            string value,
            Vector3 localPosition)
        {
            GameObject labelObject =
                new GameObject(
                    "Label");

            Undo.RegisterCreatedObjectUndo(
                labelObject,
                "Create M54 Label");

            labelObject.transform.SetParent(
                parent,
                false);

            labelObject.transform.localPosition =
                localPosition;

            TextMesh label =
                labelObject.AddComponent<
                    TextMesh>();

            label.text = value;
            label.anchor =
                TextAnchor.MiddleCenter;
            label.alignment =
                TextAlignment.Center;
            label.characterSize =
                0.11f;
            label.fontSize = 40;
            label.color =
                new Color(
                    0.96f,
                    0.88f,
                    0.50f,
                    1f);
        }

        private static MonoBehaviour FindRelatedClarityState(
            Transform figureRoot)
        {
            string[] acceptedNames =
            {
                "FigureClarityState",
                "ClarityState"
            };

            MonoBehaviour[] related =
                figureRoot.GetComponentsInChildren<
                    MonoBehaviour>(
                        true);

            foreach (MonoBehaviour candidate in related)
            {
                if (
                    candidate != null &&
                    NameAccepted(
                        candidate.GetType().Name,
                        acceptedNames)
                )
                {
                    return candidate;
                }
            }

            related =
                figureRoot.GetComponentsInParent<
                    MonoBehaviour>(
                        true);

            foreach (MonoBehaviour candidate in related)
            {
                if (
                    candidate != null &&
                    NameAccepted(
                        candidate.GetType().Name,
                        acceptedNames)
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

            foreach (MonoBehaviour candidate in all)
            {
                if (
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid() &&
                    NameAccepted(
                        candidate.GetType().Name,
                        acceptedNames)
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool NameAccepted(
            string candidate,
            string[] accepted)
        {
            for (int index = 0;
                 index < accepted.Length;
                 index++)
            {
                if (
                    candidate ==
                    accepted[index]
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static MonoBehaviour FindRequiredMonoBehaviour(
            string typeName)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            foreach (MonoBehaviour candidate in behaviours)
            {
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

        private static void ClearGeneratedChild(
            Transform parent,
            string childName)
        {
            Transform existing =
                parent.Find(
                    childName);

            if (existing != null)
            {
                Undo.DestroyObjectImmediate(
                    existing.gameObject);
            }
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
            Vector2 sizeDelta)
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

            image.color =
                new Color(
                    0.035f,
                    0.045f,
                    0.050f,
                    0.95f);

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
            Vector2 sizeDelta)
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
            text.color =
                new Color(
                    0.82f,
                    0.94f,
                    1f,
                    1f);
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
                    $"{name} RectTransform içermiyor.");
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
                    $"{typeof(T).Name} eklenemedi.");
            }

            return added;
        }

        private static T FindOptional<T>()
            where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (T candidate in objects)
            {
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
