#if UNITY_EDITOR
using System;
using System.Reflection;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeUnderlayerFrameBraceMilestone
    {
        private const string RootName =
            "M52_2_UnderlayerFrameBraceRiskSpikeC3";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M52_2_UnderlayerFrameBracePanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.2 - Apply Underlayer Frame Brace Risk Spike C3")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M52.2 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeUnderlayerDiveController dive =
                    FindRequired<
                        PrototypeUnderlayerDiveController>();

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                MonoBehaviour figureToolSource =
                    FindRelatedToolSource(
                        figureMotor.transform);

                if (figureToolSource == null)
                {
                    throw new InvalidOperationException(
                        "Figure tool/loadout kaynağı bulunamadı.");
                }

                MonoBehaviour sharedInputSource =
                    FindRelatedUseToolInputSource(
                        figureMotor.transform);

                if (sharedInputSource == null)
                {
                    throw new InvalidOperationException(
                        "Figure hierarchy içinde mevcut UseTool InputActionReference sahibi bulunamadı.");
                }

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

                PrototypeUnderlayerFrameBraceController controller =
                    GetOrAdd<
                        PrototypeUnderlayerFrameBraceController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(20f, -20f),
                        new Vector2(500f, 118f));

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
                        "ALT KATMAN • ÇERÇEVE ANKRAJI",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.72f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -7f),
                        new Vector2(-24f, -3f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Çerçeve Tabancası bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.24f),
                        new Vector2(1f, 0.75f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "ÇERÇEVE TABANCASI • DİKİŞE YAKLAŞ • MEVCUT E",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 6f),
                        new Vector2(-24f, -2f));

                controller.Configure(
                    dive,
                    figureMotor.transform,
                    figureToolSource,
                    sharedInputSource,
                    clarityState,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    controller);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M52.2 Setup] Underlayer Frame Brace Risk Spike C3 ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"ToolSource={figureToolSource.GetType().Name} @ {GetHierarchyPath(figureToolSource.transform)}\n" +
                    $"UseToolInputSource={sharedInputSource.GetType().Name} @ {GetHierarchyPath(sharedInputSource.transform)}\n" +
                    $"ClarityState={(clarityState != null ? clarityState.GetType().Name : "OPTIONAL")}\n" +
                    "BraceSeconds=5.25\n" +
                    "UsesExistingFigureUseToolActionReadOnly=True\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "BraceTemporarilyOverridesOilSeal=True\n" +
                    "BraceDeletesOilSealTimer=False\n" +
                    "OilPaintCannotSealActiveBrace=True\n" +
                    "FullStainCanUseBracedSealedSeam=True\n" +
                    "NetworkAuthorityEnabled=False",
                    controller);

                EditorUtility.DisplayDialog(
                    "M52.2 Hazır",
                    "Çerçeve Tabancası mevcut UseTool action'ıyla dikişi kısa süre " +
                    "açık tutabilir; aktif ankraj Yağlı Boya mühürlemesini geçici " +
                    "olarak karşılar.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M52.2 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.2 - Force Brace Nearest Seam (Play Mode)")]
        public static void ForceBrace()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M52.2",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return;
            }

            PrototypeUnderlayerFrameBraceController controller =
                FindOptional<
                    PrototypeUnderlayerFrameBraceController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M52.2] Controller bulunamadı.");

                return;
            }

            controller.ForceBraceNearestForPrototype();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.2 - Diagnose Underlayer Frame Brace")]
        public static void Diagnose()
        {
            PrototypeUnderlayerFrameBraceController controller =
                FindOptional<
                    PrototypeUnderlayerFrameBraceController>();

            PrototypeUnderlayerDiveController dive =
                FindOptional<
                    PrototypeUnderlayerDiveController>();

            PrototypeUnderlayerSeam seamA =
                dive != null
                    ? dive.SeamA
                    : null;

            PrototypeUnderlayerSeam seamB =
                dive != null
                    ? dive.SeamB
                    : null;

            string report =
                "[M52.2 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"CurrentTool={(controller != null ? controller.CurrentTool : "N/A")}\n" +
                $"FrameGunSelected={(controller != null && controller.FrameGunSelected)}\n" +
                $"UseToolInputResolved={(controller != null && controller.UseToolInputResolved)}\n" +
                $"UseToolInputContract={(controller != null ? controller.UseToolInputContract : "N/A")}\n" +
                $"PrimaryToolAllowed={(controller != null && controller.PrimaryToolAllowed)}\n" +
                $"NearestSeam={(controller != null ? controller.NearestSeam : "N/A")}\n" +
                $"NearestSeamDistance={(controller != null ? controller.NearestSeamDistance : -1f):F2}\n" +
                $"SeamInRange={(controller != null && controller.SeamInRange)}\n" +
                $"BraceAttemptCount={(controller != null ? controller.BraceAttemptCount : 0)}\n" +
                $"BraceSuccessCount={(controller != null ? controller.BraceSuccessCount : 0)}\n" +
                $"RejectedBraceCount={(controller != null ? controller.RejectedBraceCount : 0)}\n" +
                $"SeamABraced={(seamA != null && seamA.IsBraced)}\n" +
                $"SeamABraceRemaining={(seamA != null ? seamA.BraceRemainingSeconds : 0f):F2}\n" +
                $"SeamASealed={(seamA != null && seamA.IsSealed)}\n" +
                $"SeamABlocksTraversal={(seamA != null && seamA.BlocksTraversal)}\n" +
                $"SeamBBraced={(seamB != null && seamB.IsBraced)}\n" +
                $"SeamBBraceRemaining={(seamB != null ? seamB.BraceRemainingSeconds : 0f):F2}\n" +
                $"SeamBSealed={(seamB != null && seamB.IsSealed)}\n" +
                $"SeamBBlocksTraversal={(seamB != null && seamB.BlocksTraversal)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "UsesExistingFigureUseToolActionReadOnly=True\n" +
                "AddsNewGameplayInputAction=False\n" +
                "BraceTemporarilyOverridesOilSeal=True\n" +
                "BraceDeletesOilSealTimer=False\n" +
                "OilPaintCannotSealActiveBrace=True\n" +
                "FullStainCanUseBracedSealedSeam=True\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                controller != null
                    ? controller
                    : dive);

            EditorUtility.DisplayDialog(
                "M52.2 Diagnose",
                report,
                "Tamam");
        }

        private static MonoBehaviour FindRelatedToolSource(
            Transform root)
        {
            MonoBehaviour[] related =
                root.GetComponentsInChildren<
                    MonoBehaviour>(
                        true);

            MonoBehaviour best = null;
            int bestScore = int.MinValue;

            foreach (MonoBehaviour candidate in related)
            {
                if (candidate == null)
                {
                    continue;
                }

                Type type =
                    candidate.GetType();

                int score = 0;
                string name = type.Name;

                if (
                    name.IndexOf(
                        "FigureToolLoadout",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 500;
                }

                if (
                    name.IndexOf(
                        "ToolLoadout",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 350;
                }

                if (
                    name.IndexOf(
                        "FigureTool",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 250;
                }

                if (
                    HasReadableToolMember(
                        type)
                )
                {
                    score += 200;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            if (
                best != null &&
                bestScore > 0
            )
            {
                return best;
            }

            return null;
        }

        private static bool HasReadableToolMember(
            Type type)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (string name in new[]
            {
                "CurrentTool",
                "ActiveTool",
                "SelectedTool",
                "currentTool",
                "activeTool",
                "selectedTool"
            })
            {
                if (
                    type.GetProperty(
                        name,
                        flags) != null ||
                    type.GetField(
                        name,
                        flags) != null
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static MonoBehaviour
            FindRelatedUseToolInputSource(
                Transform root)
        {
            MonoBehaviour[] related =
                root.GetComponentsInChildren<
                    MonoBehaviour>(
                        true);

            MonoBehaviour best = null;
            int bestScore = int.MinValue;

            foreach (MonoBehaviour candidate in related)
            {
                if (candidate == null)
                {
                    continue;
                }

                Type type =
                    candidate.GetType();

                if (
                    !HasUseToolActionReference(
                        candidate,
                        out string memberName)
                )
                {
                    continue;
                }

                int score = 100;

                if (
                    type.Name.IndexOf(
                        "PaletteKnife",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 300;
                }

                if (
                    type.Name.IndexOf(
                        "Tool",
                        StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    score += 100;
                }

                if (
                    string.Equals(
                        memberName,
                        "useToolAction",
                        StringComparison.Ordinal)
                )
                {
                    score += 150;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static bool HasUseToolActionReference(
            MonoBehaviour candidate,
            out string resolvedMember)
        {
            resolvedMember = null;

            Type type =
                candidate.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (string name in new[]
            {
                "useToolAction",
                "UseToolAction",
                "useAction",
                "UseAction",
                "primaryAction",
                "PrimaryAction",
                "fireAction",
                "FireAction"
            })
            {
                FieldInfo field =
                    type.GetField(
                        name,
                        flags);

                if (
                    field == null ||
                    !typeof(InputActionReference)
                        .IsAssignableFrom(
                            field.FieldType)
                )
                {
                    continue;
                }

                InputActionReference reference = null;

                try
                {
                    reference =
                        field.GetValue(candidate)
                        as InputActionReference;
                }
                catch (Exception)
                {
                    reference = null;
                }

                if (
                    reference != null &&
                    reference.action != null
                )
                {
                    resolvedMember = name;
                    return true;
                }
            }

            return false;
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
                    0.050f,
                    0.045f,
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
                    0.56f,
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
