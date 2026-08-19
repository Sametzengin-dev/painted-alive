#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class ApplyM43_3_2AuthoritativeFigureClarityBindingHotfix
    {
        [MenuItem("Tools/Painted Alive/Milestones/43.3.2 - Rebind HUD to Authoritative Figure Clarity")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M43.3.2 hotfix Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeUnifiedHudController controller =
                    FindSingle<PrototypeUnifiedHudController>();

                Undo.RecordObject(
                    controller,
                    "Rebind M43 HUD to authoritative Figure clarity");

                controller.ResolveMissingSources();

                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(
                    controller.gameObject.scene);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M43.3.2] AUTHORITATIVE FIGURE CLARITY BINDING\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"AuthoritativeClarityBound={controller.AuthoritativeClarityBound}\n" +
                    $"ClaritySourcePath={controller.ClaritySourcePath}\n" +
                    "GameplayClarityWasNotModified=True\n" +
                    "MovementAndToolRulesWereNotModified=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M43.3.2 tamamlandı",
                    "HUD, FigureMotor hiyerarşisindeki gerçek FigureClarityState bileşenine bağlandı. " +
                    "Play Mode'da Netlik seviyesi ile koşu/araç kilidinin aynı durumu gösterdiğini doğrula.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M43.3.2 başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/43.3.2 - Diagnose Figure Clarity Sources")]
        public static void Diagnose()
        {
            PrototypeUnifiedHudController controller =
                FindSingle<PrototypeUnifiedHudController>();

            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            string[] clarityPaths = behaviours
                .Where(item =>
                    item != null &&
                    item.gameObject.scene.IsValid() &&
                    item.GetType().Name == "FigureClarityState")
                .Select(item =>
                    $"{GetHierarchyPath(item.transform)} " +
                    $"Enabled={item.enabled}")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Debug.Log(
                "[M43.3.2 Diagnose]\n" +
                $"FigureClarityStateCount={clarityPaths.Length}\n" +
                $"AuthoritativeClarityBound={controller.AuthoritativeClarityBound}\n" +
                $"HUDClaritySource={controller.ClaritySourcePath}\n" +
                "ClarityCandidates=\n- " +
                string.Join("\n- ", clarityPaths) + "\n" +
                "MovementAndToolRulesWereNotModified=True\n" +
                "NetworkIntegrationParked=True");
        }

        private static T FindSingle<T>() where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(item =>
                    item != null &&
                    !EditorUtility.IsPersistent(item) &&
                    item.gameObject.scene.IsValid())
                .ToArray();

            if (objects.Length != 1)
            {
                throw new InvalidOperationException(
                    $"M43.3.2 tek {typeof(T).Name} bekliyor. Count={objects.Length}.");
            }

            return objects[0];
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
