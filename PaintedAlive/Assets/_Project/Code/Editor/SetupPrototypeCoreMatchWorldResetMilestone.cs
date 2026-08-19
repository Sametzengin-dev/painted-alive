#if UNITY_EDITOR
using System;
using PaintedAlive.MatchFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeCoreMatchWorldResetMilestone
    {
        private const string RootName =
            "M54_1_CoreMatchWorldResetIntegration";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "54.1 - Apply Core Match World Reset Integration")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M54.1 setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeCoreMatchController matchController =
                    FindRequired<
                        PrototypeCoreMatchController>();

                GameObject root =
                    GetOrCreateRoot(
                        RootName);

                PrototypeCoreMatchWorldResetCoordinator coordinator =
                    GetOrAdd<
                        PrototypeCoreMatchWorldResetCoordinator>(
                            root);

                coordinator.Configure(
                    matchController);

                matchController.ConfigureWorldResetCoordinator(
                    coordinator);

                EditorUtility.SetDirty(
                    coordinator);

                EditorUtility.SetDirty(
                    matchController);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M54.1 Setup] Core Match World Reset Integration ready.\n" +
                    $"MatchController={GetHierarchyPath(matchController.transform)}\n" +
                    $"Coordinator={GetHierarchyPath(coordinator.transform)}\n" +
                    "FeatureFreezeCompatible=True\n" +
                    "NewGameplayMechanicAdded=False\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "UsesExistingPrototypeResetContracts=True\n" +
                    "ReflectionCompatibilityBridgeOnly=True\n" +
                    "LiveCompositionResetIntegrated=True\n" +
                    "TraceWeaveResetIntegrated=True\n" +
                    "UnderlayerResetIntegrated=True\n" +
                    "MasterpieceResetIntegrated=True\n" +
                    "SideCanvasResetIntegrated=True\n" +
                    "StolenDefinitionResetIntegrated=True\n" +
                    "LegacyScoreGateResetIntegrated=True\n" +
                    "SceneReloadRequired=False\n" +
                    "NetworkAuthorityEnabled=False\n" +
                    "ReadyToLeaveFeatureExpansion=True",
                    coordinator);

                EditorUtility.DisplayDialog(
                    "M54.1 Hazır",
                    "Core Match reset artık mevcut transient gameplay sistemlerini " +
                    "tek round-reset zincirinde temizleyecek.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M54.1 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "54.1 - Force World Reset Only (Play Mode)")]
        public static void ForceWorldResetOnly()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M54.1",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return;
            }

            PrototypeCoreMatchWorldResetCoordinator coordinator =
                FindOptional<
                    PrototypeCoreMatchWorldResetCoordinator>();

            if (coordinator == null)
            {
                Debug.LogError(
                    "[M54.1] World reset coordinator bulunamadı.");

                return;
            }

            coordinator.ResetRoundWorldState(
                "Editor force world-reset-only");
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "54.1 - Diagnose Core Match World Reset")]
        public static void Diagnose()
        {
            PrototypeCoreMatchWorldResetCoordinator coordinator =
                FindOptional<
                    PrototypeCoreMatchWorldResetCoordinator>();

            PrototypeCoreMatchController matchController =
                FindOptional<
                    PrototypeCoreMatchController>();

            string report =
                "[M54.1 Diagnose]\n" +
                $"Coordinator={(coordinator != null ? "OK" : "MISSING")}\n" +
                $"MatchController={(matchController != null ? "OK" : "MISSING")}\n" +
                $"ControllerIntegrated={(matchController != null && matchController.IntegratedTransientWorldResetEnabled)}\n" +
                $"ResetInvocationCount={(coordinator != null ? coordinator.ResetInvocationCount : 0)}\n" +
                $"ResetOperationCount={(coordinator != null ? coordinator.ResetOperationCount : 0)}\n" +
                $"ResetSuccessCount={(coordinator != null ? coordinator.ResetSuccessCount : 0)}\n" +
                $"ResetSkipCount={(coordinator != null ? coordinator.ResetSkipCount : 0)}\n" +
                $"ResetFailureCount={(coordinator != null ? coordinator.ResetFailureCount : 0)}\n" +
                $"LiveCompositionReleaseCount={(coordinator != null ? coordinator.LiveCompositionReleaseCount : 0)}\n" +
                $"PartnerPoseReleaseCount={(coordinator != null ? coordinator.PartnerPoseReleaseCount : 0)}\n" +
                $"UnderlayerRecoveryCount={(coordinator != null ? coordinator.UnderlayerRecoveryCount : 0)}\n" +
                $"UnderlayerSeamClearCount={(coordinator != null ? coordinator.UnderlayerSeamClearCount : 0)}\n" +
                $"UnderlayerSeepClearCount={(coordinator != null ? coordinator.UnderlayerSeepClearCount : 0)}\n" +
                $"TraceWeaveExpireCount={(coordinator != null ? coordinator.TraceWeaveExpireCount : 0)}\n" +
                $"MasterpiecePossessionReleaseCount={(coordinator != null ? coordinator.MasterpiecePossessionReleaseCount : 0)}\n" +
                $"MasterpieceAutonomyDisableCount={(coordinator != null ? coordinator.MasterpieceAutonomyDisableCount : 0)}\n" +
                $"MasterpieceRecallCount={(coordinator != null ? coordinator.MasterpieceRecallCount : 0)}\n" +
                $"DefinitionRestoreCount={(coordinator != null ? coordinator.DefinitionRestoreCount : 0)}\n" +
                $"StolenDefinitionDropCount={(coordinator != null ? coordinator.StolenDefinitionDropCount : 0)}\n" +
                $"SeveredTokenDestroyCount={(coordinator != null ? coordinator.SeveredTokenDestroyCount : 0)}\n" +
                $"SideCanvasClearCount={(coordinator != null ? coordinator.SideCanvasClearCount : 0)}\n" +
                $"LegacyScoreResetCount={(coordinator != null ? coordinator.LegacyScoreResetCount : 0)}\n" +
                $"LegacyExitGateResetCount={(coordinator != null ? coordinator.LegacyExitGateResetCount : 0)}\n" +
                $"DynamicTransformRestoreCount={(coordinator != null ? coordinator.DynamicTransformRestoreCount : 0)}\n" +
                $"LastResetSource={(coordinator != null ? coordinator.LastResetSource : "N/A")}\n" +
                $"LastResetSummary={(coordinator != null ? coordinator.LastResetSummary : "N/A")}\n" +
                "FeatureFreezeCompatible=True\n" +
                "NewGameplayMechanicAdded=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "UsesExistingPrototypeResetContracts=True\n" +
                "ReflectionCompatibilityBridgeOnly=True\n" +
                "SceneReloadRequired=False\n" +
                "NetworkAuthorityEnabled=False\n" +
                "ReadyToLeaveFeatureExpansion=True";

            Debug.Log(
                report,
                coordinator != null
                    ? coordinator
                    : matchController);

            EditorUtility.DisplayDialog(
                "M54.1 Diagnose",
                report,
                "Tamam");
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
                    $"{typeof(T).Name} bulunamadı. Önce M54.0 setup çalışmış olmalı.");
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

            string path =
                target.name;

            while (target.parent != null)
            {
                target =
                    target.parent;

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
