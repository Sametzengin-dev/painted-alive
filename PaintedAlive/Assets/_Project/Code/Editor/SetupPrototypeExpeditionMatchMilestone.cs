#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PaintedAlive.Core.Prototypes;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    /// <summary>
    /// M38.1 migration. The original M38 mistakenly created a second match
    /// authority. This setup removes that scene instance and extends the
    /// existing PrototypeMatchController instead.
    /// </summary>
    public static class SetupPrototypeExpeditionMatchMilestone
    {
        private const string ObsoleteRootName = "M38_PrototypeExpeditionMatch";

        [MenuItem("Tools/Painted Alive/Milestones/38 - Setup Prototype Expedition Match Flow")]
        public static void RedirectOldSetupMenu()
        {
            Migrate();
        }

        [MenuItem("Tools/Painted Alive/Milestones/38.1 - Migrate to Existing Prototype Match Flow")]
        public static void Migrate()
        {
            try
            {
                PrototypeMatchController matchController =
                    FindRequiredSceneObject<PrototypeMatchController>(
                        "mevcut PrototypeMatchController");

                PrototypeJourneyScoreTracker tracker =
                    FindRequiredSceneObject<PrototypeJourneyScoreTracker>(
                        "M37 PrototypeJourneyScoreTracker");

                FigureClarityState figure = tracker.Figure != null
                    ? tracker.Figure
                    : FindRequiredSceneObject<FigureClarityState>(
                        "FigureClarityState");

                PrototypeFrameExitGate exitGate =
                    FindRequiredSceneObject<PrototypeFrameExitGate>(
                        "M36 PrototypeFrameExitGate");

                int removedRoots = RemoveObsoleteM38Roots();
                int removedComponents = RemoveStrayObsoleteM38Components();
                int disabledLegacyFinishTriggers = DisableLegacyFinishTriggers();

                PrototypeMatchExpeditionBridge bridge =
                    GetOrAddComponent<PrototypeMatchExpeditionBridge>(
                        matchController.gameObject);
                bridge.Configure(matchController, tracker, figure, exitGate);

                PrototypeMatchExpeditionResultHUD hud =
                    GetOrAddComponent<PrototypeMatchExpeditionResultHUD>(
                        matchController.gameObject);
                hud.Configure(bridge);

                EditorUtility.SetDirty(bridge);
                EditorUtility.SetDirty(hud);
                EditorUtility.SetDirty(tracker);
                EditorUtility.SetDirty(exitGate);
                EditorUtility.SetDirty(matchController.gameObject);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[M38.1 Migration] Tamamlandı.\n" +
                    $"AuthoritativeMatch={GetHierarchyPath(matchController.transform)}\n" +
                    $"Bridge={GetHierarchyPath(bridge.transform)}\n" +
                    $"M37Tracker={GetHierarchyPath(tracker.transform)}\n" +
                    $"M36Gate={GetHierarchyPath(exitGate.transform)}\n" +
                    $"RemovedObsoleteRoots={removedRoots}\n" +
                    $"RemovedStrayM38Components={removedComponents}\n" +
                    $"DisabledLegacyFinishTriggers={disabledLegacyFinishTriggers}\n" +
                    "Countdown, timer, input locking, reset and ENTER restart are now owned only by PrototypeMatchController.",
                    bridge);

                EditorUtility.DisplayDialog(
                    "PAINTED ALIVE — M38.1",
                    "M38 mevcut asıl 1v1 maç döngüsüne taşındı.\n\n" +
                    "Tek maç otoritesi: PrototypeMatchController\n" +
                    "M36: normal/Leke çıkış kararı\n" +
                    "M37: güncel yolculuk skoru\n" +
                    "M38.1: yalnız bağlama ve sonuç özeti\n\n" +
                    $"Kaldırılan eski M38 root: {removedRoots}\n" +
                    $"Kapatılan eski FinishTrigger: {disabledLegacyFinishTriggers}\n\n" +
                    "Sahneyi Ctrl+S ile kaydet.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M38.1 migration başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/38.1 - Diagnose Existing Prototype Match Integration")]
        public static void Diagnose()
        {
            PrototypeMatchController match = FindFirstSceneObject<PrototypeMatchController>();
            PrototypeMatchExpeditionBridge bridge = FindFirstSceneObject<PrototypeMatchExpeditionBridge>();
            PrototypeMatchExpeditionResultHUD hud = FindFirstSceneObject<PrototypeMatchExpeditionResultHUD>();
            PrototypeJourneyScoreTracker tracker = FindFirstSceneObject<PrototypeJourneyScoreTracker>();
            PrototypeFrameExitGate gate = FindFirstSceneObject<PrototypeFrameExitGate>();

            int obsoleteRoots = 0;
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null &&
                    all[i].scene.IsValid() &&
                    all[i].name == ObsoleteRootName)
                {
                    obsoleteRoots++;
                }
            }

            int legacyFinishEnabled = 0;
            PrototypeFinishTrigger[] oldTriggers =
                UnityEngine.Object.FindObjectsByType<PrototypeFinishTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < oldTriggers.Length; i++)
            {
                if (oldTriggers[i] != null && oldTriggers[i].enabled)
                {
                    legacyFinishEnabled++;
                }
            }

            PrototypeExpeditionResultSnapshot snapshot = bridge != null
                ? bridge.CurrentSnapshot
                : default;

            Debug.Log(
                "[M38.1 Diagnose]\n" +
                $"PrototypeMatchController={(match != null ? GetHierarchyPath(match.transform) : "MISSING")}\n" +
                $"Bridge={(bridge != null ? "OK" : "MISSING")}\n" +
                $"ResultHUD={(hud != null ? "OK" : "MISSING")}\n" +
                $"M37Tracker={(tracker != null ? "OK" : "MISSING")}\n" +
                $"M36Gate={(gate != null ? "OK" : "MISSING")}\n" +
                $"ObsoleteM38Roots={obsoleteRoots}\n" +
                $"EnabledLegacyFinishTriggers={legacyFinishEnabled}\n" +
                $"State={(match != null ? match.State.ToString() : "Unknown")}\n" +
                $"TimeRemaining={(match != null ? match.TimeRemaining : 0f):0.00}\n" +
                $"Score={snapshot.Score.TotalScore}\n" +
                $"StainArrival={snapshot.StainArrivalDuringRun}\n" +
                $"NormalExitForwarded={(bridge != null && bridge.NormalExitForwarded)}\n" +
                $"NormalExitForwardCount={(bridge != null ? bridge.NormalExitForwardCount : 0)}\n" +
                $"RunNumber={(bridge != null ? bridge.RunNumber : 0)}\n" +
                $"ResetCount={(bridge != null ? bridge.ResetCount : 0)}");
        }

        private static int RemoveObsoleteM38Roots()
        {
            int removed = 0;
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = all.Length - 1; i >= 0; i--)
            {
                GameObject candidate = all[i];
                if (candidate == null ||
                    !candidate.scene.IsValid() ||
                    candidate.name != ObsoleteRootName)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(candidate);
                removed++;
            }

            return removed;
        }

        private static int RemoveStrayObsoleteM38Components()
        {
            var obsoleteNames = new HashSet<string>
            {
                "PaintedAlive.Core.MatchFlow.PrototypeExpeditionMatchFlow",
                "PaintedAlive.Core.MatchFlow.PrototypeMatchInputLock",
                "PaintedAlive.Core.MatchFlow.PrototypeExpeditionMatchHUD"
            };

            int removed = 0;
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = behaviours.Length - 1; i >= 0; i--)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    !behaviour.gameObject.scene.IsValid() ||
                    !obsoleteNames.Contains(behaviour.GetType().FullName))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(behaviour);
                removed++;
            }

            return removed;
        }

        private static int DisableLegacyFinishTriggers()
        {
            int disabled = 0;
            PrototypeFinishTrigger[] triggers =
                UnityEngine.Object.FindObjectsByType<PrototypeFinishTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < triggers.Length; i++)
            {
                PrototypeFinishTrigger trigger = triggers[i];
                if (trigger == null || !trigger.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (trigger.enabled)
                {
                    Undo.RecordObject(trigger, "Disable legacy prototype finish trigger");
                    trigger.enabled = false;
                    EditorUtility.SetDirty(trigger);
                    disabled++;
                }
            }

            return disabled;
        }

        private static T FindRequiredSceneObject<T>(string label)
            where T : UnityEngine.Object
        {
            T found = FindFirstSceneObject<T>();
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"M38.1 {label} bulamadı. Mevcut ana sahneyi aç ve " +
                    "M36–M37'nin kurulu olduğundan emin ol.");
            }

            return found;
        }

        private static T FindFirstSceneObject<T>() where T : UnityEngine.Object
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < objects.Length; i++)
            {
                T candidate = objects[i];
                if (candidate == null || EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                if (candidate is Component component && component.gameObject.scene.IsValid())
                {
                    return candidate;
                }

                if (candidate is GameObject gameObject && gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return Undo.AddComponent<T>(target);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "None";
            }

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
#endif
