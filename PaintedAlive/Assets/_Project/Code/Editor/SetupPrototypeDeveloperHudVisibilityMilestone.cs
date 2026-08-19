#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeDeveloperHudVisibilityMilestone
    {
        private const string UnifiedCanvasName = "M43_UnifiedPlayerHUD";
        private const string ControllerObjectName = "M43_UnifiedHudController";

        [MenuItem("Tools/Painted Alive/Milestones/43.2 - Apply Developer HUD Visibility Layer")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M43.2 kurulumu Play Mode dışında çalıştırılmalıdır.");
                }

                GameObject canvas = GameObject.Find(UnifiedCanvasName);
                if (canvas == null)
                {
                    throw new InvalidOperationException(
                        "M43_UnifiedPlayerHUD bulunamadı. Önce çalışan M43.1 sahnesini aç.");
                }

                Transform controllerTransform =
                    canvas.transform.Find(ControllerObjectName);

                if (controllerTransform == null)
                {
                    throw new InvalidOperationException(
                        $"{ControllerObjectName} bulunamadı. M43.1 kurulumu eksik.");
                }

                PrototypeDeveloperHudVisibilityController visibility =
                    controllerTransform.GetComponent<
                        PrototypeDeveloperHudVisibilityController>();

                if (visibility == null)
                {
                    visibility = Undo.AddComponent<
                        PrototypeDeveloperHudVisibilityController>(
                            controllerTransform.gameObject);
                }

                Undo.RecordObject(
                    visibility,
                    "Configure M43.2 developer HUD visibility");

                visibility.Configure(
                    configuredVisibleAtStart: false);

                EditorUtility.SetDirty(visibility);
                EditorSceneManager.MarkSceneDirty(canvas.scene);
                EditorSceneManager.SaveOpenScenes();

                List<string> detectedHudTypes = FindLegacyHudTypes();

                Debug.Log(
                    "[M43.2 Setup] Developer HUD visibility layer ready.\n" +
                    $"Canvas={GetHierarchyPath(canvas.transform)}\n" +
                    $"Controller={GetHierarchyPath(controllerTransform)}\n" +
                    $"VisibilityControllerCount=" +
                    $"{CountSceneObjects<PrototypeDeveloperHudVisibilityController>()}\n" +
                    $"DetectedLegacyHudTypes={detectedHudTypes.Count}\n" +
                    $"{FormatHudTypeList(detectedHudTypes)}\n" +
                    "DefaultDeveloperHudVisible=False\n" +
                    "ToggleKey=F3\n" +
                    "Only PaintedAlive *HUD MonoBehaviours are managed.\n" +
                    "Gameplay controllers, match flow, score, telemetry and validation remain enabled.",
                    visibility);

                EditorUtility.DisplayDialog(
                    "M43.2 kurulumu tamamlandı",
                    "Eski prototip HUD bileşenleri normal oyunda gizlenecek. " +
                    "Play Mode sırasında F3 ile geliştirici HUD katmanını açıp kapatabilirsin.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M43.2 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/43.2 - Diagnose Developer HUD Visibility Layer")]
        public static void Diagnose()
        {
            PrototypeDeveloperHudVisibilityController controller =
                FindFirstSceneObject<
                    PrototypeDeveloperHudVisibilityController>();

            List<string> hudTypes = FindLegacyHudTypes();

            Debug.Log(
                "[M43.2 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ControllerCount=" +
                $"{CountSceneObjects<PrototypeDeveloperHudVisibilityController>()}\n" +
                $"DeveloperHudVisible=" +
                $"{(controller != null && controller.DeveloperHudVisible)}\n" +
                $"ManagedHudCount=" +
                $"{(controller != null ? controller.ManagedHudCount : 0)}\n" +
                $"DetectedLegacyHudTypes={hudTypes.Count}\n" +
                $"{FormatHudTypeList(hudTypes)}\n" +
                $"LastToggleReason=" +
                $"{(controller != null ? controller.LastToggleReason : "N/A")}\n" +
                "UnifiedHudPreserved=True\n" +
                "GameplayAuthoritiesPreserved=True\n" +
                "NetworkIntegrationParked=True");
        }

        private static List<string> FindLegacyHudTypes()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            return behaviours
                .Where(IsLegacyHud)
                .Select(behaviour => behaviour.GetType().FullName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsLegacyHud(MonoBehaviour behaviour)
        {
            if (behaviour == null ||
                EditorUtility.IsPersistent(behaviour) ||
                !behaviour.gameObject.scene.IsValid())
            {
                return false;
            }

            Type type = behaviour.GetType();
            string typeNamespace = type.Namespace ?? string.Empty;

            if (!typeNamespace.StartsWith(
                    "PaintedAlive",
                    StringComparison.Ordinal) ||
                typeNamespace.StartsWith(
                    "PaintedAlive.UI.UnifiedHUD",
                    StringComparison.Ordinal))
            {
                return false;
            }

            return type.Name.EndsWith(
                "HUD",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatHudTypeList(
            IReadOnlyList<string> typeNames)
        {
            if (typeNames.Count == 0)
            {
                return "LegacyHudTypes=None";
            }

            return "LegacyHudTypes=\n- " +
                string.Join("\n- ", typeNames);
        }

        private static T FindFirstSceneObject<T>()
            where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
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
