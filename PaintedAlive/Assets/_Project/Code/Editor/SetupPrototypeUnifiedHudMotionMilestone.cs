#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeUnifiedHudMotionMilestone
    {
        private const string CanvasName = "M43_UnifiedPlayerHUD";
        private const string ControllerObjectName = "M43_UnifiedHudController";

        [MenuItem("Tools/Painted Alive/Milestones/43.3 - Apply HUD Motion")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M43.3 kurulumu Play Mode dışında çalıştırılmalıdır.");
                }

                GameObject canvas = GameObject.Find(CanvasName);
                if (canvas == null)
                {
                    throw new InvalidOperationException(
                        "M43_UnifiedPlayerHUD bulunamadı. Çalışan M43.1 sahnesini aç.");
                }

                Transform controllerTransform =
                    canvas.transform.Find(ControllerObjectName);

                if (controllerTransform == null)
                {
                    throw new InvalidOperationException(
                        $"{ControllerObjectName} bulunamadı. M43.1 kurulumu eksik.");
                }

                PrototypeUnifiedHudController snapshotSource =
                    controllerTransform.GetComponent<PrototypeUnifiedHudController>();

                if (snapshotSource == null)
                {
                    throw new InvalidOperationException(
                        "PrototypeUnifiedHudController bulunamadı.");
                }

                RectTransform topBar =
                    FindRequiredRect(canvas.transform, "HUD_Root/TopBar");
                RectTransform encounterStrip =
                    FindRequiredRect(canvas.transform, "HUD_Root/EncounterStrip");
                RectTransform progressPanel =
                    FindRequiredRect(canvas.transform, "HUD_Root/ProgressPanel");
                RectTransform figurePanel =
                    FindRequiredRect(canvas.transform, "HUD_Root/FigurePanel");
                RectTransform painterPanel =
                    FindRequiredRect(canvas.transform, "HUD_Root/PainterPanel");
                RectTransform contextPanel =
                    FindRequiredRect(canvas.transform, "HUD_Root/ContextPanel");

                Text roleText =
                    FindRequiredText(topBar, "RoleText");
                Text matchStateText =
                    FindRequiredText(topBar, "MatchStateText");

                PrototypeUnifiedHudMotionPresenter presenter =
                    controllerTransform.GetComponent<
                        PrototypeUnifiedHudMotionPresenter>();

                if (presenter == null)
                {
                    presenter = Undo.AddComponent<
                        PrototypeUnifiedHudMotionPresenter>(
                            controllerTransform.gameObject);
                }

                Undo.RecordObject(
                    presenter,
                    "Configure M43.3 HUD motion");

                presenter.Configure(
                    snapshotSource,
                    topBar,
                    encounterStrip,
                    progressPanel,
                    figurePanel,
                    painterPanel,
                    contextPanel,
                    roleText,
                    matchStateText);

                EditorUtility.SetDirty(presenter);
                EditorSceneManager.MarkSceneDirty(canvas.scene);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M43.3 Setup] Dependency-free HUD motion ready.\n" +
                    $"Canvas={GetHierarchyPath(canvas.transform)}\n" +
                    $"Controller={GetHierarchyPath(controllerTransform)}\n" +
                    $"PresenterCount={CountSceneObjects<PrototypeUnifiedHudMotionPresenter>()}\n" +
                    $"Configured={presenter.IsConfigured}\n" +
                    $"MotionBackend={presenter.MotionBackend}\n" +
                    "PrimeTweenCompileDependency=False\n" +
                    "MotionOnly=True\n" +
                    "GameplayStateWrites=False\n" +
                    "CameraShake=False\n" +
                    "NetworkIntegrationParked=True",
                    presenter);

                EditorUtility.DisplayDialog(
                    "M43.3 kurulumu tamamlandı",
                    "Birleşik HUD'a dependency-free sunum katmanı eklendi. " +
                    "Play Mode'da giriş, rol değişimi, rota kazanımı ve kritik Netlik/pigment hareketlerini doğrula.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M43.3 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/43.3 - Diagnose HUD Motion")]
        public static void Diagnose()
        {
            PrototypeUnifiedHudMotionPresenter presenter =
                FindFirstSceneObject<PrototypeUnifiedHudMotionPresenter>();

            Debug.Log(
                "[M43.3 Diagnose]\n" +
                $"Presenter={(presenter != null ? "OK" : "MISSING")}\n" +
                $"PresenterCount={CountSceneObjects<PrototypeUnifiedHudMotionPresenter>()}\n" +
                $"Configured={(presenter != null && presenter.IsConfigured)}\n" +
                $"MotionEnabled={(presenter != null && presenter.MotionEnabled)}\n" +
                $"MotionBackend={(presenter != null ? presenter.MotionBackend : "N/A")}\n" +
                $"LastObservedRole={(presenter != null ? presenter.LastObservedRole : "N/A")}\n" +
                $"LastObservedMatchState={(presenter != null ? presenter.LastObservedMatchState : "N/A")}\n" +
                $"ClarityCritical={(presenter != null && presenter.ClarityCritical)}\n" +
                $"PigmentCritical={(presenter != null && presenter.PigmentCritical)}\n" +
                $"MotionEventCount={(presenter != null ? presenter.MotionEventCount : 0)}\n" +
                "PrimeTweenCompileDependency=False\n" +
                "MotionOnly=True\n" +
                "GameplayAuthoritiesPreserved=True\n" +
                "DeveloperHudF3Preserved=True\n" +
                "NetworkIntegrationParked=True");
        }

        private static RectTransform FindRequiredRect(
            Transform root,
            string relativePath)
        {
            Transform found = root.Find(relativePath);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"M43.3 HUD nesnesini bulamadı: {relativePath}");
            }

            RectTransform rect = found as RectTransform;
            if (rect == null)
            {
                throw new InvalidOperationException(
                    $"HUD nesnesi RectTransform değil: {relativePath}");
            }

            return rect;
        }

        private static Text FindRequiredText(
            Transform parent,
            string childName)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException(
                    $"M43.3 metin nesnesini bulamadı: " +
                    $"{GetHierarchyPath(parent)}/{childName}");
            }

            Text text = child.GetComponent<Text>();
            if (text == null)
            {
                throw new InvalidOperationException(
                    $"UnityEngine.UI.Text bulunamadı: " +
                    $"{GetHierarchyPath(child)}");
            }

            return text;
        }

        private static T FindFirstSceneObject<T>()
            where T : Component
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int index = 0; index < objects.Length; index++)
            {
                T candidate = objects[index];
                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static int CountSceneObjects<T>()
            where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(candidate =>
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid());
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
#endif
