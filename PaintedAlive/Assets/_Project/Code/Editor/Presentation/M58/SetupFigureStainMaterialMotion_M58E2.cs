#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using PaintedAlive.Figures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools.Presentation
{
    public static class SetupFigureStainMaterialMotion_M58E2
    {
        private const string TransitionParticlePath =
            "Assets/_Project/Prefabs/VFX/Paint/" +
            "VFX_OilKnifeCut.prefab";

        private const string VisualRootName =
            "CharacterVisualRoot";

        private const string ProductionVisualName =
            "Figure_ProductionVisual";

        private const string StainVisualName =
            "M28_PlayerStainVisual";

        private const string AnchorName =
            "M58_FigureMaterialMotionAnchor";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0E.2 - Apply Figure Stain Material Motion")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[M58.0E.2] Exit Play Mode before applying setup.");
                return;
            }

            ParticleSystem transitionParticle =
                LoadTransitionParticle();

            if (transitionParticle == null)
            {
                return;
            }

            FigureMotor[] motors =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (motors.Length == 0)
            {
                Debug.LogError(
                    "[M58.0E.2] No FigureMotor was found in the " +
                    "active scene.");
                return;
            }

            int configured = 0;
            int skipped = 0;

            foreach (FigureMotor motor in motors)
            {
                if (!TryResolve(
                        motor,
                        out FigureClarityState clarity,
                        out Renderer[] productionRenderers,
                        out Renderer stainRenderer,
                        out string failure))
                {
                    skipped++;
                    Debug.LogError(
                        "[M58.0E.2] " + failure + " | " +
                        GetHierarchyPath(motor.transform),
                        motor);
                    continue;
                }

                Transform anchor = GetOrCreateAnchor(motor.transform);
                FigureClarityMaterialMotionFeedback feedback =
                    motor.GetComponent<
                        FigureClarityMaterialMotionFeedback>();

                if (feedback == null)
                {
                    feedback = Undo.AddComponent<
                        FigureClarityMaterialMotionFeedback>(
                        motor.gameObject);
                }

                Undo.RecordObject(
                    feedback,
                    "Configure M58.0E.2 Figure Feedback");

                feedback.Configure(
                    clarity,
                    productionRenderers,
                    stainRenderer,
                    anchor,
                    transitionParticle);

                EditorUtility.SetDirty(feedback);
                EditorUtility.SetDirty(anchor);
                EditorSceneManager.MarkSceneDirty(
                    motor.gameObject.scene);
                configured++;
            }

            Debug.Log(
                "[M58.0E.2 APPLY]\n" +
                $"Configured={configured}\n" +
                $"Skipped={skipped}\n" +
                "ProductionMesh=SkinnedMeshRenderer only\n" +
                "SharedMaterialsMutated=False\n" +
                "GameplayOrNetworkMutation=False\n" +
                "Save the scene, then run Diagnose.");
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0E.2 - Diagnose Figure Stain Material Motion")]
        public static void Diagnose()
        {
            FigureClarityMaterialMotionFeedback[] feedbacks =
                UnityEngine.Object.FindObjectsByType<
                    FigureClarityMaterialMotionFeedback>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            List<string> lines = new();
            bool pass = feedbacks.Length > 0;

            foreach (
                FigureClarityMaterialMotionFeedback feedback
                in feedbacks)
            {
                bool hasMotor =
                    feedback.GetComponent<FigureMotor>() != null;
                bool configured = feedback.IsConfigured;
                bool rendererCountOk =
                    feedback.ProductionRendererCount > 0;

                pass &= hasMotor && configured && rendererCountOk;

                lines.Add(
                    $"{(configured && hasMotor ? "PASS" : "FAIL")}" +
                    $" | {GetHierarchyPath(feedback.transform)}" +
                    $" | Renderers={feedback.ProductionRendererCount}" +
                    $" | Stain={feedback.HasStainRenderer}" +
                    $" | Transition={feedback.HasTransitionParticle}" +
                    $" | FigureMotor={hasMotor}");
            }

            FigureMotor[] motors =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int missingFeedback = motors.Count(
                motor => motor.GetComponent<
                    FigureClarityMaterialMotionFeedback>() == null);

            pass &= missingFeedback == 0;

            string report =
                "[M58.0E.2 DIAGNOSE]\n" +
                $"FigureMotors={motors.Length}\n" +
                $"FeedbackComponents={feedbacks.Length}\n" +
                $"MissingFeedback={missingFeedback}\n" +
                "MaterialPropertyBlock=True\n" +
                "SharedMaterialMutation=False\n" +
                "ColliderMutation=False\n" +
                "AuthorityMutation=False\n" +
                string.Join("\n", lines) + "\n" +
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

        private static bool TryResolve(
            FigureMotor motor,
            out FigureClarityState clarity,
            out Renderer[] productionRenderers,
            out Renderer stainRenderer,
            out string failure)
        {
            clarity = motor != null
                ? motor.GetComponent<FigureClarityState>()
                : null;
            productionRenderers = Array.Empty<Renderer>();
            stainRenderer = null;
            failure = string.Empty;

            if (motor == null || clarity == null)
            {
                failure =
                    "FigureMotor root requires FigureClarityState.";
                return false;
            }

            Transform visualRoot =
                motor.transform.Find(VisualRootName);
            Transform productionVisual = visualRoot != null
                ? visualRoot.Find(ProductionVisualName)
                : null;

            if (productionVisual == null)
            {
                failure =
                    "M55.1.1 production Figure visual was not found.";
                return false;
            }

            productionRenderers = productionVisual
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer != null)
                .Cast<Renderer>()
                .ToArray();

            if (productionRenderers.Length == 0)
            {
                failure =
                    "Production visual has no SkinnedMeshRenderer.";
                return false;
            }

            Transform stainVisual =
                motor.transform.Find(StainVisualName);
            stainRenderer = stainVisual != null
                ? stainVisual.GetComponent<Renderer>()
                : null;

            if (stainRenderer == null)
            {
                failure =
                    "M28 player Stain visual was not found.";
                return false;
            }

            return true;
        }

        private static Transform GetOrCreateAnchor(
            Transform figureRoot)
        {
            Transform anchor = figureRoot.Find(AnchorName);

            if (anchor != null)
            {
                return anchor;
            }

            GameObject anchorObject = new GameObject(AnchorName);
            Undo.RegisterCreatedObjectUndo(
                anchorObject,
                "Create M58.0E.2 Transition Anchor");

            anchor = anchorObject.transform;
            anchor.SetParent(figureRoot, false);
            anchor.localPosition = new Vector3(0f, 1.05f, 0f);
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static ParticleSystem LoadTransitionParticle()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    TransitionParticlePath);

            ParticleSystem particle = prefab != null
                ? prefab.GetComponent<ParticleSystem>()
                : null;

            if (particle == null)
            {
                Debug.LogError(
                    "[M58.0E.2] Existing pigment particle prefab " +
                    $"was not found at {TransitionParticlePath}.");
            }

            return particle;
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
