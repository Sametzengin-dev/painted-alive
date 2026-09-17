#if UNITY_EDITOR
using System;
using PaintedAlive.Paint.Watercolor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    /// <summary>
    /// M55.6.2 targeted repair for scene-authored Atelier WatercolorFlowSurface
    /// templates. It does not rebuild traversal, routes, gameplay tools, or the
    /// imported Atelier model; it only binds the component references that the
    /// M13 WatercolorFlowSurface expects to be serialized.
    /// </summary>
    public static class RepairAtelierWatercolorReferences_M55_6_2
    {
        private const string MenuRoot = "Tools/Painted Alive/Milestones/";
        private const string IntegrationRootName = "M55_4_AtelierTestScene";
        private const string GeneratedRootName = "M55_6_AtelierGameplayPass";

        [MenuItem(MenuRoot + "55.6.2 - Repair Watercolor References")]
        public static void Repair()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("M55.6.2", "Play Mode'dan çıkıp tekrar çalıştır.", "Tamam");
                return;
            }

            GameObject integrationRoot = GameObject.Find(IntegrationRootName);
            if (integrationRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "M55.6.2",
                    "M55.4 Atelier root bulunamadı: " + IntegrationRootName,
                    "Tamam");
                return;
            }

            Transform generatedRoot = FindChildRecursive(integrationRoot.transform, GeneratedRootName);
            if (generatedRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "M55.6.2",
                    "M55.6 generated gameplay pass bulunamadı. Önce '55.6 - Setup + Repair Atelier Gameplay Pass' çalıştır.",
                    "Tamam");
                return;
            }

            WatercolorFlowSurface[] flows = generatedRoot.GetComponentsInChildren<WatercolorFlowSurface>(true);
            int repaired = 0;
            int missingComponents = 0;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("M55.6.2 Repair Watercolor References");

            foreach (WatercolorFlowSurface flow in flows)
            {
                if (flow == null)
                    continue;

                MeshFilter filter = flow.GetComponent<MeshFilter>();
                MeshRenderer renderer = flow.GetComponent<MeshRenderer>();
                MeshCollider collider = flow.GetComponent<MeshCollider>();

                if (filter == null || renderer == null || collider == null)
                {
                    missingComponents++;
                    Debug.LogError(
                        $"[M55.6.2] {flow.name}: MeshFilter/MeshRenderer/MeshCollider eksik; otomatik referans repair uygulanamadı.",
                        flow);
                    continue;
                }

                Undo.RecordObject(flow, "Bind WatercolorFlowSurface references");
                SerializedObject serialized = new SerializedObject(flow);
                SerializedProperty meshFilter = serialized.FindProperty("meshFilter");
                SerializedProperty meshRenderer = serialized.FindProperty("meshRenderer");
                SerializedProperty meshCollider = serialized.FindProperty("meshCollider");

                if (meshFilter == null || meshRenderer == null || meshCollider == null)
                {
                    Debug.LogError(
                        $"[M55.6.2] {flow.name}: WatercolorFlowSurface serialized dependency fields bulunamadı.",
                        flow);
                    continue;
                }

                meshFilter.objectReferenceValue = filter;
                meshRenderer.objectReferenceValue = renderer;
                meshCollider.objectReferenceValue = collider;
                serialized.ApplyModifiedProperties();

                EditorUtility.SetDirty(flow);
                repaired++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            AssetDatabase.SaveAssets();

            string message =
                $"WatercolorFlowSurface bulundu: {flows.Length}\n" +
                $"Referansı bağlanan: {repaired}\n" +
                $"Eksik component: {missingComponents}\n\n" +
                "Ctrl+S ile sahneyi kaydet, Console'u temizle ve Play Mode'u tekrar dene.";

            Debug.Log("[Painted Alive M55.6.2] " + message.Replace("\n", " | "), integrationRoot);
            EditorUtility.DisplayDialog("M55.6.2 Watercolor Repair", message, "Tamam");
        }

        [MenuItem(MenuRoot + "55.6.2 - Diagnose Watercolor References")]
        public static void Diagnose()
        {
            GameObject integrationRoot = GameObject.Find(IntegrationRootName);
            Transform generatedRoot = integrationRoot != null
                ? FindChildRecursive(integrationRoot.transform, GeneratedRootName)
                : null;

            if (generatedRoot == null)
            {
                EditorUtility.DisplayDialog("M55.6.2 Diagnose", "M55.6 generated root bulunamadı.", "Tamam");
                return;
            }

            WatercolorFlowSurface[] flows = generatedRoot.GetComponentsInChildren<WatercolorFlowSurface>(true);
            int pass = 0;
            int fail = 0;

            foreach (WatercolorFlowSurface flow in flows)
            {
                if (flow == null)
                    continue;

                SerializedObject serialized = new SerializedObject(flow);
                UnityEngine.Object filter = serialized.FindProperty("meshFilter")?.objectReferenceValue;
                UnityEngine.Object renderer = serialized.FindProperty("meshRenderer")?.objectReferenceValue;
                UnityEngine.Object collider = serialized.FindProperty("meshCollider")?.objectReferenceValue;

                bool ok = filter != null && renderer != null && collider != null;
                if (ok) pass++; else fail++;

                Debug.Log(
                    $"[M55.6.2] {(ok ? "PASS" : "FAIL")} | {GetPath(flow.transform)} | " +
                    $"MeshFilter={(filter != null ? "OK" : "MISSING")} | " +
                    $"MeshRenderer={(renderer != null ? "OK" : "MISSING")} | " +
                    $"MeshCollider={(collider != null ? "OK" : "MISSING")}",
                    flow);
            }

            EditorUtility.DisplayDialog(
                "M55.6.2 Diagnose",
                $"Flows={flows.Length}\nPASS={pass}\nFAIL={fail}",
                "Tamam");
        }

        private static Transform FindChildRecursive(Transform root, string exactName)
        {
            if (root == null)
                return null;

            if (string.Equals(root.name, exactName, StringComparison.Ordinal))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildRecursive(root.GetChild(i), exactName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static string GetPath(Transform target)
        {
            if (target == null)
                return "<null>";

            string path = target.name;
            Transform current = target.parent;
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
