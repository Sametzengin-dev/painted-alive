#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeLiveCompositionPartnerPoseMilestone
    {
        private const string RootName =
            "M53_3_LiveCompositionPartnerPoseRiskSpikeD4";

        private const string PartnerProxyName =
            "M53_1_PartnerProxy";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M53_3_LiveCompositionPartnerPosePanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.3 - Apply Live Composition Partner Pose Risk Spike D4")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M53.3 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeLiveCompositionController liveComposition =
                    FindRequired<
                        PrototypeLiveCompositionController>();

                PrototypeLiveCompositionConsentHarness consentHarness =
                    FindRequired<
                        PrototypeLiveCompositionConsentHarness>();

                GameObject partnerProxy =
                    FindSceneObject(
                        PartnerProxyName);

                if (partnerProxy == null)
                {
                    throw new InvalidOperationException(
                        "M53.1 Partner Proxy bulunamadı. M53.1 setup sahnede mevcut olmalıdır.");
                }

                // Partner Proxy must remain a non-gameplay visual participant.
                Collider[] proxyColliders =
                    partnerProxy.GetComponentsInChildren<
                        Collider>(
                            true);

                foreach (Collider collider in proxyColliders)
                {
                    if (collider != null)
                    {
                        Undo.DestroyObjectImmediate(
                            collider);
                    }
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

                PrototypeLiveCompositionPartnerPoseBinder binder =
                    GetOrAdd<
                        PrototypeLiveCompositionPartnerPoseBinder>(
                            root);

                binder.Configure(
                    liveComposition,
                    consentHarness,
                    partnerProxy.transform);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f),
                        new Vector2(20f, 0f),
                        new Vector2(470f, 112f));

                CanvasGroup group =
                    GetOrAdd<CanvasGroup>(
                        panel.gameObject);

                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;

                PrototypeLiveCompositionPartnerPoseHud hud =
                    GetOrAdd<
                        PrototypeLiveCompositionPartnerPoseHud>(
                            panel.gameObject);

                Text title =
                    CreateText(
                        panel,
                        "TitleText",
                        "CANLI KOMPOZİSYON • PARTNER POZ BAĞI",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.70f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -7f),
                        new Vector2(-24f, -3f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Partner fiziksel poz bağı bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.22f),
                        new Vector2(1f, 0.73f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "M53.1 ORTAK KÖPRÜ → PARTNER POZU OTOMATİK BAĞLANIR",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.25f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 6f),
                        new Vector2(-24f, -2f));

                hud.Configure(
                    binder,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    binder);

                EditorUtility.SetDirty(
                    hud);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M53.3 Setup] Live Composition Partner Pose Risk Spike D4 ready.\n" +
                    $"LiveComposition={GetHierarchyPath(liveComposition.transform)}\n" +
                    $"ConsentHarness={GetHierarchyPath(consentHarness.transform)}\n" +
                    $"PartnerProxy={GetHierarchyPath(partnerProxy.transform)}\n" +
                    $"Binder={GetHierarchyPath(binder.transform)}\n" +
                    "PartnerProxyPhysicalContributionEnabled=True\n" +
                    "PartnerProxyFollowsWatercolorDeformation=True\n" +
                    "IndependentWithdrawalRestoresPartnerPose=True\n" +
                    "FigureReleaseRestoresPartnerPose=True\n" +
                    "PartnerProxyHasGameplayCollider=False\n" +
                    "PartnerProxyAffectsBridgeCollision=False\n" +
                    "RealSecondFigureMotorBound=False\n" +
                    "AutomaticNearbyPlayerBindingEnabled=False\n" +
                    "IKEnabled=False\n" +
                    "RagdollEnabled=False\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "NetworkAuthorityEnabled=False",
                    binder);

                EditorUtility.DisplayDialog(
                    "M53.3 Hazır",
                    "Partner Proxy artık ortak kompozisyon sırasında fiziksel poz formuna bağlanacak. " +
                    "Gerçek ikinci Figure motoru veya network authority eklenmedi.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M53.3 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.3 - Diagnose Live Composition Partner Pose")]
        public static void Diagnose()
        {
            PrototypeLiveCompositionPartnerPoseBinder binder =
                FindOptional<
                    PrototypeLiveCompositionPartnerPoseBinder>();

            PrototypeLiveCompositionController live =
                FindOptional<
                    PrototypeLiveCompositionController>();

            PrototypeLiveCompositionConsentHarness consent =
                FindOptional<
                    PrototypeLiveCompositionConsentHarness>();

            string report =
                "[M53.3 Diagnose]\n" +
                $"Binder={(binder != null ? "OK" : "MISSING")}\n" +
                $"LiveComposition={(live != null ? "OK" : "MISSING")}\n" +
                $"ConsentHarness={(consent != null ? "OK" : "MISSING")}\n" +
                $"Composing={(live != null && live.Composing)}\n" +
                $"ConsentState={(consent != null ? consent.State.ToString() : "N/A")}\n" +
                $"PartnerPoseBound={(binder != null && binder.PartnerPoseBound)}\n" +
                $"CompositionObserved={(binder != null && binder.CompositionObserved)}\n" +
                $"PartnerConsentSessionObserved={(binder != null && binder.PartnerConsentSessionObserved)}\n" +
                $"BindCount={(binder != null ? binder.BindCount : 0)}\n" +
                $"ReleaseCount={(binder != null ? binder.ReleaseCount : 0)}\n" +
                $"WatercolorFollowFrameCount={(binder != null ? binder.WatercolorFollowFrameCount : 0)}\n" +
                $"BoundTargetPosition={(binder != null ? binder.BoundTargetPosition.ToString("F3") : "N/A")}\n" +
                $"LastState={(binder != null ? binder.LastState : "N/A")}\n" +
                "PartnerProxyPhysicalContributionEnabled=True\n" +
                "PartnerProxyFollowsWatercolorDeformation=True\n" +
                "IndependentWithdrawalRestoresPartnerPose=True\n" +
                "FigureReleaseRestoresPartnerPose=True\n" +
                "PartnerProxyHasGameplayCollider=False\n" +
                "PartnerProxyAffectsBridgeCollision=False\n" +
                "RealSecondFigureMotorBound=False\n" +
                "AutomaticNearbyPlayerBindingEnabled=False\n" +
                "IKEnabled=False\n" +
                "RagdollEnabled=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                binder != null
                    ? binder
                    : live);

            EditorUtility.DisplayDialog(
                "M53.3 Diagnose",
                report,
                "Tamam");
        }

        private static GameObject FindSceneObject(
            string objectName)
        {
            GameObject[] objects =
                UnityEngine.Object.FindObjectsByType<
                    GameObject>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            foreach (GameObject candidate in objects)
            {
                if (
                    candidate != null &&
                    candidate.scene.IsValid() &&
                    candidate.name ==
                        objectName
                )
                {
                    return candidate;
                }
            }

            return null;
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
                    0.038f,
                    0.050f,
                    0.040f,
                    0.94f);

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
                    0.95f,
                    0.62f,
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

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;

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
