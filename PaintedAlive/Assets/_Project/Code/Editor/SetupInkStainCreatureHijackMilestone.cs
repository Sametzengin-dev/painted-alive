using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint.Ink.Possession;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Paint.Ink.StainSabotage;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkStainCreatureHijackMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Ink/" +
            "M26_StainCreatureHijackConfig.asset";
        private const string HudPanelName =
            "M26_StainCreatureHijackPanel";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "26 - Setup Stain Creature Hijack")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M26 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                Prerequisites prerequisites =
                    ValidatePrerequisites();
                InkStainHijackConfig config =
                    GetOrCreateConfig();
                InkStainCreatureHijackController controller =
                    GetOrCreateController(
                        prerequisites,
                        config);
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
                    "[M26 Setup] Tamamlandı. Tam Leke hâlinde küçük " +
                    "yaratığı önce M25 ile sabote et; E'yi bırakıp aynı " +
                    "yaratığa yeniden E basılı tut. WASD/fare ile kısa " +
                    "süre kontrol et, E ile erken çık.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "26 - Diagnose Stain Creature Hijack")]
        public static void Diagnose()
        {
            InkStainCreatureHijackController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkStainCreatureHijackController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkStainCreatureHijackHud[] huds =
                UnityEngine.Object.FindObjectsByType<
                    InkStainCreatureHijackHud>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M26 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"HUDs={huds.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                InkStainCreatureHijackController controller =
                    controllers[i];
                Debug.Log(
                    "[M26 Diagnose Controller] " +
                    $"Path={GetHierarchyPath(controller.transform)}, " +
                    $"StainRole={controller.IsStainRoleActive}, " +
                    $"Hijacking={controller.IsHijacking}, " +
                    $"Target={GetName(controller.CurrentTarget)}, " +
                    $"Creature={GetName(controller.HijackedCreature)}, " +
                    $"EntryArmed={controller.EntryArmed}, " +
                    $"Entry={controller.EntryProgress:0.00}, " +
                    $"Remaining={controller.RemainingSeconds:0.00}, " +
                    $"Result={controller.LastResult}",
                    controller);
            }
        }

        private static Prerequisites ValidatePrerequisites()
        {
            InkStainSabotageController[] sabotageControllers =
                UnityEngine.Object.FindObjectsByType<
                    InkStainSabotageController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (sabotageControllers.Length != 1 ||
                authorities.Length != 1)
            {
                throw new InvalidOperationException(
                    "M26 çalışan tek M25 SabotageController ve tek " +
                    "RoleAuthority bekliyor. " +
                    $"M25={sabotageControllers.Length}, " +
                    $"Authorities={authorities.Length}.");
            }

            InkStainSabotageController sabotage =
                sabotageControllers[0];
            FigureMotor figure =
                sabotage.GetComponent<FigureMotor>();
            FigureClarityState clarity =
                figure != null
                    ? figure.GetComponent<FigureClarityState>()
                    : null;
            FigurePrimaryToolClarityGate clarityGate =
                figure != null
                    ? figure.GetComponent<
                        FigurePrimaryToolClarityGate>()
                    : null;
            Camera figureCamera =
                ResolveFigureCamera(authorities[0]);
            Canvas canvas =
                UnityEngine.Object.FindFirstObjectByType<Canvas>(
                    FindObjectsInactive.Include);

            if (figure == null ||
                clarity == null ||
                clarityGate == null ||
                figureCamera == null ||
                canvas == null)
            {
                throw new InvalidOperationException(
                    "M26 ön koşulları eksik. " +
                    $"Figure={(figure != null)}, " +
                    $"Clarity={(clarity != null)}, " +
                    $"M25.1Gate={(clarityGate != null)}, " +
                    $"FigureCamera={(figureCamera != null)}, " +
                    $"Canvas={(canvas != null)}.");
            }

            InkPossessionController painterPossession =
                figure.GetComponent<InkPossessionController>();
            return new Prerequisites(
                figure,
                clarity,
                figureCamera,
                authorities[0],
                sabotage,
                painterPossession,
                canvas);
        }

        private static Camera ResolveFigureCamera(
            InkPainterRoleAuthority authority)
        {
            if (authority == null)
            {
                return null;
            }

            SerializedObject serialized =
                new SerializedObject(authority);
            SerializedProperty property =
                serialized.FindProperty("figureCamera");
            return property != null
                ? property.objectReferenceValue as Camera
                : null;
        }

        private static InkStainHijackConfig GetOrCreateConfig()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Ink");
            InkStainHijackConfig config =
                AssetDatabase.LoadAssetAtPath<
                    InkStainHijackConfig>(ConfigPath);

            if (config != null)
            {
                return config;
            }

            config =
                ScriptableObject.CreateInstance<
                    InkStainHijackConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static InkStainCreatureHijackController
            GetOrCreateController(
                Prerequisites prerequisites,
                InkStainHijackConfig config)
        {
            InkStainCreatureHijackController[] existing =
                UnityEngine.Object.FindObjectsByType<
                    InkStainCreatureHijackController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (existing.Length > 1)
            {
                throw new InvalidOperationException(
                    "Sahnede birden fazla M26 HijackController var. " +
                    "Kopyaları temizleyip Setup'ı yeniden çalıştır.");
            }

            InkStainCreatureHijackController controller =
                existing.Length == 1
                    ? existing[0]
                    : Undo.AddComponent<
                        InkStainCreatureHijackController>(
                        prerequisites.Figure.gameObject);
            controller.Configure(
                prerequisites.Clarity,
                prerequisites.Figure,
                prerequisites.Camera,
                prerequisites.RoleAuthority,
                prerequisites.Sabotage,
                prerequisites.PainterPossession,
                config);
            return controller;
        }

        private static HudReferences GetOrCreateHud(
            Canvas canvas,
            InkStainCreatureHijackController controller)
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
                    "Create M26 Stain Creature Hijack HUD");
                panel.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect =
                (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 234f);
            rect.sizeDelta = new Vector2(430f, 66f);
            Image background = panel.GetComponent<Image>();
            background.color =
                new Color(0.08f, 0.015f, 0.095f, 0.91f);
            background.raycastTarget = false;

            Text text = GetOrCreateText(rect);
            Image progress = GetOrCreateProgress(rect);
            InkStainCreatureHijackHud hud =
                controller.GetComponent<
                    InkStainCreatureHijackHud>();

            if (hud == null)
            {
                hud =
                    Undo.AddComponent<
                        InkStainCreatureHijackHud>(
                        controller.gameObject);
            }

            hud.Configure(controller, panel, text, progress);
            panel.SetActive(false);
            return new HudReferences(hud, panel);
        }

        private static Text GetOrCreateText(
            RectTransform parent)
        {
            const string name = "HijackText";
            Transform existing = parent.Find(name);
            GameObject target =
                existing != null
                    ? existing.gameObject
                    : new GameObject(
                        name,
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Text));

            if (existing == null)
            {
                target.transform.SetParent(parent, false);
            }

            RectTransform rect =
                (RectTransform)target.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 11f);
            rect.offsetMax = new Vector2(-12f, -8f);
            Text text = target.GetComponent<Text>();
            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            text.fontSize = 13;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color =
                new Color(1f, 0.82f, 0.96f, 1f);
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
            const string name = "HijackProgress";
            Transform existing = parent.Find(name);
            GameObject target =
                existing != null
                    ? existing.gameObject
                    : new GameObject(
                        name,
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));

            if (existing == null)
            {
                target.transform.SetParent(parent, false);
            }

            RectTransform rect =
                (RectTransform)target.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-18f, 5f);
            Image image = target.GetComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 0f;
            image.raycastTarget = false;
            return image;
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

        private static string GetHierarchyPath(
            Transform target)
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

        private static string GetName(
            UnityEngine.Object target)
        {
            return target != null ? target.name : "None";
        }

        private readonly struct Prerequisites
        {
            public Prerequisites(
                FigureMotor figure,
                FigureClarityState clarity,
                Camera camera,
                InkPainterRoleAuthority roleAuthority,
                InkStainSabotageController sabotage,
                InkPossessionController painterPossession,
                Canvas canvas)
            {
                Figure = figure;
                Clarity = clarity;
                Camera = camera;
                RoleAuthority = roleAuthority;
                Sabotage = sabotage;
                PainterPossession = painterPossession;
                Canvas = canvas;
            }

            public FigureMotor Figure { get; }
            public FigureClarityState Clarity { get; }
            public Camera Camera { get; }
            public InkPainterRoleAuthority RoleAuthority { get; }
            public InkStainSabotageController Sabotage { get; }
            public InkPossessionController PainterPossession { get; }
            public Canvas Canvas { get; }
        }

        private readonly struct HudReferences
        {
            public HudReferences(
                InkStainCreatureHijackHud hud,
                GameObject panel)
            {
                Hud = hud;
                Panel = panel;
            }

            public InkStainCreatureHijackHud Hud { get; }
            public GameObject Panel { get; }
        }
    }
}
