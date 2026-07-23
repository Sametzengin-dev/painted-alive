using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.StainSabotage;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkStainSabotageMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Ink/M25_InkStainSabotageConfig.asset";
        private const string HudPanelName =
            "M25_InkStainSabotagePanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/25 - Setup Ink Stain Sabotage")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M25 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                Prerequisites prerequisites =
                    ValidatePrerequisites();
                InkStainSabotageConfig config =
                    GetOrCreateConfig();
                InkStainSabotageController controller =
                    GetOrCreateController(prerequisites, config);
                HudReferences hud =
                    GetOrCreateHud(
                        prerequisites.Canvas,
                        controller);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(hud.Hud);
                EditorUtility.SetDirty(hud.Panel);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.Figure.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M25 Setup] Tamamlandı. Leke hâlindeki Figür, " +
                    "küçük Mürekkep yaratığına bakıp E basılı tutarak " +
                    "sinyalini geçici bozar. Emir ve sahiplenme kopar; " +
                    "yüksek karmaşıklıklı Kabuklu bağışıktır.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/25 - Diagnose Ink Stain Sabotage")]
        public static void Diagnose()
        {
            InkStainSabotageController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkStainSabotageController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkStainSabotageHud[] huds =
                UnityEngine.Object.FindObjectsByType<
                    InkStainSabotageHud>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkStainSabotageStatus[] statuses =
                UnityEngine.Object.FindObjectsByType<
                    InkStainSabotageStatus>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M25 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"HUDs={huds.Length}, " +
                $"Statuses={statuses.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                InkStainSabotageController controller =
                    controllers[i];
                Debug.Log(
                    "[M25 Diagnose Controller] " +
                    $"Path={GetHierarchyPath(controller.transform)}, " +
                    $"StainRoleActive={controller.IsStainRoleActive}, " +
                    $"Progress={controller.HoldProgress:0.00}, " +
                    $"Successes={controller.SuccessfulSabotages}, " +
                    $"Result={controller.LastResult}",
                    controller);
            }

            for (int i = 0; i < statuses.Length; i++)
            {
                InkStainSabotageStatus status = statuses[i];
                Debug.Log(
                    "[M25 Diagnose Status] " +
                    $"Path={GetHierarchyPath(status.transform)}, " +
                    $"Active={status.IsSabotaged}, " +
                    $"Remaining={status.RemainingSeconds:0.00}, " +
                    $"Result={status.LastReason}",
                    status);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/25 - Debug Set Figure To Stain")]
        public static void DebugSetFigureToStain()
        {
            try
            {
                if (!Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "Bu debug komutu yalnız Play Mode'da çalışır.");
                }

                FigureClarityState clarity =
                    FindSingleClarityState();
                clarity.ApplyPaintExposure(
                    clarity.MaximumClarity + 100f,
                    FigurePaintRegion.Torso);
                Debug.Log(
                    $"[M25 Debug] Figure level={clarity.CurrentLevel}.",
                    clarity);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/25 - Debug Restore Figure")]
        public static void DebugRestoreFigure()
        {
            try
            {
                if (!Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "Bu debug komutu yalnız Play Mode'da çalışır.");
                }

                FigureClarityState clarity =
                    FindSingleClarityState();
                clarity.ResetToFull();
                Debug.Log(
                    $"[M25 Debug] Figure level={clarity.CurrentLevel}.",
                    clarity);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static Prerequisites ValidatePrerequisites()
        {
            FigureMotor[] figures =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (figures.Length != 1 || authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M25 tek FigureMotor ve tek RoleAuthority bekliyor. " +
                    $"Figures={figures.Length}, " +
                    $"Authorities={authorities.Length}.");
            }

            FigureClarityState clarity =
                figures[0].GetComponent<FigureClarityState>();
            SerializedObject authorityObject =
    new SerializedObject(authorities[0]);

SerializedProperty figureCameraProperty =
    authorityObject.FindProperty("figureCamera");

Camera camera = figureCameraProperty != null
    ? figureCameraProperty.objectReferenceValue as Camera
    : null;
            Canvas canvas =
                UnityEngine.Object.FindFirstObjectByType<Canvas>(
                    FindObjectsInactive.Include);

            if (clarity == null || camera == null || canvas == null)
            {
                throw new InvalidOperationException(
                    "M25 Figure üzerinde FigureClarityState, çocuk Camera " +
                    "ve sahnede Canvas bekliyor. " +
                    $"Clarity={(clarity != null)}, " +
                    $"Camera={(camera != null)}, " +
                    $"Canvas={(canvas != null)}.");
            }

            return new Prerequisites(
                figures[0],
                clarity,
                camera,
                authorities[0],
                canvas);
        }

        private static InkStainSabotageConfig GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Ink");
            InkStainSabotageConfig config =
                AssetDatabase.LoadAssetAtPath<
                    InkStainSabotageConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    InkStainSabotageConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static InkStainSabotageController
            GetOrCreateController(
                Prerequisites prerequisites,
                InkStainSabotageConfig config)
        {
            InkStainSabotageController[] existing =
                UnityEngine.Object.FindObjectsByType<
                    InkStainSabotageController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (existing.Length > 1)
            {
                throw new InvalidOperationException(
                    "Sahnede birden fazla M25 SabotageController var. " +
                    "Kopyaları temizleyip Setup'ı yeniden çalıştır.");
            }

            InkStainSabotageController controller =
                existing.Length == 1
                    ? existing[0]
                    : Undo.AddComponent<InkStainSabotageController>(
                        prerequisites.Figure.gameObject);
            controller.Configure(
                prerequisites.Clarity,
                prerequisites.Figure,
                prerequisites.Camera,
                prerequisites.RoleAuthority,
                config);
            return controller;
        }

        private static HudReferences GetOrCreateHud(
            Canvas canvas,
            InkStainSabotageController controller)
        {
            Transform existing =
                canvas.transform.Find(HudPanelName);
            GameObject panel;

            if (existing != null)
            {
                panel = existing.gameObject;
            }
            else
            {
                panel = new GameObject(
                    HudPanelName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(
                    panel,
                    "Create M25 Ink Stain Sabotage HUD");
                panel.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect =
                (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 158f);
            rect.sizeDelta = new Vector2(430f, 68f);
            Image background = panel.GetComponent<Image>();
            background.color =
                new Color(0.015f, 0.095f, 0.08f, 0.9f);
            background.raycastTarget = false;

            Text text = GetOrCreateText(rect);
            Image progress = GetOrCreateProgress(rect);
            InkStainSabotageHud hud =
                controller.GetComponent<InkStainSabotageHud>();

            if (hud == null)
            {
                hud = Undo.AddComponent<InkStainSabotageHud>(
                    controller.gameObject);
            }

            hud.Configure(controller, panel, text, progress);
            panel.SetActive(false);
            return new HudReferences(hud, panel);
        }

        private static Text GetOrCreateText(RectTransform parent)
        {
            const string name = "SabotageText";
            Transform existing = parent.Find(name);
            GameObject target;

            if (existing != null)
            {
                target = existing.gameObject;
            }
            else
            {
                target = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                target.transform.SetParent(parent, false);
            }

            RectTransform rect =
                (RectTransform)target.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 12f);
            rect.offsetMax = new Vector2(-12f, -8f);
            Text text = target.GetComponent<Text>();
            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            text.fontSize = 13;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color =
                new Color(0.78f, 1f, 0.94f, 1f);
            text.raycastTarget = false;
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Overflow;
            return text;
        }

        private static Image GetOrCreateProgress(
            RectTransform parent)
        {
            const string name = "SabotageProgress";
            Transform existing = parent.Find(name);
            GameObject target;

            if (existing != null)
            {
                target = existing.gameObject;
            }
            else
            {
                target = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                target.transform.SetParent(parent, false);
            }

            RectTransform rect =
                (RectTransform)target.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(-18f, 5f);
            Image image = target.GetComponent<Image>();
            image.color =
                new Color(0.12f, 0.95f, 0.75f, 0.95f);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 0f;
            image.raycastTarget = false;
            return image;
        }

        private static FigureClarityState FindSingleClarityState()
        {
            FigureClarityState[] states =
                UnityEngine.Object.FindObjectsByType<
                    FigureClarityState>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            if (states.Length != 1)
            {
                throw new InvalidOperationException(
                    "Debug komutu tek aktif FigureClarityState bekliyor. " +
                    $"Count={states.Length}.");
            }

            return states[0];
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');

            if (slash <= 0)
            {
                return;
            }

            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "null";
            }

            string path = target.name;
            Transform current = target.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private readonly struct Prerequisites
        {
            public Prerequisites(
                FigureMotor figure,
                FigureClarityState clarity,
                Camera camera,
                InkPainterRoleAuthority roleAuthority,
                Canvas canvas)
            {
                Figure = figure;
                Clarity = clarity;
                Camera = camera;
                RoleAuthority = roleAuthority;
                Canvas = canvas;
            }

            public FigureMotor Figure { get; }
            public FigureClarityState Clarity { get; }
            public Camera Camera { get; }
            public InkPainterRoleAuthority RoleAuthority { get; }
            public Canvas Canvas { get; }
        }

        private readonly struct HudReferences
        {
            public HudReferences(
                InkStainSabotageHud hud,
                GameObject panel)
            {
                Hud = hud;
                Panel = panel;
            }

            public InkStainSabotageHud Hud { get; }
            public GameObject Panel { get; }
        }
    }
}
