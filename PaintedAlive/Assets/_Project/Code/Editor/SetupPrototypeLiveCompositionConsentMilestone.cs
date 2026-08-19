#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeLiveCompositionConsentMilestone
    {
        private const string RootName =
            "M53_1_LiveCompositionConsentRiskSpikeD2";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M53_1_LiveCompositionConsentPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.1 - Apply Live Composition Consent Risk Spike D2")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M53.1 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeLiveCompositionController liveComposition =
                    FindRequired<
                        PrototypeLiveCompositionController>();

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

                Transform posePoint =
                    FindDescendant(
                        liveComposition.transform,
                        "CompositionPosePoint");

                if (posePoint == null)
                {
                    posePoint =
                        FindSceneTransform(
                            "CompositionPosePoint");
                }

                if (posePoint == null)
                {
                    throw new InvalidOperationException(
                        "M53.0 CompositionPosePoint bulunamadı.");
                }

                GameObject root =
                    GetOrCreateRoot(
                        RootName);

                GameObject proxy =
                    GetOrCreatePartnerProxy(
                        root.transform,
                        posePoint);

                Renderer proxyRenderer =
                    proxy.GetComponent<
                        Renderer>();

                PrototypeLiveCompositionConsentHarness harness =
                    GetOrAdd<
                        PrototypeLiveCompositionConsentHarness>(
                            root);

                harness.Configure(
                    liveComposition,
                    proxy.transform,
                    proxyRenderer);

                liveComposition.ConfigureConsentHarness(
                    harness);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(-20f, -20f),
                        new Vector2(455f, 122f));

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
                        "CANLI KOMPOZİSYON • PARTNER ONAYI",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperRight,
                        new Vector2(0f, 0.72f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(-14f, -7f),
                        new Vector2(-24f, -3f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Partner daveti henüz yok.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperRight,
                        new Vector2(0f, 0.24f),
                        new Vector2(1f, 0.75f),
                        new Vector2(1f, 1f),
                        new Vector2(-14f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "DEV HARNESS: Partner Accept / Withdraw menüleri",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerRight,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(1f, 0f),
                        new Vector2(-14f, 6f),
                        new Vector2(-24f, -2f));

                PrototypeLiveCompositionConsentHud hud =
                    GetOrAdd<
                        PrototypeLiveCompositionConsentHud>(
                            panel.gameObject);

                hud.Configure(
                    harness,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    liveComposition);

                EditorUtility.SetDirty(
                    harness);

                EditorUtility.SetDirty(
                    hud);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M53.1 Setup] Live Composition Consent Risk Spike D2 ready.\n" +
                    $"LiveComposition={GetHierarchyPath(liveComposition.transform)}\n" +
                    $"Harness={GetHierarchyPath(harness.transform)}\n" +
                    $"PartnerProxy={GetHierarchyPath(proxy.transform)}\n" +
                    "RequiresPartnerConsent=True\n" +
                    "IndependentPartnerConsentRequired=True\n" +
                    "IndependentPartnerWithdrawalEnabled=True\n" +
                    "AutomaticNearbyPlayerBindingEnabled=False\n" +
                    "ConsentExpires=True\n" +
                    "ConsentConsumedPerComposition=True\n" +
                    "StaleConsentReusable=False\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "RealNetworkParticipantBound=False\n" +
                    "NetworkAuthorityEnabled=False",
                    harness);

                EditorUtility.DisplayDialog(
                    "M53.1 Hazır",
                    "Partner Proxy davet/onay/geri çekilme güvenlik harness'i kuruldu. " +
                    "Gerçek network katılımcısı bağlanmadı.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M53.1 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.1 - Partner Accept Invitation (Play Mode)")]
        public static void PartnerAccept()
        {
            PrototypeLiveCompositionConsentHarness harness =
                FindPlayModeHarness();

            if (harness == null)
            {
                return;
            }

            if (
                !harness.AcceptInvitationFromDevelopmentHarness()
            )
            {
                Debug.LogWarning(
                    "[M53.1] Partner Accept reddedildi: aktif davet yok.",
                    harness);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.1 - Partner Withdraw Consent (Play Mode)")]
        public static void PartnerWithdraw()
        {
            PrototypeLiveCompositionConsentHarness harness =
                FindPlayModeHarness();

            if (harness == null)
            {
                return;
            }

            if (
                !harness.WithdrawConsentFromDevelopmentHarness(
                    "Development harness withdrawal")
            )
            {
                Debug.LogWarning(
                    "[M53.1] Partner Withdraw: çekilecek aktif onay yok.",
                    harness);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.1 - Diagnose Live Composition Consent")]
        public static void Diagnose()
        {
            PrototypeLiveCompositionConsentHarness harness =
                FindOptional<
                    PrototypeLiveCompositionConsentHarness>();

            PrototypeLiveCompositionController controller =
                FindOptional<
                    PrototypeLiveCompositionController>();

            string report =
                "[M53.1 Diagnose]\n" +
                $"Harness={(harness != null ? "OK" : "MISSING")}\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"State={(harness != null ? harness.State.ToString() : "N/A")}\n" +
                $"StateLabel={(harness != null ? harness.StateLabel : "N/A")}\n" +
                $"InvitationPending={(harness != null && harness.InvitationPending)}\n" +
                $"PartnerConsentGranted={(harness != null && harness.PartnerConsentGranted)}\n" +
                $"CanCommitComposition={(harness != null && harness.CanCommitComposition)}\n" +
                $"WithdrawalRequested={(harness != null && harness.WithdrawalRequested)}\n" +
                $"InvitationCount={(harness != null ? harness.InvitationCount : 0)}\n" +
                $"PartnerAcceptCount={(harness != null ? harness.PartnerAcceptCount : 0)}\n" +
                $"PartnerWithdrawCount={(harness != null ? harness.PartnerWithdrawCount : 0)}\n" +
                $"InvitationExpireCount={(harness != null ? harness.InvitationExpireCount : 0)}\n" +
                $"ConsentSessionCount={(harness != null ? harness.ConsentSessionCount : 0)}\n" +
                $"ReleaseResetCount={(harness != null ? harness.ReleaseResetCount : 0)}\n" +
                $"InvitationRemainingSeconds={(harness != null ? harness.InvitationRemainingSeconds : 0f):F2}\n" +
                $"AcceptedCommitRemainingSeconds={(harness != null ? harness.AcceptedCommitRemainingSeconds : 0f):F2}\n" +
                $"ConsentGeneration={(harness != null ? harness.ConsentGeneration : 0)}\n" +
                $"ActiveConsentGeneration={(harness != null ? harness.ActiveConsentGeneration : 0)}\n" +
                $"Composing={(controller != null && controller.Composing)}\n" +
                $"ExplicitConsentReceived={(controller != null && controller.ExplicitConsentReceived)}\n" +
                $"PartnerWithdrawalReleaseCount={(controller != null ? controller.PartnerWithdrawalReleaseCount : 0)}\n" +
                $"PartnerGateRejectCount={(controller != null ? controller.PartnerGateRejectCount : 0)}\n" +
                $"MotorSuppressed={(controller != null && controller.MotorSuppressed)}\n" +
                $"LastHarnessAction={(harness != null ? harness.LastAction : "N/A")}\n" +
                $"LastControllerAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "LocalPartnerProxyOnly=True\n" +
                "RealNetworkParticipantBound=False\n" +
                "AutomaticNearbyPlayerBindingEnabled=False\n" +
                "IndependentPartnerConsentRequired=True\n" +
                "IndependentPartnerWithdrawalEnabled=True\n" +
                "ConsentExpires=True\n" +
                "ConsentConsumedPerComposition=True\n" +
                "StaleConsentReusable=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                harness != null
                    ? harness
                    : controller);

            EditorUtility.DisplayDialog(
                "M53.1 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeLiveCompositionConsentHarness
            FindPlayModeHarness()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M53.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeLiveCompositionConsentHarness harness =
                FindOptional<
                    PrototypeLiveCompositionConsentHarness>();

            if (harness == null)
            {
                Debug.LogError(
                    "[M53.1] Consent harness bulunamadı.");
            }

            return harness;
        }

        private static GameObject GetOrCreatePartnerProxy(
            Transform parent,
            Transform posePoint)
        {
            Transform existing =
                parent.Find(
                    "M53_1_PartnerProxy");

            GameObject proxy;

            if (existing != null)
            {
                proxy = existing.gameObject;
            }
            else
            {
                proxy =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Capsule);

                proxy.name =
                    "M53_1_PartnerProxy";

                Undo.RegisterCreatedObjectUndo(
                    proxy,
                    "Create M53.1 Partner Proxy");

                proxy.transform.SetParent(
                    parent,
                    true);
            }

            Vector3 right =
                posePoint.right;

            if (
                right.sqrMagnitude <
                0.001f
            )
            {
                right = Vector3.right;
            }

            proxy.transform.position =
                posePoint.position +
                right.normalized *
                1.10f +
                Vector3.up *
                0.90f;

            proxy.transform.rotation =
                posePoint.rotation;

            proxy.transform.localScale =
                new Vector3(
                    0.58f,
                    0.88f,
                    0.58f);

            Collider collider =
                proxy.GetComponent<
                    Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    collider);
            }

            Renderer renderer =
                proxy.GetComponent<
                    Renderer>();

            if (
                renderer != null &&
                renderer.sharedMaterial == null
            )
            {
                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit") ??
                    Shader.Find(
                        "Standard");

                if (shader != null)
                {
                    Material material =
                        new Material(
                            shader);

                    material.name =
                        "M53_1_PartnerProxy_RuntimeSetup";

                    renderer.sharedMaterial =
                        material;
                }
            }

            Transform label =
                proxy.transform.Find(
                    "Label");

            if (label == null)
            {
                GameObject labelObject =
                    new GameObject(
                        "Label");

                labelObject.transform.SetParent(
                    proxy.transform,
                    false);

                labelObject.transform.localPosition =
                    new Vector3(
                        0f,
                        1.65f,
                        0f);

                TextMesh text =
                    labelObject.AddComponent<
                        TextMesh>();

                text.text =
                    "PARTNER PROXY\nDAVET / ONAY";

                text.anchor =
                    TextAnchor.MiddleCenter;

                text.alignment =
                    TextAlignment.Center;

                text.characterSize =
                    0.13f;

                text.fontSize = 38;

                text.color =
                    new Color(
                        0.95f,
                        0.90f,
                        0.70f,
                        1f);
            }

            return proxy;
        }

        private static Transform FindDescendant(
            Transform root,
            string name)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] all =
                root.GetComponentsInChildren<
                    Transform>(
                        true);

            foreach (Transform candidate in all)
            {
                if (
                    candidate != null &&
                    candidate.name ==
                        name
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform FindSceneTransform(
            string name)
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
                        name
                )
                {
                    return candidate.transform;
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
                    0.045f,
                    0.050f,
                    0.040f,
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
                    0.88f,
                    0.96f,
                    0.78f,
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
