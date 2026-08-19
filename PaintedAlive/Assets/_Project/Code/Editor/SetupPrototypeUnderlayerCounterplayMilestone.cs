#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeUnderlayerCounterplayMilestone
    {
        private const string RootName =
            "M52_1_UnderlayerCounterplayRiskSpikeC2";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M52_1_UnderlayerCounterplayPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.1 - Apply Underlayer Counterplay Risk Spike C2")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M52.1 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeUnderlayerDiveController dive =
                    FindRequired<
                        PrototypeUnderlayerDiveController>();

                MonoBehaviour traceCounterplay =
                    FindRequiredMonoBehaviour(
                        "PrototypeTraceWeaveCounterplayController");

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                MonoBehaviour clarityState =
                    FindRelatedMonoBehaviour(
                        figureMotor.transform,
                        "FigureClarityState");

                if (clarityState == null)
                {
                    throw new InvalidOperationException(
                        "FigureClarityState bulunamadı.");
                }

                if (
                    dive.SeamA == null ||
                    dive.SeamB == null ||
                    dive.SeamA.UnderlayerAnchor == null ||
                    dive.SeamB.UnderlayerAnchor == null
                )
                {
                    throw new InvalidOperationException(
                        "M52.0 Seam A/B sözleşmesi eksik.");
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

                PrototypeUnderlayerCounterplayController controller =
                    GetOrAdd<
                        PrototypeUnderlayerCounterplayController>(
                            root);

                PrototypeUnderlayerInkRootThreat threat =
                    GetOrAdd<
                        PrototypeUnderlayerInkRootThreat>(
                            GetOrCreateChild(
                                root.transform,
                                "M52_1_InkRootPatrol"));

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(1f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(-20f, 20f),
                        new Vector2(485f, 136f));

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
                        "ALT KATMAN • KARŞI-OYUN",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperRight,
                        new Vector2(0f, 0.73f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(-14f, -8f),
                        new Vector2(-24f, -4f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Alt Katman karşı-oyunu bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperRight,
                        new Vector2(0f, 0.25f),
                        new Vector2(1f, 0.76f),
                        new Vector2(1f, 1f),
                        new Vector2(-14f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "PAINTER DİKİŞ + F4/F10 • STAIN TEMAS",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerRight,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(1f, 0f),
                        new Vector2(-14f, 6f),
                        new Vector2(-24f, -2f));

                controller.Configure(
                    dive,
                    traceCounterplay,
                    clarityState,
                    figureMotor.transform,
                    group,
                    title,
                    state,
                    controls);

                threat.Configure(
                    dive,
                    figureMotor.transform,
                    dive.SeamA.UnderlayerAnchor,
                    dive.SeamB.UnderlayerAnchor);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    threat);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M52.1 Setup] Underlayer Counterplay Risk Spike C2 ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"Dive={GetHierarchyPath(dive.transform)}\n" +
                    $"TraceCounterplay={GetHierarchyPath(traceCounterplay.transform)}\n" +
                    $"FigureClarityState={GetHierarchyPath(clarityState.transform)}\n" +
                    $"InkRoot={GetHierarchyPath(threat.transform)}\n" +
                    "UsesExistingM51PainterMaterialActionsReadOnly=True\n" +
                    "AddsNewPainterGameplayInputAction=False\n" +
                    "WatercolorSharedBinding=F4\n" +
                    "OilPaintSharedBinding=F10\n" +
                    "OilSealSeconds=6\n" +
                    "PreventsLastSafeExitSeal=True\n" +
                    "WatercolorUnderlayerSeepEnabled=True\n" +
                    "WatercolorRawDamageEnabled=False\n" +
                    "StainCrackEntryEnabled=True\n" +
                    "StainCrackNeedsNewInput=False\n" +
                    "InkRootRawDamageEnabled=False\n" +
                    "NetworkAuthorityEnabled=False",
                    controller);

                EditorUtility.DisplayDialog(
                    "M52.1 Hazır",
                    "M51.2'nin mevcut F4/F10 Painter malzeme inputları Alt Katman " +
                    "dikişlerine bağlandı; Yağlı Boya mühür, Suluboya sızıntı, " +
                    "Full Stain temasla çatlak girişi ve Mürekkep Kökü tehdidi kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M52.1 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.1 - Force Oil Seal A (Play Mode)")]
        public static void ForceOilA()
        {
            FindPlayModeController()?
                .ForceOilSealA();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.1 - Force Watercolor Seep A (Play Mode)")]
        public static void ForceWaterA()
        {
            FindPlayModeController()?
                .ForceWatercolorSeepA();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.1 - Diagnose Underlayer Counterplay")]
        public static void Diagnose()
        {
            PrototypeUnderlayerCounterplayController controller =
                FindOptional<
                    PrototypeUnderlayerCounterplayController>();

            PrototypeUnderlayerDiveController dive =
                FindOptional<
                    PrototypeUnderlayerDiveController>();

            PrototypeUnderlayerInkRootThreat threat =
                FindOptional<
                    PrototypeUnderlayerInkRootThreat>();

            PrototypeUnderlayerSeam seamA =
                dive != null
                    ? dive.SeamA
                    : null;

            PrototypeUnderlayerSeam seamB =
                dive != null
                    ? dive.SeamB
                    : null;

            string report =
                "[M52.1 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"Dive={(dive != null ? "OK" : "MISSING")}\n" +
                $"InkRoot={(threat != null ? "OK" : "MISSING")}\n" +
                $"PainterRoleActive={(controller != null && controller.PainterRoleActive)}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"SharedMaterialInputResolved={(controller != null && controller.SharedMaterialInputResolved)}\n" +
                $"SharedInputContract={(controller != null ? controller.SharedInputContract : "N/A")}\n" +
                $"PainterCameraResolved={(controller != null && controller.PainterCameraResolved)}\n" +
                $"SeamAimValid={(controller != null && controller.SeamAimValid)}\n" +
                $"AimedSeam={(controller != null ? controller.AimedSeam : "N/A")}\n" +
                $"SeamASealed={(seamA != null && seamA.IsSealed)}\n" +
                $"SeamASealRemaining={(seamA != null ? seamA.SealedRemainingSeconds : 0f):F2}\n" +
                $"SeamASealCount={(seamA != null ? seamA.SealCount : 0)}\n" +
                $"SeamBSealed={(seamB != null && seamB.IsSealed)}\n" +
                $"SeamBSealRemaining={(seamB != null ? seamB.SealedRemainingSeconds : 0f):F2}\n" +
                $"SeamBSealCount={(seamB != null ? seamB.SealCount : 0)}\n" +
                $"WatercolorSuccessCount={(controller != null ? controller.WatercolorSuccessCount : 0)}\n" +
                $"OilSuccessCount={(controller != null ? controller.OilSuccessCount : 0)}\n" +
                $"WatercolorSeepActive={(controller != null && controller.WatercolorSeepActive)}\n" +
                $"SeepRemainingSeconds={(controller != null ? controller.SeepRemainingSeconds : 0f):F2}\n" +
                $"SeepContactFrameCount={(controller != null ? controller.SeepContactFrameCount : 0)}\n" +
                $"SeepAppliedDistance={(controller != null ? controller.SeepAppliedDistance : 0f):F2}\n" +
                $"ClarityLevel={(controller != null ? controller.ClarityLevel : "N/A")}\n" +
                $"FullStain={(controller != null && controller.FullStain)}\n" +
                $"StainCrackReady={(controller != null && controller.StainCrackReady)}\n" +
                $"StainCrackProgress={(controller != null ? controller.StainCrackProgress : 0f):F2}\n" +
                $"StainEntryCount={(controller != null ? controller.StainEntryCount : 0)}\n" +
                $"StainExitCount={(controller != null ? controller.StainExitCount : 0)}\n" +
                $"InkRootPatrolProgress={(threat != null ? threat.PatrolProgress : 0f):F2}\n" +
                $"InkRootContactRecoveryCount={(threat != null ? threat.ContactRecoveryCount : 0)}\n" +
                $"InUnderlayer={(dive != null && dive.InUnderlayer)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "UsesExistingM51PainterMaterialActionsReadOnly=True\n" +
                "AddsNewPainterGameplayInputAction=False\n" +
                "OilPaintSeamSealEnabled=True\n" +
                "PreventsLastSafeExitSeal=True\n" +
                "WatercolorUnderlayerSeepEnabled=True\n" +
                "WatercolorRawDamageEnabled=False\n" +
                "StainCrackEntryEnabled=True\n" +
                "StainCrackNeedsNewInput=False\n" +
                "InkRootRawDamageEnabled=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : dive);

            EditorUtility.DisplayDialog(
                "M52.1 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeUnderlayerCounterplayController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M52.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeUnderlayerCounterplayController controller =
                FindOptional<
                    PrototypeUnderlayerCounterplayController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M52.1] Controller bulunamadı.");
            }

            return controller;
        }

        private static MonoBehaviour FindRelatedMonoBehaviour(
            Transform root,
            string typeName)
        {
            if (root != null)
            {
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
                    0.045f,
                    0.040f,
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
                    0.86f,
                    0.90f,
                    0.96f,
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
