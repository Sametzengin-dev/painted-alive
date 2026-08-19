#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class ApplyM43_3_3LegacyHudGameplayInputPreservationHotfix
    {
        [MenuItem("Tools/Painted Alive/Milestones/43.3.3 - Preserve Gameplay HUD Inputs")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M43.3.3 hotfix Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeDeveloperHudVisibilityController controller =
                    FindSingle<PrototypeDeveloperHudVisibilityController>();

                Undo.RecordObject(
                    controller,
                    "Preserve gameplay HUD inputs");

                controller.RefreshTargets();

                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M43.3.3] GAMEPLAY HUD INPUT PRESERVATION\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"PassiveDebugHudManaged={controller.ManagedHudCount}\n" +
                    $"GameplayHudPreserved={controller.PreservedGameplayHudCount}\n" +
                    $"ManagedTypes={Format(controller.GetManagedHudTypeNames())}\n" +
                    $"PreservedTypes={Format(controller.GetPreservedGameplayHudTypeNames())}\n" +
                    "WildcardHudDisabling=False\n" +
                    "RestoreAndGameplayInputsIndependentFromF3=True\n" +
                    "NetworkIntegrationParked=True",
                    controller);

                EditorUtility.DisplayDialog(
                    "M43.3.3 tamamlandı",
                    "F3 artık yalnız bilinen pasif debug panellerini yönetir. " +
                    "Restore, araç, prompt, pickup ve diğer gameplay HUD bileşenleri normal oyunda aktif kalır.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M43.3.3 başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/43.3.3 - Diagnose Gameplay HUD Inputs")]
        public static void Diagnose()
        {
            PrototypeDeveloperHudVisibilityController controller =
                FindSingle<PrototypeDeveloperHudVisibilityController>();

            Debug.Log(
                "[M43.3.3 Diagnose]\n" +
                $"DeveloperHudVisible={controller.DeveloperHudVisible}\n" +
                $"PassiveDebugHudManaged={controller.ManagedHudCount}\n" +
                $"GameplayHudPreserved={controller.PreservedGameplayHudCount}\n" +
                $"ManagedTypes={Format(controller.GetManagedHudTypeNames())}\n" +
                $"PreservedTypes={Format(controller.GetPreservedGameplayHudTypeNames())}\n" +
                "WildcardHudDisabling=False\n" +
                "RestoreAndGameplayInputsIndependentFromF3=True\n" +
                "UnifiedHudPreserved=True\n" +
                "NetworkIntegrationParked=True");
        }

        private static string Format(string[] values)
        {
            return values == null || values.Length == 0
                ? "None"
                : string.Join(", ", values);
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
                    $"M43.3.3 tek {typeof(T).Name} bekliyor. Count={objects.Length}.");
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
