using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Paint.Ink.Possession;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkPainterRoleCameraMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string DataFolder = RootFolder + "/Data/Paint/Ink";
        private const string ConfigPath =
            DataFolder + "/InkPainterRoleCameraConfig.asset";
        private const string PainterCameraName =
            "M21_InkPainterIndependentCamera";
        private const string CrosshairName = "M21_InkPainterCrosshair";

        [MenuItem(
            "Tools/Painted Alive/Milestones/21 - Setup Painter Role Camera")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M21 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                Prerequisites prerequisites = ValidatePrerequisites();
                EnsureFolder(DataFolder);
                InkPainterRoleCameraConfig config = GetOrCreateConfig();
                Camera painterCamera = GetOrCreatePainterCamera(
                    prerequisites.FigureCamera);
                InkPainterIndependentCamera cameraController =
                    GetOrAdd<InkPainterIndependentCamera>(
                        painterCamera.gameObject);
                cameraController.Configure(
                    painterCamera,
                    prerequisites.Figure,
                    config);

                SetObjectReference(
                    prerequisites.Possession,
                    "sourceCamera",
                    painterCamera);
                SetObjectReference(
                    prerequisites.NestController,
                    "painterCamera",
                    painterCamera);

                CrosshairReferences crosshair = GetOrCreateCrosshair(
                    prerequisites.PainterHud);
                InkPainterRoleAuthority authority =
                    GetOrAdd<InkPainterRoleAuthority>(
                        prerequisites.Possession.gameObject);
                authority.Configure(
                    prerequisites.Figure,
                    prerequisites.FigureCamera,
                    painterCamera,
                    cameraController,
                    prerequisites.Possession,
                    prerequisites.NestController,
                    prerequisites.PainterHud.gameObject,
                    crosshair.Root);
                crosshair.Crosshair.Configure(
                    authority,
                    prerequisites.NestController,
                    prerequisites.Possession,
                    crosshair.Segments,
                    crosshair.StateText);

                prerequisites.FigureCamera.enabled = true;
                SetAudioListenerEnabled(
                    prerequisites.FigureCamera,
                    true);
                painterCamera.enabled = false;
                SetAudioListenerEnabled(painterCamera, false);
                cameraController.enabled = false;
                prerequisites.Possession.enabled = false;
                prerequisites.NestController.enabled = false;
                prerequisites.PainterHud.gameObject.SetActive(false);
                crosshair.Root.SetActive(false);

                MarkDirty(
                    config,
                    painterCamera,
                    cameraController,
                    prerequisites.Possession,
                    prerequisites.NestController,
                    authority,
                    crosshair.Crosshair,
                    prerequisites.PainterHud.gameObject,
                    crosshair.Root);
                EditorSceneManager.MarkSceneDirty(
                    prerequisites.Possession.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M21 Setup] Painter role authority and independent " +
                    "camera are ready. F1=Ink Painter, F2=Figure, " +
                    "Painter WASD/QE=move, Mouse=look, Shift=boost, " +
                    "Alt=planning stance, R=reframe, F7=nest, " +
                    "F6=possess. Scene was not saved automatically; " +
                    "press Ctrl+S.",
                    authority);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/21 - Diagnose Painter Role Camera")]
        public static void Diagnose()
        {
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterIndependentCamera[] cameras =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterIndependentCamera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterCrosshair[] crosshairs =
                UnityEngine.Object.FindObjectsByType<InkPainterCrosshair>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPossessionController[] possessions =
                UnityEngine.Object.FindObjectsByType<
                    InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterNestController[] nests =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterNestController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleCameraConfig config =
                AssetDatabase.LoadAssetAtPath<
                    InkPainterRoleCameraConfig>(ConfigPath);

            Debug.Log(
                "[M21 Diagnose] " +
                $"Configs={(config != null ? 1 : 0)}, " +
                $"RoleAuthorities={authorities.Length}, " +
                $"PainterCameras={cameras.Length}, " +
                $"Crosshairs={crosshairs.Length}, " +
                $"Possessions={possessions.Length}, " +
                $"NestControllers={nests.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < authorities.Length; i++)
            {
                InkPainterRoleAuthority authority = authorities[i];
                Debug.Log(
                    "[M21 Diagnose Authority] " +
                    $"Path={GetHierarchyPath(authority.transform)}, " +
                    $"Role={authority.CurrentRole}, " +
                    $"IsInkPainter={authority.IsInkPainter}, " +
                    $"ActiveCamera={GetObjectName(authority.ActiveRoleCamera)}, " +
                    $"LastReason={authority.LastRoleReason}",
                    authority);
            }

            for (int i = 0; i < cameras.Length; i++)
            {
                InkPainterIndependentCamera cameraController = cameras[i];
                Camera targetCamera = cameraController.ControlledCamera;
                Debug.Log(
                    "[M21 Diagnose Camera] " +
                    $"Path={GetHierarchyPath(cameraController.transform)}, " +
                    $"Camera={GetObjectName(targetCamera)}, " +
                    $"CameraEnabled=" +
                    $"{(targetCamera != null && targetCamera.enabled)}, " +
                    $"ControllerEnabled={cameraController.enabled}, " +
                    $"Planning={cameraController.PlanningStance}, " +
                    $"BoundaryLimited={cameraController.BoundaryLimited}",
                    cameraController);
            }
        }

        private static Prerequisites ValidatePrerequisites()
        {
            InkPossessionController[] possessions =
                UnityEngine.Object.FindObjectsByType<
                    InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterNestController[] nests =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterNestController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterHud[] huds =
                UnityEngine.Object.FindObjectsByType<InkPainterHud>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (possessions.Length != 1 ||
                possessions[0].TargetFigure == null)
            {
                throw new InvalidOperationException(
                    "M21 tek bir M17 InkPossessionController ve atanmış " +
                    "FigureMotor bekliyor.");
            }

            if (nests.Length != 1 || huds.Length != 1)
            {
                throw new InvalidOperationException(
                    "M21 tek bir M20 InkPainterNestController ve " +
                    "InkPainterHud bekliyor. Önce M20 Setup'ı doğrula.");
            }

            InkPainterRoleAuthority existingAuthority =
                possessions[0].GetComponent<InkPainterRoleAuthority>();
            Camera figureCamera = ResolveFigureCamera(
                possessions[0],
                existingAuthority);

            if (figureCamera == null)
            {
                throw new InvalidOperationException(
                    "Figür kamerası bulunamadı. M17 Source Camera " +
                    "referansını kontrol et.");
            }

            return new Prerequisites(
                possessions[0].TargetFigure,
                figureCamera,
                possessions[0],
                nests[0],
                huds[0]);
        }

        private static Camera ResolveFigureCamera(
            InkPossessionController possession,
            InkPainterRoleAuthority existingAuthority)
        {
            if (existingAuthority != null)
            {
                SerializedObject authorityObject =
                    new SerializedObject(existingAuthority);
                Camera configuredFigureCamera =
                    authorityObject.FindProperty("figureCamera")
                        .objectReferenceValue as Camera;

                if (configuredFigureCamera != null)
                {
                    return configuredFigureCamera;
                }
            }

            Camera source = possession.SourceCamera;

            if (source != null &&
                source.name != PainterCameraName)
            {
                return source;
            }

            Camera[] childCameras =
                possession.TargetFigure.GetComponentsInChildren<Camera>(
                    true);

            for (int i = 0; i < childCameras.Length; i++)
            {
                Camera candidate = childCameras[i];

                if (candidate != null &&
                    candidate.name != PainterCameraName)
                {
                    return candidate;
                }
            }

            Camera[] sceneCameras =
                UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < sceneCameras.Length; i++)
            {
                Camera candidate = sceneCameras[i];

                if (candidate != null &&
                    candidate.name != PainterCameraName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static InkPainterRoleCameraConfig GetOrCreateConfig()
        {
            InkPainterRoleCameraConfig config =
                AssetDatabase.LoadAssetAtPath<
                    InkPainterRoleCameraConfig>(ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<
                    InkPainterRoleCameraConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            return config;
        }

        private static Camera GetOrCreatePainterCamera(
            Camera figureCamera)
        {
            GameObject existing = GameObject.Find(PainterCameraName);
            GameObject cameraObject;

            if (existing != null)
            {
                cameraObject = existing;
            }
            else
            {
                cameraObject = new GameObject(PainterCameraName);
                Undo.RegisterCreatedObjectUndo(
                    cameraObject,
                    "Create M21 Painter Camera");
            }

            Camera painterCamera = cameraObject.GetComponent<Camera>();

            if (painterCamera == null)
            {
                painterCamera = Undo.AddComponent<Camera>(cameraObject);
            }

            painterCamera.CopyFrom(figureCamera);
            painterCamera.name = PainterCameraName;
            painterCamera.transform.SetPositionAndRotation(
                figureCamera.transform.position,
                figureCamera.transform.rotation);
            painterCamera.targetTexture = null;
            painterCamera.enabled = false;

            if (figureCamera.GetComponent<AudioListener>() != null &&
                painterCamera.GetComponent<AudioListener>() == null)
            {
                AudioListener listener =
                    Undo.AddComponent<AudioListener>(cameraObject);
                listener.enabled = false;
            }

            return painterCamera;
        }

        private static CrosshairReferences GetOrCreateCrosshair(
            InkPainterHud painterHud)
        {
            Canvas canvas = painterHud.GetComponentInParent<Canvas>(true);

            if (canvas == null)
            {
                throw new InvalidOperationException(
                    "M20 InkPainterHud bir Canvas altında değil.");
            }

            Transform existing = canvas.transform.Find(CrosshairName);
            GameObject root;

            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = new GameObject(
                    CrosshairName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer));
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create M21 Painter Crosshair");
                root.transform.SetParent(canvas.transform, false);
            }

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image[] segments = new Image[4];
            segments[0] = GetOrCreateSegment(
                rootRect,
                "Top",
                new Vector2(0f, 14f),
                new Vector2(2f, 10f));
            segments[1] = GetOrCreateSegment(
                rootRect,
                "Bottom",
                new Vector2(0f, -14f),
                new Vector2(2f, 10f));
            segments[2] = GetOrCreateSegment(
                rootRect,
                "Left",
                new Vector2(-14f, 0f),
                new Vector2(10f, 2f));
            segments[3] = GetOrCreateSegment(
                rootRect,
                "Right",
                new Vector2(14f, 0f),
                new Vector2(10f, 2f));
            Text stateText = GetOrCreateStateText(rootRect);
            InkPainterCrosshair crosshair =
                GetOrAdd<InkPainterCrosshair>(root);
            return new CrosshairReferences(
                root,
                crosshair,
                segments,
                stateText);
        }

        private static Image GetOrCreateSegment(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject segmentObject = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            if (existing == null)
            {
                segmentObject.transform.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)segmentObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = segmentObject.GetComponent<Image>();
            image.color = new Color(0.9f, 0.82f, 1f, 0.95f);
            image.raycastTarget = false;
            return image;
        }

        private static Text GetOrCreateStateText(RectTransform parent)
        {
            const string name = "StateText";
            Transform existing = parent.Find(name);
            GameObject textObject = existing != null
                ? existing.gameObject
                : new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            if (existing == null)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -34f);
            rect.sizeDelta = new Vector2(420f, 24f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.text = "F7 YUVA  •  F6 SAHİPLEN";
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 0.82f, 1f, 0.95f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serializedObject =
                new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} bulunamadı.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAudioListenerEnabled(
            Camera targetCamera,
            bool active)
        {
            AudioListener listener = targetCamera != null
                ? targetCamera.GetComponent<AudioListener>()
                : null;

            if (listener != null)
            {
                listener.enabled = active;
            }
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(target);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void MarkDirty(params UnityEngine.Object[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    EditorUtility.SetDirty(targets[i]);
                }
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }

        private static string GetObjectName(
            UnityEngine.Object target)
        {
            return target != null ? target.name : "<null>";
        }

        private readonly struct Prerequisites
        {
            public Prerequisites(
                FigureMotor figure,
                Camera figureCamera,
                InkPossessionController possession,
                InkPainterNestController nestController,
                InkPainterHud painterHud)
            {
                Figure = figure;
                FigureCamera = figureCamera;
                Possession = possession;
                NestController = nestController;
                PainterHud = painterHud;
            }

            public FigureMotor Figure { get; }
            public Camera FigureCamera { get; }
            public InkPossessionController Possession { get; }
            public InkPainterNestController NestController { get; }
            public InkPainterHud PainterHud { get; }
        }

        private readonly struct CrosshairReferences
        {
            public CrosshairReferences(
                GameObject root,
                InkPainterCrosshair crosshair,
                Image[] segments,
                Text stateText)
            {
                Root = root;
                Crosshair = crosshair;
                Segments = segments;
                StateText = stateText;
            }

            public GameObject Root { get; }
            public InkPainterCrosshair Crosshair { get; }
            public Image[] Segments { get; }
            public Text StateText { get; }
        }
    }
}
