#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeLiveCompositionMilestone
    {
        private const string RootName =
            "M53_0_LiveCompositionRiskSpikeD1";

        private const string LabName =
            "M53_0_LiveCompositionLab";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M53_0_LiveCompositionPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.0 - Apply Live Composition Bridge Risk Spike D1")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M53.0 setup Play Mode dışında çalıştırılmalıdır.");
                }

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                PrototypeUnderlayerFrameBraceController frameSource =
                    FindRequired<
                        PrototypeUnderlayerFrameBraceController>();

                MonoBehaviour clarityState =
                    FindRelatedMonoBehaviour(
                        figureMotor.transform,
                        "FigureClarityState");

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

                GameObject lab =
                    GetOrCreateChild(
                        root.transform,
                        LabName);

                ClearChildren(
                    lab.transform);

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

                Vector3 right =
                    Vector3.Cross(
                        Vector3.up,
                        forward).normalized;

                Vector3 basePoint =
                    ResolveGroundPoint(
                        figureMotor.transform.position +
                        right *
                        9.0f +
                        forward *
                        2.5f,
                        figureMotor.transform);

                float halfSpan = 2.55f;

                Vector3 pointA =
                    ResolveGroundPoint(
                        basePoint -
                        forward *
                        halfSpan,
                        figureMotor.transform);

                Vector3 pointB =
                    ResolveGroundPoint(
                        basePoint +
                        forward *
                        halfSpan,
                        figureMotor.transform);

                Vector3 posePosition =
                    (
                        pointA +
                        pointB
                    ) *
                    0.5f +
                    Vector3.up *
                    0.12f;

                Transform anchorPointA =
                    CreateAnchorPoint(
                        lab.transform,
                        "CompositionAnchor_A",
                        pointA);

                Transform anchorPointB =
                    CreateAnchorPoint(
                        lab.transform,
                        "CompositionAnchor_B",
                        pointB);

                Transform posePoint =
                    CreateAnchorPoint(
                        lab.transform,
                        "CompositionPosePoint",
                        posePosition);

                PrototypeLiveCompositionAnchor anchorA =
                    GetOrAdd<
                        PrototypeLiveCompositionAnchor>(
                            anchorPointA.gameObject);

                PrototypeLiveCompositionAnchor anchorB =
                    GetOrAdd<
                        PrototypeLiveCompositionAnchor>(
                            anchorPointB.gameObject);

                anchorA.Configure(
                    PrototypeLiveCompositionAnchorId.A,
                    anchorPointA);

                anchorB.Configure(
                    PrototypeLiveCompositionAnchorId.B,
                    anchorPointB);

                CreateMarker(
                    anchorPointA,
                    "Anchor_A_Marker",
                    new Color(
                        0.72f,
                        0.60f,
                        0.20f,
                        1f),
                    0.72f);

                CreateMarker(
                    anchorPointB,
                    "Anchor_B_Marker",
                    new Color(
                        0.72f,
                        0.60f,
                        0.20f,
                        1f),
                    0.72f);

                CreateMarker(
                    posePoint,
                    "Pose_Marker",
                    new Color(
                        0.92f,
                        0.88f,
                        0.64f,
                        1f),
                    0.52f);

                CreateWorldLabel(
                    anchorPointA,
                    "CANLI KOMP.\nANKRAJ A\n3 + E",
                    new Vector3(
                        0f,
                        1.25f,
                        0f));

                CreateWorldLabel(
                    anchorPointB,
                    "CANLI KOMP.\nANKRAJ B\n3 + E",
                    new Vector3(
                        0f,
                        1.25f,
                        0f));

                CreateWorldLabel(
                    posePoint,
                    "KÖPRÜ POZU\nE = ONAY",
                    new Vector3(
                        0f,
                        1.15f,
                        0f));

                PrototypeLiveCompositionController controller =
                    GetOrAdd<
                        PrototypeLiveCompositionController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, 18f),
                        new Vector2(590f, 124f));

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
                        "CANLI KOMPOZİSYON • TEK FİGÜR KÖPRÜ",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperCenter,
                        new Vector2(0f, 0.72f),
                        new Vector2(1f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(12f, -7f),
                        new Vector2(-24f, -3f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Canlı Kompozisyon ankrajları bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperCenter,
                        new Vector2(0f, 0.24f),
                        new Vector2(1f, 0.75f),
                        new Vector2(0.5f, 1f),
                        new Vector2(12f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "3 ÇERÇEVE TABANCASI • A E • B E • ORTA POZ E",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerCenter,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(0.5f, 0f),
                        new Vector2(12f, 6f),
                        new Vector2(-24f, -2f));

                controller.Configure(
                    figureMotor.transform,
                    figureMotor,
                    frameSource,
                    clarityState,
                    anchorA,
                    anchorB,
                    posePoint,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    anchorA);

                EditorUtility.SetDirty(
                    anchorB);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M53.0 Setup] Live Composition Bridge Risk Spike D1 ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"FrameGunSource={GetHierarchyPath(frameSource.transform)}\n" +
                    $"AnchorA={pointA:F3}\n" +
                    $"AnchorB={pointB:F3}\n" +
                    $"PosePoint={posePosition:F3}\n" +
                    "SingleFigureTemplateEnabled=True\n" +
                    "BridgeTemplateEnabled=True\n" +
                    "TwoEnvironmentalAnchorsRequired=True\n" +
                    "ExplicitParticipationConsentRequired=True\n" +
                    "SingleInputReleaseEnabled=True\n" +
                    "AutomaticNearbyPlayerBindingEnabled=False\n" +
                    "RagdollCompositionEnabled=False\n" +
                    "FreeformPhysicsCompositionEnabled=False\n" +
                    "UsesExistingFrameGunUseToolReadOnly=True\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "MultiplayerCompositionEnabled=False\n" +
                    "NetworkAuthorityEnabled=False",
                    controller);

                EditorUtility.DisplayDialog(
                    "M53.0 Hazır",
                    "İki çevresel Çerçeve Ankrajı + açık poz onayıyla tek Figür " +
                    "Köprü Kompozisyonu risk spike'ı kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M53.0 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.0 - Diagnose Live Composition Bridge")]
        public static void Diagnose()
        {
            PrototypeLiveCompositionController controller =
                FindOptional<
                    PrototypeLiveCompositionController>();

            PrototypeLiveCompositionBridge bridge =
                controller != null
                    ? controller.ActiveBridge
                    : null;

            string report =
                "[M53.0 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"State={(controller != null ? controller.State.ToString() : "N/A")}\n" +
                $"FrameGunSelected={(controller != null && controller.FrameGunSelected)}\n" +
                $"UseToolInputResolved={(controller != null && controller.UseToolInputResolved)}\n" +
                $"InputContract={(controller != null ? controller.InputContract : "N/A")}\n" +
                $"PrimaryToolAllowed={(controller != null && controller.PrimaryToolAllowed)}\n" +
                $"FullStain={(controller != null && controller.FullStain)}\n" +
                $"NearestAnchor={(controller != null ? controller.NearestAnchor : "N/A")}\n" +
                $"NearestAnchorDistance={(controller != null ? controller.NearestAnchorDistance : -1f):F2}\n" +
                $"PoseDistance={(controller != null ? controller.PoseDistance : -1f):F2}\n" +
                $"AnchorLatchCount={(controller != null ? controller.AnchorLatchCount : 0)}\n" +
                $"ExplicitConsentReceived={(controller != null && controller.ExplicitConsentReceived)}\n" +
                $"Composing={(controller != null && controller.Composing)}\n" +
                $"MotorSuppressed={(controller != null && controller.MotorSuppressed)}\n" +
                $"ReleaseInputAvailable={(controller != null && controller.ReleaseInputAvailable)}\n" +
                $"CompositionStartCount={(controller != null ? controller.CompositionStartCount : 0)}\n" +
                $"CompositionReleaseCount={(controller != null ? controller.CompositionReleaseCount : 0)}\n" +
                $"SafetyReleaseCount={(controller != null ? controller.SafetyReleaseCount : 0)}\n" +
                $"RejectedActionCount={(controller != null ? controller.RejectedActionCount : 0)}\n" +
                $"RemainingCompositionSeconds={(controller != null ? controller.RemainingCompositionSeconds : 0f):F2}\n" +
                $"Bridge={(bridge != null ? "OK" : "NONE")}\n" +
                $"BridgeActive={(bridge != null && bridge.ActiveBridge)}\n" +
                $"BridgeColliderSegments={(bridge != null ? bridge.ColliderSegmentCount : 0)}\n" +
                $"BridgeSpanLength={(bridge != null ? bridge.SpanLength : 0f):F2}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "SingleFigureTemplateEnabled=True\n" +
                "BridgeTemplateEnabled=True\n" +
                "TwoEnvironmentalAnchorsRequired=True\n" +
                "ExplicitParticipationConsentRequired=True\n" +
                "SingleInputReleaseEnabled=True\n" +
                "AutomaticNearbyPlayerBindingEnabled=False\n" +
                "RagdollCompositionEnabled=False\n" +
                "FreeformPhysicsCompositionEnabled=False\n" +
                "UsesExistingFrameGunUseToolReadOnly=True\n" +
                "AddsNewGameplayInputAction=False\n" +
                "MultiplayerCompositionEnabled=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                controller);

            EditorUtility.DisplayDialog(
                "M53.0 Diagnose",
                report,
                "Tamam");
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

        private static Transform CreateAnchorPoint(
            Transform parent,
            string name,
            Vector3 position)
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

            return anchor.transform;
        }

        private static void CreateMarker(
            Transform parent,
            string name,
            Color color,
            float diameter)
        {
            GameObject marker =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);

            marker.name = name;

            Undo.RegisterCreatedObjectUndo(
                marker,
                $"Create {name}");

            marker.transform.SetParent(
                parent,
                false);

            marker.transform.localPosition =
                new Vector3(
                    0f,
                    0.055f,
                    0f);

            marker.transform.localScale =
                new Vector3(
                    diameter,
                    0.035f,
                    diameter);

            Renderer renderer =
                marker.GetComponent<
                    Renderer>();

            if (renderer != null)
            {
                Material material =
                    new Material(
                        Shader.Find(
                            "Universal Render Pipeline/Lit") ??
                        Shader.Find(
                            "Standard"));

                material.color = color;

                renderer.sharedMaterial =
                    material;
            }

            Collider collider =
                marker.GetComponent<
                    Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    collider);
            }
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
                "Create M53 Label");

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
                0.105f;

            label.fontSize = 42;

            label.color =
                new Color(
                    0.96f,
                    0.84f,
                    0.42f,
                    1f);
        }

        private static void ClearChildren(
            Transform target)
        {
            for (int index =
                     target.childCount - 1;
                 index >= 0;
                 index--)
            {
                UnityEngine.Object.DestroyImmediate(
                    target.GetChild(index)
                        .gameObject);
            }
        }

        private static MonoBehaviour FindRelatedMonoBehaviour(
            Transform root,
            string typeName)
        {
            MonoBehaviour[] related =
                root.GetComponentsInChildren<
                    MonoBehaviour>(
                        true);

            foreach (MonoBehaviour candidate in related)
            {
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

            foreach (MonoBehaviour candidate in related)
            {
                if (
                    candidate != null &&
                    candidate.GetType().Name ==
                        typeName
                )
                {
                    return candidate;
                }
            }

            return FindOptionalMonoBehaviour(
                typeName);
        }

        private static MonoBehaviour FindRequiredMonoBehaviour(
            string typeName)
        {
            MonoBehaviour found =
                FindOptionalMonoBehaviour(
                    typeName);

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"{typeName} bulunamadı.");
            }

            return found;
        }

        private static MonoBehaviour FindOptionalMonoBehaviour(
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

        private static GameObject GetOrCreateChild(
            Transform parent,
            string name)
        {
            Transform existing =
                parent.Find(
                    name);

            if (existing != null)
            {
                return existing.gameObject;
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

            return child;
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
                    0.055f,
                    0.050f,
                    0.035f,
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
                    0.96f,
                    0.88f,
                    0.60f,
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
