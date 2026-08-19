#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeLiveCompositionPainterCounterplayMilestone
    {
        private const string RootName =
            "M53_2_LiveCompositionPainterCounterplayRiskSpikeD3";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M53_2_LiveCompositionPainterCounterplayPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.2 - Apply Live Composition Painter Counterplay Risk Spike D3")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M53.2 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeLiveCompositionController liveComposition =
                    FindRequired<
                        PrototypeLiveCompositionController>();

                MonoBehaviour materialSource =
                    FindRequiredMonoBehaviour(
                        "PrototypeTraceWeaveCounterplayController");

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

                PrototypeLiveCompositionPainterCounterplayController controller =
                    GetOrAdd<
                        PrototypeLiveCompositionPainterCounterplayController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0f, -20f),
                        new Vector2(630f, 118f));

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
                        "CANLI KOMPOZİSYON • RESSAM KARŞI-OYUNU",
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
                        "Aktif Canlı Kompozisyon bekleniyor.",
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
                        "KÖPRÜYE İMLEÇ • F4 SULUBOYA • F10 YAĞ • F12 SİLGİ",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerCenter,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(0.5f, 0f),
                        new Vector2(12f, 6f),
                        new Vector2(-24f, -2f));

                controller.Configure(
                    liveComposition,
                    materialSource,
                    group,
                    title,
                    state,
                    controls);

                liveComposition.ConfigurePainterCounterplayHarness(
                    controller);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    liveComposition);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M53.2 Setup] Live Composition Painter Counterplay Risk Spike D3 ready.\n" +
                    $"LiveComposition={GetHierarchyPath(liveComposition.transform)}\n" +
                    $"MaterialSource={materialSource.GetType().Name} @ {GetHierarchyPath(materialSource.transform)}\n" +
                    $"Counterplay={GetHierarchyPath(controller.transform)}\n" +
                    "WatercolorBinding=F4 shared read-only\n" +
                    "OilBinding=F10 shared read-only\n" +
                    "EraserBinding=F12 shared read-only\n" +
                    "UsesExistingM51PainterMaterialActionsReadOnly=True\n" +
                    "AddsNewPainterGameplayInputAction=False\n" +
                    "WatercolorBendEnabled=True\n" +
                    "OilDelayedCollapseEnabled=True\n" +
                    "EraserSafeSeverEnabled=True\n" +
                    "AllowLocalPainterRoleSwitchBypass=True\n" +
                    "LocalRoleSwitchBypassIsPrototypeOnly=True\n" +
                    "NetworkAuthorityEnabled=False",
                    controller);

                EditorUtility.DisplayDialog(
                    "M53.2 Hazır",
                    "Canlı Kompozisyon için Suluboya bükme, Yağlı Boya gecikmeli " +
                    "çökertme ve Silgi güvenli bağlantı kesme karşı-oyunu kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M53.2 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.2 - Force Watercolor Bend (Play Mode)")]
        public static void ForceWatercolor()
        {
            FindPlayModeController()?
                .ForceWatercolorBendForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.2 - Force Oil Load (Play Mode)")]
        public static void ForceOil()
        {
            FindPlayModeController()?
                .ForceOilLoadForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.2 - Force Eraser Sever (Play Mode)")]
        public static void ForceEraser()
        {
            FindPlayModeController()?
                .ForceEraserSeverForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "53.2 - Diagnose Live Composition Painter Counterplay")]
        public static void Diagnose()
        {
            PrototypeLiveCompositionPainterCounterplayController counterplay =
                FindOptional<
                    PrototypeLiveCompositionPainterCounterplayController>();

            PrototypeLiveCompositionController live =
                FindOptional<
                    PrototypeLiveCompositionController>();

            PrototypeLiveCompositionBridge bridge =
                live != null
                    ? live.ActiveBridge
                    : null;

            PrototypeLiveCompositionConsentHarness consent =
                live != null
                    ? live.ConsentHarness
                    : null;

            string report =
                "[M53.2 Diagnose]\n" +
                $"Counterplay={(counterplay != null ? "OK" : "MISSING")}\n" +
                $"LiveComposition={(live != null ? "OK" : "MISSING")}\n" +
                $"PainterRoleActive={(counterplay != null && counterplay.PainterRoleActive)}\n" +
                $"SharedMaterialInputResolved={(counterplay != null && counterplay.SharedMaterialInputResolved)}\n" +
                $"SharedInputContract={(counterplay != null ? counterplay.SharedInputContract : "N/A")}\n" +
                $"PainterCameraResolved={(counterplay != null && counterplay.PainterCameraResolved)}\n" +
                $"BridgeTargetValid={(counterplay != null && counterplay.BridgeTargetValid)}\n" +
                $"Composing={(live != null && live.Composing)}\n" +
                $"BridgeActive={(bridge != null && bridge.ActiveBridge)}\n" +
                $"BridgeWatercolorBendCount={(bridge != null ? bridge.WatercolorBendCount : 0)}\n" +
                $"BridgeBodyPoint={(bridge != null ? bridge.BodyPoint.ToString("F3") : "N/A")}\n" +
                $"WatercolorSuccessCount={(counterplay != null ? counterplay.WatercolorSuccessCount : 0)}\n" +
                $"OilSuccessCount={(counterplay != null ? counterplay.OilSuccessCount : 0)}\n" +
                $"OilCollapseCount={(counterplay != null ? counterplay.OilCollapseCount : 0)}\n" +
                $"OilStressNormalized={(counterplay != null ? counterplay.OilStressNormalized : 0f):F2}\n" +
                $"EraserSuccessCount={(counterplay != null ? counterplay.EraserSuccessCount : 0)}\n" +
                $"RejectedCounterplayCount={(counterplay != null ? counterplay.RejectedCounterplayCount : 0)}\n" +
                $"MotorSuppressed={(live != null && live.MotorSuppressed)}\n" +
                $"ConsentState={(consent != null ? consent.State.ToString() : "N/A")}\n" +
                $"ConsentReleaseResetCount={(consent != null ? consent.ReleaseResetCount : 0)}\n" +
                $"LastAction={(counterplay != null ? counterplay.LastAction : "N/A")}\n" +
                "UsesExistingM51PainterMaterialActionsReadOnly=True\n" +
                "AddsNewPainterGameplayInputAction=False\n" +
                "WatercolorBendEnabled=True\n" +
                "OilDelayedCollapseEnabled=True\n" +
                "EraserSafeSeverEnabled=True\n" +
                "WatercolorRawDamageEnabled=False\n" +
                "OilRawDamageEnabled=False\n" +
                "EraserRawDamageEnabled=False\n" +
                "AllowLocalPainterRoleSwitchBypass=True\n" +
                "LocalRoleSwitchBypassIsPrototypeOnly=True\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                counterplay != null
                    ? counterplay
                    : live);

            EditorUtility.DisplayDialog(
                "M53.2 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeLiveCompositionPainterCounterplayController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M53.2",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeLiveCompositionPainterCounterplayController controller =
                FindOptional<
                    PrototypeLiveCompositionPainterCounterplayController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M53.2] Counterplay controller bulunamadı.");
            }

            return controller;
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
                    0.90f,
                    0.86f,
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
