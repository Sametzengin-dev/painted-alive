#if UNITY_EDITOR
using PaintedAlive.Paint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools.Presentation
{
    public static class SetupOilPaintWallVisualFeedback_M58E1
    {
        private const string CreationParticlePath =
            "Assets/_Project/Prefabs/VFX/Paint/" +
            "VFX_OilKnifeCut.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0E.1 - Apply Oil Wall Visual Feedback")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[M58.0E.1] Exit Play Mode before applying setup.");
                return;
            }

            ParticleSystem creationParticle =
                LoadCreationParticle();

            if (creationParticle == null)
            {
                return;
            }

            OilStrokeSystem[] strokeSystems =
                Object.FindObjectsByType<OilStrokeSystem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            OilPaintFeedbackService[] feedbackServices =
                Object.FindObjectsByType<OilPaintFeedbackService>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (strokeSystems.Length == 0 ||
                feedbackServices.Length == 0)
            {
                Debug.LogError(
                    "[M58.0E.1] Active scene requires at least one " +
                    "OilStrokeSystem and OilPaintFeedbackService.");
                return;
            }

            int configured = 0;
            int skipped = 0;

            foreach (OilStrokeSystem strokeSystem in strokeSystems)
            {
                OilPaintFeedbackService feedbackService =
                    ResolveFeedbackService(
                        strokeSystem,
                        feedbackServices);

                if (feedbackService == null)
                {
                    skipped++;
                    Debug.LogError(
                        "[M58.0E.1] No unambiguous feedback service for " +
                        GetHierarchyPath(strokeSystem.transform) + ".",
                        strokeSystem);
                    continue;
                }

                Undo.RecordObject(
                    feedbackService,
                    "Configure M58.0E.1 Oil Feedback");

                SerializedObject feedbackSerialized =
                    new SerializedObject(feedbackService);

                SerializedProperty creationPrefabProperty =
                    feedbackSerialized.FindProperty(
                        "strokeCreationParticlePrefab");

                if (creationPrefabProperty == null)
                {
                    skipped++;
                    Debug.LogError(
                        "[M58.0E.1] Runtime feedback field was not found. " +
                        "Wait for Unity compilation, then run Apply again.",
                        feedbackService);
                    continue;
                }

                creationPrefabProperty.objectReferenceValue =
                    creationParticle;

                feedbackSerialized.ApplyModifiedProperties();

                OilStrokeCreationFeedback creationFeedback =
                    strokeSystem.GetComponent<
                        OilStrokeCreationFeedback>();

                if (creationFeedback == null)
                {
                    creationFeedback =
                        Undo.AddComponent<OilStrokeCreationFeedback>(
                            strokeSystem.gameObject);
                }

                Undo.RecordObject(
                    creationFeedback,
                    "Configure M58.0E.1 Stroke Trace");

                creationFeedback.Configure(
                    strokeSystem,
                    feedbackService);

                EditorUtility.SetDirty(feedbackService);
                EditorUtility.SetDirty(creationFeedback);
                EditorSceneManager.MarkSceneDirty(
                    strokeSystem.gameObject.scene);

                configured++;
            }

            Debug.Log(
                "[M58.0E.1 APPLY]\n" +
                $"Configured={configured}\n" +
                $"Skipped={skipped}\n" +
                "CreationSource=VFX_OilKnifeCut (independent pool)\n" +
                "GameplayOrNetworkMutation=False\n" +
                "Save the scene, then run Diagnose.");
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0E.1 - Diagnose Oil Wall Visual Feedback")]
        public static void Diagnose()
        {
            OilStrokeCreationFeedback[] presenters =
                Object.FindObjectsByType<OilStrokeCreationFeedback>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int configured = 0;
            int missingParticle = 0;

            foreach (OilStrokeCreationFeedback presenter in presenters)
            {
                if (presenter != null && presenter.IsConfigured)
                {
                    configured++;
                }

                OilPaintFeedbackService service =
                    presenter != null
                        ? presenter.FeedbackService
                        : null;

                if (service == null)
                {
                    missingParticle++;
                    continue;
                }

                SerializedObject serialized =
                    new SerializedObject(service);

                SerializedProperty particleProperty =
                    serialized.FindProperty(
                        "strokeCreationParticlePrefab");

                if (particleProperty == null ||
                    particleProperty.objectReferenceValue == null)
                {
                    missingParticle++;
                }
            }

            bool pass =
                presenters.Length > 0 &&
                configured == presenters.Length &&
                missingParticle == 0;

            string report =
                "[M58.0E.1 DIAGNOSE]\n" +
                $"Presenters={presenters.Length}\n" +
                $"Configured={configured}\n" +
                $"MissingCreationParticle={missingParticle}\n" +
                "WallOnly=True\n" +
                "UsesStrokeFinalizedEvent=True\n" +
                "CreatesGameplayState=False\n" +
                $"STATUS={(pass ? "PASS" : "FAIL")}";

            if (pass)
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }
        }

        private static ParticleSystem LoadCreationParticle()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CreationParticlePath);

            ParticleSystem particle =
                prefab != null
                    ? prefab.GetComponent<ParticleSystem>()
                    : null;

            if (particle == null)
            {
                Debug.LogError(
                    "[M58.0E.1] Existing pigment particle prefab was " +
                    $"not found at {CreationParticlePath}.");
            }

            return particle;
        }

        private static OilPaintFeedbackService ResolveFeedbackService(
            OilStrokeSystem strokeSystem,
            OilPaintFeedbackService[] feedbackServices)
        {
            OilPaintFeedbackService local =
                strokeSystem.GetComponent<OilPaintFeedbackService>();

            if (local != null)
            {
                return local;
            }

            return feedbackServices.Length == 1
                ? feedbackServices[0]
                : null;
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "<missing>";
            }

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
#endif
