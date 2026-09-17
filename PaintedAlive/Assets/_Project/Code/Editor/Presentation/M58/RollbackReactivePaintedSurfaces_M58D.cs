#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools.Presentation
{
    /// <summary>
    /// M58.0D rollback utility.
    /// Removes only the M58.0D reactive-painted-surfaces update and leaves
    /// M58.0A / M58.0B / M58.0C intact.
    ///
    /// Important: M58.0D did not persist a per-renderer material backup.
    /// For renderers that were changed and belong to prefab/model instances,
    /// this tool restores the material array from the original source renderer.
    /// Any renderer that cannot be restored exactly is reported and left alone
    /// rather than guessed.
    /// </summary>
    public static class RollbackReactivePaintedSurfaces_M58D
    {
        private const string RootName = "M58D_ReactivePaintedSurfaces";
        private const string MaterialPrefix = "MAT_M58D_";

        private static readonly string[] MaterialAssets =
        {
            "Assets/_Project/Art/Materials/Presentation/M58/MAT_M58D_CanvasClean.mat",
            "Assets/_Project/Art/Materials/Presentation/M58/MAT_M58D_PaperClean.mat",
            "Assets/_Project/Art/Materials/Presentation/M58/MAT_M58D_CanvasAged.mat",
            "Assets/_Project/Art/Materials/Presentation/M58/MAT_M58D_PainterStrokeBlue.mat",
            "Assets/_Project/Art/Materials/Presentation/M58/MAT_M58D_WatercolorSplashA.mat",
            "Assets/_Project/Art/Materials/Presentation/M58/MAT_M58D_WatercolorSplashB.mat",
        };

        private static readonly string[] TextureAssets =
        {
            "Assets/_Project/Art/Textures/Presentation/M58/Generated/natural_canvas_cream.png",
            "Assets/_Project/Art/Textures/Presentation/M58/Generated/textured_cream_paper.png",
            "Assets/_Project/Art/Textures/Presentation/M58/Generated/aged_stained_canvas.png",
            "Assets/_Project/Art/Textures/Presentation/M58/Generated/blue_brush_stroke_alpha.png",
            "Assets/_Project/Art/Textures/Presentation/M58/Generated/watercolor_splash_a_alpha.png",
            "Assets/_Project/Art/Textures/Presentation/M58/Generated/watercolor_splash_b_alpha.png",
        };

        private static readonly string[] RuntimeAndSetupScripts =
        {
            "Assets/_Project/Code/Runtime/Presentation/M58/M58ReactivePaintRuntimeBinder.cs",
            "Assets/_Project/Code/Runtime/Presentation/M58/M58WaterblobVisualPulse.cs",
            "Assets/_Project/Code/Editor/Presentation/M58/SetupReactivePaintedSurfaces_M58D.cs",
        };

        [MenuItem("Tools/Painted Alive/Milestones/58.0D - ROLLBACK Reactive Painted Surfaces")]
        public static void Rollback()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[M58.0D ROLLBACK] No valid loaded scene.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Rollback M58.0D",
                    "This removes only M58.0D Reactive Painted Surfaces.\n\n" +
                    "M58.0A Visual Foundation, M58.0B lighting and M58.0C are not intentionally removed.\n\n" +
                    "Continue?",
                    "Rollback M58.0D",
                    "Cancel"))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rollback M58.0D Reactive Painted Surfaces");

            int renderersWithM58D = 0;
            int renderersRestored = 0;
            int slotsRestored = 0;
            int unresolvedRenderers = 0;
            int componentsRemoved = 0;
            int rootObjectsRemoved = 0;
            var unresolved = new List<string>();

            try
            {
                // 1) Restore scene renderer material arrays from their original
                // prefab/model source wherever Unity can provide an exact source.
                Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || renderer.gameObject.scene != scene)
                        continue;

                    if (!UsesM58DMaterial(renderer))
                        continue;

                    renderersWithM58D++;

                    Renderer sourceRenderer =
                        PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer) as Renderer;

                    if (sourceRenderer == null)
                    {
                        sourceRenderer =
                            PrefabUtility.GetCorrespondingObjectFromSource(renderer) as Renderer;
                    }

                    if (sourceRenderer != null &&
                        sourceRenderer.sharedMaterials != null &&
                        sourceRenderer.sharedMaterials.Length > 0)
                    {
                        Material[] sourceMaterials = sourceRenderer.sharedMaterials;
                        Material[] restored = new Material[sourceMaterials.Length];
                        Array.Copy(sourceMaterials, restored, sourceMaterials.Length);

                        int previousSlots = renderer.sharedMaterials != null
                            ? renderer.sharedMaterials.Length
                            : 0;

                        Undo.RecordObject(renderer, "Restore pre-M58D source materials");
                        renderer.sharedMaterials = restored;
                        EditorUtility.SetDirty(renderer);

                        renderersRestored++;
                        slotsRestored += Mathf.Max(previousSlots, restored.Length);
                    }
                    else
                    {
                        unresolvedRenderers++;
                        unresolved.Add(GetHierarchyPath(renderer.transform));
                    }
                }

                // 2) Remove runtime M58.0D components before their scripts/assets
                // are deleted, so no Missing Script components remain.
                MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour == null || behaviour.gameObject.scene != scene)
                        continue;

                    Type type = behaviour.GetType();
                    string fullName = type.FullName ?? type.Name;

                    if (fullName == "PaintedAlive.Presentation.M58.M58ReactivePaintRuntimeBinder" ||
                        fullName == "PaintedAlive.Presentation.M58.M58WaterblobVisualPulse")
                    {
                        Undo.DestroyObjectImmediate(behaviour);
                        componentsRemoved++;
                    }
                }

                // 3) Remove the dedicated scene root if it exists.
                GameObject root = GameObject.Find(RootName);
                if (root != null && root.scene == scene)
                {
                    Undo.DestroyObjectImmediate(root);
                    rootObjectsRemoved++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                // 4) Delete assets introduced only by M58.0D.
                int assetsDeleted = 0;
                foreach (string path in MaterialAssets)
                    if (AssetDatabase.DeleteAsset(path)) assetsDeleted++;

                foreach (string path in TextureAssets)
                    if (AssetDatabase.DeleteAsset(path)) assetsDeleted++;

                // Delete M58.0D runtime/setup scripts last. This rollback tool is
                // intentionally left in the project so the operation remains auditable.
                foreach (string path in RuntimeAndSetupScripts)
                    if (AssetDatabase.DeleteAsset(path)) assetsDeleted++;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string unresolvedText = unresolved.Count == 0
                    ? "None"
                    : string.Join("\n - ", unresolved);

                Debug.Log(
                    "[M58.0D ROLLBACK] COMPLETE\n" +
                    $"Scene={scene.name}\n" +
                    $"RenderersWithM58D={renderersWithM58D}\n" +
                    $"RenderersRestoredFromSource={renderersRestored}\n" +
                    $"ApproxSlotsRestored={slotsRestored}\n" +
                    $"UnresolvedRenderers={unresolvedRenderers}\n" +
                    $"RuntimeComponentsRemoved={componentsRemoved}\n" +
                    $"RootObjectsRemoved={rootObjectsRemoved}\n" +
                    $"M58DAssetsDeleted={assetsDeleted}\n" +
                    "M58A_B_C_Preserved=True\n" +
                    "Unresolved:\n - " + unresolvedText);

                if (unresolvedRenderers > 0)
                {
                    EditorUtility.DisplayDialog(
                        "M58.0D rollback completed with unresolved renderers",
                        $"Rollback completed, but {unresolvedRenderers} renderer(s) had M58.0D materials and no prefab/model source from which an exact pre-M58D material array could be recovered.\n\n" +
                        "They were intentionally left untouched instead of guessing. Check the Console for their hierarchy paths.",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "M58.0D rollback complete",
                        "M58.0D scene/runtime assets were removed and M58.0A/B/C were left intact.\n\nThe active scene was saved automatically.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[M58.0D ROLLBACK] FAILED\n" + ex);
                EditorUtility.DisplayDialog(
                    "M58.0D rollback failed",
                    "The rollback stopped because of an exception. Check Console before saving or making more changes.",
                    "OK");
                throw;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/58.0D - Diagnose Rollback State")]
        public static void DiagnoseRollback()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[M58.0D ROLLBACK Diagnose] No valid loaded scene.");
                return;
            }

            int rendererSlots = 0;
            int components = 0;
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene != scene) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null &&
                        material.name.StartsWith(MaterialPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        rendererSlots++;
                    }
                }
            }

            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour.gameObject.scene != scene) continue;
                string fullName = behaviour.GetType().FullName ?? behaviour.GetType().Name;
                if (fullName == "PaintedAlive.Presentation.M58.M58ReactivePaintRuntimeBinder" ||
                    fullName == "PaintedAlive.Presentation.M58.M58WaterblobVisualPulse")
                {
                    components++;
                }
            }

            GameObject root = GameObject.Find(RootName);
            bool rootPresent = root != null && root.scene == scene;

            int materialAssets = 0;
            foreach (string path in MaterialAssets)
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) materialAssets++;

            int textureAssets = 0;
            foreach (string path in TextureAssets)
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) textureAssets++;

            int hardErrors = rendererSlots + components + (rootPresent ? 1 : 0) + materialAssets + textureAssets;

            Debug.Log(
                "[M58.0D ROLLBACK Diagnose]\n" +
                $"Scene={scene.name}\n" +
                $"M58DRendererSlots={rendererSlots}\n" +
                $"M58DRuntimeComponents={components}\n" +
                $"M58DRootPresent={rootPresent}\n" +
                $"M58DMaterialAssets={materialAssets}\n" +
                $"M58DTextureAssets={textureAssets}\n" +
                $"HardErrors={hardErrors}\n" +
                $"STATUS={(hardErrors == 0 ? "ROLLBACK_PASS" : "CHECK_REQUIRED")}");
        }

        private static bool UsesM58DMaterial(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null) return false;

            foreach (Material material in materials)
            {
                if (material != null &&
                    material.name.StartsWith(MaterialPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return "<null>";
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
